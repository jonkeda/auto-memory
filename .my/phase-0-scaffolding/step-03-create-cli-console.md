# Step 03 — Create AutoMemory.Cli console

## Goal
Create the console entry-point project that will produce the `session-recall` binary.

## Commands
```powershell
dotnet new console -o net/src/AutoMemory.Cli
```

## Csproj edits
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <AssemblyName>session-recall</AssemblyName>
  <RootNamespace>AutoMemory.Cli</RootNamespace>
</PropertyGroup>
```

## Done when
- [ ] Project compiles `Program.cs` placeholder.
- [ ] `dotnet run --project net/src/AutoMemory.Cli` prints "Hello, World!".
- [ ] Output binary named `session-recall(.exe)`.
