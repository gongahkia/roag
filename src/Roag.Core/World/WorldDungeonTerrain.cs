using Roag.Core.Dungeons;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>Semantic overlay; chunks remain the terrain/collision authority.</summary>
    public char? DungeonTerrainAt(WorldPosition p) => Plan.Contains(p) && State.Dungeon?.Terrain.TryGetValue(DungeonRunState.CellKey(p), out var glyph) == true
        && Chunks.At(p) == Material.Floor ? glyph : null;

    internal static char? SnapshotDungeonTerrainAt(WorldState world, ChunkCache chunks, WorldPosition p)
        => world.Dungeon?.Terrain.TryGetValue(DungeonRunState.CellKey(p), out var glyph) == true && chunks.At(p) == Material.Floor ? glyph : null;
}
