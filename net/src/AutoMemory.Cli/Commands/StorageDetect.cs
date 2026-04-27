using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMemory.Core;

namespace AutoMemory.Cli.Commands;

/// <summary>
/// Detects the Copilot session storage format present on this machine.
/// </summary>
internal static class StorageDetect
{
    /// <summary>
    /// Path to the VS Code Copilot flat-file session store.
    /// </summary>
    private static string SessionStatePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".copilot", "session-state");

    /// <summary>
    /// Returns how many VS Code Copilot session folders exist under ~/.copilot/session-state/,
    /// or 0 if the folder doesn't exist.
    /// </summary>
    internal static int VsCodeSessionCount()
    {
        try
        {
            var dir = SessionStatePath;
            if (!Directory.Exists(dir)) return 0;
            // Each session is a UUID-named subdirectory containing workspace.yaml
            return Directory.GetDirectories(dir).Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Returns how many VS Code Copilot Chat transcript files exist across all workspaceStorage roots,
    /// or 0 if none are found.
    /// </summary>
    internal static int VscChatSessionCount()
    {
        int count = 0;
        foreach (var root in PlatformStorageRoots())
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var hashDir in Directory.EnumerateDirectories(root))
                {
                    var transcriptDir = Path.Combine(hashDir, "GitHub.copilot-chat", "transcripts");
                    if (Directory.Exists(transcriptDir))
                        count += Directory.GetFiles(transcriptDir, "*.jsonl").Length;
                }
            }
            catch
            {
                // Skip inaccessible storage roots
            }
        }
        return count;
    }

    /// <summary>
    /// Enumerates platform-specific VS Code workspaceStorage roots.
    /// Mirrors WorkspaceStorageLocator.PlatformCandidates() but without Core dependency.
    /// </summary>
    private static IEnumerable<string> PlatformStorageRoots()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Path.Combine(appData, "Code", "User", "workspaceStorage");
            yield return Path.Combine(appData, "Code - Insiders", "User", "workspaceStorage");
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, "Library", "Application Support", "Code", "User", "workspaceStorage");
            yield return Path.Combine(home, "Library", "Application Support", "Code - Insiders", "User", "workspaceStorage");
        }
        else if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".config", "Code", "User", "workspaceStorage");
            yield return Path.Combine(home, ".config", "Code - Insiders", "User", "workspaceStorage");
        }
    }

    /// <summary>
    /// Builds the JSON fragment to embed in a no-DB response, including VS Code session info.
    /// </summary>
    internal static string NoDatabaseJson(string dbPath)
    {
        var safeDbPath = dbPath.Replace("\\", "\\\\");
        var stateCount = VsCodeSessionCount();
        var chatCount = VscChatSessionCount();

        if (stateCount > 0 || chatCount > 0)
        {
            return $"{{" +
                   $"\"warning\":\"no database found\"," +
                   $"\"db_path\":\"{safeDbPath}\"," +
                   $"\"storage_format\":\"vscode-session-state\"," +
                   $"\"session_state_count\":{stateCount}," +
                   $"\"vscode_chat_count\":{chatCount}" +
                   $"}}";
        }

        return $"{{\"warning\":\"no database found\",\"db_path\":\"{safeDbPath}\"}}";
    }
}
