using Microsoft.Xna.Framework;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private bool _nativeMapVisible = true;
    private DungeonMap? _nativeMap;
    private (WorldSession Session, WorldPosition Position, long Event, long Observer)? _nativeMapStamp;

    private void ToggleDungeonMap()
    {
        _nativeMapVisible = !_nativeMapVisible;
        _notice = _nativeMapVisible ? "Map shown / no time" : "Map hidden / no time";
    }

    private Rectangle DungeonMapBounds()
    {
        var floor = _session.State.Dungeon!.Floors.Single(f => f.Contains(_session.State.Position));
        int titleWidth = "MAP / Tab".Length * (_uiFonts?.Advance ?? 8);
        return new(_map.X + 4, _map.Y + 4, Math.Max((int)Math.Ceiling(floor.Width * 1.3f), titleWidth) + 16,
            (int)Math.Ceiling(floor.Height * 1.3f) + 32);
    }

    private void DrawDungeonMap()
    {
        if (!_nativeMapVisible || _session.State.Dungeon == null || _panel.Length > 0 || _session.State.AwaitingContinuation) return;
        var stamp = (_session, _session.State.Position, _session.State.Tactics!.NextEvent, _session.State.Tactics.ControlledActor);
        if (_nativeMap == null || _nativeMapStamp != stamp) { _nativeMap = DungeonMap.Build(_session); _nativeMapStamp = stamp; }
        var map = _nativeMap; var bounds = DungeonMapBounds();
        Fill(bounds, new Color(9, 17, 16) * .94f); Text("MAP / Tab", bounds.X + 8, bounds.Y + 6, Muted);
        const float scale = 1.3f;
        Rectangle Cell(WorldPosition p) => new(bounds.X + 8 + (int)((p.X - map.Origin.X) * scale),
            bounds.Y + 25 + (int)((p.Y - map.Origin.Y) * scale), 2, 2);
        foreach (var cell in map.Cells)
        {
            Color color = cell.Mark switch { DungeonMapMark.Wall => new(139, 164, 139), DungeonMapMark.Water => new(59, 130, 179),
                DungeonMapMark.Door => Gold, DungeonMapMark.Stairs => new(110, 224, 245), _ => new(102, 158, 100) };
            Fill(Cell(cell.Position), color * (cell.Visible ? 1 : .4f));
        }
        var player = Cell(map.Player); Fill(new(player.X - 1, player.Y - 1, 4, 4), Gold);
    }
}
