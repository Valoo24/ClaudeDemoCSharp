using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using ClaudeDemo.Const;
using ClaudeDemo.Demo;

namespace ClaudeDemo.Scenarios;

// Ends the conversation with a partial Assistant message so Claude continues writing from exactly that point,
// here the start of an appsettings.json. A prefill cannot end with trailing whitespace and is no longer supported since Claude 4.6.
public static class AssistantPrefill
{
    private const string Prefill = """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Warning"
            }
          },
          "AllowedHosts": "*",
          "
        """;

    public static async Task RunAsync()
    {
        Console.WriteLine("=== Assistant prefill ===\n");
        Console.WriteLine("Describe the appsettings.json you need.");
        Console.Write("[You]: ");
        var prompt = DemoInput.ReadLine(DemoPrompts.AssistantPrefill);

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        await SendAsync(client, Model.ClaudeHaiku4_5_20251001, prompt!);

        // The same prompt on a model that no longer supports prefill: the API rejects the request.
        Console.Write("\n[You]: ");
        prompt = DemoInput.ReadLine(prompt!);

        await SendAsync(client, Model.ClaudeSonnet5, prompt!);
    }

    static async Task SendAsync(AnthropicClient client, Model model, string prompt)
    {
        Console.WriteLine($"\n[Model: {JsonSerializer.SerializeToElement(model).GetString()}]");

        try
        {
            var response = await client.Messages.Create(
                new MessageCreateParams
                {
                    Model = model,
                    MaxTokens = 1024,
                    System = SystemPrompt.DotNetSeniorDev,
                    Messages =
                    [
                        new() { Role = Role.User, Content = prompt },
                        // A last message with the Assistant role is the prefill: Claude continues from it.
                        new() { Role = Role.Assistant, Content = Prefill },
                    ],
                }
            );

            // The response only contains the continuation, so the prefill is printed separately (in yellow).
            Console.WriteLine("[Claude]");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(Prefill);
            Console.ResetColor();

            foreach (var contentBlock in response.Content)
                if (contentBlock.TryPickText(out var textBlock)) Console.Write(textBlock.Text);

            Console.WriteLine();
        }
        // A rejected request (HTTP 400) is thrown as a typed exception carrying the API's error message.
        catch (AnthropicBadRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[API error] {ex.Message}");
            Console.ResetColor();
        }
    }
}
