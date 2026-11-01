using System.Text.Json;
using Roag.Client;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class DungeonMapTests
{
    [Fact]
    public void MapContainsOnlyExploredCurrentFloorTerrainAndNeverAdvancesTheGame()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        string before = JsonSerializer.Serialize(world.State);
        var map = DungeonMap.Build(world);
        Assert.NotEmpty(map.Cells);
        Assert.All(map.Cells, cell =>
        {
            Assert.Equal(world.State.Position.Floor, cell.Position.Floor);
            Assert.True(world.Chunks.Seen(cell.Position));
            Assert.Equal(world.Visible.Contains(cell.Position), cell.Visible);
        });
        Assert.DoesNotContain(map.Cells, cell => cell.Position == map.Origin);
        Assert.Equal(world.State.Position, map.Player);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void RememberedGateMarksDoNotRevealAnOffscreenOpeningOrClosure()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        var gate = world.State.Tactics!.Gates.Nodes.First(g => g.Position.Floor == 1);
        Assert.True(world.Chunks.Seen(gate.Position)); world.Visible.Clear();
        var closed = DungeonMap.Build(world).Cells.Single(c => c.Position == gate.Position);
        Assert.Equal(DungeonMapMark.Door, closed.Mark); Assert.False(closed.Visible);
        world.Chunks.Edit(gate.Position, Material.Door);
        var opened = DungeonMap.Build(world).Cells.Single(c => c.Position == gate.Position);
        Assert.Equal(closed, opened);
    }
}
