using System.Text.Json.Serialization;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public enum MigrationReason { Food, Pressure, HomeUnavailable }
public sealed class WildlifeMigration
{
    [JsonRequired] public WorldPosition Origin { get; set; }
    [JsonRequired] public WorldPosition Center { get; set; }
    [JsonRequired] public WorldPosition? Target { get; set; }
    [JsonRequired] public MigrationReason Reason { get; set; }
    [JsonRequired] public int Cursor { get; set; }
    [JsonRequired] public long Since { get; set; }
    [JsonRequired] public long LastProgressTick { get; set; }
    [JsonRequired] public WorldPosition LastPosition { get; set; }
}

public sealed partial class WorldSession
{
    public const int WildlifeMigrationRadius = 96, WildlifeSearchBudget = 128;
    public const int WildlifeSearchCells = 1 + 2 * WildlifeMigrationRadius * (WildlifeMigrationRadius + 1);
    private long _wildlifeCandidates;
    private readonly HashSet<long> _wildlifeMigrationHandled = [];
    public long WildlifeMigrationCandidates => _wildlifeCandidates;
    private void WakeHabitatWildlife(WorldPosition position)
    {
        foreach (var actor in State.Actors)
            if (actor.Life == LifeState.Alive && actor.Habitat is { } home && home.Chunk == position.Chunk) actor.WildlifeAfter = State.Tick;
    }
    private bool WildlifeHomeSuitable(WorldPosition home, EcologyRules rules)
    {
        if (!Plan.Contains(home) || rules.Underground != (home.Floor > 0) || Chunks.IsEdited(home)
            || !rules.Biomes.Contains(Chunks.Get(home.Chunk).Biomes[home.LocalIndex])
            || home.Floor == 0 && Plan.Towns.Any(t => t.Center.Distance(home) < 42)
            || home.Floor > 0 && !Plan.Sites.Any(s => Math.Abs(s.Center.X - home.X) < 30 && Math.Abs(s.Center.Y - home.Y) < 30)) return false;
        return rules.Resource is { } resource ? ResourceAt(home) is { Available: > 0 } node && node.Definition.Id == resource
            : Chunks.At(home) == (rules.Underground ? Material.Floor : Material.Grass) && GroundOpen(home) && ResourceAt(home) == null;
    }
    private MigrationReason? WildlifeMigrationNeed(Actor actor, EcologyRules rules)
    {
        var home = actor.Habitat!.Value; var habitat = HabitatAt(home);
        // A seeded home's generated terrain is immutable. Healthy offscreen
        // decisions read only its live edit/stock ledger, without loading chunks.
        var stock = Chunks.ResourceStockAt(home);
        if (Chunks.IsEdited(home) || rules.Resource != null && stock is { Remaining: 0 }
            && (stock.RecoverAt == 0 || stock.RecoverAt > State.Tick)) return MigrationReason.HomeUnavailable;
        if (habitat.Pressure >= 600 || habitat.Capacity < 2) return MigrationReason.Pressure;
        return habitat.Food < 2 && actor.Body.Hunger >= Simulation.MealAt && FreeCargo(actor, "food") == 0 ? MigrationReason.Food : null;
    }
    private bool WildlifeThreatened(Actor actor, EcologyRules rules)
    {
        if (rules.Behavior != "flee") return IsHostile(actor);
        foreach (long id in State.Party)
        { var member = ActorById(id); if (member.Life != LifeState.Dead && member.Position.Distance(actor.Position) <= rules.Notice) return true; }
        return false;
    }
    private bool WildlifeDestinationClaimed(Actor actor, ChunkKey key) => State.Actors.Any(a => a.Id != actor.Id
        && a.Life != LifeState.Dead && (a.Habitat?.Chunk == key || a.Migration?.Target?.Chunk == key));
    private bool WildlifeDestinationSuitable(Actor actor, EcologyRules rules, WorldPosition home)
    {
        var view = HabitatAt(home);
        return view.Food >= 2 && view.Pressure < 200 && view.Capacity >= 2 && !WildlifeDestinationClaimed(actor, home.Chunk)
            && WildlifeHomeSuitable(home, rules);
    }
    internal static WorldPosition WildlifeSearchCell(WorldPosition center, int index)
    {
        if (index == 0) return center;
        int radius = (int)Math.Ceiling((Math.Sqrt(1 + 2 * index) - 1) / 2);
        int offset = index - (1 + 2 * (radius - 1) * radius), side = offset / radius, step = offset % radius;
        return side switch { 0 => center.Offset(step, -radius + step), 1 => center.Offset(radius - step, step),
            2 => center.Offset(-step, radius - step), _ => center.Offset(-radius + step, -step) };
    }
    private void SearchWildlifeDestination(Actor actor, EcologyRules rules, WildlifeMigration migration, ref int budget)
    {
        while (migration.Cursor < WildlifeSearchCells && budget > 0)
        {
            var candidate = WildlifeSearchCell(migration.Center, migration.Cursor++); budget--; _wildlifeCandidates++;
            if (!Plan.Contains(candidate) || candidate == migration.Origin
                || migration.Reason != MigrationReason.HomeUnavailable && candidate.Chunk == migration.Origin.Chunk
                || !WildlifeDestinationSuitable(actor, rules, candidate)) continue;
            bool approach = false;
            foreach (var (x, y) in Directions) if (GroundOpen(candidate.Offset(x, y))) { approach = true; break; }
            if (!approach) continue;
            migration.Target = candidate; migration.LastProgressTick = State.Tick; migration.LastPosition = actor.Position;
            State.EcologyChunks.Add(candidate.Chunk); actor.Journey = null;
            Diagnose("wildlife-migration-planned", new { actor = actor.Id, migration.Origin, migration.Target, migration.Reason });
            break;
        }
        if (migration.Target == null && migration.Cursor == WildlifeSearchCells)
        { actor.Migration = null; actor.WildlifeAfter = State.Tick + TicksPerHour; }
    }
    private void AdvanceWildlifeMigrations()
    {
        _wildlifeMigrationHandled.Clear();
        if (State.Tick % 5 != 0 || State.Actors.Count == 0) return;
        int budget = WildlifeSearchBudget, start = (int)(State.Tick / 5 % State.Actors.Count);
        // Rotate the shared search budget by logical tick and stable collection
        // order; save/load and observer distance cannot alter planner fairness.
        for (int offset = 0; offset < State.Actors.Count; offset++)
        {
            var actor = State.Actors[(start + offset) % State.Actors.Count];
            if (actor.Life != LifeState.Alive || actor.Recruited || actor.Habitat == null || Ecology(actor) is not { } rules) continue;
            if (actor.Migration == null && State.Tick >= actor.WildlifeAfter)
            {
                actor.WildlifeAfter = State.Tick + TicksPerHour;
                if (WildlifeMigrationNeed(actor, rules) is { } reason)
                {
                    actor.NatalHabitat ??= actor.Habitat; actor.Body.ForageProgress = 0;
                    actor.Migration = new() { Origin = actor.Habitat.Value, Center = actor.Position, Reason = reason,
                        Since = State.Tick, LastProgressTick = State.Tick, LastPosition = actor.Position };
                }
            }
            if (actor.Migration is not { } migration) continue;
            _wildlifeMigrationHandled.Add(actor.Id);
            if (State.Battle?.ActorIds.Contains(actor.Id) == true || WildlifeThreatened(actor, rules)
                || actor.Body.RestUntil > State.Tick || actor.Body.ForageProgress > 0 || HasWork(actor))
            { migration.LastProgressTick = State.Tick; continue; }
            if (migration.Target == null) SearchWildlifeDestination(actor, rules, migration, ref budget);
            if (migration.Target is not { } target) continue;
            if (actor.Position != migration.LastPosition) { migration.LastPosition = actor.Position; migration.LastProgressTick = State.Tick; }
            // Pressure/food changes are cheap live checks. Natural target terrain
            // is reconsidered at decision cadence or immediately after an edit.
            var habitat = HabitatAt(target);
            bool invalid = habitat.Food < 2 || habitat.Pressure >= 200 || WildlifeDestinationClaimed(actor, target.Chunk) || Chunks.IsEdited(target);
            if (invalid || State.Tick - migration.LastProgressTick >= 100)
            { migration.Target = null; actor.Journey = null; continue; }
            if (actor.Position.Distance(target) <= 1)
            {
                if (!WildlifeHomeSuitable(target, rules)) { migration.Target = null; actor.Journey = null; continue; }
                actor.Habitat = target; actor.Migration = null; actor.Journey = null; actor.WildlifeAfter = State.Tick + TicksPerHour;
                NoteWildlifeHome(actor);
                Diagnose("wildlife-migration-arrived", new { actor = actor.Id, home = target }); continue;
            }
            var next = ResidentStep(actor, target);
            if (next != actor.Position) CommitResidentStep(actor, next);
        }
    }
    private static void ValidateWildlifeMigrations(WorldPlan plan, WorldState state, ContentCatalog catalog)
    {
        foreach (var actor in state.Actors)
        {
            bool wildlife = (catalog.Entries[actor.Archetype] as CharacterDefinition)?.Ecology != null;
            if (!wildlife ? actor.NatalHabitat != null || actor.Migration != null || actor.WildlifeAfter != 0
                : actor.WildlifeAfter < 0 || actor.WildlifeAfter > state.Tick + TicksPerHour
                    || actor.NatalHabitat is { } natal && (!plan.Contains(natal) || natal.Floor != actor.Position.Floor || !state.EcologyChunks.Contains(natal.Chunk)))
                throw new InvalidDataException("Invalid wildlife migration identity or deadline.");
            if (actor.Migration is not { } migration) continue;
            if (!wildlife || actor.NatalHabitat == null || actor.Life != LifeState.Alive || actor.Habitat != migration.Origin
                || !Enum.IsDefined(migration.Reason) || !plan.Contains(migration.Center) || migration.Center.Floor != actor.Position.Floor
                || migration.Cursor is < 0 or > WildlifeSearchCells || migration.Since < 0 || migration.Since > state.Tick
                || migration.LastProgressTick < migration.Since || migration.LastProgressTick > state.Tick || !plan.Contains(migration.LastPosition)
                || migration.LastPosition.Floor != actor.Position.Floor
                || migration.Target is { } target && (!plan.Contains(target) || target.Distance(migration.Center) > WildlifeMigrationRadius
                    || target == migration.Origin || !state.EcologyChunks.Contains(target.Chunk)
                    || migration.Reason != MigrationReason.HomeUnavailable && target.Chunk == migration.Origin.Chunk))
                throw new InvalidDataException("Invalid wildlife migration search or target.");
        }
        var residents = state.Actors.Where(a => a.Life != LifeState.Dead && a.Habitat != null).ToArray();
        if (residents.GroupBy(a => a.Habitat!.Value.Chunk).Any(g => g.Count() > 1)
            || residents.Where(a => a.Migration?.Target != null).GroupBy(a => a.Migration!.Target!.Value.Chunk).Any(g => g.Count() > 1)
            || residents.Any(a => a.Migration?.Target is { } target && residents.Any(b => b.Id != a.Id && b.Habitat!.Value.Chunk == target.Chunk)))
            throw new InvalidDataException("Duplicate wildlife home or migration reservation.");
    }
}
