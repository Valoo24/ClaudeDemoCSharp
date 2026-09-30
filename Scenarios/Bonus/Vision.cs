using Anthropic;
using Anthropic.Models.Messages;
using ClaudeDemo.Demo;

namespace ClaudeDemo.Scenarios;

// Sends a local image from the asset folder, encoded in base64, and asks Claude about it.
public static class Vision
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Vision (local image) ===\n");

        var imagePath = AssetFiles.FindFirst(".jpg", ".jpeg", ".png", ".gif", ".webp");
        if (imagePath is null)
        {
            Console.WriteLine($"No image found in: {AssetFiles.Folder}");
            return;
        }

        var mediaType = Path.GetExtension(imagePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => MediaType.ImageJpeg,
            ".png" => MediaType.ImagePng,
            ".gif" => MediaType.ImageGif,
            ".webp" => MediaType.ImageWebP,
            _ => MediaType.ImageJpeg,
        };

        Console.WriteLine($"Image used: {Path.GetFileName(imagePath)}\n");
        Console.Write("[You]: ");
        var prompt = DemoInput.ReadLine(DemoPrompts.Vision);
        Console.WriteLine();

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var base64Data = Convert.ToBase64String(imageBytes);

        var client = new AnthropicClient { Timeout = TimeSpan.FromMinutes(5), MaxRetries = 0 };

        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeSonnet5,
            MaxTokens = 400,
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    // A message can mix content blocks: the image block goes before the text, and the media type gives its format.
                    Content = new List<ContentBlockParam>
                    {
                        new ImageBlockParam(
                            new Base64ImageSource { Data = base64Data, MediaType = mediaType }
                        ),
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
