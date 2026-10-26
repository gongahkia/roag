using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>References that must survive bounded world corpse/history reclamation.</summary>
public static class TacticalReferences
{
    public static bool RetainsActor(WorldState world, long id)
    {
        if (world.Tactics is not { } state) return false;
        if (state.ActiveActor == id || state.ControlledActor == id || state.Queue.Snapshot().Pending.Any(w => w.ActorId == id)
            || state.Attacks.Any(a => a.Source == id || a.IntendedTarget == id || a.Causality?.Ancestry.Any(k => k.Owner == id) == true)
            || state.Events.Any(e => e.Source == id || e.Target == id)) return true;
        bool Activation(BoonActivation a) => a.OwnerId == id || a.PairTargetId == id || a.Event.SourceId == id || a.Event.TargetId == id
            || a.Ancestry.Any(k => k.Owner == id);
        if (state.BoonEffects.Echoes.Values.Any(e => Activation(e.Activation))) return true;
        if (state.Loot.Drops.Any(d => d.DefeatedActor == id || d.CarriedBy == id)) return true;
        if (state.Rewards.Enemies.Any(p => p.Key == id || p.Value.Members.ContainsKey(id) || p.Value.UnhealedDamage.ContainsKey(id))) return true;
        foreach (var actor in world.Actors)
        {
            if (actor.Tactics is not { } tactics) continue;
            if (tactics.SnaredBy == id || tactics.Burning is { } burn && Activation(burn.Activation)) return true;
            var resources = tactics.Resources;
            if (actor.Id == id && (resources.Snare != null || resources.Decoy != null || resources.Spirit != null
                || resources.Beacon != null || resources.Bulwark != null || resources.Sentry is { Phase: not SentryPhase.Spent })) return true;
        }
        foreach (var history in state.BoonEffects.Dispatch.Chains.Values)
            if (history.Damages.Any(d => d.Target == id) || history.Pairs.Any(p => p.Owner == id || p.First == id || p.Second == id)
                || history.Actions.Any(a => a.Owner == id) || history.Windups.Any(a => a.Owner == id)
                || history.Groups.Any(g => g.Owner == id || g.Seen.Any(v => v.Target == id))) return true;
        return false;
    }
}
