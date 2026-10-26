using Microsoft.Xna.Framework;
using Roag.Core.World;
using Roag.Graphics;

namespace Roag.Client;

/// <summary>Presentation-only hover state. Previewing never submits a world command.</summary>
internal sealed class WorldPointerPreview
{
    public WorldPosition? Cell { get; private set; }
    public WorldPosition? NavigationCell { get; private set; }
    public string BlockReason { get; private set; } = "";

    public void Update(WorldSession session, MapCamera camera, Point pointer, bool enabled = true, WorldPosition? interactionCell = null)
    {
        if (!enabled || !camera.Viewport.Contains(pointer)) { Clear(); return; }
        var world = camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var cell = new WorldPosition((int)Math.Floor(world.X), (int)Math.Floor(world.Y), session.State.Position.Floor);
        if (!session.Plan.Contains(cell)) { Clear(); return; }
        var target = interactionCell ?? cell;
        Cell = cell; NavigationCell = target;
        BlockReason = WorldMouseNavigation.HoverBlockReason(session, target);
    }

    private void Clear() { Cell = NavigationCell = null; BlockReason = ""; }
}
