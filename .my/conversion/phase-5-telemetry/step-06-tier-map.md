# Step 06 — TIER_MAP

## Goal
Mirror Python's `TIER_MAP` exactly.

## Constant
```csharp
internal static readonly IReadOnlyDictionary<string, int> TierMap = new Dictionary<string, int> {
    ["list"] = 1, ["files"] = 1, ["checkpoints"] = 1,
    ["search"] = 2,
    ["show"] = 3,
    ["health"] = 0, ["schema-check"] = 0, ["calibrate"] = 0,
};
```

## Done when
- [ ] Unit test asserts every key/value matches the Python dict.
