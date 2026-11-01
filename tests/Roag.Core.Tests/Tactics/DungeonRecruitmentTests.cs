using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonRecruitmentTests
{
    private static Actor StandBeside(WorldSession world, DungeonRecruit recruit)
    {
        var actor = world.DungeonCombat.Active!;
        var target = world.State.Actors.Single(a => a.Id == recruit.Actor);
        var cell = new[] { target.Position.Offset(0, -1), target.Position.Offset(1, 0), target.Position.Offset(0, 1), target.Position.Offset(-1, 0) }
            .First(p => world.CanEnter(p, actor.Id));
        actor.Position = cell;
        Assert.True(world.SelectDungeonMember(actor.Id).Success);
        return actor;
    }

    [Theory]
    [InlineData(5)] [InlineData(37)] [InlineData(42)] [InlineData(-17)]
    public void NaturalAdventurersHaveFixedStoredClassesClearSafeRoomsAndNoTurnsBeforeJoining(int seed)
    {
        using var world = WorldSession.CreateDungeonRun(seed);
        var run = world.State.Dungeon!;
        Assert.Single(world.Party); Assert.Equal(4, run.Recruits.Count);
        Assert.Equal(3, run.Recruits.Count(r => world.State.Actors.Single(a => a.Id == r.Actor).Position.Floor == 1));
        foreach (var recruit in run.Recruits)
        {
            var actor = world.State.Actors.Single(a => a.Id == recruit.Actor);
            Assert.False(actor.Recruited); Assert.False(world.IsHostile(actor)); Assert.Null(actor.Tactics);
            Assert.Equal(NativeAppearance.ForClass(recruit.Class), actor.SpriteId);
            Assert.DoesNotContain(actor.Id, world.State.Tactics!.Participants);
            Assert.DoesNotContain(world.State.Tactics.Timeline.Pending, e => e.ActorId == actor.Id);
            var floor = run.Floors.Single(f => f.Contains(actor.Position));
            var room = floor.Rooms.Single(r => r.Contains(actor.Position));
            Assert.Equal("start", room.Role); Assert.NotEqual(floor.Entry, actor.Position);
            Assert.DoesNotContain(world.State.Actors, a => a.Tactics?.Enemy != null && room.Contains(a.Position));
            Assert.Null(world.ObjectAt(actor.Position)); Assert.Null(world.DungeonTerrainAt(actor.Position));
            Assert.True(TacticalReferences.RetainsActor(world.State, actor.Id));
        }
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ThreePaidRecruitmentsKeepEveryPositionAndBuildIdentityAndRejectTheFifthFreely()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var recruits = world.State.Dungeon!.Recruits.Where(r => world.State.Actors.Single(a => a.Id == r.Actor).Position.Floor == 1).ToArray();
        int gold = world.State.Inventory["gold"];
        foreach (var recruit in recruits)
        {
            var recruiter = StandBeside(world, recruit);
            var positions = world.State.Actors.ToDictionary(a => a.Id, a => a.Position);
            long clock = world.DungeonCombat.Clock;
            var preview = world.DungeonCombat.Preview(new(TacticalAction.Recruit, Target: recruit.Actor));
            Assert.True(preview.Valid, preview.Reason);
            Assert.True(world.DungeonCombat.Commit(new(TacticalAction.Recruit, Target: recruit.Actor)).Success);
            var member = world.Party.Single(a => a.Id == recruit.Actor);
            Assert.Equal(recruit.Class, member.Tactics!.Build!.Class);
            Assert.Equal(member.MaxHealth, member.Health);
            Assert.All(world.State.Actors, a => Assert.Equal(positions[a.Id], a.Position));
            Assert.Equal(gold, world.State.Inventory["gold"]);
            world.DungeonCombat.AdvanceToPartyDecision();
            long ready = world.State.Tactics!.ActiveActor == recruiter.Id ? world.DungeonCombat.Clock
                : world.State.Tactics.Timeline.Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == recruiter.Id).Due;
            Assert.Equal(clock + preview.Recovery, ready);
            Assert.True(world.DungeonCombat.Clock > clock);
        }
        Assert.Equal(4, world.Party.Count());
        var extra = world.State.Dungeon.Recruits.Single(r => !world.State.Party.Contains(r.Actor));
        StandBeside(world, extra);
        string before = JsonSerializer.Serialize(world.State);
        var rejected = world.SubmitDungeon(new(TacticalAction.Recruit, Target: extra.Actor));
        Assert.False(rejected.Success); Assert.Contains("Four", rejected.Message);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.False(world.ChooseDungeonClass(ClassKind.Ranger).Success);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void PermanentDeathFreesASurvivingSlotWhileRetainingTheFoundersAssignedBuildThroughSave()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var founder = world.Party.Single();
        founder.Tactics!.Boons.Inventory = founder.Tactics.Boons.Inventory.Grant("core:boon/temporal_echo", BoonRarity.Common, ClassKind.Adventurer);
        foreach (var recruit in world.State.Dungeon!.Recruits.Where(r => world.ActorById(r.Actor).Position.Floor == 1))
        {
            StandBeside(world, recruit);
            Assert.True(world.SubmitDungeon(new(TacticalAction.Recruit, Target: recruit.Actor)).Success);
        }
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy != null);
        void Hit(int damage) => world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, enemy.Id, enemy.Position,
            [founder.Position], "test:replacement-slot", AttackDelivery.Area, DamageElement.Physical, damage, world.DungeonCombat.Clock));
        Hit(10000); for (int i = 0; i < 5; i++) Hit(10);
        Assert.Equal(LifeState.Dead, founder.Life); Assert.False(world.State.AwaitingContinuation);
        world.DungeonCombat.AdvanceToPartyDecision();
        var replacement = world.State.Dungeon.Recruits.Single(r => !world.State.Party.Contains(r.Actor));
        StandBeside(world, replacement);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Recruit, Target: replacement.Actor)).Success);
        Assert.Equal(5, world.Party.Count()); Assert.Equal(4, world.Party.Count(a => a.Life != LifeState.Dead));
        string builds = JsonSerializer.Serialize(world.Party.Select(a => a.Tactics));
        string root = Path.Combine(Path.GetTempPath(), "roag-replacement-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot);
            using var loaded = WorldSession.Load(slot);
            Assert.Equal(world.State.Party, loaded.State.Party);
            Assert.Equal(builds, JsonSerializer.Serialize(loaded.Party.Select(a => a.Tactics)));
            Assert.Equal(LifeState.Dead, loaded.ActorById(founder.Id).Life);
            Assert.Single(loaded.ActorById(founder.Id).Tactics!.Boons.Inventory.Stacks);
            Assert.Empty(loaded.ActorById(replacement.Actor).Tactics!.Boons.Inventory.Stacks);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void UnjoinedAndJoinedRecruitsResumeWithoutRepopulationOrClassRerolls()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        var recruit = world.State.Dungeon!.Recruits[0];
        StandBeside(world, recruit);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Recruit, Target: recruit.Actor)).Success);
        string directory = Path.Combine(Path.GetTempPath(), "roag-recruits-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(directory);
            using var loaded = WorldSession.Load(directory);
            Assert.Equal(world.State.Dungeon.Recruits, loaded.State.Dungeon!.Recruits);
            Assert.Equal(world.State.Actors.Count, loaded.State.Actors.Count);
            Assert.Equal(JsonSerializer.Serialize(world.Party.Select(a => a.Tactics)), JsonSerializer.Serialize(loaded.Party.Select(a => a.Tactics)));
            Assert.Equal(JsonSerializer.Serialize(world.State.Tactics!.Timeline), JsonSerializer.Serialize(loaded.State.Tactics!.Timeline));
            Assert.False(loaded.SubmitDungeon(new(TacticalAction.Recruit, Target: recruit.Actor)).Success);
            loaded.State.Dungeon.Recruits[0] = recruit with { Class = (ClassKind)(((int)recruit.Class + 1) % 15) };
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(loaded.Plan, loaded.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void RemoteOrHostileRecruitmentRejectsWithoutChangingState()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        var remote = world.State.Dungeon!.Recruits.Single(r => world.State.Actors.Single(a => a.Id == r.Actor).Position.Floor == 2);
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy != null);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Recruit, Target: remote.Actor)).Success);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Recruit, Target: enemy.Id)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
}
