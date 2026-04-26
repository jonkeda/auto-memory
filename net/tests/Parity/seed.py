#!/usr/bin/env python3
"""
Deterministic seeder for parity-test SQLite fixture.

Creates a fixture database with:
- 25 sessions across 3 repos
- 200 file rows
- 15 checkpoints
- 8 sessions with summaries (intentional gaps for freshness/coverage scoring)
- Fixed random seed for reproducibility
- Deterministic timestamps anchored at BASE_DATE

Usage: python seed.py <output_path.sqlite>
"""
import hashlib
import random
import sqlite3
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

# Fixed seed for reproducibility
RANDOM_SEED = 42

# Fixed base date for deterministic timestamps (2024-01-01 00:00:00 UTC)
BASE_DATE = datetime(2024, 1, 1, 0, 0, 0, tzinfo=timezone.utc)

# Repository configuration
REPOS = [
    "acme/frontend",
    "acme/backend",
    "acme/mobile",
]

BRANCHES = ["main", "develop", "feature-auth", "bugfix-123", "release-v2"]

# Tool names for file tracking
TOOLS = ["read_file", "replace_string_in_file", "grep_search", "semantic_search", "list_dir"]

def create_schema(conn):
    """Create all required tables matching Copilot CLI schema."""
    conn.execute("""
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
    """)
    
    conn.execute("""
        CREATE TABLE turns (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            turn_index INTEGER,
            user_message TEXT,
            assistant_response TEXT,
            timestamp TEXT
        )
    """)
    
    conn.execute("""
        CREATE TABLE session_files (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            file_path TEXT,
            tool_name TEXT,
            turn_index INTEGER,
            first_seen_at TEXT
        )
    """)
    
    conn.execute("""
        CREATE TABLE session_refs (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            ref_type TEXT,
            ref_value TEXT,
            turn_index INTEGER,
            created_at TEXT
        )
    """)
    
    conn.execute("""
        CREATE TABLE checkpoints (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            checkpoint_number INTEGER,
            title TEXT,
            overview TEXT,
            created_at TEXT
        )
    """)
    
    # Create FTS5 search index (virtual table)
    conn.execute("""
        CREATE VIRTUAL TABLE search_index USING fts5(
            content, 
            session_id UNINDEXED, 
            source_type UNINDEXED, 
            source_id UNINDEXED
        )
    """)


def seed_data(conn):
    """Populate fixture with deterministic data."""
    random.seed(RANDOM_SEED)
    
    # Track which sessions get summaries (8 out of 25)
    # We'll give summaries to sessions at indices: 0, 2, 5, 8, 11, 15, 19, 23
    sessions_with_summaries = {0, 2, 5, 8, 11, 15, 19, 23}
    
    # Generate 25 sessions
    session_ids = []
    for i in range(25):
        # Use hex-based IDs (valid for show_session regex: ^[0-9a-fA-F-]{4,}$)
        # Format: abcd0000, abcd0001, etc. (deterministic, ≥4 hex chars)
        session_id = f"abcd{i:04x}"
        session_ids.append(session_id)
        
        # Distribute across repos
        repo = REPOS[i % len(REPOS)]
        branch = BRANCHES[i % len(BRANCHES)]
        cwd = f"/home/user/{repo.split('/')[1]}"
        
        # Only some sessions get summaries (intentional gaps for coverage testing)
        # Add searchable keywords to some summaries
        if i in sessions_with_summaries:
            if i % 4 == 0:
                summary = f"Session {i}: hello world feature for {repo}"
            elif i % 4 == 1:
                summary = f"Session {i}: needle search implementation for {repo}"
            else:
                summary = f"Session {i}: implemented feature X for {repo}"
        else:
            summary = None
        
        # Spread sessions across 60 days before BASE_DATE
        days_offset = i * 2 + (i % 3)  # Creates irregular spacing
        created_at = (BASE_DATE - timedelta(days=days_offset, hours=i % 24, minutes=i % 60))
        updated_at = created_at + timedelta(minutes=random.randint(5, 120))
        
        host_type = "cli" if i % 4 != 0 else "vscode"
        
        conn.execute(
            """INSERT INTO sessions 
               (id, cwd, repository, branch, summary, created_at, updated_at, host_type)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
            (session_id, cwd, repo, branch, summary, 
             created_at.isoformat(), updated_at.isoformat(), host_type)
        )
        
        # Add summary to search_index if present
        if summary:
            conn.execute(
                """INSERT INTO search_index (content, session_id, source_type, source_id)
                   VALUES (?, ?, 'summary', ?)""",
                (summary, session_id, session_id)
            )
    
    # Generate turns (3-5 per session for variety)
    turn_id = 1
    # Keywords for search testing
    search_keywords = [
        "hello world implementation",
        "needle in haystack pattern",
        "hello testing framework",
        "database optimization",
        "hello API endpoint",
        "needle finder utility",
        "performance tuning",
        "hello service integration",
        "error handling",
        "hello configuration"
    ]
    
    for i, session_id in enumerate(session_ids):
        num_turns = 3 + (i % 3)  # 3, 4, or 5 turns
        session_created = BASE_DATE - timedelta(days=i * 2 + (i % 3))
        
        for turn_idx in range(num_turns):
            timestamp = session_created + timedelta(minutes=turn_idx * 5)
            # Use search keywords for some turns to enable search testing
            keyword_content = search_keywords[(i + turn_idx) % len(search_keywords)]
            user_msg = f"User message {turn_idx} for {session_id}"
            assistant_resp = f"Assistant response {turn_idx} with {keyword_content} for the task"
            
            conn.execute(
                """INSERT INTO turns 
                   (id, session_id, turn_index, user_message, assistant_response, timestamp)
                   VALUES (?, ?, ?, ?, ?, ?)""",
                (turn_id, session_id, turn_idx, user_msg, assistant_resp, timestamp.isoformat())
            )
            
            # Add to search_index (index assistant responses)
            conn.execute(
                """INSERT INTO search_index (content, session_id, source_type, source_id)
                   VALUES (?, ?, 'turn', ?)""",
                (assistant_resp, session_id, str(turn_id))
            )
            
            turn_id += 1
    
    # Generate 200 file rows distributed across all sessions
    file_id = 1
    files_per_session = 200 // 25  # ~8 files per session
    remainder = 200 % 25
    
    file_extensions = [".ts", ".py", ".cs", ".md", ".json", ".yaml", ".txt", ".sh"]
    file_dirs = ["src", "tests", "docs", "config", "scripts", "lib", "utils"]
    
    for i, session_id in enumerate(session_ids):
        # First few sessions get extra files to reach exactly 200
        num_files = files_per_session + (1 if i < remainder else 0)
        session_created = BASE_DATE - timedelta(days=i * 2 + (i % 3))
        
        for f in range(num_files):
            dir_name = file_dirs[f % len(file_dirs)]
            ext = file_extensions[f % len(file_extensions)]
            file_path = f"/{dir_name}/file_{i}_{f}{ext}"
            tool_name = TOOLS[f % len(TOOLS)]
            turn_index = f % 5  # Reference turn 0-4
            first_seen = session_created + timedelta(minutes=turn_index * 5 + f)
            
            conn.execute(
                """INSERT INTO session_files 
                   (id, session_id, file_path, tool_name, turn_index, first_seen_at)
                   VALUES (?, ?, ?, ?, ?, ?)""",
                (file_id, session_id, file_path, tool_name, turn_index, first_seen.isoformat())
            )
            file_id += 1
    
    # Generate 15 checkpoints spread across first 15 sessions (1 per session)
    checkpoint_id = 1
    for i in range(15):
        session_id = session_ids[i]
        session_created = BASE_DATE - timedelta(days=i * 2 + (i % 3))
        
        checkpoint_num = 1
        title = f"Checkpoint {checkpoint_num} for {session_id}"
        overview = f"Completed phase {checkpoint_num} of the implementation"
        created_at = session_created + timedelta(minutes=30)
        
        conn.execute(
            """INSERT INTO checkpoints 
               (id, session_id, checkpoint_number, title, overview, created_at)
               VALUES (?, ?, ?, ?, ?, ?)""",
            (checkpoint_id, session_id, checkpoint_num, title, overview, created_at.isoformat())
        )
        checkpoint_id += 1
    
    # Generate some session_refs for variety (20 refs across different sessions)
    ref_id = 1
    ref_types = ["issue", "pr", "commit", "doc"]
    for i in range(20):
        session_id = session_ids[i]
        session_created = BASE_DATE - timedelta(days=i * 2 + (i % 3))
        
        ref_type = ref_types[i % len(ref_types)]
        ref_value = f"{ref_type}_{i:03d}"
        turn_index = i % 5
        created_at = session_created + timedelta(minutes=turn_index * 5)
        
        conn.execute(
            """INSERT INTO session_refs 
               (id, session_id, ref_type, ref_value, turn_index, created_at)
               VALUES (?, ?, ?, ?, ?, ?)""",
            (ref_id, session_id, ref_type, ref_value, turn_index, created_at.isoformat())
        )
        ref_id += 1


def compute_sha256(path: Path) -> str:
    """Compute SHA-256 hash of file."""
    sha256 = hashlib.sha256()
    with open(path, 'rb') as f:
        while chunk := f.read(65536):
            sha256.update(chunk)
    return sha256.hexdigest()


def main():
    if len(sys.argv) != 2:
        print("Usage: python seed.py <output_path.sqlite>", file=sys.stderr)
        return 2
    
    output_path = Path(sys.argv[1])
    
    # Remove existing file if present
    if output_path.exists():
        output_path.unlink()
    
    # Ensure parent directory exists
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    # Create and populate database
    conn = sqlite3.connect(str(output_path))
    create_schema(conn)
    seed_data(conn)
    conn.commit()
    
    # Gather stats
    stats = {
        'sessions': conn.execute("SELECT COUNT(*) FROM sessions").fetchone()[0],
        'sessions_with_summary': conn.execute("SELECT COUNT(*) FROM sessions WHERE summary IS NOT NULL").fetchone()[0],
        'turns': conn.execute("SELECT COUNT(*) FROM turns").fetchone()[0],
        'files': conn.execute("SELECT COUNT(*) FROM session_files").fetchone()[0],
        'checkpoints': conn.execute("SELECT COUNT(*) FROM checkpoints").fetchone()[0],
        'refs': conn.execute("SELECT COUNT(*) FROM session_refs").fetchone()[0],
        'search_entries': conn.execute("SELECT COUNT(*) FROM search_index").fetchone()[0],
    }
    
    conn.close()
    
    # Compute size and hash
    size_bytes = output_path.stat().st_size
    size_mb = size_bytes / (1024 * 1024)
    sha256 = compute_sha256(output_path)
    
    print(f"✓ Created parity fixture: {output_path}")
    print(f"  Sessions: {stats['sessions']} ({stats['sessions_with_summary']} with summaries)")
    print(f"  Turns: {stats['turns']}")
    print(f"  Files: {stats['files']}")
    print(f"  Checkpoints: {stats['checkpoints']}")
    print(f"  Refs: {stats['refs']}")
    print(f"  Search entries: {stats['search_entries']}")
    print(f"  Size: {size_mb:.2f} MB")
    print(f"  SHA-256: {sha256}")
    
    # Verify requirements
    success = True
    if stats['sessions'] != 25:
        print(f"  ✗ Expected 25 sessions, got {stats['sessions']}", file=sys.stderr)
        success = False
    if stats['files'] != 200:
        print(f"  ✗ Expected 200 files, got {stats['files']}", file=sys.stderr)
        success = False
    if stats['checkpoints'] != 15:
        print(f"  ✗ Expected 15 checkpoints, got {stats['checkpoints']}", file=sys.stderr)
        success = False
    if stats['sessions_with_summary'] != 8:
        print(f"  ✗ Expected 8 sessions with summaries, got {stats['sessions_with_summary']}", file=sys.stderr)
        success = False
    if size_mb >= 5.0:
        print(f"  ✗ Size {size_mb:.2f} MB exceeds 5 MB limit", file=sys.stderr)
        success = False
    
    return 0 if success else 1


if __name__ == '__main__':
    sys.exit(main())
