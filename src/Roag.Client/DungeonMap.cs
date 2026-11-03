using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal enum DungeonMapMark { Ground, Wall, Water, Door, Stairs }
internal sealed record DungeonMapCell(WorldPosition Position, DungeonMapMark Mark, bool Visible);
internal sealed record DungeonMap(WorldPosition Origin, int Width, int Height, WorldPosition Player, DungeonMapCell[] Cells)
{
    /// <summary>Explored terrain only. Never includes actors, loot or offscreen gate receipts.</summary>
    internal static DungeonMap Build(WorldSession session)
    {
        var floor = session.State.Dungeon?.Floors.Single(f => f.Contains(session.State.Position));
        // World campaigns show a local explored region rather than generating
        // or disclosing an entire outdoor world merely to draw its map.
        int width = floor?.Width ?? 128, height = floor?.Height ?? 96;
        var origin = floor?.Origin ?? new WorldPosition(
            Math.Clamp(session.State.Position.X - width / 2, 0, WorldPlan.Size - width),
            Math.Clamp(session.State.Position.Y - height / 2, 0, WorldPlan.Size - height), session.State.Position.Floor);
        var tactics = session.State.Tactics!;
        var doors = tactics.Doors.Nodes.Select(n => n.Position).Concat(tactics.Gates.Nodes.Select(n => n.Position))
            .Concat(tactics.Quests.Nodes.SelectMany(q => q.Nodes.Where(n => n.Kind == Roag.Core.Dungeons.DungeonBranchNodeKind.Gate).Select(n => n.Position)))
            .ToHashSet();
        var fragile = tactics.OptionalTerrain.Nodes.Where(n => n.Kind == OptionalTerrainKind.FragileCrossing).Select(n => n.Position).ToHashSet();
        var exits = (session.State.Dungeon?.Floors ?? []).Where(f => f.Boss > 0 && f.Stage < 4).Select(f => f.Exit).ToHashSet();
        var cells = new List<DungeonMapCell>();
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            var p = origin.Offset(x, y);
            if (!session.Chunks.Seen(p)) continue;
            bool visible = session.Visible.Contains(p);
            var material = session.Chunks.At(p);
            // Remembered doors/exits use identity-only marks. A collapsing bank
            // cannot disclose its current offscreen closure through water color.
            var mark = doors.Contains(p) ? DungeonMapMark.Door : exits.Contains(p) || material is Material.StairsDown or Material.StairsUp ? DungeonMapMark.Stairs
                : material is Material.Wall or Material.Rock or Material.Pillar ? DungeonMapMark.Wall
                : material == Material.Water && (visible || !fragile.Contains(p)) ? DungeonMapMark.Water : DungeonMapMark.Ground;
            cells.Add(new(p, mark, visible));
        }
        return new(origin, width, height, session.State.Position, cells.ToArray());
    }
}
