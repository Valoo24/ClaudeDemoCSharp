using Anthropic;
using Anthropic.Models.Messages;
using ClaudeDemo.Demo;

namespace ClaudeDemo.Scenarios;

// Fan-out / fan-in: three reviewer agents evaluate the same request in parallel from different angles, then a fourth
// agent synthesizes their opinions into one decision. The team structure lives in the C# code (Task.WhenAll), not in a model's choices.
public static class AgentTeamFanOut
{
    sealed record Reviewer(string Name, string SystemPrompt);

    public static async Task RunAsync()
    {
        Console.WriteLine("=== Fan-out / fan-in agent team ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        Console.Write("[You]: ");
        var request = DemoInput.ReadLine(DemoPrompts.AgentTeamFanOut)!;
        Console.WriteLine();

        Reviewer[] reviewers =
        [
            new(
                "cost_controller",
                "You are a cost-control reviewer. Weigh this expense exception strictly "
                    + "against budget discipline and precedent. Be terse: 2-3 sentences, end "
                    + "with a leaning of APPROVE, DENY, or ESCALATE."
            ),
            new(
                "people_manager",
                "You are a people manager reviewing this expense exception with empathy for "
                    + "the employee's situation. Be terse: 2-3 sentences, end with a leaning "
                    + "of APPROVE, DENY, or ESCALATE."
            ),
            new(
                "compliance_auditor",
                "You are a compliance auditor reviewing this expense exception for policy "
                    + "and audit risk. Be terse: 2-3 sentences, end with a leaning of "
                    + "APPROVE, DENY, or ESCALATE."
            ),
        ];

        Console.WriteLine("Fanning out to 3 reviewer agents in parallel...\n");
        // Fan-out: one independent Messages call per reviewer, each with its own system prompt, started without awaiting.
        var reviewTasks = reviewers.Select(reviewer =>
            client.Messages.Create(
                new MessageCreateParams
                {
                    // Haiku for the reviewers keeps the cost of each extra agent low; Sonnet is kept for the synthesis.
                    Model = Model.ClaudeHaiku4_5,
                    MaxTokens = 300,
                    System = reviewer.SystemPrompt,
                    Messages = [new() { Role = Role.User, Content = request }],
                }
            )
        );

        // Waits for all the reviewers at once.
        var reviewResponses = await Task.WhenAll(reviewTasks);

        var reviews = reviewers
            .Zip(reviewResponses, (reviewer, response) => (reviewer.Name, Text: TextOf(response.Content)))
            .ToList();

        foreach (var (name, text) in reviews)
        {
            Console.WriteLine($"[{name}]\n{text}\n");
        }

        // Fan-in: a fourth call receives the three opinions as plain text and makes the final decision.
        var summary = string.Join("\n\n", reviews.Select(r => $"{r.Name}: {r.Text}"));
        var synthesis = await client.Messages.Create(
            new MessageCreateParams
            {
                Model = Model.ClaudeSonnet5,
                MaxTokens = 500,
                System =
                    "You are the decision-maker synthesizing three independent reviews of an "
                    + "expense exception into one final decision. Weigh all three, note any "
                    + "disagreement, and give a clear final verdict with a short rationale.",
                Messages =
                [
                    new() { Role = Role.User, Content = $"Request:\n{request}\n\nReviews:\n{summary}" },
                ],
            }
        );

        Console.WriteLine("[final decision]");
        Console.WriteLine(TextOf(synthesis.Content));
    }

    static string TextOf(IReadOnlyList<ContentBlock> content)
    {
        var parts = new List<string>();
        foreach (var block in content)
        {
            if (block.TryPickText(out var text))
            {
                parts.Add(text.Text);
            }
        }
        return string.Join("\n", parts);
    }
}
