using System.Text.Json;

namespace ClaudeDemo.Services;

// Sends a message to a Managed Agent session and displays the turn in the way chosen by the display mode.
public class SessionDisplayService(ClaudeConsoleService claudeConsole)
{
    public Task ShowAsync(string sessionId, string text, StreamDisplayMode mode) =>
        mode switch
        {
            StreamDisplayMode.ThoughtProcess => ShowThoughtProcessAsync(sessionId, text),
            StreamDisplayMode.FinalAnswer => ShowFinalAnswerAsync(sessionId, text),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

    // Only the final answer: the text of the last agent message of the turn.
    async Task ShowFinalAnswerAsync(string sessionId, string text)
    {
        var answer = "";

        // Streams the session events until the session goes idle.
        await foreach (var ev in claudeConsole.SendMessageAsync(sessionId, text))
        {
            // agent.message events carry the agent's text; the last one of the turn is the final answer.
            if (ev.TryPickAgentMessageEvent(out var messageEvent))
            {
                answer = string.Concat(
                    messageEvent.Content.Select(block => block.TryPickBetaManagedAgentsTextBlock(out var t) ? t.Text : ""));
            }
        }

        Console.WriteLine($"[Agent] : {answer}");
    }

    // Everything the agent does during a turn: the commands it runs in its container (which is what makes
    // the Skills visible) and, in a multiagent session, the messages exchanged with the subagents.
    async Task ShowThoughtProcessAsync(string sessionId, string text)
    {
        Console.Write("[Agent] : ");
        var midLine = true;

        void StartLine()
        {
            if (midLine) Console.WriteLine();
            midLine = false;
        }

        await foreach (var ev in claudeConsole.SendMessageAsync(sessionId, text))
        {
            if (ev.TryPickAgentToolUseEvent(out var toolUse))
            {
                // The agent is working in its container: reading a Skill, running a script...
                StartLine();
                Console.WriteLine($"  [tool: {toolUse.Name}] {Shorten(Describe(toolUse.Input))}");
            }
            else if (ev.TryPickAgentMessageEvent(out var agentMessage))
            {
                foreach (var block in agentMessage.Content)
                {
                    if (block.TryPickBetaManagedAgentsTextBlock(out var textBlock))
                    {
                        Console.Write(textBlock.Text);
                        midLine = true;
                    }
                }
            }

            // Multiagent sessions: the primary stream shows a condensed view of the
            // subagents (thread lifecycle and messages, not their every tool call).
            else if (ev.TryPickSessionThreadCreatedEvent(out var threadCreated))
            {
                StartLine();
                Console.WriteLine($"  [subagent thread started: {threadCreated.AgentName}]");
            }
            else if (ev.TryPickAgentThreadMessageSentEvent(out var sent))
            {
                StartLine();
                Console.WriteLine($"  [coordinator -> {sent.ToAgentName}]");
                foreach (var block in sent.Content)
                {
                    if (block.TryPickBetaManagedAgentsTextBlock(out var t)) PrintIndented(t.Text);
                }
            }
            else if (ev.TryPickAgentThreadMessageReceivedEvent(out var received))
            {
                StartLine();
                Console.WriteLine($"  [{received.FromAgentName} -> coordinator]");
                foreach (var block in received.Content)
                {
                    if (block.TryPickBetaManagedAgentsTextBlock(out var t)) PrintIndented(t.Text);
                }
            }
            else if (ev.TryPickSessionErrorEvent(out _))
            {
                StartLine();
                Console.WriteLine("  [session error - see the trace in the Console]");
            }
        }
    }

    // The command a tool call runs, e.g. "node scripts/check-compliance.ts equipment 202.4".
    static string Describe(IReadOnlyDictionary<string, JsonElement> input)
    {
        foreach (var key in new[] { "command", "file_path", "path", "pattern" })
        {
            if (input.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? "";
            }
        }
        return "";
    }

    static string Shorten(string text)
    {
        var singleLine = text.ReplaceLineEndings(" ");
        return singleLine.Length <= 140 ? singleLine : singleLine[..140] + "...";
    }

    static void PrintIndented(string text)
    {
        foreach (var line in text.ReplaceLineEndings("\n").Split('\n'))
        {
            Console.WriteLine($"      {line}");
        }
    }
}
