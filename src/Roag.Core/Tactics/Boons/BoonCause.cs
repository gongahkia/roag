using System.Collections.Immutable;

namespace Roag.Core.Tactics;

/// <summary>Finite causal lineage carried by immediate work and committed attacks.</summary>
public sealed record BoonCause(long ChainId, long? RootActionId, ImmutableArray<BoonActivationKey> Ancestry,
    double ProcCoefficient = 1, bool ClassGenerated = false)
{
    public static BoonCause From(BoonActivation activation, double coefficient = 1) =>
        new(activation.ChainId, activation.RootActionId, activation.Ancestry, coefficient);
    public static BoonCause From(BoonEvent e) => new(e.ChainId, e.RootActionId, e.Ancestry, e.ProcCoefficient, e.ClassGenerated);
}
