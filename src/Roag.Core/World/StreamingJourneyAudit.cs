using System.Security.Cryptography;
using System.Text.Json;
using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record StreamingStop(WorldPosition Target, bool Stairs = false);
public sealed record StreamingJourneyFixture(WorldPosition Start, WorldPosition Site, IReadOnlyList<StreamingStop> Stops);
public sealed record StreamingJourneyPoint(string Phase, long Tick, WorldPosition Position, int ResidentChunks, ChunkPreparationStatus Preparation, string Hash);
public sealed record StreamingJourneyReport(bool Passed, int Seed, int PartySize, int Steps, int SurfaceCrossings, int DeepCrossings, int StairTransfers,
    int PeakResidentChunks, int DemandMisses, int PreparedHits, IReadOnlyList<StreamingJourneyPoint> Checkpoints,
    AuditTickTiming Timing, string Scope);

/// <summary>Actual cardinal party steps and all stair floors, without waiting for speculative work.</summary>
public static class StreamingJourneyAudit
{
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException("Streaming journey: " + message); }
    private static string State(WorldSession world) => JsonSerializer.Serialize(world.State);
    private static void Command(WorldSession world, WorldCommand command)
    { var result = world.Submit(command); Require(result.Success, result.Message + " at " + world.State.Position + " toward " + command.Target); }
    public static StreamingJourneyFixture Prepare(WorldSession world)
    {
        var site = world.Plan.Sites.First(s => s.Center.X > 192 && s.Center.X < WorldPlan.Size - 192);
        var center = site.Center; var start = new WorldPosition(center.Chunk.X * 64 - 70, center.Y - 6);
        int turnX = (center.Chunk.X + 1) * 64 + 12;
        for (int floor = 1; floor < WorldPlan.Floors; floor++) world.EnsureEncounterPopulation(site.Id, floor);
        int parked = 0;
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited))
        {
            actor.Body.RestUntil = world.State.Tick + 10000;
            if (actor.Position.X < start.X - 20 || actor.Position.X > turnX + 20 || Math.Abs(actor.Position.Y - center.Y) > 20) continue;
            actor.Position = center.Offset(30 + parked % 8 * 2, 40 + parked / 8 * 2) with { Floor = actor.Position.Floor };
            actor.Life = LifeState.Downed; actor.Health = 0; parked++; world.Chunks.Edit(actor.Position, Material.Floor);
        }
        void Clear(WorldPosition p)
        { if (world.Chunks.At(p) is not (Material.StairsUp or Material.StairsDown)) world.Chunks.Edit(p, Material.Floor); }
        for (int floor = 0; floor < WorldPlan.Floors; floor++)
        {
            for (int y = -9; y <= -3; y++) for (int x = start.X - 4; x <= turnX + 4; x++) Clear(new(x, center.Y + y, floor));
            for (int y = -10; y <= 6; y++) for (int x = -6; x <= 6; x++) Clear(center.Offset(x, y) with { Floor = floor });
            for (int y = center.Chunk.Y - 1; y <= center.Chunk.Y + 1; y++)
                for (int x = start.Chunk.X - 1; x <= turnX / 64 + 1; x++)
                    if (x is >= 0 and < 64 && y is >= 0 and < 64) world.State.EcologyChunks.Add(new(x, y, floor));
        }
        // Recruitment uses its real rule; only the isolated fixture relocates recruits.
        for (int y = -4; y <= 4; y++) for (int x = -4; x <= 4; x++) Clear(world.State.Position.Offset(x, y));
        foreach (var actor in world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited && a.Life == LifeState.Alive).Take(3).ToArray())
        {
            actor.Position = world.State.Position.Offset(0, 1); world.Chunks.Edit(actor.Position, Material.Floor);
            Command(world, new(WorldCommandKind.Recruit, ActorId: actor.Id));
        }
        int member = 0;
        foreach (var actor in world.Party) { actor.Position = start.Offset(-member++, 0); actor.Body.RestUntil = 0; actor.Body.Hunger = actor.Body.Fatigue = 0; }
        world.State.Position = start; world.State.WorkRally = null; world.State.Inventory["food"] = 9999; world.Reveal();
        var stops = new List<StreamingStop> { new(center.Offset(1, -6)), new(center.Offset(1, 0)) };
        for (int floor = 0; floor < 3; floor++)
        {
            if (floor > 0) stops.Add(new(center.Offset(1, 0) with { Floor = floor }));
            stops.Add(new(center.Offset(2, 0) with { Floor = floor }, true));
        }
        stops.Add(new(center.Offset(-2, -6) with { Floor = 3 }));
        stops.Add(new(new(turnX, center.Y - 6, 3))); stops.Add(new(center.Offset(-2, -6) with { Floor = 3 }));
        for (int floor = 3; floor > 0; floor--)
        { stops.Add(new(center.Offset(-1, 0) with { Floor = floor })); stops.Add(new(center.Offset(-2, 0) with { Floor = floor }, true)); }
        stops.Add(new(center.Offset(1, -6))); stops.Add(new(start));
        world.Paused = true; return new(start, center, stops);
    }
    public static StreamingJourneyReport Run(ContentCatalog content, int seed)
    {
        var prepared = WorldSession.Create(seed, content); var plain = WorldSession.Create(seed, content);
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-streaming-journey-" + Guid.NewGuid().ToString("N"));
        try
        {
            var fixture = Prepare(prepared); Prepare(plain); plain.DetailedBodiesEverywhere = true;
            Require(State(prepared) == State(plain), "initial worlds differ");
            prepared.Paused = plain.Paused = false;
            var preparedMisses = new List<ChunkKey>(); var plainMisses = new List<ChunkKey>(); int hits = 0, checkedMisses = 0;
            void Observe(WorldSession w, List<ChunkKey> misses, bool streamed)
            {
                w.Chunks.DiagnosticSink = e =>
                {
                    if (e.Kind != "chunk-generation") return;
                    var data = JsonSerializer.SerializeToElement(e.Data); misses.Add(data.GetProperty("key").Deserialize<ChunkKey>());
                    if (streamed && data.GetProperty("prepared").GetBoolean()) hits++;
                };
            }
            Observe(prepared, preparedMisses, true); Observe(plain, plainMisses, false);
            var points = new List<StreamingJourneyPoint>(); var meter = new AuditTickMeter();
            int steps = 0, surface = 0, deep = 0, stairs = 0, peak = 0;
            long resources = prepared.AccountedResources(), flow = prepared.RegionalResourceFlow;
            void Check()
            {
                Require(preparedMisses.SequenceEqual(plainMisses), "terrain demand order changed");
                var keys = preparedMisses.Skip(checkedMisses).ToArray(); checkedMisses = preparedMisses.Count;
                foreach (var key in keys)
                {
                    var actual = prepared.Chunks.Get(key); var expected = plain.Chunks.Get(key);
                    Require(actual.BaseHash == expected.BaseHash && actual.Tiles.SequenceEqual(expected.Tiles)
                        && actual.Biomes.SequenceEqual(expected.Biomes) && actual.Cultures.SequenceEqual(expected.Cultures), "terrain arrays differ at " + key);
                }
                peak = Math.Max(peak, prepared.Chunks.ResidentCount);
                var status = prepared.Chunks.PreparationStatus;
                Require(prepared.Chunks.ResidentCount == plain.Chunks.ResidentCount && peak <= ChunkCache.Capacity
                    && prepared.Chunks.Generator.CachedFootprints <= 8 && status.Requested <= ChunkCache.PreparationCapacity && status.InFlight <= 1, "cache/preparation bounds changed");
                var party = prepared.Party.ToArray();
                Require(party.Length == 4 && party.Select(a => a.Position).Distinct().Count() == 4 && party.All(a => a.Position.Floor == prepared.State.Position.Floor), "party formation changed");
                Require(prepared.AccountedResources() == resources + prepared.RegionalResourceFlow - flow, "resource conservation changed");
            }
            void Checkpoint(string phase, bool reload)
            {
                Check(); string actual = State(prepared); Require(actual == State(plain), phase + " full state differs");
                points.Add(new(phase, prepared.State.Tick, prepared.State.Position, prepared.Chunks.ResidentCount, prepared.Chunks.PreparationStatus,
                    Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(actual)))));
                if (!reload) return;
                foreach (bool streamed in new[] { true, false })
                {
                    var w = streamed ? prepared : plain; string path = Path.Combine(directory, streamed ? "prepared.json" : "plain.json");
                    w.Save(path); var loaded = WorldSession.Load(path, content); loaded.Paused = false; loaded.DetailedBodiesEverywhere = !streamed;
                    Require(State(w) == State(loaded), phase + " reload changed state"); w.Dispose();
                    if (streamed) prepared = loaded; else plain = loaded;
                    Observe(loaded, streamed ? preparedMisses : plainMisses, streamed);
                }
            }
            Checkpoint("start", false);
            for (int index = 0; index < fixture.Stops.Count; index++)
            {
                var stop = fixture.Stops[index];
                while (prepared.State.Position.Distance(stop.Target) > (stop.Stairs ? 1 : 0))
                {
                    var before = prepared.State.Position;
                    var goal = before.Offset(Math.Clamp(stop.Target.X - before.X, -6, 6), Math.Clamp(stop.Target.Y - before.Y, -6, 6));
                    var path = prepared.Route(goal); Require(path.Count > 0, "no actual observed route from " + before + " toward " + goal);
                    var next = path[0]; Require(steps < 2000, "travel did not make bounded progress");
                    prepared.Chunks.PrepareView(before, before.X - 10, before.Y - 7, before.X + 10, before.Y + 7, next.X - before.X, next.Y - before.Y);
                    Command(prepared, new(WorldCommandKind.Move, next)); Command(plain, new(WorldCommandKind.Move, next)); steps++;
                    if (before.Chunk != next.Chunk) { if (before.Floor == 0) surface++; else deep++; }
                    Require(meter.Advance(prepared) == 1 && plain.AdvanceTicks(1) == 1, "travel clock froze"); Check();
                    if (steps == 25) Checkpoint("surface-walking", true);
                }
                if (stop.Stairs)
                {
                    var before = prepared.State.Position;
                    prepared.Chunks.PrepareView(before, before.X - 10, before.Y - 7, before.X + 10, before.Y + 7);
                    Command(prepared, new(WorldCommandKind.Interact, stop.Target)); Command(plain, new(WorldCommandKind.Interact, stop.Target)); stairs++;
                    Require(Math.Abs(prepared.State.Position.Floor - before.Floor) == 1, "stairs did not change floor");
                }
                Checkpoint("stop-" + index, stop.Stairs || index == 8);
            }
            Require(prepared.State.Position == fixture.Start && stairs == 6 && surface >= 4 && deep >= 2 && peak == ChunkCache.Capacity, "did not exercise the complete crossing/floor/eviction workload");
            Checkpoint("returned", true);
            return new(true, seed, 4, steps, surface, deep, stairs, peak, preparedMisses.Count, hits, points, meter.Report(),
                "Real four-person cardinal steps across surface chunks, descent/return through all four floors, deep chunk crossing and exact walking/floor saves. Background preparation receives normal views/movement prediction and is never awaited; synchronous fallback remains valid. Prepared and synchronous worlds compare every demanded terrain/biome/culture array, cache demand order, complete checkpoint state, resources and full body cadence. Explicit cleared corridors, downed/parked route threats, resting background roster, preobserved ecology chunks and ample pack food isolate streaming; not ecology, untouched-map accessibility or client FPS. Tick timing covers clock steps only, excludes movement commands/assertions/saves. No player slots, graphics or model calls.");
        }
        finally { prepared.Dispose(); plain.Dispose(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
