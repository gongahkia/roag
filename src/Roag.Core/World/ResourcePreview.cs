using Roag.Core.Content;

namespace Roag.Core.World;

/// <summary>Locates natural seeded resources, then exercises the same work commands as the mouse client.</summary>
public static class ResourcePreview
{
    public static WorldPosition Find(WorldSession world, string id)
    {
        var definition = world.Content.Get<ResourceDefinition>(id);
        IEnumerable<WorldPosition> centers = definition.Underground ? world.Plan.Sites.Select(s => s.Center with { Floor = 1 })
            : world.Plan.Towns.Select(t => t.Center).Concat(world.Plan.BiomeRegions.Where(b => definition.Biomes.Contains(b.Biome)).Select(b => new WorldPosition(b.X, b.Y)));
        var visited = new HashSet<ChunkKey>();
        foreach (var center in centers)
            for (int radius = 0; radius <= 2; radius++)
                for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
                {
                    var key = new ChunkKey(center.Chunk.X + dx, center.Chunk.Y + dy, center.Floor);
                    if (!visited.Add(key) || key.X is < 0 or >= 64 || key.Y is < 0 or >= 64) continue;
                    var chunk = world.Chunks.Get(key);
                    for (int index = 0; index < 4096; index++)
                    {
                        if (chunk.Tiles[index] != definition.Terrain || !definition.Biomes.Contains(chunk.Biomes[index])) continue;
                        var p = new WorldPosition(key.X * 64 + index % 64, key.Y * 64 + index / 64, key.Floor);
                        if (world.ResourceAt(p)?.Definition.Id == id) return p;
                    }
                }
        throw new InvalidDataException("No natural resource found in the bounded preview sample: " + id);
    }
    public static WorldPosition Prepare(WorldSession world, string id)
    {
        var p = Find(world, id);
        // A local fixture clears only the approach, preserving the naturally generated resource tile.
        var leader = world.Party.First(); var origin = WorldSession.Directions.Select(d => p.Offset(d.X, d.Y)).First(world.Plan.Contains);
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited && a.Position.Distance(p) < 100))
            actor.Position = world.Plan.Towns[(actor.HomeTown + 12) % 24].Center;
        world.Chunks.Edit(origin, Material.Floor); leader.Position = origin; world.State.Position = origin;
        world.State.WorkRally = origin; world.Paused = false; world.Chunks.Prefetch(origin); world.Reveal();
        return p;
    }
    public static object Run(ContentCatalog catalog, string id, int seed)
    {
        var world = WorldSession.Create(seed, catalog); var target = Prepare(world, id);
        var node = world.ResourceAt(target)!; var before = new Dictionary<string, int>(world.State.Inventory);
        var queued = world.DesignateWork(WorldCommandKind.Harvest, [target]);
        for (int t = 0; t < 400 && world.State.Work.Count > 0; t++) world.AdvanceTicks(1);
        return new { seed, target, definition = id, node.Definition.Activity, queued, ticks = world.State.Tick,
            remainingOrders = world.State.Work.Count, before = node.Available, after = world.ResourceAt(target)!.Available,
            yielded = world.State.Inventory.Where(i => i.Value != before.GetValueOrDefault(i.Key)).ToDictionary(i => i.Key, i => i.Value - before.GetValueOrDefault(i.Key)),
            recovery = world.ResourceAt(target)!.Description, workDone = world.Party.First().Body.WorkDone };
    }
}
