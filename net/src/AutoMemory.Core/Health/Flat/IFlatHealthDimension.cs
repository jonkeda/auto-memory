namespace AutoMemory.Core.Health.Flat;

public interface IFlatHealthDimension
{
    string Name { get; }
    FlatDimResult Score(FlatHealthContext ctx);
}

public sealed record FlatDimResult(
    string Name,
    double? Score,   // null = CALIBRATING
    string Zone,     // GREEN / AMBER / RED / CALIBRATING
    string Detail,
    string Hint
);
