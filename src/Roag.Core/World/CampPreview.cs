using Roag.Core.Content;

namespace Roag.Core.World;

public static class CampPreview
{
    public static WorldPosition Prepare(WorldSession world)
    {
        var origin = world.State.Position;
        int parked = 0;
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited && a.Position.Distance(origin) < 100))
        { actor.Position = origin.Offset(40 + parked++, 0); world.Chunks.Edit(actor.Position, Material.Floor); }
        for (int y = -8; y <= 8; y++) for (int x = -8; x <= 8; x++) world.Chunks.Edit(origin.Offset(x, y), Material.Floor);
        foreach (var actor in world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(3).ToArray())
        { actor.Position = origin.Offset(1, 0); world.Submit(new(WorldCommandKind.Recruit, ActorId: actor.Id)); }
        var camp = origin.Offset(0, -1); world.Chunks.Edit(camp, Material.Camp);
        foreach (var item in world.Content.Game.CampActivities.Select(world.Content.Get<CampDefinition>).SelectMany(d => d.Cost.Keys).Distinct())
            world.State.Inventory[world.Content.Get<ItemDefinition>(item).Stack] = 12;
        foreach (var actor in world.Party) { actor.Body.Wounds = 2; actor.Health = Math.Max(1, actor.Health - 12); actor.Body.Fatigue = 15; }
        world.Paused = false; world.Reveal(); return camp;
    }
    public static object Run(ContentCatalog catalog, string id, int seed)
    {
        var world = WorldSession.Create(seed, catalog); var camp = Prepare(world);
        var result = world.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: world.State.Party[0], Item: id));
        for (int t = 0; t < 500 && world.State.Work.Count > 0; t++) world.AdvanceTicks(1);
        return new { seed, camp, result, ticks = world.State.Tick, remainingOrders = world.State.Work.Count,
            party = world.Party.Select(a => new { a.Name, a.Health, a.Body.Wounds, a.Body.WorkDone, a.Preparations }),
            world.State.LastCampStory, world.State.KnownSites, inventory = world.State.Inventory };
    }
}
