namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public bool CanHuntNativeWildlife(Actor actor) => NativeCreatureNeeds(actor) && Ecology(actor) != null;

    // Confirmed physical contact commits hostility before damage/contribution
    // settlement. It grants no observation of the attacker's hidden position.
    internal bool ProvokeNativeCreature(Actor source, Actor target)
    {
        if (State.NativeCampaign == null || !State.Party.Contains(source.Id) || State.Party.Contains(target.Id)
            || target is not { Life: LifeState.Alive, Kind: ActorKind.Beast or ActorKind.Raider, Tactics.Enemy.Retained: not null }) return false;
        bool changed = false;
        if (SituationForActor(target) is { Outcome: "" or "tunnel", Engaged: false } discovery)
        { discovery.Engaged = true; changed = true; }
        if (Ecology(target) != null)
        {
            changed |= target.ProvokedUntil <= State.Tick;
            target.ProvokedUntil = State.Tick + 75;
        }
        return changed;
    }
}
