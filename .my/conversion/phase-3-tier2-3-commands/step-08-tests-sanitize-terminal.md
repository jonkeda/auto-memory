# Step 08 — Tests: sanitize terminal

## Goal
Port `tests/test_sanitize_terminal.py` → `AutoMemory.Tests/Util/AnsiSanitizerTests.cs`.

## Cases
- One assertion per ANSI/control category (CSI, OSC, ESC, C0, C1).
- Mixed input preserves printable text.
- Empty input → empty output.

## Done when
- [ ] All categories pass.
- [ ] Byte-exact output against Python fixture strings.
