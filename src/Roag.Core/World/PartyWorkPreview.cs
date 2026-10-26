using Roag.Core.Content;

namespace Roag.Core.World;

public static class PartyWorkPreview
{
    public static object Run(ContentCatalog catalog, int seed)
    {
        var world = WorldSession.Create(seed, catalog); var origin = world.State.Position;
        int parked = 0;
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited && a.Position.Distance(origin) < 100))
        { actor.Position = origin.Offset(40 + parked++, 0); world.Chunks.Edit(actor.Position, Material.Floor); }
        for (int y = -10; y <= 10; y++) for (int x = -10; x <= 10; x++) world.Chunks.Edit(origin.Offset(x, y), Material.Floor);
        foreach (var actor in world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(3).ToArray())
        { actor.Position = origin.Offset(1, 0); world.Submit(new(WorldCommandKind.Recruit, ActorId: actor.Id)); }
        world.State.Inventory["food"] = 100;
        var targets = new[] { origin.Offset(-6, 0), origin.Offset(6, 0), origin.Offset(0, -6), origin.Offset(0, 6) };
        foreach (var p in targets) world.Chunks.Edit(p, Material.Rock);
        world.Reveal(); world.DesignateWork(WorldCommandKind.Dig, targets); world.Paused = false;
        int maxWorkers = 0; bool collisionFree = true, bounded = true;
        for (int i = 0; i < 200 && world.State.Work.Count > 0; i++)
        {
            world.AdvanceTicks(1);
            maxWorkers = Math.Max(maxWorkers, world.State.Work.Count(o => o.Worker != 0));
            collisionFree &= world.Party.Select(a => a.Position).Distinct().Count() == world.State.Party.Count;
            bounded &= world.Party.All(a => a.Position.Distance(origin) <= WorldSession.WorkRallyRadius);
        }
        return new { seed, maxWorkers, collisionFree, bounded, remainingOrders = world.State.Work.Count,
            completed = targets.All(p => world.Chunks.At(p) == Material.Earth), ticks = world.State.Tick, workers = world.Party.Select(a => new { a.Name, a.Body.WorkDone, a.Position }) };
    }
}
