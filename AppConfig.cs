using Microsoft.Extensions.Configuration;

namespace ClaudeDemo;

public static class AppConfig
{
    // appsettings.local.json (git-ignored) overrides appsettings.json, so a real
    // API key never has to sit in the file that gets shared with colleagues.
    static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.local.json", optional: true)
        .Build();

    public static string? CustomSkillId => Configuration["CustomSkillId:expense-compliance-check"];

    public static string? ManagedAgentId => Configuration["ManagedAgent:expense-compliance-agent"];

    public static string? ManagedAgentWorkflowId => Configuration["ManagedAgent:expense-workflow-agent"];

    public static string? ManagedAgentEnvironmentId => Configuration["ManagedAgent:EnvironmentId"];

    public static string? ManagedAgentMemoryStoreId => Configuration["ManagedAgent:MemoryStoreId"];

    // The SDK reads ANTHROPIC_API_KEY when a client is created, so exposing the
    // configured key there covers every scenario. The file wins over a stale
    // environment variable, unless it is still a "<...>" placeholder from the template.
    public static void ApplyApiKey()
    {
        var apiKey = Configuration["Anthropic:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith('<'))
        {
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", apiKey);
        }
    }
}
