# Step 03 — ANSI sanitizer

## Goal
Strip ANSI/control sequences from session text for the default `show` view (full mode preserves them).

## Source Mapping
- Python uses regex in `commands/show_session.py` / `util/format_output.py` — port literally.
- .NET: `AutoMemory.Core/Util/AnsiSanitizer.cs` (or extend `FormatOutput`).

## Regex
- CSI: `\x1b\[[0-?]*[ -/]*[@-~]`
- OSC: `\x1b\][^\x07\x1b]*(?:\x07|\x1b\\)`
- Other ESC: `\x1b[@-Z\\-_]`
- C0 (except TAB/LF/CR): `[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]`
- C1: `[\x80-\x9f]`

## Done when
- [ ] Each ANSI category tested.
- [ ] UTF-8 preserved (no double-decoding).
- [ ] `--full` path bypasses stripping.
