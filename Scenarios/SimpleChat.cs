using Anthropic;
using Anthropic.Models.Messages;
using ClaudeDemo.Demo;

namespace ClaudeDemo.Scenarios;

// Sends a single prompt to Claude and streams the answer to the console as it is generated.
public static class SimpleChat
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Simple prompt with streaming ===\n");
        Console.Write("[You]: ");
        var prompt = DemoInput.ReadLine(DemoPrompts.SimpleChat);

        // Reads the API key from ANTHROPIC_API_KEY; the explicit timeout replaces the shorter default
        // derived from MaxTokens, and MaxRetries = 0 disables the automatic retries.
        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        // The request: the model, a ceiling on the output tokens and the conversation messages.
        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeHaiku4_5_20251001,
            MaxTokens = 1024,
            Messages = [
            new MessageParam() { 
                Role = Role.User,
                Content = prompt!
            }],
        };

        Console.WriteLine("\n[Claude]");


        // Non-streaming call: returns the complete message at once, as a list of content blocks.
        //var response = await client.Messages.Create(parameters);
        //foreach (var contentBlock in response.Content)
        //    if (contentBlock.TryPickText(out var textBlock)) Console.WriteLine(textBlock.Text);

        // Streaming call: yields events as they are generated, the text arrives in content block deltas.
        await foreach (var streamEvent in client.Messages.CreateStreaming(parameters))
        {
            if (streamEvent.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var text))
            {
                Console.Write(text.Text);
            }
        }
    }
}
