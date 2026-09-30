using Anthropic;
using Anthropic.Models.Beta.Sessions;
using Anthropic.Models.Beta.Sessions.Events;
using Microsoft.Extensions.Logging;

namespace ClaudeDemo.Services;

// Talks to the Claude Console's Managed Agents API on behalf of the scenarios.
public class ClaudeConsoleService(AnthropicClient client, ILogger<ClaudeConsoleService> logger)
{
    /// <summary>
    /// Starts a session on an existing agent, with a memory store mounted in its container.
    /// </summary>
    /// <param name="agentId">The agent to run. A bare ID means its latest version.</param>
    /// <param name="environmentId">The environment (container) the session runs in.</param>
    /// <param name="memoryStoreId">The memory store to mount. It can only be attached at creation.</param>
    /// <param name="title">The session title shown in the Console.</param>
    /// <param name="memoryInstructions">Optional guidance telling the agent what to keep in the store.</param>
    /// <returns>The session ID.</returns>
    public async Task<string> CreateSessionAsync(
        string agentId,
        string environmentId,
        string memoryStoreId,
        string title,
        string? memoryInstructions = null
    )
    {
        var session = await client.Beta.Sessions.Create(new SessionCreateParams
        {
            Agent = agentId,
            EnvironmentID = environmentId,
            Title = title,
            Resources =
            [
                new BetaManagedAgentsMemoryStoreResourceParam
                {
                    Type = BetaManagedAgentsMemoryStoreResourceParamType.MemoryStore,
                    MemoryStoreID = memoryStoreId,
                    Access = Access.ReadWrite,
                    Instructions = memoryInstructions,
                },
            ],
        });

        logger.LogInformation(
            "Session {SessionId} created. Trace: https://platform.claude.com/workspaces/default/sessions/{SessionId}",
            session.ID,
            session.ID
        );
        return session.ID;
    }

    /// <summary>
    /// Sends a user message to a session and streams the session's events until the agent is done.
    /// </summary>
    /// <param name="sessionId">The session to talk to.</param>
    /// <param name="text">The user message.</param>
    /// <returns>The events of this turn, up to and including the one that ends it.</returns>
    public async IAsyncEnumerable<BetaManagedAgentsStreamSessionEvents> SendMessageAsync(string sessionId, string text)
    {

        await using var stream = client.Beta.Sessions.Events.StreamStreaming(sessionId).GetAsyncEnumerator();
        var next = stream.MoveNextAsync().AsTask();

        await client.Beta.Sessions.Events.Send(sessionId, new EventSendParams
        {
            Events =
            [
                new BetaManagedAgentsUserMessageEventParams
                {
                    Type = BetaManagedAgentsUserMessageEventParamsType.UserMessage,
                    Content =
                    [
                        new BetaManagedAgentsTextBlock
                        {
                            Type = BetaManagedAgentsTextBlockType.Text,
                            Text = text,
                        },
                    ],
                },
            ],
        });

        while (await next)
        {
            var ev = stream.Current;
            yield return ev;

            if (ev.TryPickSessionStatusTerminatedEvent(out _)) yield break;

            if (ev.TryPickSessionStatusIdleEvent(out var idle)
                && idle.StopReason.Value is not BetaManagedAgentsSessionRequiresAction) yield break;

            next = stream.MoveNextAsync().AsTask();
        }
    }
}
