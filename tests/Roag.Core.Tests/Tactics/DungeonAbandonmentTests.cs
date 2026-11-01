using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonAbandonmentTests
{
    internal static void Down(WorldSession world, Actor member)
    {
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy != null && a.Position.Floor == member.Position.Floor);
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, enemy.Id, enemy.Position,
            [member.Position], "test:abandon-casualty", AttackDelivery.Area, DamageElement.Physical, 10000, world.DungeonCombat.Clock));
        Assert.Equal(LifeState.Downed, member.Life);
    }

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void AbandonmentPaysTheActingMembersRecoveryRegardlessOfDistanceAndRetainsCorpseOwnership(int floor)
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var actor = world.Party.Single(); var member = DungeonPartyTests.AddMember(world, ClassKind.Tinkerer, floor);
        member.Tactics!.Boons.Inventory = member.Tactics.Boons.Inventory.Grant("core:boon/temporal_echo", BoonRarity.Common, ClassKind.Tinkerer);
        Down(world, member);
        string before = JsonSerializer.Serialize(world.State), supplies = JsonSerializer.Serialize(world.State.Inventory);
        var position = member.Position; long experience = actor.Tactics!.Build!.Experience;
        var preview = world.PreviewDungeonAim(new("abandon", Target: member.Id));
        Assert.True(preview.Valid); Assert.Empty(preview.Cells); Assert.Equal(100, preview.Recovery);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.DungeonCombat.Commit(new(TacticalAction.Abandon, Target: member.Id)).Success);
        Assert.Equal(0, world.DungeonCombat.Clock); Assert.Null(world.DungeonCombat.Active);
        Assert.Equal(100, Assert.Single(world.State.Tactics!.Timeline.Pending, e => e.ActorId == actor.Id && e.Kind == ScheduledKind.ActorReady).Due);
        Assert.Equal(LifeState.Dead, member.Life); Assert.Equal(0, member.Tactics.BleedTurns); Assert.Equal(position, member.Position);
        Assert.Contains(member.Id, world.State.Party); Assert.Single(member.Tactics.Boons.Inventory.Stacks);
        Assert.Equal(experience, actor.Tactics.Build.Experience); Assert.Equal(supplies, JsonSerializer.Serialize(world.State.Inventory));
        Assert.DoesNotContain(world.State.Tactics.Timeline.Pending, e => e.ActorId == member.Id && e.Kind == ScheduledKind.ActorReady);
        Assert.Single(world.State.Tactics.Events, e => e.Kind == "Abandoned" && e.Source == actor.Id && e.Target == member.Id);
        Assert.False(world.State.AwaitingContinuation);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void InvalidTargetsAndInspectingARecoveringOrDownedMemberCannotAbandonOrSpendTime()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var hero = world.Party.Single(); var member = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        foreach (long target in new[] { 0, long.MaxValue, hero.Id, member.Id, world.State.Dungeon!.Recruits[0].Actor })
        {
            string before = JsonSerializer.Serialize(world.State);
            Assert.False(world.SubmitDungeon(new(TacticalAction.Abandon, Target: target)).Success);
            Assert.Equal(before, JsonSerializer.Serialize(world.State));
        }
        Down(world, member); Assert.True(world.SelectDungeonMember(member.Id).Success);
        string downed = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Abandon, Target: member.Id)).Success);
        Assert.Equal(downed, JsonSerializer.Serialize(world.State));
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Abandon, Target: member.Id)).Success);
        string dead = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Abandon, Target: member.Id)).Success);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Revive, Target: member.Id)).Success);
        Assert.Equal(dead, JsonSerializer.Serialize(world.State));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AbandonmentPersistsAfterTheCompletePaidActionAndFailedWritesRemainRetryable(bool failWrite)
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var member = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2); Down(world, member);
        string root = Path.Combine(Path.GetTempPath(), "roag-abandon-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); byte[] before = File.ReadAllBytes(slot);
            if (failWrite) world.SavePath = Path.Combine(slot, "cannot-write.json");
            var result = world.SubmitDungeon(new(TacticalAction.Abandon, Target: member.Id));
            Assert.True(result.Success); Assert.False(world.State.AwaitingContinuation);
            Assert.Equal(LifeState.Dead, member.Life); Assert.Equal(100, world.DungeonCombat.Clock);
            Assert.Equal(failWrite, world.DeathPending);
            if (failWrite)
            {
                Assert.Contains("Abandonment could not be saved", result.Message); Assert.Equal(before, File.ReadAllBytes(slot));
                world.Save(slot); Assert.False(world.DeathPending);
            }
            else Assert.NotEqual(before, File.ReadAllBytes(slot));
            using var loaded = WorldSession.Load(slot);
            Assert.Equal(LifeState.Dead, loaded.ActorById(member.Id).Life); Assert.Equal(world.State.Party, loaded.State.Party);
            Assert.Equal(JsonSerializer.Serialize(world.State.Tactics), JsonSerializer.Serialize(loaded.State.Tactics));
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
