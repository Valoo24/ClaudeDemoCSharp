using Anthropic;
using ClaudeDemo.Demo;
using ClaudeDemo.Services;

namespace ClaudeDemo.Scenarios;

// Chats with a Managed Agent created in the Claude Console and only referenced here by its ID: the C# code opens a session,
// sends messages and reads the event stream, while Anthropic runs the agent loop in a hosted container. Two agents share this code:
// a single compliance agent with one Skill, and a workflow coordinator composing six Skills and an advisor subagent.
public static class ManagedAgentScenario
{
    sealed record AgentDemo(
        string Name,
        string AgentId,
        string? MemoryInstructions,
        Queue<string> AutoPrompts,
        string? Welcome,
        StreamDisplayMode DisplayMode,
        string? Context = null
    );

    const string ExitCommand = "/exit";
    const StreamDisplayMode display = StreamDisplayMode.ThoughtProcess;

    public static Task RunSingleAgentAsync() =>
        RunAsync(
            new AgentDemo(
                "expense-compliance-agent",
                AppConfig.ManagedAgentId!,
                DemoPrompts.ManagedAgentComplianceMemoryInstructions,
                DemoPrompts.ManagedAgentCompliance,
                DemoPrompts.ManagedAgentComplianceWelcome,
                display
            )
        );

    public static Task RunWorkflowAsync() =>
        RunAsync(
            new AgentDemo(
                "expense-workflow-agent",
                AppConfig.ManagedAgentWorkflowId!,
                MemoryInstructions: null,
                DemoPrompts.ManagedAgentWorkflow,
                DemoPrompts.ManagedAgentWorkflowWelcome,
                display,
                DemoPrompts.SatellitUserContext
            )
        );

    static async Task RunAsync(AgentDemo agent)
    {
        Console.WriteLine($"=== Managed Agent: {agent.Name} ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        // The SDK has no high-level helper for Managed Agents yet, so this service wraps the beta session and event APIs.
        var claudeConsole = new ClaudeConsoleService(client, ConsoleLogging.Create<ClaudeConsoleService>());
        var sessionDisplay = new SessionDisplayService(claudeConsole);

        // A session is an instance of the agent running in an environment, with the memory store mounted as a resource.
        var sessionId = await claudeConsole.CreateSessionAsync(
            agent.AgentId,
            AppConfig.ManagedAgentEnvironmentId!,
            AppConfig.ManagedAgentMemoryStoreId!,
            $"{agent.Name} (C# demo)",
            agent.MemoryInstructions);

        Console.WriteLine();
        Console.WriteLine(agent.Welcome is null ? "Type your requests." : $"[Agent] : {agent.Welcome}");

        // The context is sent once, with the first message: the session keeps the history afterwards.
        var pendingContext = agent.Context;

        while (true)
        {
            Console.Write("\n[You] : ");
            var userPrompt = DemoInput.ReadLine(agent.AutoPrompts);
            if (userPrompt is null || userPrompt.Trim().ToLowerInvariant() == ExitCommand) break;

            var message = userPrompt;
            if (pendingContext is not null)
            {
                DemoInput.ShowContext(pendingContext);
                message = $"{pendingContext}\n\n{userPrompt}";
                pendingContext = null;
            }

            Console.WriteLine();
            await sessionDisplay.ShowAsync(sessionId, message, agent.DisplayMode);
            Console.WriteLine();
        }
    }
}
