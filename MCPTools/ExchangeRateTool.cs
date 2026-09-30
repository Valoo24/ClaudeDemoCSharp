using System.Text.Json;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Beta.Messages;

namespace ClaudeDemo.MCPTools;

// Fake currency conversion (fixed indicative rate, demo only).
public static class ExchangeRateTool
{
    public static BetaRunnableTool Create() =>
        new()
        {
            Name = "exchange_rate",
            Definition = new BetaTool
            {
                Name = "exchange_rate",
                Description = "Converts an amount from one currency to another.",
                InputSchema = new InputSchema
                {
                    Properties = new Dictionary<string, JsonElement>
                    {
                        ["amount"] = JsonSerializer.SerializeToElement(
                            new { type = "number", description = "Amount to convert" }
                        ),
                        ["from_currency"] = JsonSerializer.SerializeToElement(
                            new { type = "string", description = "Source currency, e.g. EUR" }
                        ),
                        ["to_currency"] = JsonSerializer.SerializeToElement(
                            new { type = "string", description = "Target currency, e.g. USD" }
                        ),
                    },
                    Required = ["amount", "from_currency", "to_currency"],
                },
            },
            Run = (call, _) =>
            {
                var amount = call.Input.TryGetValue("amount", out var m) ? m.GetDouble() : 0;
                var fromCurrency = call.Input.TryGetValue("from_currency", out var f)
                    ? f.GetString() ?? ""
                    : "";
                var toCurrency = call.Input.TryGetValue("to_currency", out var t)
                    ? t.GetString() ?? ""
                    : "";
                Console.WriteLine($"  [tool] exchange_rate({amount} {fromCurrency} -> {toCurrency})");
                var rate = 1.08;
                return Task.FromResult<BetaToolResultBlockParamContent>(
                    $"{amount:F2} {fromCurrency} = {amount * rate:F2} {toCurrency} (indicative rate: {rate})"
                );
            },
        };
}
