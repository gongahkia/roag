using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;

namespace Roag.Core.World;

public static class JourneyPreview
{
    // A separate empty field lets the observer leave without activating another
    // settlement. Only this isolated developer fixture edits its ground.
    public static WorldPosition PrepareFarField(WorldSession world)
    {
        var origin = world.State.Position;
        foreach (int radius in new[] { 160, 224, 288, 352 })
            foreach (int y in new[] { -radius, radius }) foreach (int x in new[] { -radius, radius })
            {
                var p = origin.Offset(x, y);
                if (!world.Plan.Contains(p.Offset(-8, -8)) || !world.Plan.Contains(p.Offset(8, 8))
                    || world.State.Actors.Any(a => a.Position.Distance(p) <= 120) || world.Plan.Roads.Any(r => r.Contains(p, 32))) continue;
                for (int dy = -8; dy <= 8; dy++) for (int dx = -8; dx <= 8; dx++) world.Chunks.Edit(p.Offset(dx, dy), Material.Floor);
                return p;
            }
        throw new InvalidDataException("No isolated journey-observation field found.");
    }
    public static object Run(ContentCatalog content, int seed)
    {
        string root = Path.Combine(Path.GetTempPath(), "vagabonds-journey-preview-" + Guid.NewGuid().ToString("N"));
        try
        {
            var cases = new List<object>();
            foreach (bool rest in new[] { false, true })
            {
                using var near = WorldSession.Create(seed, content); var person = RoutinePreview.Prepare(near);
                // The near observer stays outside the trip's corridor. Removing
                // a person from the route would legitimately change its path.
                near.State.Position = near.Party.Single().Position = near.State.Position.Offset(-25, -25);
                var farField = PrepareFarField(near);
                if (rest) { person.Body.Hunger = 0; person.Body.Fatigue = 80; }
                near.Paused = false; near.AdvanceTicks(9);
                string path = Path.Combine(root, rest ? "rest.json" : "food.json"); near.Save(path);
                using var far = WorldSession.Load(path, content); far.Paused = false;
                if (JsonSerializer.Serialize(near.State) != JsonSerializer.Serialize(far.State)) throw new InvalidDataException("Journey load changed the saved state.");
                far.State.Position = far.Party.Single().Position = farField;
                var remote = far.ActorById(person.Id); long arrival = 0; var checkpoints = new List<object>();
                for (int i = 0; i < 450; i++)
                {
                    var previous = remote.Position;
                    if (near.AdvanceTicks(1) != 1 || far.AdvanceTicks(1) != 1 || previous.Distance(remote.Position) > 1)
                        throw new InvalidDataException("Journey stopped or moved more than one tile at " + far.State.Tick + ".");
                    if (JsonSerializer.Serialize(person) != JsonSerializer.Serialize(remote))
                        throw new InvalidDataException($"Near/far { (rest ? "rest" : "food") } journey differs at {far.State.Tick}: "
                            + Difference(JsonSerializer.SerializeToNode(person), JsonSerializer.SerializeToNode(remote)));
                    if (arrival == 0 && (rest ? remote.Body.RestUntil > far.State.Tick : remote.Body.Hunger < 10))
                    {
                        if (remote.Position.Distance(remote.Routine!.Target) > 1) throw new InvalidDataException("Resident recovered before arriving.");
                        arrival = far.State.Tick;
                    }
                    if (i % 100 == 0 || i == 449) checkpoints.Add(new { far.State.Tick, remote.Position, remote.Routine, remote.Body.Hunger,
                        remote.Body.Fatigue, remote.Body.RestUntil, bounds = far.ResidentJourneys, residentChunks = far.Chunks.ResidentCount });
                }
                if (arrival == 0 || far.ResidentJourneys.RetainedSteps > far.State.Actors.Count * WorldSession.ResidentJourneyLimit)
                    throw new InvalidDataException("Journey did not arrive or exceeded its retained path bound.");
                far.Save(path); using var resumed = WorldSession.Load(path, content); resumed.Paused = false;
                if (JsonSerializer.Serialize(far.State) != JsonSerializer.Serialize(resumed.State)) throw new InvalidDataException("Offscreen journey save changed state.");
                for (int i = 0; i < 100; i++)
                {
                    far.AdvanceTicks(1); resumed.AdvanceTicks(1);
                    if ((i % 13 == 0 || i == 99) && JsonSerializer.Serialize(far.State) != JsonSerializer.Serialize(resumed.State)) throw new InvalidDataException("Offscreen saved continuation differs.");
                }
                cases.Add(new { kind = rest ? "Rest" : "Food", person.Id, arrivalTick = arrival, farField, checkpoints, exactInterruptedSave = true });
            }
            return new { passed = true, seed, cases, scope = "Real near/far resident ticks, cardinal journeys, arrival before food/rest, bounded routes and exact interrupted saves. Isolated fields and temporary saves; no graphics, player saves or model calls." };
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private static string Difference(JsonNode? left, JsonNode? right, string path = "actor")
    {
        if (left is JsonObject a && right is JsonObject b)
            foreach (var key in a.Select(p => p.Key).Union(b.Select(p => p.Key)))
                if (!JsonNode.DeepEquals(a[key], b[key])) return Difference(a[key], b[key], path + "." + key);
        if (left is JsonArray x && right is JsonArray y)
            for (int i = 0; i < Math.Min(x.Count, y.Count); i++)
                if (!JsonNode.DeepEquals(x[i], y[i])) return Difference(x[i], y[i], path + "[" + i + "]");
        return path + ": " + left?.ToJsonString() + " / " + right?.ToJsonString();
    }
}
