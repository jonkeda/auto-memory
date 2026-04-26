#!/usr/bin/env pwsh
<#
.SYNOPSIS
    End-to-end golden-file parity test: Python vs .NET telemetry output.

.DESCRIPTION
    1. Creates a fixture database with sample data.
    2. Runs a fixed sequence of commands with both Python and .NET CLIs.
    3. Compares telemetry output (ignoring ts and duration_ms).
    4. Verifies jq can process the merged telemetry file.

.EXAMPLE
    pwsh net/tests/verify_golden_parity.ps1

.NOTES
    Requires:
    - Python with session_recall installed in editable mode
    - .NET CLI built (session-recall.exe in Release)
    - jq installed and in PATH
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Resolve repo root
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Paths
$dotnetExe = Join-Path $repoRoot "net\src\AutoMemory.Cli\bin\Release\net10.0\session-recall.exe"
$pythonModule = "session_recall"

# Check prerequisites
if (-not (Test-Path $dotnetExe)) {
    Write-Error ".NET CLI not found at $dotnetExe. Run: dotnet build net/AutoMemory.sln -c Release"
    exit 1
}

if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    Write-Error "python not found in PATH"
    exit 1
}

# Check Python module is available
$pythonCheck = python -c "import sys; sys.path.insert(0, r'$repoRoot\src'); import session_recall; print('OK')" 2>$null
if ($pythonCheck -ne "OK") {
    Write-Error "Python session_recall module not available. Ensure src/ is in PYTHONPATH."
    exit 1
}

$hasJq = $null -ne (Get-Command jq -ErrorAction SilentlyContinue)
if (-not $hasJq) {
    Write-Warning "jq not found in PATH. Will use PowerShell fallback for command counting."
}

Write-Host "[INFO] Prerequisites OK" -ForegroundColor Green

# Create temp directory
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "golden_parity_test_$(New-Guid)"
New-Item -ItemType Directory -Path $tempDir | Out-Null
Write-Host "[INFO] Working directory: $tempDir" -ForegroundColor Cyan

try {
    # Create fixture database
    $fixtureDb = Join-Path $tempDir "fixture.db"
    Write-Host "[INFO] Creating fixture database..." -ForegroundColor Cyan

    # Use .NET API to create the database with schema
    $createDbScript = @"
using Microsoft.Data.Sqlite;
using System;

var connStr = `"Data Source=$($fixtureDb.Replace('\', '\\'))`";
using var conn = new SqliteConnection(connStr);
conn.Open();

// Create schema
conn.CreateCommand().With(c => {
    c.CommandText = @`"
        CREATE TABLE sessions (
            id TEXT PRIMARY KEY,
            cwd TEXT,
            repository TEXT,
            branch TEXT,
            summary TEXT,
            created_at TEXT,
            updated_at TEXT,
            host_type TEXT
        )
    `";
    c.ExecuteNonQuery();
});

conn.CreateCommand().With(c => {
    c.CommandText = @`"
        CREATE TABLE turns (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            turn_index INTEGER,
            user_message TEXT,
            assistant_response TEXT,
            timestamp TEXT
        )
    `";
    c.ExecuteNonQuery();
});

conn.CreateCommand().With(c => {
    c.CommandText = @`"
        CREATE TABLE files (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            file_path TEXT,
            access_time TEXT
        )
    `";
    c.ExecuteNonQuery();
});

conn.CreateCommand().With(c => {
    c.CommandText = @`"
        CREATE TABLE checkpoints (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            checkpoint_time TEXT,
            description TEXT
        )
    `";
    c.ExecuteNonQuery();
});

// Insert sample data
var sessionId1 = `"sess_001`";
var sessionId2 = `"sess_002`";

conn.CreateCommand().With(c => {
    c.CommandText = `"INSERT INTO sessions (id, cwd, repository, branch, summary, created_at, updated_at, host_type) VALUES (@id, @cwd, @repo, @branch, @summary, @created, @updated, @host)`";
    c.Parameters.AddWithValue(`"@id`", sessionId1);
    c.Parameters.AddWithValue(`"@cwd`", `"/home/user/project`");
    c.Parameters.AddWithValue(`"@repo`", `"project-repo`");
    c.Parameters.AddWithValue(`"@branch`", `"main`");
    c.Parameters.AddWithValue(`"@summary`", `"Test session 1`");
    c.Parameters.AddWithValue(`"@created`", `"2026-04-01T10:00:00Z`");
    c.Parameters.AddWithValue(`"@updated`", `"2026-04-01T11:00:00Z`");
    c.Parameters.AddWithValue(`"@host`", `"cli`");
    c.ExecuteNonQuery();
});

conn.CreateCommand().With(c => {
    c.CommandText = `"INSERT INTO sessions (id, cwd, repository, branch, summary, created_at, updated_at, host_type) VALUES (@id, @cwd, @repo, @branch, @summary, @created, @updated, @host)`";
    c.Parameters.AddWithValue(`"@id`", sessionId2);
    c.Parameters.AddWithValue(`"@cwd`", `"/home/user/other`");
    c.Parameters.AddWithValue(`"@repo`", `"other-repo`");
    c.Parameters.AddWithValue(`"@branch`", `"dev`");
    c.Parameters.AddWithValue(`"@summary`", `"Test session 2`");
    c.Parameters.AddWithValue(`"@created`", `"2026-04-02T10:00:00Z`");
    c.Parameters.AddWithValue(`"@updated`", `"2026-04-02T11:00:00Z`");
    c.Parameters.AddWithValue(`"@host`", `"cli`");
    c.ExecuteNonQuery();
});

// Add some turns
conn.CreateCommand().With(c => {
    c.CommandText = `"INSERT INTO turns (session_id, turn_index, user_message, assistant_response, timestamp) VALUES (@sid, @idx, @user, @asst, @ts)`";
    c.Parameters.AddWithValue(`"@sid`", sessionId1);
    c.Parameters.AddWithValue(`"@idx`", 0);
    c.Parameters.AddWithValue(`"@user`", `"What is the meaning of life?`");
    c.Parameters.AddWithValue(`"@asst`", `"42`");
    c.Parameters.AddWithValue(`"@ts`", `"2026-04-01T10:05:00Z`");
    c.ExecuteNonQuery();
});

Console.WriteLine(`"OK`");
"@

    # Use inline C# with dotnet-script or direct compilation
    # Simpler approach: Use SQLite CLI or Python to create the DB
    # Let me use Python since it's already available

    $createDbPy = @"
import sqlite3
import sys

db_path = r'$fixtureDb'
conn = sqlite3.connect(db_path)
cur = conn.cursor()

# Create schema
cur.execute('''
    CREATE TABLE sessions (
        id TEXT PRIMARY KEY,
        cwd TEXT,
        repository TEXT,
        branch TEXT,
        summary TEXT,
        created_at TEXT,
        updated_at TEXT,
        host_type TEXT
    )
''')

cur.execute('''
    CREATE TABLE turns (
        id INTEGER PRIMARY KEY,
        session_id TEXT,
        turn_index INTEGER,
        user_message TEXT,
        assistant_response TEXT,
        timestamp TEXT
    )
''')

cur.execute('''
    CREATE TABLE files (
        id INTEGER PRIMARY KEY,
        session_id TEXT,
        file_path TEXT,
        access_time TEXT
    )
''')

cur.execute('''
    CREATE TABLE checkpoints (
        id INTEGER PRIMARY KEY,
        session_id TEXT,
        checkpoint_time TEXT,
        description TEXT
    )
''')

# Insert sample data
session_id1 = 'sess_001'
session_id2 = 'sess_002'

cur.execute('''
    INSERT INTO sessions (id, cwd, repository, branch, summary, created_at, updated_at, host_type)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
''', (session_id1, '/home/user/project', 'project-repo', 'main', 'Test session 1',
      '2026-04-01T10:00:00Z', '2026-04-01T11:00:00Z', 'cli'))

cur.execute('''
    INSERT INTO sessions (id, cwd, repository, branch, summary, created_at, updated_at, host_type)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
''', (session_id2, '/home/user/other', 'other-repo', 'dev', 'Test session 2',
      '2026-04-02T10:00:00Z', '2026-04-02T11:00:00Z', 'cli'))

# Add some turns with searchable content
cur.execute('''
    INSERT INTO turns (session_id, turn_index, user_message, assistant_response, timestamp)
    VALUES (?, ?, ?, ?, ?)
''', (session_id1, 0, 'What is the meaning of life?', '42', '2026-04-01T10:05:00Z'))

cur.execute('''
    INSERT INTO turns (session_id, turn_index, user_message, assistant_response, timestamp)
    VALUES (?, ?, ?, ?, ?)
''', (session_id1, 1, 'Tell me about Python', 'Python is a programming language', '2026-04-01T10:10:00Z'))

# Add some files
cur.execute('''
    INSERT INTO files (session_id, file_path, access_time)
    VALUES (?, ?, ?)
''', (session_id1, '/home/user/project/main.py', '2026-04-01T10:15:00Z'))

# Add some checkpoints
cur.execute('''
    INSERT INTO checkpoints (session_id, checkpoint_time, description)
    VALUES (?, ?, ?)
''', (session_id1, '2026-04-01T10:20:00Z', 'First checkpoint'))

conn.commit()
conn.close()
print('OK')
"@

    $createDbPyFile = Join-Path $tempDir "create_db.py"
    Set-Content -Path $createDbPyFile -Value $createDbPy -Encoding UTF8
    $result = python $createDbPyFile
    if ($result -ne "OK") {
        Write-Error "Failed to create fixture database"
        exit 1
    }

    Write-Host "[OK] Fixture database created" -ForegroundColor Green

    # Set up environment for both CLIs
    $env:SESSION_RECALL_DB = $fixtureDb
    
    # Telemetry paths
    $dotnetTelemetry = Join-Path $tempDir "dotnet_telemetry.json"
    $pythonTelemetry = Join-Path $tempDir "python_telemetry.json"

    # Fixed command sequence
    $commands = @(
        @{Cmd = "list"; Args = @() },
        @{Cmd = "list"; Args = @("--json") },
        @{Cmd = "files"; Args = @() },
        @{Cmd = "checkpoints"; Args = @() },
        @{Cmd = "search"; Args = @("python") },
        @{Cmd = "show"; Args = @("sess_001") },
        @{Cmd = "schema-check"; Args = @() },
        @{Cmd = "health"; Args = @() }
    )

    # Run commands with .NET CLI
    Write-Host "[INFO] Running commands with .NET CLI..." -ForegroundColor Cyan
    $env:SESSION_RECALL_TELEMETRY = $dotnetTelemetry
    foreach ($cmdInfo in $commands) {
        $cmdLine = $cmdInfo.Cmd
        $argList = $cmdInfo.Args
        if ($Verbose) {
            Write-Host "  > session-recall $cmdLine $($argList -join ' ')" -ForegroundColor DarkGray
        }
        # Redirect to null to avoid console encoding issues
        $null = & $dotnetExe $cmdLine @argList 2>&1
    }
    Write-Host "[OK] .NET CLI executed $($commands.Count) commands" -ForegroundColor Green

    # Run commands with Python CLI
    Write-Host "[INFO] Running commands with Python CLI..." -ForegroundColor Cyan
    $env:SESSION_RECALL_TELEMETRY = $pythonTelemetry
    $env:PYTHONPATH = Join-Path $repoRoot "src"
    $env:PYTHONIOENCODING = "utf-8"
    foreach ($cmdInfo in $commands) {
        $cmdLine = $cmdInfo.Cmd
        $argList = $cmdInfo.Args
        if ($Verbose) {
            Write-Host "  > python -m session_recall $cmdLine $($argList -join ' ')" -ForegroundColor DarkGray
        }
        # Redirect to null to avoid console encoding issues
        $null = python -m session_recall $cmdLine @argList 2>&1
    }
    Write-Host "[OK] Python CLI executed $($commands.Count) commands" -ForegroundColor Green

    # Parse and normalize telemetry files
    Write-Host "[INFO] Comparing telemetry output..." -ForegroundColor Cyan

    $normalizeScript = @"
import json
import sys

def normalize_entry(entry):
    """Remove ts and duration_ms for comparison."""
    normalized = entry.copy()
    normalized.pop('ts', None)
    normalized.pop('duration_ms', None)
    return normalized

if len(sys.argv) != 3:
    print('Usage: normalize.py <input_json> <output_json>', file=sys.stderr)
    sys.exit(1)

input_path = sys.argv[1]
output_path = sys.argv[2]

with open(input_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

entries = data.get('entries', [])
normalized_entries = [normalize_entry(e) for e in entries]

with open(output_path, 'w', encoding='utf-8') as f:
    json.dump({'entries': normalized_entries}, f, indent=2, sort_keys=True)

print('OK')
"@

    $normalizeScriptFile = Join-Path $tempDir "normalize.py"
    Set-Content -Path $normalizeScriptFile -Value $normalizeScript -Encoding UTF8

    # Normalize both telemetry files
    $dotnetNormalized = Join-Path $tempDir "dotnet_normalized.json"
    $pythonNormalized = Join-Path $tempDir "python_normalized.json"

    $result = python $normalizeScriptFile $dotnetTelemetry $dotnetNormalized
    if ($result -ne "OK") {
        Write-Error "Failed to normalize .NET telemetry"
        exit 1
    }

    $result = python $normalizeScriptFile $pythonTelemetry $pythonNormalized
    if ($result -ne "OK") {
        Write-Error "Failed to normalize Python telemetry"
        exit 1
    }

    # Compare normalized files
    $dotnetContent = Get-Content $dotnetNormalized -Raw
    $pythonContent = Get-Content $pythonNormalized -Raw

    if ($dotnetContent -eq $pythonContent) {
        Write-Host "[OK] Telemetry output matches (ignoring ts and duration_ms)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Telemetry output differs" -ForegroundColor Red
        Write-Host "Normalized .NET output: $dotnetNormalized" -ForegroundColor Yellow
        Write-Host "Normalized Python output: $pythonNormalized" -ForegroundColor Yellow
        
        # Show actual content for debugging
        Write-Host "`n=== .NET Telemetry (normalized) ===" -ForegroundColor Yellow
        Write-Host $dotnetContent
        Write-Host "`n=== Python Telemetry (normalized) ===" -ForegroundColor Yellow
        Write-Host $pythonContent
        
        exit 1
    }

    # Test jq on merged file
    Write-Host "[INFO] Testing jq on merged telemetry..." -ForegroundColor Cyan

    $mergedFile = Join-Path $tempDir "merged.jsonl"

    # Convert both telemetry files to JSONL format (one entry per line)
    $convertScript = @"
import json
import sys

if len(sys.argv) != 3:
    print('Usage: to_jsonl.py <input_json> <output_jsonl>', file=sys.stderr)
    sys.exit(1)

input_path = sys.argv[1]
output_path = sys.argv[2]

with open(input_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

entries = data.get('entries', [])

with open(output_path, 'w', encoding='utf-8') as f:
    for entry in entries:
        f.write(json.dumps(entry) + '\n')

print('OK')
"@

    $convertScriptFile = Join-Path $tempDir "to_jsonl.py"
    Set-Content -Path $convertScriptFile -Value $convertScript -Encoding UTF8

    # Convert .NET telemetry to JSONL
    $dotnetJsonl = Join-Path $tempDir "dotnet.jsonl"
    $result = python $convertScriptFile $dotnetTelemetry $dotnetJsonl
    if ($result -ne "OK") {
        Write-Error "Failed to convert .NET telemetry to JSONL"
        exit 1
    }

    # Merge: just use one of them since they're identical (after normalization)
    Copy-Item $dotnetJsonl $mergedFile

    # Test jq or PowerShell equivalent
    if ($hasJq) {
        Write-Host "[INFO] Testing with jq..." -ForegroundColor Cyan
        $jqOutput = jq -r '.cmd' $mergedFile | Sort-Object | Group-Object | Select-Object Count, Name
        
        if ($jqOutput.Count -eq 0) {
            Write-Host "[FAIL] jq produced no output" -ForegroundColor Red
            exit 1
        }

        Write-Host "[OK] jq successfully processed merged telemetry" -ForegroundColor Green
    } else {
        Write-Host "[INFO] Testing with PowerShell fallback..." -ForegroundColor Cyan
        # PowerShell equivalent of: jq -r '.cmd' merged.jsonl | sort | uniq -c
        $lines = Get-Content $mergedFile
        $commands = @()
        foreach ($line in $lines) {
            try {
                $entry = $line | ConvertFrom-Json
                $commands += $entry.cmd
            } catch {
                Write-Host "[FAIL] Failed to parse JSONL line: $line" -ForegroundColor Red
                exit 1
            }
        }
        
        if ($commands.Count -eq 0) {
            Write-Host "[FAIL] No commands found in merged telemetry" -ForegroundColor Red
            exit 1
        }

        $jqOutput = $commands | Group-Object | Sort-Object Name | Select-Object Count, Name
        Write-Host "[OK] PowerShell successfully processed merged telemetry" -ForegroundColor Green
    }
    
    if ($Verbose) {
        Write-Host "`nCommand counts:" -ForegroundColor Cyan
        $jqOutput | Format-Table -AutoSize | Out-String | Write-Host
    }

    Write-Host "`n[SUCCESS] Golden-file parity test passed" -ForegroundColor Green -NoNewline
    Write-Host " ✓" -ForegroundColor Green
    exit 0

} finally {
    # Cleanup
    if (Test-Path $tempDir) {
        Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
    }
}
