using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AutoMemory.Core.Util;

/// <summary>
/// Detects the current repository from git remote origin.
/// </summary>
public static partial class DetectRepo
{
    // SSH: git@github.com:owner/repo.git
    [GeneratedRegex(@"git@[^:]+:(.+?)(?:\.git)?$")]
    private static partial Regex SshPattern();

    // HTTPS: https://github.com/owner/repo.git
    [GeneratedRegex(@"https?://[^/]+/(.+?)(?:\.git)?$")]
    private static partial Regex HttpsPattern();

    /// <summary>
    /// Returns 'owner/repo' from git remote origin, or null.
    /// </summary>
    public static string? Detect()
    {
        string url;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "remote get-url origin",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            // 5 second timeout
            if (!process.WaitForExit(5000))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Ignore kill failures
                }
                return null;
            }

            url = process.StandardOutput.ReadToEnd().Trim();
        }
        catch
        {
            // FileNotFoundException (git not on PATH), or other exceptions
            return null;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // Try SSH pattern
        var match = SshPattern().Match(url);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // Try HTTPS pattern
        match = HttpsPattern().Match(url);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}
