namespace Vagabonds.Core.World;

internal readonly record struct ResidentGround(Material Terrain, int Culture, int Landmarks);
public sealed record ResidentJourneyStatus(int Active, int RetainedSteps, long Plans, long CommittedSteps, long TerrainReads);

public sealed partial class WorldSession
{
    public const int ResidentJourneyLimit = 64;
    private long _residentPlans, _residentSteps, _residentTerrainReads;
    public ResidentJourneyStatus ResidentJourneys => new(State.Actors.Count(a => a.Journey != null),
        State.Actors.Sum(a => a.Journey?.Steps.Count ?? 0), _residentPlans, _residentSteps, _residentTerrainReads);
    private ResidentGround NaturalResidentGround(WorldPosition p)
    { _residentTerrainReads++; var chunk = Chunks.Get(p.Chunk); return new(Chunks.UneditedAt(p), chunk.Cultures[p.LocalIndex], State.Discoveries.Count); }
    // Generated terrain never changes. Per-actor bounded snapshots avoid loading
    // distant chunks for every footstep; live edits, objects and occupants still apply.
    private bool ResidentGroundOpen(WorldPosition p, ResidentGround natural)
    {
        Material value = default;
        bool edited = Chunks.ExistingDelta(p.Chunk)?.Edits.TryGetValue(p.LocalIndex, out value) == true;
        var terrain = edited ? value : natural.Terrain;
        if (!Materials.Walkable(terrain) || terrain == Material.Camp) return false;
        if (TryExplicitObject(p, out var placed)) return placed is not { Solid: true };
        return edited || terrain is not (Material.Grass or Material.Earth or Material.Floor) || natural.Culture < 0 || p.Floor > 0
            || WorldRandom.Hash(Seed, p.X, p.Y, 999) % 61 != 0;
    }
    internal bool ResidentAnchorOpen(Actor actor, WorldPosition p)
    {
        if (actor.AnchorGround is not { } anchor || anchor.Position != p || anchor.Ground.Landmarks != State.Discoveries.Count)
            actor.AnchorGround = anchor = (p, NaturalResidentGround(p));
        return ResidentGroundOpen(p, anchor.Ground);
    }
    private WorldPosition ResidentStep(Actor actor, WorldPosition target)
    {
        var journey = actor.Journey;
        if (journey == null || journey.Target != target || journey.Next == journey.Steps.Count
            && (journey.Steps.Count > 0 || State.Tick >= journey.RetryAt || journey.Revision != State.NavigationRevision))
            actor.Journey = journey = PlanResidentJourney(actor, target);
        if (journey.Next >= journey.Steps.Count) return actor.Position;
        if (actor.GroundJourney != journey || actor.JourneyGround![0].Landmarks != State.Discoveries.Count)
        {
            actor.GroundJourney = journey; actor.JourneyGround = new ResidentGround[journey.Steps.Count];
            for (int i = 0; i < journey.Steps.Count; i++) actor.JourneyGround[i] = NaturalResidentGround(journey.Steps[i]);
        }
        var next = journey.Steps[journey.Next];
        if (actor.Position.Distance(next) != 1 || !ResidentGroundOpen(next, actor.JourneyGround![journey.Next]) || OccupantAt(next, actor.Id) != null)
        {
            actor.Journey = journey = PlanResidentJourney(actor, target);
            actor.GroundJourney = null;
            if (journey.Steps.Count == 0) return actor.Position;
            // The fresh search checked live terrain and occupancy. Its first step
            // is committed below; its ground snapshot is hydrated on the next use.
            next = journey.Steps[0];
        }
        return next;
    }
    private void CommitResidentStep(Actor actor, WorldPosition next)
    {
        var journey = actor.Journey;
        CommitClimateMove(actor, next);
        if (journey != null) { journey.Next++; actor.Journey = journey; }
        _residentSteps++;
    }
    private ResidentJourney PlanResidentJourney(Actor actor, WorldPosition target)
    {
        _residentPlans++;
        var result = new ResidentJourney { Origin = actor.Position, Target = target, RetryAt = State.Tick + 25, Revision = State.NavigationRevision };
        if (!Plan.Contains(target) || target.Floor != actor.Position.Floor) return result;
        var scratch = _localRouteScratch ??= new(); var open = scratch.Open; var nodes = scratch.Nodes;
        open.Clear(); nodes.Clear(); nodes[actor.Position] = (actor.Position, 0);
        var best = actor.Position; int tie = 0; open.Enqueue(best, (best.Distance(target), tie++));
        while (open.TryDequeue(out var current, out _) && nodes.Count < 256)
        {
            if (current.Distance(target) < best.Distance(target)) best = current;
            if (current.Distance(target) <= 1) break;
            foreach (var (x, y) in Directions)
            {
                var next = current.Offset(x, y); int cost = nodes[current].Cost + 1;
                if (!Plan.Contains(next) || next.Distance(actor.Position) > 24 || OccupantAt(next, actor.Id) != null || !GroundOpen(next)
                    || nodes.TryGetValue(next, out var old) && old.Cost <= cost) continue;
                nodes[next] = (current, cost); open.Enqueue(next, (cost + next.Distance(target), tie++));
            }
        }
        for (var p = best; p != actor.Position; p = nodes[p].Previous) result.Steps.Add(p);
        result.Steps.Reverse();
        if (result.Steps.Count > ResidentJourneyLimit) result.Steps.RemoveRange(ResidentJourneyLimit, result.Steps.Count - ResidentJourneyLimit);
        return result;
    }
    private static void ValidateResidentJourneys(WorldPlan plan, WorldState state)
    {
        if (state.NavigationRevision < 0) throw new InvalidDataException("Invalid navigation revision.");
        foreach (var actor in state.Actors)
        {
            if (actor.Journey is not { } journey) continue;
            if (actor.Kind != ActorKind.Person && (actor.Kind != ActorKind.Beast || actor.Migration?.Target != journey.Target)
                || actor.Recruited || journey.Steps == null || journey.Steps.Count > ResidentJourneyLimit
                || !plan.Contains(journey.Origin) || !plan.Contains(journey.Target) || journey.Origin.Floor != journey.Target.Floor
                || journey.Next < 0 || journey.Next > journey.Steps.Count || journey.RetryAt < 0 || journey.RetryAt > state.Tick + 25
                || journey.Revision < 0 || journey.Revision > state.NavigationRevision)
                throw new InvalidDataException("Invalid resident journey.");
            var previous = journey.Origin;
            var seen = new HashSet<WorldPosition> { previous };
            foreach (var step in journey.Steps)
            {
                if (!plan.Contains(step) || previous.Distance(step) != 1 || step.Distance(journey.Origin) > 24 || !seen.Add(step))
                    throw new InvalidDataException("Disconnected, repeated or out-of-range resident journey.");
                previous = step;
            }
            if (actor.Position != (journey.Next == 0 ? journey.Origin : journey.Steps[journey.Next - 1]))
                throw new InvalidDataException("Resident journey does not match its actor position.");
        }
    }
}
