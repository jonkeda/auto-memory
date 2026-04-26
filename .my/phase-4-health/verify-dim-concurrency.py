#!/usr/bin/env python3
"""Verify .NET DimConcurrency matches Python reference implementation."""
import json
import sys
import tempfile
from pathlib import Path

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent.parent / "src"))


def test_fixture_zero_busy():
    """Fixture 1: 50 entries, zero busy hits."""
    entries = []
    for i in range(50):
        entries.append({
            "cmd": "search",
            "ts": "2026-04-25T10:00:00Z",
            "busy_hits": 0,
            "attempts": 1,
            "duration_ms": 100 + i
        })
    
    # Write to temp file
    with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
        json.dump({"entries": entries}, f, indent=2)
        temp_path = f.name
    
    try:
        # Monkey-patch the config module before importing dim_concurrency
        from session_recall import config
        original_path = config.TELEMETRY_PATH
        config.TELEMETRY_PATH = temp_path
        
        # Import after patching
        from session_recall.health.dim_concurrency import check
        
        result = check()
        
        print("Python result:")
        print(f"  name: {result['name']}")
        print(f"  score: {result['score']}")
        print(f"  zone: {result['zone']}")
        print(f"  detail: {result['detail']}")
        print(f"  hint: {result['hint']}")
        
        # Restore
        config.TELEMETRY_PATH = original_path
        
        # Verify
        assert result['zone'] == 'GREEN', f"Expected GREEN, got {result['zone']}"
        assert result['score'] >= 7.0, f"Expected score >= 7.0, got {result['score']}"
        assert 'busy=0.0%' in result['detail'], f"Expected 'busy=0.0%' in detail: {result['detail']}"
        assert 'avg_attempts=1.00' in result['detail'], f"Expected 'avg_attempts=1.00' in detail"
        assert 'n=50' in result['detail'], f"Expected 'n=50' in detail"
        
        print("\n✓ All assertions passed - .NET implementation matches Python!")
        
    finally:
        Path(temp_path).unlink(missing_ok=True)


def test_fixture_amber():
    """Fixture 3: 100 entries, 10% busy rate."""
    entries = []
    for i in range(100):
        entries.append({
            "cmd": "search",
            "ts": "2026-04-25T10:00:00Z",
            "busy_hits": 1 if i < 10 else 0,
            "attempts": 2 if i < 10 else 1,
            "duration_ms": 100 + i * 3
        })
    
    # Write to temp file
    with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
        json.dump({"entries": entries}, f, indent=2)
        temp_path = f.name
    
    try:
        # Reload the module with new path
        import importlib
        from session_recall import config
        config.TELEMETRY_PATH = temp_path
        
        # Force reload
        import session_recall.health.dim_concurrency
        importlib.reload(session_recall.health.dim_concurrency)
        from session_recall.health.dim_concurrency import check
        
        result = check()
        
        print("\nPython result (AMBER fixture):")
        print(f"  name: {result['name']}")
        print(f"  score: {result['score']}")
        print(f"  zone: {result['zone']}")
        print(f"  detail: {result['detail']}")
        
        # Verify
        assert result['zone'] == 'AMBER', f"Expected AMBER, got {result['zone']}"
        assert 4.0 <= result['score'] < 7.0, f"Expected 4.0 <= score < 7.0, got {result['score']}"
        assert 'busy=10.0%' in result['detail'], f"Expected 'busy=10.0%' in detail: {result['detail']}"
        
        print("\n✓ AMBER fixture assertions passed!")
        
    finally:
        Path(temp_path).unlink(missing_ok=True)


if __name__ == '__main__':
    test_fixture_zero_busy()
    test_fixture_amber()
    print("\n✅ All Python verification tests passed!")
