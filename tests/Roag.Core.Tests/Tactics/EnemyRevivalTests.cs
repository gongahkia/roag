using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static string NativeActorSnapshot(WorldSession world) => JsonSerializer.Serialize(world.State.Actors
        .Where(a => a.Tactics != null).Select(a => new { a.Id, a.Health, a.Life, a.Position, a.Tactics }));

    private static Actor RevivalTestEnemy(Arena a, string definition, WorldPosition position, long ready = 10000)
    {
        var actor = ActorFactory.Create(a.World.Content, "character.roag-revived", a.World.Seed,
            a.World.State.NextActorId++, position, a.Enemy.HomeTown, a.Enemy.Culture, a.Enemy.FactionId);
        a.World.State.Actors.Add(actor); a.Combat.RegisterEnemy(actor, "core:enemy/" + definition, ready);
        return actor;
    }

    private static Actor PrepareCantor(Arena a)
    {
        EnemyProfile(a, "grave_cantor"); a.Enemy.Position = new(103, 100);
        a.Hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 37);
        a.Friend.Position = new(110, 110);
        var victim = RevivalTestEnemy(a, "mossbound_guard", new(101, 100));
        victim.Tactics!.InnateStats = victim.Tactics.InnateStats! with { MaxHealth = 6 }; victim.Health = 3;
        return victim;
    }

    private static void RunRevivalEnemies(WorldSession world, TacticalCombat combat, long hero)
    {
        for (int i = 0; ; i++)
        {
            Assert.True(i < 60, "Enemy commands did not return control");
            var actor = combat.AdvanceToDecision(); Assert.NotNull(actor);
            if (actor.Id == hero) return;
            combat.CommitEnemyTurn(hero, (from, goals) => EnemyPlanningTests.FirstStep(from, goals,
                p => p.X is >= 95 and <= 110 && p.Y is >= 95 and <= 110 && world.CanEnter(p, actor.Id)));
        }
    }

    private static void CommitCantor(Arena a)
    {
        a.Act(TacticalAction.BasicAttack); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
        a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
        a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
        Assert.Equal(300, a.Combat.Clock);
        Assert.Equal(EnemySupportKind.Revive, Assert.Single(a.Combat.State.Attacks).Support!.Kind);
    }

    [Fact]
    public void CantorPublicCommandsMatchOriginalClaimsBlockedLandingsCompetitionAndRevivedRewards()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-revival-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = sample.GetProperty("input").GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 200); PrepareCantor(a);
            if (name == "competing") RevivalTestEnemy(a, "grave_cantor", new(103, 101), 200);
            a.Combat.State.RandomState = 37; a.World.State.Inventory["gold"] = 0; a.World.Reveal(); int index = 0;
            foreach (var command in sample.GetProperty("input").GetProperty("commands").EnumerateArray())
            {
                if (index == 3 && name is "wall" or "stairs") a.World.Chunks.Edit(new(101, 100), name == "wall" ? Material.Wall : Material.StairsDown);
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() switch { "basic_attack" => TacticalAction.BasicAttack, "move" => TacticalAction.Move, _ => TacticalAction.Wait });
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "SupportResolved" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "EnemyRevived" => $"revived:{e.Time}:{e.Ability}", "RevivalBlocked" => $"blocked:{e.Time}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}",
                    "ExperienceGained" or "CurrencyDropped" => $"{e.Kind}:{e.Time}:{e.Amount}", _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("xp").GetInt32(), a.Hero.Tactics!.Build!.Experience);
                // Original level-up RNG belongs to the per-member build in the party adaptation.
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Hero.Tactics.Build.RandomState);
                Assert.Equal(37, a.Combat.State.RandomState);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), a.World.Count("gold"));
                Assert.Equal(expected.GetProperty("drops").GetInt32(), a.Combat.State.Loot.Drops.Count);
                Assert.Equal(expected.GetProperty("spent").GetBoolean(), a.Enemy.Tactics!.Enemy!.RevivalSpent);
                Assert.Equal(expected.GetProperty("corpses").EnumerateArray().Select(c =>
                    (c.GetProperty("x").GetInt32(), c.GetProperty("y").GetInt32(), c.GetProperty("claimed").GetBoolean())),
                    a.Combat.State.Corpses.Select(c => { var actor = a.World.State.Actors.Single(x => x.Id == c.Actor);
                        return (actor.Position.X - 100, actor.Position.Y - 100, c.ClaimedBy != 0); }));
                Assert.Equal(expected.GetProperty("revived").EnumerateArray().Select(e =>
                    (e.GetProperty("x").GetInt32(), e.GetProperty("y").GetInt32(), e.GetProperty("hp").GetInt32(), e.GetProperty("max_hp").GetInt32())),
                    a.World.State.Actors.Where(x => x.Life == LifeState.Alive && x.Tactics?.Enemy?.RevivalOf > 0)
                        .Select(x => (x.Position.X - 100, x.Position.Y - 100, x.Health, x.MaxHealth)));
                foreach (var (key, kind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                    Assert.Equal(expected.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : expected.GetProperty(key).GetInt64(),
                        a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == kind)?.Due);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(30, decisions);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("revived")]
    [InlineData("cancelled")]
    public void CantorActualWorldSaveRetainsClaimsBudgetAndRewardExemptAncestry(string phase)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 200); var victim = PrepareCantor(a); CommitCantor(a);
        if (phase == "revived") { a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); }
        if (phase == "cancelled") Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        string directory = Path.Combine(Path.GetTempPath(), "roag-revival-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            for (int i = 0; i < 2; i++)
            {
                a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); RunRevivalEnemies(loaded, resumed, a.Hero.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
                Assert.Equal(a.Hero.Health, loaded.Party.Single().Health);
            }
            Assert.True(loaded.State.Actors.Single(x => x.Id == a.Enemy.Id).Tactics!.Enemy!.RevivalSpent);
            if (phase == "cancelled") Assert.DoesNotContain(loaded.State.Actors, x => x.Tactics?.Enemy?.RevivalOf > 0);
            else Assert.Single(loaded.State.Actors, x => x.Tactics?.Enemy?.RevivalOf == victim.Id);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellingAClaimLeavesNoOrphanEvenWhenAnotherUnspentCantorSurvives(bool killCaster)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 200); PrepareCantor(a);
        var other = RevivalTestEnemy(a, "grave_cantor", new(106, 100)); CommitCantor(a);
        if (killCaster) a.Hit(a.Enemy, 1000, source: a.Hero);
        else Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        Assert.Empty(a.Combat.State.Corpses); Assert.True(a.Enemy.Tactics!.Enemy!.RevivalSpent);
        Assert.False(other.Tactics!.Enemy!.RevivalSpent);
        Assert.DoesNotContain(a.Combat.State.Attacks, p => p.Support?.Kind == EnemySupportKind.Revive);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void SameBatchCasterAndAllyDeathsCannotLeaveAnEligibleCorpseOrRevival()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 200); var victim = PrepareCantor(a);
        a.Enemy.Position = new(101, 99); a.Enemy.Health = 3; victim.Health = 2;
        a.Act(TacticalAction.Special);
        Assert.Equal(LifeState.Dead, a.Enemy.Life); Assert.Equal(LifeState.Dead, victim.Life);
        Assert.Empty(a.Combat.State.Corpses);
        Assert.DoesNotContain(a.World.State.Actors, x => x.Tactics?.Enemy?.RevivalOf > 0);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void CantorRevivesOnAnOffscreenFloorWithPrivateIntentAndOriginalAppearance()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "grave_cantor"); a.Enemy.Position = new(103, 100, 1); a.Friend.Position = new(100, 100, 1);
        var victim = RevivalTestEnemy(a, "mossbound_guard", new(101, 100, 1));
        victim.Tactics!.InnateStats = victim.Tactics.InnateStats! with { MaxHealth = 7 }; victim.Health = 7;
        victim.Name = "Retained guard"; victim.SpriteId = "skeleton";
        a.Hit(victim, 1000, source: a.Friend);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.World.Visible.Clear(); string before = JsonSerializer.Serialize(a.World.State);
        Assert.Equal(victim.Id, a.Combat.PreviewEnemyTurn(a.Friend.Id, (_, _) => null).Support!.Corpse);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next(); Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        a.Act(TacticalAction.Wait); a.Next();
        var revived = Assert.Single(a.World.State.Actors, x => x.Tactics?.Enemy?.RevivalOf == victim.Id);
        Assert.Equal(new WorldPosition(101, 100, 1), revived.Position); Assert.Equal(4, revived.MaxHealth); Assert.Equal(4, revived.Health);
        Assert.Equal(victim.SpriteId, revived.SpriteId); Assert.Equal(victim.Name, revived.Name);
        Assert.Equal(victim.FactionId, revived.FactionId); Assert.True(revived.Tactics!.RewardExempt);
        Assert.Equal(LifeState.Dead, victim.Life); Assert.Empty(a.Combat.State.Corpses);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void EqualDistanceCorpsesKeepDeathOrderRatherThanActorIdentityOrder()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0); var victim = PrepareCantor(a);
        victim.Position = new(102, 100);
        var first = RevivalTestEnemy(a, "mossbound_guard", new(103, 99));
        Assert.True(first.Id > victim.Id);
        a.Hit(first, 1000, source: a.Hero); a.Hit(victim, 1000, source: a.Hero);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        Assert.Equal(first.Id, a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null).Support!.Corpse);
    }

    [Fact]
    public void RevivalCorpseStorageKeepsOriginalFloorAndWorldCapsWithoutRevivableChains()
    {
        using var a = new Arena(ClassKind.Adventurer); EnemyProfile(a, "grave_cantor");
        for (int floor = 0; floor < 3; floor++)
        {
            a.World.Chunks.Edit(new(104, 104, floor), Material.Floor); a.World.Chunks.Edit(new(105, 104, floor), Material.Floor);
            var caster = floor == 0 ? a.Enemy : RevivalTestEnemy(a, "grave_cantor", new(104, 104, floor));
            for (int i = 0; i < 18; i++)
            {
                var victim = RevivalTestEnemy(a, "mossbound_guard", new(105, 104, floor));
                a.Hit(victim, 1000, source: caster);
            }
        }
        Assert.Equal(32, a.Combat.State.Corpses.Count);
        Assert.Equal(new[] { 16, 16 }, a.Combat.State.Corpses.GroupBy(c => a.World.State.Actors.Single(x => x.Id == c.Actor).Position.Floor).Select(g => g.Count()));
        Assert.False(EnemyRevivalRules.Revivable("core:enemy/grave_cantor"));
        Assert.False(EnemyRevivalRules.Revivable("core:enemy/bone_splitter"));
        Assert.False(EnemyRevivalRules.Revivable("core:enemy/thorn_warden"));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void RevivalValidationRejectsMissingClaimsRenewedBudgetsAndForgedAncestry()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 200); var victim = PrepareCantor(a); CommitCantor(a);
        var corpse = Assert.Single(a.Combat.State.Corpses);
        a.Combat.State.Corpses[0] = corpse with { ClaimedBy = a.Hero.Id };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Combat.State.Corpses[0] = corpse;
        a.Enemy.Tactics!.Enemy!.RevivalSpent = false;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Enemy.Tactics.Enemy.RevivalSpent = true;
        a.Combat.State.Corpses.Clear();
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Combat.State.Corpses.Add(corpse);
        a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
        var revived = Assert.Single(a.World.State.Actors, x => x.Tactics?.Enemy?.RevivalOf > 0);
        revived.Tactics!.RewardExempt = false;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        revived.Tactics.RewardExempt = true;
        revived.Tactics.Enemy!.RevivalOf = a.Enemy.Id;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        revived.Tactics.Enemy.RevivalOf = victim.Id;
        revived.Tactics.Enemy.RevivedBy = a.Hero.Id;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        revived.Tactics.Enemy.RevivedBy = a.Enemy.Id;
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        a.Combat.State.Events.Clear(); a.Combat.State.Loot.Drops.Clear();
        Assert.True(TacticalReferences.RetainsActor(a.World.State, victim.Id));
        Assert.True(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
    }

    [Fact]
    public void InvalidRevivalCommitCannotSpendTheAttemptOrClaimACorpse()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0); var victim = PrepareCantor(a);
        a.Hit(victim, 1000, source: a.Hero);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<ArgumentException>(() => Commit("core:ability/cantor_revive", victim.Position, null));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/cantor_touch", victim.Position, new(EnemySupportKind.Revive, Corpse: victim.Id)));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/cantor_revive", victim.Position, new(EnemySupportKind.Revive, Corpse: a.World.State.NextActorId)));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/cantor_revive", victim.Position.Offset(0, 1), new(EnemySupportKind.Revive, Corpse: victim.Id)));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        Assert.False(a.Enemy.Tactics!.Enemy!.RevivalSpent);
        Assert.Equal(0, Assert.Single(a.Combat.State.Corpses).ClaimedBy);
        LockedAttack Commit(string ability, WorldPosition cell, EnemySupport? support)
            => a.Combat.CommitWindup(ability, [cell], AttackDelivery.Melee, DamageElement.Physical, 1, 150, 160, support: support);
    }
}
