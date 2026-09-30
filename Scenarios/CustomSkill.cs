using Anthropic;
using Anthropic.Models.Messages;
using ClaudeDemo.Demo;
using ClaudeDemo.Services;

namespace ClaudeDemo.Scenarios;

// Uses a custom Skill (expense-compliance-check), uploaded once in the Claude Console, by referencing its ID
// in the request container with the code execution tool enabled.
public static class CustomSkill
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Custom Skill via the API ===\n");
        Console.WriteLine("[Claude] : Tell me what expense you want to check. I'll analyze it for you.");
        Console.Write("[You]: ");
        var userInput = DemoInput.ReadLine(DemoPrompts.CustomSkill);
        var context = DemoPrompts.SatellitUserContext;
        DemoInput.ShowContext(context);
        var finalUserInput = $"{context}\n\n{userInput}";

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeHaiku4_5_20251001,
            MaxTokens = 1024,
            // Skills run in Anthropic's code execution sandbox, so this tool must be enabled.
            Tools = [new CodeExecutionTool20250825()],
            Container = new ContainerParams
            {
                // References the uploaded Skill by its ID; Custom distinguishes it from Anthropic's built-in Skills.
                Skills = [new SkillParams { SkillID = AppConfig.CustomSkillId!, Type = SkillParamsType.Custom }],
            },
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = finalUserInput,
                },
            ],
        };

        var stream = client.Messages.CreateStreaming(parameters);

        await StreamDisplayService.ShowAsync(stream, StreamDisplayMode.FinalAnswer);
    }
}
