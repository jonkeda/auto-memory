using System.Globalization;
using System.Text;

namespace AutoMemory.Cli;

/// <summary>
/// Hand-rolled argument parser. Zero deps.
/// </summary>
public sealed class ArgParser
{
    private readonly string _programName;
    private readonly string _description;
    private readonly List<OptionDef> _options = [];
    private readonly List<PositionalDef> _positionals = [];

    public ArgParser(string programName, string description = "")
    {
        _programName = programName;
        _description = description;
    }

    public ArgParser AddOption(string name, string? shortName = null, bool isFlag = false, string? defaultValue = null, string? help = null)
    {
        _options.Add(new OptionDef
        {
            Name = name,
            ShortName = shortName,
            IsFlag = isFlag,
            DefaultValue = defaultValue,
            Help = help
        });
        return this;
    }

    public ArgParser AddPositional(string name, bool required = true, string? help = null)
    {
        _positionals.Add(new PositionalDef
        {
            Name = name,
            Required = required,
            Help = help
        });
        return this;
    }

    public ParsedArgs Parse(string[] argv)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var positionalValues = new List<string>();
        var i = 0;

        // Initialize defaults
        foreach (var opt in _options)
        {
            if (opt.DefaultValue is not null)
            {
                values[opt.Name] = opt.DefaultValue;
            }
            else if (opt.IsFlag)
            {
                values[opt.Name] = "false";
            }
        }

        // Parse arguments
        while (i < argv.Length)
        {
            var arg = argv[i];

            // Check for help
            if (arg is "-h" or "--help")
            {
                throw new HelpRequestedException(GetHelpText());
            }

            // Long option: --name=value or --name value
            if (arg.StartsWith('-') && arg.Length > 1 && arg[1] == '-')
            {
                var eqIdx = arg.IndexOf('=', StringComparison.Ordinal);
                string optName;
                string? optValue = null;

                if (eqIdx > 0)
                {
                    // --name=value
                    optName = arg.Substring(2, eqIdx - 2);
                    optValue = arg.Substring(eqIdx + 1);
                }
                else
                {
                    // --name value
                    optName = arg.Substring(2);
                }

                var optDef = _options.FirstOrDefault(o => 
                    string.Equals(o.Name, optName, StringComparison.OrdinalIgnoreCase));

                if (optDef is null)
                {
                    throw new UsageException($"unrecognized argument: --{optName}");
                }

                if (optDef.IsFlag)
                {
                    values[optDef.Name] = "true";
                }
                else
                {
                    if (optValue is null)
                    {
                        // Need next arg as value
                        if (i + 1 >= argv.Length)
                        {
                            throw new UsageException($"argument --{optName}: expected one argument");
                        }
                        optValue = argv[++i];
                    }
                    values[optDef.Name] = optValue;
                }

                i++;
            }
            // Short option: -x
            else if (arg.StartsWith('-') && arg.Length > 1 && arg[1] != '-')
            {
                var shortName = arg.Substring(1);
                var optDef = _options.FirstOrDefault(o => 
                    string.Equals(o.ShortName, shortName, StringComparison.OrdinalIgnoreCase));

                if (optDef is null)
                {
                    throw new UsageException($"unrecognized argument: -{shortName}");
                }

                if (optDef.IsFlag)
                {
                    values[optDef.Name] = "true";
                }
                else
                {
                    if (i + 1 >= argv.Length)
                    {
                        throw new UsageException($"argument -{shortName}: expected one argument");
                    }
                    values[optDef.Name] = argv[++i];
                }

                i++;
            }
            else
            {
                // Positional argument
                positionalValues.Add(arg);
                i++;
            }
        }

        // Validate positionals
        var requiredPositionals = _positionals.Where(p => p.Required).ToList();
        if (positionalValues.Count < requiredPositionals.Count)
        {
            var missing = requiredPositionals[positionalValues.Count];
            throw new UsageException($"the following arguments are required: {missing.Name}");
        }

        return new ParsedArgs(values, positionalValues);
    }

    private string GetHelpText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"usage: {_programName} [options]");
        if (!string.IsNullOrEmpty(_description))
        {
            sb.AppendLine();
            sb.AppendLine(_description);
        }

        if (_options.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("options:");
            foreach (var opt in _options)
            {
                var names = new List<string>();
                if (opt.ShortName is not null)
                {
                    names.Add($"-{opt.ShortName}");
                }
                names.Add($"--{opt.Name}");

                var line = $"  {string.Join(", ", names)}";
                if (!string.IsNullOrEmpty(opt.Help))
                {
                    line += $"  {opt.Help}";
                }
                sb.AppendLine(line);
            }
        }

        return sb.ToString().TrimEnd();
    }

    private sealed class OptionDef
    {
        public required string Name { get; init; }
        public string? ShortName { get; init; }
        public bool IsFlag { get; init; }
        public string? DefaultValue { get; init; }
        public string? Help { get; init; }
    }

    private sealed class PositionalDef
    {
        public required string Name { get; init; }
        public bool Required { get; init; }
        public string? Help { get; init; }
    }
}

/// <summary>
/// Parsed arguments.
/// </summary>
public sealed class ParsedArgs
{
    private readonly Dictionary<string, string?> _values;
    private readonly List<string> _positionals;

    internal ParsedArgs(Dictionary<string, string?> values, List<string> positionals)
    {
        _values = values;
        _positionals = positionals;
    }

    /// <summary>
    /// Default constructor for testing.
    /// </summary>
    public ParsedArgs()
    {
        _values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        _positionals = [];
    }

    public bool GetFlag(string name)
    {
        return _values.TryGetValue(name, out var val) && 
               string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
    }

    public string? GetOption(string name)
    {
        _values.TryGetValue(name, out var val);
        return val;
    }

    public int? GetInt(string name)
    {
        if (!_values.TryGetValue(name, out var val) || val is null)
        {
            return null;
        }

        if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new UsageException($"argument --{name}: invalid int value: '{val}'");
        }

        if (result < 0)
        {
            throw new UsageException($"argument --{name}: must be >= 0, got {result}");
        }

        return result;
    }

    public string? GetPositional(int index)
    {
        return index < _positionals.Count ? _positionals[index] : null;
    }

    public IReadOnlyList<string> Positionals => _positionals;

    /// <summary>
    /// Set an option value (for testing).
    /// </summary>
    public void SetOption(string name, string value)
    {
        _values[name] = value;
    }

    /// <summary>
    /// Set a positional argument (for testing).
    /// </summary>
    public void SetPositional(int index, string value)
    {
        while (_positionals.Count <= index)
        {
            _positionals.Add(string.Empty);
        }
        _positionals[index] = value;
    }
}

/// <summary>
/// Usage error — caller should exit 2.
/// </summary>
public sealed class UsageException : Exception
{
    public UsageException(string message) : base(message) { }
}

/// <summary>
/// Help requested — caller should print help and exit 0.
/// </summary>
public sealed class HelpRequestedException : Exception
{
    public string HelpText { get; }

    public HelpRequestedException(string helpText) : base("Help requested")
    {
        HelpText = helpText;
    }
}
