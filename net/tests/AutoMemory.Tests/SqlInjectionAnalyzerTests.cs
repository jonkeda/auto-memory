using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests;

/// <summary>
/// Tests that the SQL injection analyzer (AUTOMEM001) correctly flags risky patterns.
/// These tests verify the analyzer works by intentionally writing bad code that should fail to compile.
/// </summary>
public class SqlInjectionAnalyzerTests
{
    /// <summary>
    /// This method contains intentionally bad SQL patterns that should be caught by the analyzer.
    /// EXPECTED: This file should NOT compile if the analyzer is working correctly.
    /// To verify the analyzer works, temporarily uncomment the code and confirm it produces AUTOMEM001 errors.
    /// </summary>
    [Fact(Skip = "Demonstrates analyzer behavior - uncomment code to test")]
    public void AnalyzerDetectsStringInterpolation()
    {
        // Uncomment to test analyzer:
        // using var conn = new SqliteConnection("Data Source=:memory:");
        // conn.Open();
        // using var cmd = conn.CreateCommand();
        // string userInput = "malicious'; DROP TABLE users--";
        // cmd.CommandText = $"SELECT * FROM users WHERE name = '{userInput}'";  // Should trigger AUTOMEM001
        // cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// This method contains intentionally bad SQL concatenation that should be caught by the analyzer.
    /// EXPECTED: This file should NOT compile if the analyzer is working correctly.
    /// To verify the analyzer works, temporarily uncomment the code and confirm it produces AUTOMEM001 errors.
    /// </summary>
    [Fact(Skip = "Demonstrates analyzer behavior - uncomment code to test")]
    public void AnalyzerDetectsStringConcatenation()
    {
        // Uncomment to test analyzer:
        // using var conn = new SqliteConnection("Data Source=:memory:");
        // conn.Open();
        // using var cmd = conn.CreateCommand();
        // string tableName = "users";
        // cmd.CommandText = "SELECT * FROM " + tableName + " WHERE id = 1";  // Should trigger AUTOMEM001
        // cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Demonstrates the correct pattern: parameterized queries are safe and should NOT trigger the analyzer.
    /// </summary>
    [Fact]
    public void ParameterizedQueriesAreAllowed()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        
        // Safe: using parameters
        cmd.CommandText = "SELECT * FROM users WHERE name = @name";
        cmd.Parameters.AddWithValue("@name", "test");
        
        // This should compile without AUTOMEM001 errors
    }

    /// <summary>
    /// Demonstrates that constant concatenation is allowed.
    /// </summary>
    [Fact]
    public void ConstantConcatenationIsAllowed()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        
        // Safe: concatenating constants
        const string BaseQuery = "SELECT * FROM users";
        cmd.CommandText = BaseQuery + " WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", 1);
        
        // This should compile without AUTOMEM001 errors
    }
}
