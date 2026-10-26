using System.Text.Json;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class VisibilityTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-visibility-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

    [Fact]
    public void SurfaceViewShowsBehindWallsWithoutAdvancingSimulationOrRevealingDistantPlaces()
    {
        var s = WorldSession.Create(42); var p = s.State.Position;
        for (int y = -3; y <= 3; y++) s.Chunks.Edit(p.Offset(1, y), Material.Wall);
        foreach (var delta in s.State.Chunks) delta.Discovered.Clear();
        long tick = s.State.Tick; uint random = s.State.CombatRandom;
        string actors = JsonSerializer.Serialize(s.State.Actors), inventory = JsonSerializer.Serialize(s.State.Inventory);
        s.RevealSurface(p.X - 3, p.Y - 3, p.X + 4, p.Y + 4);
        Assert.Equal(49, s.Visible.Count); Assert.Contains(p.Offset(2, 0), s.Visible);
        Assert.True(s.Chunks.Seen(p.Offset(2, 0))); Assert.False(s.Chunks.Seen(s.Plan.Towns[1].Center));
        Assert.Equal(tick, s.State.Tick); Assert.Equal(random, s.State.CombatRandom); Assert.Equal(p, s.State.Position);
        Assert.Equal(actors, JsonSerializer.Serialize(s.State.Actors)); Assert.Equal(inventory, JsonSerializer.Serialize(s.State.Inventory));
        string before = JsonSerializer.Serialize(s.State);
        s.RevealSurface(p.X - 3, p.Y - 3, p.X + 4, p.Y + 4); Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }

    [Fact]
    public void MovementAndTerrainEditsRetainTheSurfaceView()
    {
        var s = WorldSession.Create(42); var p = s.State.Position;
        s.Chunks.Edit(p.Offset(-1, 0), Material.Floor);
        s.RevealSurface(p.X - 15, p.Y - 10, p.X + 16, p.Y + 11);
        Assert.True(s.Submit(new(WorldCommandKind.Move, p.Offset(-1, 0))).Success);
        Assert.Contains(p.Offset(15, 0), s.Visible);
        s.Chunks.Edit(p.Offset(1, 0), Material.Wall); s.Reveal();
        Assert.Contains(p.Offset(15, 0), s.Visible); Assert.Equal(31 * 21, s.Visible.Count);
        s.RevealSurface(p.X - 16, p.Y - 10, p.X + 15, p.Y + 11);
        Assert.DoesNotContain(p.Offset(15, 0), s.Visible); Assert.True(s.Chunks.Seen(p.Offset(15, 0)));
    }

    [Fact]
    public void DungeonFogSurvivesStairsAndCannotBeDisabledBySurfaceObservation()
    {
        var s = WorldSession.Create(42); var stair = s.Plan.Sites[0].Center.Offset(2, 0);
        s.State.Position = stair.Offset(1, 0);
        s.RevealSurface(stair.X - 20, stair.Y - 20, stair.X + 21, stair.Y + 21);
        Assert.True(s.Submit(new(WorldCommandKind.Interact, stair)).Success);
        var p = s.State.Position; Assert.Equal(1, p.Floor);
        for (int y = -3; y <= 3; y++) for (int x = -3; x <= 3; x++) s.Chunks.Edit(p.Offset(x, y), Material.Floor);
        for (int y = -3; y <= 3; y++) s.Chunks.Edit(p.Offset(1, y), Material.Wall);
        foreach (var delta in s.State.Chunks.Where(c => c.Key.Floor == 1)) delta.Discovered.Clear();
        s.Reveal(); Assert.Contains(p.Offset(1, 0), s.Visible);
        Assert.DoesNotContain(p.Offset(2, 0), s.Visible); Assert.False(s.Chunks.Seen(p.Offset(2, 0)));
        string before = JsonSerializer.Serialize(s.State);
        s.RevealSurface(p.X - 20, p.Y - 20, p.X + 21, p.Y + 21);
        Assert.Equal(before, JsonSerializer.Serialize(s.State)); Assert.Empty(s.Route(p.Offset(2, 0)));
        // Restore the generated stair after creating the visibility fixture and return normally.
        s.Chunks.Edit(p, Material.StairsUp);
        Assert.True(s.Submit(new(WorldCommandKind.Interact, p)).Success); Assert.Equal(0, s.State.Position.Floor);
        s.RevealSurface(stair.X - 20, stair.Y - 20, stair.X + 21, stair.Y + 21);
        Assert.Equal(41 * 41, s.Visible.Count); Assert.All(s.Visible, cell => Assert.Equal(0, cell.Floor));
    }

    [Fact]
    public void SurfaceDiscoveryPersistsButDoesNotRevealUndergroundOrTheWholeWorld()
    {
        var s = WorldSession.Create(42); var p = s.State.Position; var observed = p.Offset(20, 0);
        s.RevealSurface(p.X - 21, p.Y - 12, p.X + 22, p.Y + 13);
        string slot = Path.Combine(_root, "campaign.json"); s.Save(slot); var loaded = WorldSession.Load(slot);
        Assert.True(loaded.Chunks.Seen(observed)); Assert.False(loaded.Chunks.Seen(observed with { Floor = 1 }));
        Assert.False(loaded.Chunks.Seen(s.Plan.Towns[1].Center)); Assert.True(loaded.Paused);
        loaded.RevealSurface(p.X - 21, p.Y - 12, p.X + 22, p.Y + 13);
        Assert.True(s.Visible.SetEquals(loaded.Visible));
    }

    [Fact]
    public void SurfaceBoundsAreClampedAtWorldEdges()
    {
        var s = WorldSession.Create(42); s.RevealSurface(-2, -2, 3, 3);
        Assert.Equal(9, s.Visible.Count); Assert.All(s.Visible, cell => Assert.True(s.Plan.Contains(cell)));
        Assert.Throws<ArgumentException>(() => s.RevealSurface(5, 0, 2, 10));
        s.RevealSurface(WorldPlan.Size - 2, WorldPlan.Size - 2, WorldPlan.Size + 4, WorldPlan.Size + 4);
        Assert.Equal(4, s.Visible.Count); Assert.All(s.Visible, cell => Assert.True(s.Plan.Contains(cell)));
    }
}
