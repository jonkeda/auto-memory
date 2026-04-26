using System.Globalization;
using AutoMemory.Cli;
using Xunit;

namespace AutoMemory.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class ArgParserTests
{
    [Fact]
    public void Parse_LongOption_EqualsStyle()
    {
        var parser = new ArgParser("test")
            .AddOption("repo", defaultValue: null);

        var args = parser.Parse(["--repo=myrepo"]);

        Assert.Equal("myrepo", args.GetOption("repo"));
    }

    [Fact]
    public void Parse_LongOption_SpaceStyle()
    {
        var parser = new ArgParser("test")
            .AddOption("repo", defaultValue: null);

        var args = parser.Parse(["--repo", "myrepo"]);

        Assert.Equal("myrepo", args.GetOption("repo"));
    }

    [Fact]
    public void Parse_BooleanFlag_True()
    {
        var parser = new ArgParser("test")
            .AddOption("json", isFlag: true);

        var args = parser.Parse(["--json"]);

        Assert.True(args.GetFlag("json"));
    }

    [Fact]
    public void Parse_BooleanFlag_False_WhenNotProvided()
    {
        var parser = new ArgParser("test")
            .AddOption("json", isFlag: true);

        var args = parser.Parse([]);

        Assert.False(args.GetFlag("json"));
    }

    [Fact]
    public void Parse_ShortOption_Flag()
    {
        var parser = new ArgParser("test")
            .AddOption("json", shortName: "j", isFlag: true);

        var args = parser.Parse(["-j"]);

        Assert.True(args.GetFlag("json"));
    }

    [Fact]
    public void Parse_ShortOption_WithValue()
    {
        var parser = new ArgParser("test")
            .AddOption("repo", shortName: "r", defaultValue: null);

        var args = parser.Parse(["-r", "myrepo"]);

        Assert.Equal("myrepo", args.GetOption("repo"));
    }

    [Fact]
    public void Parse_PositionalArgs()
    {
        var parser = new ArgParser("test")
            .AddPositional("query", required: true);

        var args = parser.Parse(["search-term"]);

        Assert.Equal("search-term", args.GetPositional(0));
    }

    [Fact]
    public void Parse_MixedOptionsAndPositionals()
    {
        var parser = new ArgParser("test")
            .AddOption("json", isFlag: true)
            .AddOption("limit", defaultValue: null)
            .AddPositional("query", required: true);

        var args = parser.Parse(["--json", "--limit=5", "search-term"]);

        Assert.True(args.GetFlag("json"));
        Assert.Equal("5", args.GetOption("limit"));
        Assert.Equal("search-term", args.GetPositional(0));
    }

    [Fact]
    public void Parse_UnknownFlag_ThrowsUsageException()
    {
        var parser = new ArgParser("test")
            .AddOption("json", isFlag: true);

        var ex = Assert.Throws<UsageException>(() =>
            parser.Parse(["--unknown"]));

        Assert.Contains("unrecognized argument: --unknown", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_UnknownShortFlag_ThrowsUsageException()
    {
        var parser = new ArgParser("test")
            .AddOption("json", shortName: "j", isFlag: true);

        var ex = Assert.Throws<UsageException>(() =>
            parser.Parse(["-x"]));

        Assert.Contains("unrecognized argument: -x", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_MissingRequiredPositional_ThrowsUsageException()
    {
        var parser = new ArgParser("test")
            .AddPositional("query", required: true);

        var ex = Assert.Throws<UsageException>(() =>
            parser.Parse([]));

        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_MissingOptionValue_ThrowsUsageException()
    {
        var parser = new ArgParser("test")
            .AddOption("repo", defaultValue: null);

        var ex = Assert.Throws<UsageException>(() =>
            parser.Parse(["--repo"]));

        Assert.Contains("expected one argument", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetInt_ValidPositive_ReturnsValue()
    {
        var parser = new ArgParser("test")
            .AddOption("limit", defaultValue: null);

        var args = parser.Parse(["--limit=10"]);

        Assert.Equal(10, args.GetInt("limit"));
    }

    [Fact]
    public void GetInt_Zero_ReturnsZero()
    {
        var parser = new ArgParser("test")
            .AddOption("limit", defaultValue: null);

        var args = parser.Parse(["--limit=0"]);

        Assert.Equal(0, args.GetInt("limit"));
    }

    [Fact]
    public void GetInt_Negative_ThrowsUsageException()
    {
        var parser = new ArgParser("test")
            .AddOption("limit", defaultValue: null);

        var args = parser.Parse(["--limit=-5"]);

        var ex = Assert.Throws<UsageException>(() => args.GetInt("limit"));

        Assert.Contains("must be >= 0", ex.Message, StringComparison.Ordinal);
        Assert.Contains("-5", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetInt_InvalidFormat_ThrowsUsageException()
    {
        var parser = new ArgParser("test")
            .AddOption("limit", defaultValue: null);

        var args = parser.Parse(["--limit=abc"]);

        var ex = Assert.Throws<UsageException>(() => args.GetInt("limit"));

        Assert.Contains("invalid int value", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetInt_NotProvided_ReturnsNull()
    {
        var parser = new ArgParser("test")
            .AddOption("limit", defaultValue: null);

        var args = parser.Parse([]);

        Assert.Null(args.GetInt("limit"));
    }

    [Fact]
    public void Parse_DefaultValue_Applied()
    {
        var parser = new ArgParser("test")
            .AddOption("days", defaultValue: "30");

        var args = parser.Parse([]);

        Assert.Equal("30", args.GetOption("days"));
    }

    [Fact]
    public void Parse_DefaultValue_OverriddenByUser()
    {
        var parser = new ArgParser("test")
            .AddOption("days", defaultValue: "30");

        var args = parser.Parse(["--days=7"]);

        Assert.Equal("7", args.GetOption("days"));
    }

    [Fact]
    public void Parse_CaseInsensitive_OptionName()
    {
        var parser = new ArgParser("test")
            .AddOption("repo", defaultValue: null);

        var args = parser.Parse(["--REPO=test"]);

        Assert.Equal("test", args.GetOption("repo"));
        Assert.Equal("test", args.GetOption("REPO"));
        Assert.Equal("test", args.GetOption("Repo"));
    }

    [Fact]
    public void Parse_ShortHelp_ThrowsHelpRequestedException()
    {
        var parser = new ArgParser("test", "Test command")
            .AddOption("json", isFlag: true);

        var ex = Assert.Throws<HelpRequestedException>(() =>
            parser.Parse(["-h"]));

        Assert.Contains("usage: test", ex.HelpText, StringComparison.Ordinal);
        Assert.Contains("Test command", ex.HelpText, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_LongHelp_ThrowsHelpRequestedException()
    {
        var parser = new ArgParser("test", "Test command")
            .AddOption("json", isFlag: true);

        var ex = Assert.Throws<HelpRequestedException>(() =>
            parser.Parse(["--help"]));

        Assert.Contains("usage: test", ex.HelpText, StringComparison.Ordinal);
        Assert.Contains("Test command", ex.HelpText, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Help_ShortCircuits_BeforeOtherParsing()
    {
        var parser = new ArgParser("test")
            .AddOption("json", isFlag: true);

        // Help should be triggered even with other valid args present
        var ex = Assert.Throws<HelpRequestedException>(() =>
            parser.Parse(["-h", "--json"]));

        Assert.Contains("usage: test", ex.HelpText, StringComparison.Ordinal);
    }
}
