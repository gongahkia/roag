using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeSocietyTests
{
    // An explicit calendar-boundary fixture, with real retained people/towns and
    // native combat. Distant creature turns are deferred to isolate society.
    private static WorldSession AtTick(long tick)
    {
        var world = WorldSession.Create(42);
        world.State.Tick = tick; world.State.NativeCampaign = new();
        var combat = world.CreateTacticalSimulation(new(EnableSurvival: true));
        var hero = world.Party.Single();
        combat.Register(hero, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42), AutoEat = false });
        foreach (var enemy in world.State.Actors.Where(a => a.Life == LifeState.Alive && a.Kind is ActorKind.Beast or ActorKind.Raider))
            combat.RegisterRetainedEnemy(enemy, combat.Clock + 1000000);
        world.Reveal(); Assert.Same(hero, combat.AdvanceToPartyDecision());
        return world;
    }

    private static void Wait(WorldSession world, int turns)
    {
        for (int i = 0; i < turns; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
    }

    [Theory]
    [InlineData(0, 60)] [InlineData(299, 12)]
    public void NeverVisitedResidentsActivateAndWalkPhysicallyWithoutHourlyGuardRelocation(long tick, int turns)
    {
        using var world = AtTick(tick);
        var guard = world.State.Actors.First(a => a.Job == "guard" && a.HomeTown == 1);
        var home = world.Plan.Towns[guard.HomeTown].Center;
        for (int y = -20; y <= -16; y++) for (int x = -8; x <= 12; x++) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        guard.Position = home.Offset(10, -18); var start = guard.Position;
        Assert.True(start.Distance(world.State.Position) > 100);
        Assert.False(world.Chunks.Seen(start)); Assert.Null(guard.Routine);
        var identities = world.State.Actors.Select(a => a.Id).ToArray();
        Wait(world, turns);
        Assert.NotNull(guard.Routine);
        Assert.NotEqual(start, guard.Position);
        Assert.Equal(1, start.Distance(guard.Position));
        Assert.False(world.Chunks.Seen(start)); Assert.False(world.Chunks.Seen(guard.Position));
        Assert.Equal(identities, world.State.Actors.Select(a => a.Id));
        Assert.All(world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited && a.Life == LifeState.Alive), a => Assert.NotNull(a.Routine));
        Assert.Null(guard.Tactics);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void UnseenResidentJourneysResumeExactlyAfterSavingWithThePartyOnAnotherFloor()
    {
        using var world = AtTick(0); var hero = world.Party.Single();
        hero.Position = world.State.Position = hero.Position with { Floor = 1 };
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        Wait(world, 60);
        Assert.Contains(world.State.Actors, a => !a.Recruited && a.Kind == ActorKind.Person && a.HomeTown == 1 && a.Journey != null);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-society-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Wait(world, 60); Wait(loaded, 60);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void ARescuerCanKeepTheirNativeBuildAndParticipateInOrdinaryCommunityLife()
    {
        using var world = AtTick(299);
        var resident = world.ActorById(1004); var friend = world.ActorById(1005);
        var home = world.Plan.Towns[resident.HomeTown].Center;
        for (int y = -19; y <= -16; y++) for (int x = 4; x <= 8; x++) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        resident.Position = home.Offset(6, -18); friend.Position = resident.Position.Offset(1, 0);
        world.DungeonCombat.Register(resident, new() { Build = CharacterBuild.Create(ClassKind.Spiritbinder, 42), AutoEat = false,
            Resident = new() { Home = resident.Position, LastKnown = resident.Position } }, world.DungeonCombat.Clock + 1000000);
        int trust = resident.Trust.GetValueOrDefault(friend.Id);
        Wait(world, 12);
        Assert.Contains(resident.Memories, m => m.Key == "company" && m.Other == friend.Id);
        Assert.Contains(friend.Memories, m => m.Key == "company" && m.Other == resident.Id);
        Assert.Equal(trust + 2, resident.Trust[friend.Id]);
        Assert.NotNull(resident.Tactics!.Resident); Assert.False(resident.Recruited);
        Assert.DoesNotContain(world.Party.Single().Memories, m => m.Key == "company");
    }

    [Fact]
    public void NativeSocialInvitationsAreAuthoritativeAcrossSavingBetweenWorldTicks()
    {
        using var world = AtTick(17 * WorldSession.TicksPerHour - 1);
        Wait(world, 12); // Populate ordinary world routines before the native decision.
        var resident = world.ActorById(1004); var inviter = world.ActorById(1005);
        var place = world.Plan.Towns.First(t => !world.WeatherAt(t.Center).Severe).Center.Offset(0, -10);
        for (int y = -3; y <= 3; y++) for (int x = -3; x <= 3; x++) world.Chunks.Edit(place.Offset(x, y), Material.Floor);
        resident.Position = place; inviter.Position = place.Offset(3, 0);
        resident.FriendId = 1007; resident.Trust[inviter.Id] = 20;
        inviter.Routine = new(RoutineKind.Social, place, world.State.Tick, "Seeking a friend's company", resident.Id);
        world.DungeonCombat.Register(resident, new() { Build = CharacterBuild.Create(ClassKind.Spiritbinder, 42), AutoEat = false,
            Resident = new() { Home = place, LastKnown = place } }, world.DungeonCombat.Clock + 1);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-invitation-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Wait(world, 1); Wait(loaded, 1);
            Assert.Equal(RoutineKind.Social, resident.Routine!.Kind);
            Assert.Equal(inviter.Id, resident.Routine.Other);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
