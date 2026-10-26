using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record BoonDamageRecord(long Target, string? Faction, WorldPosition Position);
public sealed record BoonPairKey(long Owner, string Boon, long First, long Second);
public sealed record BoonSequenceKey(long Owner, string Boon, long Sequence);
public sealed record BoonGroupValue(long? Target = null, string? Boon = null);
public sealed record BoonPairCandidate(BoonPairKey Key, BoonDamageRecord Prior);
public sealed class BoonDistinctGroup
{
    public long Owner { get; init; }
    public required string Boon { get; init; }
    // Null and explicit TargetId are distinct authored groups in the original rules.
    public BoonEventField? Field { get; init; }
    public HashSet<BoonGroupValue> Seen { get; init; } = [];
    public int Count { get; set; }
}

/// <summary>Finite bookkeeping for one causal chain, retained only while its delayed work/statuses exist.</summary>
public sealed class BoonChainHistory
{
    public List<BoonDamageRecord> Damages { get; init; } = [];
    public HashSet<BoonPairKey> Pairs { get; init; } = [];
    public HashSet<BoonSequenceKey> Actions { get; init; } = [];
    public HashSet<BoonSequenceKey> Windups { get; init; } = [];
    public List<BoonDistinctGroup> Groups { get; init; } = [];

    public IReadOnlyList<BoonPairCandidate> Candidates(BoonActorView owner, string boon, BoonEvent e, BoonNeighborhood neighborhood)
    {
        if (e.TargetId is not { } target || e.Position is not { } at) return [];
        var matches = new List<BoonPairCandidate>();
        foreach (var prior in Damages)
        {
            int dx = Math.Abs(prior.Position.X - at.X), dy = Math.Abs(prior.Position.Y - at.Y);
            // Preserve the original predicate's OR: cardinal also admits diagonal pairs.
            bool adjacent = neighborhood == BoonNeighborhood.Cardinal && dx + dy == 1 || Math.Max(dx, dy) == 1;
            if (prior.Target == target || prior.Position.Floor != at.Floor || prior.Faction is null
                || prior.Faction == owner.Faction || !adjacent) continue;
            var key = new BoonPairKey(owner.Actor.Id, boon, Math.Min(prior.Target, target), Math.Max(prior.Target, target));
            if (!Pairs.Contains(key)) matches.Add(new(key, prior));
        }
        return matches;
    }

    public bool Distinct(long owner, string boon, BoonEventField? field, BoonEvent e, int size)
    {
        BoonGroupValue? value = field == BoonEventField.BoonId
            ? e.BoonId is { } id ? new(Boon: id) : null
            : e.TargetId is { } target ? new(Target: target) : null;
        if (value is null) return false;
        var group = Groups.FirstOrDefault(g => g.Owner == owner && g.Boon == boon && g.Field == field);
        if (group is null) { group = new() { Owner = owner, Boon = boon, Field = field }; Groups.Add(group); }
        if (!group.Seen.Add(value)) return false;
        group.Count++;
        if (group.Count < size) return false;
        group.Count = 0; // Seen identities persist for the entire chain, including after a group fires.
        return true;
    }

    public void RecordDamage(BoonEvent e)
    {
        if (e.Kind == BoonTrigger.DamageTaken && e.ActualLoss > 0 && e.TargetId is { } target && e.Position is { } at)
            Damages.Add(new(target, e.TargetFaction, at));
    }
}
