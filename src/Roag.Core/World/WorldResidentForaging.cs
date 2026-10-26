namespace Roag.Core.World;

public sealed record ResidentForageStatus(long Searches, long TerrainCandidates, int Requests, int Claims, int Selected);

public sealed partial class WorldSession
{
    public const int ResidentForageRadius = 96, ResidentForageSearchers = 4, ResidentForageProbes = 16;
    private const string ForageReason = "Foraging for food";
    private const string HomeFoodReason = "Seeking food from home supplies";
    private readonly Dictionary<ChunkKey, int> _residentForageClaims = [];
    private readonly List<long> _residentForageRequests = [];
    private readonly long[] _residentForageTurns = new long[ResidentForageSearchers];
    private int _residentForageTurnCount;
    private long _residentForagePlanTick = -1, _residentForageSearches, _residentForageCandidates;
    private static readonly (int X, int Y)[] ResidentForageOffsets = ForageOffsets();
    private readonly record struct ForageCandidate(WorldPosition Position, int Distance);
    public ResidentForageStatus ResidentForaging => new(_residentForageSearches, _residentForageCandidates,
        _residentForageRequests.Count, _residentForageClaims.Count, _residentForageTurnCount);
    private static (int, int)[] ForageOffsets()
    {
        var offsets = new List<(int, int)>();
        for (int radius = 0; radius <= 6; radius++)
            for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++)
                if (Math.Abs(x) + Math.Abs(y) == radius) offsets.Add((x, y));
        return offsets.ToArray();
    }
    private static bool ForagingRoutine(ResidentRoutine? routine) => routine is { Kind: RoutineKind.Food, Reason: ForageReason };
    private bool ForageClaim(Actor actor, ResidentRoutine? routine) => actor.Kind == ActorKind.Person && actor.Life == LifeState.Alive
        && !actor.Recruited && actor.Body.Hunger >= Simulation.MealAt && ForagingRoutine(routine);
    private void ChangeForageClaim(Actor actor, ResidentRoutine? routine, int change)
    {
        if (_residentForagePlanTick != State.Tick || !ForageClaim(actor, routine)) return;
        var key = routine!.Target.Chunk; int count = _residentForageClaims.GetValueOrDefault(key) + change;
        if (count <= 0) _residentForageClaims.Remove(key); else _residentForageClaims[key] = count;
    }
    private int AvailableResidentForage(Actor actor, WorldPosition position)
    {
        int others = _residentForageClaims.GetValueOrDefault(position.Chunk);
        if (ForageClaim(actor, actor.Routine) && actor.Routine!.Target.Chunk == position.Chunk) others--;
        return HabitatAt(position).Food - Math.Max(0, others) * 2;
    }
    private bool ResidentTownFood(Actor actor)
    {
        var home = Plan.Towns[actor.HomeTown].Center;
        return actor.Position.Floor == 0 && actor.Position.Distance(home) <= 100 && State.Towns[actor.HomeTown].Stocks["food"] > 0;
    }
    private bool ResidentCarriedFood(Actor actor) => FreeCargo(actor, "food") > 0
        || actor.Occupation is { Phase: OccupationPhase.Delivering } job && job.Carried.GetValueOrDefault("food") > 0;
    private bool RetainForageDestination(Actor actor) => ForagingRoutine(actor.Routine) && !actor.Routine!.Blocked
        && actor.Routine.Target.Distance(actor.Position) <= ResidentForageRadius && AvailableResidentForage(actor, actor.Routine.Target) >= 2;
    private bool NeedsForageSearch(Actor actor) => actor.Kind == ActorKind.Person && actor.Life == LifeState.Alive && !actor.Recruited
        && actor.Body.Hunger >= Simulation.MealAt && (actor.Routine != null || actor.Position.Distance(State.Position) <= 100)
        && !ResidentTownFood(actor) && !ResidentCarriedFood(actor) && AvailableResidentForage(actor, actor.Position) < 2 && !RetainForageDestination(actor);
    private void PrepareResidentForagePlan()
    {
        if (_residentForagePlanTick == State.Tick) return;
        _residentForagePlanTick = State.Tick; _residentForageClaims.Clear(); _residentForageRequests.Clear();
        foreach (var other in State.Actors) ChangeForageClaim(other, other.Routine, 1);
        foreach (var other in State.Actors) if (NeedsForageSearch(other)) _residentForageRequests.Add(other.Id);
        _residentForageTurnCount = Math.Min(ResidentForageSearchers, _residentForageRequests.Count);
        // Rotate among the actual requests, not the whole roster. Rebuilding
        // from saved state and logical ticks preserves exact reload decisions.
        int start = _residentForageRequests.Count == 0 ? 0 : (int)(State.Tick / 5 % _residentForageRequests.Count);
        for (int i = 0; i < _residentForageTurnCount; i++) _residentForageTurns[i] = _residentForageRequests[(start + i) % _residentForageRequests.Count];
    }
    private WorldPosition ResidentForageTarget(Actor actor)
    {
        PrepareResidentForagePlan();
        if (ResidentCarriedFood(actor) || AvailableResidentForage(actor, actor.Position) >= 2) return actor.Position;
        if (RetainForageDestination(actor)) return actor.Routine!.Target;
        var fallback = ForagingRoutine(actor.Routine) && actor.Routine!.Target.Distance(actor.Position) <= ResidentForageRadius ? actor.Routine.Target : actor.Position;
        bool turn = false; for (int i = 0; i < _residentForageTurnCount; i++) if (_residentForageTurns[i] == actor.Id) turn = true;
        if (!turn) return fallback;
        _residentForageSearches++;
        if (_diagnosticSink != null) Diagnose("resident-forage-search", new { actor = actor.Id });
        Span<ForageCandidate> candidates = stackalloc ForageCandidate[8]; int count = 0;
        var key = actor.Position.Chunk;
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
        {
            if (x == 0 && y == 0) continue;
            // Interior targets keep the route engine's adjacent arrival inside
            // the selected habitat; reaching its outside edge cannot gather it.
            int left = (key.X + x) * 64, top = (key.Y + y) * 64;
            var target = new WorldPosition(Math.Clamp(actor.Position.X, left + 7, left + 56), Math.Clamp(actor.Position.Y, top + 7, top + 56), key.Floor);
            int distance = target.Distance(actor.Position);
            if (!Plan.Contains(target) || distance > ResidentForageRadius || AvailableResidentForage(actor, target) < 2
                || actor.Routine is { Blocked: true } old && ForagingRoutine(old) && old.Target != actor.Position && old.Target.Chunk == target.Chunk) continue;
            int index = count++;
            while (index > 0 && candidates[index - 1].Distance > distance) { candidates[index] = candidates[index - 1]; index--; }
            candidates[index] = new(target, distance);
        }
        long since = ForagingRoutine(actor.Routine) ? actor.Routine!.Since : State.Tick;
        int offset = (int)(Math.Max(0, State.Tick - since) / 5 * 2 % ResidentForageOffsets.Length), probes = 0;
        for (int pass = 0; pass < 2 && probes < ResidentForageProbes; pass++)
            for (int i = 0; i < count && probes < ResidentForageProbes; i++)
            {
                var (dx, dy) = ResidentForageOffsets[(offset + pass) % ResidentForageOffsets.Length]; var target = candidates[i].Position.Offset(dx, dy);
                if (target.Distance(actor.Position) > ResidentForageRadius) continue;
                probes++; _residentForageCandidates++;
                if (GroundOpen(target) && OccupantAt(target, actor.Id) == null) return target;
            }
        return fallback;
    }
}
