using Anthropic;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using Anthropic.Models.Messages;
using ClaudeDemo.Demo;
using Beta = Anthropic.Models.Beta.Messages;

namespace ClaudeDemo.Scenarios;

// Connects Claude to a remote MCP server (DeepWiki, public and unauthenticated) with the server-side MCP connector:
// Anthropic's infrastructure calls the server's tools, so there is no loop to write on the client side.
public static class McpConnector
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== MCP connector (server-side) ===\n");
        Console.WriteLine("Welcome to the chat using the DeepWiki MCP server.\n");
        Console.Write("[You]: ");
        var prompt = DemoInput.ReadLine(DemoPrompts.McpConnector);
        Console.WriteLine();

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        var parameters = new Beta::MessageCreateParams
        {
            Model = Model.ClaudeSonnet5,
            MaxTokens = 1024,
            // The MCP connector is in beta: this header is required to use McpServers and BetaMcpToolset.
            Betas = [AnthropicBeta.McpClient2025_11_20],
            Messages =
            [
                new()
                {
                    Role = Beta::Role.User,
                    Content = prompt!,
                },
            ],
            // Declares the remote MCP server by name and URL...
            McpServers =
            [
                new BetaRequestMcpServerUrlDefinition
                {
                    Name = "deepwiki",
                    Url = "https://mcp.deepwiki.com/mcp",
                },
            ],
            // ...and allows Claude to use its tools.
            Tools = [new BetaMcpToolset { McpServerName = "deepwiki" }],
        };

        var response = await client.Beta.Messages.Create(parameters);

        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var text))
            {
                Console.WriteLine(text.Text);
            }
        }
    }
}
