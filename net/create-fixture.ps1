param([string]$dbPath)

Add-Type -Path "net\src\AutoMemory.Cli\bin\Release\net10.0\Microsoft.Data.Sqlite.dll"

$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbPath")
$conn.Open()

$cmd = $conn.CreateCommand()

# Create tables
$cmd.CommandText = @"
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    repository TEXT,
    branch TEXT,
    summary TEXT,
    created_at TEXT,
    updated_at TEXT
);
CREATE TABLE turns (
    session_id TEXT,
    turn_index INTEGER,
    user_message TEXT,
    assistant_response TEXT,
    timestamp TEXT
);
CREATE TABLE session_files (
    session_id TEXT,
    file_path TEXT,
    tool_name TEXT,
    turn_index INTEGER,
    first_seen_at TEXT
);
CREATE TABLE session_refs (
    session_id TEXT,
    ref_type TEXT,
    ref_value TEXT,
    turn_index INTEGER,
    created_at TEXT
);
CREATE TABLE checkpoints (
    session_id TEXT,
    checkpoint_number INTEGER,
    title TEXT,
    overview TEXT,
    created_at TEXT
);
"@
$cmd.ExecuteNonQuery() | Out-Null

# Insert sessions with recent dates
$now = [DateTime]::UtcNow
for ($i = 1; $i -le 5; $i++) {
    $daysAgo = 5 - $i
    $date = $now.AddDays(-$daysAgo).ToString("yyyy-MM-ddTHH:mm:ss")
    $cmd.CommandText = "INSERT INTO sessions (id, repository, branch, summary, created_at, updated_at) VALUES (@id, @repo, @branch, @summary, @created, @updated)"
    $cmd.Parameters.Clear()
    $cmd.Parameters.AddWithValue("@id", "s$i")
    $cmd.Parameters.AddWithValue("@repo", "/home/user/proj")
    $cmd.Parameters.AddWithValue("@branch", "main")
    $cmd.Parameters.AddWithValue("@summary", "Test session $i")
    $cmd.Parameters.AddWithValue("@created", $date)
    $cmd.Parameters.AddWithValue("@updated", $date)
    $cmd.ExecuteNonQuery() | Out-Null
}

$conn.Close()
Write-Host "Fixture DB created at $dbPath"
