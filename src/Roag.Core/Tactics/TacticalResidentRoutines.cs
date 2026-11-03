using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private void CommitResidentRoutineTurn(Actor resident)
    {
        var routine = _world.PlanNativeResidentRoutine(resident);
        if (routine.Kind != RoutineKind.Danger && _world.NativeResidentHasFood(resident))
        {
            CommitResidentRoutineSlice(resident, (action, _) =>
            {
                if (_world.FeedNativeResident(resident))
                    Emit("FoodEaten", resident.Id, resident.Id, action, resident.Position, _world.Simulation.MealReduction, ability: "resident-meal");
            });
            return;
        }
        if (routine.Kind == RoutineKind.AssignedWork && _world.State.Work.FirstOrDefault(w => w.Worker == resident.Id && !w.Suspended) is { } order)
        {
            var work = _world.NativeWorkStep(resident, order.Id);
            if (work is { Action: TacticalAction.ContinueActivity })
                CommitResidentRoutineSlice(resident, (action, recovery) => _world.PerformNativeWork(resident, order.Id, recovery, this, action));
            else if (work != null)
            {
                var result = Commit(work);
                if (!result.Success) throw new InvalidOperationException("Resident work navigation rejected its own step: " + result.Message);
            }
            else CommitResidentRoutineSlice(resident, (_, _) => { });
            return;
        }
        if (resident.Position.Distance(routine.Target) <= 1)
        {
            _world.MarkNativeResidentBlocked(resident, routine.Kind == RoutineKind.Danger);
            CommitResidentRoutineSlice(resident, (_, _) => { });
            return;
        }
        var goals = new HashSet<WorldPosition>();
        foreach (var facing in new[] { Facing.North, Facing.East, Facing.South, Facing.West })
        {
            var (x, y) = AttackGeometry.Offset(facing); var p = routine.Target.Offset(x, y);
            if (_world.CanEnter(p, resident.Id)) goals.Add(p);
        }
        if (_world.CanEnter(routine.Target, resident.Id)) goals.Add(routine.Target);
        var step = _world.Navigate(resident, goals, _world.Plan.Contains);
        if (routine.Kind == RoutineKind.Danger && _world.State.Actors.FirstOrDefault(a => a.Id == routine.Other && a.Life == LifeState.Alive) is { } threat
            && step != null && step.Destination.Distance(threat.Position) <= resident.Position.Distance(threat.Position)) step = null;
        _world.MarkNativeResidentBlocked(resident, step == null);
        if (step == null) { CommitResidentRoutineSlice(resident, (_, _) => { }); return; }
        var moved = Commit(new(step.Action, step.Facing));
        if (!moved.Success) throw new InvalidOperationException("Resident routine navigation rejected its own step: " + moved.Message);
    }

    private void CommitResidentRoutineSlice(Actor resident, Action<long, long> effect)
    {
        long action = State.NextAction++, recovery = ActionTiming.Recovery(Tuning.WaitCost, EffectiveSpeed(resident));
        ResolveAtomically(RootCause(action), () =>
        {
            State.Queue.Schedule(ScheduledKind.ActorReady, checked(Clock + recovery), resident.Id);
            State.ActiveActor = 0;
            effect(action, recovery);
            Emit("Waited", resident.Id, resident.Id, action, resident.Position);
        });
    }
}
