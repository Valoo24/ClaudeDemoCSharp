using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using ClaudeDemo.Demo;

namespace ClaudeDemo.Scenarios;

// Keeps the conversation history in a list and resends all of it on every turn, streaming each answer.
// Prompt caching is enabled so the previous turns are not billed at full price again.
public static class MultiTurnChatSimple
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Simple multi-turn chat ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        // The API is stateless: Claude remembers nothing, so the whole history is sent on every call.
        List<MessageParam> messages = [];

        var autoPrompts = DemoPrompts.MultiTurnChat();

        while (true)
        {
            Console.Write("[You]: ");
            var input = DemoInput.ReadLine(autoPrompts);

            if (string.IsNullOrWhiteSpace(input) || input.Trim() == "/exit") break;

            messages.Add(new MessageParam { Role = Role.User, Content = input });

            var parameters = new MessageCreateParams
            {
                Model = Model.ClaudeSonnet5,
                MaxTokens = 1024,
                Messages = messages,
                // Caches the conversation prefix for 5 minutes so earlier turns are re-read at a lower price.
                CacheControl = new CacheControlEphemeral { Ttl = Ttl.Ttl5m },
            };

            var answer = new StringBuilder();
            Console.Write("[Claude]: ");

            await foreach (var streamEvent in client.Messages.CreateStreaming(parameters))
            {
                if (streamEvent.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var text))
                {
                    Console.Write(text.Text);
                    answer.Append(text.Text);
                }
            }

            // Claude's answer goes back into the history with the Assistant role, ready for the next turn.
            messages.Add(new MessageParam { Role = Role.Assistant, Content = answer.ToString() });

            Console.WriteLine("\n");
        }
    }
}
