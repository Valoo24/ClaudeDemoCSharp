using System.Text;
using Anthropic.Models.Messages;

namespace ClaudeDemo.Services;

// Displays a streamed response in the console, in the way chosen by the display mode.
public static class StreamDisplayService
{
    public static Task ShowAsync(IAsyncEnumerable<RawMessageStreamEvent> stream, StreamDisplayMode mode) =>
        mode switch
        {
            StreamDisplayMode.ThoughtProcess => ShowThoughtProcessAsync(stream),
            StreamDisplayMode.FinalAnswer => ShowFinalAnswerAsync(stream),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

    static async Task ShowThoughtProcessAsync(IAsyncEnumerable<RawMessageStreamEvent> stream)
    {
        var printedText = false;
        var midLine = false;

        await foreach (var streamEvent in stream)
        {
            if (streamEvent.TryPickContentBlockStart(out var start))
            {
                // A server tool use block means Claude is running code in the container, and no text streams meanwhile.
                if (start.ContentBlock.TryPickServerToolUse(out _))
                {
                    if (midLine) Console.WriteLine();

                    Console.WriteLine("[code execution in progress...]");
                    midLine = false;
                }
                // A new text block after earlier text starts on its own paragraph.
                else if (start.ContentBlock.TryPickText(out _) && printedText)
                {
                    if (midLine) Console.WriteLine();

                    Console.WriteLine();
                    midLine = false;
                }
            }

            if (streamEvent.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var text))
            {
                Console.Write(text.Text);
                printedText = midLine = true;
            }
        }
    }

    // Shows only the final answer: the text written after the last tool call, displayed once the stream ends.
    static async Task ShowFinalAnswerAsync(IAsyncEnumerable<RawMessageStreamEvent> stream)
    {
        var finalAnswer = new StringBuilder();

        await foreach (var streamEvent in stream)
        {
            if (streamEvent.TryPickContentBlockStart(out var start) && start.ContentBlock.TryPickServerToolUse(out _))
            {
                finalAnswer.Clear();
            }

            if (streamEvent.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var text))
            {
                finalAnswer.Append(text.Text);
            }
        }

        Console.WriteLine(finalAnswer.ToString().Trim());
    }
}
