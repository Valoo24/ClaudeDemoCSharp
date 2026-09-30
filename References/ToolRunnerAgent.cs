using Anthropic;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Messages;
using Beta = Anthropic.Models.Beta.Messages;
using ClaudeDemo.MCPTools;

namespace ClaudeDemo.Scenarios;

// Gives Claude two tools through the ToolRunner and lets it chain them on its own:
// it checks a card balance, then converts the amount to US dollars.
public static class ToolRunnerAgent
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Multi-tool agent with ToolRunner ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        var balanceTool = CardBalanceTool.Create();
        var exchangeTool = ExchangeRateTool.Create();

        // Several tools can be passed; Claude decides which to call and in what order.
        var runner = client.Beta.Messages.ToolRunner(
            new Beta::MessageCreateParams
            {
                Model = Model.ClaudeHaiku4_5_20251001,
                MaxTokens = 1024,
                Messages =
                [
                    new()
                    {
                        Role = Beta::Role.User,
                        Content =
                            "Check the balance of the card ending in 7742, then tell me how "
                            + "much that is converted to US dollars.",
                    },
                ],
            },
            [balanceTool, exchangeTool]
        );

        var turn = 0;
        await foreach (var message in runner)
        {
            turn++;
            Console.WriteLine($"\n--- Turn {turn} (stop_reason: {message.StopReason}) ---");
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
