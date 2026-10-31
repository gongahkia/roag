using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void CleanseFlaskRejectsFreelyWithoutBurningAndCancelsTheActualPendingStatus()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Hero.Tactics!.Supplies.Utility = new(UtilityKind.CleanseFlask);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(new(TacticalAction.UseUtility)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        var burning = Activation(a, a.Enemy, "core:boon/emberbrand", 1, a.Hero);
        Apply(a, burning, BoonEffectKind.ApplyStatus);
        long schedule = a.Hero.Tactics.Burning!.Schedule; int hp = a.Hero.Health;
        Assert.True(a.Combat.Commit(new(TacticalAction.UseUtility)).Success);
        Assert.Null(a.Hero.Tactics.Burning); Assert.Null(a.Hero.Tactics.Supplies.Utility);
        Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, w => w.Sequence == schedule);
        var cleansed = Assert.Single(a.Combat.State.Events, e => e.Kind == "StatusCleansed");
        Assert.Equal(a.Hero.Id, cleansed.Source); Assert.Equal(a.Hero.Id, cleansed.Target);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "Waited");
        a.Next(); Assert.Equal(hp, a.Hero.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void SentryDeploymentAndAutonomousDamageDoNotSpendAnImmediateAttackFocus()
    {
        using var a = new Arena(ClassKind.Tinkerer); a.World.Reveal();
        a.Hero.Tactics!.Supplies.FocusActions = 3;
        a.Act(TacticalAction.Special); a.Next();
        Assert.Equal(2, a.Hero.Tactics.Supplies.FocusActions);
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(1, a.Hero.Tactics.Supplies.FocusActions);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "DamageTaken" && e.Ability == "core:ability/tinkerer_sentry");
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "SupplyFocusSpent");
        a.Act(TacticalAction.BasicAttack);
        Assert.Equal(0, a.Hero.Tactics.Supplies.FocusActions);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "SupplyFocusSpent");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void AnotherMembersPaidActionCannotConsumeTheOwnersSupplyDuration()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Hero.Tactics!.Supplies.FocusActions = 3; a.Friend.Tactics!.Supplies.WardActions = 3;
        a.Act(TacticalAction.Wait); Assert.Equal(2, a.Hero.Tactics.Supplies.FocusActions);
        Assert.Equal(3, a.Friend.Tactics.Supplies.WardActions);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision()); a.Act(TacticalAction.Wait);
        Assert.Equal(2, a.Hero.Tactics.Supplies.FocusActions); Assert.Equal(2, a.Friend.Tactics.Supplies.WardActions);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }
}
