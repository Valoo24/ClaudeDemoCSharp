using System.Text.Json;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Beta.Messages;

namespace ClaudeDemo.MCPTools;

// Fake employee benefits card balance lookup (hard-coded answer, demo only).
public static class CardBalanceTool
{
    public static BetaRunnableTool Create() =>
        new()
        {
            Name = "check_card_balance",
            Definition = new BetaTool
            {
                Name = "check_card_balance",
                Description =
                    "Returns the available balance on an employee benefits card, identified "
                    + "by its last 4 digits.",
                InputSchema = new InputSchema
                {
                    Properties = new Dictionary<string, JsonElement>
                    {
                        ["last_digits"] = JsonSerializer.SerializeToElement(
                            new { type = "string", description = "Last 4 digits of the card" }
                        ),
                    },
                    Required = ["last_digits"],
                },
            },
            Run = (call, _) =>
            {
                var digits = call.Input.TryGetValue("last_digits", out var c)
                    ? c.GetString() ?? ""
                    : "";
                Console.WriteLine($"  [tool] check_card_balance({digits})");
                return Task.FromResult<BetaToolResultBlockParamContent>(
                    $"Card ***{digits}: balance of 42.50 EUR (meal vouchers)."
                );
            },
        };
}
