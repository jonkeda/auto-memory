# Step 03 — Case file format

## Goal
Define a simple text format for a parity case so contributors can add new cases without changing code.

## Format (`.case`)
```
ARGV: list --limit 5 --json
EXIT: 0
TIER: 1
STDERR_REGEX: ^$
STDOUT_HASH: <sha256>
```

Or with inline expected stdout for small outputs:
```
ARGV: schema-check
EXIT: 0
STDOUT:
schema OK
END_STDOUT
```

## Done when
- [ ] Parser implemented and unit-tested.
- [ ] At least one case loads and executes end-to-end.
