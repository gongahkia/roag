using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void BulwarkPreviewIsPureAndTheOverlayBlocksMovementSightAndProjectiles()
    {
        using var a = new Arena(ClassKind.Geomancer);
        var cell = new WorldPosition(101, 100);
        string before = JsonSerializer.Serialize(a.World.State);
        for (int i = 0; i < 20; i++) Assert.True(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        int random = a.Combat.State.RandomState;
        a.Act(TacticalAction.Special);
        Assert.Equal(Material.Floor, a.World.Chunks.At(cell));
        Assert.Equal(random, a.Combat.State.RandomState);
        Assert.False(a.World.GroundOpen(cell));
        Assert.True(a.World.ObjectAt(cell)!.Solid);
        Assert.Contains(cell, a.World.Visible);
        Assert.DoesNotContain(a.Enemy.Position, a.World.Visible);
        a.Next();
        Assert.Equal(153, a.Combat.Clock);
        Assert.False(a.Combat.Preview(new(TacticalAction.Move, Facing.East)).Valid);
        Assert.Equal(new[] { cell }, a.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Cells);
        int enemyHealth = a.Enemy.Health;
        a.Act(TacticalAction.BasicAttack); a.Next();
        Assert.Equal(294, a.Combat.Clock);
        Assert.Equal(enemyHealth, a.Enemy.Health);
        Assert.NotNull(a.Hero.Tactics!.Resources.Bulwark);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "HitConfirmed");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void NativeTerrainCollisionKeepsPathfindingProbesAllocationFree()
    {
        using var a = new Arena(ClassKind.Geomancer);
        a.Act(TacticalAction.Special);
        var target = new WorldPosition(107, 100);
        for (int i = 0; i < 20; i++) a.World.LocalStep(a.Hero.Position, target, a.Hero.Id);
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        int checksum = 0;
        for (int i = 0; i < 1000; i++) checksum += a.World.LocalStep(a.Hero.Position, target, a.Hero.Id).Y;
        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        Assert.InRange(bytes, 0, 1024);
        Assert.True(checksum > 0);
        Assert.NotEqual(new(101, 100), a.World.LocalStep(a.Hero.Position, target, a.Hero.Id));
    }

    [Theory]
    [InlineData(Material.StairsUp)]
    [InlineData(Material.StairsDown)]
    [InlineData(Material.Door)]
    [InlineData(Material.Water)]
    [InlineData(Material.Camp)]
    public void BulwarkCannotReplaceStairsOrNonPlainGround(Material material)
    {
        using var a = new Arena(ClassKind.Geomancer);
        a.World.Chunks.Edit(new(101, 100), material);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(new(TacticalAction.Special, Facing.East)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Theory]
    [InlineData(Material.Grass)]
    [InlineData(Material.Earth)]
    [InlineData(Material.Bridge)]
    public void PlainWorldGroundSupportsTheSameConstructPlacementAsDungeonFloor(Material material)
    {
        using var a = new Arena(ClassKind.Geomancer);
        a.World.Chunks.Edit(new(101, 100), material);
        a.Act(TacticalAction.Special);
        Assert.Equal(material, a.World.Chunks.At(new(101, 100)));
    }

    [Fact]
    public void BulwarkCannotSealAOneTilePassageButCanOccupyItsDeadEnd()
    {
        using var a = new Arena(ClassKind.Geomancer);
        for (int y = 99; y <= 101; y++) for (int x = 99; x <= 103; x++)
            a.World.Chunks.Edit(new(x, y), y == 100 && x is >= 100 and <= 102 ? Material.Floor : Material.Wall);
        Assert.False(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
        a.World.Chunks.Edit(new(102, 100), Material.Wall);
        Assert.True(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
        Assert.False(a.Combat.Preview(new(TacticalAction.Special, Facing.SouthEast)).Valid);
        a.Enemy.Position = new(101, 100);
        Assert.False(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
    }

    [Fact]
    public void BulwarkConnectivityMatchesAnIndependentFullFloodAcrossRandomTerrain()
    {
        using var a = new Arena(ClassKind.Geomancer);
        a.Friend.Position = new(110, 109); a.Enemy.Position = new(110, 110);
        var random = new Random(7412);
        var candidate = new WorldPosition(101, 100);
        for (int sample = 0; sample < 200; sample++)
        {
            for (int y = 96; y <= 106; y++) for (int x = 96; x <= 106; x++)
                a.World.Chunks.Edit(new(x, y), x is 96 or 106 || y is 96 or 106 || random.Next(100) < 35
                    ? Material.Wall : Material.Floor);
            a.World.Chunks.Edit(a.Hero.Position, Material.Floor);
            a.World.Chunks.Edit(candidate, Material.Floor);
            int Reachable(bool excludeCandidate)
            {
                var seen = new HashSet<WorldPosition> { a.Hero.Position };
                var queue = new Queue<WorldPosition>(); queue.Enqueue(a.Hero.Position);
                while (queue.TryDequeue(out var p))
                    foreach (var next in new[] { p.Offset(0, -1), p.Offset(1, 0), p.Offset(0, 1), p.Offset(-1, 0) })
                        if ((!excludeCandidate || next != candidate) && a.World.GroundOpen(next) && seen.Add(next)) queue.Enqueue(next);
                return seen.Count;
            }
            Assert.Equal(Reachable(false) - 1 == Reachable(true), a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
        }
    }

    [Fact]
    public void DuplicateGeomancersReplaceOnlyTheirOwnBlockAndCancelOnlyTheirOldExpiry()
    {
        using var a = new Arena(ClassKind.Geomancer, ClassKind.Geomancer);
        a.Act(TacticalAction.Special);
        long oldTimer = a.Hero.Tactics!.Resources.Bulwark!.Schedule;
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Special);
        var other = a.Friend.Tactics!.Resources.Bulwark!;
        a.Next();
        Assert.True(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid); // replacing own tile is legal
        a.Act(TacticalAction.Special, Facing.North);
        Assert.True(a.World.GroundOpen(new(101, 100)));
        Assert.False(a.World.GroundOpen(other.Position));
        Assert.Equal(453, a.Hero.Tactics.Resources.Bulwark!.Expires);
        Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, e => e.Sequence == oldTimer);
        Assert.Contains(a.Combat.State.Queue.Snapshot().Pending, e => e.Sequence == other.Schedule);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void OtherGeomancersBlocksStillConstrainPlacementConnectivity()
    {
        using var a = new Arena(ClassKind.Geomancer, ClassKind.Geomancer);
        // A loop with a second exit. The friend's block closes that exit; the hero
        // cannot close the remaining passage. Mobile actors never divide the graph.
        for (int y = 98; y <= 104; y++) for (int x = 98; x <= 104; x++)
            a.World.Chunks.Edit(new(x, y), x is 98 or 104 || y is 98 or 104 ? Material.Wall : Material.Floor);
        for (int y = 99; y <= 103; y++) a.World.Chunks.Edit(new(101, y), y is 100 or 102 ? Material.Floor : Material.Wall);
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Special);
        a.Next();
        Assert.False(a.Combat.Preview(new(TacticalAction.Special, Facing.East)).Valid);
    }

    [Fact]
    public void BulwarkExpiresAtItsOriginalWorldTimeAfterOwnerDeathOnAnotherFloor()
    {
        using var a = new Arena(ClassKind.Geomancer, ClassKind.Duelist);
        a.Act(TacticalAction.Special);
        var position = a.Hero.Tactics!.Resources.Bulwark!.Position;
        a.Friend.Position = new(100, 102, 1);
        a.Hit(a.Hero, 10_000);
        for (int i = 0; i < 5; i++) a.Hit(a.Hero);
        Assert.Equal(LifeState.Dead, a.Hero.Life);
        Assert.False(a.World.GroundOpen(position));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        a.Next(a.Friend.Id);
        while (a.Combat.Clock < 300) { a.Act(TacticalAction.Wait); a.Next(a.Friend.Id); }
        Assert.Null(a.Hero.Tactics.Resources.Bulwark);
        Assert.True(a.World.GroundOpen(position));
        Assert.Equal(Material.Floor, a.World.Chunks.At(position));
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "GeomancerBlockExpired" && e.Time == 300 && e.Source == a.Hero.Id);
        Assert.Equal(1, a.World.State.Position.Floor);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void SavedBulwarksResumeExactlyAndRejectMissingOrMistimedExpiry()
    {
        using var a = new Arena(ClassKind.Geomancer);
        a.Act(TacticalAction.Special);
        string directory = Path.Combine(Path.GetTempPath(), "roag-bulwark-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json");
            a.World.Save(slot);
            using var loaded = WorldSession.Load(slot);
            var resumed = new TacticalCombat(loaded, _ => { });
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(a.Combat.AdvanceToDecision()!.Id, resumed.AdvanceToDecision()!.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(a.Hero.Tactics!.Resources.Bulwark, loaded.Party.Single().Tactics!.Resources.Bulwark);
                a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            }
            using var corrupt = WorldSession.Load(slot);
            var hero = corrupt.Party.Single();
            var block = hero.Tactics!.Resources.Bulwark!;
            hero.Tactics.Resources.Bulwark = block with { Expires = block.Expires - 1 };
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(corrupt.Plan, corrupt.State));
            hero.Tactics.Resources.Bulwark = block;
            corrupt.State.Tactics!.Queue.Cancel(block.Schedule);
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(corrupt.Plan, corrupt.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
