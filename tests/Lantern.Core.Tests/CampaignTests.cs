using System.Text.Json.Nodes;
using Lantern.Core;
using Xunit;

namespace Lantern.Core.Tests;

public sealed class CampaignTests
{
    [Fact]
    public void StartsWithFourDistinctCompanionsAndSeparateSaveSlot()
    {
        var c = new Campaign();
        Assert.Equal(4, c.Party.Count);
        Assert.Equal(4, c.Party.Select(h => h.Role).Distinct().Count());
        Assert.All(c.Party, h => Assert.Equal(h.MaxHealth, h.Health));
        Assert.Equal("hearth", c.ZoneId);
        Assert.EndsWith("lantern.json", Campaign.DefaultSave);
    }
    [Fact]
    public void MovementIsCardinalAndCollisionBlocksWalls()
    {
        var c = new Campaign();
        var before = c.Position;
        Assert.False(c.Move(1, 1).Success);
        Assert.Equal(before, c.Position);
        Assert.True(c.Move(1, 0).Success);
        Assert.Equal(before + new Cell(1, 0), c.Position);
        var z = new Zone("test", "", "", 8, 8, Ground.Stone);
        z.Set(new(3, 3), Ground.Wall);
        Assert.False(z.Walkable(new(3, 3)));
        Assert.False(z.Walkable(new(-1, 0)));
    }
    [Fact]
    public void VisibilityRevealsBlockingWallButNotItsShadow()
    {
        var z = new Zone("test", "", "", 12, 12, Ground.Stone);
        for (int y = 0; y < 12; y++)
            z.Set(new(5, y), Ground.Wall);
        z.Reveal(new(3, 5), 8);
        Assert.True(z.Visible[z.Index(new(5, 5))]);
        Assert.False(z.Visible[z.Index(new(6, 5))]);
        Assert.True(z.Seen[z.Index(new(3, 5))]);
    }
    [Fact]
    public void EveryInteractiveObjectHasAnApproachableTile()
    {
        var c = new Campaign();
        foreach (var z in c.Zones)
        foreach (var o in z.Objects.Where(o => o.Kind != ObjectKind.Scenery))
            Assert.Contains(z.Neighbors(o.Position), z.Walkable);
    }
    [Fact]
    public void ChestsAndShrinesCannotBeFarmed()
    {
        var c = new Campaign();
        Assert.True(c.TravelTo("to-wilds").Success);
        Assert.True(c.TravelTo("iron-cache").Success);
        Assert.True(c.HasKey);
        int gold = c.Gold, potions = c.Potions;
        Assert.False(c.Interact("iron-cache").Success);
        Assert.Equal(gold, c.Gold);
        Assert.Equal(potions, c.Potions);
        Assert.True(c.TravelTo("forest-shrine").Success);
        Assert.False(c.Interact("forest-shrine").Success);
    }
    [Fact]
    public void GateRequiresTheActualKeyAndPersistsWhenOpened()
    {
        var c = new Campaign();
        c.TravelTo("to-wilds");
        c.TravelTo("to-crypt");
        var result = c.TravelTo("iron-gate");
        Assert.False(result.Success);
        Assert.False(c.Walkable(new(34, 16)));
        c.TravelTo("to-forest");
        c.TravelTo("iron-cache");
        c.TravelTo("to-crypt");
        Assert.True(c.TravelTo("iron-gate").Success);
        Assert.False(c.HasKey);
        Assert.True(c.Walkable(new(34, 16)));
        var loaded = Campaign.FromJson(c.StateJson());
        Assert.True(loaded.Walkable(new(34, 16)));
    }
    [Fact]
    public void InvalidActionsSpendNoManaPotionsOrTurns()
    {
        var c = InBattle();
        var b = c.Battle!;
        string before = c.StateJson();
        Assert.False(c.Act(0, 0).Success);
        Assert.Equal(before, c.StateJson());
        Assert.False(c.Act(-1).Success);
        Assert.Equal(before, c.StateJson());
        Assert.True(b.Active.Friendly);
    }
    [Fact]
    public void EnemyIntentAndPaidPlayerActionAdvanceCombat()
    {
        var c = InBattle();
        var b = c.Battle!;
        var enemy = b.Units.First(u => !u.Friendly);
        Assert.NotEmpty(c.IntentText(enemy));
        int hp = enemy.Health;
        var actor = b.Active;
        int mana = actor.Mana;
        Assert.True(c.Act(0, enemy.Id).Success);
        Assert.True(enemy.Health < hp);
        Assert.Equal(mana, actor.Mana);
    }
    [Fact]
    public void BattleEndsInRewardsAndDoesNotRespawnClearedEncounter()
    {
        var c = InBattle();
        int gold = c.Gold;
        c.AutoBattle();
        Assert.Null(c.Battle);
        Assert.Contains("wilds-hounds", c.Completed);
        Assert.True(c.Gold > gold);
        Assert.Null(c.ObjectAt(new(11, 16)));
        Assert.All(c.Party, h => Assert.True(h.Experience > 0));
    }
    [Fact]
    public void GuardReducesTheSameSeededEnemyStrike()
    {
        var guarded = InBattle();
        int target = guarded.Battle!.Active.Id;
        guarded.Act(3);
        while (guarded.Battle!.Active.Friendly)
            guarded.Act(3);
        guarded.Battle.Active.Intent = IntentKind.Strike;
        guarded.Battle.Active.IntentTarget = target;
        var exposed = Campaign.FromJson(guarded.StateJson());
        exposed.Battle!.Units[target].Guarding = false;
        int before = guarded.Battle.Units[target].Health;
        guarded.AdvanceEnemy();
        exposed.AdvanceEnemy();
        Assert.True(guarded.Battle!.Units[target].Health > exposed.Battle!.Units[target].Health);
        Assert.True(guarded.Battle.Units[target].Health < before);
    }
    [Fact]
    public void MendPaysManaRevivesAndClearsBleeding()
    {
        var c = InBattle();
        WaitForRole(c, 3);
        var b = c.Battle!;
        int mana = b.Active.Mana;
        b.Units[0].Health = 0;
        b.Units[0].Bleed = 2;
        Assert.True(c.Act(1, 0).Success);
        Assert.Equal(mana - 3, b.Units[3].Mana);
        Assert.True(b.Units[0].Health > 0);
        Assert.Equal(0, b.Units[0].Bleed);
    }
    [Fact]
    public void EmberAppliesBurnAndManaCannotBeOverspent()
    {
        var c = InBattle();
        WaitForRole(c, 2);
        var b = c.Battle!;
        var actor = b.Active;
        var target = b.Units.First(u => !u.Friendly);
        int mana = actor.Mana;
        Assert.True(c.Act(1, target.Id).Success);
        Assert.Equal(mana - 3, actor.Mana);
        Assert.True(target.Burn > 0 || !target.Alive);
        WaitForRole(c, 2);
        c.Battle!.Active.Mana = 0;
        string before = c.StateJson();
        Assert.False(c.Act(1, target.Id).Success);
        Assert.Equal(before, c.StateJson());
    }
    [Fact]
    public void StressWeakensAnOtherwiseIdenticalAttack()
    {
        var calm = InBattle();
        var shaken = Campaign.FromJson(calm.StateJson());
        shaken.Battle!.Active.Stress = 80;
        int enemy = 4;
        calm.Act(0, enemy);
        shaken.Act(0, enemy);
        Assert.Equal(2, shaken.Battle!.Units[enemy].Health - calm.Battle!.Units[enemy].Health);
    }
    [Fact]
    public void RetreatLeavesEncounterAvailableAndCostsComposure()
    {
        var c = InBattle();
        Assert.True(c.Act(5).Success);
        Assert.Null(c.Battle);
        Assert.DoesNotContain("wilds-hounds", c.Completed);
        Assert.All(c.Party, h => Assert.Equal(5, h.Stress));
        Assert.NotEqual(new Cell(11, 16), c.Position);
    }
    [Fact]
    public void DefeatReturnsPartyToTownWithoutErasingProgress()
    {
        var c = InBattle();
        c.Act(3);
        while (c.Battle!.Active.Friendly)
            c.Act(3);
        foreach (var hero in c.Battle.Units.Where(u => u.Friendly))
            hero.Health = 1;
        for (int i = 0; c.Battle is not null && i < 100; i++)
        {
            if (c.Battle.Active.Friendly)
                c.Act(3);
            else
                c.AdvanceEnemy();
        }
        Assert.Null(c.Battle);
        Assert.Equal("hearth", c.ZoneId);
        Assert.All(c.Party, h => Assert.True(h.Health > 0));
        Assert.DoesNotContain("wilds-hounds", c.Completed);
    }
    [Fact]
    public void CampAndMerchantSpendSuppliesOncePerAction()
    {
        var c = new Campaign();
        int rations = c.Rations;
        Assert.True(c.Camp().Success);
        Assert.Equal(rations - 2, c.Rations);
        c.TravelTo("merchant");
        Assert.Equal(26, c.Gold);
        Assert.Equal(7, c.Potions);
        Assert.Equal(6, c.Torches);
        Assert.Equal(6, c.Rations);
        c.TravelTo("to-wilds");
        Assert.False(c.Camp().Success);
    }
    [Fact]
    public void RelicCannotBeTakenWhileItsGuardianLives()
    {
        var c = new Campaign();
        c.TravelTo("to-wilds");
        c.TravelTo("iron-cache");
        c.TravelTo("to-crypt");
        c.TravelTo("iron-gate");
        var result = c.TravelTo("relic");
        Assert.False(result.Success);
        Assert.Contains("Hollow Abbot", result.Message);
        Assert.False(c.HasRelic);
    }
    [Theory]
    [InlineData(42)]
    [InlineData(7)]
    [InlineData(1337)]
    public void FullExpeditionCanReturnTheEmberThroughRealMovementAndCombat(int seed)
    {
        var c = new Campaign(seed);
        c.RunDemoExpedition();
        Assert.True(c.Victory);
        Assert.True(c.HasRelic);
        Assert.Equal("hearth", c.ZoneId);
        Assert.Contains("abbey-abbot", c.Completed);
        Assert.All(c.Party, h => Assert.True(h.Level >= 2));
    }
    [Fact]
    public void SaveDuringCombatContinuesExactly()
    {
        var c = InBattle();
        c.Act(0, c.Battle!.Units.First(u => !u.Friendly).Id);
        var loaded = Campaign.FromJson(c.StateJson());
        Assert.Equal(c.StateJson(), loaded.StateJson());
        c.AutoBattle();
        loaded.AutoBattle();
        Assert.Equal(c.StateJson(), loaded.StateJson());
    }
    [Fact]
    public void AtomicSaveRoundTripsOutsideCombat()
    {
        var c = new Campaign();
        c.TravelTo("to-wilds");
        c.TravelTo("iron-cache");
        string file = Path.Combine(Path.GetTempPath(), "lantern-" + Guid.NewGuid() + ".json");
        try
        {
            c.Save(file);
            Assert.Equal(c.StateJson(), Campaign.Load(file).StateJson());
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(file)!, Path.GetFileName(file) + ".*.tmp"));
        }
        finally { File.Delete(file); }
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"format\":\"regolith-csharp\",\"version\":1}")]
    [InlineData("{\"format\":\"lantern-rpg\",\"party\":null}")]
    public void InvalidOrColonySaveIsRejected(string json) => Assert.Throws<InvalidDataException>(() => Campaign.FromJson(json));
    [Fact]
    public void SaveRejectsInvalidHealthAndBattleOrder()
    {
        var c = InBattle();
        var json = JsonNode.Parse(c.StateJson())!;
        json["party"]![0]!["health"] = -1;
        Assert.Throws<InvalidDataException>(() => Campaign.FromJson(json.ToJsonString()));
        json = JsonNode.Parse(c.StateJson())!;
        json["battle"]!["turnIndex"] = 999;
        Assert.Throws<InvalidDataException>(() => Campaign.FromJson(json.ToJsonString()));
    }
    private static void WaitForRole(Campaign c, int role)
    {
        for (int i = 0; i < 100; i++)
        {
            var b = c.Battle!;
            if (b.Active.Friendly && c.Party[b.Active.HeroId].Role == role)
                return;
            if (b.Active.Friendly)
                c.Act(3);
            else
                c.AdvanceEnemy();
        }
        throw new InvalidOperationException("Requested companion never received a turn.");
    }
    private static Campaign InBattle()
    {
        var c = new Campaign();
        Assert.True(c.TravelTo("to-wilds").Success);
        Assert.True(c.TravelTo("wilds-hounds").Success);
        Assert.NotNull(c.Battle);
        return c;
    }
}
