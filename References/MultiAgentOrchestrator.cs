using Anthropic;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Messages;
using Beta = Anthropic.Models.Beta.Messages;
using ClaudeDemo.MCPTools;

namespace ClaudeDemo.Scenarios;

// An orchestrator (Sonnet) delegates currency conversion and compliance checks to two Haiku sub-agents exposed as tools,
// the "agents as tools" pattern: the orchestrator decides which to call and in what order, and the ToolRunner drives the loop.
public static class MultiAgentOrchestrator
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Multi-agent orchestration (agents as tools) ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        // Each sub-agent is a full Claude call with its own prompt and model, wrapped as a runnable tool.
        var fxAgentTool = FxAgentTool.Create(client);
        var complianceAgentTool = ComplianceAgentTool.Create(client);

        // To the orchestrator, sub-agents are tools like any other, so they are passed to the ToolRunner the same way.
        var runner = client.Beta.Messages.ToolRunner(
            new Beta::MessageCreateParams
            {
                Model = Model.ClaudeSonnet5,
                MaxTokens = 1024,
                System =
                    "You coordinate a small team of specialist agents to produce an expense "
                    + "report. Delegate currency conversion to fx_agent and policy checks to "
                    + "compliance_agent - never do those computations yourself. Finish with a "
                    + "short consolidated report: each item's EUR amount, its verdict, and the "
                    + "total approved reimbursement.",
                Messages =
                [
                    new()
                    {
                        Role = Beta::Role.User,
                        Content =
                            "A traveling employee submitted three expenses: a $340 hotel night in "
                            + "New York (lodging), a $60 client dinner (meals), and a $90 taxi "
                            + "(transport). Check compliance and give me the total reimbursement "
                            + "in EUR.",
                    },
                ],
            },
            [fxAgentTool, complianceAgentTool]
        );

        var turn = 0;
        await foreach (var message in runner)
        {
            turn++;
            Console.WriteLine($"\n--- Orchestrator turn {turn} (stop_reason: {message.StopReason}) ---");
            foreach (var block in message.Content)
            {
                if (block.TryPickText(out var text))
                {
                    Console.WriteLine(text.Text);
                }
            }
        }
    }
}
