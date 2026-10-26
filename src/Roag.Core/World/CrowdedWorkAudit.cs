using System.Security.Cryptography;
using System.Text.Json;
using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record CrowdedWorkCheckpoint(long Tick, int Pending, int Carrying, long Stone, string Hash);
public sealed record CrowdedWorkReport(bool Passed, int Seed, int Queued, int PartySize, int Blockers, long Ticks,
    long ExtractedStone, int BlockedTicks, IReadOnlyList<string> SavedPhases, AuditTickTiming Timing,
    IReadOnlyList<CrowdedWorkCheckpoint> Checkpoints, string Scope);

public static class CrowdedWorkAudit
{
    private static string State(WorldSession world) => JsonSerializer.Serialize(world.State);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    private static void Command(WorldSession world, WorldCommand command)
    { var result = world.Submit(command); Require(result.Success, "Crowded work: " + result.Message); }
    public static CrowdedWorkReport Run(ContentCatalog catalog, int seed)
    {
        var world = WorldSession.Create(seed, catalog);
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-crowded-work-" + Guid.NewGuid().ToString("N"));
        try
        {
            var origin = world.State.Position; int parked = 0;
            foreach (var actor in world.State.Actors)
            {
                actor.Body.RestUntil = 10000; actor.Body.Hunger = actor.Body.Fatigue = 0;
                if (actor.Recruited || actor.Position.Distance(origin) > 100) continue;
                actor.Position = origin.Offset(120 + parked++ * 2, 20); world.Chunks.Edit(actor.Position, Material.Floor);
            }
            for (int y = -15; y <= 15; y++) for (int x = -15; x <= 15; x++) world.Chunks.Edit(origin.Offset(x, y), Material.Floor);
            foreach (var actor in world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(3).ToArray())
            {
                actor.Position = origin.Offset(0, -1); Command(world, new(WorldCommandKind.Recruit, ActorId: actor.Id));
            }
            int member = 0;
            foreach (var actor in world.Party) { actor.Position = origin.Offset(-member++, 0); actor.Body.RestUntil = 0; actor.Body.Hunger = actor.Body.Fatigue = 0; }
            world.State.Inventory["food"] = 9999; world.Reveal();
            var targets = Enumerable.Range(-4, 8).SelectMany(y => Enumerable.Range(2, 8).Select(x => origin.Offset(x, y))).ToArray();
            foreach (var target in targets) world.Chunks.Edit(target, Material.Rock);
            for (int y = -15; y <= 15; y++) world.Chunks.Edit(origin.Offset(1, y), Material.Wall);
            var blockers = world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(12).ToArray();
            for (int i = 0; i < blockers.Length; i++)
            {
                blockers[i].Position = i == 0 ? origin.Offset(1, 0) : origin.Offset(-7 + i % 4, 5 + i / 4);
                blockers[i].Life = LifeState.Downed; blockers[i].Health = 0;
            }
            world.Chunks.Edit(origin.Offset(1, 0), Material.Floor); world.Reveal();
            Require(world.DesignateWork(WorldCommandKind.Dig, targets).Queued == 64, "Could not queue all 64 observed jobs.");
            long[] orders = world.State.Work.Select(o => o.Id).ToArray();
            long initialStone = world.AccountedMaterial("stone"), resources = world.AccountedResources(), flow = world.RegionalResourceFlow;
            string path = Path.Combine(directory, "world.json"); world.Save(path);
            using var detailed = WorldSession.Load(path, catalog); detailed.DetailedBodiesEverywhere = true;
            world.Paused = detailed.Paused = false;
            var phases = new List<string>(); var points = new List<CrowdedWorkCheckpoint>(); var meter = new AuditTickMeter();
            long suspended = 0; int suspendedAt = 0, blockedTicks = 0; double heldProgress = 0;
            bool walkingSaved = false, workSaved = false, carryingSaved = false;
            void Reload(string phase)
            {
                world.Save(path); var loaded = WorldSession.Load(path, catalog); loaded.Paused = false;
                Require(State(world) == State(loaded), "Crowded " + phase + " reload changed state.");
                world.Dispose(); world = loaded; phases.Add(phase);
            }
            Reload("blocked");
            for (int tick = 1; tick <= 4000 && world.State.Work.Count > 0; tick++)
            {
                if (tick == 50)
                    foreach (var w in new[] { world, detailed }) w.ActorById(blockers[0].Id).Position = origin.Offset(-10, 10);
                if (tick == 150)
                    foreach (var w in new[] { world, detailed }) for (int y = -2; y <= 2; y++) w.Chunks.Edit(origin.Offset(1, y), Material.Floor);
                if (suspended != 0 && tick == suspendedAt + 25)
                {
                    Require(world.State.Work.Single(o => o.Id == suspended).Progress == heldProgress, "Suspended work advanced.");
                    foreach (var w in new[] { world, detailed }) Command(w, new(WorldCommandKind.ConfigureWork, WorkId: suspended, Item: "resume"));
                }
                Require(meter.Advance(world) == 1 && detailed.AdvanceTicks(1) == 1, "Crowded simulation froze.");
                Require(world.State.Work.Count <= 64, "Work queue exceeded its bound.");
                Require(world.AccountedResources() == resources + world.RegionalResourceFlow - flow, "Crowded regional resources diverged.");
                int extracted = targets.Count(p => world.Chunks.At(p) == Material.Earth);
                Require(world.AccountedMaterial("stone") == initialStone + extracted * 5L, "Crowded extraction lost or duplicated stone.");
                var party = world.Party.ToArray();
                Require(party.Select(a => a.Position).Distinct().Count() == 4 && party[0].Position == world.State.Position,
                    "Crowded workers overlapped or lost the leader.");
                if (tick < 50)
                {
                    Require(world.State.Work.All(o => o.Progress == 0 && o.Output == null), "Blocked workers made progress through an occupant."); blockedTicks++;
                }
                if (!walkingSaved && tick > 50 && world.Party.Any(a => a.Position.X > origin.X)) { walkingSaved = true; Reload("walking"); }
                if (!workSaved && world.State.Work.FirstOrDefault(o => o.Progress > 0 && o.Progress < o.Required) is { } started)
                {
                    suspended = started.Id; suspendedAt = tick; heldProgress = started.Progress;
                    foreach (var w in new[] { world, detailed }) Command(w, new(WorldCommandKind.ConfigureWork, WorkId: suspended, Item: "suspend"));
                    workSaved = true; Reload("suspended-work");
                }
                if (!carryingSaved && world.State.Work.Any(o => o.Output != null)) { carryingSaved = true; Reload("hauling"); }
                if (tick % 25 == 0 || world.State.Work.Count == 0)
                {
                    string actual = State(world); Require(actual == State(detailed), "Crowded scheduled/full-cadence state differs at " + tick + ".");
                    points.Add(new(world.State.Tick, world.State.Work.Count, world.State.Work.Count(o => o.Output != null), world.AccountedMaterial("stone"), Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(actual)))));
                }
            }
            Require(world.State.Work.Count == 0 && targets.All(p => world.Chunks.At(p) == Material.Earth), "64 jobs did not finish within 4000 actual ticks: " + string.Join("; ", world.State.Work.Select(o => o.Status).Distinct()));
            Require(walkingSaved && workSaved && carryingSaved, "Did not exercise all interrupted phases.");
            Require(orders.Length == 64 && world.AccountedMaterial("stone") == initialStone + 320, "Wrong final extraction total.");
            Reload("completed");
            return new(true, seed, 64, 4, blockers.Length, world.State.Tick, 320, blockedTicks, phases, meter.Report(), points,
                "Real 64-order excavation, maximum four-person party, twelve downed obstructing people, actual output cargo/delivery, suspend/resume and exact blocked/walking/work/hauling/completed saves against full body cadence. Scripted cleared arena, background rest, gate/blocker removal and food supplies isolate the workload; no player saves, graphics or model calls. Not a survival/balance fixture.");
        }
        finally { world.Dispose(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
