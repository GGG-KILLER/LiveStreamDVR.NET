using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LiveStreamDVR.Api.Helpers;

public static partial class PathEx
{
    [GeneratedRegex(@"[\0-\31<>:""\/\\|?*]+|[\s.]+$")]
    private static partial Regex ForbiddenCharsRegex();

    private static readonly FrozenSet<string> s_invalidFileNames = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "CON", "PRN", "AUX", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5",
        "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³", "LPT0", "LPT1", "LPT2",
        "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³",
        "..", ".");

    public static string SanitizeFileName(string fileName)
    {
        // Remove all unallowed chars from the name.
        var name = ForbiddenCharsRegex().Replace(fileName.Trim(), "_");

        // These names aren't allowed on Windows and/or Linux, so we prefix them.
        if (s_invalidFileNames.Contains(name)
            || s_invalidFileNames.Contains(Path.GetFileNameWithoutExtension(name)))
        {
            name = $"__{name}";
        }

        return name;
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
