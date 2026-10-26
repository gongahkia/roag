using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record WeatherFixture(long Resident, WorldPosition Camp, WorldPosition Home, long ChangeAt, WeatherKind Weather);
public static class WeatherPreview
{
    public static long Find(WorldSession world, WorldPosition p, Func<ClimateConditions, bool> predicate, long minimum = 0)
    {
        long first = minimum / WorldSession.WeatherPeriod;
        for (long epoch = first; epoch < first + 480; epoch++)
        {
            long tick = epoch * WorldSession.WeatherPeriod + 10;
            if (tick >= minimum && predicate(WorldSession.ClimateAt(world.Plan, p, tick))) return tick;
        }
        throw new InvalidDataException("Requested climate absent from this surface region.");
    }
    public static WeatherFixture Prepare(WorldSession world, bool nearChange = true)
    {
        var actor = RoutinePreview.Prepare(world); var center = world.Plan.Towns[actor.HomeTown].Center;
        var home = center.Offset((int)(actor.Id % 5) * 3 - 6, -18); var observer = home.Offset(4, 3); var camp = observer.Offset(0, -1);
        actor.Position = home.Offset(4, 0); actor.Body.Hunger = actor.Body.Fatigue = 0; actor.Body.RestUntil = 0; actor.Routine = null;
        world.State.Position = world.Party.Single().Position = observer;
        world.State.Tick = Find(world, observer, c => c.Severe && !WorldSession.ClimateAt(world.Plan, observer, c.Until).Severe, world.State.Tick);
        foreach (var other in world.State.Actors.Where(a => !a.Recruited && a.Id != actor.Id)) other.Body.RestUntil = world.State.Tick + WorldSession.WeatherPeriod + 1000;
        world.Reveal(); var result = world.Submit(new(WorldCommandKind.Build, camp, Material: Material.Camp));
        if (!result.Success) throw new InvalidDataException(result.Message);
        world.Paused = false;
        for (int i = 0; i < 120 && world.State.Work.Count > 0; i++) world.AdvanceTicks(1);
        if (world.State.Work.Count > 0 || world.Chunks.At(camp) != Material.Camp || !world.Sheltered(actor))
            throw new InvalidDataException("Actual camp construction or the resident's shelter journey did not finish.");
        long change = world.WeatherAt(observer).Until;
        if (nearChange) world.State.Tick = change - 20;
        world.Reveal(); world.Paused = true; return new(actor.Id, camp, home, change, world.WeatherAt(observer).Weather);
    }
    public static object Run(ContentCatalog content, int seed)
    {
        using var world = WorldSession.Create(seed, content); var fixture = Prepare(world, nearChange: false); var hero = world.Party.Single();
        var start = world.State.Position; var weather = world.WeatherAt(start); double before = hero.Body.Fatigue;
        if (!world.Submit(new(WorldCommandKind.Move, start.Offset(1, 0))).Success) throw new InvalidDataException("Weather preview step blocked.");
        double exposure = hero.Body.Fatigue - before;
        if (Math.Abs(exposure - weather.TravelFatigue) > 1e-9 || !world.Submit(new(WorldCommandKind.Move, start)).Success
            || !world.Sheltered(hero) || !world.Submit(new(WorldCommandKind.Forage)).Success) throw new InvalidDataException("Real travel/covered gathering did not use climate conditions.");
        long balance = world.AccountedResources(), flow = world.RegionalResourceFlow; world.Paused = false;
        while (world.State.Tick < fixture.ChangeAt)
        {
            if (world.AdvanceTicks(1) != 1 || world.AccountedResources() != balance + world.RegionalResourceFlow - flow)
                throw new InvalidDataException("Weather transition froze or violated regional conservation.");
        }
        if (world.WeatherAt(start).Severe || world.State.Work.Count > 0) throw new InvalidDataException("Actual weather change or covered gathering failed.");
        return new { passed = true, seed, fixture, before = weather, after = world.WeatherAt(start), exposedStepFatigue = exposure,
            resident = world.RoutineDescription(world.ActorById(fixture.Resident)), world.State.Tick,
            scope = "Isolated cleared home approaches and an explicit severe-weather clock. Actual camp construction, walked shelter arrival, exposed/covered steps, gathering and real ticks through the next six-hour change with regional conservation. No player saves, graphics or model calls; not seasonal balance evidence." };
    }
}
