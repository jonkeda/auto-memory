# Step 01 — Csproj publish profile

## Goal
Author the publish-friendly properties on `AutoMemory.Cli.csproj`.

## Snippet
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <AssemblyName>session-recall</AssemblyName>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>partial</TrimMode>
  <InvariantGlobalization>true</InvariantGlobalization>
  <DebugType>embedded</DebugType>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
</PropertyGroup>
```

## Done when
- [ ] `dotnet publish -c Release -r linux-x64` produces a single binary.
- [ ] Trim warnings reviewed; none blocking.
