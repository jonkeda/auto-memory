using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace AutoMemory.Parity;

/// <summary>
/// Parity runner: executes test cases against both Python and .NET CLIs,
/// validates output equality modulo whitelisted fields.
/// </summary>
public sealed class ParityRunner
{
    private readonly ITestOutputHelper _output;

    // Paths relative to test assembly location
    private static readonly string s_repoRoot = Path.GetFullPath(
        Path.Combine(
            Path.GetDirectoryName(typeof(ParityRunner).Assembly.Location)!,
            "..", "..", "..", "..", "..", ".."));

    private static readonly string s_casesDir = Path.Combine(s_repoRoot, "net", "tests", "Parity", "cases");
    private static readonly string s_fixtureDb = Path.Combine(s_repoRoot, "net", "tests", "Parity", "fixture.sqlite");
    private static readonly string s_fixtureTelemetry = Path.Combine(s_repoRoot, "net", "tests", "Parity", "fixture-telemetry.json");

    // CLI paths
    private static readonly string s_pythonExe = "python";
    private static readonly string s_dotnetCli = OperatingSystem.IsWindows()
        ? Path.Combine(s_repoRoot, "net", "publish-test-win", "session-recall.exe")
        : Path.Combine(s_repoRoot, "net", "publish-test", "session-recall");

    public ParityRunner(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Discover all case files under net/tests/Parity/cases/*.case
    /// </summary>
    public static IEnumerable<object[]> GetCaseFiles()
    {
        if (!Directory.Exists(s_casesDir))
        {
            // Return empty if cases directory doesn't exist yet
            return Enumerable.Empty<object[]>();
        }

        return Directory.GetFiles(s_casesDir, "*.case")
            .Select(p => new object[] { Path.GetFileName(p) });
    }

    [Theory]
    [MemberData(nameof(GetCaseFiles))]
    public void RunParityCase(string caseFileName)
    {
        var casePath = Path.Combine(s_casesDir, caseFileName);
        _output.WriteLine($"Running parity case: {caseFileName}");

        // 1. Parse case file
        var testCase = ParseCaseFile(casePath);
        _output.WriteLine($"  ARGV: {testCase.Argv}");
        _output.WriteLine($"  Expected exit code: {testCase.ExpectedExitCode}");

        // 2. Ensure fixture DB exists
        var dbPath = testCase.DbPath is not null 
            ? Path.Combine(s_repoRoot, "net", "tests", "Parity", testCase.DbPath)
            : s_fixtureDb;
            
        if (!File.Exists(dbPath))
        {
            Assert.Fail($"Fixture DB not found: {dbPath}\nRun: python net/tests/Parity/seed.py {s_fixtureDb}");
        }

        // 3. Run Python CLI
        var pythonResult = RunCli(s_pythonExe, $"-m session_recall {testCase.Argv}", dbPath);
        _output.WriteLine($"Python exit code: {pythonResult.ExitCode}");

        // 4. Run .NET CLI
        var dotnetResult = RunCli(s_dotnetCli, testCase.Argv, dbPath);
        _output.WriteLine($".NET exit code: {dotnetResult.ExitCode}");

        // 5. Assert exit codes match
        Assert.Equal(pythonResult.ExitCode, dotnetResult.ExitCode);

        // 6. Validate stdout
        if (testCase.StdoutContent is not null)
        {
            // Inline expected stdout - compare against exact content
            var expectedStdout = ApplyRedactions(testCase.StdoutContent);
            var pythonStdout = ApplyRedactions(pythonResult.Stdout);
            var dotnetStdout = ApplyRedactions(dotnetResult.Stdout);
            
            if (pythonStdout != expectedStdout)
            {
                _output.WriteLine("=== PYTHON STDOUT MISMATCH ===");
                _output.WriteLine("Expected:");
                _output.WriteLine(testCase.StdoutContent);
                _output.WriteLine("\nActual:");
                _output.WriteLine(pythonResult.Stdout);
                Assert.Fail("Python stdout does not match expected");
            }
            
            if (dotnetStdout != expectedStdout)
            {
                _output.WriteLine("=== .NET STDOUT MISMATCH ===");
                _output.WriteLine("Expected:");
                _output.WriteLine(testCase.StdoutContent);
                _output.WriteLine("\nActual:");
                _output.WriteLine(dotnetResult.Stdout);
                Assert.Fail(".NET stdout does not match expected");
            }
        }
        else if (testCase.StdoutHash is not null)
        {
            // Hash-based comparison for large outputs
            var pythonStdout = ApplyRedactions(pythonResult.Stdout);
            var dotnetStdout = ApplyRedactions(dotnetResult.Stdout);
            
            var pythonHash = ComputeHash(pythonStdout);
            var dotnetHash = ComputeHash(dotnetStdout);
            var expectedHash = testCase.StdoutHash.ToLowerInvariant();
            
            Assert.Equal(expectedHash, pythonHash.ToLowerInvariant());
            Assert.Equal(expectedHash, dotnetHash.ToLowerInvariant());
        }
        else
        {
            // Default: compare Python and .NET outputs directly
            var pythonStdout = ApplyRedactions(pythonResult.Stdout);
            var dotnetStdout = ApplyRedactions(dotnetResult.Stdout);

            if (pythonStdout != dotnetStdout)
            {
                _output.WriteLine("=== STDOUT MISMATCH ===");
                _output.WriteLine("Python stdout:");
                _output.WriteLine(pythonResult.Stdout);
                _output.WriteLine("\n.NET stdout:");
                _output.WriteLine(dotnetResult.Stdout);
                _output.WriteLine("\n=== DIFF ===");
                ShowDiff(pythonStdout, dotnetStdout);

                Assert.Fail("Stdout mismatch between Python and .NET");
            }
        }

        // 7. Assert stderr matches pattern
        if (!string.IsNullOrEmpty(testCase.StderrRegex))
        {
            var pythonMatches = Regex.IsMatch(pythonResult.Stderr, testCase.StderrRegex);
            var dotnetMatches = Regex.IsMatch(dotnetResult.Stderr, testCase.StderrRegex);

            if (!pythonMatches)
            {
                _output.WriteLine($"Python stderr did not match pattern: {testCase.StderrRegex}");
                _output.WriteLine($"Actual stderr: {pythonResult.Stderr}");
            }

            if (!dotnetMatches)
            {
                _output.WriteLine($".NET stderr did not match pattern: {testCase.StderrRegex}");
                _output.WriteLine($"Actual stderr: {dotnetResult.Stderr}");
            }

            Assert.True(pythonMatches && dotnetMatches, "Stderr pattern mismatch");
        }

        _output.WriteLine("✓ Parity check passed");
    }

    internal sealed record TestCase(
        string Argv,
        int ExpectedExitCode,
        int Tier,
        string? StderrRegex,
        string? StdoutHash,
        string? StdoutContent,
        string? DbPath);

    internal static TestCase ParseCaseFile(string path)
    {
        var lines = File.ReadAllLines(path);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? stdoutContent = null;
        
        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i];
            
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                i++;
                continue;
            }
            
            // Check for STDOUT: multi-line block
            if (line.TrimStart().StartsWith("STDOUT:", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                i++; // Move to next line
                
                // Collect lines until END_STDOUT
                while (i < lines.Length)
                {
                    if (lines[i].TrimStart().Equals("END_STDOUT", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    sb.AppendLine(lines[i]);
                    i++;
                }
                
                // Remove trailing newline added by AppendLine
                stdoutContent = sb.Length > 0 ? sb.ToString().TrimEnd('\r', '\n') : "";
                i++; // Skip END_STDOUT line
                continue;
            }
            
            // Parse key: value
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim();
                dict[key] = value;
            }
            
            i++;
        }

        return new TestCase(
            Argv: dict.TryGetValue("ARGV", out var argv) ? argv : "",
            ExpectedExitCode: dict.TryGetValue("EXIT", out var exit) 
                ? int.Parse(exit, CultureInfo.InvariantCulture) 
                : 0,
            Tier: dict.TryGetValue("TIER", out var tier)
                ? int.Parse(tier, CultureInfo.InvariantCulture)
                : 1,
            StderrRegex: dict.TryGetValue("STDERR_REGEX", out var stderrRegex) ? stderrRegex : null,
            StdoutHash: dict.TryGetValue("STDOUT_HASH", out var stdoutHash) ? stdoutHash : null,
            StdoutContent: stdoutContent,
            DbPath: dict.TryGetValue("DB", out var db) ? db : null);
    }

    private sealed record CliResult(int ExitCode, string Stdout, string Stderr);

    private static CliResult RunCli(string executable, string arguments, string dbPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Set DB and telemetry paths via environment variables
        startInfo.Environment["SESSION_RECALL_DB"] = dbPath;
        startInfo.Environment["SESSION_RECALL_TELEMETRY"] = s_fixtureTelemetry;
        
        // Set Python IO encoding to UTF-8 to handle Unicode characters (emoji icons)
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start process: {executable}");
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new CliResult(process.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Apply whitelist redactions: replace timestamps, durations, etc.
    /// </summary>
    private static string ApplyRedactions(string text)
    {
        // Normalize JSON encoding differences: \u002B → +, \u0027 → ', etc.
        // System.Text.Json escapes more characters than Python's json.dumps by default
        text = Regex.Replace(text, @"\\u002[Bb]", "+");    // Plus sign
        text = Regex.Replace(text, @"\\u0027", "'");        // Single quote
        text = Regex.Replace(text, @"\\u0060", "`");        // Backtick

        // Redact ISO timestamps: 2024-01-01T00:00:00.123456Z → <TIMESTAMP>
        text = Regex.Replace(text, @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z?", "<TIMESTAMP>");

        // Redact duration_ms: "duration_ms": 123 → "duration_ms": <DURATION>
        text = Regex.Replace(text, @"""duration_ms"":\s*\d+", @"""duration_ms"": <DURATION>");

        // Redact standalone duration fields in plain text (e.g., "123 ms")
        text = Regex.Replace(text, @"\b\d+\s*ms\b", "<DURATION> ms");

        // Redact telemetry counts in health output that change on each run
        // Examples: "n=85" → "n=<COUNT>", "meta=30" → "meta=<COUNT>", "unknown=2" → "unknown=<COUNT>"
        text = Regex.Replace(text, @"\bn=\d+\b", "n=<COUNT>");
        text = Regex.Replace(text, @"\bmeta=\d+\b", "meta=<COUNT>");
        text = Regex.Replace(text, @"\bunknown=\d+\b", "unknown=<COUNT>");
        
        // Redact Progressive Disclosure percentages that vary with entry counts
        // Examples: "T1=54%" → "T1=<PCT>%", "T2=15%" → "T2=<PCT>%"
        text = Regex.Replace(text, @"\bT1=\d+%", "T1=<PCT>%");
        text = Regex.Replace(text, @"\bT2=\d+%", "T2=<PCT>%");
        text = Regex.Replace(text, @"\bT3=\d+%", "T3=<PCT>%");
        
        // Redact Progressive Disclosure avg= and esc_rate= that vary with entry distribution
        // Examples: "avg=1.78" → "avg=<AVG>", "esc_rate=95%" → "esc_rate=<PCT>%"
        text = Regex.Replace(text, @"\bavg=\d+\.\d+\b", "avg=<AVG>");
        text = Regex.Replace(text, @"\besc_rate=\d+%", "esc_rate=<PCT>%");
        
        // Redact meta_entries, unknown_entries, and scored_entries JSON fields (Progressive Disclosure dimension)
        // Example: "meta_entries": 37 → "meta_entries": <COUNT>
        text = Regex.Replace(text, @"""meta_entries"":\s*\d+", @"""meta_entries"": <COUNT>");
        text = Regex.Replace(text, @"""unknown_entries"":\s*\d+", @"""unknown_entries"": <COUNT>");
        text = Regex.Replace(text, @"""scored_entries"":\s*\d+", @"""scored_entries"": <COUNT>");

        // Normalize whole number scores: Python inconsistently outputs some as "10" and others as "10.0"
        // Convert all whole number scores to integer format for consistency
        text = Regex.Replace(text, @"""score"":\s*(\d+)\.0\b", @"""score"": $1");

        return text;
    }

    private static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private void ShowDiff(string python, string dotnet)
    {
        var pythonLines = python.Split('\n');
        var dotnetLines = dotnet.Split('\n');

        var maxLines = Math.Max(pythonLines.Length, dotnetLines.Length);
        for (int i = 0; i < maxLines && i < 20; i++) // Show first 20 lines of diff
        {
            var pyLine = i < pythonLines.Length ? pythonLines[i] : "<missing>";
            var dotnetLine = i < dotnetLines.Length ? dotnetLines[i] : "<missing>";

            if (pyLine != dotnetLine)
            {
                _output.WriteLine($"Line {i + 1}:");
                _output.WriteLine($"  Python: {pyLine}");
                _output.WriteLine($"  .NET:   {dotnetLine}");
            }
        }

        if (maxLines > 20)
        {
            _output.WriteLine($"... ({maxLines - 20} more lines)");
        }
    }
}
