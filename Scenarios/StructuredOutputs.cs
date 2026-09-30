using Anthropic;
using Anthropic.Helpers;
using Anthropic.Models.Messages;
using Anthropic.Services;
using ClaudeDemo.Demo;

namespace ClaudeDemo.Scenarios;

// Asks Claude to fill in a C# class instead of writing free text: the JSON schema is generated from the class
// attributes and the response is parsed into a typed object.
public static class StructuredOutputs
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Structured outputs ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        Console.Write("[Customer]: ");
        var customerMessage = DemoInput.ReadLine(DemoPrompts.StructuredOutputs);

        const string instruction = "Extract the information from this customer message:";
        Console.WriteLine($"[You]: {instruction}\n");

        // The generic overload sends the schema derived from SupportTicket and constrains Claude's output to it.
        var response = await client.Messages.Create<SupportTicket>(
            new MessageCreateParams
            {
                Model = Model.ClaudeHaiku4_5_20251001,
                MaxTokens = 512,
                Messages =
                [
                    new()
                    {
                        Role = Role.User,
                        Content = $"{instruction}\n\n{customerMessage}",
                    },
                ],
            }
        );

        // Parsed() returns the response as a SupportTicket instance.
        var ticket = response.Content[0].Parsed();

        Console.WriteLine(ticket!.ToJson());
    }
}

// [SchemaClass] describes the object as a whole in the generated schema, and [SchemaProperty] describes each
// property, with its constraints (format, allowed values).
[SchemaClass("A customer support ticket extracted from a customer message")]
public class SupportTicket : StructuredOutputModel
{
    [SchemaProperty("Customer's full name")]
    public string CustomerName { get; set; } = "";

    [SchemaProperty("Customer's email address", Format = StringFormat.Email)]
    public string? Email { get; set; }

    [SchemaProperty(
        "Category of the issue",
        Enum = new object[] { "login", "billing", "bug", "other" }
    )]
    public string Category { get; set; } = "";

    [SchemaProperty("True if the customer mentions an urgency or a close deadline")]
    public bool Urgent { get; set; }

    [SchemaProperty("One-sentence summary of the issue")]
    public string Summary { get; set; } = "";
}
