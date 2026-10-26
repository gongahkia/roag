using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record EcologyStressPoint(long Tick, int Generation, int Deaths, int History, int Actors, int Populations, string Hash);
public sealed record EcologyStressReport(bool Passed, int Seed, int Ticks, long Population, WorldPosition Origin, WorldPosition MigratedHome,
    int Hunts, int Births, int Reclaimed, bool RemainsBlocked, long RecoveryAttempts, long MigrationCandidates,
    IReadOnlyList<string> SavedPhases, IReadOnlyList<EcologyStressPoint> Checkpoints, AuditMeasurements Measurements, string Scope);

/// <summary>Elapsed ecology, actual hunts/loot, retained remains and scheduled/reference continuation.</summary>
public static class EcologyStressAudit
{
    private static string State(WorldSession world) => JsonSerializer.Serialize(world.State);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException("Ecology stress: " + message); }
    public static EcologyStressReport Run(ContentCatalog content, int seed, int ticks = 720000, string? failureDirectory = null)
    {
        if (ticks is < 50400 or > 720000) throw new ArgumentOutOfRangeException(nameof(ticks), "Choose 50400..720000 actual ticks (7..100 days).");
        var world = WorldSession.Create(seed, content);
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-ecology-stress-" + Guid.NewGuid().ToString("N"));
        try
        {
            var fixture = MigrationPreview.Prepare(world); var original = world.ActorById(fixture.Actor);
            var near = world.State.Position; var far = JourneyPreview.PrepareFarField(world);
            world.Chunks.Edit(far with { Floor = 1 }, Material.Floor);
            foreach (var actor in world.State.Actors.Where(a => a.Id != original.Id)) actor.Body.RestUntil = world.State.Tick + ticks + 5000;
            var hero = world.Party.Single(); hero.Role = 0; hero.BaseSpeed = 30; hero.BasePower = 100;
            world.State.Inventory["food"] = 9999;
            long start = world.State.Tick, finish = start + ticks;
            string path = Path.Combine(directory, "world.json"); world.Save(path);
            using var detailed = WorldSession.Load(path, content); detailed.DetailedBodiesEverywhere = true;
            world.Paused = detailed.Paused = false;
            long balance = world.AccountedResources(), flow = world.RegionalResourceFlow;
            long allocated = GC.GetTotalAllocatedBytes(); int[] collections = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
            var meter = new AuditTickMeter(); var referenceMeter = new AuditTickMeter(); var growth = new List<AuditGrowth>();
            var points = new List<EcologyStressPoint>(); var phases = new List<string>();
            int hunts = 0, reclaimed = 0, generation = 0; bool migrated = false, blocked = false;
            long root = 0, bornAt = 0, blockedUntil = 0, attempts = 0, candidates = 0;
            WorldPosition home = fixture.Origin;
            var retained = new HashSet<long>();
            WildlifePopulation? Population() => root == 0 ? null : world.State.WildlifePopulations[root];
            void Compare()
            {
                string actual = State(world), expected = State(detailed);
                if (actual != expected)
                {
                    if (failureDirectory != null)
                    {
                        Directory.CreateDirectory(failureDirectory);
                        File.WriteAllText(Path.Combine(failureDirectory, "scheduled-state.json"), actual);
                        File.WriteAllText(Path.Combine(failureDirectory, "detailed-state.json"), expected);
                    }
                    Require(false, "scheduled/full-cadence state differs at " + world.State.Tick + ": "
                        + Difference(JsonNode.Parse(actual), JsonNode.Parse(expected)));
                }
                Require(world.AccountedResources() == balance + world.RegionalResourceFlow - flow, "regional resources differ at " + world.State.Tick);
                var population = Population();
                points.Add(new(world.State.Tick, population?.Generation ?? 0, population?.Deaths ?? 0, population?.History.Count ?? 0,
                    world.State.Actors.Count, world.State.WildlifePopulations.Count, Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(actual)))));
            }
            void Reload(string phase)
            {
                world.Save(path); var loaded = WorldSession.Load(path, content); loaded.Paused = false;
                Require(State(world) == State(loaded), phase + " reload changed state"); world.Dispose(); world = loaded;
                phases.Add(phase); Compare();
            }
            void Hunt(WorldSession w, long id)
            {
                var hunter = w.Party.Single(); hunter.Health = hunter.MaxHealth; hunter.Mana = hunter.MaxMana;
                w.StartBattle(id); var result = w.Act(0, 1);
                Require(result.Success && w.ActorById(id).Life == LifeState.Dead && !w.IsInCombat, "actual hunt failed: " + result.Message);
                w.Paused = false;
            }
            Reload("migration"); growth.Add(SimulationAuditMetrics.Capture(world, path, allocated, collections));
            bool walkingSaved = false, recoveredSaved = false, reclaimedSaved = false;
            while (world.State.Tick < finish)
            {
                long beforeAttempts = world.WildlifeRecoveryAttempts, beforeCandidates = world.WildlifeMigrationCandidates;
                Require(meter.Advance(world) == 1 && referenceMeter.Advance(detailed) == 1, "world froze at " + world.State.Tick);
                long usedAttempts = world.WildlifeRecoveryAttempts - beforeAttempts, usedCandidates = world.WildlifeMigrationCandidates - beforeCandidates;
                Require(usedAttempts <= WorldSession.WildlifeRecoveryBudget && usedCandidates <= WorldSession.WildlifeSearchBudget, "ecology exceeded its per-tick budget");
                attempts += usedAttempts; candidates += usedCandidates;
                if (!walkingSaved && world.ActorById(fixture.Actor).Journey?.Steps.Count > 0) { walkingSaved = true; Reload("walking"); }
                if (!migrated)
                {
                    var animal = world.ActorById(fixture.Actor);
                    if (animal.Habitat != fixture.Origin && animal.Migration == null && animal.WildlifePopulation > 0)
                    {
                        migrated = true; root = animal.WildlifePopulation; home = animal.Habitat!.Value;
                        Require(animal.NatalHabitat == fixture.Origin, "migration lost natal identity");
                        Hunt(world, animal.Id); Hunt(detailed, animal.Id); hunts++; retained.Add(animal.Id); Reload("pending-recovery");
                    }
                }
                if (Population() is { } population)
                {
                    Require(population.Home == home && population.Origin == fixture.Origin, "population did not follow its migrated home");
                    Require(population.History.Count <= WorldSession.WildlifeHistoryLimit, "history exceeded its limit");
                    var current = world.ActorById(population.Actor);
                    if (world.State.Tick % 50 == 0)
                        Require(world.State.Actors.Count(a => a.WildlifePopulation == root && a.Life == LifeState.Alive) == (current.Life == LifeState.Alive ? 1 : 0), "population duplicated a living identity");
                    if (population.Generation > generation)
                    {
                        Require(population.Generation == generation + 1 && current.Id != fixture.Actor && !world.State.RewardedEncounterIds.Contains(current.Id), "birth duplicated identity/reward");
                        Require(world.State.Tick >= population.LastDeath + WorldSession.WildlifeRecoveryTicks, "birth skipped its quiet period");
                        generation = population.Generation; bornAt = world.State.Tick;
                        int removed = retained.RemoveWhere(id => !world.State.Actors.Any(a => a.Id == id)); reclaimed += removed;
                        foreach (long id in retained) Require(world.State.Loot.Any(c => c.SourceActor == id), "uncollected remains disappeared");
                        if (!recoveredSaved) { recoveredSaved = true; Reload("recovered"); }
                        if (removed > 0 && !reclaimedSaved) { reclaimedSaved = true; Reload("reclaimed"); }
                    }
                    if (population.Recovery == WildlifeRecovery.Remains)
                    {
                        Require(population.History.All(id => world.State.Loot.Any(c => c.SourceActor == id)), "blocked history lost remains");
                        if (!blocked) { blocked = true; blockedUntil = world.State.Tick + WorldSession.TicksPerDay; Reload("retained-remains"); }
                        if (world.State.Tick >= blockedUntil)
                        {
                            long id = population.History[0]; var cache = world.State.Loot.Single(c => c.SourceActor == id);
                            if (!cache.Taken)
                            {
                                var side = WorldSession.Directions.Select(d => cache.Position.Offset(d.X, d.Y)).First(p => p != population.Home && p != fixture.Origin && world.Plan.Contains(p));
                                foreach (var w in new[] { world, detailed })
                                {
                                    if (!w.GroundOpen(side)) w.Chunks.Edit(side, Material.Floor);
                                    w.State.Position = w.Party.Single().Position = side;
                                    var result = w.Submit(new(WorldCommandKind.Loot, cache.Position)); Require(result.Success, "real remains collection failed: " + result.Message);
                                }
                                Compare();
                            }
                        }
                    }
                    if (current.Life == LifeState.Alive && world.State.Tick - bornAt >= WorldSession.TicksPerHour * 2)
                    {
                        long id = current.Id; Hunt(world, id); Hunt(detailed, id); hunts++; retained.Add(id); Compare();
                    }
                }
                long elapsed = world.State.Tick - start;
                if (elapsed % WorldSession.TicksPerDay == 0)
                {
                    var observer = (elapsed / WorldSession.TicksPerDay % 3) switch { 0 => near, 1 => far, _ => far with { Floor = 1 } };
                    foreach (var w in new[] { world, detailed }) w.State.Position = w.Party.Single().Position = observer;
                }
                if (elapsed % 300 == 0 || world.State.Tick >= finish) Compare();
                if (SimulationAuditMetrics.GrowthCheckpoint((int)elapsed, ticks)) growth.Add(SimulationAuditMetrics.Capture(world, path, allocated, collections));
            }
            Require(migrated && walkingSaved && recoveredSaved && hunts >= 2 && generation >= 2, "elapsed migration/hunting/recovery phases were not exercised");
            if (ticks >= 30 * WorldSession.TicksPerDay) Require(blocked && reclaimed > 0 && reclaimedSaved, "long run did not exercise blocked and reclaimed history");
            Reload("completed");
            Require(world.AccountedResources() == balance + world.RegionalResourceFlow - flow, "final accounting changed");
            return new(true, seed, (int)(world.State.Tick - start), root, fixture.Origin, home, hunts, generation, reclaimed, blocked, attempts, candidates,
                phases, points, new(meter.Report(), referenceMeter.Report(), growth),
                "Real elapsed 7..100 days, seeded migration followed by repeated actual hunts and adjacent loot commands, near/far/other-floor observer placements, full-cadence comparison and exact migration/walking/pending/recovered/history saves. Four retained untaken remains deliberately delay recovery for a real day before actual collection permits safe reclamation. Explicit initial pressure, cleared approaches, resting background roster, strong hunter and ample pack food isolate ecology; observer placements are fixture relocations, not walking/travel proof. Tick metrics exclude assertions, saves and combat commands. No player slots, graphics or model calls; not natural unassisted population balance.");
        }
        finally { world.Dispose(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    private static string Difference(JsonNode? left, JsonNode? right, string path = "state")
    {
        if (left is JsonObject a && right is JsonObject b)
            foreach (string key in a.Select(p => p.Key).Union(b.Select(p => p.Key)))
                if (!JsonNode.DeepEquals(a[key], b[key])) return Difference(a[key], b[key], path + "." + key);
        if (left is JsonArray x && right is JsonArray y)
            for (int i = 0; i < Math.Min(x.Count, y.Count); i++)
                if (!JsonNode.DeepEquals(x[i], y[i])) return Difference(x[i], y[i], path + "[" + i + "]");
        return path + ": " + left?.ToJsonString() + " / " + right?.ToJsonString();
    }
}
