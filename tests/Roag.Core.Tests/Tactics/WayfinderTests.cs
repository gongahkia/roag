using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void BeaconPlantIsPureUntilPaidAndReturnEntersOnlyTheLanding()
    {
        using var a = new Arena(ClassKind.Wayfinder);
        string before = JsonSerializer.Serialize(a.World.State);
        for (int i = 0; i < 20; i++) Assert.True(a.Combat.Preview(new(TacticalAction.Special, Facing.West)).Valid);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        int random = a.Combat.State.RandomState;
        a.Act(TacticalAction.Special, Facing.West); a.Next();
        Assert.Equal(91, a.Combat.Clock);
        Assert.Equal(random, a.Combat.State.RandomState);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "HitConfirmed");
        a.Enemy.Position = new(110, 110);
        for (int i = 0; i < 3; i++) { a.Act(TacticalAction.Move); a.Next(); }
        var preview = a.Combat.Preview(new(TacticalAction.Dash, Facing.East));
        Assert.True(preview.Valid, preview.Reason);
        Assert.Equal(4, preview.ReturnDistance);
        Assert.Equal(100, preview.Recovery);
        long action = a.Combat.State.NextAction;
        long clock = a.Combat.Clock;
        a.Act(TacticalAction.Dash); a.Next();
        Assert.Equal(clock + 100, a.Combat.Clock);
        Assert.Equal(new(99, 100), a.Hero.Position);
        Assert.Null(a.Hero.Tactics!.Resources.Beacon);
        var entered = Assert.Single(a.Combat.State.Events, e => e.Kind == "TileEntered" && e.Action == action);
        Assert.Equal(a.Hero.Position, entered.Position);
        Assert.True(entered.Forced);
        Assert.Equal(new WorldPosition(103, 100), entered.From);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "Moved" && e.Action == action);
        Assert.Null(a.Combat.Preview(new(TacticalAction.Dash, Facing.East)).ReturnDistance);
        Assert.Equal(2, a.Combat.Preview(new(TacticalAction.Dash, Facing.East)).Cells.Count);
    }

    [Fact]
    public void BeaconPlantRejectsUnexploredOccupiedDiagonalAndStairTilesForFree()
    {
        using var a = new Arena(ClassKind.Wayfinder);
        var target = new WorldPosition(101, 100);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(new(TacticalAction.Special, Facing.NorthEast)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.World.Chunks.Delta(target.Chunk).Discovered.Remove(target.LocalIndex);
        Assert.False(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
        a.World.Chunks.Delta(target.Chunk).Discovered.Add(target.LocalIndex);
        a.Enemy.Position = target;
        Assert.False(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
        a.Enemy.Position = new(103, 100);
        a.World.Chunks.Edit(target, Material.StairsDown);
        Assert.False(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
        Assert.Equal(0, a.Combat.Clock);
        Assert.Null(a.Hero.Tactics!.Resources.Beacon);
    }

    [Fact]
    public void FailedReturnsKeepTheirBeaconWithoutFallingBackToAnOrdinaryDash()
    {
        using var a = new Arena(ClassKind.Wayfinder);
        a.Act(TacticalAction.Special); a.Next();
        var beacon = a.Hero.Tactics!.Resources.Beacon!;
        void Reject()
        {
            string before = JsonSerializer.Serialize(a.World.State);
            Assert.False(a.Combat.Commit(new(TacticalAction.Dash, Facing.North)).Success);
            Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
            Assert.Equal(beacon, a.Hero.Tactics.Resources.Beacon);
        }
        a.Enemy.Position = beacon.Position; Reject();
        a.Enemy.Position = new(110, 110);
        a.World.Visible.Remove(beacon.Position); Reject();
        a.World.Visible.Add(beacon.Position);
        a.World.Chunks.Edit(beacon.Position, Material.StairsUp); Reject();
        a.World.Chunks.Edit(beacon.Position, Material.Floor);
        a.Hero.Position = new(108, 100); Reject(); // seven tile shortest path
        a.Hero.Position = beacon.Position; Reject();
        a.Hero.Position = new(100, 100, 1); Reject();
    }

    [Fact]
    public void BeaconReturnCountsActualExploredRouteLengthAndIgnoresIntermediateMobileBodies()
    {
        using var a = new Arena(ClassKind.Wayfinder);
        a.Act(TacticalAction.Special); a.Next();
        a.Hero.Position = a.World.State.Position = new(105, 100);
        a.Enemy.Position = new(104, 100);
        a.World.Chunks.Edit(new(103, 100), Material.Wall);
        a.World.Reveal();
        a.World.Visible.Add(new(101, 100)); // isolate route eligibility from line of sight
        var preview = a.Combat.Preview(new(TacticalAction.Dash));
        Assert.Equal(6, preview.ReturnDistance); // four straight tiles plus two around the wall
        a.World.Chunks.Edit(new(103, 99), Material.Wall);
        a.World.Chunks.Edit(new(103, 101), Material.Wall);
        Assert.False(a.Combat.Preview(new(TacticalAction.Dash)).Valid); // detour now exceeds six
        a.World.Chunks.Edit(new(103, 100), Material.Floor);
        // Only the straight row is explored, so the missing tile cannot be bypassed.
        for (int y = 95; y <= 110; y++) for (int x = 95; x <= 110; x++)
        {
            var p = new WorldPosition(x, y);
            if (y != 100 || x == 102) a.World.Chunks.Delta(p.Chunk).Discovered.Remove(p.LocalIndex);
        }
        Assert.False(a.Combat.Preview(new(TacticalAction.Dash)).Valid);
        a.World.Chunks.Delta(new WorldPosition(102, 100).Chunk).Discovered.Add(new WorldPosition(102, 100).LocalIndex);
        Assert.Equal(4, a.Combat.Preview(new(TacticalAction.Dash)).ReturnDistance);
        long action = a.Combat.State.NextAction;
        a.Act(TacticalAction.Dash);
        Assert.Equal(new(101, 100), a.Hero.Position);
        Assert.Equal(new(104, 100), a.Enemy.Position);
        Assert.Single(a.Combat.State.Events, e => e.Action == action && e.Kind == "TileEntered");
    }

    [Fact]
    public void BeaconRemainsBoundToAnActualGeneratedRoomAfterExcavation()
    {
        using var a = new Arena(ClassKind.Wayfinder);
        var site = a.World.Plan.Sites.First(s => s.Purpose == "fort");
        var footprint = a.World.Chunks.Generator.GenerateFootprint(false, site.Id, 1);
        WorldPosition Cell(int i) => new(site.Center.X - 32 + i % 64, site.Center.Y - 32 + i / 64, 1);
        int start = Enumerable.Range(65, footprint.Cells.Length - 130).First(i => i % 64 < 62
            && footprint.RoomCells[i] > 0 && footprint.RoomCells[i] == footprint.RoomCells[i + 1]
            && footprint.Cells[i] == Material.Floor && footprint.Cells[i + 1] == Material.Floor
            && a.World.CanEnter(Cell(i)) && a.World.CanEnter(Cell(i + 1)));
        a.Hero.Position = a.World.State.Position = Cell(start); a.World.Reveal();
        a.Act(TacticalAction.Special); a.Next();
        var beacon = a.Hero.Tactics!.Resources.Beacon!;
        Assert.Equal(RoomArea.Site, beacon.Room.Area);
        int elsewhere = Enumerable.Range(0, footprint.Cells.Length).First(i => footprint.RoomCells[i] > 0
            && footprint.RoomCells[i] != beacon.Room.Room && footprint.Cells[i] == Material.Floor);
        a.Hero.Position = Cell(elsewhere); a.World.Visible.Add(beacon.Position);
        Assert.False(a.Combat.Preview(new(TacticalAction.Dash)).Valid);
        // Digging through intervening walls does not merge the authored room identities.
        for (int i = 0; i < footprint.Cells.Length; i++) a.World.Chunks.Edit(Cell(i), Material.Floor);
        Assert.False(a.Combat.Preview(new(TacticalAction.Dash)).Valid);
        a.Hero.Position = Cell(start);
        Assert.True(a.Combat.Preview(new(TacticalAction.Dash)).Valid);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        a.Hero.Tactics.Resources.Beacon = beacon with { Room = beacon.Room with { Room = beacon.Room.Room + 10_000 } };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void DuplicateWayfindersKeepIndependentBeaconsThroughSaveAndReturn()
    {
        using var a = new Arena(ClassKind.Wayfinder, ClassKind.Wayfinder);
        a.Act(TacticalAction.Special);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision()); a.Act(TacticalAction.Special);
        string directory = Path.Combine(Path.GetTempPath(), "roag-beacons-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json");
            a.World.Save(slot);
            using var loaded = WorldSession.Load(slot);
            var resumed = new TacticalCombat(loaded, _ => { });
            a.Next(); Assert.Equal(a.Hero.Id, resumed.AdvanceToDecision()!.Id);
            a.Act(TacticalAction.Dash); Assert.True(resumed.Commit(new(TacticalAction.Dash, Facing.East)).Success);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.Equal(a.Hero.Position, loaded.Party.First().Position);
            Assert.Null(a.Hero.Tactics!.Resources.Beacon);
            Assert.NotNull(a.Friend.Tactics!.Resources.Beacon);
            Assert.Equal(a.Friend.Tactics.Resources.Beacon, loaded.Party.Last().Tactics!.Resources.Beacon);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
