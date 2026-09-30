using Anthropic;
using Anthropic.Models.Messages;

namespace ClaudeDemo.Scenarios;

// Automatically sourced citations. Claude can cite the exact
// source passage for each claim - useful whenever traceability matters
// (compliance, customer support, legal...).
public static class Citations
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Sourced citations ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        var leavePolicy =
            "Article 4 - Paid leave. Employees are entitled to 25 business days of paid "
            + "leave per year. Unused days as of May 31 can be carried over for the "
            + "following 3 months, unless otherwise agreed with the manager. "
            + "Article 5 - RTT days. An allowance of 10 RTT days (reduced working-time "
            + "days) is granted pro rata to time worked over the calendar year.";

        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeSonnet5,
            MaxTokens = 500,
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = new List<ContentBlockParam>
                    {
                        new DocumentBlockParam(new PlainTextSource { Data = leavePolicy })
                        {
                            Title = "Excerpt from the internal policy",
                            Citations = new CitationsConfigParam { Enabled = true },
                        },
                        new TextBlockParam
                        {
                            Text = "How many paid leave days and RTT days do I get per year?",
                        },
                    },
                },
            ],
        };

        var response = await client.Messages.Create(parameters);

        foreach (var block in response.Content)
        {
            if (!block.TryPickText(out var text))
            {
                continue;
            }

            Console.WriteLine(text.Text);

            foreach (var citation in text.Citations ?? [])
            {
                if (citation.TryPickCitationCharLocation(out var loc))
                {
                    Console.WriteLine($"  cited source: \"{loc.CitedText}\"");
                }
            }
        }
    }
}
