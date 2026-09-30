using Anthropic;
using Anthropic.Models.Messages;
using Anthropic.Models.Messages.Batches;
using ClaudeDemo.Demo;
using ClaudeDemo.Services;

namespace ClaudeDemo.Scenarios;

// Sends several independent requests in a single batch at half price, then retrieves the results in a separate run.
// Meant for large volumes of non-urgent requests: a batch can take up to 24 hours to process, and its results stay available for 29 days.
public static class BatchProcessing
{
    public static async Task CreateAsync()
    {
        Console.WriteLine("=== Batch processing - create a batch ===\n");

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        var topics = new[] { "Portugal", "Corsica", "Iceland" };
        DemoInput.ShowContext(DemoPrompts.BatchContext(topics));
        Console.WriteLine();

        var requests = topics
            .Select(
                (topic, i) => new Request
                {
                    // Identifies each request, since results are not guaranteed to come back in order.
                    CustomID = $"destination-{i}",
                    // The same parameters as a regular Messages call.
                    Params = new Params
                    {
                        Model = Model.ClaudeHaiku4_5,
                        MaxTokens = 200,
                        Messages =
                        [
                            new()
                            {
                                Role = Role.User,
                                Content = DemoPrompts.BatchRequest(topic),
                            },
                        ],
                    },
                }
            )
            .ToList();

        // Submits all the requests at once; they are processed asynchronously, so the program does not have to wait.
        var batch = await client.Messages.Batches.Create(new BatchCreateParams { Requests = requests });

        LastBatchStore.Save(batch.ID);
        Console.WriteLine($"Batch created: {batch.ID} ({batch.RequestCounts.Processing} request(s) in progress)");
        Console.WriteLine("Its ID is saved: the results can be retrieved later, from another run.");
    }

    public static async Task RetrieveAsync()
    {
        Console.WriteLine("=== Batch processing - retrieve the results ===\n");

        var lastBatchId = LastBatchStore.Load();
        Console.Write(lastBatchId is null ? "Batch ID: " : $"Batch ID (empty for the last one, {lastBatchId}): ");

        var batchId = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(batchId)) batchId = lastBatchId;
        if (string.IsNullOrEmpty(batchId)) return;
        Console.WriteLine();

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        // Only the batch ID is needed: any run of any program using the same workspace can retrieve the batch.
        var batch = await client.Messages.Batches.Retrieve(batchId);
        Console.WriteLine($"Batch {batch.ID}: {batch.ProcessingStatus} ({batch.RequestCounts.Processing} request(s) in progress)");

        if (batch.ProcessingStatus != ProcessingStatus.Ended)
        {
            Console.WriteLine("Not finished yet, try again later.");
            return;
        }

        Console.WriteLine("\nResults:");
        // Streams the results once the batch has ended; each one is either succeeded, errored, canceled or expired.
        await foreach (var result in client.Messages.Batches.ResultsStreaming(batch.ID))
        {
            Console.Write($"  [{result.CustomID}] ");
            if (result.Result.TryPickSucceeded(out var success))
            {
                var text = string.Join(
                    " ",
                    success.Message.Content.Select(c => c.Value).OfType<TextBlock>().Select(t => t.Text)
                );
                Console.WriteLine(text);
            }
            else
            {
                Console.WriteLine("(failed or canceled)");
            }
        }
    }
}
