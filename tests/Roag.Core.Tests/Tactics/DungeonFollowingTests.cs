using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonFollowingTests
{
    private sealed class Group : IDisposable
    {
        public WorldSession World { get; }
        public Actor[] Members { get; }
        public WorldPosition Origin { get; }
        public Group(ClassKind leaderKind = ClassKind.Adventurer, ClassKind followerKind = ClassKind.Adventurer)
        {
            World = WorldSession.CreateDungeonRun(42, leaderKind);
            var floor = World.State.Dungeon!.Floors[0];
            var lowerFloor = World.State.Dungeon.Floors[1];
            Origin = (from x in Enumerable.Range(1, floor.Width - 14) from y in Enumerable.Range(1, floor.Height - 10)
                let p = floor.Origin.Offset(x, y)
                let lower = p.Offset(-4, 0) with { Floor = 2 }
                where lowerFloor.Contains(lower) && lowerFloor.Contains(lower.Offset(12, 8))
                where World.ObjectAt(lower.Offset(6, 4)) == null
                where !World.State.Actors.Any(a => !a.Recruited
                    && a.Position.X >= p.X - 2 && a.Position.X <= p.X + 14 && a.Position.Y >= p.Y - 2 && a.Position.Y <= p.Y + 10)
                where !Enumerable.Range(0, 13).Any(dx => Enumerable.Range(0, 9).Any(dy => World.ObjectAt(p.Offset(dx, dy)) != null))
                select p).First();
            for (int y = 0; y < 9; y++) for (int x = 0; x < 13; x++)
            {
                var p = Origin.Offset(x, y);
                World.Chunks.Edit(p, x is 0 or 12 || y is 0 or 8 ? Material.Wall : Material.Floor);
                World.State.Dungeon.Terrain.Remove(Roag.Core.Dungeons.DungeonRunState.CellKey(p));
            }
            var hero = World.Party.Single();
            Members = [hero, DungeonPartyTests.AddMember(World, followerKind, 1),
                DungeonPartyTests.AddMember(World, followerKind, 1), DungeonPartyTests.AddMember(World, followerKind, 1)];
            for (int i = 0; i < Members.Length; i++) Members[i].Position = Origin.Offset(5 - i, 4);
            Assert.True(World.SelectDungeonMember(hero.Id).Success);
            Assert.True(World.RegroupDungeonParty(hero.Id).Success);
        }
        public void Dispose() => World.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FasterFollowersClearTheFrontOfASlowerLeaderOnTheirOwnPaidTurns(bool farAhead)
    {
        using var group = new Group(ClassKind.Tinkerer, ClassKind.Ranger); var world = group.World;
        var leader = group.Members[0];
        group.Members[1].Position = group.Origin.Offset(farAhead ? 8 : 6, farAhead ? 2 : 4);
        group.Members[2].Position = group.Origin.Offset(farAhead ? 7 : 5, 2);
        group.Members[3].Position = group.Origin.Offset(4, 4);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.North)).Success);
        Assert.Equal(group.Origin.Offset(5, 3), leader.Position);
        if (farAhead) Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "Moved" && e.Source == group.Members[1].Id);
        Assert.DoesNotContain(group.Members.Skip(1), a => a.Position == leader.Position.Offset(0, -1));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.North)).Success);
        Assert.Equal(group.Origin.Offset(5, 2), leader.Position);
        Assert.Equal(4, group.Members.Select(a => a.Position).Distinct().Count());
        Assert.Contains(world.State.Tactics!.Timeline.Pending, e => e.ActorId == group.Members[2].Id && e.Kind == ScheduledKind.ActorReady);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void EveryFollowerStepIsItsOwnPaidScheduledActionWithoutSwappingOrBorrowingReadiness()
    {
        using var group = new Group(); var world = group.World;
        var order = world.State.Party.ToArray();
        for (int step = 0; step < 4; step++)
        {
            long clock = world.DungeonCombat.Clock;
            var positions = group.Members.ToDictionary(a => a.Id, a => a.Position);
            int events = world.State.Tactics!.Events.Count;
            Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
            Assert.Equal(clock + 100, world.DungeonCombat.Clock);
            Assert.Equal(group.Members[0].Id, world.State.Tactics.ActiveActor);
            foreach (var actor in group.Members)
            {
                Assert.Equal(positions[actor.Id].Offset(1, 0), actor.Position);
                Assert.Single(world.State.Tactics.Events.Skip(events), e => e.Kind == "Moved" && e.Source == actor.Id);
                if (actor.Id != world.State.Tactics.ActiveActor)
                    Assert.Equal(world.DungeonCombat.Clock, world.State.Tactics.Timeline.Pending.Single(e => e.Kind == ScheduledKind.ActorReady && e.ActorId == actor.Id).Due);
            }
            Assert.Equal(4, group.Members.Select(a => a.Position).Distinct().Count());
            Assert.Equal(order, world.State.Party);
        }
        Assert.Equal(16, world.State.Tactics!.Events.Count(e => e.Kind == "Moved" && world.State.Party.Contains(e.Source)));
        Assert.Contains(world.State.Tactics.Events, e => e.Kind == "Waited" && e.Time > 0 && !world.State.Party.Contains(e.Source));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void SplittingIsFreeAndRestoresIndividualInputWithoutMovingTheSeparatedMember()
    {
        using var group = new Group(); var world = group.World; var split = group.Members[3];
        string queue = JsonSerializer.Serialize(world.State.Tactics!.Timeline);
        long next = world.State.Tactics.NextAction;
        var position = split.Position;
        Assert.True(world.SplitDungeonMember(split.Id).Success);
        Assert.Equal(queue, JsonSerializer.Serialize(world.State.Tactics.Timeline)); Assert.Equal(next, world.State.Tactics.NextAction);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal(split.Id, world.State.Tactics.ActiveActor);
        Assert.Equal(0, world.DungeonCombat.Clock);
        Assert.Equal(position, split.Position);
        Assert.DoesNotContain(split.Id, world.State.Dungeon!.Following.Keys);
    }

    [Fact]
    public void LeaderChangesNeverReorderTurnsAndCannotSwapThroughAnOccupiedCorridor()
    {
        using var group = new Group(); var world = group.World;
        for (int y = 1; y < 8; y++) for (int x = 1; x < 12; x++)
            world.Chunks.Edit(group.Origin.Offset(x, y), y == 4 ? Material.Floor : Material.Wall);
        var positions = group.Members.ToDictionary(a => a.Id, a => a.Position);
        string queue = JsonSerializer.Serialize(world.State.Tactics!.Timeline);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Move, Facing.West)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.ChooseDungeonLeader(group.Members[3].Id).Success);
        Assert.Equal(queue, JsonSerializer.Serialize(world.State.Tactics.Timeline));
        Assert.Equal(group.Members[0].Id, world.State.Tactics.ActiveActor);
        Assert.All(group.Members, a => Assert.Equal(positions[a.Id], a.Position));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(group.Members[3].Id, world.State.Tactics.ActiveActor); // Its original time-zero turn, not a new turn.
        Assert.Equal(0, world.DungeonCombat.Clock);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.West)).Success);
        Assert.Equal(group.Origin.Offset(1, 4), group.Members[3].Position);
        Assert.Equal(4, group.Members.Select(a => a.Position).Distinct().Count());
    }

    [Fact]
    public void StairsMoveOnlyTheActorAndSeparateFollowersBeforeTheirNextDecision()
    {
        using var group = new Group(); var world = group.World; var hero = group.Members[0];
        var top = hero.Position.Offset(1, 0); var bottom = new WorldPosition(top.X - 4, top.Y, 2);
        world.Chunks.Edit(top, Material.StairsDown); world.Chunks.Edit(bottom, Material.StairsUp);
        Assert.True(world.CanEnter(bottom));
        var positions = group.Members.Skip(1).ToDictionary(a => a.Id, a => a.Position);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal(bottom, hero.Position);
        Assert.All(group.Members.Skip(1), a => Assert.Equal(positions[a.Id], a.Position));
        Assert.Empty(world.State.Dungeon!.Following);
        Assert.Equal(group.Members[1].Id, world.State.Tactics!.ActiveActor);
        Assert.All(world.Visible, p => Assert.Equal(1, p.Floor));
        Assert.False(world.RegroupDungeonParty(hero.Id).Success);
    }

    [Fact]
    public void NearbyCombatReturnsFollowerInputAndADownedLeaderCanBeRescuedInsteadOfAutoFollowing()
    {
        using var group = new Group(); var world = group.World; var hero = group.Members[0];
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy != null);
        enemy.Position = hero.Position.Offset(0, 2);
        Assert.True(world.IsActorInCombat(hero.Id));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(group.Members[1].Id, world.State.Tactics!.ActiveActor);
        Assert.False(world.RegroupDungeonParty(hero.Id).Success);
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics.NextAction++, enemy.Id, enemy.Position, [hero.Position],
            "test:down-leader", AttackDelivery.Melee, DamageElement.Physical, 1000, world.DungeonCombat.Clock));
        Assert.Equal(LifeState.Downed, hero.Life); Assert.Equal(5, hero.Tactics!.BleedTurns);
        world.State.Inventory["medicine"] = 1;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Revive, Target: hero.Id)).Success);
        Assert.Equal(LifeState.Alive, hero.Life); Assert.Equal(hero.MaxHealth / 4, hero.Health);
        Assert.Equal(0, world.Count("medicine"));
    }

    [Fact]
    public void GroupOrdersAndIndividualSelectionResumeExactlyAndSavedCyclesAreRejected()
    {
        using var group = new Group(); var world = group.World;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.True(world.SelectDungeonMember(group.Members[2].Id).Success);
        string directory = Path.Combine(Path.GetTempPath(), "roag-following-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(directory);
            using var loaded = WorldSession.Load(directory);
            Assert.Equal(JsonSerializer.Serialize(world.State.Dungeon!.Following), JsonSerializer.Serialize(loaded.State.Dungeon!.Following));
            Assert.Equal(world.DungeonMember.Id, loaded.DungeonMember.Id);
            Assert.Equal(world.State.Tactics!.ActiveActor, loaded.State.Tactics!.ActiveActor);
            Assert.Equal(JsonSerializer.Serialize(world.State.Tactics.Timeline), JsonSerializer.Serialize(loaded.State.Tactics.Timeline));
            Assert.All(world.Party, a => Assert.Equal(a.Position, loaded.ActorById(a.Id).Position));
            loaded.State.Dungeon.Following[group.Members[0].Id] = group.Members[1].Id;
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(loaded.Plan, loaded.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void SplitFloorGroupsShareOneClockWhileLocalCombatAndPendingAttacksRemainPrivateAndResumeExactly()
    {
        using var group = new Group(); var world = group.World;
        var a = group.Members[0]; var b = group.Members[1]; var c = group.Members[2]; var d = group.Members[3];
        Assert.True(world.SplitDungeonMember(c.Id).Success); Assert.True(world.SplitDungeonMember(d.Id).Success);
        for (int y = 0; y < 9; y++) for (int x = 0; x < 13; x++)
            world.Chunks.Edit(group.Origin.Offset(x, y) with { Floor = 2 }, x is 0 or 12 || y is 0 or 8 ? Material.Wall : Material.Floor);
        c.Position = group.Origin.Offset(4, 4) with { Floor = 2 };
        d.Position = group.Origin.Offset(3, 4) with { Floor = 2 };
        Assert.Null(world.ObjectAt(c.Position)); Assert.Null(world.ObjectAt(d.Position));
        Assert.True(world.RegroupDungeonParty(c.Id).Success);
        var enemy = world.State.Actors.First(e => e.Tactics?.DefinitionId == "core:enemy/mossbound_guard");
        enemy.Position = a.Position.Offset(0, 1);
        Assert.True(world.IsActorInCombat(a.Id)); Assert.False(world.IsActorInCombat(c.Id));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(b.Id, world.State.Tactics!.ActiveActor);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(c.Id, world.State.Tactics.ActiveActor);
        var beforeD = d.Position;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal(beforeD.Offset(1, 0), d.Position);
        Assert.Equal(100, world.DungeonCombat.Clock);
        for (int i = 0; i < 3; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(200, world.DungeonCombat.Clock);
        var attack = Assert.Single(world.State.Tactics.Attacks, attack => attack.Source == enemy.Id);
        Assert.Equal(a.Id, attack.IntendedTarget);
        Assert.True(world.SelectDungeonMember(c.Id).Success);
        Assert.Empty(world.RoagTelegraphs());
        Assert.DoesNotContain(world.DungeonCombat.VisibleTimeline(), entry => entry.ActorId == enemy.Id);
        string directory = Path.Combine(Path.GetTempPath(), "roag-split-groups-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(directory);
            using var loaded = WorldSession.Load(directory);
            Assert.Equal(c.Id, loaded.DungeonMember.Id);
            Assert.Equal(JsonSerializer.Serialize(world.State.Tactics), JsonSerializer.Serialize(loaded.State.Tactics));
            Assert.Equal(JsonSerializer.Serialize(world.State.Dungeon!.Following), JsonSerializer.Serialize(loaded.State.Dungeon!.Following));
            Assert.True(world.SelectDungeonMember(a.Id).Success); Assert.True(loaded.SelectDungeonMember(a.Id).Success);
            for (int i = 0; i < 3; i++)
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            }
            Assert.True(a.Health < a.MaxHealth);
            Assert.All(world.Party, member =>
            {
                var restored = loaded.ActorById(member.Id);
                Assert.Equal(member.Position, restored.Position); Assert.Equal(member.Health, restored.Health);
            });
            Assert.Equal(JsonSerializer.Serialize(world.State.Tactics), JsonSerializer.Serialize(loaded.State.Tactics));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
