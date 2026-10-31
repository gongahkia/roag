using Roag.Core.Dungeons;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>Semantic overlay; chunks remain the terrain/collision authority.</summary>
    public char? DungeonTerrainAt(WorldPosition p) => Plan.Contains(p) && State.Dungeon?.Terrain.TryGetValue(DungeonRunState.CellKey(p), out var glyph) == true
        && Chunks.At(p) == Material.Floor ? glyph : null;

    public bool DungeonMistBlocks(WorldPosition p) => DungeonTerrainAt(p) == 'm'
        && State.Tactics?.RoomFeatures.MistCleared(p, State.Tactics.Queue.Clock) != true;

    internal static bool SnapshotDungeonMistBlocks(WorldState world, ChunkCache chunks, WorldPosition p)
        => SnapshotDungeonTerrainAt(world, chunks, p) == 'm'
            && world.Tactics?.RoomFeatures.MistCleared(p, world.Tactics.Queue.Clock) != true;

    internal static char? SnapshotDungeonTerrainAt(WorldState world, ChunkCache chunks, WorldPosition p)
        => world.Dungeon?.Terrain.TryGetValue(DungeonRunState.CellKey(p), out var glyph) == true && chunks.At(p) == Material.Floor ? glyph : null;
}
