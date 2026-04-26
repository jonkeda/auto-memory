# AutoMemory.Core

**Embeddable session recall for .NET tools.**

This package provides the core API to build tools that query GitHub Copilot CLI session history. Use it to embed session recall into your own .NET applications.

## Public API

- **`SessionStore`** — Manages SQLite connection and query execution.
- **`HealthRunner`** — Verifies database schema and configuration.
- **`SchemaChecker`** — Validates FTS5 and table structure.
- **`TelemetryWriter`** — Emits structured JSON telemetry events.

## Installation

```bash
dotnet add package AutoMemory.Core
```

## Quick Example

```csharp
using AutoMemory.Core;
using AutoMemory.Core.Db;

var cfg = Config.Load();
using var store = SessionStore.Connect(cfg);
var results = store.ListConversations(limit: 10);

foreach (var conv in results) {
    Console.WriteLine($"{conv.Id}: {conv.StartedAt}");
}
```

## Requirements

- .NET 10.0+
- GitHub Copilot CLI with session store enabled

## License

MIT License — see [LICENSE](https://github.com/dezgit2025/auto-memory/blob/main/LICENSE)
