# Step 01 — ArgParser

## Goal
Hand-rolled `ArgParser` in `AutoMemory.Cli/ArgParser.cs`. Zero deps.

## Features
- `--name=value` and `--name value`.
- Boolean flags (`--json`, `--full`).
- Positional args.
- `-h` / `--help` per subcommand.
- Unknown flag → exit **2** with `usage: ...` line on stderr.

## API sketch
```csharp
public sealed class ArgParser {
    public ArgParser AddOption(string name, string? shortName = null, bool isFlag = false, string? defaultValue = null);
    public ArgParser AddPositional(string name, bool required = true);
    public ParsedArgs Parse(string[] argv); // throws UsageException → caller exits 2
}
```

## Notes
- Argparse error wording can differ slightly from Python — document divergence in `net/README.md`.
- Force `CultureInfo.InvariantCulture` for any int parsing.

## Done when
- [ ] All flag styles parsed.
- [ ] Negative-int rejection produces a usage error.
- [ ] Unknown flag → exit 2.
- [ ] `-h` prints command-specific help.
