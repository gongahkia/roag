using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WorldEncounterRewardTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-rewards-" + Guid.NewGuid().ToString("N"));
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static void DefeatAll(WorldSession world)
    {
        var battle = world.State.Battle!;
        foreach (var unit in battle.Units.Where(u => !u.Friendly && u.Alive))
        { unit.Health = 1; world.State.Actors.Single(a => a.Id == battle.ActorIds[unit.Id]).Health = 1; }
        WorldEncounterGroupTests.GuardUntil(world, b => b.Active.Friendly && world.ActiveSkills().Any(s => s.Id == "ability.volley"));
        int index = world.ActiveSkills().Select((s, i) => (s, i)).Single(p => p.s.Id == "ability.volley").i;
        Assert.True(world.Act(index).Success); Assert.Null(world.State.Battle);
    }

    [Theory]
    [InlineData("encounter.road-wardens")] [InlineData("encounter.cinder-ambush")] [InlineData("encounter.hollow-vigil")]
    public void ComposedEncountersGrantTheirAuthoredPoolOnceAndPersistUniqueEquipment(string id)
    {
        var world = WorldSession.Create(42); var fixture = EncounterGroupPreview.Prepare(world, id);
        var expected = world.Content.Get<RewardDefinition>(world.Content.Get<EncounterDefinition>(id).Reward);
        int gold = world.Count("gold"), food = world.Count("food");
        DefeatAll(world); var reward = world.State.LastEncounterReward!;
        Assert.Equal(expected.Gold, reward.Gold); Assert.Equal(expected.Food, reward.Food); Assert.Equal(expected.Experience, reward.Experience);
        Assert.Equal(gold + expected.Gold, world.Count("gold")); Assert.Equal(food + expected.Food, world.Count("food"));
        Assert.Equal(new[] { fixture.Leader }, reward.Sources); Assert.Contains(fixture.Leader, world.State.RewardedEncounterIds);
        var gear = Assert.Single(reward.Items, i => world.Item(i.Item).Slot != "none");
        Assert.Contains(world.State.GeneratedEquipment[gear.Item].Base, expected.EquipmentPool); Assert.Equal(1, world.Count(gear.Item));
        foreach (var (item, count) in expected.Supplies) Assert.Contains(reward.Items, i => i.Item == item && i.Count == count);
        world.Save(Slot); var loaded = WorldSession.Load(Slot);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        // Deliberately reconstruct a repeated victory fixture: a persistent source must never pay twice.
        int oldGold = loaded.Count("gold"), oldExperience = loaded.Party.First().Experience;
        foreach (var actor in loaded.State.Actors.Where(a => fixture.Members.Contains(a.Id))) { actor.Life = LifeState.Alive; actor.Health = 1; }
        loaded.Party.First().Mana = loaded.Party.First().MaxMana;
        loaded.StartBattle(fixture.Leader); DefeatAll(loaded);
        Assert.Equal(0, loaded.State.LastEncounterReward!.Experience); Assert.Empty(loaded.State.LastEncounterReward.Items);
        Assert.Equal(oldGold, loaded.Count("gold")); Assert.Equal(oldExperience, loaded.Party.First().Experience);
        Assert.Equal(1, loaded.Count(gear.Item));
        loaded.Save(Slot); Assert.NotNull(WorldSession.Load(Slot));
    }

    [Fact]
    public void MixedUngroupedBattlesCombineEveryDefeatedActorsThematicReward()
    {
        var world = WorldSession.Create(42); CampPreview.Prepare(world);
        var hero = world.Party.First(); hero.Role = 1; hero.BaseSpeed = 30; hero.Mana = hero.MaxMana;
        var ids = new List<long>();
        foreach (string type in new[] { "character.raider", "character.wisp" })
        {
            long id = world.State.NextActorId++; ids.Add(id);
            var actor = ActorFactory.Create(world.Content, type, 42, id, world.State.Position.Offset(5 + ids.Count, 0), 0);
            actor.BaseSpeed = 1; world.State.Actors.Add(actor);
        }
        world.StartBattle(ids[0]); Assert.Equal(2, world.State.Battle!.Units.Count(u => !u.Friendly)); DefeatAll(world);
        var reward = world.State.LastEncounterReward!;
        var raiders = world.Content.Get<RewardDefinition>("reward.raiders"); var spirits = world.Content.Get<RewardDefinition>("reward.spirits");
        Assert.Equal(raiders.Gold + spirits.Gold, reward.Gold); Assert.Equal(raiders.Experience + spirits.Experience, reward.Experience);
        Assert.Equal(ids.Order(), reward.Sources); Assert.Contains(raiders.Name, reward.Theme); Assert.Contains(spirits.Name, reward.Theme);
    }

    [Fact]
    public void RetreatAndReloadPreserveAnUnpaidPartialGroupThenRewardItsCompletion()
    {
        var world = WorldSession.Create(42); var fixture = EncounterGroupPreview.Prepare(world);
        var battle = world.State.Battle!; int leader = battle.ActorIds.IndexOf(fixture.Leader);
        battle.Units[leader].Health = 1; world.State.Actors.Single(a => a.Id == fixture.Leader).Health = 1;
        Assert.True(world.Act(0, leader).Success); Assert.True(world.Act(5).Success);
        Assert.Null(world.State.LastEncounterReward); Assert.DoesNotContain(fixture.Leader, world.State.RewardedEncounterIds);
        world.Save(Slot); var loaded = WorldSession.Load(Slot);
        Assert.Equal(LifeState.Dead, loaded.State.Actors.Single(a => a.Id == fixture.Leader).Life);
        loaded.StartBattle(fixture.Members[1]); DefeatAll(loaded);
        Assert.Equal(new[] { fixture.Leader }, loaded.State.LastEncounterReward!.Sources);
        Assert.Equal(loaded.Content.Get<RewardDefinition>("reward.cinder-ambush").Experience, loaded.State.LastEncounterReward.Experience);
    }

    [Fact]
    public void FinalGuardianGrantsItsFamilySignatureEvenWhenAttendantsSurvive()
    {
        var world = WorldSession.Create(42); CampPreview.Prepare(world);
        var hero = world.Party.First(); hero.Role = 1; hero.BaseSpeed = 30; hero.Body.Wounds = 0;
        long bossId = world.State.Endgame.ActorId;
        var boss = world.State.Actors.Single(a => a.Id == bossId);
        var expected = world.Content.Get<RewardDefinition>(world.Content.Get<BossDefinition>(boss.Archetype).Reward);
        world.StartBattle(bossId); var battle = world.State.Battle!; int bossIndex = battle.ActorIds.IndexOf(bossId);
        battle.Units[bossIndex].Health = boss.Health = 1;
        Assert.True(world.Act(0, bossIndex).Success); Assert.True(world.State.Endgame.Victory);
        var reward = world.State.LastEncounterReward!;
        Assert.Equal(new[] { bossId }, reward.Sources);
        Assert.Contains(reward.Items, i => world.State.GeneratedEquipment.TryGetValue(i.Item, out var g) && g.Base == expected.Signature);
        Assert.All(world.State.Actors.Where(a => a.EncounterLeader == bossId && a.Id != bossId), a => Assert.Equal(LifeState.Alive, a.Life));
        world.Save(Slot); var loaded = WorldSession.Load(Slot);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        Assert.True(loaded.State.Endgame.Victory);
    }

    [Fact]
    public void AllBossFamiliesHaveThematicSignaturesExcludedFromOrdinaryCaches()
    {
        var world = WorldSession.Create(42); var bosses = world.Content.All<BossDefinition>().ToArray();
        Assert.Equal(48, bosses.Length);
        var signatures = bosses.Select(b => world.Content.Get<RewardDefinition>(b.Reward).Signature).Distinct().ToArray();
        Assert.True(signatures.Length >= 8);
        Assert.All(signatures, id => Assert.Contains("signature", world.Content.Get<ItemDefinition>(id!).Tags));
        Assert.DoesNotContain(world.State.Loot.SelectMany(c => c.Choices), id =>
            world.Item(id).Tags.Contains("signature") || world.State.GeneratedEquipment.TryGetValue(id, out var gear) && signatures.Contains(gear.Base));
    }

    [Fact]
    public void FullSupplyStacksStayLoadableAndReceiptReportsOnlyAcceptedAmounts()
    {
        var world = WorldSession.Create(42); EncounterGroupPreview.Prepare(world, "encounter.road-wardens");
        world.State.Inventory["gold"] = 9998; world.State.Inventory["food"] = world.State.Inventory["medicine"] = 9999;
        DefeatAll(world); var reward = world.State.LastEncounterReward!;
        Assert.Equal(1, reward.Gold); Assert.Equal(0, reward.Food);
        Assert.DoesNotContain(reward.Items, i => i.Item == "item.medicine");
        Assert.All(new[] { "gold", "food", "medicine" }, stack => Assert.Equal(9999, world.Count(stack)));
        world.Save(Slot); Assert.NotNull(WorldSession.Load(Slot));
    }

    [Theory]
    [InlineData("reward.wildlife")] [InlineData("reward.cinder-ambush")]
    [InlineData("reward.guardian-iron")] [InlineData("reward.guardian-memory")]
    public void RewardPreviewExercisesActualGrantSaveAndReplayProtection(string id)
    {
        var result = EncounterRewardPreview.Run(ContentCatalog.Default, id, 42);
        Assert.True(result.Passed); Assert.Contains(result.Events, e => e.Kind == "reward");
        var expected = ContentCatalog.Default.Get<RewardDefinition>(id);
        Assert.Equal(expected.Gold, result.Granted.Gold); Assert.Equal(expected.Experience, result.Granted.Experience);
        Assert.Single(result.Granted.Sources);
        if (expected.Signature != null) Assert.Contains(result.Equipment, e => e.Base == expected.Signature);
        if (id == "reward.wildlife") { Assert.Empty(result.Equipment); Assert.Equal(0, result.Granted.Gold); Assert.Equal(0, result.Granted.Food); }
    }

    [Fact]
    public void EquipmentGrantRollsDoNotConsumeTheCombatRandomStream()
    {
        var noEquipment = WorldEncounterGroupTests.Changed("reward.cinder-ambush", node => node["equipmentChancePercent"] = 0);
        var normal = WorldSession.Create(42); var without = WorldSession.Create(42, noEquipment);
        EncounterGroupPreview.Prepare(normal); EncounterGroupPreview.Prepare(without);
        DefeatAll(normal); DefeatAll(without);
        Assert.NotEmpty(normal.State.LastEncounterReward!.Items); Assert.Empty(without.State.LastEncounterReward!.Items);
        Assert.Equal(normal.State.CombatRandom, without.State.CombatRandom);
        Assert.Equal(normal.State.LastEncounterReward.Experience, without.State.LastEncounterReward.Experience);
    }

    [Theory]
    [InlineData("living-source")] [InlineData("wrong-item-kind")] [InlineData("unknown-source")]
    public void ForgedRewardContinuationIsRejected(string fault)
    {
        var world = WorldSession.Create(42); var fixture = EncounterGroupPreview.Prepare(world); DefeatAll(world); world.Save(Slot);
        var json = JsonNode.Parse(File.ReadAllText(Slot))!; var state = json["state"]!;
        if (fault == "living-source") state["rewardedEncounterIds"]!.AsArray().Add(world.State.Party[0]);
        if (fault == "wrong-item-kind") state["lastEncounterReward"]!["items"]![0]!["item"] = "ability.strike";
        if (fault == "unknown-source") state["lastEncounterReward"]!["sources"]![0] = fixture.Leader + 999999;
        File.WriteAllText(Slot, json.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
}
