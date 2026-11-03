using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeResidentWorkTests
{
    private static (WorldSession World, Actor Member, Actor Resident, WorkOrder Order) Prepare()
    {
        var world = WorldSession.CreateNativeCampaign(42); var member = world.DungeonMember;
        var resident = world.ActorById(1004);
        resident.Position = member.Position.Offset(0, -1);
        world.Chunks.Edit(resident.Position, Material.Floor);
        var target = resident.Position.Offset(1, 0); world.Chunks.Edit(target, Material.Floor); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Build, target, Material: Material.Wall))).Success);
        return (world, member, resident, Assert.Single(world.State.Work));
    }

    private static TacticalIntent Assign(Actor resident, WorkOrder order) => new(TacticalAction.StartWork,
        Work: new(WorldCommandKind.AssignWork, ActorId: resident.Id, WorkId: order.Id));

    [Fact]
    public void DelegatingPaysOnlyTheReadyMembersRecoveryAndKeepsTheRealWorkerAndReservation()
    {
        var (world, member, resident, order) = Prepare(); using var dispose = world;
        var combat = world.DungeonCombat; var intent = Assign(resident, order);
        string before = JsonSerializer.Serialize(world.State);
        var preview = combat.Preview(intent); Assert.True(preview.Valid, preview.Reason);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        long clock = combat.Clock; int stone = world.Count("stone"); var position = resident.Position;
        Assert.True(combat.Commit(intent).Success);
        Assert.Equal(clock, combat.Clock);
        Assert.Equal(clock + preview.Recovery, Assert.Single(world.State.Tactics!.Timeline.Pending,
            e => e.Kind == ScheduledKind.ActorReady && e.ActorId == member.Id).Due);
        Assert.Equal(resident.Id, order.Worker); Assert.Equal(member.Id, order.RequestedBy);
        Assert.Contains(order, world.DungeonWorkOrders); Assert.True(order.PinnedWorker); Assert.False(order.Suspended);
        Assert.Null(member.Tactics!.Activity); Assert.Null(resident.Tactics); Assert.False(resident.Recruited);
        Assert.Equal(position, resident.Position); Assert.Equal(0, order.Progress); Assert.Equal(stone, world.Count("stone"));
        Assert.Single(world.State.Tactics.Events, e => e.Kind == "WorkAssigned" && e.Source == member.Id && e.Target == resident.Id);
        Assert.Same(member, combat.AdvanceToPartyDecision());
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData("range")] [InlineData("floor")] [InlineData("wall")] [InlineData("busy")] [InlineData("missing")] [InlineData("unowned")]
    public void InvalidDelegationCannotSpendRecoveryChangeTheWorkerOrReserveAgain(string reason)
    {
        var (world, member, resident, order) = Prepare(); using var dispose = world;
        if (reason == "range") resident.Position = member.Position.Offset(0, -4);
        if (reason == "floor") resident.Position = resident.Position with { Floor = 1 };
        if (reason == "wall")
        {
            resident.Position = member.Position.Offset(0, -2);
            world.Chunks.Edit(member.Position.Offset(0, -1), Material.Wall);
        }
        if (reason == "busy") world.State.Work.Add(new() { Id = world.State.NextWorkId++, Kind = WorkKind.Dig,
            Target = resident.Position.Offset(0, -1), Worker = resident.Id, Before = Material.Wall, After = Material.Floor, Required = 10 });
        if (reason == "unowned") { order.Worker = 0; order.RequestedBy = 0; member.Tactics!.Activity = null; }
        var intent = reason == "missing" ? new TacticalIntent(TacticalAction.StartWork,
            Work: new(WorldCommandKind.AssignWork, ActorId: 999999, WorkId: order.Id)) : Assign(resident, order);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.DungeonCombat.Preview(intent).Valid);
        Assert.False(world.DungeonCombat.Commit(intent).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void DelegatedOrdersStayVisibleAndCanBeCancelledOrTakenBackWithoutDuplicatingSupplies()
    {
        var (world, member, resident, order) = Prepare(); using var dispose = world;
        int stone = world.Count("stone");
        Assert.True(world.SubmitDungeon(Assign(resident, order)).Success);
        Assert.Contains(order, world.DungeonWorkOrders);
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.AssignWork, WorkId: order.Id))).Success);
        Assert.Equal(member.Id, order.Worker); Assert.Equal(order.Id, member.Tactics!.Activity!.Work);
        Assert.Equal(stone, world.Count("stone"));
        Assert.True(world.SubmitDungeon(Assign(resident, order)).Success);
        long clock = world.DungeonCombat.Clock;
        Assert.True(world.CancelDungeonWork(order.Id).Success);
        Assert.Equal(clock, world.DungeonCombat.Clock); Assert.Equal(stone + 2, world.Count("stone"));
        Assert.Empty(world.DungeonWorkOrders); Assert.False(world.CancelDungeonWork(order.Id).Success);
    }

    [Fact]
    public void DelegatedSuppliedWorkFinishesPhysicallyOffscreenAndContinuesExactlyAfterLoading()
    {
        var (world, member, resident, order) = Prepare(); using var dispose = world;
        // Isolate the final slice of an existing supplied construction order.
        order.Progress = 14.9;
        foreach (var supply in order.Supplies!) supply.Phase = SupplyPhase.Delivered;
        Assert.True(world.SubmitDungeon(Assign(resident, order)).Success);
        member.Position = world.State.Position = member.Position with { Floor = 1 };
        world.Chunks.Edit(member.Position, Material.Floor); world.Reveal();
        var npcPosition = resident.Position; int stone = world.Count("stone");
        string root = Path.Combine(Path.GetTempPath(), "roag-delegated-work-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            for (int i = 0; i < 12; i++)
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            }
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Empty(world.DungeonWorkOrders); Assert.Equal(Material.Wall, world.Chunks.At(order.Target));
            Assert.True(resident.Position.Distance(order.Target) <= 1); Assert.Equal(npcPosition, resident.Position);
            Assert.Equal(stone, world.Count("stone")); Assert.True(resident.Body.WorkDone > 0);
            Assert.Null(resident.Tactics);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
