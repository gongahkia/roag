using System.Text.Json;
using Vagabonds.Core.Content;
using Xunit;
using Vagabonds.Core.World;

namespace Vagabonds.Core.Tests.World;

public sealed class AdventureTests
{
    [Fact]
    public void LargeBossRosterHasDistinctPhaseDesignsAndRunsThroughTheRealResolver()
    {
        var content = ContentCatalog.Default;
        Assert.Equal(48, content.Game.Bosses.Length);
        var bosses = content.Game.Bosses.Select(content.Get<BossDefinition>).ToArray();
        Assert.True(bosses.Select(b => b.Family).Distinct().Count() >= 16);
        Assert.True(bosses.Select(b => JsonSerializer.Serialize(new { b.Behaviors, b.Enraged, b.Minions })).Distinct().Count() >= 40);
        foreach (var boss in bosses)
        {
            var preview = EncounterPreview.Run(content, boss.Id, 42);
            Assert.NotEmpty(preview.Frames); Assert.All(preview.Frames, f => Assert.NotEmpty(f.Intentions));
            Assert.All(preview.Frames, f => Assert.DoesNotContain(f.Health.Values, health => health < 0));
        }
    }
    [Fact]
    public void SelectedFinalThreatAndAllDungeonDepthsExistBeforeAnyArcProgress()
    {
        var selections = new HashSet<string>();
        for (int seed = 0; seed < 16; seed++)
        {
            var s = WorldSession.Create(seed); var boss = s.FinalBoss;
            Assert.Equal(0, s.State.Arc.Progress); Assert.Equal(48, s.State.Actors.Count(a => a.Id == a.EncounterLeader && s.Content.Entries[a.Archetype] is BossDefinition));
            Assert.InRange(boss.Position.Floor, 1, 3); selections.Add(boss.Archetype + ":" + boss.VariantId);
            var site = s.Plan.Sites[s.State.Endgame.SiteId];
            for (int floor = 0; floor < boss.Position.Floor; floor++)
            {
                var stair = site.Center.Offset(2, 0) with { Floor = floor };
                s.State.Position = stair; foreach (var a in s.Party) a.Position = stair;
                Assert.True(s.Submit(new(WorldCommandKind.Interact, stair)).Success);
                Assert.Equal(floor + 1, s.State.Position.Floor);
                Assert.Equal(0, s.State.Arc.Progress);
            }
        }
        Assert.True(selections.Count >= 14);
    }
    [Fact]
    public void ThinkingIsFreeAndCompletedRoundsAdvanceExactlyOneMinute()
    {
        var s = WorldSession.Create(42); var enemy = s.State.Actors.First(a => a.Kind == ActorKind.Beast);
        s.Party.First().BaseHealth = 1000; s.Party.First().Health = 1000;
        s.StartBattle(enemy.Id); Assert.Equal(0, s.AdvanceTicks(100));
        Assert.Equal(0, s.AdvanceFrame(TimeSpan.FromSeconds(3))); Assert.Equal(0, s.State.Tick);
        var initialPlans = JsonSerializer.Serialize(s.State.Battle!.Plans);
        Assert.False(s.Act(99).Success); Assert.Equal(initialPlans, JsonSerializer.Serialize(s.State.Battle.Plans));
        int budget = 20;
        while (s.State.Battle!.Round == 1 && budget-- > 0)
        { if (s.State.Battle.Active.Friendly) Assert.True(s.Act(3).Success); else Assert.True(s.AdvanceEnemy().Success); }
        Assert.True(budget > 0); Assert.Equal(WorldSession.TicksPerSecond, s.State.Tick);
    }
    [Fact]
    public void EnemyDefinitionsChooseTheActualTargetAndBossHealthSelectsThePhase()
    {
        var content = Vagabonds.Tests.Content.ContentTests.Edited("character.prowler", n => n["behaviors"] = System.Text.Json.Nodes.JsonNode.Parse("[{\"ability\":\"ability.dread\",\"target\":\"strongest\"}]")!);
        var s = WorldSession.Create(42, content); var hero = s.Party.First(); hero.BaseHealth = 100; hero.Health = 100;
        var npc = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); npc.Position = s.State.Position;
        Assert.True(s.Submit(new(WorldCommandKind.Recruit, ActorId: npc.Id)).Success);
        var enemy = s.State.Actors.First(a => a.Archetype == "character.prowler"); enemy.BaseSpeed = 30;
        s.StartBattle(enemy.Id); var b = s.State.Battle!;
        Assert.False(b.Active.Friendly); Assert.Equal("ability.dread", b.Plans[b.Active.Id].Ability);
        Assert.Equal(0, b.Plans[b.Active.Id].Target);
        Assert.True(s.AdvanceEnemy().Success); Assert.True(hero.Stress >= 8); Assert.True(hero.Health < 100);
        foreach (bool enraged in new[] { false, true })
        {
            var phaseContent = Vagabonds.Tests.Content.ContentTests.Edited("boss.ash-knight", n =>
            {
                n["behaviors"] = System.Text.Json.Nodes.JsonNode.Parse("[{\"ability\":\"ability.hex\"}]");
                n["enraged"] = System.Text.Json.Nodes.JsonNode.Parse("[{\"ability\":\"ability.sweep\"}]");
            });
            var w = WorldSession.Create(42, phaseContent);
            var boss = ActorFactory.CreateBoss(phaseContent, "boss.ash-knight", "variant.swift", w.State.NextActorId++, w.State.Position, 0);
            if (enraged) boss.Health = boss.MaxHealth * 45 / 100;
            w.State.Actors.Add(boss); w.StartBattle(boss.Id);
            int unit = w.State.Battle!.ActorIds.IndexOf(boss.Id);
            Assert.Equal(enraged ? "ability.sweep" : "ability.hex", w.State.Battle.Plans[unit].Ability);
        }
    }
    [Fact]
    public void KillingTheSelectedBossCompletesTheRunAndFreezesIt()
    {
        var s = WorldSession.Create(42); var hero = s.Party.First(); hero.BaseSpeed = 30;
        var boss = s.FinalBoss; boss.Health = 1; s.StartBattle(boss.Id);
        Assert.True(s.State.Battle!.Active.Friendly);
        int target = s.State.Battle.ActorIds.IndexOf(boss.Id);
        Assert.True(s.Act(0, target).Success); Assert.False(s.IsInCombat); Assert.True(s.State.Endgame.Victory);
        s.Paused = false; Assert.Equal(0, s.AdvanceTicks(100));
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Move, s.State.Position.Offset(1, 0))).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
    [Fact]
    public void LootIsOneTimeAndEquipmentSwapsPreserveItemsAndAlterCombatStats()
    {
        var s = WorldSession.Create(42); var hero = s.Party.First(); var cache = s.State.Loot[0];
        s.State.Position = cache.Position; hero.Position = cache.Position;
        Assert.True(s.Submit(new(WorldCommandKind.Loot, cache.Position)).Success);
        var item = s.Content.All<ItemDefinition>().Single(i => i.Slot != "none" && cache.Contents.ContainsKey(i.Stack));
        int count = s.Count(item.Stack), health = hero.MaxHealth, power = hero.Power, armor = hero.Armor;
        Assert.True(s.Submit(new(WorldCommandKind.Equip, ActorId: hero.Id, Item: item.Id)).Success);
        Assert.Equal(health + item.Health, hero.MaxHealth); Assert.Equal(power + item.Power, hero.Power); Assert.Equal(armor + item.Armor, hero.Armor);
        Assert.Equal(count - 1, s.Count(item.Stack));
        string before = JsonSerializer.Serialize(s.State); Assert.False(s.Submit(new(WorldCommandKind.Loot, cache.Position)).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        Assert.True(s.Submit(new(WorldCommandKind.Unequip, ActorId: hero.Id, Item: item.Slot)).Success);
        Assert.Equal(count, s.Count(item.Stack)); Assert.Equal(health, hero.MaxHealth);
    }
}
