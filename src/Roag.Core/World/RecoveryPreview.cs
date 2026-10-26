using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record RecoveryFixture(long Population, long OriginalActor, WorldPosition Home);
public static class RecoveryPreview
{
    public static RecoveryFixture Prepare(WorldSession world, bool nearDeadline = true)
    {
        var animal = WildlifePreview.Prepare(world, "character.rabbit"); var hero = world.Party.Single();
        hero.Role = 0; hero.BaseSpeed = 30; hero.BasePower = 100;
        world.StartBattle(animal.Id);
        if (!world.Act(0, 1).Success || animal.Life != LifeState.Dead) throw new InvalidDataException("Recovery fixture did not complete an actual hunt.");
        var population = world.State.WildlifePopulations[animal.WildlifePopulation];
        if (nearDeadline) world.State.Tick = population.RecoverAt - 50;
        var observer = animal.Habitat!.Value.Offset(7, 5); world.Chunks.Edit(observer, Material.Floor);
        world.State.Position = hero.Position = observer;
        foreach (var actor in world.State.Actors) actor.Body.RestUntil = world.State.Tick + WorldSession.WildlifeRecoveryTicks + 2000;
        world.Reveal(); world.Paused = true; return new(animal.WildlifePopulation, animal.Id, animal.Habitat.Value);
    }
    public static object Run(ContentCatalog content, int seed)
    {
        using var world = WorldSession.Create(seed, content); var fixture = Prepare(world, nearDeadline: false);
        var population = world.State.WildlifePopulations[fixture.Population]; long deadline = population.RecoverAt;
        int food = world.Count("food"), gold = world.Count("gold"), experience = world.Party.Single().Experience, count = world.State.Actors.Count;
        long balance = world.AccountedResources(), flow = world.RegionalResourceFlow;
        world.Paused = false; long attempts = world.WildlifeRecoveryAttempts;
        for (int i = 0; i < WorldSession.WildlifeRecoveryTicks + 100 && population.Generation == 0; i++)
        {
            long before = world.WildlifeRecoveryAttempts;
            if (world.AdvanceTicks(1) != 1 || world.WildlifeRecoveryAttempts - before > WorldSession.WildlifeRecoveryBudget
                || world.AccountedResources() != balance + world.RegionalResourceFlow - flow) throw new InvalidDataException("Recovery froze, exceeded its budget or lost regional accounting.");
            if (world.State.Tick < deadline && population.Generation != 0) throw new InvalidDataException("Wildlife returned before the quiet period.");
        }
        var animal = world.ActorById(population.Actor);
        if (population.Generation != 1 || animal.Id == fixture.OriginalActor || animal.Habitat != fixture.Home || animal.Life != LifeState.Alive
            || world.State.Actors.Count != count + 1 || world.State.Loot.Count(c => c.SourceActor == fixture.OriginalActor) != 1
            || world.Count("food") > food || world.Count("gold") != gold || world.Party.Single().Experience != experience || world.State.RewardedEncounterIds.Contains(animal.Id))
            throw new InvalidDataException("Population recovery duplicated an identity, reward or material.");
        return new { passed = true, seed, fixture, deadline, world.State.Tick, animal = animal.Id, generation = population.Generation,
            foodSpent = WorldSession.WildlifeBirthFood, attempts = world.WildlifeRecoveryAttempts - attempts, history = population.History,
            scope = "Actual hunting and a full three-day sequence of real world ticks, bounded recovery, distinct identity, conserved regional resources and persistent original remains. Isolated source approach/resting roster; no player saves, graphics or model calls." };
    }
}
