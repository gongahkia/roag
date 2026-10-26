using System.Text.Json;
using Microsoft.Xna.Framework;
using Roag.Client;
using Roag.Core.World;
using Roag.Graphics;
using Xunit;

namespace Roag.Tests.Client;

public sealed class WorldPointerTests
{
    private static MapCamera Camera(WorldSession session) => new()
    { Center = new(session.State.Position.X + .5f, session.State.Position.Y + .5f), Viewport = new(10, 20, 480, 480) };
    private static Point Screen(MapCamera camera, WorldPosition cell) => camera.WorldToScreen(new(cell.X + .5f, cell.Y + .5f)).ToPoint();

    [Fact]
    public void HoverHighlightsWithoutSearchingForAConnectedRouteOrChangingState()
    {
        var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var target = session.State.Position.Offset(3, 0);
        foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) session.Chunks.Edit(target.Offset(x, y), Material.Wall);
        var camera = Camera(session); var pointer = new WorldPointerPreview(); string before = JsonSerializer.Serialize(session.State);
        pointer.Update(session, camera, Screen(camera, target));
        Assert.Equal(target, pointer.Cell); Assert.Empty(pointer.BlockReason);
        Assert.Empty(WorldInspection.Inspect(session, target).Reason);
        Assert.False(WorldMouseNavigation.Plan(session, target).Reachable);
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }

    [Fact]
    public void SweepingHoverAndTooltipsUsesBoundedAllocationsWithoutRouteSearches()
    {
        var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var camera = Camera(session); var pointer = new WorldPointerPreview();
        var targets = Enumerable.Range(-4, 9).Select(x => session.State.Position.Offset(x, 3)).ToArray();
        foreach (var target in targets) { pointer.Update(session, camera, Screen(camera, target)); WorldInspection.Inspect(session, target); }
        string before = JsonSerializer.Serialize(session.State);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            var target = targets[i % targets.Length];
            pointer.Update(session, camera, Screen(camera, target)); WorldInspection.Inspect(session, target);
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Assert.True(allocated < 512_000, $"Hover allocated {allocated} bytes; it must not build route searches.");
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }

    [Fact]
    public void UnknownTilesAreHighlightedWithoutRevealingTheirTerrain()
    {
        var session = WorldSession.Create(42); var target = session.State.Position.Offset(3, 0);
        foreach (var delta in session.State.Chunks) delta.Discovered.Clear();
        session.Chunks.Delta(session.State.Position.Chunk).Discovered.Add(session.State.Position.LocalIndex);
        var camera = Camera(session); var pointer = new WorldPointerPreview(); string before = JsonSerializer.Serialize(session.State);
        pointer.Update(session, camera, Screen(camera, target));
        Assert.Equal(target, pointer.Cell); Assert.NotEmpty(pointer.BlockReason); Assert.False(session.Chunks.Seen(target));
        Assert.Equal("unknown", WorldInspection.Inspect(session, target).Kind);
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }

    [Fact]
    public void StationaryHoverReflectsTerrainDiscoveryAndOccupantChanges()
    {
        var session = Roag.Core.Tests.World.WorldCollisionTests.Arena(); var target = session.State.Position.Offset(2, 0);
        var camera = Camera(session); var pointer = new WorldPointerPreview(); var screen = Screen(camera, target);
        pointer.Update(session, camera, screen); Assert.Empty(pointer.BlockReason);
        session.Chunks.Edit(target, Material.Wall); pointer.Update(session, camera, screen); Assert.Contains("dig", pointer.BlockReason);
        session.Chunks.Edit(target, Material.Floor); pointer.Update(session, camera, screen); Assert.Empty(pointer.BlockReason);
        var actor = session.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); actor.Position = target;
        pointer.Update(session, camera, screen); Assert.Equal(actor.Name, WorldInspection.Inspect(session, target).Title); Assert.Empty(pointer.BlockReason);
        foreach (var delta in session.State.Chunks) delta.Discovered.Clear();
        pointer.Update(session, camera, screen); Assert.NotEmpty(pointer.BlockReason);
    }

    [Fact]
    public void PointerRespectsZoomViewportAndMenus()
    {
        var session = WorldSession.Create(42); var target = session.State.Position.Offset(2, 0);
        var camera = Camera(session); camera.ZoomAt(camera.Viewport.Center.ToVector2(), 1);
        var pointer = new WorldPointerPreview(); var screen = Screen(camera, target);
        pointer.Update(session, camera, screen); Assert.Equal(target, pointer.Cell);
        pointer.Update(session, camera, screen, enabled: false); Assert.Null(pointer.Cell); Assert.Empty(pointer.BlockReason);
        pointer.Update(session, camera, new(0, 0)); Assert.Null(pointer.Cell);
        pointer.Update(session, camera, screen); Assert.Equal(target, pointer.Cell);
    }
}
