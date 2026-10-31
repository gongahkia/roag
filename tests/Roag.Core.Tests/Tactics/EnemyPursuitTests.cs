using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Theory]
    [InlineData("cinder_mender", EnemySupportKind.Heal)]
    [InlineData("mend_acolyte", EnemySupportKind.Heal)]
    [InlineData("ward_weaver", EnemySupportKind.Ward)]
    [InlineData("grave_cantor", EnemySupportKind.Revive)]
    public void WorldSupportActsOnPhysicalAlliesWithoutAnyPartyTargetOnItsFloor(string definition, EnemySupportKind kind)
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, definition); a.Enemy.Position = new(103, 100);
        var ally = RevivalTestEnemy(a, "mossbound_guard", new(102, 100));
        ally.Health = 1;
        if (definition == "mend_acolyte")
        {
            a.Enemy.Tactics!.Enemy!.Squad = new("support", EnemySquadKind.HealerRushwings, a.Enemy.Id, a.Enemy.Position);
            ally.Tactics!.Enemy!.Squad = a.Enemy.Tactics.Enemy.Squad;
            ally.Tactics.DefinitionId = "core:enemy/rushwing";
        }
        if (kind == EnemySupportKind.Revive) a.Hit(ally, 1000);
        a.Hero.Position = new(100, 100, 1); a.Friend.Position = new(110, 110, 1);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        var before = JsonSerializer.Serialize(a.World.State);
        Assert.Equal(kind, a.Combat.PreviewWorldEnemyTurn().Support!.Kind);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        Assert.Equal(kind, a.Combat.CommitWorldEnemyTurn().Support!.Kind);
        var attack = Assert.Single(a.Combat.State.Attacks);
        Assert.Equal(0, attack.IntendedTarget);
        a.Combat.AdvanceToPartyDecision();
        for (int i = 0; i < 3; i++) { a.Act(TacticalAction.Wait); a.Combat.AdvanceToPartyDecision(); }
        if (kind == EnemySupportKind.Heal) Assert.True(ally.Health > 1);
        else if (kind == EnemySupportKind.Ward) Assert.True(a.Enemy.Tactics!.Enemy!.WardReady);
        else Assert.Contains(a.World.State.Actors, actor => actor.Tactics?.Enemy?.RevivalOf == ally.Id);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "DamageTaken" && (e.Target == a.Hero.Id || e.Target == a.Friend.Id));
    }

    [Fact]
    public void WorldEnemyPreviewIsFreeAndLocalCommitUsesTheOriginalLockedAttack()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Equal(EnemyIntentKind.Windup, a.Combat.PreviewWorldEnemyTurn().Kind);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        Assert.Equal(EnemyIntentKind.Windup, a.Combat.CommitWorldEnemyTurn().Kind);
        var attack = Assert.Single(a.Combat.State.Attacks);
        Assert.Equal(a.Hero.Id, attack.IntendedTarget); Assert.Equal(new[] { a.Hero.Position }, attack.Cells);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void WorldEnemySearchesLastObservationWithoutTrackingAnUnseenMovingMember()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = new(106, 100);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        var observation = a.Enemy.Tactics!.Enemy!.Awareness;
        Assert.Equal(a.Hero.Position, observation!.LastKnown);
        a.Hero.Position = new(100, 100, 1); // No witnessed stair departure.
        Assert.Equal(EnemyIntentKind.Move, a.Combat.CommitWorldEnemyTurn().Kind);
        Assert.Equal(new(105, 100), a.Enemy.Position);
        Assert.Equal(observation, a.Enemy.Tactics.Enemy.Awareness);
        Assert.Empty(a.Combat.State.Attacks);
    }

    [Fact]
    public void WitnessedStairDepartureIsPursuedPhysicallyWithoutChangingTheView()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 150);
        EnemyProfile(a, "mossbound_guard"); PrepareStairs(a); a.Enemy.Position = new(103, 100);
        a.Act(TacticalAction.Move); a.Next(); a.Act(TacticalAction.Move, Facing.South); a.Next(a.Enemy.Id);
        Assert.Equal(StairBottom, a.Enemy.Tactics!.Enemy!.Awareness!.LastKnown);
        var view = a.World.Visible.ToHashSet(); var position = a.World.State.Position;
        Assert.Equal(EnemyIntentKind.Move, a.Combat.CommitWorldEnemyTurn().Kind);
        Assert.Equal(new(102, 100), a.Enemy.Position);
        a.Next(a.Enemy.Id); a.Combat.CommitWorldEnemyTurn();
        Assert.Equal(StairBottom, a.Enemy.Position);
        Assert.Equal(position, a.World.State.Position); Assert.True(view.SetEquals(a.World.Visible));
        Assert.Equal(2, a.Combat.State.Events.Count(e => e.Kind == "FloorChanged"));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void AnEnemyAlreadyOnTheObservedStairPaysToUseItsEndpoint()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); PrepareStairs(a); a.Enemy.Position = StairTop;
        a.Enemy.Tactics!.Enemy!.Awareness = new(a.Hero.Id, StairBottom.Offset(0, 1), 0);
        a.Hero.Position = StairBottom.Offset(0, 1);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        Assert.Equal(EnemyIntentKind.UseStairs, a.Combat.CommitWorldEnemyTurn().Kind);
        Assert.Equal(StairBottom, a.Enemy.Position);
        Assert.Equal(ActionTiming.Recovery(a.Enemy.Tactics.MoveCost, a.Enemy.Speed),
            a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Enemy.Id).Due);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldPursuitDoesNotCrossBlockedOrUnpairedStairLandings(bool occupied)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); PrepareStairs(a); a.Enemy.Position = StairTop;
        a.Hero.Position = StairBottom.Offset(0, 1);
        a.Enemy.Tactics!.Enemy!.Awareness = new(a.Hero.Id, a.Hero.Position, 0);
        if (occupied) a.Friend.Position = StairBottom; else a.World.Chunks.Edit(StairBottom, Material.Floor);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        Assert.Equal(EnemyIntentKind.Wait, a.Combat.CommitWorldEnemyTurn().Kind);
        Assert.Equal(StairTop, a.Enemy.Position); Assert.Empty(a.Combat.State.Attacks);
    }

    [Fact]
    public void WorldNavigationFindsADetourBeyondTheOldLocalRadiusAndCombatWindow()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = new(140, 100);
        // Two disconnected-looking ends joined only by an 80-step U-shaped corridor.
        for (int y = 99; y <= 141; y++) for (int x = 139; x <= 143; x++)
            a.World.Chunks.Edit(new(x, y), Material.Wall);
        for (int y = 100; y <= 140; y++) foreach (int x in new[] { 140, 142 }) a.World.Chunks.Edit(new(x, y), Material.Floor);
        a.World.Chunks.Edit(new(141, 140), Material.Floor);
        a.Hero.Position = new(142, 100, 1); // Search only remembered floor-zero terrain.
        a.Enemy.Tactics!.Enemy!.Awareness = new(a.Hero.Id, new(142, 100), 0);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        Assert.Equal(Facing.South, a.Combat.PreviewWorldEnemyTurn().Facing);
        a.Combat.CommitWorldEnemyTurn(); Assert.Equal(new(140, 101), a.Enemy.Position);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void WorldPursuitExpiresAtItsOwnClockBoundaryAndNeverFollowsADeadIdentity(int reason)
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: reason == 0 ? 3000 : 0);
        EnemyProfile(a, "mossbound_guard"); a.Hero.Position = new(100, 100, 1);
        a.Friend.Position = new(110, 110, 1);
        a.Enemy.Tactics!.Enemy!.Awareness = new(a.Hero.Id, new(100, 100), 0);
        if (reason == 1)
        {
            a.Enemy.Tactics.Enemy.Awareness = new(a.Friend.Id, new(100, 100), 0);
            a.Hit(a.Friend, 1000);
            for (int i = 0; i < 5; i++) a.Hit(a.Friend, 1);
        }
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        Assert.Null(a.Enemy.Tactics.Enemy.Awareness);
        Assert.Equal(EnemyIntentKind.Wait, a.Combat.CommitWorldEnemyTurn().Kind);
    }

    [Fact]
    public void DistantTimelineMembersAndUninvolvedResidentsDoNotImplyLocalCombat()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        EnemyProfile(a, "mossbound_guard"); a.Hero.Position = new(100, 100, 1);
        a.Friend.Position = new(110, 110, 1);
        Assert.False(a.World.IsInCombat); Assert.False(a.Combat.IsLocallyEngaged(a.Hero.Id));
        a.Friend.Position = new(104, 100);
        Assert.True(a.World.IsInCombat); Assert.True(a.World.IsActorInCombat(a.Friend.Id));
        Assert.False(a.World.IsActorInCombat(a.Hero.Id));
        a.Enemy.Kind = ActorKind.Person;
        Assert.False(a.World.IsInCombat); Assert.Null(a.Combat.SelectEnemyTarget(a.Enemy.Id));
    }

    [Fact]
    public void WallsSeparateLocalEngagementWithoutRemovingGlobalTurnMembership()
    {
        using var a = new Arena(ClassKind.Adventurer);
        EnemyProfile(a, "mossbound_guard");
        for (int y = 89; y <= 111; y++) a.World.Chunks.Edit(new(101, y), Material.Wall);
        Assert.False(a.World.IsInCombat); Assert.Contains(a.Enemy.Id, a.Combat.State.Participants);
        a.World.Chunks.Edit(new(101, 100), Material.Floor);
        Assert.True(a.World.IsInCombat);
    }

    [Fact]
    public void PursuerConfinementBlocksUnownedWorldDecisionsAndAllConfiguredEscapeSteps()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "extraction_pursuer"); PrepareStairs(a); a.Enemy.Position = StairBottom;
        a.Hero.Position = StairBottom.Offset(0, 2);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        Assert.Equal(EnemyIntentKind.Wait, a.Combat.PreviewWorldEnemyTurn().Kind);
        a.Enemy.Tactics!.Enemy!.PursuitBoundary = new(95, 95, 110, 110, 1, 1);
        Assert.False(a.Combat.Preview(new(TacticalAction.UseStairs)).Valid);
        Assert.Null(a.Combat.State.Attacks.SingleOrDefault());
        a.Hero.Position = StairTop; // Outside the dungeon: no tracking through the entrance.
        a.Enemy.Tactics.Enemy.Awareness = null;
        Assert.Equal(EnemyIntentKind.Wait, a.Combat.CommitWorldEnemyTurn().Kind);
        Assert.Equal(StairBottom, a.Enemy.Position);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void PursuitObservationAndPhysicalFloorRouteContinueThroughAnActualSave()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 150);
        EnemyProfile(a, "mossbound_guard"); PrepareStairs(a);
        a.Act(TacticalAction.Move); a.Next(); a.Act(TacticalAction.Move, Facing.South); a.Next(a.Enemy.Id);
        string directory = Path.Combine(Path.GetTempPath(), "roag-pursuit-" + Guid.NewGuid().ToString("N"));
        try
        {
            a.World.Save(directory);
            using var loaded = WorldSession.Load(directory); var resumed = new TacticalCombat(loaded, _ => { });
            for (int i = 0; i < 6; i++)
            {
                var actor = a.Combat.AdvanceToDecision(); var restored = resumed.AdvanceToDecision();
                Assert.NotNull(actor); Assert.Equal(actor.Id, restored!.Id);
                if (actor.Id == a.Enemy.Id)
                {
                    var intent = a.Combat.CommitWorldEnemyTurn(); var restoredIntent = resumed.CommitWorldEnemyTurn();
                    Assert.Equal(intent.Kind, restoredIntent.Kind); Assert.Equal(intent.Facing, restoredIntent.Facing);
                    Assert.Equal(intent.Ability, restoredIntent.Ability);
                }
                else
                {
                    Assert.True(a.Combat.Commit(new(TacticalAction.Wait)).Success);
                    Assert.True(resumed.Commit(new(TacticalAction.Wait)).Success);
                }
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void SavedEnemyObservationsRejectForgedTargetsTimesLocationsAndBoundaryRoles()
    {
        using var a = new Arena(ClassKind.Adventurer);
        EnemyProfile(a, "mossbound_guard");
        var memory = a.Enemy.Tactics!.Enemy!;
        foreach (var bad in new[] { new EnemyAwareness(-1, a.Hero.Position, 0), new(a.Enemy.Id, a.Hero.Position, 0),
            new(a.Hero.Id, new(-1, 100), 0), new(a.Hero.Id, a.Hero.Position, 1) })
        {
            memory.Awareness = bad; Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        }
        memory.Awareness = null; memory.PursuitBoundary = new(95, 95, 110, 110, 0, 1);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void WorldPursuitUsesTwoPhysicalFloorLinksAndThenEmergesOutdoors()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); PrepareStairs(a);
        // The lower flight shares its top with the upper flight's arrival tile.
        // Use a separate adjacent top to preserve both reciprocal endpoints.
        var middle = StairBottom.Offset(0, 1); var deep = middle.Offset(-4, 0) with { Floor = 2 };
        a.World.Chunks.Edit(middle, Material.StairsDown);
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) a.World.Chunks.Edit(deep.Offset(x, y), Material.Floor);
        a.World.Chunks.Edit(deep, Material.StairsUp);
        a.Enemy.Position = deep;
        a.Hero.Position = StairTop.Offset(0, -1);
        a.Enemy.Tactics!.Enemy!.Awareness = new(a.Hero.Id, a.Hero.Position, 0);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        Assert.Equal(EnemyIntentKind.UseStairs, a.Combat.CommitWorldEnemyTurn().Kind);
        Assert.Equal(middle, a.Enemy.Position);
        a.Next(a.Enemy.Id); a.Combat.CommitWorldEnemyTurn();
        Assert.Equal(StairTop, a.Enemy.Position);
        Assert.Equal(2, a.Combat.State.Events.Count(e => e.Kind == "FloorChanged"));
        Assert.Empty(a.Combat.State.Attacks);
        a.Next(a.Enemy.Id); Assert.Equal(EnemyIntentKind.Windup, a.Combat.CommitWorldEnemyTurn().Kind);
        Assert.Equal(a.Hero.Id, Assert.Single(a.Combat.State.Attacks).IntendedTarget);
    }

    [Fact]
    public void PartyDecisionPumpRunsPhysicalEnemyPursuitWithoutIndependentWorldTime()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = new(106, 100);
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Hero, a.Combat.AdvanceToPartyDecision());
        Assert.Equal(new(105, 100), a.Enemy.Position);
        Assert.Equal(100, a.Combat.Clock);
        Assert.Equal(0, a.World.State.Tick); // This fixture deliberately supplies a no-world callback.
        Assert.Contains(a.Combat.State.Queue.Snapshot().Pending, w => w.ActorId == a.Enemy.Id);
    }
}
