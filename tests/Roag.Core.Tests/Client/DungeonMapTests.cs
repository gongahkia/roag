using System.Text.Json;
using Roag.Client;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class DungeonMapTests
{
    [Fact]
    public void CampaignMapShowsOnlyExploredLocalTerrainWithoutRevealingTheWorld()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        Assert.Equal(world.Visible.Count, world.State.Chunks.Sum(c => c.Discovered.Count));
        Assert.All(world.Visible, p => Assert.True(
            Math.Pow(p.X - world.State.Position.X, 2) + Math.Pow(p.Y - world.State.Position.Y, 2)
            <= Math.Pow(world.SightRadiusAt(world.State.Position), 2)));
        string before = JsonSerializer.Serialize(world.State);
        var map = DungeonMap.Build(world);
        Assert.NotEmpty(map.Cells); Assert.Equal(128, map.Width); Assert.Equal(96, map.Height);
        Assert.Equal(world.State.Position, map.Player);
        Assert.All(map.Cells, c =>
        {
            Assert.Equal(map.Player.Floor, c.Position.Floor); Assert.True(world.Chunks.Seen(c.Position));
            Assert.Equal(world.Visible.Contains(c.Position), c.Visible);
            Assert.InRange(c.Position.X, map.Origin.X, map.Origin.X + map.Width - 1);
            Assert.InRange(c.Position.Y, map.Origin.Y, map.Origin.Y + map.Height - 1);
        });
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        var sight = world.Visible.ToHashSet();
        world.RevealSurface(0, 0, WorldPlan.Size, WorldPlan.Size);
        Assert.True(sight.SetEquals(world.Visible));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        world.Visible.Clear();
        Assert.All(DungeonMap.Build(world).Cells, c => Assert.False(c.Visible));
    }
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
