using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private bool ResumingNativeResident(Actor actor) => State.NativeCampaign != null && !actor.Recruited
        && actor is { Kind: ActorKind.Person, Life: LifeState.Alive, Tactics.Resident.Target: 0 };

    internal ResidentRoutine PlanNativeResidentRoutine(Actor actor)
    {
        if (!ResumingNativeResident(actor)) throw new InvalidOperationException("Only an existing resident between rescues resumes a world routine.");
        SelectResidentRoutine(actor);
        return actor.Routine!;
    }

    internal bool NativeResidentHasFood(Actor actor) => actor.Body.Hunger >= Simulation.MealAt
        && (actor.Occupation is { Phase: OccupationPhase.Delivering } job && job.Carried.GetValueOrDefault("food") > 0
            || FreeCargo(actor, "food") > 0
            || actor.Routine is { Kind: RoutineKind.Food, Reason: HomeFoodReason }
                && actor.Position.Distance(Plan.Towns[actor.HomeTown].Center) <= 1 && State.Towns[actor.HomeTown].Stocks["food"] > 0);

    internal bool FeedNativeResident(Actor actor)
    {
        if (!NativeResidentHasFood(actor) || !TryEat(actor)) return false;
        actor.Body.Hunger = Math.Max(0, actor.Body.Hunger - Simulation.MealReduction);
        return true;
    }

    internal void MarkNativeResidentBlocked(Actor actor, bool blocked) => MarkResidentBlocked(actor, blocked);
}
