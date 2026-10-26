using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record MigrationFixture(long Actor, WorldPosition Origin);
public static class MigrationPreview
{
    public static MigrationFixture Prepare(WorldSession world, string species = "character.rabbit")
    {
        var actor = WildlifePreview.Prepare(world, species); var home = actor.Habitat!.Value;
        var observer = home.Offset(7, 5); world.Chunks.Edit(observer, Material.Floor);
        world.State.Position = world.Party.Single().Position = observer;
        foreach (var other in world.State.Actors.Where(a => !a.Recruited && a.Id != actor.Id)) other.Body.RestUntil = world.State.Tick + 3000;
        actor.Body.Hunger = actor.Body.Fatigue = 0; actor.Body.RestUntil = 0;
        world.State.Habitats[WorldSession.HabitatIndex(home.Chunk)] = new() { Food = 0, Pressure = 800, Epoch = world.State.Tick / WorldSession.TicksPerHour };
        world.Reveal(); world.Paused = false; world.AdvanceTicks(5); world.Paused = true;
        if (actor.Migration == null) throw new InvalidDataException("Pressure did not start an actual wildlife migration.");
        return new(actor.Id, home);
    }
    public static object Run(ContentCatalog content, int seed)
    {
        using var world = WorldSession.Create(seed, content); var fixture = Prepare(world); var actor = world.ActorById(fixture.Actor);
        long candidates = world.WildlifeMigrationCandidates; int population = world.State.Actors.Count, steps = 0;
        var positions = new List<WorldPosition> { actor.Position }; world.Paused = false;
        for (int i = 0; i < 3000 && actor.Habitat == fixture.Origin; i++)
        {
            var previous = actor.Position; long before = world.WildlifeMigrationCandidates;
            if (world.AdvanceTicks(1) != 1 || previous.Distance(actor.Position) > 1 || world.WildlifeMigrationCandidates - before > WorldSession.WildlifeSearchBudget)
                throw new InvalidDataException("Migration froze, teleported or exceeded its search budget.");
            if (previous != actor.Position) { steps++; positions.Add(actor.Position); }
        }
        if (actor.Habitat == fixture.Origin || actor.Migration != null || actor.NatalHabitat != fixture.Origin || world.State.Actors.Count != population)
            throw new InvalidDataException("The original wildlife identity did not finish migrating: " + world.WildlifeDescription(actor));
        return new { passed = true, seed, fixture, destination = actor.Habitat, actor.Id, world.State.Tick, steps,
            candidateVisits = world.WildlifeMigrationCandidates - candidates, retainedRouteLimit = WorldSession.ResidentJourneyLimit, positions,
            scope = "A real seeded animal seeks and walks to a natural habitat using pressure/food, shared bounded searches and live paths. Explicit initial pressure and cleared source approach; no duplicate population, player saves, graphics or model calls. Population recovery remains separate." };
    }
}
