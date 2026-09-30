using Anthropic.Models.Messages;

namespace ClaudeDemo.MCPTools;

internal static class SubAgentText
{
    // Sub-agents use the stable (non-beta) Messages API, so their responses
    // are plain Anthropic.Models.Messages.ContentBlock, not the Beta union.
    public static string Of(IReadOnlyList<ContentBlock> content)
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
