using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace ClaudeDemo.Scenarios;

public static class BasicToolUse
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Tool use - the manual loop ===\n");
        Console.WriteLine("What city would you like to know the weather for?");
        var city = Console.ReadLine();

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        // 1. Describe the tool to Claude: a name, a precise description
        //    (this is what guides the model) and a JSON schema for the
        //    expected parameters.
        var weatherTool = new Tool
        {
            Name = "get_weather",
            Description =
                "Returns the current weather for a given city. Use this as soon as the "
                + "user asks about the weather or temperature in a specific place. "
                + "Does not provide multi-day forecasts.",
            InputSchema = new InputSchema
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["city"] = JsonSerializer.SerializeToElement(
                        new { type = "string", description = "City name, e.g. Lyon" }
                    ),
                },
                Required = ["city"],
            },
        };


        List<MessageParam> messages =
        [
            new() { 
                Role = Role.User, 
                Content = $"What's the weather like in {city} right now?" 
            },
        ];

        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeSonnet5,
            MaxTokens = 1024,
            Tools = [weatherTool],
            Messages = messages,
        };

        // 2. First call: Claude will most likely ask to use the tool instead
        //    of answering directly.
        var response = await client.Messages.Create(parameters);
        Console.WriteLine($"stop_reason: {response.StopReason}\n");

        if (response.StopReason != StopReason.ToolUse)
        {
            PrintText(response);
            return;
        }

        // 3. Replay the assistant's turn as-is (Claude needs to see its own
        //    tool request in the history), then prepare the tool result for
        //    the next turn.
        var assistantBlocks = new List<ContentBlockParam>();
        ToolUseBlock? toolCall = null;

        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var text))
            {
                Console.WriteLine($"[Claude says] {text.Text}");
                assistantBlocks.Add(new TextBlockParam { Text = text.Text });
            }
            else if (block.TryPickToolUse(out var toolUse))
            {
                toolCall = toolUse;
                Console.WriteLine(
                    $"[Claude requests the tool] {toolUse.Name}({JsonSerializer.Serialize(toolUse.Input)})"
                );
                assistantBlocks.Add(
                    new ToolUseBlockParam
                    {
                        ID = toolUse.ID,
                        Name = toolUse.Name,
                        Input = toolUse.Input,
                    }
                );
            }
        }

        if (toolCall == null)
        {
            return;
        }

        // 4. This is where you would run the real code (HTTP call, database
        //    lookup, etc.). For the demo, we simulate a response.
        var cityExemple = toolCall.Input.TryGetValue("city", out var v) ? v.GetString() : "?";
        var toolResult = $"In {cityExemple}: 17C, clear sky, light breeze.";
        Console.WriteLine($"[Our code runs the tool] -> {toolResult}\n");

        messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantBlocks });
        messages.Add(
            new MessageParam
            {
                Role = Role.User,
                Content = new List<ContentBlockParam>
                {
                    new ToolResultBlockParam { ToolUseID = toolCall.ID, Content = toolResult },
                },
            }
        );

        // 5. Second call: Claude receives the result and writes the final answer.
        var finalResponse = await client.Messages.Create(parameters with { Messages = messages });
        PrintText(finalResponse);
    }

    static void PrintText(Message message)
    {
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var text))
            {
                Console.WriteLine($"\n[Final answer] {text.Text}");
            }
        }
    }
}
