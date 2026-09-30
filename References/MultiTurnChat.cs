using System.Text;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;

namespace ClaudeDemo.Scenarios;

// Multi-turn chat, done properly.
//
// The Messages API is STATELESS: Claude remembers nothing between two calls. A
// "conversation" is just a list of alternating user / assistant messages that
// YOUR code keeps and re-sends in full on every turn (a single-prompt call only
// ever sends one message). Every practice below follows from that single fact:
//
//   1. History lives client-side and is only modified once a turn has fully succeeded.
//   2. The system prompt goes in the top-level `System` parameter (never as messages[0]) and never changes.
//   3. Streaming for the display, accumulation for the history.
//   4. stop_reason is checked on every turn, never assumed to be "end_turn".
//   5. Prompt caching on the whole prefix (system + history) with a single line.
//   6. The history is bounded - in batches - so it can't grow (and be billed) forever.
//   7. SDK retries + typed exceptions; a failed turn leaves the history untouched.
//
// Commands: /history (shows what is re-sent on every turn), /reset, /exit.
public static class MultiTurnChat
{
    // Streaming makes a generous MaxTokens safe (no HTTP timeout on long replies).
    const int MaxTokens = 4096;

    // History bounds. A "turn" = one user message + Claude's reply. Deliberately
    // small so the trim is easy to trigger during a live demo; in production, size
    // it from the real token usage (see PrintUsage) rather than from a turn count.
    const int MaxTurnsKept = 10;
    const int TurnsDroppedPerTrim = 5;

    // Stable on purpose: no timestamp, no per-turn value. Any change here invalidates
    // the cached prefix (render order is tools -> system -> messages). Something
    // dynamic belongs in the user message, not in the system prompt.
    const string SystemPrompt = """
        You are a pragmatic senior .NET engineer helping colleagues during a live demo.

        Style:
        - Answer in the language the user writes in.
        - Be concise: a short paragraph or a few bullet points, unless asked for more.
        - Your output is shown in a plain-text terminal: no markdown headings or tables.
          Short code snippets in fenced blocks are fine.
        - If you are not sure about something, say so instead of guessing.

        Conversation:
        - Use what was said earlier in the conversation; never ask again for
          information the user has already given you.
        """;

    public static async Task RunAsync()
    {
        Console.WriteLine("=== Multi-turn chat (best practices) ===\n");
        Console.WriteLine("  /history   show the messages re-sent on every turn");
        Console.WriteLine("  /reset     start a new conversation");
        Console.WriteLine("  /exit      back to the menu");

        // The SDK retries 408/409/429/5xx and connection errors with exponential
        // backoff (the other scenarios set 0 to fail fast; a chat should absorb
        // transient errors instead of surfacing them to the user).
        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 2 };

        // The entire conversation state. Invariant: user, assistant, user, assistant...
        // (starts with a user message, always an even count) - which is why trimming
        // and committing are done a turn at a time.
        List<MessageParam> history = [];

        while (true)
        {
            Console.Write("\n[You]: ");
            var input = Console.ReadLine()?.Trim();

            if (input is null or "/exit") break; // null = end of input (Ctrl+Z, closed pipe)
            if (input.Length == 0) continue; // the API rejects empty text blocks
            if (input == "/reset")
            {
                history.Clear();
                Meta("(new conversation)");
                continue;
            }
            if (input == "/history")
            {
                PrintHistory(history);
                continue;
            }

            TrimHistory(history);

            var userTurn = new MessageParam { Role = Role.User, Content = input };

            try
            {
                Console.WriteLine("\n[Claude]");

                // The request is built from a COPY of the history + the new message:
                // `history` itself is not touched until the reply is complete.
                var reply = await StreamReplyAsync(client, [.. history, userTurn]);

                Console.WriteLine();
                PrintUsage(reply);
                ReportStopReason(reply.StopReason);

                // Commit the turn only now. If the call had failed halfway (network,
                // 529 overloaded...) we would have left a dangling user message in the
                // history, and the next request would carry two user turns in a row.
                // A refused turn is not kept either: it would keep being re-sent.
                if (reply.StopReason != StopReason.Refusal && !string.IsNullOrWhiteSpace(reply.Text))
                {
                    history.Add(userTurn);
                    history.Add(new MessageParam { Role = Role.Assistant, Content = reply.Text });
                }
                else
                {
                    Meta("(this turn was not added to the history)");
                }
            }
            // Most specific first. Retryable errors were already retried by the SDK
            // (MaxRetries); what reaches us here is worth a clear message.
            catch (AnthropicRateLimitException)
            {
                Fail("Rate limit (429) still exceeded after retries: wait a moment, then resend.");
            }
            catch (AnthropicIOException)
            {
                Fail("Network error: check the connection, then resend.");
            }
            catch (AnthropicApiException ex)
            {
                Fail($"API error {(int)ex.StatusCode}: {ex.Message}");
            }
            catch (AnthropicServiceException ex) // e.g. an error event received mid-stream
            {
                Fail($"Error during streaming: {ex.Message}");
            }
        }
    }

    static async Task<Reply> StreamReplyAsync(AnthropicClient client, IReadOnlyList<MessageParam> messages)
    {
        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeSonnet5,
            MaxTokens = MaxTokens,
            System = SystemPrompt,
            Messages = messages,
            // Top-level cache_control: asks the API to cache everything up to the last
            // block of the request. Each turn then re-reads the previous turns from the
            // cache (about 90% cheaper, and faster) instead of paying for them again, and
            // the breakpoint moves forward on its own as the conversation grows.
            // Silently ignored below the model's minimum cacheable prefix: 1024 tokens
            // on Sonnet 5, 4096 on Haiku 4.5.
            CacheControl = new CacheControlEphemeral(),
        };

        var text = new StringBuilder();
        Usage? inputUsage = null;
        long outputTokens = 0;
        StopReason? stopReason = null;

        // Stream to the screen AND accumulate: what the user saw is exactly what goes
        // into the history. Only text blocks here; with tools or extended thinking you
        // would keep the full content blocks instead (see the Handoff scenario).
        await foreach (var streamEvent in client.Messages.CreateStreaming(parameters))
        {
            if (streamEvent.TryPickStart(out var start))
            {
                inputUsage = start.Message.Usage; // input side: known as soon as the stream opens
            }
            else if (streamEvent.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var chunk))
            {
                Console.Write(chunk.Text);
                text.Append(chunk.Text);
            }
            else if (streamEvent.TryPickDelta(out var end))
            {
                stopReason = end.Delta.StopReason?.Value(); // why the generation stopped
                outputTokens = end.Usage.OutputTokens;
            }
        }

        return new Reply(text.ToString(), stopReason, inputUsage, outputTokens);
    }

    // Bounds the history so the prompt (and the bill) can't grow forever.
    //
    // Old turns are dropped in BATCHES, not one per message: the cache matches an
    // identical prefix, so sliding the window by one turn on every request would
    // rewrite the start of the conversation - and miss the cache - every single time.
    // Dropping a batch costs one cache write, then every following turn hits again.
    //
    // Dropping is the simplest strategy, and Claude then genuinely forgets those turns.
    // Alternatives: summarize the dropped turns into one message, or let the API do it
    // (server-side compaction / context editing, beta, via client.Beta.Messages).
    static void TrimHistory(List<MessageParam> history)
    {
        if (history.Count / 2 < MaxTurnsKept)
            return;

        history.RemoveRange(0, TurnsDroppedPerTrim * 2); // whole turns: alternation is preserved
        Meta($"[history] the {TurnsDroppedPerTrim} oldest turns were dropped to keep the prompt small.");
    }

    // The tokens really sent, measured by the API (no guessing with a character
    // count): InputTokens only counts what comes AFTER the last cache breakpoint,
    // so the full prompt size is the sum of the three counters.
    static void PrintUsage(Reply reply)
    {
        if (reply.Usage is not { } usage)
            return;

        long read = usage.CacheReadInputTokens ?? 0;
        long written = usage.CacheCreationInputTokens ?? 0;
        Meta(
            $"[prompt: {usage.InputTokens + read + written} tokens = {read} read from cache"
                + $" + {written} written to cache + {usage.InputTokens} uncached | reply: {reply.OutputTokens} tokens]"
        );
    }

    // EndTurn is the normal case. The others each deserve their own handling.
    static void ReportStopReason(StopReason? stopReason)
    {
        switch (stopReason)
        {
            case StopReason.MaxTokens:
                // The truncated reply is kept in the history (it is what the user saw)
                // so that "continue" works.
                Meta("[stop_reason: max_tokens] The reply was cut off: type 'continue', or raise MaxTokens.");
                break;
            case StopReason.Refusal:
                Meta("[stop_reason: refusal] Claude declined to answer this request.");
                break;
            case StopReason.ModelContextWindowExceeded:
                Meta("[stop_reason: model_context_window_exceeded] The context window is full: /reset, or trim more.");
                break;
        }
    }

    // Shows what is really re-sent on every request - the point of the demo:
    // Claude's "memory" is nothing more than this list.
    static void PrintHistory(List<MessageParam> history)
    {
        Console.WriteLine($"\n{history.Count} messages re-sent with every request (turns: {history.Count / 2}):");
        foreach (var message in history)
        {
            message.Content.TryPickString(out var content);
            var line = (content ?? "").ReplaceLineEndings(" ");
            if (line.Length > 70)
                line = line[..70] + "...";
            var speaker = message.Role == Role.User ? "You" : "Claude";
            Console.WriteLine($"  {speaker,-7} {line}");
        }
    }

    // Diagnostics are printed in gray so they stand out from the conversation.
    static void Meta(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    static void Fail(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[error] {message}");
        Console.ResetColor();
        Meta("(this turn was not added to the history - you can simply resend your message)");
    }

    sealed record Reply(string Text, StopReason? StopReason, Usage? Usage, long OutputTokens);
}
