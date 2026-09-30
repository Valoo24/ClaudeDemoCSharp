using System.Text.Json;
using Anthropic;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Messages;
using Beta = Anthropic.Models.Beta.Messages;

namespace ClaudeDemo.MCPTools;

// Sub-agent 2: expense compliance specialist. From the orchestrator's point
// of view it is a plain tool; internally it is its own Claude call (Haiku 4.5).
public static class ComplianceAgentTool
{
    public static BetaRunnableTool Create(AnthropicClient client) =>
        new()
        {
            Name = "compliance_agent",
            Definition = new Beta::BetaTool
            {
                Name = "compliance_agent",
                Description =
                    "Delegates to the expense-policy compliance specialist agent. Give it EUR "
                    + "amounts with their category so it can check them against reimbursement caps.",
                InputSchema = new Beta::InputSchema
                {
                    Properties = new Dictionary<string, JsonElement>
                    {
                        ["instruction"] = JsonSerializer.SerializeToElement(
                            new { type = "string", description = "EUR amounts and categories to check" }
                        ),
                    },
                    Required = ["instruction"],
                },
            },
            Run = async (call, _) =>
            {
                var instruction = call.Input.TryGetValue("instruction", out var i) ? i.GetString() ?? "" : "";
                Console.WriteLine($"  [orchestrator -> compliance_agent] {instruction}");

                var complianceResponse = await client.Messages.Create(
                    new MessageCreateParams
                    {
                        Model = Model.ClaudeHaiku4_5,
                        MaxTokens = 512,
                        System =
                            "You are an expense-policy compliance specialist. Reimbursement caps "
                            + "per expense, in EUR: meals 25, transport none (must be itemized), "
                            + "lodging 150/night, equipment 150 (needs manager approval above), "
                            + "other 50. For each item you are given, state the category, the cap "
                            + "applied, the amount, and one verdict: APPROVED, "
                            + "NEEDS_MANAGER_APPROVAL, or OVER_CAP. Be concise.",
                        Messages = [new() { Role = Role.User, Content = instruction }],
                    }
                );

                var result = SubAgentText.Of(complianceResponse.Content);
                Console.WriteLine($"  [compliance_agent -> orchestrator] {result}");
                return result;
            },
        };
}
