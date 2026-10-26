using System.Numerics;
using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static long XpUnits(Actor actor) => actor.Tactics!.Build!.Experience * ContributionRewards.UnitsPerExperience
        + actor.Tactics.ExperienceFraction;

    [Fact]
    public void SmallSharesAccumulateAcrossDistinctKillsWithoutLosingSupportProgress()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        var enemies = new[] { a.Enemy }.Concat(a.World.State.Actors.Where(x => x.Kind == ActorKind.Raider
            && x.Tactics == null && x.Life == LifeState.Alive && x.EncounterLeader == 0).Take(3)).ToArray();
        Assert.Equal(4, enemies.Length);
        foreach (var enemy in enemies)
        {
            if (enemy != a.Enemy)
            {
                enemy.Position = a.Enemy.Position;
                a.Combat.Register(enemy, new() { InnateStats = new(200, 6, 6, 0, 100, 0, 150) }, 10_000);
                enemy.Health = 200;
            }
            enemy.Tactics!.ExperienceReward = 1;
            a.Hit(enemy, 50, source: a.Hero);
            a.Hit(enemy, 1000, source: a.Friend);
        }
        Assert.Equal(1, a.Hero.Tactics!.Build!.Experience);
        Assert.Equal(3, a.Friend.Tactics!.Build!.Experience);
        Assert.Equal(0, a.Hero.Tactics.ExperienceFraction);
        Assert.Equal(0, a.Friend.Tactics.ExperienceFraction);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void EqualEffectiveProtectionAndDamageProduceEqualProgress()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Tactics!.ExperienceReward = 10; a.Enemy.Health = 4;
        a.Act(TacticalAction.Guard); a.Hit(a.Hero, 8);
        a.Hit(a.Enemy, 1000, source: a.Friend);
        Assert.Equal(5_000_000, XpUnits(a.Hero));
        Assert.Equal(5_000_000, XpUnits(a.Friend));
        Assert.Equal(2, a.Hero.Level); Assert.Equal(2, a.Friend.Level);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HealthGrowthHealsLivingContributorsButNeverRevivesDownedOnes(bool downed)
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 1); // first growth is max HP
        int originalMaximum = a.Hero.MaxHealth;
        a.Enemy.Tactics!.ExperienceReward = 5;
        a.Hit(a.Enemy, 199, source: a.Hero);
        a.Hit(a.Hero, downed ? 1000 : 5);
        int before = a.Hero.Health;
        long combatRandom = a.Combat.State.RandomState;
        // An environmental finish still pays the member who earned the entire share.
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Enemy.Id, a.Enemy.Position, [a.Enemy.Position],
            "test:environment", AttackDelivery.Area, DamageElement.Fire, 1, a.Combat.Clock, HitsOwner: true));
        Assert.Equal(originalMaximum + 4, a.Hero.MaxHealth);
        Assert.Equal(downed ? 0 : before + 4, a.Hero.Health);
        Assert.Equal(downed ? LifeState.Downed : LifeState.Alive, a.Hero.Life);
        Assert.Equal(combatRandom, a.Combat.State.RandomState); // level growth uses the member's stream
        Assert.Equal(5_000_000, XpUnits(a.Hero));
        var progression = a.Combat.State.Events.Where(e => e.Source == a.Hero.Id
            && e.Kind is "ExperienceGained" or "LevelUp" or "StatIncreased").ToArray();
        Assert.Equal(new[] { "ExperienceGained", "LevelUp", "StatIncreased" }, progression.Select(e => e.Kind));
        Assert.Equal(new[] { 5, 2, 4 }, progression.Select(e => e.Amount));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void UncreditedOrExemptKillsDoNotInventRecipients()
    {
        using (var a = new Arena(ClassKind.Adventurer))
        {
            a.Enemy.Tactics!.ExperienceReward = 100;
            a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Enemy.Id, a.Enemy.Position, [a.Enemy.Position],
                "test:environment", AttackDelivery.Area, DamageElement.Fire, 1000, a.Combat.Clock, HitsOwner: true));
            Assert.Equal(0, XpUnits(a.Hero));
            Assert.True(a.Enemy.Tactics.RewardsClaimed);
        }
        using (var a = new Arena(ClassKind.Adventurer))
        {
            a.Enemy.Tactics!.ExperienceReward = 100; a.Enemy.Tactics.RewardExempt = true;
            a.Hit(a.Enemy, 1000, source: a.Hero);
            Assert.Equal(0, XpUnits(a.Hero));
            Assert.Empty(a.Combat.State.Rewards.Enemies);
        }
    }

    [Fact]
    public void StunOnlyCreditsActualDelayOrAnInterruptedWindup()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Tactics!.RegularEnemy = true;
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitWindup("test:windup", [a.Hero.Position], AttackDelivery.Area, DamageElement.Fire, 3, 50, 500);
        var activation = Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy);
        Apply(a, activation, BoonEffectKind.StunTarget);
        var contribution = a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id];
        Assert.Equal(100, contribution.Control);
        Apply(a, activation, BoonEffectKind.StunTarget);
        Assert.Equal(100, contribution.Control); // no remaining windup and no new delay
        a.Next(a.Enemy.Id);
        Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        Assert.Equal(200, contribution.Control); // a ready actor's next turn was delayed
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeadContributorsRedistributeTheirShareButDownedContributorsKeepIt(bool permanentlyDead)
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Tactics!.ExperienceReward = 10;
        a.Hit(a.Enemy, 50, source: a.Friend);
        a.Hit(a.Hero, 1000);
        // The friend earned one quarter before being downed; hero is revived only
        // to leave one survivor while exercising actual downing/death events.
        a.Next(a.Friend.Id);
        a.Friend.Position = a.Hero.Position.Offset(0, 1);
        Assert.True(a.Combat.Commit(new(TacticalAction.Revive, Target: a.Hero.Id)).Success);
        // Account for the separate healing contribution from the setup in the expected weight below.
        a.Hit(a.Friend, 1000);
        long friendHealing = a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Friend.Id].Healing;
        if (permanentlyDead) for (int i = 0; i < 5; i++) a.Hit(a.Friend, 1);
        Assert.Equal(permanentlyDead ? LifeState.Dead : LifeState.Downed, a.Friend.Life);
        a.Hit(a.Enemy, 1000, source: a.Hero); // only the remaining 150 HP count
        Assert.True(a.Enemy.Tactics.RewardsClaimed);
        Assert.Empty(a.Combat.State.Rewards.Enemies);
        if (permanentlyDead)
        {
            Assert.Equal(10_000_000, XpUnits(a.Hero));
            Assert.Equal(0, XpUnits(a.Friend));
        }
        else
        {
            var expected = ContributionRewards.Allocate(10_000_000,
                [(a.Hero.Id, new BigInteger(150)), (a.Friend.Id, new BigInteger(50 + friendHealing))]);
            Assert.Equal(expected[a.Hero.Id], XpUnits(a.Hero));
            Assert.Equal(expected[a.Friend.Id], XpUnits(a.Friend));
            Assert.Equal(0, a.Friend.Health);
            Assert.Equal(5, a.Friend.Tactics!.BleedTurns);
        }
        long earned = XpUnits(a.Hero);
        a.Hit(a.Enemy, 1000, source: a.Hero);
        Assert.Equal(earned, XpUnits(a.Hero));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void WeightedSharesConserveSmallRewardsAndDoNotPayNoncontributors()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Tactics!.ExperienceReward = 1;
        a.Hit(a.Enemy, 50, source: a.Hero);
        a.Hit(a.Enemy, 1000, source: a.Friend);
        Assert.Equal(250_000, XpUnits(a.Hero));
        Assert.Equal(750_000, XpUnits(a.Friend));
        Assert.Equal(0, a.Hero.Tactics!.Build!.Experience);
        Assert.Equal(0, a.Friend.Tactics!.Build!.Experience);

        using var solo = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        solo.Enemy.Tactics!.ExperienceReward = 6;
        solo.Hit(solo.Enemy, 1000, source: solo.Hero);
        Assert.Equal(6_000_000, XpUnits(solo.Hero));
        Assert.Equal(0, XpUnits(solo.Friend));
        Assert.Equal(2, solo.Hero.Level);
    }

    [Fact]
    public void GuardAndActualHealingReceiveDamageEquivalentWeightWithoutOverkillOrFriendlyFireCredit()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Tactics!.ExperienceReward = 10;
        a.Act(TacticalAction.Guard);
        a.Hit(a.Hero, 8); // four prevented, four actual enemy damage
        var ledger = a.Combat.State.Rewards.Enemies[a.Enemy.Id];
        Assert.Equal(4, ledger.Members[a.Hero.Id].Protection);
        Assert.Equal(4, ledger.UnhealedDamage[a.Hero.Id]);
        a.Hit(a.Hero, 4, source: a.Friend); // friendly fire creates no additional debt
        Assert.Equal(4, ledger.UnhealedDamage[a.Hero.Id]);
        var healing = Activation(a, a.Hero, "core:boon/mist_lullaby", 100);
        Apply(a, healing, BoonEffectKind.HealOwner);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.Equal(4, ledger.Members[a.Hero.Id].Healing);
        Assert.Empty(ledger.UnhealedDamage);
        Apply(a, healing, BoonEffectKind.HealOwner); // overheal cannot manufacture contribution
        Assert.Equal(4, ledger.Members[a.Hero.Id].Healing);
        a.Hit(a.Enemy, 1000, source: a.Friend);
        var expected = ContributionRewards.Allocate(10_000_000,
            [(a.Hero.Id, new BigInteger(8)), (a.Friend.Id, new BigInteger(200))]);
        Assert.Equal(expected[a.Hero.Id], XpUnits(a.Hero));
        Assert.Equal(expected[a.Friend.Id], XpUnits(a.Friend));
    }

    [Fact]
    public void ProtectionThatDoesNotSaveHealthFromOverkillEarnsNoCredit()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Hero.Health = 1;
        a.Act(TacticalAction.Guard);
        a.Hit(a.Hero, 1000);
        Assert.Empty(a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members);
        Assert.Equal(1, a.Combat.State.Rewards.Enemies[a.Enemy.Id].UnhealedDamage[a.Hero.Id]);
    }

    [Fact]
    public void HealingAfterEnemyDeathCannotClaimAnAlreadySettledReward()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Tactics!.ExperienceReward = 1;
        a.Hit(a.Hero, 5);
        a.Hit(a.Enemy, 1000, source: a.Friend);
        Apply(a, Activation(a, a.Hero, "core:boon/mist_lullaby", 5), BoonEffectKind.HealOwner);
        Assert.Equal(0, XpUnits(a.Hero));
        Assert.Equal(1_000_000, XpUnits(a.Friend));
        Assert.Empty(a.Combat.State.Rewards.Enemies);
    }

    [Fact]
    public void NativeControlCreditsActualPullAndSnareDelayToTheirOwner()
    {
        using (var a = new Arena(ClassKind.Hookrunner))
        {
            a.Act(TacticalAction.Special);
            var contribution = a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id];
            Assert.Equal(3, contribution.Damage);
            Assert.Equal(200, contribution.Control); // two successfully displaced tiles
            a.Next(); a.Enemy.Tactics!.ImmuneToPull = true;
            a.Act(TacticalAction.Special);
            Assert.Equal(200, contribution.Control);
        }
        using (var a = new Arena(ClassKind.Trapper, enemyDue: 0))
        {
            a.Enemy.Position = new(102, 100);
            a.Act(TacticalAction.Special);
            Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
            a.Act(TacticalAction.Move, Facing.West);
            var contribution = a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id];
            Assert.Equal(3, contribution.Damage);
            Assert.Equal(0, contribution.Control); // snare is not yet holding a turn
            a.Next();
            Assert.Equal(100, contribution.Control);
        }
    }

    [Fact]
    public void SentryKillsAwardTheirOwnerDespiteSuppressingAttackBoons()
    {
        using var a = new Arena(ClassKind.Tinkerer, ClassKind.Adventurer);
        a.Enemy.Tactics!.ExperienceReward = 6; a.Enemy.Health = 3;
        a.Act(TacticalAction.Special);
        a.Next(); a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(LifeState.Dead, a.Enemy.Life);
        Assert.Equal(6_000_000, XpUnits(a.Hero));
        Assert.Equal(0, XpUnits(a.Friend));
        Assert.Empty(a.Combat.State.Rewards.Enemies);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ScheduledBurnKillsAwardTheStatusOwner()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Tactics!.ExperienceReward = 1; a.Enemy.Health = 3;
        Apply(a, Activation(a, a.Friend, "core:boon/emberbrand", 3, a.Enemy), BoonEffectKind.ApplyStatus);
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(LifeState.Dead, a.Enemy.Life);
        Assert.Equal(0, XpUnits(a.Hero));
        Assert.Equal(1_000_000, XpUnits(a.Friend));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void XpLedgerFractionalCarryAndDeathReceiptResumeExactlyThroughWorldSave()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Tactics!.ExperienceReward = 6;
        a.Hero.Tactics!.ExperienceFraction = 750_000;
        a.Friend.Tactics!.ExperienceFraction = 250_000;
        a.Hit(a.Enemy, 150, source: a.Hero);
        a.Hit(a.Friend, 4);
        string directory = Path.Combine(Path.GetTempPath(), "roag-xp-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(directory, "world.json"); a.World.Save(path);
            using var loaded = WorldSession.Load(path);
            var combat = new TacticalCombat(loaded, _ => { });
            long action = a.Combat.State.NextAction;
            a.Hit(a.Enemy, 1000, source: a.Friend);
            combat.ResolveAttack(new(combat.State.NextAction++, a.Friend.Id, a.Friend.Position, [a.Enemy.Position],
                "test:attack", AttackDelivery.Melee, DamageElement.Physical, 1000, combat.Clock));
            Assert.Equal(action + 1, combat.State.NextAction);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(combat.State));
            foreach (var original in new[] { a.Hero, a.Friend, a.Enemy })
            {
                var restored = loaded.State.Actors.Single(x => x.Id == original.Id);
                Assert.Equal(JsonSerializer.Serialize(original.Tactics), JsonSerializer.Serialize(restored.Tactics));
                Assert.Equal(original.Health, restored.Health);
            }
            Assert.Equal(5_250_000, XpUnits(a.Hero));
            Assert.Equal(1_750_000, XpUnits(a.Friend));
            loaded.Save(path);
            using var afterKill = WorldSession.Load(path);
            Assert.True(afterKill.State.Actors.Single(x => x.Id == a.Enemy.Id).Tactics!.RewardsClaimed);
            Assert.Empty(afterKill.State.Tactics!.Rewards.Enemies);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void NativeLevelAboveOneHundredPersistsAndGrowthNeverChangesCommittedRecovery()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 42).GainExperience(25_250).Build;
        Assert.Equal(101, a.Hero.Level);
        a.Act(TacticalAction.Wait);
        var due = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id).Due;
        a.Enemy.Tactics!.ExperienceReward = 1000;
        a.Hit(a.Enemy, 1000, source: a.Hero);
        Assert.Equal(due, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id).Due);
        string directory = Path.Combine(Path.GetTempPath(), "roag-high-level-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(directory, "world.json"); a.World.Save(path);
            using var loaded = WorldSession.Load(path);
            Assert.Equal(a.Hero.Tactics.Build, loaded.Party.Single().Tactics!.Build);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void ContributionAllocationHandlesLargeWeightsAndRejectsInvalidSavedLedgers()
    {
        var shares = ContributionRewards.Allocate((long)int.MaxValue * 1_000_000,
            [(1, (BigInteger)long.MaxValue * int.MaxValue), (2, (BigInteger)long.MaxValue * int.MaxValue)]);
        Assert.Equal((long)int.MaxValue * 1_000_000, shares.Values.Sum());
        Assert.Equal(shares[1], shares[2]);
        using var a = new Arena(ClassKind.Adventurer);
        a.Hit(a.Enemy, 1, source: a.Hero);
        Assert.True(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
        var contribution = a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id];
        contribution.Healing = -1;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        contribution.Healing = 0;
        a.Hero.Tactics!.ExperienceFraction = 1_000_000;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Hero.Tactics.ExperienceFraction = 0;
        a.Enemy.Tactics!.RewardsClaimed = true;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }
}
