using System.Globalization;

namespace AutoMemory.Core.Health.Flat;

public sealed class DimFlatSummaryCoverage : IFlatHealthDimension
{
    public string Name => "Summary Coverage";

    public FlatDimResult Score(FlatHealthContext ctx)
    {
        if (ctx.TotalSessions == 0)
        {
            return new FlatDimResult(Name, null, "CALIBRATING", "0 sessions", string.Empty);
        }

        var pct = ctx.SummaryCount * 100.0 / ctx.TotalSessions;
        var (score, zone) = pct switch
        {
            >= 80 => (10.0, "GREEN"),
            >= 60 => (8.0,  "GREEN"),
            >= 40 => (6.0,  "AMBER"),
            >= 20 => (4.0,  "AMBER"),
            _     => (2.0,  "RED")
        };

        var detail = string.Format(
            CultureInfo.InvariantCulture,
            "{0}% ({1}/{2})",
            (int)pct,
            ctx.SummaryCount,
            ctx.TotalSessions
        );

        return new FlatDimResult(Name, score, zone, detail, string.Empty);
    }
}
