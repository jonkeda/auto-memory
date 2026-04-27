using System.Globalization;

namespace AutoMemory.Core.Health.Flat;

public sealed class DimFlatCorpus : IFlatHealthDimension
{
    public string Name => "Corpus Size";

    public FlatDimResult Score(FlatHealthContext ctx)
    {
        var (score, zone) = ctx.TotalSessions switch
        {
            >= 100 => (10.0, "GREEN"),
            >= 50  => (8.0,  "GREEN"),
            >= 20  => (6.0,  "AMBER"),
            >= 5   => (4.0,  "AMBER"),
            _      => (2.0,  "RED")
        };

        var detail = string.Format(
            CultureInfo.InvariantCulture,
            "{0} sessions ({1} state + {2} chat)",
            ctx.TotalSessions,
            ctx.SessionStateSessions,
            ctx.ChatSessions
        );

        return new FlatDimResult(Name, score, zone, detail, string.Empty);
    }
}
