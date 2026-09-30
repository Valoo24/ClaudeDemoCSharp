using Anthropic;
using Anthropic.Models.Messages;

namespace ClaudeDemo.Scenarios;

// Marks a large system prompt with a cache breakpoint, then compares the token usage of two identical
// calls to show the cache being written and then read.
public static class PromptCaching
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Prompt caching ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        var sentence =
            "Internal rule: every call to the billing API must go through the "
            + "Billing.Gateway module, which handles resilience, retries and logging. ";
        // Repeated to exceed the minimum cacheable size (1,024 tokens on Sonnet).
        var internalDocs = string.Concat(Enumerable.Repeat(sentence, 120));

        var system = new List<TextBlockParam>
        {
            new()
            {
                Text = internalDocs,
                // Same cache control as before, but set on a system block to place the breakpoint explicitly.
                CacheControl = new CacheControlEphemeral { Ttl = Ttl.Ttl5m },
            },
        };

        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeSonnet5,
            MaxTokens = 300,
            System = system,
            Messages = [new() { Role = Role.User, Content = "Summarize the rule in one sentence." }],
        };

        var firstCall = await client.Messages.Create(parameters);
        PrintUsage("First call (cache creation)", firstCall.Usage);

        var secondCall = await client.Messages.Create(parameters);
        PrintUsage("Second call (cache read)", secondCall.Usage);
    }

    static void PrintUsage(string label, Usage usage)
    {
        // Usage reports normal, cache-written and cache-read input tokens separately.
        Console.WriteLine($"\n{label}:");
        Console.WriteLine($"  input tokens billed at full price   : {usage.InputTokens}");
        Console.WriteLine($"  tokens written to the cache          : {usage.CacheCreationInputTokens ?? 0}");
        Console.WriteLine($"  tokens read from the cache (~90% off): {usage.CacheReadInputTokens ?? 0}");
    }
}
