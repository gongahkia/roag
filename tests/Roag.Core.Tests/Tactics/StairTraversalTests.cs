using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static readonly WorldPosition StairTop = new(101, 100);
    private static readonly WorldPosition StairBottom = new(97, 100, 1);

    private static void PrepareStairs(Arena a)
    {
        a.World.Chunks.Edit(StairTop, Material.StairsDown);
        a.World.Chunks.Edit(StairBottom, Material.StairsUp);
    }

    [Theory]
    [InlineData(ClassKind.Adventurer)]
    [InlineData(ClassKind.Ranger)]
    [InlineData(ClassKind.Berserker)]
    public void StairStepMovesOnlyTheActingMemberAndPaysTheirMovementRecovery(ClassKind kind)
    {
        using var a = new Arena(kind, ClassKind.Adventurer); PrepareStairs(a);
        var friend = a.Friend.Position; var enemy = a.Enemy.Position;
        string before = JsonSerializer.Serialize(a.World.State);
        var preview = a.Combat.Preview(new(TacticalAction.Move, Facing.East));
        Assert.True(preview.Valid); Assert.Equal(StairBottom, preview.StairLanding);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Act(TacticalAction.Move);
        Assert.Equal(StairBottom, a.Hero.Position); Assert.Equal(friend, a.Friend.Position); Assert.Equal(enemy, a.Enemy.Position);
        Assert.Equal(StairBottom, a.World.State.Position);
        Assert.All(a.World.Visible, p => Assert.Equal(1, p.Floor));
        var turn = a.Combat.State.Queue.Snapshot().Pending.Single(e => e.ActorId == a.Hero.Id && e.Kind == ScheduledKind.ActorReady);
        Assert.Equal(ActionTiming.Recovery(ClassCatalog.Get(kind).Costs.Move, a.Hero.Speed), turn.Due);
        Assert.Equal(0, a.Combat.Clock);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        Assert.All(a.World.Visible, p => Assert.Equal(0, p.Floor));
        Assert.Single(a.Combat.State.Events, e => e.Kind == "FloorChanged");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void StairReturnIsAnotherPaidActionAndDoesNotMoveCompanions()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer); PrepareStairs(a);
        a.Act(TacticalAction.Move); a.Next();
        Assert.Equal(100, a.Combat.Clock);
        a.Act(TacticalAction.UseStairs);
        Assert.Equal(StairTop, a.Hero.Position); Assert.Equal(new(100, 102), a.Friend.Position);
        a.Next(); Assert.Equal(200, a.Combat.Clock);
        Assert.Equal(2, a.Combat.State.Events.Count(e => e.Kind == "FloorChanged"));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("wrong-way")]
    [InlineData("wall")]
    [InlineData("occupied")]
    [InlineData("downed")]
    public void StairObstructionsRejectWithoutSpendingTimeOrMovingAnyone(string obstruction)
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer); PrepareStairs(a);
        if (obstruction == "missing") a.World.Chunks.Edit(StairBottom, Material.Floor);
        if (obstruction == "wrong-way") a.World.Chunks.Edit(StairBottom, Material.StairsDown);
        if (obstruction == "wall") a.World.Chunks.Edit(StairBottom, Material.Wall);
        if (obstruction is "occupied" or "downed") a.Friend.Position = StairBottom;
        if (obstruction == "downed") a.Hit(a.Friend, 1000);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Preview(new(TacticalAction.Move, Facing.East)).Valid);
        Assert.False(a.Combat.Commit(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void StairCommitRechecksLandingAfterAnEarlierPreview()
    {
        using var a = new Arena(ClassKind.Adventurer); PrepareStairs(a);
        Assert.True(a.Combat.Preview(new(TacticalAction.Move, Facing.East)).Valid);
        a.Friend.Position = StairBottom;
        Assert.False(a.Combat.Commit(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal(new(100, 100), a.Hero.Position); Assert.Equal(0, a.Combat.Clock);
    }

    [Fact]
    public void StairsRequireAnActualEndpointAndCannotLeaveWorldBounds()
    {
        using var a = new Arena(ClassKind.Adventurer);
        Assert.False(a.Combat.Commit(new(TacticalAction.UseStairs)).Success);
        a.World.Chunks.Edit(a.Hero.Position, Material.StairsUp);
        Assert.False(a.Combat.Commit(new(TacticalAction.UseStairs)).Success);
        a.Hero.Position = a.World.State.Position = a.Hero.Position with { Floor = WorldPlan.Floors - 1 };
        a.World.Chunks.Edit(a.Hero.Position, Material.StairsDown);
        Assert.False(a.Combat.Commit(new(TacticalAction.UseStairs)).Success);
        Assert.Equal(0, a.Combat.Clock);
    }

    [Fact]
    public void DashingAcrossAStairDoesNotTeleportOrSpendTwoMovementActions()
    {
        using var a = new Arena(ClassKind.Adventurer); PrepareStairs(a);
        a.Act(TacticalAction.Dash);
        Assert.Equal(new(102, 100), a.Hero.Position);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "FloorChanged");
    }

    [Fact]
    public void StairDepartureEvadesALockedAttackWithoutMovingOrCancellingIt()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareStairs(a);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        var attack = a.Combat.CommitWindup("test:stair-escape", [a.Hero.Position, StairTop], AttackDelivery.Area,
            DamageElement.Physical, 5, 100, 100, a.Hero.Id);
        a.Next(); a.Act(TacticalAction.Move);
        Assert.Equal(JsonSerializer.Serialize(attack), JsonSerializer.Serialize(a.Combat.State.Attacks.Single()));
        a.Next(); Assert.Equal(a.Hero.MaxHealth, a.Hero.Health); Assert.Empty(a.Combat.State.Attacks);
    }

    [Fact]
    public void EnemyStairTraversalUsesTheSamePhysicalLinkWithoutChangingPrivateVision()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0); PrepareStairs(a);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = StairTop;
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        var viewpoint = a.World.State.Position; var vision = a.World.Visible.ToHashSet();
        a.Act(TacticalAction.UseStairs);
        Assert.Equal(StairBottom, a.Enemy.Position); Assert.Equal(viewpoint, a.World.State.Position);
        Assert.True(vision.SetEquals(a.World.Visible));
        Assert.Equal(ActionTiming.Recovery(a.Enemy.Tactics!.MoveCost, a.Enemy.Speed),
            a.Combat.State.Queue.Snapshot().Pending.Single(e => e.ActorId == a.Enemy.Id).Due);
    }

    [Fact]
    public void StairSplitAndPendingAttackResumeThroughAnActualWorldSave()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 50); PrepareStairs(a);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        a.Combat.CommitWindup("test:stairs", [StairTop], AttackDelivery.Melee, DamageElement.Physical, 5, 100, 100);
        a.Next(); a.Act(TacticalAction.Move);
        string directory = Path.Combine(Path.GetTempPath(), "roag-stairs-" + Guid.NewGuid().ToString("N"));
        try
        {
            a.World.Save(directory);
            using var loaded = WorldSession.Load(directory); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            for (int i = 0; i < 4; i++)
            {
                var actor = a.Combat.AdvanceToDecision(); var restored = resumed.AdvanceToDecision();
                Assert.NotNull(actor); Assert.Equal(actor.Id, restored!.Id);
                var intent = new TacticalIntent(actor.Id == a.Hero.Id && actor.Position == StairBottom
                    ? TacticalAction.UseStairs : TacticalAction.Wait, Facing.East);
                Assert.True(a.Combat.Commit(intent).Success); Assert.True(resumed.Commit(intent).Success);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void StairEventsRejectForgedCrossFloorMovementButRetainHistoryAfterTerrainEdits()
    {
        using var a = new Arena(ClassKind.Adventurer); PrepareStairs(a); a.Act(TacticalAction.Move);
        a.World.Chunks.Edit(StairTop, Material.Wall); a.World.Chunks.Edit(StairBottom, Material.Floor);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        int index = a.Combat.State.Events.FindIndex(e => e.Kind == "FloorChanged");
        var original = a.Combat.State.Events[index];
        foreach (var forged in new[] { original with { Position = StairBottom.Offset(1, 0) },
            original with { Kind = "Moved" }, original with { From = null }, original with { Forced = true } })
        {
            a.Combat.State.Events[index] = forged;
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        }
        a.Combat.State.Events[index] = original;
    }

    [Fact]
    public void GeneratedDungeonStairsHaveReciprocalPhysicalEndpointsOnEveryFloor()
    {
        using var world = WorldSession.Create(42);
        var site = world.Plan.Sites[0];
        for (int floor = 0; floor < WorldPlan.Floors - 1; floor++)
        {
            var top = site.Center.Offset(2, 0) with { Floor = floor };
            var bottom = site.Center.Offset(-2, 0) with { Floor = floor + 1 };
            Assert.Equal(bottom, world.StairLanding(top)); Assert.Equal(top, world.StairLanding(bottom));
        }
    }
}
