using Anthropic;
using Anthropic.Models.Messages;
using ClaudeDemo.Const;
using Microsoft.Extensions.Logging;

namespace ClaudeDemo.Scenarios;

// Sends one prompt and logs what is needed to audit the call: input, output, token counts (including cache),
// stop reason and request id. Logging goes through ILogger with structured placeholders.
public static class LoggedChat
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Simple prompt + logging ===\n");
        Console.Write("[You]: ");
        var prompt = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(prompt))
        {
            Console.WriteLine("Nothing to send.");
            return;
        }

        using var loggerFactory = LoggerFactory.Create(logging =>
            logging
                .SetMinimumLevel(LogLevel.Debug)
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.IncludeScopes = true;
                    options.TimestampFormat = "HH:mm:ss.fff ";
                })
        );
        var logger = loggerFactory.CreateLogger(nameof(LoggedChat));

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        using (logger.BeginScope("call {CallId}", Guid.NewGuid().ToString("N")[..8]))
        {
            await AskAsync(client, logger, prompt);
        }
    }

    static async Task AskAsync(AnthropicClient client, ILogger logger, string prompt)
    {
        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeSonnet5,
            MaxTokens = 1024,
            // The system prompt sets the persona and rules for the whole request, separately from the user messages.
            System = SystemPrompt.DotNetSeniorDev,
            Messages = [new() { Role = Role.User, Content = prompt }],
            CacheControl = new CacheControlEphemeral { Ttl = Ttl.Ttl5m },
        };

        logger.LogDebug("Input: \"{Input}\"", prompt);

        // WithRawResponse gives access to the HTTP response, hence the request-id header (Anthropic support asks for it).
        using var response = await client.Messages.WithRawResponse.Create(parameters);
        // Deserialize turns the raw HTTP response into the usual Message.
        var message = await response.Deserialize();

        OutputText(message, out var output);

        // InputTokens only counts what is AFTER the last cache breakpoint, so the real
        // prompt size is the sum of the three counters.
        var cacheRead = message.Usage.CacheReadInputTokens ?? 0;
        var cacheWrite = message.Usage.CacheCreationInputTokens ?? 0;
        var promptTokens = message.Usage.InputTokens + cacheRead + cacheWrite;
        // StopReason tells whether the answer ended normally (EndTurn) or was cut off, for example by MaxTokens.
        var stopReason = message.StopReason?.Value();

        logger.LogDebug("Output: \"{Output}\"", output);
        logger.Log(
            stopReason == StopReason.EndTurn ? LogLevel.Information : LogLevel.Warning,
            "Stop reason: {StopReason}",
            stopReason
        );
        logger.LogInformation("Prompt tokens: {PromptTokens}", promptTokens);
        logger.LogInformation("Output tokens: {OutputTokens}", message.Usage.OutputTokens);
        logger.LogInformation("Total tokens: {TotalTokens}", promptTokens + message.Usage.OutputTokens);
        logger.LogInformation("Cache read tokens: {CacheReadTokens}", cacheRead);
        logger.LogInformation("Cache write tokens: {CacheWriteTokens}", cacheWrite);
        logger.LogInformation("Request id: {RequestId}", response.RequestID);
        logger.LogInformation("Message id: {MessageId}", message.ID);
    }

    static void OutputText(Message message, out string output)
    {
        // Alternative to TryPickText: filters the content blocks by type.
        var textBlocks = message.Content.Select(block => block.Value).OfType<TextBlock>();
        output = string.Concat(textBlocks.Select(block => block.Text));

        Console.WriteLine($"\n[Claude]\n{output}\n");
    }
}
