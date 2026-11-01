using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonPartyTests
{
    private static Actor AddMember(WorldSession world, ClassKind kind, int floor)
    {
        var site = world.State.Dungeon!.Floors.Single(f => f.Origin.Floor == floor);
        var cell = Enumerable.Range(1, site.Width - 2)
            .SelectMany(x => Enumerable.Range(1, site.Height - 2).Select(y => site.Origin.Offset(x, y)))
            .First(p => world.CanEnter(p));
        var member = ActorFactory.Create(world.Content, world.Content.Game.Traveller, world.Seed,
            world.State.NextActorId++, cell, 0);
        member.Recruited = true; member.Equipment.Clear(); member.Talents.Clear(); member.CombatTrait = "";
        world.State.Actors.Add(member); world.State.Party.Add(member.Id);
        world.DungeonCombat.Register(member, new() { Build = CharacterBuild.Create(kind, (int)member.Id), AutoEat = false });
        return member;
    }

    [Fact]
    public void SelectingAnotherFloorChangesOnlyTheViewAndCannotSpendTheReadyMembersAction()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        var hero = world.Party.Single(); var friend = AddMember(world, ClassKind.Ranger, 2);
        string queue = JsonSerializer.Serialize(world.State.Tactics!.Timeline);
        int random = world.State.Tactics.RandomState;
        long next = world.State.Tactics.NextAction;
        Assert.True(world.SelectDungeonMember(friend.Id).Success);
        Assert.Same(friend, world.DungeonMember);
        Assert.Equal(hero.Id, world.State.Tactics.ActiveActor);
        Assert.Equal(friend.Position, world.State.Position);
        Assert.All(world.Visible, p => Assert.Equal(2, p.Floor));
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.False(world.PreviewDungeonAim(new("move", 1, 0)).Valid);
        Assert.Empty(world.RoagPreview(new("basic_attack", 1, 0)));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.Equal(queue, JsonSerializer.Serialize(world.State.Tactics.Timeline));
        Assert.Equal(random, world.State.Tactics.RandomState); Assert.Equal(next, world.State.Tactics.NextAction);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(friend.Id, world.State.Tactics.ActiveActor);
        Assert.Equal(friend.Id, world.State.Tactics.ControlledActor);
        Assert.Equal(0, world.DungeonCombat.Clock); // The friend's already-scheduled readiness is still at zero.
    }

    [Fact]
    public void FourMemberSavePreservesIndependentBuildsPositionsRecoveryAndSelectedView()
    {
        using var world = WorldSession.CreateDungeonRun(42, ClassKind.Tinkerer);
        var hero = world.Party.Single();
        var same = AddMember(world, ClassKind.Tinkerer, 1);
        var ranger = AddMember(world, ClassKind.Ranger, 2);
        var sentinel = AddMember(world, ClassKind.Sentinel, 2);
        hero.Tactics!.Boons.Inventory = hero.Tactics.Boons.Inventory.Grant("core:boon/temporal_echo", BoonRarity.Common, ClassKind.Tinkerer);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(same.Id, world.State.Tactics!.ActiveActor);
        Assert.True(world.SelectDungeonMember(ranger.Id).Success);
        string directory = Path.Combine(Path.GetTempPath(), "roag-party-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(directory);
            using var loaded = WorldSession.Load(directory);
            Assert.Equal(world.State.Party, loaded.State.Party);
            Assert.Equal(ranger.Id, loaded.DungeonMember.Id);
            Assert.Equal(same.Id, loaded.State.Tactics!.ActiveActor);
            Assert.Equal(ranger.Position, loaded.State.Position);
            Assert.Equal(JsonSerializer.Serialize(world.State.Tactics.Timeline), JsonSerializer.Serialize(loaded.State.Tactics.Timeline));
            foreach (var member in world.Party)
            {
                var restored = loaded.Party.Single(a => a.Id == member.Id);
                Assert.Equal(member.Position, restored.Position);
                Assert.Equal(JsonSerializer.Serialize(member.Tactics), JsonSerializer.Serialize(restored.Tactics));
            }
            Assert.Empty(loaded.Party.Single(a => a.Id == same.Id).Tactics!.Boons.Inventory.Stacks);
            Assert.False(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(loaded.SelectDungeonMember(same.Id).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(ranger.Id, loaded.State.Tactics.ActiveActor);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void InvalidSelectionIsFreeAndDoesNotRevealAnEnemyOrNonmember()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        string before = JsonSerializer.Serialize(world.State);
        var visible = world.Visible.ToHashSet();
        Assert.False(world.SelectDungeonMember(world.State.Actors.First(a => a.Tactics?.Enemy != null).Id).Success);
        Assert.False(world.SelectDungeonMember(long.MaxValue).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State)); Assert.True(visible.SetEquals(world.Visible));
    }
}
