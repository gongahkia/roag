using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public const int WorkRallyRadius = 16;
    // A live movement command temporarily takes precedence over autonomous work.
    // This is input state, like Pause, rather than an offline order in the save.
    public bool PartyTravelling { get; set; }
    public bool InWorkRally(WorldPosition p) => p.Distance(State.WorkRally ?? State.Position) <= WorkRallyRadius;

    private ActionResult ConfigureWork(long id, string setting, int value)
    {
        var order = State.Work.FirstOrDefault(o => o.Id == id);
        if (order == null) return new(false, "That order has finished or been cancelled.");
        switch (setting)
        {
            case "suspend": if (!order.PinnedWorker) order.Worker = 0; order.Suspended = true; order.Status = "Suspended"; break;
            case "resume": order.Suspended = false; order.PartyApproach = true; order.Status = "Waiting for an available worker"; break;
            case "automatic": order.Worker = 0; order.PinnedWorker = false; order.PartyApproach = true; order.Suspended = false; break;
            case "priority" when value is >= 0 and <= 2: order.Priority = value; break;
            default: return new(false, "Choose suspend, resume, automatic assignment or priority 0..2.");
        }
        return new(true, "Work order updated; reserved supplies and progress retained.");
    }

    private bool ReadyForWork(Actor a) => a.Life == LifeState.Alive && a.Body.RestUntil <= State.Tick
        && a.Body.Fatigue < 75 && a.Body.Hunger < Simulation.StarveAt && a.Health >= a.MaxHealth / 4;

    private void DispatchPartyWork()
    {
        if (State.Work.Count == 0 || IsInCombat || PartyTravelling) return;
        if (State.Work.Count > 0) State.WorkRally ??= State.Position;
        // Reconsider unladen automatic claims every tick. A ready carrier keeps
        // the trip until delivery; an unavailable carrier can hand off the cargo.
        foreach (var order in State.Work.Where(o => !o.PinnedWorker && o.PartyApproach && !o.Suspended))
            if (!HoldsWorkSupplies(order, order.Worker) || !ReadyForWork(ActorById(order.Worker))) order.Worker = 0;
        var available = Party.Where(a => ReadyForWork(a) && !State.Work.Any(o => o.Worker == a.Id))
            .OrderBy(a => a.Id).ToList();
        var routes = new Dictionary<long, WorkRoutes>();
        WorkRoutes Routes(Actor actor) => routes.TryGetValue(actor.Id, out var cached) ? cached : routes[actor.Id] = BuildWorkRoutes(actor);
        foreach (var order in State.Work.Where(o => o.Worker == 0 && !o.Suspended && o.PartyApproach)
            .OrderByDescending(o => o.Priority).ThenBy(o => o.Id))
        {
            if (!InWorkRally(order.Target)) { order.Status = "Outside the 16-tile rally area; move the party closer"; continue; }
            if (available.Count == 0) { order.Status = "Waiting for a free, rested party member"; continue; }
            var choice = available.Where(a => (order.Kind != WorkKind.Forage || a.Body.ForageAfter <= State.Tick) && CampWorkerEligible(order, a)).Select(a => (Actor: a,
                Path: a.Position.Distance(WorkStop(order, a.Id)) <= 1 ? new List<WorldPosition>() : WorkPath(a, WorkStop(order, a.Id), routes: Routes(a))))
                .Where(p => p.Path != null).OrderBy(p => p.Path!.Count).ThenBy(p => p.Actor.Id).FirstOrDefault();
            if (choice.Actor == null) { order.Status = "No free observed approach; open access or move blockers"; continue; }
            order.Worker = choice.Actor.Id; available.Remove(choice.Actor);
        }
    }

    private sealed record WorkRoutes(Dictionary<WorldPosition, WorldPosition> Previous, Dictionary<WorldPosition, int> Distance);
    // One distance field per available member is shared across all 64 candidate
    // jobs. Unreachable selections must not cause 256 repeated terrain searches.
    private WorkRoutes BuildWorkRoutes(Actor actor)
    {
        var previous = new Dictionary<WorldPosition, WorldPosition> { [actor.Position] = actor.Position };
        var distances = new Dictionary<WorldPosition, int> { [actor.Position] = 0 };
        if (!InWorkRally(actor.Position)) return new(previous, distances);
        var occupied = State.Actors.Where(a => a.Id != actor.Id && a.Life != LifeState.Dead && (a.Recruited || Visible.Contains(a.Position))).Select(a => a.Position).ToHashSet();
        var queue = new Queue<WorldPosition>(); queue.Enqueue(actor.Position);
        while (queue.TryDequeue(out var cell))
        {
            foreach (var (dx, dy) in Directions)
            {
                var next = cell.Offset(dx, dy);
                if (previous.ContainsKey(next) || !InWorkRally(next) || !Chunks.Seen(next) || occupied.Contains(next) || !GroundOpen(next)) continue;
                previous[next] = cell; distances[next] = distances[cell] + 1; queue.Enqueue(next);
            }
        }
        return new(previous, distances);
    }

    // The work target need not be walkable: work takes place from adjacent ground.
    private List<WorldPosition>? WorkPath(Actor actor, WorldPosition target, int range = 1, WorkRoutes? routes = null)
    {
        if (actor.Position.Distance(target) <= range) return [];
        if (!InWorkRally(target) || !InWorkRally(actor.Position)) return null;
        routes ??= BuildWorkRoutes(actor);
        WorldPosition? approach = null;
        int bestDistance = int.MaxValue;
        // Only the target's small interaction neighbourhood can be an approach.
        // Y/X iteration preserves the original distance, then Y, then X tie order.
        for (int y = -range; y <= range; y++) for (int x = -range; x <= range; x++)
        {
            if (Math.Abs(x) + Math.Abs(y) > range) continue;
            var cell = target.Offset(x, y);
            if (routes.Distance.TryGetValue(cell, out int distance) && distance < bestDistance)
            { approach = cell; bestDistance = distance; }
        }
        if (approach == null) return null;
        var path = new List<WorldPosition>();
        for (var p = approach.Value; p != actor.Position; p = routes.Previous[p]) path.Add(p);
        path.Reverse(); return path;
    }

    private void MoveWorker(Actor actor, WorldPosition next)
    {
        if (!CanEnter(next, actor.Id)) return;
        actor.Position = next;
        if (actor.Id == State.Party[0]) { State.Position = next; EnsureLocalEncounterPopulation(); }
        Reveal();
    }

    private void ReturnIdleParty()
    {
        if (IsInCombat || PartyTravelling || State.WorkRally is not { } rally) return;
        foreach (var actor in Party.Where(a => a.Life == LifeState.Alive && a.Body.RestUntil <= State.Tick
            && !State.Work.Any(o => o.Worker == a.Id && !o.Suspended && o.PartyApproach)))
        {
            var path = WorkPath(actor, rally, range: 2);
            if (path is { Count: > 0 }) { MoveWorker(actor, path[0]); Contact(); if (IsInCombat) break; }
        }
        if (State.Work.Count == 0 && Party.All(a => a.Position.Distance(rally) <= 2)) State.WorkRally = null;
    }
}
