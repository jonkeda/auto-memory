# Step 06 — Util FormatOutput

## Goal
Port `util/format_output.py` → `AutoMemory.Core/Util/FormatOutput.cs`. ANSI/control sanitization + table rendering.

## Source Mapping
- **Python**: `src/session_recall/util/format_output.py`
- **.NET**: `AutoMemory.Core/Util/FormatOutput.cs`

## Sanitizer Regex (terminal control)
- CSI: `\x1b\[[0-?]*[ -/]*[@-~]`
- OSC: `\x1b\][^\x07\x1b]*(?:\x07|\x1b\\)`
- ESC-prefixed: `\x1b[@-Z\\-_]`
- C0 (except TAB/LF/CR): `[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]`
- C1: `[\x80-\x9f]`

## Methods
- `SanitizeForTerminal(string)`
- `FmtJson(object)` — `System.Text.Json` pretty (2-space indent), snake_case.
- `FmtHumanSessions(IEnumerable<SessionRecord>)` — fixed-width table.
- `Output(object payload, bool jsonMode)` — dispatch.

## Table Layout
```
ID        Date        Turns  Summary
--------  ----------  -----  -------
abcd1234  2025-04-01     12  ...
```
Columns: ID(8) Date(10) Turns(5, right) Summary(40, truncate).

## Done when
- [ ] All ANSI/control sequences stripped.
- [ ] UTF-8 preserved.
- [ ] JSON output byte-equal to Python's pretty form.
- [ ] Table column widths/alignment match Python.
