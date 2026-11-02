using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeActivityTests
{
    private static Actor AddCamper(WorldSession world, ClassKind kind, int floor)
    {
        var actor = DungeonPartyTests.AddMember(world, kind, floor);
        // Independent safe camp, rather than the population helper's arbitrary
        // first free tile (which may be in sight of a hostile).
        var room = world.State.Dungeon!.Floors.Single(f => f.Origin.Floor == floor).Rooms.Single(r => r.Role == "start");
        actor.Position = floor == 2 ? room.Origin.Offset(room.Width / 2, room.Height / 2) : world.Party.First().Position.Offset(6, 0);
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++) world.Chunks.Edit(actor.Position.Offset(x, y), Material.Floor);
        return actor;
    }

    [Fact]
    public void AnUnassignedReadyMemberStillRequiresInputWhileAnotherMemberRests()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        var hero = world.Party.Single(); var other = AddCamper(world, ClassKind.Ranger, 2);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 10)).Success);
        Assert.Equal(other.Id, world.DungeonCombat.Active!.Id);
        string state = JsonSerializer.Serialize(world.State);
        Assert.False(world.ContinueDungeonActivities(256));
        Assert.Equal(state, JsonSerializer.Serialize(world.State)); Assert.NotNull(hero.Tactics!.Activity);
    }

    [Fact]
    public void AThreatBeyondTheSleepingNoticeRadiusDoesNotMagicallyWakeThatMember()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 10)).Success);
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy is { Dormant: false } && a.Position.Floor == 1);
        enemy.Position = hero.Position.Offset(10, 0); world.Chunks.Edit(enemy.Position, Material.Floor);
        world.ContinueDungeonActivities(1);
        Assert.NotNull(hero.Tactics!.Activity);
        Assert.DoesNotContain(world.State.Tactics!.Events, e => e.Kind == "ActivityStopped");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(241)]
    public void InvalidDurationCannotSpendAnActionOrStartAnOrder(int minutes)
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Rest, Choice: minutes)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void RestIsAnExplicitPaidOrderAndCompletesWithRealNeedsButIdleFramesDoNothing()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        var hero = world.Party.Single(); hero.Body.Fatigue = 20;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 1)).Success);
        Assert.NotNull(hero.Tactics!.Activity); Assert.True(world.DungeonCombat.Clock > 0);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Equal(0, world.AdvanceFrame(TimeSpan.FromHours(1)));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        for (int i = 0; i < 10 && hero.Tactics.Activity != null; i++) world.ContinueDungeonActivities();
        Assert.Null(hero.Tactics.Activity); Assert.InRange(world.DungeonCombat.Clock, 6000, 6100);
        Assert.InRange(hero.Body.Fatigue, 19, 20); Assert.True(hero.Body.Hunger > 0);
        Assert.Equal(0, hero.Body.RestUntil);
        long clock = world.DungeonCombat.Clock;
        Assert.False(world.ContinueDungeonActivities()); Assert.Equal(clock, world.DungeonCombat.Clock);
    }

    [Fact]
    public void CancellationDuringRecoveryIsFreeAndDoesNotRefundTheAlreadyPaidTurn()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        var hero = world.Party.Single(); var other = AddCamper(world, ClassKind.Ranger, 2);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 60)).Success);
        Assert.Equal(other.Id, world.DungeonCombat.Active!.Id);
        var timeline = JsonSerializer.Serialize(world.State.Tactics!.Timeline);
        Assert.True(world.CancelDungeonActivity(hero.Id).Success);
        Assert.Equal(timeline, JsonSerializer.Serialize(world.State.Tactics.Timeline));
        Assert.Equal(0, hero.Body.RestUntil); Assert.Null(hero.Tactics!.Activity);
        Assert.False(world.CancelDungeonActivity(other.Id).Success);
    }

    [Fact]
    public void WatchMissesRestWhileASeparatelySleepingMemberRecovers()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        var watcher = world.Party.Single(); var sleeper = AddCamper(world, ClassKind.Ranger, 2);
        watcher.Body.Fatigue = sleeper.Body.Fatigue = 20;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Watch, Choice: 1)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 1)).Success);
        for (int i = 0; i < 15 && world.Party.Any(a => a.Tactics!.Activity != null); i++)
            if (!world.ContinueDungeonActivities(32)) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.All(world.Party, a => Assert.Null(a.Tactics!.Activity));
        Assert.True(watcher.Body.Fatigue > 20); Assert.True(sleeper.Body.Fatigue < 20);
        Assert.Equal(0, watcher.Body.RestUntil);
    }

    [Fact]
    public void AlarmsWakeOnlyMembersWhoHearAndAttractExistingPhysicalHostiles()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        var watcher = world.Party.Single(); var near = AddCamper(world, ClassKind.Ranger, 1);
        var far = AddCamper(world, ClassKind.Sentinel, 2);
        var origin = watcher.Position;
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 12; x++) world.Chunks.Edit(origin.Offset(x, y), Material.Floor);
        near.Position = origin.Offset(6, 0);
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy is { Dormant: false } && a.Position.Floor == 1);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Watch, Choice: 60)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 60)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 60)).Success);
        enemy.Position = origin.Offset(10, 0);
        world.ContinueDungeonActivities(1);
        Assert.Null(watcher.Tactics!.Activity); Assert.Null(near.Tactics!.Activity); Assert.NotNull(far.Tactics!.Activity);
        Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "CampAlarm" && e.Source == watcher.Id);
        Assert.Equal(new EnemyAwareness(watcher.Id, origin, world.State.Tactics.Events.Last(e => e.Kind == "CampAlarm").Time), enemy.Tactics!.Enemy!.Awareness);
    }

    [Fact]
    public void SoundLosesRangeThroughWallsAndDoesNotLeakAcrossFloors()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        var source = world.Party.Single().Position; var listener = source.Offset(10, 0);
        for (int y = -17; y <= 17; y++) for (int x = -17; x <= 17; x++) world.Chunks.Edit(source.Offset(x, y), Material.Floor);
        var combat = world.DungeonCombat;
        Assert.True(combat.CanHear(source, listener, 16));
        for (int y = -17; y <= 17; y++) for (int x = 3; x <= 7; x++) world.Chunks.Edit(source.Offset(x, y), Material.Rock);
        Assert.False(combat.CanHear(source, listener, 16));
        Assert.False(combat.CanHear(source, source with { Floor = 2 }, 16));
    }

    [Fact]
    public void DamageInterruptsOnlyItsVictimWithoutMovingTheirRecovery()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        var hero = world.Party.Single(); var friend = AddCamper(world, ClassKind.Ranger, 2);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 60)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 60)).Success);
        var combat = world.DungeonCombat; string timeline = JsonSerializer.Serialize(combat.State.Timeline);
        combat.ResolveAttack(new(combat.State.NextAction++, friend.Id, friend.Position, [hero.Position],
            "test:camp-hit", AttackDelivery.Area, DamageElement.Physical, 1, combat.Clock));
        Assert.Null(hero.Tactics!.Activity); Assert.NotNull(friend.Tactics!.Activity);
        Assert.Equal(timeline, JsonSerializer.Serialize(combat.State.Timeline));
    }

    [Fact]
    public void PartialRestAndWatchResumeExactlyAndAutomaticFoodKeepsTheOrder()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        var hero = world.Party.Single(); hero.Body.Fatigue = 20; hero.Body.Hunger = 35;
        var other = AddCamper(world, ClassKind.Ranger, 2);
        int food = world.Count("food");
        Assert.True(world.SubmitDungeon(new(TacticalAction.Rest, Choice: 10)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Watch, Choice: 10)).Success);
        world.ContinueDungeonActivities(8);
        Assert.Equal(food - 1, world.Count("food")); Assert.NotNull(hero.Tactics!.Activity);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-camp-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            for (int i = 0; i < 4; i++) { world.ContinueDungeonActivities(8); loaded.ContinueDungeonActivities(8); }
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
