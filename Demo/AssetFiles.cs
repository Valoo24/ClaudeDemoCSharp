namespace ClaudeDemo.Demo;

// Finds files in the asset folder, which the build copies next to the executable.
public static class AssetFiles
{
    public static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "asset");

    public static string? FindFirst(params string[] extensions) =>
        Directory.Exists(Folder)
            ? Directory
                .EnumerateFiles(Folder)
                .FirstOrDefault(file =>
                    extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)
                )
            : null;
}
