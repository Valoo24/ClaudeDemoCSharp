using Anthropic;
using Anthropic.Models.Messages;
using ClaudeDemo.Demo;

namespace ClaudeDemo.Scenarios;

// Sends a local PDF from the asset folder, encoded in base64, and asks Claude about it.
// Claude reads both the text and the layout (tables, charts) with no extraction step.
public static class PdfSupport
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== PDF support ===\n");

        var pdfPath = AssetFiles.FindFirst(".pdf");
        if (pdfPath is null)
        {
            Console.WriteLine($"No PDF found in: {AssetFiles.Folder}");
            return;
        }

        Console.WriteLine($"PDF used: {Path.GetFileName(pdfPath)}\n");
        Console.Write("[You]: ");
        var prompt = DemoInput.ReadLine(DemoPrompts.PdfSupport);
        Console.WriteLine();

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
        var base64Data = Convert.ToBase64String(pdfBytes);

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeSonnet5,
            MaxTokens = 500,
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    // The document block carries the PDF; a URL or an uploaded file ID can be used as a source instead.
                    Content = new List<ContentBlockParam>
                    {
                        new DocumentBlockParam(new Base64PdfSource { Data = base64Data }),
                        new TextBlockParam { Text = prompt! },
                    },
                },
            ],
        };

        var response = await client.Messages.Create(parameters);

        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var text))
            {
                Console.WriteLine(text.Text);
            }
        }
    }
}
