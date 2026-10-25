using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed record BodyAuditCheckpoint(long Tick, string ScheduledHash, string DetailedHash, long Resources, int Actors, int QueuedDeadlines);
public sealed record BodyAuditSeed(int Seed, int Ticks, long ScheduledVisits, long DetailedVisits, double ScheduledMs, double DetailedMs,
    bool SaveContinued, IReadOnlyList<BodyAuditCheckpoint> Checkpoints);
public sealed record BodyAuditReport(bool Passed, int SchemaVersion, string ContentHash, IReadOnlyList<BodyAuditSeed> Seeds, string Scope);

/// <summary>Real tick pipelines compared against full body cadence, including an interrupted save.</summary>
public static class BodyScheduleAudit
{
    public static BodyAuditReport Run(ContentCatalog catalog, int[] seeds, int ticks = 7200)
    {
        if (seeds.Length is < 1 or > 16 || seeds.Distinct().Count() != seeds.Length) throw new ArgumentException("Choose 1..16 distinct seeds.");
        if (ticks is < 50 or > 720_000) throw new ArgumentOutOfRangeException(nameof(ticks), "Choose 50..720000 ticks.");
        var reports = new List<BodyAuditSeed>();
        foreach (int seed in seeds) reports.Add(RunSeed(catalog, seed, ticks));
        return new(true, 1, catalog.Hash, reports,
            "Headless real world ticks, scheduled versus full body cadence. Includes hunger/rest/forage pressure, non-hour expiry, near/far moves, live mutations, exact save interruption and resource conservation. Visit counts are exact; paired elapsed times include JIT/scheduling and are not FPS predictions. No player saves, graphics or model calls.");
    }
    private static BodyAuditSeed RunSeed(ContentCatalog catalog, int seed, int ticks)
    {
        var scheduled = WorldSession.Create(seed, catalog); using var detailed = WorldSession.Create(seed, catalog);
        detailed.DetailedBodiesEverywhere = true;
        Prepare(scheduled); Prepare(detailed);
        long initial = scheduled.AccountedResources(), produced = scheduled.State.ResourcesProduced,
            consumed = scheduled.State.ResourcesConsumed, lost = scheduled.State.ResourcesLost;
        long completedVisits = 0; double scheduledMs = 0, detailedMs = 0; bool resumed = false;
        var checkpoints = new List<BodyAuditCheckpoint>();
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-body-audit-" + Guid.NewGuid().ToString("N"));
        try
        {
            for (int tick = 1; tick <= ticks; tick++)
            {
                Mutate(scheduled, tick); Mutate(detailed, tick);
                if (tick % 17 == 0) foreach (var actor in scheduled.State.Actors) { _ = actor.Body.Hunger; _ = actor.Body.Fatigue; }
                long started = Stopwatch.GetTimestamp(); int a = scheduled.AdvanceTicks(1); scheduledMs += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                started = Stopwatch.GetTimestamp(); int b = detailed.AdvanceTicks(1); detailedMs += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                if (a != 1 || b != 1) throw new InvalidDataException($"Body audit seed {seed} froze at {tick}; combat/end-state coverage is not silently skipped.");
                if (scheduled.BodySimulation.QueuedDeadlines > scheduled.State.Actors.Count) throw new InvalidDataException("Body deadlines exceeded actor count.");
                if (tick is 17 or 23 or 50 || tick % 300 == 0 || tick == ticks || tick == ticks / 2)
                {
                    string actual = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(scheduled.State)));
                    string expected = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(detailed.State)));
                    if (actual != expected) throw new InvalidDataException($"Body audit seed {seed} diverged at {tick}: {actual} / {expected}.");
                    long resources = scheduled.AccountedResources();
                    if (resources != initial + scheduled.State.ResourcesProduced - produced - (scheduled.State.ResourcesConsumed - consumed) - (scheduled.State.ResourcesLost - lost))
                        throw new InvalidDataException($"Body audit seed {seed} violated resource conservation at {tick}.");
                    checkpoints.Add(new(tick, actual, expected, resources, scheduled.State.Actors.Count, scheduled.BodySimulation.QueuedDeadlines));
                }
                if (tick == ticks / 2)
                {
                    string file = Path.Combine(directory, "world.json"); scheduled.Save(file);
                    var loaded = WorldSession.Load(file, catalog); loaded.Paused = false;
                    if (JsonSerializer.Serialize(scheduled.State) != JsonSerializer.Serialize(loaded.State))
                    { loaded.Dispose(); throw new InvalidDataException($"Body audit seed {seed}: immediate saved continuation differed."); }
                    completedVisits += scheduled.BodySimulation.Processed; scheduled.Dispose(); scheduled = loaded; resumed = true;
                }
            }
            return new(seed, ticks, completedVisits + scheduled.BodySimulation.Processed, detailed.BodySimulation.Processed,
                scheduledMs, detailedMs, resumed, checkpoints);
        }
        finally { scheduled.Dispose(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    private static Actor Subject(WorldSession world) => world.State.Actors.First(a => a.Kind == ActorKind.Person && a.HomeTown == 1);
    private static void Prepare(WorldSession world)
    {
        var actor = Subject(world); actor.Body.Hunger = world.Simulation.MealAt - .01; actor.Body.Fatigue = 74.99;
        actor.Memories.Add(new("brief-thought", "A brief thought", 0, 17, 0));
        var camp = world.Content.Get<CampDefinition>("camp.apple-broth"); actor.Preparations.Add(new(camp.Id, camp.Effect!, camp.Power, 23));
        world.State.Towns[1].Stocks["food"] = 0;
        var forager = world.State.Actors.First(a => a.Kind == ActorKind.Person && a.HomeTown == 2);
        forager.Body.Hunger = 100; world.State.Towns[2].Stocks["food"] = 0;
        world.Paused = false;
    }
    private static void Mutate(WorldSession world, int tick)
    {
        var actor = Subject(world);
        if (tick is 80 or 180)
        {
            var center = tick == 80 ? world.State.Position : world.Plan.Towns[1].Center;
            for (int y = -4; y <= 4; y++) for (int x = -4; x <= 4; x++)
            {
                var p = center.Offset(x, y);
                if (world.GroundOpen(p) && world.OccupantAt(p, actor.Id) == null) { actor.Position = p; return; }
            }
            throw new InvalidDataException("No valid near/far audit fixture cell.");
        }
        if (tick == 200) { actor.Level = 8; actor.TraitId = world.Party.First().TraitId; }
        if (tick == 250) actor.Body.RestUntil = world.State.Tick + 3;
        if (tick == 270) actor.Life = LifeState.Downed;
        if (tick == 280) actor.Life = LifeState.Alive;
        if (tick == 310) actor.Memories = [new("replacement", "A new thought", world.State.Tick, world.State.Tick + 7, 0)];
        if (tick == 411) actor.Body = new() { Hunger = 34.99, Fatigue = 20, WorkDone = 5 };
    }
}
