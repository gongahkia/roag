namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    private readonly List<Actor> _routineActors = [], _routineThreats = [];
    // Activation is local; activated people keep the same decisions and journeys
    // when the player leaves. No second regional copy of the actor is created.
    private void AdvanceRoutines()
    {
        if (IsInCombat) return;
        _routineActors.Clear(); ActorIndex.AddNearTo(State.Position, 100, _routineActors);
        if (State.Tick % 5 == 0)
            foreach (var actor in State.Actors)
                if (actor.Routine != null && actor.Kind == ActorKind.Person && !actor.Recruited && actor.Life == LifeState.Alive && actor.Position.Distance(State.Position) > 100)
                    _routineActors.Add(actor);
        _routineActors.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        foreach (var actor in _routineActors)
        {
            if (actor.Kind != ActorKind.Person || actor.Recruited || actor.Life != LifeState.Alive || actor.EncounterLeader > 0) continue;
            if (actor.Routine != null && State.Tick % 5 != 0) continue;
            SelectResidentRoutine(actor);
        }
    }
    private void SelectResidentRoutine(Actor actor)
    {
        Actor? danger = null; int distance = 7;
        _routineThreats.Clear(); ActorIndex.AddNearTo(actor.Position, 6, _routineThreats);
        foreach (var other in _routineThreats)
        {
            int next = actor.Position.Distance(other.Position);
            if (IsHostile(other) && (next < distance || next == distance && other.Id < danger!.Id)) { danger = other; distance = next; }
        }
        if (danger != null)
        {
            int dx = Math.Sign(actor.Position.X - danger.Position.X), dy = Math.Sign(actor.Position.Y - danger.Position.Y);
            if (dx == 0 && dy == 0) dx = 1;
            SetResidentRoutine(actor, RoutineKind.Danger, actor.Position.Offset(dx * 6, dy * 6), "Taking cover from nearby danger", danger.Id);
            if (actor.Body.RestUntil > State.Tick) actor.Body.RestUntil = State.Tick;
            return;
        }
        var home = Plan.Towns[actor.HomeTown].Center with { Floor = actor.Position.Floor };
        if (actor.Body.Hunger >= Simulation.MealAt)
        {
            bool supplies = actor.Position.Floor == 0 && actor.Position.Distance(home) <= 100 && State.Towns[actor.HomeTown].Stocks["food"] > 0;
            SetResidentRoutine(actor, RoutineKind.Food, supplies ? home : actor.Position,
                supplies ? "Seeking food from home supplies" : "Foraging for food");
            return;
        }
        if (actor.Body.Fatigue >= 75 || actor.Body.RestUntil > State.Tick || actor.Health < actor.MaxHealth / 4)
        {
            SetResidentRoutine(actor, RoutineKind.Rest, home.Offset((int)(actor.Id % 5) * 3 - 6, -18), "Resting at home");
            // A physically activated resident must reach home before resting.
            if (actor.Position.Distance(actor.Routine!.Target) > 1 && actor.Body.RestUntil > State.Tick) actor.Body.RestUntil = State.Tick;
            if (actor.Position.Distance(actor.Routine!.Target) <= 1 && actor.Body.RestUntil <= State.Tick) actor.Body.RestUntil = State.Tick + 300;
            return;
        }
        var work = State.Work.FirstOrDefault(w => w.Worker == actor.Id && !w.Suspended);
        if (work != null) { SetResidentRoutine(actor, RoutineKind.AssignedWork, WorkStop(work, actor.Id), work.Output != null ? "Delivering gathered materials" : "Completing assigned " + work.Kind.ToString().ToLowerInvariant(), ground: false); return; }
        int hour = (int)(State.Tick / TicksPerHour % 24);
        if (hour is >= 17 and <= 20 && actor.FriendId > 0)
        {
            // Answer a friend's invitation before starting another one; a chain
            // of equally fast people chasing moving friends never meets.
            var friend = _routineActors.Where(a => a.Life == LifeState.Alive && a.Routine is { Kind: RoutineKind.Social } r && r.Other == actor.Id
                && a.Position.Distance(actor.Position) <= 28 && actor.Trust.GetValueOrDefault(a.Id) > -20).MinBy(a => a.Id)
                ?? State.Actors.FirstOrDefault(a => a.Id == actor.FriendId && a.Life == LifeState.Alive);
            if (friend != null && friend.Position.Distance(actor.Position) <= 28 && actor.Trust.GetValueOrDefault(friend.Id) > -20
                && (friend.Routine?.Kind != RoutineKind.Social || friend.Routine.Other == actor.Id))
            { SetResidentRoutine(actor, RoutineKind.Social, friend.Position, "Seeking a friend's company", friend.Id, ground: false); return; }
        }
        if (hour is >= 6 and < 17)
        {
            var target = actor.Job switch
            {
                "farmer" => home.Offset(-14, 12), "woodcutter" => home.Offset(24, 12), "quarrier" => home.Offset(20, -12),
                "healer" => home.Offset(-6, 0), "trader" => home.Offset(0, 4), "guard" => home.Offset(0, 20), _ => home.Offset(8, -8)
            };
            SetResidentRoutine(actor, RoutineKind.Occupation, target, "Working as a " + actor.Job); return;
        }
        SetResidentRoutine(actor, RoutineKind.Home, home.Offset((int)(actor.Id % 5) * 3 - 6, -18), "Returning home after the day's work");
    }
    private void SetResidentRoutine(Actor actor, RoutineKind kind, WorldPosition target, string reason, long other = 0, bool ground = true)
    {
        // Home/work anchors stay stable across decisions and save/load. Dynamic
        // friend/danger targets are reconsidered without storing route history.
        if (ground && actor.Routine is { } previous && previous.Kind == kind && previous.Reason == reason && previous.Other == other && previous.Target.Distance(target) <= 6 && ResidentAnchorOpen(actor, previous.Target)
            && kind is not (RoutineKind.Danger or RoutineKind.Social)) target = previous.Target;
        else if (ground) target = ResidentGround(target, actor.Position);
        var old = actor.Routine;
        if (old != null && old.Kind == kind && old.Target == target && old.Reason == reason && old.Other == other) return;
        actor.Routine = new(kind, target, State.Tick, reason, other);
    }
    private WorldPosition ResidentGround(WorldPosition anchor, WorldPosition fallback)
    {
        for (int radius = 0; radius <= 6; radius++)
            for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++)
            {
                if (Math.Abs(x) + Math.Abs(y) != radius) continue;
                var p = anchor.Offset(x, y); if (GroundOpen(p)) return p;
            }
        return Plan.Contains(anchor) ? anchor : fallback;
    }
    private bool CanBeginResidentRest(Actor actor) => actor.Routine == null || actor.Recruited
        || actor.Routine.Kind == RoutineKind.Rest && actor.Position.Distance(actor.Routine.Target) <= 1;
    private bool RoutineInterruptsWork(Actor actor) => !actor.Recruited && actor.Routine?.Kind is RoutineKind.Danger or RoutineKind.Food or RoutineKind.Rest;
    private bool CanResidentForage(Actor actor) => actor.Kind != ActorKind.Person || actor.Routine == null
        || actor.Routine is { Kind: RoutineKind.Food, Reason: "Foraging for food" };
    private void MoveResident(Actor actor)
    {
        if (actor.Routine is not { } routine || actor.EncounterLeader > 0 || State.Battle?.ActorIds.Contains(actor.Id) == true
            || routine.Kind == RoutineKind.AssignedWork) return;
        if (routine.Kind != RoutineKind.Danger && (actor.Body.RestUntil > State.Tick || actor.Body.ForageProgress > 0 && CanResidentForage(actor))) return;
        if (actor.Position.Distance(routine.Target) <= 1) { MarkResidentBlocked(actor, routine.Kind == RoutineKind.Danger); return; }
        var step = ResidentStep(actor, routine.Target);
        if (routine.Kind == RoutineKind.Danger && State.Actors.FirstOrDefault(a => a.Id == routine.Other && a.Life == LifeState.Alive) is { } threat
            && step.Distance(threat.Position) <= actor.Position.Distance(threat.Position)) step = actor.Position;
        bool moved = step != actor.Position;
        if (moved) CommitResidentStep(actor, step);
        MarkResidentBlocked(actor, !moved);
    }
    private static void MarkResidentBlocked(Actor actor, bool blocked)
    {
        if (actor.Routine is { } routine && routine.Blocked != blocked) actor.Routine = routine with { Blocked = blocked };
    }
    public string RoutineDescription(Actor actor)
    {
        if (actor.Recruited || actor.Routine is not { } routine) return "";
        string activity = routine.Reason;
        if (routine.Kind == RoutineKind.Social && State.Actors.FirstOrDefault(a => a.Id == routine.Other) is { } friend && Visible.Contains(friend.Position)) activity = "Seeking company with " + friend.Name;
        if (routine.Blocked) return activity + "; route blocked, waiting for access.";
        return activity + (actor.Position.Distance(routine.Target) <= 1 ? "; at the destination." : "; on the way.");
    }
}
