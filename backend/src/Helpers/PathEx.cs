using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LiveStreamDVR.Api.Helpers;

public static partial class PathEx
{
    [GeneratedRegex(@"[\0-\31<>:""\/\\|?*]+|[\s.]+$")]
    private static partial Regex ForbiddenCharsRegex();

    // This doesn't include .. nor . because it'll be handled by the forbidden chars regex.
    private static readonly FrozenSet<string> s_invalidFileNames = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "CON", "PRN", "AUX", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5",
        "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³", "LPT0", "LPT1", "LPT2",
        "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³");

    /// <summary>
    /// Sanitizes file names according to https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file#naming-conventions
    /// </summary>
    /// <remarks>
    /// This function explicitly forces Windows rules upon Windows as well because Windows' rules make more sense to me and should
    /// cause the least trouble with most tools, CIFS/SMB and other things.
    /// </remarks>
    /// <param name="fileName">File name to sanitize.</param>
    /// <returns>The sanitized file name.</returns>
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

    /// <summary>
    /// <para>Tries to find the full path to a binary.</para>
    /// <para>If it's a path, we return it if it exists, otherwise we just search for it in all PATH components.</para>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string? GetBinaryPath(string name)
    {
        if (name.Contains('/'))
        {
            return File.Exists(name) ? name : null;
        }

        var basePaths = Environment.GetEnvironmentVariable("PATH")
                ?.Split(Path.PathSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            ?? [];

        foreach (var basePath in basePaths)
        {
            string path = Path.Combine(basePath, name);
            if (File.Exists(path)
                && (OperatingSystem.IsWindows()
                    // Need to check if file is executable on unix-like platforms.
                    || (File.GetUnixFileMode(path) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0))
            {
                return path;
            }
        }
        return null;
    }
}
