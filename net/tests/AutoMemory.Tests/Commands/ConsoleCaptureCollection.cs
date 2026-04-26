using Xunit;

namespace AutoMemory.Tests.Commands;

/// <summary>
/// Collection definition to prevent parallel execution of tests that capture Console output.
/// </summary>
[CollectionDefinition("Console Capture")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not end in Collection")]
public class ConsoleCaptureCollection
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
