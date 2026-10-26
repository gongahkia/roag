using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void PhysicalLootPublicCommandsMatchOriginalPickupsEnrichmentAndBoonChains()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Tactics/Fixtures/original-physical-loot.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer);
            var origin = a.Hero.Position;
            a.Hero.Health -= input.GetProperty("missing").GetInt32();
            a.World.State.Inventory["gold"] = 0;
            a.Friend.Position = new(110, 110);
            a.Enemy.Position = new(110, 109);
            if (input.GetProperty("kill").GetBoolean())
            {
                a.Enemy.Position = origin.Offset(1, 0); a.Enemy.Health = 1;
                a.Enemy.Tactics!.DefinitionId = "core:enemy/ruin_skitter";
                a.Enemy.Tactics.RegularEnemy = true;
                a.Enemy.Tactics.CurrencyDrop = sample.GetProperty("currency").GetInt32();
                a.Enemy.Tactics.ExperienceReward = sample.GetProperty("experience").GetInt32();
            }
            foreach (var wall in input.GetProperty("walls").EnumerateArray())
                a.World.Chunks.Edit(origin.Offset(wall[0].GetInt32(), wall[1].GetInt32()), Material.Wall);
            foreach (var grant in input.GetProperty("boons").EnumerateArray())
                a.Hero.Tactics!.Boons.Inventory = a.Hero.Tactics.Boons.Inventory.Grant("core:boon/" + grant.GetProperty("id").GetString(),
                    BoonRarity.Common, ClassKind.Adventurer, grant.TryGetProperty("count", out var count) ? count.GetInt32() : 1);
            foreach (var drop in input.GetProperty("drops").EnumerateArray())
            {
                bool currency = drop.GetProperty("kind").GetString() == "currency";
                var spawned = a.Combat.SpawnLoot(currency ? GroundLootKind.Currency : GroundLootKind.Health,
                    currency ? drop.GetProperty("amount").GetInt32() : 10,
                    origin.Offset(drop.GetProperty("dx").GetInt32(), drop.GetProperty("dy").GetInt32()));
                if (drop.TryGetProperty("carried", out var carried) && carried.GetBoolean())
                    a.Combat.State.Loot.Drops[^1] = spawned with { CarriedBy = a.Hero.Id };
            }
            a.Combat.State.RandomState = input.GetProperty("seed").GetInt32(); a.World.Reveal();
            int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetProperty("kind").GetString() switch
                { "move" => TacticalAction.Move, "dash" => TacticalAction.Dash, "basic_attack" => TacticalAction.BasicAttack, _ => TacticalAction.Wait });
                a.Next();
                var expected = sample.GetProperty("expected")[index++];
                var trace = new List<string>();
                foreach (var e in a.Combat.State.Events.Where(e => e.Sequence >= first))
                {
                    if (e.Kind == "BoonActivated") trace.Add(e.Kind + ":" + e.Ability);
                    else if (e.Kind is "CurrencyCollected" or "CurrencyDropped" or "CurrencyDropEnriched")
                        trace.Add($"{e.Kind}:{e.Loot!.Drop}:{e.Amount}");
                    else if (e.Kind == "SupplyCollected") trace.Add($"{e.Kind}:{e.Loot!.Drop}:{e.Amount}:{e.Loot.ActualGain}");
                    else if (e.Kind == "Healed") trace.Add(e.Kind + ":" + e.Amount);
                }
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace),
                    name + " / " + index + " trace: " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("health").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), a.World.State.Inventory["gold"]);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                Assert.Equal(origin.Offset(expected.GetProperty("dx").GetInt32(), expected.GetProperty("dy").GetInt32()), a.Hero.Position);
                Assert.Equal(expected.GetProperty("drops").EnumerateArray().Select(d =>
                        (d.GetProperty("id").GetInt64(), d.GetProperty("kind").GetString()!, d.GetProperty("amount").GetInt32(),
                        d.GetProperty("dx").GetInt32(), d.GetProperty("dy").GetInt32(), d.GetProperty("carried").GetBoolean())),
                    a.Combat.State.Loot.Drops.Select(d => (d.Id, d.Kind == GroundLootKind.Currency ? "currency" : "health", d.Amount,
                        d.Position.X - origin.X, d.Position.Y - origin.Y, d.CarriedBy != 0)));
                TacticalValidation.Validate(a.World.Plan, a.World.State);
                decisions++;
            }
        }
        Assert.Equal(24, decisions);
    }

    [Fact]
    public void NativeKillSpawnsPhysicalRewardsExactlyOnceIncludingAutonomousKills()
    {
        using var a = new Arena(ClassKind.Tinkerer);
        a.Enemy.Tactics!.CurrencyDrop = 7; a.Enemy.Tactics.HealthDrop = true; a.Enemy.Tactics.ExperienceReward = 3;
        a.Enemy.Health = 3; int gold = a.World.Count("gold");
        a.Act(TacticalAction.Special); a.Next(); a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(LifeState.Dead, a.Enemy.Life);
        Assert.Equal(gold, a.World.Count("gold")); // still on the ground
        Assert.Equal(3, a.Hero.Tactics!.Build!.Experience);
        Assert.Equal(new[] { (GroundLootKind.Currency, 7), (GroundLootKind.Health, 10) },
            a.Combat.State.Loot.Drops.Select(d => (d.Kind, d.Amount)));
        Assert.All(a.Combat.State.Loot.Drops, d => Assert.Equal(a.Enemy.Id, d.DefeatedActor));
        a.Hit(a.Enemy, 1000, source: a.Hero);
        Assert.Equal(2, a.Combat.State.Loot.Drops.Count);
        Assert.Equal(3, a.Combat.State.Loot.NextDrop);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void RewardExemptVictimsCreateNeitherDropsNorFuneralBonus()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Tactics!.CurrencyDrop = 7; a.Enemy.Tactics.HealthDrop = true; a.Enemy.Tactics.RewardExempt = true;
        Grant(a.Hero, "core:boon/funeral_coin");
        a.Hit(a.Enemy, 1000, source: a.Hero);
        Assert.Empty(a.Combat.State.Loot.Drops);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "CurrencyDropEnriched");
        Assert.True(a.Enemy.Tactics.RewardsClaimed);
    }

    [Fact]
    public void CollectionUsesEachMembersPositionAndBoonsWithGloballySharedGold()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Friend.Position = new(105, 105, 1);
        Grant(a.Friend, "core:boon/scavengers_hook"); Grant(a.Friend, "core:boon/prospectors_heart");
        a.Friend.Health -= 5; a.Hero.Health -= 5;
        var friendGold = a.Combat.SpawnLoot(GroundLootKind.Currency, 9, a.Friend.Position.Offset(0, -1));
        var wrongFloor = a.Combat.SpawnLoot(GroundLootKind.Currency, 11, friendGold.Position with { Floor = 0 });
        int before = a.World.Count("gold");
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Wait);
        Assert.Equal(before + 9, a.World.Count("gold"));
        Assert.Equal(a.Friend.MaxHealth - 4, a.Friend.Health);
        Assert.Equal(a.Hero.MaxHealth - 5, a.Hero.Health);
        Assert.Equal(new[] { wrongFloor }, a.Combat.State.Loot.Drops);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "CurrencyCollected" && e.Source == a.Friend.Id && e.Loot!.Drop == friendGold.Id);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void GatherUsesOwnersPrivateSightWithoutChangingControlledVision()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Friend.Position = new(105, 105, 1);
        a.World.Reveal();
        var visible = a.World.Visible.ToHashSet();
        var position = a.Friend.Position.Offset(1, 0);
        Assert.DoesNotContain(position, visible);
        a.Combat.SpawnLoot(GroundLootKind.Currency, 4, position);
        var gather = Activation(a, a.Friend, "core:boon/scavengers_hook", 1);
        Apply(a, gather, BoonEffectKind.GatherPickups);
        Assert.Empty(a.Combat.State.Loot.Drops);
        Assert.True(visible.SetEquals(a.World.Visible));
        Assert.Equal(a.Hero.Id, a.Combat.State.ControlledActor);
    }

    [Fact]
    public void EnrichmentFollowsKillIdentityAndDoesNotCreateOrCreditMissingDrops()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Tactics!.CurrencyDrop = 2;
        a.Hit(a.Enemy, 1000, source: a.Hero);
        var other = a.Combat.SpawnLoot(GroundLootKind.Currency, 9, a.Hero.Position.Offset(-1, 0));
        int gold = a.World.Count("gold");
        var activation = Activation(a, a.Hero, "core:boon/funeral_coin", 3, a.Enemy);
        Apply(a, activation, BoonEffectKind.EnrichDrop);
        Assert.Equal(5, a.Combat.State.Loot.Drops[0].Amount);
        Assert.Equal(other, a.Combat.State.Loot.Drops[1]);
        Assert.Equal(gold, a.World.Count("gold"));
        a.Combat.State.Loot.Drops.RemoveAt(0);
        Apply(a, activation, BoonEffectKind.EnrichDrop);
        Assert.Equal(new[] { other }, a.Combat.State.Loot.Drops);
    }

    [Fact]
    public void HealthPickupCleansBurningAndCreditsOnlyActualEnemyCausedHealing()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Hit(a.Hero, 15);
        Grant(a.Hero, "core:boon/field_dressing");
        Apply(a, Activation(a, a.Enemy, "core:boon/emberbrand", 2, a.Hero), BoonEffectKind.ApplyStatus);
        a.Combat.SpawnLoot(GroundLootKind.Health, 10, a.Hero.Position.Offset(1, 0));
        a.Act(TacticalAction.Move);
        Assert.Null(a.Hero.Tactics!.Burning);
        Assert.Equal(a.Hero.MaxHealth - 4, a.Hero.Health);
        Assert.Equal(11, a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id].Healing);
        Assert.Equal(4, a.Combat.State.Rewards.Enemies[a.Enemy.Id].UnhealedDamage[a.Hero.Id]);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "SupplyCollected" && e.Loot is { ActualGain: 10 });
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void PreviewAndInvalidActionsCannotCollectLoot()
    {
        using var a = new Arena(ClassKind.Adventurer);
        var drop = a.Combat.SpawnLoot(GroundLootKind.Currency, 5, a.Hero.Position.Offset(1, 0));
        string before = JsonSerializer.Serialize(a.Combat.State); int gold = a.World.Count("gold");
        Assert.True(a.Combat.Preview(new(TacticalAction.Move, Facing.East)).Valid);
        Assert.False(a.Combat.Commit(new(TacticalAction.Move, (Facing)99)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
        Assert.Equal(gold, a.World.Count("gold"));
        Assert.Equal(new[] { drop }, a.Combat.State.Loot.Drops);
    }

    [Fact]
    public void LootAndReceiptsPersistThroughRetreatCollectionAndReloadWithoutDuplicates()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Tactics!.CurrencyDrop = 7; a.Enemy.Tactics.HealthDrop = true;
        a.Hit(a.Enemy, 1000, source: a.Hero);
        a.Hero.Position = a.World.State.Position = new(100, 100, 1); a.World.Reveal();
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(2, a.Combat.State.Loot.Drops.Count);
        string directory = Path.Combine(Path.GetTempPath(), "roag-loot-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot);
            var combat = new TacticalCombat(loaded, _ => { });
            var restored = loaded.Party.Single();
            foreach (var (world, engine, hero) in new[] { (a.World, a.Combat, a.Hero), (loaded, combat, restored) })
            {
                hero.Position = world.State.Position = new(102, 100); hero.Health -= 2; world.Reveal();
                Assert.True(engine.Commit(new(TacticalAction.Move, Facing.East)).Success);
                Assert.Equal(hero.Id, engine.AdvanceToDecision()!.Id);
            }
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(combat.State));
            Assert.Equal(a.World.Count("gold"), loaded.Count("gold"));
            Assert.Empty(combat.State.Loot.Drops);
            loaded.Save(slot);
            using var collected = WorldSession.Load(slot);
            Assert.Empty(collected.State.Tactics!.Loot.Drops);
            Assert.Equal(3, collected.State.Tactics.Loot.NextDrop);
            Assert.True(collected.State.Actors.Single(x => x.Id == a.Enemy.Id).Tactics!.RewardsClaimed);
            Assert.Equal(loaded.Count("gold"), collected.Count("gold"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void LootValidationRejectsReusedIdentitiesDanglingOwnersAndBadAmounts()
    {
        using var a = new Arena(ClassKind.Adventurer);
        var drop = a.Combat.SpawnLoot(GroundLootKind.Currency, 2, a.Hero.Position.Offset(1, 0));
        foreach (var invalid in new[] { drop with { Id = a.Combat.State.Loot.NextDrop }, drop with { Amount = 0 },
            drop with { DefeatedActor = a.Enemy.Id }, drop with { CarriedBy = a.World.State.NextActorId + 1 } })
        {
            a.Combat.State.Loot.Drops[0] = invalid;
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        }
        a.Combat.State.Loot.Drops[0] = drop;
        a.Combat.State.Loot.Drops.Add(drop);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Combat.State.Loot.Drops.RemoveAt(1);
        a.Combat.State.Loot.Drops[0] = drop with { CarriedBy = a.Enemy.Id };
        a.Combat.State.Events.Clear();
        Assert.True(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
    }

    [Fact]
    public void TwoPartyGatherersCannotCollectTheSameDropTwice()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        Grant(a.Hero, "core:boon/scavengers_hook"); Grant(a.Friend, "core:boon/scavengers_hook");
        a.Combat.SpawnLoot(GroundLootKind.Currency, 9, new(100, 101));
        int gold = a.World.Count("gold");
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Wait);
        Assert.Equal(gold + 9, a.World.Count("gold"));
        Assert.Empty(a.Combat.State.Loot.Drops);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "CurrencyCollected");
    }

    [Fact]
    public void DownedGatherersCannotCollectHealingOrReviveWithoutRescue()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        var activation = Activation(a, a.Hero, "core:boon/scavengers_hook", 4);
        a.Combat.SpawnLoot(GroundLootKind.Health, 10, a.Hero.Position.Offset(0, -1));
        a.Combat.SpawnLoot(GroundLootKind.Currency, 9, a.Hero.Position.Offset(1, 0));
        a.Hit(a.Hero, 1000);
        int gold = a.World.Count("gold");
        Apply(a, activation, BoonEffectKind.GatherPickups);
        Assert.Equal(LifeState.Downed, a.Hero.Life);
        Assert.Equal(0, a.Hero.Health); Assert.Equal(5, a.Hero.Tactics!.BleedTurns);
        Assert.Equal(2, a.Combat.State.Loot.Drops.Count);
        Assert.Equal(gold, a.World.Count("gold"));
    }

    [Fact]
    public void DedicatedHealthOverflowKeepsItsOriginalCappedOneHitWard()
    {
        using var a = new Arena(ClassKind.Adventurer);
        Grant(a.Hero, "core:boon/overflow_chalice");
        a.Hero.Health -= 1;
        a.Combat.SpawnLoot(GroundLootKind.Health, 10, a.Hero.Position.Offset(1, 0));
        a.Act(TacticalAction.Move);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        var ward = a.Hero.Tactics!.Boons.Memory["core:boon/overflow_chalice"];
        Assert.Equal(2, ward.Ward); Assert.Equal(400, ward.ExpiresAt);
        a.Hit(a.Hero, 5);
        Assert.Equal(a.Hero.MaxHealth - 3, a.Hero.Health);
        a.Hit(a.Hero, 5);
        Assert.Equal(a.Hero.MaxHealth - 8, a.Hero.Health);
    }

    [Fact]
    public void UncollectedLootRetainsItsVictimButCollectedHistoryDoesNotGrowForever()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Tactics!.CurrencyDrop = 7;
        a.Hit(a.Enemy, 1000, source: a.Hero);
        a.Combat.State.Events.Clear();
        Assert.True(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
        a.Hero.Position = a.World.State.Position = new(102, 100);
        a.Act(TacticalAction.Move);
        Assert.Empty(a.Combat.State.Loot.Drops);
        a.Combat.State.Events.Clear();
        Assert.False(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
        var fresh = a.Combat.SpawnLoot(GroundLootKind.Currency, 2, new(104, 100));
        Assert.Equal(2, fresh.Id); // removing a collected record never reuses its identity
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }
}
