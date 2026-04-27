# Step 06 — Tests

## Goal

Ensure `WorkspaceYamlParser`, `EventsReader`, and `SessionStateStore` are
covered by unit and integration tests using real fixture data.

## Fixture directory

Create a minimal fixture session tree at:

```
tests/fixtures/session-state/
  a1b2c3d4-0000-0000-0000-000000000001/
    workspace.yaml
    events.jsonl
    checkpoints/
      index.md
    files/
  a1b2c3d4-0000-0000-0000-000000000002/
    workspace.yaml
    events.jsonl
    checkpoints/
      index.md
```

### `workspace.yaml` (session 1)

```yaml
id: a1b2c3d4-0000-0000-0000-000000000001
cwd: /home/user/repos/my-project
git_root: /home/user/repos/my-project
repository: owner/my-project
host_type: github
branch: main
summary_count: 1
created_at: 2026-01-15T10:00:00.000Z
updated_at: 2026-01-15T10:30:00.000Z
summary: "Implemented the login page using React and Tailwind CSS."
```

### `events.jsonl` (session 1)

One event per line:

```jsonl
{"type":"session.start","data":{"sessionId":"a1b2c3d4-0000-0000-0000-000000000001","startTime":"2026-01-15T10:00:00.000Z","selectedModel":"claude-sonnet-4","context":{"cwd":"/home/user/repos/my-project","repository":"owner/my-project","branch":"main"}},"id":"evt-001","timestamp":"2026-01-15T10:00:00.000Z","parentId":null}
{"type":"user.message","data":{"content":"Please implement the login page.","interactionId":"int-001"},"id":"evt-002","timestamp":"2026-01-15T10:01:00.000Z","parentId":"evt-001"}
{"type":"tool.execution_start","data":{"toolCallId":"tc-001","toolName":"create_file","arguments":{"path":"src/pages/Login.tsx","content":"..."}},"id":"evt-003","timestamp":"2026-01-15T10:05:00.000Z","parentId":"evt-002"}
{"type":"tool.execution_complete","data":{"toolCallId":"tc-001","result":"ok"},"id":"evt-004","timestamp":"2026-01-15T10:05:01.000Z","parentId":"evt-003"}
{"type":"session.shutdown","data":{},"id":"evt-005","timestamp":"2026-01-15T10:30:00.000Z","parentId":null}
```

### `workspace.yaml` (session 2 — different repo, older)

```yaml
id: a1b2c3d4-0000-0000-0000-000000000002
repository: owner/other-project
branch: feature/api
created_at: 2026-01-10T08:00:00.000Z
updated_at: 2026-01-10T08:45:00.000Z
summary: "Added REST endpoints for user authentication."
```

## Test file: `net/tests/AutoMemory.Tests/VsCodeSessions/WorkspaceYamlParserTests.cs`

```csharp
[Fact]
public void Parse_RealFields_Correct()
{
    var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000001");
    var s = WorkspaceYamlParser.Parse(dir);
    Assert.Equal("a1b2c3d4-0000-0000-0000-000000000001", s.Id);
    Assert.Equal("owner/my-project", s.Repository);
    Assert.Equal("main", s.Branch);
    Assert.Equal(2026, s.CreatedAt.Year);
    Assert.Contains("login", s.Summary, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void Parse_MissingOptionalFields_DoesNotThrow()
{
    // workspace.yaml with only id and created_at
    var dir = WriteMinimalYaml();
    var s = WorkspaceYamlParser.Parse(dir);
    Assert.NotNull(s.Id);
    Assert.Null(s.Repository);
}
```

## Test file: `net/tests/AutoMemory.Tests/VsCodeSessions/EventsReaderTests.cs`

```csharp
[Fact]
public void UserMessages_ReturnsFirst()
{
    var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000001");
    var msgs = EventsReader.UserMessages(dir).ToList();
    Assert.Single(msgs);
    Assert.Contains("login", msgs[0], StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void ToolCalls_ReturnsCreateFile()
{
    var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000001");
    var calls = EventsReader.ToolCalls(dir).ToList();
    Assert.Contains(calls, c => c.Name == "create_file");
}

[Fact]
public void TouchedFiles_ExtractsPath()
{
    var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000001");
    var files = EventsReader.TouchedFiles(dir).ToList();
    Assert.Contains(files, f => f.Contains("Login.tsx"));
}

[Fact]
public void Read_MalformedLine_Skipped()
{
    // Write a jsonl with one bad line in the middle
    var dir = WriteBadJsonl();
    var events = EventsReader.Read(dir).ToList();
    Assert.True(events.Count >= 1); // good lines still returned
}
```

## Test file: `net/tests/AutoMemory.Tests/VsCodeSessions/SessionStateStoreTests.cs`

```csharp
[Fact]
public void List_ReturnsBothSessions()
{
    var store = new SessionStateStore(FixtureRoot());
    var sessions = store.List(limit: 10, days: 3650);
    Assert.Equal(2, sessions.Count);
}

[Fact]
public void List_FilterByRepo()
{
    var store = new SessionStateStore(FixtureRoot());
    var sessions = store.List(repo: "owner/my-project", days: 3650);
    Assert.Single(sessions);
    Assert.Equal("owner/my-project", sessions[0].Repository);
}

[Fact]
public void List_NewestFirst()
{
    var store = new SessionStateStore(FixtureRoot());
    var sessions = store.List(days: 3650);
    Assert.True(sessions[0].CreatedAt >= sessions[1].CreatedAt);
}

[Fact]
public void Show_ByPrefix_Found()
{
    var store = new SessionStateStore(FixtureRoot());
    var s = store.Show("a1b2c3d4");
    Assert.NotNull(s);
}

[Fact]
public void Search_MatchesSummary()
{
    var store = new SessionStateStore(FixtureRoot());
    var results = store.Search("login");
    Assert.Contains(results, r => r.SessionId.StartsWith("a1b2c3d4-0000-0000-0000-000000000001"));
}
```

## Done when

- [ ] All tests pass with `dotnet test`
- [ ] Fixture files committed under `tests/fixtures/session-state/`
- [ ] No test touches `~/.copilot/` directly (hermetic fixture dirs only)
- [ ] CI green (tests run in the existing GitHub Actions workflow)
