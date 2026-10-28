using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static Actor PrepareOaths(Arena a, bool area = false, long partnerDue = 10000)
    {
        EnemyProfile(a, "oath_twin_a"); a.Enemy.Position = new(101, 100); a.Friend.Position = new(110, 110);
        a.Hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 37);
        var partner = RevivalTestEnemy(a, "oath_twin_b", area ? new(101, 99) : new(102, 100), partnerDue);
        a.Combat.LinkOathTwins(a.Enemy.Id, partner.Id);
        return partner;
    }

    [Fact]
    public void OathPublicCommandsMatchOriginalSharingBatchDeathsSkullBurningAndPermanentBreaks()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-oath-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: name == "turns" ? 50 : 10000);
            var partner = PrepareOaths(a, input.GetProperty("area").GetBoolean(), name == "turns" ? 60 : 10000);
            if (name == "turns") { a.Enemy.Position = new(103, 100); partner.Position = new(103, 101); }
            if (name == "armour")
            {
                a.Enemy.Tactics!.InnateStats = a.Enemy.Tactics.InnateStats! with { Defense = 2 };
                partner.Tactics!.InnateStats = partner.Tactics.InnateStats! with { Defense = 99 };
            }
            if (name is "both_die" or "grave_dividend") { a.Enemy.Health = 2; partner.Health = 2; a.Hero.Health = 10; }
            if (name is "shared_kill" or "shared_grave_kill") partner.Health = 1;
            if (name == "shared_grave_kill") a.Hero.Health = 10;
            if (input.GetProperty("boon").GetString() is { } boon)
                for (int i = 0; i < input.GetProperty("stacks").GetInt32(); i++) Grant(a.Hero, "core:boon/" + boon);
            int seed = input.GetProperty("combat_seed").GetInt32();
            a.Combat.State.RandomState = seed; a.World.State.Inventory["gold"] = 0; a.World.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                if (name == "distance") partner.Position = new(index == 0 ? 107 : 102, 100);
                if (name == "wall")
                {
                    partner.Position = new(103, 100);
                    a.World.Chunks.Edit(new(102, 100), index == 0 ? Material.Wall : Material.Floor);
                }
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() switch { "basic_attack" => TacticalAction.BasicAttack, "sweeping_slash" => TacticalAction.Special, _ => TacticalAction.Wait });
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
                var expected = sample.GetProperty("expected")[index++];
                var events = a.Combat.State.Events.Where(e => e.Sequence >= first).ToArray();
                string Target(long id) => id == a.Enemy.Id ? "a" : id == partner.Id ? "b" : id == a.Hero.Id ? "hero" : "other";
                var trace = events.Select(e => e.Kind switch
                {
                    "EntityKilled" => "killed:" + Target(e.Target), "BoonActivated" => "boon:" + e.Ability,
                    "ExperienceGained" or "CurrencyDropped" => $"{e.Kind}:{e.Amount}", _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("damage").EnumerateArray().Select(d =>
                    (d.GetProperty("time").GetInt64(), d.GetProperty("target").GetString(), d.GetProperty("loss").GetInt32(),
                        d.GetProperty("shared").GetBoolean(), d.GetProperty("given").GetInt32(), d.GetProperty("received").GetInt32())),
                    events.Where(e => e.Kind is "DamageTaken" or "DamageBlocked").Select(e =>
                        (e.Time, (string?)Target(e.Target), e.Amount, e.Oath?.SharedOnly ?? false, e.Oath?.Given ?? 0, e.Oath?.Received ?? 0)));
                Assert.All(events.Where(e => e.Kind == "DamageTaken" && e.Oath?.SharedOnly == true), e => Assert.Equal("core:ability/oath_share", e.Ability));
                Assert.Equal(expected.GetProperty("actions").EnumerateArray().Select(e => e.GetString()),
                    events.Where(e => e.Kind is "WindupStarted" or "AttackPerformed").Select(e => $"{e.Kind}:{e.Time}:{Target(e.Source)}:{e.Ability}"));
                Assert.Equal(expected.GetProperty("hits").GetInt32(), events.Count(e => e.Kind == "HitConfirmed"));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("a_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(expected.GetProperty("b_hp").GetInt32(), partner.Health);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("ax").GetInt32(), 100 + expected.GetProperty("ay").GetInt32()), a.Enemy.Position);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("bx").GetInt32(), 100 + expected.GetProperty("by").GetInt32()), partner.Position);
                // Original dead twins are removed, so only retained originals have observable broken flags.
                if (a.Enemy.Life == LifeState.Alive) Assert.Equal(expected.GetProperty("a_broken").GetBoolean(), a.Enemy.Tactics!.Enemy!.OathBroken);
                if (partner.Life == LifeState.Alive) Assert.Equal(expected.GetProperty("b_broken").GetBoolean(), partner.Tactics!.Enemy!.OathBroken);
                Assert.Equal(expected.GetProperty("xp").GetInt32(), a.Hero.Tactics!.Build!.Experience);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), a.World.Count("gold"));
                Assert.Equal(expected.GetProperty("drops").GetInt32(), a.Combat.State.Loot.Drops.Count);
                if (a.Hero.Tactics.Build.Level > 1)
                {
                    Assert.Equal(expected.GetProperty("random").GetInt32(), a.Hero.Tactics.Build.RandomState);
                    Assert.Equal(seed, a.Combat.State.RandomState);
                }
                else Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(27, decisions);
    }

    [Theory]
    [InlineData("intact")]
    [InlineData("broken")]
    [InlineData("dead")]
    [InlineData("windup")]
    public void OathWorldSaveRetainsPartnersBrokenStateAndPendingAttacks(string phase)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: phase == "windup" ? 50 : 10000);
        var partner = PrepareOaths(a);
        if (phase == "broken") partner.Position = new(107, 100);
        if (phase == "dead") partner.Health = 1;
        a.Act(phase is "intact" or "dead" ? TacticalAction.BasicAttack : TacticalAction.Wait);
        RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
        if (phase == "broken") partner.Position = new(102, 100);
        string directory = Path.Combine(Path.GetTempPath(), "roag-oath-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
            for (int i = 0; i < 3; i++)
            {
                a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); RunRevivalEnemies(loaded, resumed, a.Hero.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
            }
            Assert.Equal(phase is "broken" or "dead", a.Enemy.Tactics!.Enemy!.OathBroken);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("wall")]
    [InlineData("cache")]
    [InlineData("distance")]
    [InlineData("floor")]
    public void OathObstructionOrSeparationBreaksPermanentlyAndCannotBeSavedAsIntact(string obstacle)
    {
        using var a = new Arena(ClassKind.Adventurer); var partner = PrepareOaths(a); partner.Position = new(103, 100);
        var cache = new LootCache { Id = "oath-test-cache", Position = new(102, 100), Contents = new() { ["gold"] = 1 } };
        if (obstacle == "wall") a.World.Chunks.Edit(new(102, 100), Material.Wall);
        if (obstacle == "cache") a.World.State.Loot.Add(cache);
        if (obstacle == "distance") partner.Position = new(107, 100);
        if (obstacle == "floor") partner.Position = new(103, 100, 1);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        Assert.True(a.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Valid);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Act(TacticalAction.BasicAttack); a.Next();
        Assert.Equal(4, a.Enemy.Health); Assert.Equal(8, partner.Health);
        Assert.True(a.Enemy.Tactics!.Enemy!.OathBroken); Assert.True(partner.Tactics!.Enemy!.OathBroken);
        a.World.Chunks.Edit(new(102, 100), Material.Floor); a.World.State.Loot.Remove(cache); partner.Position = new(103, 100);
        a.Act(TacticalAction.BasicAttack);
        Assert.Equal(LifeState.Dead, a.Enemy.Life); Assert.Equal(8, partner.Health);
        Assert.Throws<ArgumentException>(() => a.Combat.LinkOathTwins(a.Enemy.Id, partner.Id));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void OathSharingOffscreenCreditsOnlyTheAttackingPartyMember()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer); var partner = PrepareOaths(a);
        a.Enemy.Position = new(101, 100, 1); partner.Position = new(102, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Act(TacticalAction.Wait); Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.World.Visible.Clear(); a.Act(TacticalAction.BasicAttack); a.Next();
        Assert.Equal(6, a.Enemy.Health); Assert.Equal(6, partner.Health);
        foreach (var victim in new[] { a.Enemy, partner })
        {
            var ledger = a.Combat.State.Rewards.Enemies[victim.Id];
            Assert.Equal(2, ledger.Members[a.Friend.Id].Damage); Assert.False(ledger.Members.ContainsKey(a.Hero.Id));
        }
        Assert.DoesNotContain(a.World.Visible, p => p.Floor == 1);
        Assert.False(a.Enemy.Tactics!.Enemy!.OathBroken); TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void OathPartyContributionCountsActualSharedLossAndPaysEachTwinOnce()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer); var partner = PrepareOaths(a);
        a.Friend.Position = new(101, 101);
        a.Act(TacticalAction.BasicAttack); Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.BasicAttack, Facing.North); Assert.Same(a.Hero, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.BasicAttack); Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.BasicAttack, Facing.North); Assert.Same(a.Hero, a.Combat.AdvanceToDecision());
        Assert.Equal(1, a.Enemy.Health); Assert.Equal(1, partner.Health);
        a.Act(TacticalAction.BasicAttack); Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Move); Assert.Same(a.Hero, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Wait); Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.BasicAttack, Facing.North); Assert.Same(a.Hero, a.Combat.AdvanceToDecision());
        Assert.Equal(LifeState.Dead, a.Enemy.Life); Assert.Equal(LifeState.Dead, partner.Life);
        Assert.Equal(4, a.Hero.Tactics!.Build!.Experience); Assert.Equal(3, a.Friend.Tactics!.Build!.Experience);
        Assert.Equal(500000, a.Hero.Tactics.ExperienceFraction); Assert.Equal(500000, a.Friend.Tactics.ExperienceFraction);
        Assert.Empty(a.Combat.State.Rewards.Enemies); Assert.Equal(2, a.Combat.State.Loot.Drops.Count);
        Assert.All(new[] { a.Enemy, partner }, x => Assert.True(x.Tactics!.RewardsClaimed));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void OathInvalidBindingIsAtomicAndBrokenPartnersDoNotPreventHistoryReclamation()
    {
        using var a = new Arena(ClassKind.Adventurer); EnemyProfile(a, "oath_twin_a"); a.Enemy.Position = new(101, 100);
        var partner = RevivalTestEnemy(a, "oath_twin_b", new(103, 100));
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<ArgumentException>(() => a.Combat.LinkOathTwins(a.Enemy.Id, a.Hero.Id));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.World.Chunks.Edit(new(102, 100), Material.Wall); before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<ArgumentException>(() => a.Combat.LinkOathTwins(a.Enemy.Id, partner.Id));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.World.Chunks.Edit(new(102, 100), Material.Floor); a.Combat.LinkOathTwins(a.Enemy.Id, partner.Id);
        Assert.True(TacticalReferences.RetainsActor(a.World.State, partner.Id));
        partner.Health = 1; a.Act(TacticalAction.BasicAttack); a.Next();
        a.Combat.State.Events.Clear(); a.Combat.State.Loot.Drops.Clear();
        Assert.Equal(LifeState.Dead, partner.Life); Assert.True(a.Enemy.Tactics!.Enemy!.OathBroken);
        Assert.False(TacticalReferences.RetainsActor(a.World.State, partner.Id));
        a.Combat.State.Participants.Remove(partner.Id); a.World.State.Actors.Remove(partner);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        a.Enemy.Tactics.Enemy.OathPartner = a.World.State.NextActorId;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void OathValidationRejectsForgedPartnersAndMismatchedBrokenFlags()
    {
        using var a = new Arena(ClassKind.Adventurer); var partner = PrepareOaths(a);
        partner.Tactics!.Enemy!.OathPartner = a.Hero.Id;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        partner.Tactics.Enemy.OathPartner = a.Enemy.Id; partner.Tactics.Enemy.OathBroken = true;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        partner.Tactics.Enemy.OathBroken = false;
        a.Enemy.Tactics!.Enemy!.OathPartner = a.Enemy.Id;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Enemy.Tactics.Enemy.OathPartner = partner.Id; TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OathTransfersBypassRecipientWardWithoutRepeatingDefenseForCombinedHits(bool area)
    {
        using var a = new Arena(ClassKind.Adventurer); var partner = PrepareOaths(a, area);
        Grant(partner, "core:boon/beggars_cap"); partner.Tactics!.Boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 5, 200, 0);
        a.Act(area ? TacticalAction.Special : TacticalAction.BasicAttack);
        Assert.Equal(6, a.Enemy.Health); Assert.Equal(area ? 7 : 6, partner.Health);
        Assert.Equal(area ? 0 : 5, partner.Tactics.Boons.WardTotal(a.Combat.Clock));
        var receipt = Assert.Single(a.Combat.State.Events, e => e.Kind == "DamageTaken" && e.Target == partner.Id);
        Assert.Equal(area ? 1 : 2, receipt.Amount); Assert.Equal(!area, receipt.Oath!.SharedOnly);
        Assert.Equal(area ? 2 : 1, a.Combat.State.Events.Count(e => e.Kind == "HitConfirmed"));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void OathSavedCollisionQueriesMatchLivePropsTerrainAndBulwarksWithoutChangingTheWorld()
    {
        using var a = new Arena(ClassKind.Geomancer);
        a.Act(TacticalAction.Special);
        var block = a.Hero.Tactics!.Resources.Bulwark!.Position;
        using var snapshot = new ChunkCache(a.World.Plan, a.World.State, a.World.Content);
        var origin = a.World.Plan.Towns[0].Center;
        var points = Enumerable.Range(-10, 21).SelectMany(y => Enumerable.Range(-10, 21).Select(x => origin.Offset(x, y)))
            .Concat(a.World.State.Discoveries.Take(4).Select(d => d.Position))
            .Concat(a.World.State.Loot.Take(4).Select(c => c.Position)).Append(block).ToArray();
        string before = JsonSerializer.Serialize(a.World.State);
        foreach (var p in points)
            Assert.Equal(a.World.Plan.Contains(p) && !Materials.Opaque(a.World.Chunks.At(p)) && a.World.ObjectAt(p) is not { Solid: true },
                WorldSession.SnapshotProjectileOpen(a.World.Plan, a.World.State, a.World.Content, snapshot, p));
        Assert.False(WorldSession.SnapshotProjectileOpen(a.World.Plan, a.World.State, a.World.Content, snapshot, block));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }
}
