# Step 06 — Tests

## Goal

Complete unit and integration tests for the M7 Chat reader.

## Fixture structure

Create `net/tests/fixtures/chat-transcripts/` with two simulated workspace hashes:

```
fixtures/chat-transcripts/
  ws-hash-0001/
    workspace.json
    GitHub.copilot-chat/
      transcripts/
        abc12345-0001-0000-0000-000000000000.jsonl   (session A: 3 user msgs, 2 tool calls)
        abc12345-0002-0000-0000-000000000000.jsonl   (session B: 1 user msg, 0 tool calls)
  ws-hash-0002/
    workspace.json
    GitHub.copilot-chat/
      transcripts/
        abc12345-0003-0000-0000-000000000000.jsonl   (session C: 5 user msgs, 10 tool calls)
```

`ws-hash-0001/workspace.json`:
```json
{"workspace": "file:///e%3A/repos/Owner/RepoA"}
```

`ws-hash-0002/workspace.json`:
```json
{"workspace": "file:///e%3A/repos/Owner/RepoB"}
```

## Test files

### `WorkspaceStorageLocatorTests.cs` (3 tests)
- `StorageRoots_DoesNotThrow`
- `FindWorkspaces_DoesNotThrow_OnCurrentMachine`
- `ReadWorkspacePath_UrlDecodes_WindowsPath` — call internal method via testable wrapper

### `TranscriptReaderTests.cs` (8 tests)
- `Read_AllEvents_ReturnedInOrder`
- `Read_MalformedLine_IsSkipped`
- `ReadMeta_SessionId_FromStartEvent`
- `ReadMeta_CreatedAt_FromStartTime`
- `ReadMeta_UpdatedAt_IsLastEventTimestamp`
- `ReadMeta_Summary_IsFirstUserMessage`
- `ReadMeta_TurnCount_Correct`
- `ReadMeta_ToolCount_Correct`

### `ChatSessionStoreTests.cs` (10 tests)
- `List_ReturnsAllSessions_WhenNoFilter`
- `List_LimitIsRespected`
- `List_RepoFilter_OnlyMatchingSessions`
- `List_DaysFilter_ExcludesOldSessions`
- `List_OrderedByCreatedAtDesc`
- `Show_ByFullId_ReturnsSession`
- `Show_ByShortPrefix_ReturnsSession`
- `Show_UnknownId_ReturnsNull`
- `Search_ByKeywordInSummary_ReturnsMatch`
- `Count_ReturnsCorrectTotal`

### `VscChatCommandTests.cs` (5 tests)
- `VscChatListCommand_OutputsJsonWithSourceField`
- `VscChatListCommand_LimitIsRespected`
- `VscChatShowCommand_KnownId_OutputsSession`
- `VscChatShowCommand_UnknownId_Returns1`
- `VscChatSearchCommand_KeywordMatch_ReturnsResults`

## Total: 26 new tests

## `.csproj` additions

```xml
<ItemGroup>
  <Content Include="..\..\fixtures\chat-transcripts\**" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## Build and test command

```powershell
cd net
dotnet test tests/AutoMemory.Tests --filter "Category=VscChat" -v normal
```

## Pass criteria

All 26 new tests pass. Existing 21 M6 tests still pass.
