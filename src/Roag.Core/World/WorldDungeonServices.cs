using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    // Preserve original draw positions: a deferred ration cache leaves its slot empty.
    private static readonly DungeonServiceKind?[] DungeonServiceOrder = [DungeonServiceKind.OfferingFont, null,
        DungeonServiceKind.CleanseFlask, DungeonServiceKind.EscapeScroll, DungeonServiceKind.ReturnToken,
        DungeonServiceKind.CharmWell, DungeonServiceKind.AttunementForge, DungeonServiceKind.TravellingTrader,
        DungeonServiceKind.Memorial, DungeonServiceKind.Camp, DungeonServiceKind.ArchiveContract];

    private void PopulateDungeonServices(DungeonLayout layout, WorldPosition origin, TacticalCombat combat)
    {
        var floor = State.Dungeon!.Floors.Single(f => f.Origin.Floor == origin.Floor);
        string sourceFloor = DungeonCatalog.Default.Biomes.Single(b => b.Id == layout.Biome).FloorId
            + "/stage/" + floor.Stage + "/depth/" + (origin.Floor % 2 == 1 ? 1 : 2);
        int seed = RoagRandom.Derive(DungeonSeed, sourceFloor + ":interactions");
        bool Available(WorldPosition p) => CanEnter(p) && Chunks.At(p) == Material.Floor && DungeonTerrainAt(p) == null
            && p.Distance(floor.Entry) >= 4 && p.Distance(floor.Exit) >= 4
            && !combat.State.Services.Nodes.Any(n => n.Position == p)
            && !combat.State.OptionalTerrain.Nodes.Any(n => n.Position == p);
        var candidates = new List<(int Room, WorldPosition Cell, int Score)>();
        foreach (var room in layout.Rooms.Where(r => r.Role is not ("start" or "elite")))
            for (int y = 1; y < room.Shape.Height - 1; y++) for (int x = 1; x < room.Shape.Width - 1; x++)
            {
                var cell = origin.Offset(room.X + x, room.Y + y);
                if (Available(cell)) candidates.Add((room.Index, cell, RoagRandom.Derive(seed, $"{room.X + x},{room.Y + y}")));
            }
        candidates.Sort((a, b) => a.Score != b.Score ? a.Score.CompareTo(b.Score)
            : a.Cell.Y != b.Cell.Y ? a.Cell.Y.CompareTo(b.Cell.Y) : a.Cell.X.CompareTo(b.Cell.X));
        var used = new HashSet<int>();
        for (int slot = 0; slot < 2; slot++)
        {
            var kind = DungeonServiceOrder[(seed % DungeonServiceOrder.Length + slot) % DungeonServiceOrder.Length];
            if (kind == null) continue;
            foreach (var candidate in candidates)
            {
                if (used.Contains(candidate.Room) || !Available(candidate.Cell)
                    || kind == DungeonServiceKind.ArchiveContract && !combat.HasDungeonContractTargets(RoomAt(candidate.Cell)!.Value)) continue;
                combat.SpawnDungeonService(kind.Value, $"floor:{origin.Floor}/service:{kind}/{candidate.Cell.X},{candidate.Cell.Y}", candidate.Cell);
                used.Add(candidate.Room); break;
            }
        }
    }
}
