using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeEncounterRewardTests
{
    private static WorldSession Arena()
    {
        var world = NativeSocietyTests.AtTick(0); var hero = world.Party.Single();
        hero.Position = world.State.Position = world.Plan.Towns[0].Center with { Floor = 1 };
        for (int y = -3; y <= 3; y++) for (int x = -3; x <= 8; x++) world.Chunks.Edit(hero.Position.Offset(x, y), Material.Floor);
        world.Reveal();
        return world;
    }

    private static Actor[] Group(WorldSession world, string encounter)
    {
        var definition = world.Content.Get<EncounterDefinition>(encounter); long leader = world.State.NextActorId;
        var result = new List<Actor>();
        foreach (string id in definition.Members)
        {
            var actor = ActorFactory.Create(world.Content, id, world.Seed, world.State.NextActorId++, world.Party.First().Position.Offset(result.Count + 1, 0), 0);
            actor.EncounterId = encounter; actor.EncounterLeader = leader; world.State.Actors.Add(actor);
            world.DungeonCombat.RegisterRetainedEnemy(actor, world.DungeonCombat.Clock + 1000000); result.Add(actor);
        }
        world.Reveal(); return result.ToArray();
    }

    private static Actor Helper(WorldSession world)
    {
        var member = world.ActorById(1004); member.Recruited = true; world.State.Party.Add(member.Id);
        member.Position = world.Party.First().Position.Offset(0, 1);
        world.DungeonCombat.Register(member, new() { Build = CharacterBuild.Create(ClassKind.Ranger, 42) }, world.DungeonCombat.Clock + 1000000);
        return member;
    }

    // Explicit canonical damage fixtures isolate reward ownership and simultaneous
    // settlements. Only recovery/collection assertions below use paid commands.
    private static void Strike(WorldSession world, long source, params Actor[] enemies)
        => world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, source, world.Party.First().Position,
            enemies.Select(a => a.Position).ToArray(), "test:native-reward", AttackDelivery.Area, DamageElement.Physical, 1000, world.DungeonCombat.Clock));

    private static long ExperienceUnits(Actor actor) => actor.Tactics!.Build!.Experience * ContributionRewards.UnitsPerExperience + actor.Tactics.ExperienceFraction;

    [Theory]
    [InlineData("encounter.cinder-ambush")] [InlineData("encounter.road-wardens")] [InlineData("encounter.penitent-procession")]
    public void SimultaneousGroupDefeatAwardsTheAuthoredPoolOnceAndLeavesActualRecoverableSupplies(string encounter)
    {
        using var world = Arena(); var group = Group(world, encounter); var hero = world.Party.Single();
        var reward = world.Content.Get<RewardDefinition>(world.Content.Get<EncounterDefinition>(encounter).Reward);
        int gold = world.Count("gold"), food = world.Count("food"), rng = world.State.Tactics!.RandomState;
        Strike(world, hero.Id, group);
        Assert.All(group, a => Assert.Equal(LifeState.Dead, a.Life));
        Assert.Equal(reward.Experience * ContributionRewards.UnitsPerExperience, ExperienceUnits(hero));
        Assert.Contains(group[0].Id, world.State.RewardedEncounterIds); Assert.Empty(world.State.NativeCampaign!.EncounterRewards.Pending);
        var cache = Assert.Single(world.State.Loot, c => c.Id == "native-encounter-" + group[0].Id);
        Assert.Equal(group[0].Position, cache.Position); Assert.Equal(reward.Gold, cache.Contents.GetValueOrDefault("gold"));
        Assert.Equal(reward.Food, cache.Contents.GetValueOrDefault("food"));
        foreach (var (item, count) in reward.Supplies) Assert.Equal(count, cache.Contents[world.Content.Get<ItemDefinition>(item).Stack]);
        Assert.Equal(gold, world.Count("gold")); Assert.Equal(food, world.Count("food"));
        Assert.Equal(rng, world.State.Tactics.RandomState);
        Assert.Single(world.State.Tactics.Events, e => e.Kind == "EncounterRewarded" && e.Source == group[0].Id);
        Assert.Empty(world.State.Tactics.Rewards.Enemies);
        long xp = ExperienceUnits(hero); int equipment = world.State.GeneratedEquipment.Count;
        Strike(world, hero.Id, group);
        Assert.Equal(xp, ExperienceUnits(hero)); Assert.Equal(equipment, world.State.GeneratedEquipment.Count);
        Assert.Single(world.State.Loot, c => c.Id == cache.Id);
        var collect = new TacticalIntent(TacticalAction.CollectCache, Item: cache.Id);
        string inspected = JsonSerializer.Serialize(world.State);
        Assert.True(world.DungeonCombat.Preview(collect).Valid); Assert.Equal(inspected, JsonSerializer.Serialize(world.State));
        Assert.True(world.SubmitDungeon(collect).Success); Assert.Equal(100, world.DungeonCombat.Clock);
        Assert.Equal(gold + reward.Gold, world.Count("gold")); Assert.Equal(food + reward.Food, world.Count("food")); Assert.True(cache.Taken);
        Assert.False(world.DungeonCombat.Preview(collect).Valid);
    }

    [Theory]
    [InlineData("other-floor")] [InlineData("downed")] [InlineData("dead")]
    public void PartialGroupCreditSurvivesRetreatAndExactLoadingBeforeItsFinalCompletion(string contributor)
    {
        using var world = Arena(); var group = Group(world, "encounter.cinder-ambush"); var hero = world.Party.First(); var helper = Helper(world);
        var reward = world.Content.Get<RewardDefinition>(world.Content.Get<EncounterDefinition>(group[0].EncounterId).Reward);
        Strike(world, hero.Id, group[0]);
        Assert.Equal(0, ExperienceUnits(hero)); Assert.DoesNotContain(group[0].Id, world.State.RewardedEncounterIds);
        Assert.True(world.State.NativeCampaign!.EncounterRewards.Pending[group[0].Id].Members[hero.Id].Damage > 0);
        if (contributor is "downed" or "dead")
        {
            Strike(world, 0, hero); // Down, then exhaust actual native bleed turns.
            if (contributor == "dead") for (int i = 0; i < 5; i++) Strike(world, 0, hero);
            Assert.Equal(contributor == "dead" ? LifeState.Dead : LifeState.Downed, hero.Life);
        }
        else { hero.Position = world.State.Position = hero.Position with { Floor = 2 }; world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal(); }
        string root = Path.Combine(Path.GetTempPath(), "roag-native-group-reward-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Strike(world, helper.Id, group[1]); Strike(loaded, helper.Id, loaded.ActorById(group[1].Id));
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Empty(world.State.NativeCampaign.EncounterRewards.Pending);
            Assert.Equal(reward.Experience * ContributionRewards.UnitsPerExperience, ExperienceUnits(hero) + ExperienceUnits(helper));
            if (contributor == "dead") Assert.Equal(0, ExperienceUnits(hero));
            else { Assert.True(ExperienceUnits(hero) > 0); Assert.True(ExperienceUnits(helper) > 0); }
            world.Save(root); using var completed = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(completed.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void EveryRetainedBossKeepsItsAuthoredSignatureEvenWhenItsAttendantsSurvive()
    {
        using var world = Arena(); var hero = world.Party.Single();
        foreach (var definition in world.Content.All<BossDefinition>())
        {
            long id = world.State.NextActorId++;
            var boss = ActorFactory.CreateBoss(world.Content, definition.Id, definition.Variants[0], id, hero.Position.Offset(1, 0), 0);
            world.State.Actors.Add(boss); world.DungeonCombat.RegisterRetainedEnemy(boss, world.DungeonCombat.Clock + 1000000);
            Actor? minion = null;
            if (definition.Minions.Length > 0)
            {
                minion = ActorFactory.Create(world.Content, definition.Minions[0], world.Seed, world.State.NextActorId++, hero.Position.Offset(2, 0), 0);
                minion.EncounterLeader = id; world.State.Actors.Add(minion);
                world.DungeonCombat.RegisterRetainedEnemy(minion, world.DungeonCombat.Clock + 1000000);
                world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, hero.Id, hero.Position,
                    [minion.Position], "test:attendant-work", AttackDelivery.Area, DamageElement.Physical, 1, world.DungeonCombat.Clock));
            }
            var reward = world.Content.Get<RewardDefinition>(definition.Reward); long before = ExperienceUnits(hero);
            Strike(world, hero.Id, boss);
            if (minion != null) Assert.Equal(LifeState.Alive, minion.Life);
            Assert.Equal(before + reward.Experience * ContributionRewards.UnitsPerExperience, ExperienceUnits(hero));
            var cache = Assert.Single(world.State.Loot, c => c.Id == "native-encounter-" + id);
            Assert.Contains(cache.Contents, p => world.State.GeneratedEquipment.TryGetValue(p.Key, out var gear) && gear.Base == reward.Signature);
            if (minion != null) Strike(world, hero.Id, minion);
            Assert.Equal(before + reward.Experience * ContributionRewards.UnitsPerExperience, ExperienceUnits(hero));
            Assert.Single(world.State.Loot, c => c.Id == cache.Id);
            Assert.Empty(world.State.Tactics!.Rewards.Enemies);
        }
        Assert.Equal(48, world.Content.All<BossDefinition>().Count());
    }

    [Fact]
    public void UnownedKillsLeaveTheSameFiniteRewardWithoutInventingAContributorOrPartySupplies()
    {
        using var world = Arena(); var group = Group(world, "encounter.road-wardens"); var hero = world.Party.Single();
        string inventory = JsonSerializer.Serialize(world.State.Inventory);
        Strike(world, 0, group);
        Assert.Equal(0, ExperienceUnits(hero)); Assert.Equal(inventory, JsonSerializer.Serialize(world.State.Inventory));
        Assert.Contains(group[0].Id, world.State.RewardedEncounterIds);
        Assert.Single(world.State.Loot, c => c.Id == "native-encounter-" + group[0].Id);
        Assert.DoesNotContain(world.State.Tactics!.Events, e => e.Kind == "ExperienceGained");
    }

    [Fact]
    public void ARewardExemptSourceCannotPayThroughItsOrdinaryAttendants()
    {
        using var world = Arena(); var group = Group(world, "encounter.cinder-ambush");
        group[0].Tactics!.RewardExempt = true;
        Strike(world, world.Party.First().Id, group[1]);
        Assert.Empty(world.State.NativeCampaign!.EncounterRewards.Pending);
        Strike(world, world.Party.First().Id, group[0]);
        Assert.DoesNotContain(group[0].Id, world.State.RewardedEncounterIds);
        Assert.Equal(0, ExperienceUnits(world.Party.First()));
        Assert.DoesNotContain(world.State.Loot, c => WorldSession.IsEncounterRewardCache(c));
    }

    [Theory]
    [InlineData("unknown-source")] [InlineData("negative")] [InlineData("unknown-member")]
    [InlineData("unhealed")] [InlineData("null")] [InlineData("paid-source")]
    public void InvalidPendingNativeSourceCreditCannotLoad(string reason)
    {
        using var world = Arena(); var group = Group(world, "encounter.cinder-ambush");
        Strike(world, world.Party.First().Id, group[0]);
        var pending = world.State.NativeCampaign!.EncounterRewards.Pending; var ledger = pending[group[0].Id];
        if (reason == "unknown-source") { pending.Remove(group[0].Id); pending[world.State.NextActorId + 10] = ledger; }
        if (reason == "negative") ledger.Members.Values.Single().Damage = -1;
        if (reason == "unknown-member") { ledger.Members.Clear(); ledger.Members[999999] = new() { Damage = 1 }; }
        if (reason == "unhealed") ledger.UnhealedDamage[world.Party.First().Id] = 1;
        if (reason == "null") pending[group[0].Id] = null!;
        if (reason == "paid-source") world.State.RewardedEncounterIds.Add(group[0].Id);
        string root = Path.Combine(Path.GetTempPath(), "roag-invalid-native-source-reward-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root);
            Assert.Throws<InvalidDataException>(() => { using var invalid = WorldSession.Load(root); });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("missing")] [InlineData("supplies")] [InlineData("source")] [InlineData("position")]
    [InlineData("gear")] [InlineData("unknown-receipt")]
    public void AuthoredSpoilsAndTheirSeededGearCannotBeRewrittenByAContinuingSave(string reason)
    {
        using var world = Arena(); var group = Group(world, "encounter.road-wardens");
        Strike(world, world.Party.First().Id, group);
        var cache = Assert.Single(world.State.Loot, c => IsCache(c, group[0].Id));
        if (reason == "missing") world.State.Loot.Remove(cache);
        if (reason == "supplies") cache.Contents["gold"] = cache.Contents.GetValueOrDefault("gold") + 1;
        if (reason == "source") cache.SourceActor = 1000;
        if (reason == "position") cache.Position = cache.Position.Offset(1, 0);
        if (reason == "gear")
        {
            string item = cache.Contents.Keys.Single(world.State.GeneratedEquipment.ContainsKey);
            var previous = world.State.GeneratedEquipment[item];
            world.State.GeneratedEquipment[item] = previous with
            { Affixes = previous.Affixes.Length > 1 ? previous.Affixes.Reverse().ToArray() : [] };
        }
        if (reason == "unknown-receipt") world.State.RewardedEncounterIds.Add(999999);
        string root = Path.Combine(Path.GetTempPath(), "roag-invalid-native-spoils-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root);
            Assert.Throws<InvalidDataException>(() => { using var invalid = WorldSession.Load(root); });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }

        static bool IsCache(LootCache value, long source) => value.Id == "native-encounter-" + source;
    }
}
