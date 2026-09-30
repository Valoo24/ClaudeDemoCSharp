using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace ClaudeDemo.Scenarios;

// Handoff pattern. Unlike the multi-agent orchestration scenario (an orchestrator that
// calls subagents as tools and stays in charge the whole time - "agents as
// tools") the agents here are peers: whichever one is currently active can
// transfer full ownership of the conversation to another peer via a handoff
// tool call, and that peer then answers the user directly from then on - no
// central orchestrator stays in the loop. This is the pattern behind
// support/triage systems ("let me transfer you to billing") and frameworks
// like OpenAI's Swarm / Agents SDK. Built on the stable (non-beta) Messages
// API with a manual loop: the system prompt and tool set change mid-
// conversation as control passes from one agent to the next, which the
// ToolRunner helper - built around one fixed system prompt/tool set for its
// whole loop - doesn't fit.
public static class Handoff
{
    sealed record Agent(string Name, string SystemPrompt, string[] HandoffTargets);

    static readonly Agent Triage = new(
        "triage",
        "You are a triage agent for expense-related requests. Never answer a question "
            + "yourself: always hand off. If the request is about currency conversion, hand "
            + "off to fx_specialist. If it's about expense policy or reimbursement caps, hand "
            + "off to compliance_specialist.",
        ["fx_specialist", "compliance_specialist"]
    );

    static readonly Agent FxSpecialist = new(
        "fx_specialist",
        "You are the currency conversion specialist. Answer conversion questions directly "
            + "and precisely, using this fixed indicative rate: 1 USD = 0.92 EUR. If the user "
            + "asks about expense policy or reimbursement caps instead, hand off to "
            + "compliance_specialist.",
        ["compliance_specialist"]
    );

    static readonly Agent ComplianceSpecialist = new(
        "compliance_specialist",
        "You are the expense-policy compliance specialist. Reimbursement caps per expense, "
            + "in EUR: meals 25, transport none (must be itemized), lodging 150/night, "
            + "equipment 150 (manager approval above), other 50. Answer directly, citing the "
            + "cap and a verdict (APPROVED, NEEDS_MANAGER_APPROVAL, or OVER_CAP). If the user "
            + "asks about currency conversion instead, hand off to fx_specialist.",
        ["fx_specialist"]
    );

    static readonly Dictionary<string, Agent> Agents = new[] { Triage, FxSpecialist, ComplianceSpecialist }.ToDictionary(
        a => a.Name
    );

    public static async Task RunAsync()
    {
        Console.WriteLine("=== Handoff pattern (peer agents transfer the conversation) ===\n");

        // Explicit timeout: the SDK derives a default timeout from MaxTokens for
        // non-streaming calls, which can be too short for network- or tool-heavy
        // requests (MCP, agent loops). A flat 5 minutes avoids surprise timeouts.
        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        List<MessageParam> messages = [];
        var currentAgent = Triage;

        // Two user turns in the same conversation: the second one arrives
        // after control has already been handed off once, and triggers a
        // second, direct peer-to-peer handoff (no going back through triage).
        string[] userTurns =
        [
            "How much is $500 in EUR?",
            "Actually, would a $500 hotel night be within our reimbursement policy?",
        ];

        foreach (var userInput in userTurns)
        {
            Console.WriteLine($"\n[user] {userInput}");
            messages.Add(new MessageParam { Role = Role.User, Content = userInput });

            // Keep handing off within the same user turn until an agent
            // actually answers instead of transferring again.
            while (true)
            {
                var response = await client.Messages.Create(
                    new MessageCreateParams
                    {
                        Model = Model.ClaudeSonnet5,
                        MaxTokens = 1024,
                        System = currentAgent.SystemPrompt,
                        Tools =
                        [
                            .. currentAgent.HandoffTargets.Select(target => new Tool
                            {
                                Name = $"handoff_to_{target}",
                                Description = $"Transfer this conversation to the {target} agent.",
                                InputSchema = new InputSchema
                                {
                                    Properties = new Dictionary<string, JsonElement>(),
                                },
                            }),
                        ],
                        Messages = messages,
                    }
                );

                var assistantBlocks = new List<ContentBlockParam>();
                ToolUseBlock? handoffCall = null;

                foreach (var block in response.Content)
                {
                    if (block.TryPickText(out var text))
                    {
                        Console.WriteLine($"[{currentAgent.Name}] {text.Text}");
                        assistantBlocks.Add(new TextBlockParam { Text = text.Text });
                    }
                    else if (block.TryPickToolUse(out var toolUse))
                    {
                        handoffCall = toolUse;
                        assistantBlocks.Add(
                            new ToolUseBlockParam { ID = toolUse.ID, Name = toolUse.Name, Input = toolUse.Input }
                        );
                    }
                }

                messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantBlocks });

                if (handoffCall == null)
                {
                    // The active agent answered directly: this user turn is done.
                    break;
                }

                var targetName = handoffCall.Name["handoff_to_".Length..];
                Console.WriteLine($"  [handoff] {currentAgent.Name} -> {targetName}");
                currentAgent = Agents[targetName];

                messages.Add(
                    new MessageParam
                    {
                        Role = Role.User,
                        Content = new List<ContentBlockParam>
                        {
                            new ToolResultBlockParam
                            {
                                ToolUseID = handoffCall.ID,
                                Content = $"Transferred to {targetName}.",
                            },
                        },
                    }
                );
                // Loop again: the new agent now sees the full shared history
                // and answers (or hands off again) in its own voice.
            }
        }
    }
}
