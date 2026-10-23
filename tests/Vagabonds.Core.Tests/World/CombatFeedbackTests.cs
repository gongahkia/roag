using System.Text.Json;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class CombatFeedbackTests
{
    private static WorldSession Fight()
    {
        var s = WorldSession.Create(42); var hero = s.Party.First(); hero.BaseSpeed = 30; hero.BaseHealth = hero.Health = 1000;
        var enemy = s.State.Actors.First(a => a.Kind == ActorKind.Beast); enemy.BaseHealth = enemy.Health = 1000;
        enemy.Position = s.State.Position.Offset(8, 0); s.Chunks.Edit(enemy.Position, Material.Floor);
        s.StartBattle(enemy.Id); return s;
    }
    [Fact]
    public void DamagePreviewUsesActualGuardArmourMarkAndStressWithoutConsumingAnything()
    {
        var s = Fight(); var battle = s.State.Battle!; var enemy = battle.Units.First(u => !u.Friendly); var hero = battle.Active;
        hero.Stress = s.Party.First().Stress = 70; enemy.Guarding = true; enemy.Mark = 2;
        string before = JsonSerializer.Serialize(s.State); var preview = s.ActionPreview(0, enemy.Id);
        Assert.Empty(preview.Reason); Assert.True(preview.Minimum > 0); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        int hp = enemy.Health; Assert.True(s.Act(0, enemy.Id).Success); Assert.InRange(hp - enemy.Health, preview.Minimum, preview.Maximum);
        Assert.Contains(s.State.CombatEvents, e => e.Kind == "damage" && e.Target == battle.ActorIds[enemy.Id] && e.Amount == hp - enemy.Health);
        before = JsonSerializer.Serialize(s.State); Assert.False(s.Act(99).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
    [Fact]
    public void StatusDamageHealingAndVictoryProduceSpecificPersistentFeedback()
    {
        var s = Fight(); var battle = s.State.Battle!; var enemy = battle.Units.First(u => !u.Friendly);
        var hero = s.Party.First(); int index = Array.FindIndex(s.Content.Classes.Single(c => c.Role == hero.Role).Abilities, id => s.Content.Get<AbilityDefinition>(id).Effect is "burn" or "bleed" or "mark" or "stun");
        Assert.True(index >= 0); Assert.True(s.Act(index, enemy.Id).Success);
        Assert.Contains(s.State.CombatEvents, e => e.Kind == "status" && e.Target == battle.ActorIds[enemy.Id]);
        while (!s.State.Battle!.Active.Friendly) s.AdvanceEnemy();
        hero.BasePower = 100; s.State.Battle.Active.Power = 100; enemy.Health = s.State.Actors.Single(a => a.Id == battle.ActorIds[enemy.Id]).Health = 1;
        var source = s.State.Actors.Single(a => a.Id == battle.ActorIds[enemy.Id]);
        var reward = s.Content.Get<RewardDefinition>(s.Content.Get<CharacterDefinition>(source.Archetype).Reward);
        int gold = s.Count("gold"); Assert.True(s.Act(0, enemy.Id).Success);
        Assert.False(s.IsInCombat); Assert.Equal(gold + reward.Gold, s.Count("gold")); Assert.Equal(reward.Experience, s.State.LastEncounterReward!.Experience);
        Assert.Contains(s.State.CombatEvents, e => e.Kind == "fall"); Assert.Contains(s.State.CombatEvents, e => e.Kind == "reward" && e.Detail.Contains(reward.Gold + " gold"));
    }
    [Fact]
    public void RequirementsExplainFailureAndQueriesPreserveTheEntireState()
    {
        var s = WorldSession.Create(42); var craft = new WorldCommand(WorldCommandKind.Craft, Item: "recipe.camp-weapon");
        string before = JsonSerializer.Serialize(s.State); string reason = s.CommandBlockReason(craft);
        Assert.Contains("camp", reason); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        Assert.Equal(reason, s.Submit(craft).Message); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        var hero = s.Party.First(); var item = s.Content.Get<ItemDefinition>("item.hunters-bow"); var compare = s.CompareGear(hero, item.Id);
        Assert.Equal(item.Power, compare.Power); Assert.Equal(item.Speed, compare.Speed); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        s.State.Inventory[item.Stack] = 1; Assert.True(s.Submit(new(WorldCommandKind.Equip, ActorId: hero.Id, Item: item.Id)).Success);
        Assert.Equal(0, s.CompareGear(hero, item.Id).Power);
    }
    [Fact]
    public void FeedbackIsBoundedAndSavesContinueWithIdenticalEventsAndRandomness()
    {
        var s = Fight(); int budget = 200;
        while (s.State.NextCombatEvent < 150 && s.IsInCombat && budget-- > 0)
        { if (s.State.Battle!.Active.Friendly) s.Act(3); else s.AdvanceEnemy(); }
        Assert.True(budget > 0); Assert.InRange(s.State.CombatEvents.Count, 1, 128);
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-feedback-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            string slot = Path.Combine(directory, "world.json"); s.Save(slot); var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
            if (s.IsInCombat) { if (s.State.Battle!.Active.Friendly) { s.Act(3); loaded.Act(3); } else { s.AdvanceEnemy(); loaded.AdvanceEnemy(); } }
            Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { Directory.Delete(directory, true); }
    }
}
