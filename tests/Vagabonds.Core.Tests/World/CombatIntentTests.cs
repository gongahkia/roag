using System.Text.Json;
using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class CombatIntentTests
{
    private static WorldSession Ready(int seed = 42)
    {
        var world = WorldSession.Create(seed); CooperationPreview.Prepare(world);
        var battle = world.State.Battle!;
        foreach (var unit in battle.Units)
        {
            unit.Health = unit.MaxHealth = 300;
            var actor = world.State.Actors.Single(a => a.Id == battle.ActorIds[unit.Id]);
            actor.BaseHealth = actor.Health = 300; actor.Body.Wounds = 0;
        }
        var enemy = battle.Units.First(u => !u.Friendly); enemy.Power = 20;
        battle.Order = [enemy.Id, .. battle.Units.Where(u => u.Friendly).Select(u => u.Id)]; battle.TurnIndex = 0;
        battle.Plans[enemy.Id] = new("ability.strike", 0);
        return world;
    }
    private static void EquipShield(WorldSession world, int unitId)
    {
        var item = world.Content.Get<ItemDefinition>("item.wardens-shield");
        world.State.Actors.Single(a => a.Id == world.State.Battle!.ActorIds[unitId]).Equipment[item.Slot] = new(item.Id, item.Health, item.Power, item.Armor, item.Speed);
    }
    [Fact]
    public void AuthoredSkillsCarryStableIdsIndependentOfDisplayNames()
    {
        foreach (var definition in ContentCatalog.Default.All<AbilityDefinition>())
        {
            var skill = definition.AsSkill(); Assert.Equal(definition.Id, skill.Id);
            Assert.Equal(skill.Id, (skill with { Name = "Localized ability name" }).Id);
        }
        Assert.Empty(Skills.Guard.Id); // Existing legacy constructors retain their compatible default.
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)] [InlineData(-100)]
    public void IntentPreservesStateAndShowsActualGuardArmourMarkStressDamage(int seed)
    {
        var world = Ready(seed); var b = world.State.Battle!; var enemy = b.Active; var target = b.Units[0];
        enemy.Stress = 70; target.Guarding = true; target.Mark = 2;
        string before = JsonSerializer.Serialize(world.State); var intent = world.EnemyActionPreview(enemy.Id)!;
        Assert.Equal(before, JsonSerializer.Serialize(world.State)); Assert.Empty(intent.Reason);
        Assert.Equal("ability.strike", intent.AbilityId); Assert.NotEmpty(intent.Icon);
        var forecast = Assert.Single(intent.Targets); Assert.Equal(b.ActorIds[0], forecast.ActorId);
        Assert.Equal(target.Sprite, forecast.Sprite); Assert.Contains("current defenses", intent.Description);
        int hp = target.Health; Assert.True(world.AdvanceEnemy().Success);
        Assert.InRange(hp - target.Health, forecast.Minimum, forecast.Maximum);
        int raw = world.Content.Get<AbilityDefinition>(intent.AbilityId).Power + enemy.Power - 6 + 4 - 2 - target.Armor;
        Assert.Equal(Math.Max(1, raw / 2), forecast.Minimum); Assert.Equal(Math.Max(1, (raw + 3) / 2), forecast.Maximum);
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)] [InlineData(-100)]
    public void GroupIntentUsesEachTargetsDefensesAndSpendsShieldCoverOnlyOnce(int seed)
    {
        var world = Ready(seed); var b = world.State.Battle!; var enemy = b.Active;
        b.Plans[enemy.Id] = new("ability.sweep", 0); EquipShield(world, 3);
        foreach (var target in b.Units.Where(u => u.Friendly)) { target.Health = 100; target.Armor = 0; }
        b.Units[2].Guarding = true; b.Units[2].Mark = 2;
        string before = JsonSerializer.Serialize(world.State); var intent = world.EnemyActionPreview(enemy.Id)!;
        Assert.Equal(before, JsonSerializer.Serialize(world.State)); Assert.Equal(4, intent.Targets.Length);
        Assert.Equal(intent.Targets[0].Minimum + 3, intent.Targets[1].Minimum);
        int[] hp = b.Units.Select(u => u.Health).ToArray(); Assert.True(world.AdvanceEnemy().Success);
        foreach (var target in intent.Targets) Assert.InRange(hp[target.UnitId] - b.Units[target.UnitId].Health, target.Minimum, target.Maximum);
        Assert.Single(world.State.CombatEvents, e => e.Label == "Ally cover");
    }
    [Fact]
    public void GroupIntentDoesNotPromiseCoverFromAProtectorDefeatedEarlierInTheSweep()
    {
        var world = Ready(); var b = world.State.Battle!; var enemy = b.Active;
        b.Plans[enemy.Id] = new("ability.sweep", 0); EquipShield(world, 0);
        foreach (var target in b.Units.Where(u => u.Friendly)) { target.Health = 100; target.Armor = 0; }
        b.Units[0].Health = 1;
        var intent = world.EnemyActionPreview(enemy.Id)!;
        Assert.Equal(1, intent.Targets[0].Maximum);
        int rawMinimum = world.Content.Get<AbilityDefinition>(intent.AbilityId).Power + enemy.Power - 6;
        Assert.Equal(rawMinimum, intent.Targets[1].Minimum);
        int hp = b.Units[1].Health; Assert.True(world.AdvanceEnemy().Success);
        Assert.Equal(0, b.Units[0].Health);
        Assert.DoesNotContain(world.State.CombatEvents, e => e.Label == "Ally cover");
        Assert.InRange(hp - b.Units[1].Health, intent.Targets[1].Minimum, intent.Targets[1].Maximum);
    }
    [Fact]
    public void FallenPlannedTargetKeepsItsPortraitAndShowsWastedActionWithoutRetargeting()
    {
        var world = Ready(); var b = world.State.Battle!; var enemy = b.Active; b.Units[0].Health = 0;
        var fallen = world.State.Actors.Single(a => a.Id == b.ActorIds[0]); fallen.Health = 0; fallen.Life = LifeState.Downed;
        string before = JsonSerializer.Serialize(world.State); var intent = world.EnemyActionPreview(enemy.Id)!;
        Assert.Equal(before, JsonSerializer.Serialize(world.State)); Assert.Contains("will not retarget", intent.Reason);
        Assert.Equal(fallen.Id, Assert.Single(intent.Targets).ActorId); Assert.Equal(0, intent.Targets[0].Maximum);
        long sequence = world.State.NextCombatEvent; Assert.True(world.AdvanceEnemy().Success);
        Assert.DoesNotContain(world.State.CombatEvents, e => e.Sequence >= sequence && e.Kind == "damage");
    }
    [Fact]
    public void SpentStunnedAndDefeatedEnemiesHaveExplicitReasonsAndNoPhantomDamage()
    {
        var world = Ready(); var b = world.State.Battle!; var enemy = b.Active;
        b.TurnIndex = 1; Assert.Contains("Already acted", world.EnemyActionPreview(enemy.Id)!.Reason);
        b.TurnIndex = 0; enemy.Stun = 1; var intent = world.EnemyActionPreview(enemy.Id)!;
        Assert.Contains("Stunned", intent.Reason); Assert.All(intent.Targets, t => Assert.Equal(0, t.Maximum));
        enemy.Health = 0; Assert.Equal("Defeated.", world.EnemyActionPreview(enemy.Id)!.Reason);
        Assert.Null(world.EnemyActionPreview(0)); Assert.Null(world.EnemyActionPreview(-1)); Assert.Null(world.EnemyActionPreview(99));
        world.State.Battle = null; Assert.Null(world.EnemyActionPreview(enemy.Id));
    }
    [Theory]
    [InlineData("ability.recover")] [InlineData("ability.guard")] [InlineData("ability.rally-host")]
    public void SupportIntentsShowRealTargetsAndHealingWithoutInventingDamage(string abilityId)
    {
        var world = Ready(); var b = world.State.Battle!; var enemy = b.Active; enemy.Health = 100;
        b.Plans[enemy.Id] = new(abilityId, enemy.Id);
        var intent = world.EnemyActionPreview(enemy.Id)!; var target = Assert.Single(intent.Targets);
        Assert.Equal(enemy.Id, target.UnitId); Assert.Empty(intent.Reason);
        int hp = enemy.Health; Assert.True(world.AdvanceEnemy().Success);
        Assert.Equal(enemy.Health - hp, target.Minimum); Assert.Equal(target.Minimum, target.Maximum);
    }
}
