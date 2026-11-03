namespace Roag.Core.World;

public sealed partial class WorldSession
{
    internal bool NativeCreatureNeeds(Actor actor) => State.NativeCampaign != null
        && actor is { Kind: ActorKind.Beast or ActorKind.Raider, Life: LifeState.Alive, Recruited: false, Tactics.Enemy.Retained: not null };

    internal bool NativeCreatureHasFood(Actor actor) => NativeCreatureNeeds(actor) && actor.Body.Hunger >= Simulation.MealAt
        && (FreeCargo(actor, "food") > 0 || actor.Position.Distance(Plan.Towns[actor.HomeTown].Center) <= 28
            && State.Towns[actor.HomeTown].Stocks["food"] > 0);

    internal Actor? NativeFleeThreat(Actor actor)
    {
        if (Ecology(actor) is not { Behavior: "flee" } ecology) return null;
        var nearby = Party.Where(p => p.Life == LifeState.Alive && p.Position.Distance(actor.Position) <= ecology.Notice).ToArray();
        if (nearby.Length == 0) return null;
        var sight = VisionFrom(actor.Position);
        return nearby.Where(p => sight.Contains(p.Position)).OrderBy(p => p.Position.Distance(actor.Position)).ThenBy(p => p.Id).FirstOrDefault();
    }

    internal bool FeedNativeCreature(Actor actor)
    {
        if (!NativeCreatureHasFood(actor) || !TryEat(actor)) return false;
        actor.Body.Hunger = Math.Max(0, actor.Body.Hunger - Simulation.MealReduction);
        return true;
    }
}
