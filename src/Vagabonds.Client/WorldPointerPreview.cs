using Microsoft.Xna.Framework;
using Vagabonds.Core.World;
using Vagabonds.Graphics;

namespace Vagabonds.Client;

/// <summary>Presentation-only hover state. Previewing never submits a world command.</summary>
internal sealed class WorldPointerPreview
{
    private WorldSession? _session;
    private WorldPosition _origin;
    private long _navigationRevision, _discoveredCount;
    private bool _navigationEnabled;
    private WorldClickIntent? _intent;
    public WorldPosition? Cell { get; private set; }
    public WorldPosition? NavigationCell { get; private set; }
    public IReadOnlyList<WorldPosition> Path { get; private set; } = [];

    public void Update(WorldSession session, MapCamera camera, Point pointer, bool enabled = true, bool navigationEnabled = true, WorldPosition? interactionCell = null)
    {
        if (!enabled || !camera.Viewport.Contains(pointer)) { Clear(); return; }
        var world = camera.ScreenToWorld(new(pointer.X, pointer.Y));
        var cell = new WorldPosition((int)Math.Floor(world.X), (int)Math.Floor(world.Y), session.State.Position.Floor);
        if (!session.Plan.Contains(cell)) { Clear(); return; }
        long discovered = session.State.Chunks.Sum(c => (long)c.Discovered.Count);
        var target = interactionCell ?? cell;
        var intent = WorldMouseNavigation.Intent(session, target);
        if (intent == _intent && NavigationCell == target && ReferenceEquals(_session, session) && Cell == cell && _origin == session.State.Position &&
            _navigationRevision == session.State.NavigationRevision && _discoveredCount == discovered && _navigationEnabled == navigationEnabled) return;

        _intent = intent; _session = session; _origin = session.State.Position; Cell = cell; NavigationCell = target;
        _navigationRevision = session.State.NavigationRevision; _discoveredCount = discovered; _navigationEnabled = navigationEnabled;
        Path = navigationEnabled ? WorldMouseNavigation.Plan(session, target).Path : [];
    }

    private void Clear() { _session = null; Cell = NavigationCell = null; Path = []; }
}
