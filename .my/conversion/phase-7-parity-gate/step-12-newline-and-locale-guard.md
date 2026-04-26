# Step 12 — Newline + locale guard

## Goal
Eliminate cross-platform output divergence.

## Actions
- At Program startup: `Console.Out.NewLine = "\n";` and `Console.Error.NewLine = "\n";`.
- `Directory.Build.props` already sets `<InvariantGlobalization>true</InvariantGlobalization>`.
- All number formatting uses `CultureInfo.InvariantCulture`.

## Done when
- [ ] Windows + Linux parity job uses identical golden files.
- [ ] No `\r\n` byte appears in stdout for any case.
