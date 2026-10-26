namespace Roag.Core.Tactics;

public sealed record BoonEcho(long Schedule, long Due, BoonActivation Activation, DamageElement Element, double ProcCoefficient);

/// <summary>One scheduled burn on the target actor; refreshing keeps its already committed tick and interval.</summary>
public sealed record BoonBurning(int Power, int Remaining, int Interval, long Due, long Schedule, BoonActivation Activation);

public sealed class BoonWorldEffects
{
    public BoonDispatchState Dispatch { get; set; } = new();
    public Dictionary<long, BoonEcho> Echoes { get; set; } = [];
}
