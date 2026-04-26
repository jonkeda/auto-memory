# Step 04 — snake_case JSON policy

## Goal
Force snake_case property names so .NET output matches Python `json.dumps`.

## Approach
- Prefer `JsonNamingPolicy.SnakeCaseLower` (.NET 9+).
- Fallback: custom `JsonNamingPolicy` that converts PascalCase → snake_case.
- Apply globally via `JsonSerializerOptions`.

## Edge cases
- Existing snake_case names preserved.
- Acronyms (e.g. `IDs`) handled deterministically.

## Done when
- [ ] Round-trip test asserts identical key set vs Python output.
