using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public static class RoutinePreview
{
    /// <summary>Isolated readable plaza; uses real actor identities and tick handlers.</summary>
    public static Actor Prepare(WorldSession world)
    {
        var p = world.State.Position; int parked = 0;
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited && a.Position.Distance(p) <= 100))
        {
            actor.Position = p.Offset(110 + parked++ * 2, 20); world.Chunks.Edit(actor.Position, Material.Floor);
        }
        for (int y = -25; y <= 25; y++) for (int x = -25; x <= 25; x++) world.Chunks.Edit(p.Offset(x, y), Material.Floor);
        var person = world.State.Actors.First(a => a.Kind == ActorKind.Person && a.HomeTown == 0 && !a.Recruited);
        person.Position = p.Offset(4, 0); person.Body.Hunger = 40; person.Body.Fatigue = 0;
        person.Body.ForageProgress = 0; person.Body.RestUntil = 0; world.Reveal();
        world.Paused = false; world.AdvanceTicks(1); world.Paused = true; return person;
    }
    public static object Run(ContentCatalog content, int seed)
    {
        using var world = WorldSession.Create(seed, content); var actor = Prepare(world); world.Paused = false;
        object Observe() => new { world.State.Tick, actor.Position, actor.Routine, actor.Body.Hunger, actor.Body.Fatigue, actor.Body.RestUntil,
            description = world.RoutineDescription(actor), food = world.State.Towns[0].Stocks["food"] };
        object seekingFood = Observe(); int food = world.State.Towns[0].Stocks["food"];
        world.AdvanceTicks(20); object afterMeal = Observe(); bool ate = world.State.Towns[0].Stocks["food"] == food - 1 && actor.Body.Hunger < 10;
        actor.Body.Fatigue = 80; world.AdvanceTicks(5); object goingHome = Observe();
        int waited = 0; while (actor.Body.RestUntil <= world.State.Tick && waited++ < 200) world.AdvanceTicks(1);
        bool reachedHome = actor.Body.RestUntil > world.State.Tick && actor.Position.Distance(actor.Routine!.Target) <= 1;
        object resting = Observe(); double fatigue = actor.Body.Fatigue; world.AdvanceTicks(20);
        return new { passed = ate && reachedHome && actor.Body.Fatigue < fatigue, seed, actor.Id, actor.Name, seekingFood, afterMeal, goingHome, resting,
            recovered = Observe(), scope = "Isolated real food/rest destinations and ticks; not regional logistics or long-term balance." };
    }
}
