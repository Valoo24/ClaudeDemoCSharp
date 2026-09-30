namespace ClaudeDemo.Services;

// How a streamed response is displayed in the console.
public enum StreamDisplayMode
{
    // Everything Claude writes as it arrives, including the text between its tool calls.
    ThoughtProcess,

    // Only the final answer, displayed once the stream ends.
    FinalAnswer,
}
