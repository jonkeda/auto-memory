using System.Globalization;

namespace AutoMemory.Core.Health.Flat;

public sealed class DimFlatRecentActivity : IFlatHealthDimension
{
    public string Name => "Recent Activity";

    public FlatDimResult Score(FlatHealthContext ctx)
    {
        var (score, zone) = ctx.RecentCount switch
        {
            >= 20 => (10.0, "GREEN"),
            >= 10 => (8.0,  "GREEN"),
            >= 5  => (6.0,  "AMBER"),
            >= 1  => (4.0,  "AMBER"),
            _     => (2.0,  "RED")
        };

        var detail = string.Format(
            CultureInfo.InvariantCulture,
            "{0} sessions (last 7d)",
            ctx.RecentCount
        );

        return new FlatDimResult(Name, score, zone, detail, string.Empty);
    }
}
