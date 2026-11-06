using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeLifeTests
{
    [Fact]
    public void PartyDowningAppliesRetainedShockAndMemoryOnceBeforePermanentDeath()
    {
        using var world = WorldSession.CreateNativeCampaign(42, ClassKind.Ranger);
        var hero = world.Party.Single(); var candidate = world.ActorById(world.NativeAdventurers[0].Actor);
        hero.Position = candidate.Position.Offset(-1, 0); world.Chunks.Edit(hero.Position, Material.Floor);
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.True(world.SubmitRoag(new("recruit", Target: candidate.Id)).Success);
        // Isolated physical fixture keeps the fall/death check separate from
        // live-town rescue availability; the rendered town journey is separate.
        hero.Position = new(100, 100); candidate.Position = new(101, 100);
        for (int y = 95; y <= 105; y++) for (int x = 95; x <= 105; x++) world.Chunks.Edit(new(x, y), Material.Floor);
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        int stress = hero.Stress;
        Kill(world, hero, candidate);
        Assert.Equal(LifeState.Downed, candidate.Life);
        var memory = Assert.Single(hero.Memories, m => m.Key == "fallen-friend" && m.Other == candidate.Id);
        Assert.Equal(Math.Min(100, stress + 18), hero.Stress);
        for (int i = 0; i < 20 && candidate.Life == LifeState.Downed; i++)
            Assert.True(world.SubmitRoag(new("wait")).Success);
        Assert.Equal(LifeState.Dead, candidate.Life);
        Assert.Equal(memory, Assert.Single(hero.Memories, m => m.Key == "fallen-friend" && m.Other == candidate.Id));
        Assert.Equal(Math.Min(100, stress + 18), hero.Stress);
        AssertContinuingSave(world);
    }

    [Fact]
    public void NativeCivilianDeathPreservesRelationshipMemoriesWithoutInventingCombatParticipation()
    {
        using var world = WorldSession.CreateNativeCampaign(42, ClassKind.Ranger);
        var victim = world.ActorById(1000);
        var friend = world.State.Actors.First(a => a.FriendId == victim.Id);
        var hero = world.Party.Single();
        hero.Position = victim.Position.Offset(0, -1); world.Chunks.Edit(hero.Position, Material.Floor);
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        int stress = friend.Stress, wounds = victim.Body.Wounds;
        Kill(world, hero, victim);
        Assert.Equal(LifeState.Dead, victim.Life); Assert.Null(victim.Tactics);
        Assert.DoesNotContain(victim.Id, world.State.Tactics!.Participants);
        Assert.Equal(Math.Min(10, wounds + 2), victim.Body.Wounds); // Heavy-hit injury plus the fall.
        var memory = Assert.Single(friend.Memories, m => m.Key == "fallen-friend" && m.Other == victim.Id);
        Assert.Equal(-8, memory.Mood); Assert.Equal(Math.Min(100, stress + 8), friend.Stress);
        Kill(world, hero, victim);
        Assert.Single(friend.Memories, m => m.Key == "fallen-friend" && m.Other == victim.Id);
        Assert.Equal(Math.Min(10, wounds + 2), victim.Body.Wounds);
        TacticalValidation.Validate(world.Plan, world.State);
        AssertContinuingSave(world);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeAnimalDeathLeavesItsActualAuthoredDropsOnceWithOptionalHabitat(bool settled)
    {
        using var world = WorldSession.CreateNativeCampaign(42, ClassKind.Ranger);
        var source = world.Content.Game.Wildlife.First(id => world.Content.Get<CharacterDefinition>(id).Ecology!.Drops.Count > 0);
        var hero = world.Party.Single(); var cell = hero.Position.Offset(1, 0);
        world.Chunks.Edit(cell, Material.Floor);
        var animal = ActorFactory.Create(world.Content, source, world.Seed, world.State.NextActorId++, cell, 0);
        if (settled) animal.Habitat = animal.NatalHabitat = cell;
        world.State.Actors.Add(animal); world.DungeonCombat.RegisterRetainedEnemy(animal, world.DungeonCombat.Clock + 1000);
        int wounds = animal.Body.Wounds;
        Kill(world, hero, animal);
        Assert.Equal(LifeState.Dead, animal.Life); Assert.Equal(Math.Min(10, wounds + 2), animal.Body.Wounds);
        var remains = Assert.Single(world.State.Loot, c => c.SourceActor == animal.Id);
        Assert.Equal(cell, remains.Position);
        var rules = world.Ecology(animal)!;
        foreach (var (item, amount) in rules.Drops)
            Assert.Equal(amount, remains.Contents[world.Content.Get<ItemDefinition>(item).Stack]);
        Assert.Equal(rules.Drops.Count, remains.Contents.Count);
        Assert.Equal(settled, world.State.WildlifePopulations.ContainsKey(animal.Id));
        if (settled)
        {
            var population = world.State.WildlifePopulations[animal.Id];
            Assert.Equal(1, population.Deaths); Assert.Equal(WildlifeRecovery.Recovering, population.Recovery);
            Assert.Equal(world.State.Tick + WorldSession.WildlifeRecoveryTicks, population.RecoverAt);
        }
        string before = JsonSerializer.Serialize(new { world.State.Loot, world.State.WildlifePopulations });
        Kill(world, hero, animal);
        Assert.Single(world.State.Loot, c => c.SourceActor == animal.Id);
        Assert.Equal(Math.Min(10, wounds + 2), animal.Body.Wounds);
        // A later shot does not duplicate remains or change the ecology death receipt.
        Assert.Equal(before, JsonSerializer.Serialize(new { world.State.Loot, world.State.WildlifePopulations }));
        TacticalValidation.Validate(world.Plan, world.State);
        // World saves require ecology animals to have an observed habitat. The
        // unowned controller fixture only checks defensive death resolution.
        if (settled) AssertContinuingSave(world);
    }

    private static void AssertContinuingSave(WorldSession world)
    {
        string folder = Path.Combine(Path.GetTempPath(), "roag-native-life-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(folder, "campaign.json"); world.Save(slot);
            using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(world.SubmitRoag(new("wait")).Success); Assert.True(loaded.SubmitRoag(new("wait")).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static void Kill(WorldSession world, Actor hero, Actor target)
    {
        var combat = world.DungeonCombat;
        combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [target.Position],
            "core:ability/ranger_shot", AttackDelivery.Projectile, DamageElement.Physical,
            target.MaxHealth + target.Armor + 10, combat.Clock));
    }
}
