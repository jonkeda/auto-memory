using System;
using System.Globalization;

namespace AutoMemory.Core.Health.Flat;

public sealed class DimFlatFreshness : IFlatHealthDimension
{
    public string Name => "Freshness";

    public FlatDimResult Score(FlatHealthContext ctx)
    {
        if (ctx.NewestUpdatedAt is null)
        {
            return new FlatDimResult(Name, null, "CALIBRATING", "No data", string.Empty);
        }

        var age = DateTimeOffset.UtcNow - ctx.NewestUpdatedAt.Value;
        var (score, zone) = age.TotalHours switch
        {
            < 1  => (10.0, "GREEN"),
            < 6  => (9.0,  "GREEN"),
            < 24 => (8.0,  "GREEN"),
            var h when h < 72  => (6.0, "AMBER"),  // < 3d
            var h when h < 168 => (4.0, "AMBER"),  // < 7d
            _    => (2.0, "RED")
        };

        var detail = age.TotalHours < 24
            ? $"{age.TotalHours.ToString("0.0", CultureInfo.InvariantCulture)}h ago"
            : $"{Math.Floor(age.TotalDays).ToString(CultureInfo.InvariantCulture)}d ago";

        return new FlatDimResult(Name, score, zone, detail, string.Empty);
    }
}
