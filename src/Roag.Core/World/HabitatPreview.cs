using Roag.Core.Content;

namespace Roag.Core.World;

public static class HabitatPreview
{
    /// <summary>Isolated approaches and an explicitly almost depleted wild-food stock.</summary>
    public static void Prepare(WorldSession world)
    {
        var field = JourneyPreview.PrepareFarField(world); world.State.Position = world.Party.Single().Position = field;
        world.Reveal(); int parked = 0;
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited))
        {
            actor.Body.RestUntil = world.State.Tick + 2000;
            if (actor.Position.Distance(field) > 100) continue;
            actor.Position = (world.Plan.Towns[actor.HomeTown].Center with { Floor = actor.Position.Floor }).Offset(30 + parked++ * 2, 30);
            world.Chunks.Edit(actor.Position, Material.Floor);
        }
        world.State.Habitats[WorldSession.HabitatIndex(field.Chunk)] = new() { Food = 2, Epoch = world.State.Tick / WorldSession.TicksPerHour };
        world.Paused = true;
    }
    public static object Run(ContentCatalog content, int seed)
    {
        using var world = WorldSession.Create(seed, content); Prepare(world);
        var before = world.HabitatAt(world.State.Position); int food = world.Count("food");
        if (!world.Submit(new(WorldCommandKind.Forage)).Success) throw new InvalidDataException("Habitat forage did not queue.");
        world.Paused = false;
        for (int i = 0; i < 100 && world.State.Work.Count > 0; i++) world.AdvanceTicks(1);
        var depleted = world.HabitatAt(world.State.Position);
        if (world.State.Work.Count != 0 || world.Count("food") != food + 2 || depleted.Food != 0 || depleted.Pressure != 8)
            throw new InvalidDataException("Forage did not consume the actual wild-food stock.");
        string rejection = world.Submit(new(WorldCommandKind.Forage)).Message;
        if (world.WildFoodUnavailable(world.State.Position).Length == 0) throw new InvalidDataException("Depleted forage was still available.");
        world.AdvanceTicks(WorldSession.TicksPerHour * 2);
        var recovered = world.HabitatAt(world.State.Position);
        if (recovered.Food < 2 || recovered.Pressure != 0 || !world.Submit(new(WorldCommandKind.Forage)).Success)
            throw new InvalidDataException("Habitat did not recover with actual elapsed world time.");
        return new { passed = true, seed, before, depleted, recovered, rejection,
            scope = "Actual forage, finite stock, no-cost depleted rejection and elapsed recovery. Explicit low-stock and cleared-approach fixture; no player saves, graphics or model calls. Migration/population recovery remain a separate subsequent step." };
    }
}
