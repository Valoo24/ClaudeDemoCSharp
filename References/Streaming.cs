using Anthropic;
using Anthropic.Models.Messages;

namespace ClaudeDemo.Scenarios;

// Streaming. The response is displayed as it is generated,
// just like in the claude.ai interface, instead of waiting for the full
// response - important for how responsive a UI feels (e.g. an Angular
// component wired to your .NET backend via SignalR/WebSocket).
public static class Streaming
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Streaming ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeHaiku4_5_20251001,
            MaxTokens = 500,
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = "Write a short paragraph about the value of streaming in an API.",
                },
            ],
        };

        await foreach (var streamEvent in client.Messages.CreateStreaming(parameters))
        {
            if (streamEvent.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var text))
                Console.Write(text.Text);
        }
    }
}
