# SQL Injection Analyzer (AUTOMEM001)

## Overview
Custom Roslyn analyzer that detects SQL injection risks in `AutoMemory.Core` and consumer projects.

## What it detects
- String interpolation flowing into `SqliteCommand.CommandText` (e.g., `cmd.CommandText = $"SELECT * FROM {table}"`)
- String concatenation with variables (e.g., `cmd.CommandText = "SELECT * FROM " + tableName`)

## What it allows
- Parameterized queries (recommended): `cmd.CommandText = "SELECT * FROM users WHERE id = @id"`
- Pure string literals: `cmd.CommandText = "SELECT * FROM users"`
- Const/readonly static field concatenation: `cmd.CommandText = BaseQuery + " WHERE ..."`
- Local variables that only receive constant assignments

## Suppression
For known-safe patterns (e.g., PRAGMA statements with controlled table names), use:
```csharp
#pragma warning disable AUTOMEM001 // reason why this is safe
cmd.CommandText = $"PRAGMA table_info({table})";
#pragma warning restore AUTOMEM001
```

## Verification
1. The analyzer correctly flags bad patterns (tested with string interpolation/concatenation)
2. Production code in AutoMemory.Core compiles with zero AUTOMEM001 errors
3. The analyzer DLL is packaged at `analyzers/dotnet/cs/AutoMemory.Analyzers.dll` in the NuGet package
4. Consumers who install AutoMemory.Core via NuGet automatically inherit the analyzer

## Testing
See `SqlInjectionAnalyzerTests.cs` for example patterns. To manually test:
1. Uncomment the bad SQL patterns in the test file
2. Run `dotnet build` and confirm AUTOMEM001 errors appear
3. Re-comment the code before committing
