# Step 05 — Util DetectRepo

## Goal
Port `util/detect_repo.py` → `AutoMemory.Core/Util/DetectRepo.cs`.

## Source Mapping
- **Python**: `src/session_recall/util/detect_repo.py`
- **.NET**: `AutoMemory.Core/Util/DetectRepo.cs`

## Behavior

1. Spawn `git remote get-url origin` with 5s timeout.
2. Parse the URL with two regexes:
   - SSH: `git@[^:]+:(.+?)(?:\.git)?$` → group 1 = `owner/repo`.
   - HTTPS: `https?://[^/]+/(.+?)(?:\.git)?$` → group 1 = `owner/repo`.
3. Return `owner/repo` or `null`.

## Failure Modes (all return `null`)
- `git` not on PATH.
- Process timeout.
- Empty stdout.
- URL doesn't match either regex.

## Examples
- `git@github.com:o/r.git` → `o/r`
- `https://github.com/o/r` → `o/r`
- `https://github.com/o/r.git` → `o/r`

## Done when
- [ ] SSH + HTTPS both parsed.
- [ ] `.git` suffix stripped.
- [ ] All failure paths return `null` (no exceptions surface).
