using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeHuntingTests
{
    private static (WorldSession World, Actor Animal, Facing Facing) Prepare(long tick = 0)
    {
        var world = NativeSocietyTests.AtTick(tick); var hero = world.Party.Single();
        var habitat = WildlifePreview.Find(world, "character.rabbit");
        hero.Position = world.State.Position = habitat.Home; world.Reveal();
        var animal = world.State.Actors.Single(a => a.Habitat == habitat.Home);
        var approach = new[] { Facing.North, Facing.East, Facing.South, Facing.West }.Select(f =>
        {
            var (x, y) = AttackGeometry.Offset(f);
            return (Position: animal.Position.Offset(-x, -y), Facing: f);
        }).First(p => p.Position != habitat.Home && world.CanEnter(p.Position, hero.Id));
        hero.Position = world.State.Position = approach.Position;
        world.Reveal();
        return (world, animal, approach.Facing);
    }

    [Fact]
    public void AConfirmedPaidAttackProvokesActualTimidWildlifeAndItRetaliatesOnItsOwnTimeline()
    {
        var (world, animal, facing) = Prepare(); using var dispose = world;
        var hero = world.Party.Single(); animal.Tactics!.Invulnerable = true;
        Assert.False(world.IsHostile(animal)); Assert.Null(world.DungeonCombat.SelectEnemyTarget(animal.Id));
        string before = JsonSerializer.Serialize(world.State);
        var intent = new TacticalIntent(TacticalAction.BasicAttack, facing);
        Assert.True(world.DungeonCombat.Preview(intent).Valid);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.SubmitDungeon(intent).Success);
        Assert.Equal(75, animal.ProvokedUntil); Assert.True(world.IsHostile(animal));
        Assert.True(world.DungeonCombat.IsLocallyEngaged(hero.Id));
        Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "HostilityProvoked" && e.Source == hero.Id && e.Target == animal.Id);
        Assert.True(animal.Tactics.Enemy!.Retained!.Actions > 0);
        Assert.True(hero.Health < hero.MaxHealth);
        Assert.Contains("Provoked by hunting", world.WildlifeDescription(animal));
        Assert.Null(world.State.Battle); Assert.False(world.State.RoagCombat);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void AFirstLethalHuntCreditsItsActualContributorBeforeSettlingTheDeathOnce()
    {
        var (world, animal, _) = Prepare(); using var dispose = world;
        var hero = world.Party.Single(); var build = hero.Tactics!.Build!;
        long before = build.Experience;
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, hero.Id, hero.Position,
            [animal.Position], "core:ability/adventurer_strike", AttackDelivery.Melee, DamageElement.Physical,
            animal.MaxHealth + animal.Armor + 10, world.DungeonCombat.Clock));
        Assert.Equal(LifeState.Dead, animal.Life);
        Assert.True(hero.Tactics.Build!.Experience > before || hero.Tactics.Build.Level > build.Level);
        Assert.Single(world.State.Loot, c => c.SourceActor == animal.Id);
        Assert.Single(world.State.Tactics.Events, e => e.Kind == "ExperienceGained" && e.Target == animal.Id && e.Source == hero.Id);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ATargetedOwnedSkillCanInitiateHuntingWithoutAutomaticAreaAttacksSelectingTimidWildlife()
    {
        var (world, animal, _) = Prepare(); using var dispose = world;
        Assert.True(world.ChooseDungeonClass(ClassKind.Ranger).Success); var hero = world.Party.Single();
        string mark = RetainedBoons.Id("ability.hunters-mark"), volley = RetainedBoons.Id("ability.volley");
        hero.Tactics!.Boons.Inventory = hero.Tactics.Boons.Inventory.Grant(mark, BoonRarity.Common, ClassKind.Ranger)
            .Grant(volley, BoonRarity.Common, ClassKind.Ranger);
        Assert.False(world.IsHostile(animal));
        var automatic = world.DungeonCombat.Preview(new(TacticalAction.RetainedSkill, Item: volley));
        Assert.DoesNotContain(animal.Position, automatic.Cells);
        var intent = new TacticalIntent(TacticalAction.RetainedSkill, Target: animal.Id, Item: mark);
        string before = JsonSerializer.Serialize(world.State);
        Assert.True(world.DungeonCombat.Preview(intent).Valid);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.SubmitDungeon(intent).Success);
        Assert.True(world.IsHostile(animal));
        Assert.Contains(animal.Tactics!.Conditions, c => c.Kind == NativeConditionKind.Mark && c.Source == hero.Id);
    }

    [Theory]
    [InlineData(DamageCause.TriggeredEffect)] [InlineData(DamageCause.OngoingEffect)]
    public void ActualPartyOwnedEffectDamageProvokesWithoutInventingAnAttackerObservation(DamageCause cause)
    {
        var (world, animal, _) = Prepare(); using var dispose = world;
        var hero = world.Party.Single(); hero.Position = world.State.Position = hero.Position with { Floor = 1 };
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, hero.Id, hero.Position,
            [animal.Position], "core:ability/ranger_shot", AttackDelivery.Area, DamageElement.Physical, 1,
            world.DungeonCombat.Clock, cause, PersistsWithoutOwner: true));
        Assert.Equal(75, animal.ProvokedUntil); Assert.Null(world.DungeonCombat.SelectEnemyTarget(animal.Id));
        Assert.Null(animal.Tactics!.Enemy!.Awareness);
        Assert.DoesNotContain(animal.Position, world.Visible);
    }

    [Fact]
    public void ARealWallOrAnAttackOnEmptyGroundDoesNotProvokeTheAnimal()
    {
        var (world, animal, _) = Prepare(); using var dispose = world;
        var hero = world.Party.Single();
        world.Chunks.Edit(animal.Position, Material.Wall);
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, hero.Id, hero.Position,
            [animal.Position], "core:ability/ranger_shot", AttackDelivery.Projectile, DamageElement.Physical, 1, world.DungeonCombat.Clock));
        var empty = new[] { Facing.North, Facing.East, Facing.South, Facing.West }.Select(f =>
        {
            var (x, y) = AttackGeometry.Offset(f); return hero.Position.Offset(x, y);
        }).First(p => world.CanEnter(p, hero.Id));
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics.NextAction++, hero.Id, hero.Position,
            [empty], "core:ability/ranger_shot", AttackDelivery.Projectile, DamageElement.Physical, 1, world.DungeonCombat.Clock));
        Assert.Equal(0, animal.ProvokedUntil); Assert.False(world.IsHostile(animal));
        Assert.DoesNotContain(world.State.Tactics.Events, e => e.Kind == "HostilityProvoked");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EnvironmentalOrUnrelatedCreatureDamageDoesNotInventHuntingByTheParty(bool environmental)
    {
        var (world, animal, _) = Prepare(); using var dispose = world;
        var other = world.State.Actors.First(a => a.Kind == ActorKind.Raider && a.Tactics?.Enemy != null);
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, environmental ? 0 : other.Id,
            animal.Position.Offset(1, 0), [animal.Position], "core:ability/ranger_shot", AttackDelivery.Area,
            DamageElement.Physical, 1, world.DungeonCombat.Clock));
        Assert.Equal(0, animal.ProvokedUntil); Assert.False(world.IsHostile(animal));
        Assert.DoesNotContain(world.State.Tactics.Events, e => e.Kind == "HostilityProvoked");
    }

    [Fact]
    public void ProvocationDoesNotBorrowVisionOrPermitPursuitOutsideTheActualHomeLeash()
    {
        var (world, animal, _) = Prepare(); using var dispose = world;
        var hero = world.Party.Single();
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, hero.Id, hero.Position,
            [animal.Position], "core:ability/ranger_shot", AttackDelivery.Projectile, DamageElement.Physical, 1, world.DungeonCombat.Clock));
        hero.Position = world.State.Position = animal.Position.Offset(2, 0);
        world.Chunks.Edit(hero.Position, Material.Floor); world.Chunks.Edit(animal.Position.Offset(1, 0), Material.Wall);
        world.Reveal();
        Assert.True(world.IsHostile(animal)); Assert.Null(world.DungeonCombat.SelectEnemyTarget(animal.Id));
        Assert.Null(animal.Tactics!.Enemy!.Awareness);
        hero.Position = world.State.Position = animal.Habitat!.Value.Offset(world.Ecology(animal)!.Leash + 1, 0);
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        Assert.False(world.IsHostile(animal)); Assert.Null(world.DungeonCombat.SelectEnemyTarget(animal.Id));
    }

    [Fact]
    public void AnExistingProvocationExpiresOnPaidWorldTimeAndContinuesExactlyThroughASave()
    {
        var (world, animal, _) = Prepare(74); using var dispose = world;
        animal.ProvokedUntil = 75; Assert.True(world.IsHostile(animal));
        string root = Path.Combine(Path.GetTempPath(), "roag-native-hunting-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            for (int i = 0; i < 12; i++)
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            }
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(75, world.State.Tick); Assert.False(world.IsHostile(animal));
            Assert.Equal(75, animal.ProvokedUntil);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
