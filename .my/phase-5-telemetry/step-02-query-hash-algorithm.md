# Step 02 — Query-hash algorithm

## Goal
Reproduce Python's `query_hash` byte-for-byte in .NET.

## Algorithm (from `util/telemetry.py`)
1. Lowercase.
2. Collapse whitespace runs to a single space; trim.
3. SHA-256 of UTF-8 bytes.
4. First 8 hex chars (lowercase).

## .NET
```csharp
using var sha = SHA256.Create();
var hex = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
return hex[..8];
```

## Cross-language fixture
`tests/fixtures/query_hashes.csv` — 50 rows of `(input, expected_hash)` shared between pytest and xUnit.

## Done when
- [ ] All 50 fixture rows match between Python and .NET.
