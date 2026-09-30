namespace ClaudeDemo.Services;

// Keeps the ID of the last created batch in a file, so a later run of the program can retrieve its results.
// A real application would store it in a database, or send it along in a message.
public static class LastBatchStore
{
    static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "last-batch-id.txt");

    public static void Save(string batchId) => File.WriteAllText(FilePath, batchId);

    public static string? Load() => File.Exists(FilePath) ? File.ReadAllText(FilePath).Trim() : null;
}
