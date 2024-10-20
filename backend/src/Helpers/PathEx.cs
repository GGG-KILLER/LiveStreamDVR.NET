namespace LiveStreamDVR.Api.Helpers;

public static class PathEx
{
    private static ReadOnlySpan<char> InvalidCharacters => [':', '*', '?', '"', '<', '>', '|', '\0', '[', ']', '\\', '/'];

    public static string SanitizeFileName(string fileName)
    {
        Span<char> sanitized = stackalloc char[fileName.Length];
        fileName.AsSpan().CopyTo(sanitized);

        foreach (var ch in InvalidCharacters)
        {
            sanitized.Replace(ch, '_');
        }

        return sanitized.ToString();
    }

    public static string? GetBinaryPath(string name)
    {
        if (Path.IsPathRooted(name))
        {
            return File.Exists(name) ? name : null;
        }

        var basePaths = Environment.GetEnvironmentVariable("PATH")
                ?.Split(Path.PathSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            ?? [];

        foreach (var basePath in basePaths)
        {
            string path = Path.Combine(basePath, name);
            if (File.Exists(path))
            {
                return path;
            }
        }
        return null;
    }
}
