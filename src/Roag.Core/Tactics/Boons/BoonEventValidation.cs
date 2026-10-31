namespace Roag.Core.Tactics;

/// <summary>Checks persisted causal state before it can consume RNG or enqueue work.</summary>
public static class BoonEventValidation
{
    public static void Validate(this BoonEvent e, BoonCatalog? catalog = null)
    {
        catalog ??= BoonCatalog.Default;
        if (e.Id <= 0 || e.ChainId <= 0 || !Enum.IsDefined(e.Kind) || e.RootActionId <= 0 || e.SourceId < 0 || e.TargetId <= 0
            || e.WindupId <= 0 || !double.IsFinite(e.ProcCoefficient) || e.ProcCoefficient is < 0 or > 10
            || e.DamageType != null && !Enum.IsDefined(e.DamageType.Value) || e.Status != null && !Enum.IsDefined(e.Status.Value)
            || e.HpBefore < 0 || e.TargetMaxHp < 0 || e.SourceHpAtAttack < 0 || e.SourceMaxHpAtAttack < 0
            || e.ActualLoss < 0 || e.Amount < 0 || e.ActualGain < 0 || e.Price < 0 || e.AdjacentHostileCount < 0 || e.AdjacentBurningCount < 0
            || e.ActualGain is { } gain && !double.IsFinite(gain)
            || e.OathGiven < 0 || e.OathReceived < 0
            || e.FateFace is < 1 or > 8 || e.Path.IsDefault || e.TargetCells.IsDefault
            || e.SourceTags.IsDefault || e.TargetTags.IsDefault || e.SourceTags.Any(string.IsNullOrWhiteSpace) || e.TargetTags.Any(string.IsNullOrWhiteSpace)
            || e.Ancestry.IsDefault || e.Ancestry.Any(k => k is null || k.Owner <= 0 || !catalog.Contains(k.Boon))
            || e.Ancestry.Distinct().Count() != e.Ancestry.Length)
            throw new InvalidDataException("Invalid boon event snapshot.");
    }

    public static void Validate(this BoonActivation activation, long nextActivationId, BoonCatalog? catalog = null)
    {
        catalog ??= BoonCatalog.Default;
        if (activation.Event is null) throw new InvalidDataException("Missing boon activation source event.");
        activation.Event.Validate(catalog);
        var e = activation.Event;
        if (activation.Id <= 0 || activation.Id >= nextActivationId || activation.OwnerId <= 0 || !catalog.Contains(activation.BoonId)
            || string.IsNullOrWhiteSpace(activation.SourceFaction) || activation.Power < 0 || activation.PairTargetId <= 0
            || activation.ChainId != e.ChainId || activation.RootActionId != e.RootActionId || activation.ParentEventId != e.Id
            || activation.Ancestry.IsDefault || !activation.Ancestry.SequenceEqual(e.Ancestry.Add(new(activation.OwnerId, activation.BoonId)))
            || e.Ancestry.Contains(new(activation.OwnerId, activation.BoonId)))
            throw new InvalidDataException("Invalid boon activation ancestry or identity.");
    }

    public static void Validate(this BoonDispatchState state, BoonCatalog? catalog = null)
    {
        catalog ??= BoonCatalog.Default;
        bool OwnerBoon(long owner, string boon) => owner > 0 && catalog.Contains(boon);
        if (state.NextActivationId <= 0 || state.Chains is null) throw new InvalidDataException("Missing boon dispatch state.");
        foreach (var (chain, history) in state.Chains)
        {
            if (chain <= 0 || history is null || history.Damages is null || history.Pairs is null || history.Actions is null || history.Windups is null || history.Groups is null)
                throw new InvalidDataException("Missing boon chain history.");
            if (history.Damages.Any(d => d is null || d.Target <= 0)
                || history.Pairs.Any(p => p is null || !OwnerBoon(p.Owner, p.Boon) || p.First <= 0 || p.Second <= p.First)
                || history.Actions.Any(a => a is null || !OwnerBoon(a.Owner, a.Boon) || a.Sequence <= 0)
                || history.Windups.Any(a => a is null || !OwnerBoon(a.Owner, a.Boon) || a.Sequence <= 0))
                throw new InvalidDataException("Invalid boon chain identities.");
            var groups = new HashSet<(long Owner, string Boon, BoonEventField? Field)>();
            foreach (var group in history.Groups)
            {
                if (group is null || !OwnerBoon(group.Owner, group.Boon) || group.Field is not (null or BoonEventField.TargetId or BoonEventField.BoonId)
                    || !groups.Add((group.Owner, group.Boon, group.Field)) || group.Seen is null || group.Count < 0 || group.Count > group.Seen.Count)
                    throw new InvalidDataException("Invalid distinct boon group.");
                foreach (var value in group.Seen)
                    if (value is null || (group.Field == BoonEventField.BoonId
                        ? string.IsNullOrWhiteSpace(value.Boon) || value.Target != null : value.Target is null or <= 0 || value.Boon != null))
                        throw new InvalidDataException("Invalid distinct boon group value.");
            }
        }
    }
}
