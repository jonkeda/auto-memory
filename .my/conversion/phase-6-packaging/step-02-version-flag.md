# Step 02 — `--version` flag

## Goal
Add a top-level `--version` flag whose output matches `pyproject.toml` version exactly.

## Implementation
- Embed version via MSBuild: `<AssemblyVersion>` / `<InformationalVersion>` injected from a `Version.props` checked into `net/`.
- A CI step asserts `net/Version.props` matches `pyproject.toml`.

## Done when
- [ ] `session-recall --version` prints e.g. `session-recall 0.4.2`.
- [ ] CI fails if versions drift between `pyproject.toml` and `Version.props`.
