using Anthropic;
using Anthropic.Models.Messages;
using ClaudeDemo.Const;
using ClaudeDemo.Demo;

namespace ClaudeDemo.Scenarios;

// Counts the tokens of a request BEFORE sending it (free, nothing is generated), then sends it and
// compares the count with the usage reported by the API. Useful to check a prompt against a budget
// or a context window, or to estimate the cost of a call, without paying for it.
public static class TokenCount
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Token counting ===\n");
        Console.Write("[You]: ");
        var prompt = DemoInput.ReadLine(DemoPrompts.TokenCount);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            Console.WriteLine("Nothing to send.");
            return;
        }

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        // Same model, system prompt and messages as the real request below: CountTokens accepts the
        // same inputs as Create (tools, documents and images as well) and returns the input size only.
        var count = await client.Messages.CountTokens(new MessageCountTokensParams
        {
            // Token counts are model-specific: count with the model that will answer.
            Model = Model.ClaudeSonnet5,
            System = SystemPrompt.DotNetSeniorDev,
            Messages = [new() { Role = Role.User, Content = prompt }],
        });

        Console.WriteLine($"\nInput tokens counted before sending: {count.InputTokens}");

        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = Model.ClaudeSonnet5,
            MaxTokens = 1024,
            System = SystemPrompt.DotNetSeniorDev,
            Messages = [new() { Role = Role.User, Content = prompt }],
        });

        var textBlocks = response.Content.Select(block => block.Value).OfType<TextBlock>();
        Console.WriteLine($"\n[Claude]\n{string.Concat(textBlocks.Select(block => block.Text))}");

        // The output size is only known after the call; the input size matches the count above
        // (no cache breakpoint here, so InputTokens is the whole prompt).
        Console.WriteLine($"\nInput tokens billed  : {response.Usage.InputTokens}");
        Console.WriteLine($"Output tokens billed : {response.Usage.OutputTokens}");
    }
}
