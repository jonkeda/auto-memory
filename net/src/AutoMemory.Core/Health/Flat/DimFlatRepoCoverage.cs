using System.Globalization;

namespace AutoMemory.Core.Health.Flat;

public sealed class DimFlatRepoCoverage : IFlatHealthDimension
{
    public string Name => "Repo Coverage";

    public FlatDimResult Score(FlatHealthContext ctx)
    {
        if (ctx.CurrentRepo is null)
        {
            return new FlatDimResult(Name, null, "CALIBRATING", "no current repo", string.Empty);
        }

        var (score, zone) = ctx.RepoSessions switch
        {
            >= 10 => (10.0, "GREEN"),
            >= 5  => (8.0,  "GREEN"),
            >= 2  => (6.0,  "AMBER"),
            1     => (4.0,  "AMBER"),
            _     => (2.0,  "RED")
        };

        var detail = string.Format(
            CultureInfo.InvariantCulture,
            "{0} sessions for {1}",
            ctx.RepoSessions,
            ctx.CurrentRepo
        );

        return new FlatDimResult(Name, score, zone, detail, string.Empty);
    }
}
