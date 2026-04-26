#!/usr/bin/env python3
"""
Create a fixture database with schema mismatch (missing column) for testing schema-check.

This script copies fixture.sqlite and drops the 'summary' column from the sessions table
to simulate schema drift.

Usage: python create_mismatch_fixture.py
"""
import shutil
import sqlite3
import sys
from pathlib import Path

def create_mismatch_fixture():
    """Create fixture-mismatch.sqlite by dropping a column from fixture.sqlite."""
    script_dir = Path(__file__).parent
    source_db = script_dir / "fixture.sqlite"
    target_db = script_dir / "fixture-mismatch.sqlite"
    
    if not source_db.exists():
        print(f"Error: Source DB not found: {source_db}", file=sys.stderr)
        print("Run: python seed.py fixture.sqlite", file=sys.stderr)
        return 1
    
    # Copy the source DB
    print(f"Copying {source_db} -> {target_db}")
    shutil.copy(source_db, target_db)
    
    # Connect and drop a column (recreate table without 'summary')
    print("Dropping 'summary' column from sessions table to create schema mismatch")
    conn = sqlite3.connect(target_db)
    
    try:
        # SQLite doesn't support DROP COLUMN directly before 3.35.0
        # Use the standard ALTER TABLE workaround: create new table, copy data, rename
        
        # 1. Create new sessions table without 'summary'
        conn.execute("""
            CREATE TABLE sessions_new (
                id TEXT PRIMARY KEY,
                cwd TEXT,
                repository TEXT,
                branch TEXT,
                created_at TEXT,
                updated_at TEXT,
                host_type TEXT
            )
        """)
        
        # 2. Copy data (excluding 'summary')
        conn.execute("""
            INSERT INTO sessions_new 
            SELECT id, cwd, repository, branch, created_at, updated_at, host_type
            FROM sessions
        """)
        
        # 3. Drop old table
        conn.execute("DROP TABLE sessions")
        
        # 4. Rename new table
        conn.execute("ALTER TABLE sessions_new RENAME TO sessions")
        
        conn.commit()
        print(f"✅ Created {target_db} with missing 'summary' column")
        return 0
        
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        conn.rollback()
        return 1
    finally:
        conn.close()

if __name__ == "__main__":
    sys.exit(create_mismatch_fixture())
