# Step 05 — Dashboard integration

## Goal

Show VS Code Copilot Chat sessions in the dashboard:
- `renderHealth` shows combined session count (session-state + chat)
- `renderSessions` shows a `● VS Code Chat sessions` badge for chat-sourced sessions
- `renderInstall` shows `• {n} VS Code Chat sessions` line
- `StorageDetect` reports chat session count in `NoDatabaseJson`

## Changes to `StorageDetect.cs`

Add a `VscChatSessionCount()` method:

```csharp
public static int VscChatSessionCount()
{
    // Enumerate workspaceStorage from all VS Code installs
    int count = 0;
    foreach (var root in PlatformStorageRoots())
    {
        if (!Directory.Exists(root)) continue;
        foreach (var hashDir in Directory.EnumerateDirectories(root))
        {
            var transcriptDir = Path.Combine(hashDir, "GitHub.copilot-chat", "transcripts");
            if (Directory.Exists(transcriptDir))
                count += Directory.GetFiles(transcriptDir, "*.jsonl").Length;
        }
    }
    return count;
}

private static IEnumerable<string> PlatformStorageRoots()
{
    // Mirrors WorkspaceStorageLocator.PlatformCandidates() but without Core dependency
    if (OperatingSystem.IsWindows())
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Path.Combine(appData, "Code", "User", "workspaceStorage");
        yield return Path.Combine(appData, "Code - Insiders", "User", "workspaceStorage");
    }
    // macOS / Linux omitted for brevity — add in full implementation
}
```

Update `NoDatabaseJson` to include `vscode_chat_count`:

```json
{
  "warning": "no database found",
  "db_path": "...",
  "storage_format": "vscode-session-state",
  "session_state_count": 44,
  "vscode_chat_count": 86
}
```

## Changes to `panel.js`

Update `renderNoDb` for `storage_format === 'vscode-session-state'`:

```js
function renderNoDb(data) {
  const stateCount = data.session_state_count ?? 0;
  const chatCount  = data.vscode_chat_count   ?? 0;
  const total      = stateCount + chatCount;

  if (data.storage_format === 'vscode-session-state' || chatCount > 0) {
    return `
      <div class="card warning">
        <h3>VS Code Copilot sessions detected (${total} sessions)</h3>
        <p>
          <strong>${stateCount}</strong> Copilot CLI sessions in <code>~/.copilot/session-state/</code><br>
          <strong>${chatCount}</strong> Copilot Chat sessions in <code>workspaceStorage/</code>
        </p>
        <p>Both are readable. Use <strong>Reload</strong> to refresh.</p>
        <button onclick="reload()">Reload</button>
      </div>`;
  }
  // ... existing fallback card ...
}
```

Update `renderSessions` to show appropriate badge:

```js
function renderSessions(data) {
  const badge = data.source === 'vscode-chat'
    ? '● VS Code Chat sessions'
    : data.source === 'vscode-session-state'
    ? '● VS Code Copilot sessions'
    : null;
  // ... render badge if present ...
}
```

Update `renderInstall` to show both counts:

```js
function renderInstall(data) {
  const lines = [];
  if (data.vscode_session_count > 0)
    lines.push(`• ${data.vscode_session_count} Copilot CLI sessions`);
  if (data.vscode_chat_count > 0)
    lines.push(`• ${data.vscode_chat_count} VS Code Chat sessions`);
  // ... append lines to install card ...
}
```

## Changes to `messages.ts`

`postInstall` passes `vscode_chat_count` from `lastHealthData` alongside `vscode_session_count`.

## Tests

Manual smoke test:
```
session-recall.exe list --json --limit 5
# expect: { "sessions": [...], "source": "vscode-chat", "total": 86 }

session-recall.exe search --json "Oravey"
# expect: sessions from the Oravey2 workspace
```
