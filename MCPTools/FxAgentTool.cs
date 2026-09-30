using System.Text.Json;
using Anthropic;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Messages;
using Beta = Anthropic.Models.Beta.Messages;

namespace ClaudeDemo.MCPTools;

// Sub-agent 1: currency conversion specialist. From the orchestrator's point
// of view it is a plain tool; internally it is its own Claude call (Haiku 4.5).
public static class FxAgentTool
{
    public static BetaRunnableTool Create(AnthropicClient client) =>
        new()
        {
            Name = "fx_agent",
            Definition = new Beta::BetaTool
            {
                Name = "fx_agent",
                Description =
                    "Delegates to the currency conversion specialist agent. Give it a "
                    + "plain-language instruction describing the amount(s) to convert to EUR.",
                InputSchema = new Beta::InputSchema
                {
                    Properties = new Dictionary<string, JsonElement>
                    {
                        ["instruction"] = JsonSerializer.SerializeToElement(
                            new { type = "string", description = "What to convert, in natural language" }
                        ),
                    },
                    Required = ["instruction"],
                },
            },
            Run = async (call, _) =>
            {
                var instruction = call.Input.TryGetValue("instruction", out var i) ? i.GetString() ?? "" : "";
                Console.WriteLine($"  [orchestrator -> fx_agent] {instruction}");

                var fxResponse = await client.Messages.Create(
                    new MessageCreateParams
                    {
                        Model = Model.ClaudeHaiku4_5,
                        MaxTokens = 512,
                        System =
                            "You are a currency conversion specialist. Use this fixed indicative "
                            + "rate: 1 USD = 0.92 EUR. Convert the amount(s) you are given and "
                            + "answer with only the converted EUR amounts, one per line, no extra "
                            + "commentary.",
                        Messages = [new() { Role = Role.User, Content = instruction }],
                    }
                );

                var result = SubAgentText.Of(fxResponse.Content);
                Console.WriteLine($"  [fx_agent -> orchestrator] {result}");
                return result;
            },
        };
}
