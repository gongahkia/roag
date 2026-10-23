using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed record EncounterPopulationSelection(int SiteId, int Floor, string EncounterId, string Tier,
    string Purpose, Biome Biome, long LeaderId, string[] Eligible, bool Repeated);
public sealed record EncounterPopulationRecord(int SiteId, int Floor, string EncounterId, bool Spawned, string Failure);
public sealed record EncounterPopulationResult(EncounterPopulationRecord Record, long[] Actors, WorldPosition[] Positions, bool Created);

public sealed partial class WorldSession
{
    public IReadOnlyList<EncounterPopulationSelection> EncounterPopulationPlan(int siteId) => PlanEncounterPopulation(Plan, Content, siteId);

    public static IReadOnlyList<EncounterPopulationSelection> PlanEncounterPopulation(WorldPlan plan, ContentCatalog catalog, int siteId)
    {
        if (siteId < 0 || siteId >= plan.Sites.Count) throw new ArgumentOutOfRangeException(nameof(siteId));
        var site = plan.Sites[siteId]; var biome = plan.BiomeAt(site.Center.X, site.Center.Y);
        var definitions = catalog.Game.Encounters.Select(catalog.Get<EncounterDefinition>).OrderBy(d => d.Id, StringComparer.Ordinal).ToArray();
        var result = new List<EncounterPopulationSelection>(); var used = new HashSet<string>(); string previous = "";
        for (int floor = 1; floor < WorldPlan.Floors; floor++)
        {
            var eligible = definitions.Where(d => d.Fits(site.Purpose, biome, floor)).ToArray();
            if (eligible.Length == 0) throw new InvalidDataException("No encounter population for " + site.Purpose + "/" + biome + "/" + floor + ".");
            var candidates = eligible.Where(d => !used.Contains(d.Id)).ToArray();
            if (candidates.Length == 0) candidates = eligible.Where(d => d.Id != previous).ToArray();
            if (candidates.Length == 0) candidates = eligible;
            var random = WorldRandom.Stream(plan.Seed, siteId, floor, 7100); int pick = random.Next(candidates.Sum(d => d.Weight));
            var chosen = candidates[0];
            foreach (var candidate in candidates) { pick -= candidate.Weight; if (pick < 0) { chosen = candidate; break; } }
            result.Add(new(siteId, floor, chosen.Id, chosen.Tier, site.Purpose, biome, 70000 + siteId * 16 + floor * 4,
                eligible.Select(d => d.Id).ToArray(), used.Contains(chosen.Id)));
            used.Add(chosen.Id); previous = chosen.Id;
        }
        return result;
    }

    // Called by successful movement/stair commands, never by loading, inspection or Reveal.
    private void EnsureLocalEncounterPopulation()
    {
        if (State.Position.Floor == 0) return;
        var site = Plan.Sites.FirstOrDefault(s => Math.Abs(s.Center.X - State.Position.X) <= 32 && Math.Abs(s.Center.Y - State.Position.Y) <= 32);
        if (site != null) EnsureEncounterPopulation(site.Id, State.Position.Floor);
    }

    /// <summary>Materializes one planned floor once, without terrain edits or simulation/combat random draws.</summary>
    public EncounterPopulationResult EnsureEncounterPopulation(int siteId, int floor)
    {
        if (floor is < 1 or >= WorldPlan.Floors) throw new ArgumentOutOfRangeException(nameof(floor));
        var selected = EncounterPopulationPlan(siteId).Single(p => p.Floor == floor);
        var existing = State.EncounterPopulations.FirstOrDefault(p => p.SiteId == siteId && p.Floor == floor);
        if (existing != null) return Result(existing, false);
        var definition = Content.Get<EncounterDefinition>(selected.EncounterId); var site = Plan.Sites[siteId];
        var positions = FindEncounterPlacement(site, floor, definition.Members.Length);
        var record = new EncounterPopulationRecord(siteId, floor, definition.Id, positions.Length == definition.Members.Length,
            positions.Length == definition.Members.Length ? "" : "No connected free cluster beyond the stairs and caches.");
        if (record.Spawned)
        {
            // Construct the complete set before mutating the world, so failures cannot leave half a group.
            var actors = definition.Members.Select((member, index) =>
            {
                var actor = ActorFactory.Create(Content, member, Seed, selected.LeaderId + index, positions[index], siteId % 24, site.Culture, siteId % 6);
                actor.EncounterLeader = selected.LeaderId; actor.EncounterId = definition.Id; return actor;
            }).ToArray();
            if (actors.Any(a => State.Actors.Any(existingActor => existingActor.Id == a.Id)))
                throw new InvalidDataException("An encounter population actor already exists without its generation record.");
            State.Actors.AddRange(actors);
        }
        State.EncounterPopulations.Add(record);
        return Result(record, true);

        EncounterPopulationResult Result(EncounterPopulationRecord entry, bool created)
        {
            var actors = entry.Spawned ? State.Actors.Where(a => a.EncounterLeader == selected.LeaderId).OrderBy(a => a.Id).ToArray() : [];
            return new(entry, actors.Select(a => a.Id).ToArray(), actors.Select(a => a.Position).ToArray(), created);
        }
    }

    private WorldPosition[] FindEncounterPlacement(SitePlan site, int floor, int count)
    {
        const int size = WorldPlan.ChunkSize;
        var footprint = Chunks.Generator.GenerateFootprint(false, site.Id, floor);
        var origin = site.Center.Offset(-size / 2, -size / 2) with { Floor = floor };
        bool InFootprint(WorldPosition p) => p.Floor == floor && p.X >= origin.X && p.Y >= origin.Y && p.X < origin.X + size && p.Y < origin.Y + size;
        var solid = State.Loot.Where(c => !c.Taken && c.SourceActor == 0 && InFootprint(c.Position)).Select(c => c.Position).ToHashSet();
        var occupied = State.Actors.Where(a => a.Life != LifeState.Dead && a.Position.Floor == floor).Select(a => a.Position).ToHashSet();
        var edits = State.Chunks.Where(c => c.Key.Floor == floor && c.Key.X >= origin.X / size && c.Key.X <= (origin.X + size - 1) / size
            && c.Key.Y >= origin.Y / size && c.Key.Y <= (origin.Y + size - 1) / size).SelectMany(c => c.Edits.Select(e =>
            new KeyValuePair<WorldPosition, Material>(new(c.Key.X * size + e.Key % size, c.Key.Y * size + e.Key / size, floor), e.Value))).ToDictionary(p => p.Key, p => p.Value);
        WorldPosition At(int index) => origin.Offset(index % size, index / size);
        Material Terrain(int index) => edits.GetValueOrDefault(At(index), footprint.Cells[index]);
        bool Open(int index) => Materials.Walkable(Terrain(index)) && Terrain(index) != Material.Camp && !solid.Contains(At(index));
        var stairs = Enumerable.Range(0, size * size).Where(i => Terrain(i) is Material.StairsDown or Material.StairsUp).ToArray();
        var reachable = new HashSet<int>(); var queue = new Queue<int>();
        // Both preserved stairs are access anchors; player occupancy on an arrival stair does not erase connectivity.
        foreach (int stair in stairs.Where(Open)) if (reachable.Add(stair)) queue.Enqueue(stair);
        while (queue.TryDequeue(out int current)) foreach (int next in Neighbors(current))
            if (Open(next) && reachable.Add(next)) queue.Enqueue(next);
        bool Candidate(int index) => reachable.Contains(index) && !occupied.Contains(At(index))
            && stairs.All(stair => At(stair).Distance(At(index)) > 4)
            && solid.All(cache => cache.Distance(At(index)) > 2);
        var candidates = reachable.Where(Candidate).OrderBy(i => WorldRandom.Hash(Seed, site.Id, floor, i + 7120)).ThenBy(i => i).ToArray();
        foreach (int anchor in candidates)
        {
            var cluster = new List<int>(); var seen = new HashSet<int> { anchor }; queue.Clear(); queue.Enqueue(anchor);
            while (queue.TryDequeue(out int current))
            {
                if (Candidate(current)) cluster.Add(current);
                if (cluster.Count == count) return cluster.Select(At).ToArray();
                foreach (int next in Neighbors(current))
                    if (At(anchor).Distance(At(next)) <= 4 && Open(next) && !occupied.Contains(At(next)) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return [];

        static IEnumerable<int> Neighbors(int index)
        {
            foreach (int offset in new[] { -1, 1, -size, size })
            {
                int next = index + offset;
                if (next >= 0 && next < size * size && Math.Abs(next % size - index % size) + Math.Abs(next / size - index / size) == 1) yield return next;
            }
        }
    }

    private static void ValidateEncounterPopulations(WorldPlan plan, WorldState state, ContentCatalog catalog)
    {
        if (state.EncounterPopulations == null || state.EncounterPopulations.Count > plan.Sites.Count * 3
            || state.EncounterPopulations.Any(p => p == null || p.SiteId < 0 || p.SiteId >= plan.Sites.Count || p.Floor is < 1 or >= WorldPlan.Floors
                || p.EncounterId == null || p.Failure == null || p.Failure.Length > 200 || p.Spawned != (p.Failure.Length == 0))
            || state.EncounterPopulations.Select(p => (p.SiteId, p.Floor)).Distinct().Count() != state.EncounterPopulations.Count)
            throw new InvalidDataException("Invalid encounter population records.");
        var accounted = new HashSet<long>();
        foreach (var record in state.EncounterPopulations)
        {
            var selected = PlanEncounterPopulation(plan, catalog, record.SiteId).Single(p => p.Floor == record.Floor);
            if (record.EncounterId != selected.EncounterId) throw new InvalidDataException("Encounter population differs from its seeded plan.");
            var definition = catalog.Get<EncounterDefinition>(record.EncounterId);
            for (int i = 0; i < 4; i++)
            {
                long id = selected.LeaderId + i; var actor = state.Actors.FirstOrDefault(a => a.Id == id);
                bool expected = record.Spawned && i < definition.Members.Length;
                if (expected ? actor == null || actor.EncounterLeader != selected.LeaderId || actor.EncounterId != record.EncounterId
                        || actor.Archetype != definition.Members[i] || actor.Position.Floor != record.Floor : actor != null)
                    throw new InvalidDataException("Encounter population membership differs from its generation record.");
                if (expected) accounted.Add(id);
            }
        }
        if (state.Actors.Any(a => a.Id >= 70000 && a.Id < 70000 + plan.Sites.Count * 16 && !accounted.Contains(a.Id)))
            throw new InvalidDataException("An encounter population actor is missing its generation record.");
    }
}
