#!/usr/bin/env python3
"""
Create a minimal fixture database for CI smoke testing.
This generates tests/fixtures/session-store.db with deterministic sample data.
"""
import sqlite3
import sys
from pathlib import Path
from datetime import datetime, timedelta, timezone

def create_fixture_db():
    """Create fixture database with sample data for smoke tests."""
    fixture_dir = Path(__file__).parent
    db_path = fixture_dir / "session-store.db"
    
    # Remove existing fixture if present
    if db_path.exists():
        db_path.unlink()
    
    conn = sqlite3.connect(str(db_path))
    cur = conn.cursor()
    
    # Create schema matching session_recall
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
        CREATE TABLE session_files (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            file_path TEXT,
            tool_name TEXT,
            turn_index INTEGER,
            first_seen_at TEXT
        )
    ''')
    
    cur.execute('''
        CREATE TABLE session_refs (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            ref_type TEXT,
            ref_value TEXT,
            turn_index INTEGER,
            created_at TEXT
        )
    ''')
    
    cur.execute('''
        CREATE TABLE checkpoints (
            id INTEGER PRIMARY KEY,
            session_id TEXT,
            checkpoint_number INTEGER,
            title TEXT,
            overview TEXT,
            created_at TEXT
        )
    ''')
    
    # Use recent dates relative to now (within last 7 days for default 30-day filter)
    now = datetime.now(timezone.utc)
    
    # Insert deterministic sample sessions (5+ for limit testing)
    sessions = [
        ('session_001', '/home/user/project', 'owner/repo', 'main', 
         'First test session', 
         (now - timedelta(days=1)).isoformat(),
         (now - timedelta(days=1, hours=-1)).isoformat(), 
         'cli'),
        ('session_002', '/home/user/project', 'owner/repo', 'feature-x', 
         'Second test session', 
         (now - timedelta(days=2)).isoformat(),
         (now - timedelta(days=2, hours=-1)).isoformat(), 
         'cli'),
        ('session_003', '/home/user/other', 'other/repo', 'main', 
         'Different repo session', 
         (now - timedelta(days=3)).isoformat(),
         (now - timedelta(days=3, hours=-1)).isoformat(), 
         'cli'),
        ('session_004', '/home/user/project', 'owner/repo', 'main', 
         'Fourth session', 
         (now - timedelta(days=4)).isoformat(),
         (now - timedelta(days=4, hours=-1)).isoformat(), 
         'cli'),
        ('session_005', '/home/user/project', 'owner/repo', 'dev', 
         'Fifth session', 
         (now - timedelta(days=5)).isoformat(),
         (now - timedelta(days=5, hours=-1)).isoformat(), 
         'cli'),
        ('session_006', '/home/user/project', 'owner/repo', 'main', 
         'Sixth session', 
         (now - timedelta(days=6)).isoformat(),
         (now - timedelta(days=6, hours=-1)).isoformat(), 
         'cli'),
    ]
    
    for sess in sessions:
        cur.execute('''
            INSERT INTO sessions (id, cwd, repository, branch, summary, created_at, updated_at, host_type)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        ''', sess)
    
    # Add some turns for each session
    turn_id = 1
    for i, (session_id, *_) in enumerate(sessions):
        for turn_idx in range(2):  # 2 turns per session
            cur.execute('''
                INSERT INTO turns (id, session_id, turn_index, user_message, assistant_response, timestamp)
                VALUES (?, ?, ?, ?, ?, ?)
            ''', (
                turn_id,
                session_id,
                turn_idx,
                f'User message {turn_idx} for {session_id}',
                f'Assistant response {turn_idx}',
                (now - timedelta(days=i+1, minutes=turn_idx*5)).isoformat()
            ))
            turn_id += 1
    
    # Add sample files
    file_id = 1
    for i, (session_id, *_) in enumerate(sessions[:3]):  # Files for first 3 sessions
        cur.execute('''
            INSERT INTO session_files (id, session_id, file_path, tool_name, turn_index, first_seen_at)
            VALUES (?, ?, ?, ?, ?, ?)
        ''', (
            file_id,
            session_id,
            f'/path/to/file_{i}.md',
            'read_file',
            0,
            (now - timedelta(days=i+1)).isoformat()
        ))
        file_id += 1
    
    # Add sample checkpoints
    checkpoint_id = 1
    for i, (session_id, *_) in enumerate(sessions[:2]):  # Checkpoints for first 2 sessions
        cur.execute('''
            INSERT INTO checkpoints (id, session_id, checkpoint_number, title, overview, created_at)
            VALUES (?, ?, ?, ?, ?, ?)
        ''', (
            checkpoint_id,
            session_id,
            1,
            f'Checkpoint for {session_id}',
            f'Sample checkpoint overview',
            (now - timedelta(days=i+1, minutes=30)).isoformat()
        ))
        checkpoint_id += 1
    
    conn.commit()
    conn.close()
    
    print(f"✓ Created fixture database: {db_path}")
    print(f"  - {len(sessions)} sessions")
    print(f"  - {turn_id - 1} turns")
    print(f"  - {file_id - 1} files")
    print(f"  - {checkpoint_id - 1} checkpoints")
    print(f"  - Date range: {sessions[-1][5][:10]} to {sessions[0][5][:10]}")
    return 0

if __name__ == '__main__':
    sys.exit(create_fixture_db())
