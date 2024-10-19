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
}
