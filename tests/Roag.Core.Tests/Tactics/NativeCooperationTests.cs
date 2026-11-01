using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    public static IEnumerable<object[]> NativeCooperativeClasses => Enum.GetValues<ClassKind>().Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(NativeCooperativeClasses))]
    public void NativeCooperationUsesAllFixedClassesAndAppliesArmorOnceWithBothRecoveries(ClassKind kind)
    {
        using var a = new Arena(kind, kind);
        a.Enemy.Position = a.Hero.Position.Offset(1, 0);
        a.Enemy.Tactics!.InnateStats = a.Enemy.Tactics.InnateStats! with { Defense = 3 };
        a.Hero.Mana = a.Friend.Mana = 0;
        var intent = new TacticalIntent(TacticalAction.Cooperate, Facing.East, Target: a.Friend.Id, Choice: 1);
        var terms = a.Combat.CooperativeOptions(a.Friend.Id).Single(o => o.Choice == 1);
        string before = JsonSerializer.Serialize(a.World.State);
        var preview = a.Combat.Preview(intent);
        Assert.True(preview.Valid, preview.Reason); Assert.Single(preview.Cells);
        Assert.Equal(a.Hero.Power + a.Friend.Power + 2, preview.Damage);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        int health = a.Enemy.Health;
        var enemyWork = a.Combat.State.Timeline.Pending.Single(w => w.ActorId == a.Enemy.Id);
        Assert.True(a.Combat.Commit(intent).Success);
        Assert.Equal(health - Math.Max(0, preview.Damage!.Value - 3), a.Enemy.Health);
        Assert.Equal(terms.Recovery, a.Combat.State.Timeline.Pending.Single(w => w.ActorId == a.Hero.Id).Due);
        Assert.Equal(terms.HelperRecovery, a.Combat.State.Timeline.Pending.Single(w => w.ActorId == a.Friend.Id).Due);
        Assert.Equal(enemyWork, a.Combat.State.Timeline.Pending.Single(w => w.ActorId == a.Enemy.Id));
        Assert.True(a.Friend.Tactics!.CooperationCommitted); Assert.Equal(0, a.Hero.Mana); Assert.Equal(0, a.Friend.Mana);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "AttackPerformed" && e.Ability == "ability.twin-strike");
        var ledger = a.Combat.State.Rewards.Enemies[a.Enemy.Id];
        Assert.Equal(health - a.Enemy.Health, ledger.Members.Values.Sum(c => c.Damage));
        Assert.True(ledger.Members[a.Hero.Id].Damage > 0); Assert.True(ledger.Members[a.Friend.Id].Damage > 0);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void NativeCooperationExtendsExistingRecoveryWithoutMovingClassWorkAndCannotReuseHelperBeforeTheirTurn()
    {
        using var a = new Arena(ClassKind.Ranger, ClassKind.Berserker);
        a.Enemy.Position = a.Hero.Position.Offset(2, 0);
        var old = a.Combat.State.Timeline.Pending.Single(w => w.ActorId == a.Friend.Id);
        a.Combat.State.Queue.Delay(old.Sequence, 500);
        // An independent committed effect must retain its timestamp and sequence.
        var unrelated = a.Combat.State.Queue.Schedule(ScheduledKind.Effect, 600, a.Friend.Id, referenceId: 0);
        var option = a.Combat.CooperativeOptions(a.Friend.Id).Single(o => o.Choice == 1);
        Assert.True(a.Combat.Commit(new(TacticalAction.Cooperate, Facing.East, Target: a.Friend.Id, Choice: 1)).Success);
        var delayed = a.Combat.State.Timeline.Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Friend.Id);
        Assert.Equal(old.Sequence, delayed.Sequence); Assert.Equal(500 + option.HelperRecovery, delayed.Due);
        Assert.Contains(unrelated, a.Combat.State.Timeline.Pending);
        a.Next();
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(new(TacticalAction.Cooperate, Facing.East, Target: a.Friend.Id, Choice: 1)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        // The synthetic unrelated work is only a timing assertion, not an executable class effect.
        a.Combat.State.Queue.Cancel(unrelated.Sequence);
        a.Next(a.Friend.Id); Assert.False(a.Friend.Tactics!.CooperationCommitted);
        a.Act(TacticalAction.Wait); a.Next();
        Assert.True(a.Combat.Preview(new(TacticalAction.Cooperate, Facing.East, Target: a.Friend.Id, Choice: 1)).Valid);
    }

    [Theory]
    [InlineData("self")] [InlineData("missing")] [InlineData("enemy")] [InlineData("downed")]
    [InlineData("stunned")] [InlineData("committed")] [InlineData("floor")] [InlineData("range")]
    [InlineData("wall")] [InlineData("no-target")] [InlineData("ability")]
    public void NativeCooperationRejectsIneligibleHelpersOrTargetsWithoutSpendingAnything(string invalid)
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Position = a.Hero.Position.Offset(1, 0);
        long helper = invalid switch { "self" => a.Hero.Id, "missing" => long.MaxValue, "enemy" => a.Enemy.Id, _ => a.Friend.Id };
        if (invalid == "downed") a.Hit(a.Friend, 1000);
        if (invalid == "stunned") a.Friend.Tactics!.StunnedUntil = 40;
        if (invalid == "committed") a.Friend.Tactics!.CooperationCommitted = true;
        if (invalid == "floor") a.Friend.Position = new(100, 102, 1);
        if (invalid == "range") a.Friend.Position = new(100, 103);
        if (invalid == "wall") a.World.Chunks.Edit(new(100, 101), Material.Wall);
        if (invalid == "no-target") a.Enemy.Position = new(103, 100);
        string before = JsonSerializer.Serialize(a.World.State);
        var intent = new TacticalIntent(TacticalAction.Cooperate, Facing.East, Target: helper, Choice: invalid == "ability" ? 3 : 1);
        Assert.False(a.Combat.Preview(intent).Valid);
        Assert.False(a.Combat.Commit(intent).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void NativeCooperationSharesActualDamageBeforeLethalRewardSettlementWithoutDoublePayment()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100); a.Enemy.Health = 5; a.Enemy.Tactics!.ExperienceReward = 2;
        Assert.True(a.Combat.Commit(new(TacticalAction.Cooperate, Facing.East, Target: a.Friend.Id, Choice: 1)).Success);
        Assert.Equal(LifeState.Dead, a.Enemy.Life);
        Assert.Equal(1_200_000, XpUnits(a.Hero)); Assert.Equal(800_000, XpUnits(a.Friend));
        Assert.False(a.Combat.State.Rewards.Enemies.ContainsKey(a.Enemy.Id));
        a.Hit(a.Enemy, 1000, source: a.Hero);
        Assert.Equal(2_000_000, XpUnits(a.Hero) + XpUnits(a.Friend));
    }

    [Fact]
    public void NativeCooperationSharedAidHealsCleansesAndCalmsNearbyLivingMembersWithoutRefundingRecovery()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Hit(a.Hero, 6); a.Hit(a.Friend, 6);
        a.Hero.Stress = a.Friend.Stress = 30;
        Apply(a, Activation(a, a.Hero, "core:boon/emberbrand", 1, a.Friend), BoonEffectKind.ApplyStatus);
        Assert.NotNull(a.Friend.Tactics!.Burning);
        a.Friend.Tactics.SnaredBy = a.Enemy.Id; a.Friend.Tactics.EnemySnareDelay = EnemySquads.SnareDelay;
        var intent = new TacticalIntent(TacticalAction.Cooperate, Target: a.Friend.Id, Choice: 2);
        Assert.True(a.Combat.Commit(intent).Success);
        Assert.Null(a.Friend.Tactics.Burning); Assert.Equal(0, a.Friend.Tactics.SnaredBy); Assert.Equal(0, a.Friend.Tactics.EnemySnareDelay);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health); Assert.Equal(a.Friend.MaxHealth, a.Friend.Health);
        Assert.Equal(20, a.Hero.Stress); Assert.Equal(20, a.Friend.Stress);
        var ledger = a.Combat.State.Rewards.Enemies[a.Enemy.Id];
        Assert.Equal(12, ledger.Members.Values.Sum(c => c.Healing)); Assert.Empty(ledger.UnhealedDamage);
        Assert.True(ledger.Members[a.Hero.Id].Healing > 0); Assert.True(ledger.Members[a.Friend.Id].Healing > 0);
        Assert.Equal(190, a.Combat.State.Timeline.Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Hero.Id).Due);
        Assert.Equal(130, a.Combat.State.Timeline.Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Friend.Id).Due);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void NativeCooperationSaveRetainsPaidHelperCommitmentAndIdenticalContinuation()
    {
        using var a = new Arena(ClassKind.Ranger, ClassKind.Tinkerer);
        a.Enemy.Position = new(102, 100);
        Assert.True(a.Combat.Commit(new(TacticalAction.Cooperate, Facing.East, Target: a.Friend.Id, Choice: 1)).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-cooperation-" + Guid.NewGuid().ToString("N"));
        try
        {
            a.World.Save(root);
            using var loaded = WorldSession.Load(root); var combat = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.World.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(a.Combat.AdvanceToDecision()!.Id, combat.AdvanceToDecision()!.Id);
            Assert.Equal(JsonSerializer.Serialize(a.World.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(a.Combat.Commit(new(TacticalAction.Wait)).Success); Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
            Assert.Equal(a.Combat.AdvanceToDecision()!.Id, combat.AdvanceToDecision()!.Id);
            Assert.False(loaded.Party.Single(p => p.Id == a.Friend.Id).Tactics!.CooperationCommitted);
            Assert.Equal(JsonSerializer.Serialize(a.World.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NativeCooperationAidKeepsPhysicalRangeDowningAndPaidStunRecovery(bool downed)
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        var third = a.World.State.Actors.First(x => x.Kind == ActorKind.Person && !x.Recruited && x.Id != a.Friend.Id);
        third.Recruited = true; third.Position = new(101, 101); a.World.State.Party.Add(third.Id);
        a.Combat.Register(third, new() { Build = CharacterBuild.Create(ClassKind.Alchemist, 44) }, 500);
        var far = a.World.State.Actors.First(x => x.Kind == ActorKind.Person && !x.Recruited);
        far.Recruited = true; far.Position = new(100, 100, 1); a.World.State.Party.Add(far.Id);
        a.Combat.Register(far, new() { Build = CharacterBuild.Create(ClassKind.Ranger, 45) }, 600);
        a.Hit(third, downed ? 1000 : 8); a.Hit(far, 8);
        int farHealth = far.Health; var farPosition = far.Position;
        if (!downed) third.Tactics!.StunnedUntil = 40;
        var thirdReady = a.Combat.State.Timeline.Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == third.Id);
        Assert.True(a.Combat.Commit(new(TacticalAction.Cooperate, Target: a.Friend.Id, Choice: 2)).Success);
        Assert.Equal(farHealth, far.Health); Assert.Equal(farPosition, far.Position);
        Assert.Contains(thirdReady, a.Combat.State.Timeline.Pending);
        if (downed) { Assert.Equal(0, third.Health); Assert.Equal(LifeState.Downed, third.Life); Assert.Equal(5, third.Tactics!.BleedTurns); }
        else { Assert.Equal(third.MaxHealth - 2, third.Health); Assert.Equal(0, third.Tactics!.StunnedUntil); }
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void NativeCooperationDoesNotBorrowOrRescheduleDuplicateClassConstructs()
    {
        using var a = new Arena(ClassKind.Tinkerer, ClassKind.Tinkerer);
        a.Act(TacticalAction.Special); a.Next(a.Friend.Id); a.Act(TacticalAction.Special); a.Next();
        string resources = JsonSerializer.Serialize(new[] { a.Hero.Tactics!.Resources, a.Friend.Tactics!.Resources });
        var work = a.Combat.State.Timeline.Pending.Where(w => w.Kind != ScheduledKind.ActorReady).ToArray();
        Assert.True(a.Combat.Commit(new(TacticalAction.Cooperate, Target: a.Friend.Id, Choice: 2)).Success);
        Assert.Equal(resources, JsonSerializer.Serialize(new[] { a.Hero.Tactics.Resources, a.Friend.Tactics.Resources }));
        Assert.Equal(work, a.Combat.State.Timeline.Pending.Where(w => w.Kind != ScheduledKind.ActorReady));
        Assert.NotEqual(a.Hero.Tactics.Resources.Sentry!.Position, a.Friend.Tactics.Resources.Sentry!.Position);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void NativeCooperationDividesOnlyCombatHealingWhenOtherWoundsHaveNoEnemyDebt()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Hero.Health -= 2; a.Hit(a.Hero, 4);
        Assert.True(a.Combat.Commit(new(TacticalAction.Cooperate, Target: a.Friend.Id, Choice: 2)).Success);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        var ledger = a.Combat.State.Rewards.Enemies[a.Enemy.Id];
        Assert.Equal(3, ledger.Members[a.Hero.Id].Healing); Assert.Equal(1, ledger.Members[a.Friend.Id].Healing);
        Assert.Empty(ledger.UnhealedDamage);
    }
}
