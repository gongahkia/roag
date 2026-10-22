using System.Text.Json;
using Microsoft.Xna.Framework;
using Vagabonds.Client;
using Vagabonds.Core.World;
using Vagabonds.Graphics;
using Xunit;

namespace Vagabonds.Tests.Client;

public sealed class WorldPointerTests
{
    private static MapCamera Camera(WorldSession session) => new()
    { Center = new(session.State.Position.X + .5f, session.State.Position.Y + .5f), Viewport = new(10, 20, 480, 480) };
    private static Point Screen(MapCamera camera, WorldPosition cell) => camera.WorldToScreen(new(cell.X + .5f, cell.Y + .5f)).ToPoint();

    [Fact]
    public void HoverPlansTheClickRouteWithoutAdvancingTimeOrChangingState()
    {
        var session = WorldSession.Create(42); var origin = session.State.Position; var target = origin.Offset(3, 0);
        for (int x = 0; x <= 3; x++) session.Chunks.Edit(origin.Offset(x, 0), Material.Floor);
        session.Reveal(); var camera = Camera(session); var pointer = new WorldPointerPreview();
        string before = JsonSerializer.Serialize(session.State);
        pointer.Update(session, camera, Screen(camera, target));
        Assert.Equal(target, pointer.Cell); Assert.Equal(session.Route(target), pointer.Path); Assert.Equal(3, pointer.Path.Count);
        Assert.Equal(before, JsonSerializer.Serialize(session.State)); Assert.True(session.Paused);
        var path = pointer.Path; pointer.Update(session, camera, Screen(camera, target)); Assert.Same(path, pointer.Path);
    }

    [Fact]
    public void UnknownTilesCanBeHighlightedWithoutRevealingOrRoutingThroughThem()
    {
        var session = WorldSession.Create(42); var target = session.State.Position.Offset(3, 0);
        foreach (var delta in session.State.Chunks) delta.Discovered.Clear();
        session.Chunks.Delta(session.State.Position.Chunk).Discovered.Add(session.State.Position.LocalIndex);
        var camera = Camera(session); var pointer = new WorldPointerPreview(); string before = JsonSerializer.Serialize(session.State);
        pointer.Update(session, camera, Screen(camera, target));
        Assert.Equal(target, pointer.Cell); Assert.Empty(pointer.Path); Assert.False(session.Chunks.Seen(target));
        Assert.Equal(before, JsonSerializer.Serialize(session.State));
    }

    [Fact]
    public void TerrainAndDiscoveryChangesInvalidateThePreview()
    {
        var session = WorldSession.Create(42); var origin = session.State.Position; var target = origin.Offset(2, 0);
        for (int x = 0; x <= 2; x++) session.Chunks.Edit(origin.Offset(x, 0), Material.Floor);
        foreach (var delta in session.State.Chunks) delta.Discovered.Clear();
        session.Chunks.Delta(origin.Chunk).Discovered.Add(origin.LocalIndex);
        var camera = Camera(session); var pointer = new WorldPointerPreview(); var screen = Screen(camera, target);
        pointer.Update(session, camera, screen); Assert.Empty(pointer.Path);
        session.Reveal(); pointer.Update(session, camera, screen); Assert.NotEmpty(pointer.Path);
        session.Chunks.Edit(target, Material.Wall); pointer.Update(session, camera, screen); Assert.Empty(pointer.Path);
        session.Chunks.Edit(target, Material.Floor); pointer.Update(session, camera, screen); Assert.Equal(2, pointer.Path.Count);
        session.Submit(new(WorldCommandKind.Move, origin.Offset(1, 0))); pointer.Update(session, camera, screen); Assert.Single(pointer.Path);
    }

    [Fact]
    public void PointerRespectsZoomViewportMenusAndTools()
    {
        var session = WorldSession.Create(42); var origin = session.State.Position; var target = origin.Offset(2, 0);
        for (int x = 0; x <= 2; x++) session.Chunks.Edit(origin.Offset(x, 0), Material.Floor);
        session.Reveal(); var camera = Camera(session); camera.ZoomAt(camera.Viewport.Center.ToVector2(), 1);
        var pointer = new WorldPointerPreview(); var screen = Screen(camera, target);
        pointer.Update(session, camera, screen); Assert.Equal(target, pointer.Cell); Assert.NotEmpty(pointer.Path);
        pointer.Update(session, camera, screen, navigationEnabled: false); Assert.Equal(target, pointer.Cell); Assert.Empty(pointer.Path);
        pointer.Update(session, camera, screen, enabled: false); Assert.Null(pointer.Cell); Assert.Empty(pointer.Path);
        pointer.Update(session, camera, new(0, 0)); Assert.Null(pointer.Cell);
        pointer.Update(session, camera, screen); Assert.NotEmpty(pointer.Path);
    }
}
