#!/usr/bin/env python3
"""Verify .NET-written telemetry by reading with Python and comparing to Python-written version.

Usage:
    python verify_telemetry_roundtrip.py <dotnet_telemetry_path> <manifest_json_path> <python_telemetry_path>

Reads the .NET-written telemetry file, validates it can be parsed by Python,
then writes the same records using Python's telemetry.record, and compares
the two files (ignoring ts and duration_ms fields).

Exit codes:
    0 - Success (files match except for ts/duration_ms)
    1 - Parse error or comparison failed
"""
import json
import sys
from pathlib import Path


def normalize_entry(entry: dict) -> dict:
    """Remove ts and duration_ms for comparison."""
    normalized = entry.copy()
    normalized.pop("ts", None)
    normalized.pop("duration_ms", None)
    return normalized


def main():
    if len(sys.argv) != 4:
        print(f"Usage: {sys.argv[0]} <dotnet_telemetry_path> <manifest_json_path> <python_telemetry_path>", file=sys.stderr)
        sys.exit(1)

    dotnet_path = Path(sys.argv[1])
    manifest_path = Path(sys.argv[2])
    python_path = Path(sys.argv[3])

    # Read .NET-written telemetry
    try:
        # Try with explicit encoding
        content = dotnet_path.read_text(encoding='utf-8')
        dotnet_data = json.loads(content)
        dotnet_entries = dotnet_data.get("entries", [])
        print(f"[OK] Parsed {len(dotnet_entries)} entries from .NET telemetry")
    except Exception as e:
        print(f"[FAIL] Failed to parse .NET telemetry: {e}", file=sys.stderr)
        sys.exit(1)

    # Read manifest (list of records to write)
    try:
        manifest = json.loads(manifest_path.read_text())
        print(f"[OK] Read {len(manifest)} records from manifest")
    except Exception as e:
        print(f"[FAIL] Failed to parse manifest: {e}", file=sys.stderr)
        sys.exit(1)

    # Write the same records using Python telemetry
    # Import here to avoid circular dependency issues
    sys.path.insert(0, str(Path(__file__).parent.parent / "src"))
    from session_recall.util import telemetry

    telemetry.init(str(python_path))
    
    try:
        for rec in manifest:
            # Map C# property names to Python parameter names
            telemetry.record(
                cmd=rec["cmd"],
                duration_ms=rec["duration_ms"],
                busy_hits=rec.get("busy_hits", 0),
                attempts=rec.get("attempts", 1),
                rows=rec.get("rows_returned", 0),
                exit_code=rec.get("exit_code", 0),
                schema_ok=rec.get("schema_ok", True),
                tier=rec.get("tier"),
                query_hash=rec.get("query_hash"),
                session_id_prefix=rec.get("session_id_prefix"),
                window_tier=rec.get("window_tier"),
            )
        print(f"[OK] Wrote {len(manifest)} records using Python telemetry")
    except Exception as e:
        print(f"[FAIL] Failed to write Python telemetry: {e}", file=sys.stderr)
        sys.exit(1)

    # Read Python-written telemetry
    try:
        python_data = json.loads(python_path.read_text())
        python_entries = python_data.get("entries", [])
        print(f"[OK] Parsed {len(python_entries)} entries from Python telemetry")
    except Exception as e:
        print(f"[FAIL] Failed to parse Python telemetry: {e}", file=sys.stderr)
        sys.exit(1)

    # Compare entries (ignoring ts and duration_ms)
    if len(dotnet_entries) != len(python_entries):
        print(f"[FAIL] Entry count mismatch: .NET={len(dotnet_entries)}, Python={len(python_entries)}", file=sys.stderr)
        sys.exit(1)

    mismatches = []
    for i, (dotnet_entry, python_entry) in enumerate(zip(dotnet_entries, python_entries)):
        normalized_dotnet = normalize_entry(dotnet_entry)
        normalized_python = normalize_entry(python_entry)
        
        if normalized_dotnet != normalized_python:
            mismatches.append((i, normalized_dotnet, normalized_python))

    if mismatches:
        print(f"[FAIL] Found {len(mismatches)} mismatched entries:", file=sys.stderr)
        for idx, dotnet_norm, python_norm in mismatches[:5]:  # Show first 5
            print(f"  Entry {idx}:", file=sys.stderr)
            print(f"    .NET:   {json.dumps(dotnet_norm, sort_keys=True)}", file=sys.stderr)
            print(f"    Python: {json.dumps(python_norm, sort_keys=True)}", file=sys.stderr)
        sys.exit(1)

    print(f"[OK] All {len(dotnet_entries)} entries match (ignoring ts and duration_ms)")
    sys.exit(0)


if __name__ == "__main__":
    main()
