namespace ClaudeDemo.Demo;

// Console input for the demo: typing "/auto" instead of a prompt uses a ready-made one,
// so scenarios stay free of demo plumbing.
public static class DemoInput
{
    const string AutoCommand = "/auto";

    // Single-prompt scenarios: "/auto" always returns the same prompt.
    public static string? ReadLine(string autoPrompt) => ReadLine(() => autoPrompt);

    // Chat scenarios: each "/auto" returns the next prompt of the script, then null once it is exhausted.
    public static string? ReadLine(Queue<string> autoPrompts) =>
        ReadLine(() => autoPrompts.TryDequeue(out var next) ? next : null);

    // Displays text that the scenario adds to the user's prompt behind the scenes.
    public static void ShowContext(string context) => WriteGray($"[context] {context}");

    static string? ReadLine(Func<string?> nextAutoPrompt)
    {
        var input = Console.ReadLine();
        if (!string.Equals(input?.Trim(), AutoCommand, StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        var autoPrompt = nextAutoPrompt();
        WriteGray(autoPrompt is null ? "[auto] no more demo prompts" : $"[auto] {autoPrompt}");
        return autoPrompt;
    }

    static void WriteGray(string text)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}

// The prompts used by "/auto" in each scenario.
public static class DemoPrompts
{
    public const string SimpleChat =
        "Explain to me what a tool is in the context of the Claude API, in 2 sentences maximum.";

    public const string TokenCount =
        "In a .NET 10 Web API, how should I call an external REST service so that connections "
        + "are reused and transient failures are retried?";

    public const string AssistantPrefill =
        "I need a SQL Server connection string for a database named Orders, "
        + "and a Serilog setup that writes to the console.";

    public const string StructuredOutputs =
        "Hi, I'm Marc Dupont (marc.dupont@example.com). Since yesterday I can no longer "
        + "log into my account, the password keeps getting rejected. "
        + "It's urgent, I have a payroll deadline tomorrow morning.";

    public const string ToolRunnerWeather = "What is the weather like in Brussels right now?";

    public const string McpConnector =
        "Use DeepWiki to explain, in 3 sentences, what the official C# SDK for the "
        + "Model Context Protocol is for and how to build a simple MCP server with it.";

    public const string CustomSkill =
        "I bought a monitor for my home office for 220 USD. Can you check if it can be reimbursed?";

    public const string SatellitUserContext =
        "My name is Benjamin and my manager is Patrick. The company is Satellit.";

    public const string AgentTeamFanOut =
        "An employee is requesting a one-time exception to reimburse a $220 "
        + "hotel night (our lodging cap is 150 EUR/night) for a client trip "
        + "that was booked late due to a schedule change on the client's side.";

    // Shared by every run of the scenario in the same program session: the first run gets the expense,
    // and a second run (a new session) gets the question about what the agent remembers.
    public static readonly Queue<string> ManagedAgentCompliance = new(
    [
        "Can you check this expense for me? A client dinner, 45 GBP.",
        "What do you remember about my previous expense report?",
    ]);

    public const string ManagedAgentComplianceWelcome =
        "Hello, I am the assistant that checks your expenses against Satellit's rules. "
        + "Write your expense and I will help you with it.";

    // Tells the agent what to save in the memory store, which outlives the session.
    public const string ManagedAgentComplianceMemoryInstructions =
        "Expense history of employees. After every expense check, save a short note in "
        + "/expense_history.md (date, employee, each item with its verdict, and any decision "
        + "by a manager such as an approval). Read this file at the start of every conversation.";

    public const string ManagedAgentWorkflowWelcome =
        "Hello, I am the Satellit expense assistant. Send me your expense and I will categorize it, "
        + "convert its currency, check it against the policy and prepare the notification.";

    // The second prompt asks for an exception, which makes the coordinator consult its advisor subagent.
    public static readonly Queue<string> ManagedAgentWorkflow = new(
    [
        "I need to expense a hotel night in New York: 210 USD.",
        "The trip was booked late because my client changed the schedule. Can you ask for an exception for that hotel night?",
    ]);

    public const string Vision =
        "Describe this image in one sentence, then identify what it represents if possible.";

    public const string PdfSupport = "In 3 bullet points, what are the main topics of this document?";

    public static string BatchRequest(string destination) =>
        $"Give 2 reasons to visit {destination}, one sentence each.";

    public static string BatchContext(IReadOnlyList<string> destinations) =>
        $"The batch holds {destinations.Count} independent requests, one per destination "
        + $"({string.Join(", ", destinations)}), each asking: \"{BatchRequest("<destination>")}\"";

    // A fresh queue per run: the second prompt only works if the history is sent back.
    public static Queue<string> MultiTurnChat() =>
        new(
        [
            "Hi! My name is Benjamin and I am a .NET developer. Reply in one sentence.",
            "What is my name and what do I do for a living? Reply in one sentence.",
            "Which Claude API feature would help me most in my job? Reply in one sentence.",
        ]);
}
