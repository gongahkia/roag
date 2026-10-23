using System.Diagnostics;
using System.Text.Json;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed record EncounterContextCoverage(string Purpose, Biome Biome, int Floor, string[] Eligible);
public sealed record EncounterPopulationMeasurement(int Seed, EncounterPopulationSelection Selection, bool Spawned,
    WorldPosition[] Positions, bool Connected, bool Clear, bool StableIds, bool Repeatable, string Failure);
public sealed record EncounterPopulationAuditReport(bool Passed, int[] Seeds, EncounterContextCoverage[] Contexts,
    EncounterPopulationMeasurement[] Groups, Dictionary<string, int> Distribution, Dictionary<string, int> TiersByDepth,
    int RepeatedSelections, int AvoidableRepetitions, bool QueriesPure, bool RandomPreserved, bool TerrainPreserved,
    bool UniqueActors, long ElapsedMilliseconds, string Interpretation);

/// <summary>Exercises actual lazy population in both visitation orders; never opens a player save.</summary>
public static class EncounterPopulationAudit
{
    public static EncounterPopulationAuditReport Run(ContentCatalog content, int[] seeds)
    {
        if (seeds.Length is < 1 or > 16 || seeds.Distinct().Count() != seeds.Length)
            throw new ArgumentException("Use one to sixteen distinct encounter-audit seeds.", nameof(seeds));
        var timer = Stopwatch.StartNew();
        var definitions = content.Game.Encounters.Select(content.Get<EncounterDefinition>).ToArray();
        var contexts = (from purpose in EncounterDefinition.AllowedPurposes from biome in Enum.GetValues<Biome>()
            from floor in Enumerable.Range(1, 3)
            select new EncounterContextCoverage(purpose, biome, floor, definitions.Where(d => d.Fits(purpose, biome, floor))
                .Select(d => d.Id).Order(StringComparer.Ordinal).ToArray())).ToArray();
        var measurements = new List<EncounterPopulationMeasurement>(); int avoidable = 0;
        bool queriesPure = true, randomPreserved = true, terrainPreserved = true, unique = true;
        foreach (int seed in seeds)
        {
            var world = WorldSession.Create(seed, content); var reverse = WorldSession.Create(seed, content);
            string before = JsonSerializer.Serialize(world.State); int footprints = world.Chunks.Generator.CachedFootprints;
            var selections = world.Plan.Sites.SelectMany(site => world.EncounterPopulationPlan(site.Id)).ToArray();
            queriesPure &= before == JsonSerializer.Serialize(world.State) && footprints == world.Chunks.Generator.CachedFootprints;
            foreach (var site in selections.GroupBy(p => p.SiteId))
            {
                var used = new HashSet<string>(); string previous = "";
                foreach (var selected in site.OrderBy(p => p.Floor))
                {
                    if (used.Contains(selected.EncounterId) && selected.Eligible.Any(id => !used.Contains(id))
                        || previous == selected.EncounterId && selected.Eligible.Any(id => id != previous)) avoidable++;
                    used.Add(selected.EncounterId); previous = selected.EncounterId;
                }
            }
            uint combat = world.State.CombatRandom, incidents = world.State.IncidentRandom;
            string terrain = JsonSerializer.Serialize(world.State.Chunks);
            var reverseResults = selections.Reverse().Select(selection => reverse.EnsureEncounterPopulation(selection.SiteId, selection.Floor))
                .ToDictionary(r => (r.Record.SiteId, r.Record.Floor));
            foreach (var selected in selections)
            {
                var result = world.EnsureEncounterPopulation(selected.SiteId, selected.Floor);
                var repeated = world.EnsureEncounterPopulation(selected.SiteId, selected.Floor);
                var reordered = reverseResults[(selected.SiteId, selected.Floor)];
                var definition = content.Get<EncounterDefinition>(selected.EncounterId);
                bool ids = result.Actors.SequenceEqual(Enumerable.Range(0, definition.Members.Length).Select(i => selected.LeaderId + i));
                bool repeatable = !repeated.Created && result.Record == repeated.Record && result.Actors.SequenceEqual(repeated.Actors)
                    && result.Positions.SequenceEqual(repeated.Positions) && result.Record == reordered.Record && result.Positions.SequenceEqual(reordered.Positions);
                var site = world.Plan.Sites[selected.SiteId];
                bool clear = result.Positions.All(p => world.GroundOpen(p)
                    && p.Distance(site.Center.Offset(-2, 0) with { Floor = selected.Floor }) > 4
                    && (selected.Floor == 3 || p.Distance(site.Center.Offset(2, 0) with { Floor = selected.Floor }) > 4)
                    && world.State.Loot.Where(c => !c.Taken && c.SourceActor == 0).All(c => c.Position.Distance(p) > 2));
                measurements.Add(new(seed, selected, result.Record.Spawned, result.Positions,
                    Connected(world, selected, result.Positions), clear, ids, repeatable, result.Record.Failure));
            }
            randomPreserved &= combat == world.State.CombatRandom && incidents == world.State.IncidentRandom && world.State.Tick == 0;
            terrainPreserved &= terrain == JsonSerializer.Serialize(world.State.Chunks);
            unique &= world.State.Actors.Select(a => a.Id).Distinct().Count() == world.State.Actors.Count;
            var population = world.State.Actors.Where(a => a.Id is >= 70000 and < 80000).ToArray();
            unique &= population.Select(a => a.Position).Distinct().Count() == population.Length
                && population.All(a => !world.State.Actors.Any(other => other.Id != a.Id && other.Life != LifeState.Dead && other.Position == a.Position));
        }
        bool passed = contexts.All(c => c.Eligible.Length >= 2) && measurements.All(m => m.Spawned && m.Connected && m.Clear && m.StableIds && m.Repeatable)
            && avoidable == 0 && queriesPure && randomPreserved && terrainPreserved && unique;
        return new(passed, seeds, contexts, measurements.ToArray(), measurements.GroupBy(m => m.Selection.EncounterId).ToDictionary(g => g.Key, g => g.Count()),
            measurements.GroupBy(m => "depth-" + m.Selection.Floor + "/" + m.Selection.Tier).ToDictionary(g => g.Key, g => g.Count()),
            measurements.Count(m => m.Selection.Repeated), avoidable, queriesPure, randomPreserved, terrainPreserved, unique, timer.ElapsedMilliseconds,
            "Checks contextual coverage, fixed compositions and actual connected placement across both visitation orders. It does not prove balance or subjective variety; review the composition distribution and counterplay audit, then playtest. No player saves are used.");
    }

    private static bool Connected(WorldSession world, EncounterPopulationSelection selection, WorldPosition[] positions)
    {
        if (positions.Length == 0) return false;
        var site = world.Plan.Sites[selection.SiteId]; var footprint = world.Chunks.Generator.GenerateFootprint(false, site.Id, selection.Floor);
        const int size = WorldPlan.ChunkSize; var origin = site.Center.Offset(-32, -32) with { Floor = selection.Floor };
        var blocked = world.State.Loot.Where(c => !c.Taken && c.SourceActor == 0).Select(c => c.Position).ToHashSet();
        int start = 32 * size + 30; var reached = new HashSet<int> { start }; var queue = new Queue<int>(); queue.Enqueue(start);
        while (queue.TryDequeue(out int cell)) foreach (int delta in new[] { -1, 1, -size, size })
        {
            int next = cell + delta;
            if (next < 0 || next >= size * size || Math.Abs(next % size - cell % size) + Math.Abs(next / size - cell / size) != 1
                || !Materials.Walkable(footprint.Cells[next]) || footprint.Cells[next] == Material.Camp
                || blocked.Contains(origin.Offset(next % size, next / size)) || !reached.Add(next)) continue;
            queue.Enqueue(next);
        }
        return positions.All(p => reached.Contains((p.Y - origin.Y) * size + p.X - origin.X));
    }
}
