using Anthropic;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Beta.Messages;
using ClaudeDemo.Const;
using ClaudeDemo.Demo;
using ClaudeDemo.MCPTools;

namespace ClaudeDemo.Scenarios;

// Lets Claude call a single tool (the free Open-Meteo weather API) through the ToolRunner helper,
// which runs the tool and feeds its result back until Claude has written the final answer.
public static class ToolRunnerWeather
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== ToolRunner - one tool (Open-Meteo weather) ===\n");
        var prompt = DemoInput.ReadLine(DemoPrompts.ToolRunnerWeather);
        Console.WriteLine();

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        // A runnable tool bundles the definition (name, description, parameter schema) with the code to execute.
        var weatherTool = WeatherTool.Create();

        // ToolRunner (beta) wraps the whole loop: request, tool_use, run the tool, tool_result, until Claude has finished.
        var runner = client.Beta.Messages.ToolRunner(
            new MessageCreateParams
            {
                Model = Anthropic.Models.Messages.Model.ClaudeSonnet5,
                MaxTokens = 1024,
                System = SystemPrompt.WeatherPresenter,
                Messages = [new() { Role = Role.User, Content = prompt! }],
            },
            [weatherTool]
        );

        var turn = 0;
        // Each iteration is one turn of the loop; the StopReason is ToolUse until the final answer.
        await foreach (var message in runner)
        {
            turn++;
            Console.WriteLine($"--- Turn {turn} (stop_reason: {message.StopReason}) ---");
            foreach (var block in message.Content)
            {
                if (block.TryPickText(out var text))
                {
                    Console.WriteLine(text.Text);
                }
            }
            Console.WriteLine();
        }
    }
}
