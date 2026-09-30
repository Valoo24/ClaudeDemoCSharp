using ClaudeDemo;
using ClaudeDemo.Scenarios;
using ClaudeDemo.Demo;

AppConfig.ApplyApiKey();

Scenario[] scenarios =
[
    new("1", "Simple prompt with streaming (type your own prompt)", SimpleChat.RunAsync),
    new("2", "Multi-turn chat (history + streaming + caching)", MultiTurnChatSimple.RunAsync),
    new("3", "Prompt caching (cost reduction)", PromptCaching.RunAsync),
    new("4", "Assistant prefill (force the start of the answer)", AssistantPrefill.RunAsync),
    new("5", "Structured outputs (guaranteed JSON)", StructuredOutputs.RunAsync),
    new("6", "ToolRunner - one tool (Open-Meteo weather)", ToolRunnerWeather.RunAsync),
    new("7", "MCP connector (remote server, no client-side loop)", McpConnector.RunAsync),
    new("8", "Custom Skill via the API", CustomSkill.RunAsync),
    new("9", "Managed Agent - expense compliance (session + event stream)", ManagedAgentScenario.RunSingleAgentAsync),
    new("10", "Managed Agent - full expense workflow (6 Skills + advisor subagent)", ManagedAgentScenario.RunWorkflowAsync),
    new("11", "Batch processing - create a batch (Batches API)", BatchProcessing.CreateAsync),
    new("12", "Batch processing - retrieve its results later", BatchProcessing.RetrieveAsync),
];

Scenario[] referenceScenarios =
[
    new("1", "Token counting (count before sending, compare with the usage)", TokenCount.RunAsync),
    new("2", "Tool use - manual loop (the fundamentals)", BasicToolUse.RunAsync),
    new("3", "Automatically sourced citations", Citations.RunAsync),
    new("4", "Agent team - handoff between peers", Handoff.RunAsync),
    new("5", "Simple prompt + logging (input, output, tokens)", LoggedChat.RunAsync),
    new("6", "Multi-turn chat (history, streaming, caching, trimming)", MultiTurnChat.RunAsync),
    new("7", "Streaming (real-time response)", Streaming.RunAsync),
    new("8", "Multi-tool agent with ToolRunner", ToolRunnerAgent.RunAsync),
    new("9", "Agent team - fan-out / fan-in", AgentTeamFanOut.RunAsync),
    new("10", "Multi-agent orchestration (agents as tools)", MultiAgentOrchestrator.RunAsync),
];

Scenario[] bonusScenarios =
[
    new("1", "Vision (image analysis)", Vision.RunAsync),
    new("2", "PDF support", PdfSupport.RunAsync),
];

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
{
    Console.WriteLine("WARNING: no API key configured.");
    Console.WriteLine("  Set \"Anthropic:ApiKey\" in appsettings.local.json, or:");
    Console.WriteLine("  PowerShell: $env:ANTHROPIC_API_KEY = \"sk-ant-...\"");
    Console.WriteLine("  bash      : export ANTHROPIC_API_KEY=\"sk-ant-...\"");
    Console.WriteLine();
}

// To run a scenario directly without going through the menu: dotnet run -- 3
if (args.Length > 0)
{
    var requestedScenario = Array.Find(scenarios, s => s.Number == args[0]);
    if (requestedScenario != null)
    {
        await requestedScenario.Run();
        return;
    }
    Console.WriteLine($"Unknown scenario '{args[0]}'.");
    return;
}

var hiddenCommands = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
{
    ["/ref"] = () => ScenarioMenu.RunAsync("References", referenceScenarios, "Back"),
    ["/bonus"] = () => ScenarioMenu.RunAsync("Bonus", bonusScenarios, "Back"),
};

await ScenarioMenu.RunAsync("Claude API Demo - Official C#/.NET SDK", scenarios, "Quit", hiddenCommands);
