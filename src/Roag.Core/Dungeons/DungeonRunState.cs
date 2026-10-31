using Roag.Core.World;
using Roag.Core.Tactics;

namespace Roag.Core.Dungeons;

public sealed record DungeonRoomSite(int Index, string Definition, WorldPosition Origin, int Width, int Height, int Turns, bool Mirror)
{
    public bool Contains(WorldPosition p) => p.Floor == Origin.Floor && p.X >= Origin.X && p.X < Origin.X + Width
        && p.Y >= Origin.Y && p.Y < Origin.Y + Height;
}
public sealed record DungeonFloorState(int Stage, string Biome, WorldPosition Origin, int Width, int Height,
    DungeonRoomSite[] Rooms, WorldPosition Entry, WorldPosition Exit, long Boss = 0)
{
    public bool Contains(WorldPosition p) => p.Floor == Origin.Floor && p.X >= Origin.X && p.X < Origin.X + Width
        && p.Y >= Origin.Y && p.Y < Origin.Y + Height;
}

/// <summary>Bounded solo expedition metadata. Chunks and persistent actors own terrain and combat state.</summary>
public sealed class DungeonRunState
{
    public int Stage { get; set; } = 1;
    public List<string> RouteHistory { get; set; } = [];
    public List<DungeonFloorState> Floors { get; set; } = [];
    public HashSet<long> BossExitReceipts { get; set; } = [];
    public Dictionary<long, char> Terrain { get; set; } = [];
    public bool Victory { get; set; }

    public bool Contains(WorldPosition p) => Floors.Any(f => f.Contains(p));
    public static long CellKey(WorldPosition p) => (long)p.Floor * WorldPlan.Size * WorldPlan.Size + (long)p.Y * WorldPlan.Size + p.X;
    public WorldRoomId? RoomAt(WorldPosition p)
    {
        var floor = Floors.FirstOrDefault(f => f.Origin.Floor == p.Floor);
        var room = floor?.Rooms.FirstOrDefault(r => r.Contains(p));
        return room == null ? null : new(RoomArea.Dungeon, 0, p.Floor, room.Index + 1);
    }

    internal void Validate(WorldPlan plan, WorldState world, IReadOnlyDictionary<long, Actor> actors)
    {
        if (Stage is < 1 or > 3 || RouteHistory == null || RouteHistory.Count != Stage - 1 || Floors == null || Floors.Count is < 1 or > 7
            || Floors.Any(f => f == null) || Floors.Select(f => f.Origin.Floor).Distinct().Count() != Floors.Count
            || Terrain == null || Terrain.Count > 100000 || BossExitReceipts == null
            || world.Tactics is not { DrivesWorldClock: true } || world.Party.Count != 1 || world.RoagCombat || world.RoagCombatState.Length != 0)
            throw new InvalidDataException("Invalid native solo expedition state.");
        foreach (var floor in Floors)
        {
            if (floor.Stage is < 1 or > 3 || !DungeonCatalog.Default.Biomes.Any(b => b.Id == floor.Biome)
                || floor.Width != 140 || floor.Height != 90 || !plan.Contains(floor.Origin)
                || !plan.Contains(floor.Origin.Offset(floor.Width - 1, floor.Height - 1)) || !floor.Contains(floor.Entry) || !floor.Contains(floor.Exit)
                || floor.Rooms == null || floor.Rooms.Length is < 8 or > 12 || floor.Rooms.Any(r => r == null) || floor.Rooms.Select(r => r.Index).Distinct().Count() != floor.Rooms.Length
                || floor.Boss < 0 || floor.Boss > 0 && (!actors.TryGetValue(floor.Boss, out var boss) || boss.Tactics?.Enemy == null))
                throw new InvalidDataException("Invalid native dungeon footprint or anchors.");
            foreach (var room in floor.Rooms)
            {
                if (room == null || room.Index < 0 || !DungeonCatalog.Default.Rooms.TryGetValue(room.Definition, out var definition)
                    || !definition.Rotations.Contains(room.Turns) || room.Mirror && !definition.Mirror
                    || !floor.Contains(room.Origin) || !floor.Contains(room.Origin.Offset(room.Width - 1, room.Height - 1)))
                    throw new InvalidDataException("Invalid authored dungeon room identity.");
                var shape = definition.Transform(room.Turns, room.Mirror);
                if (shape.Width != room.Width || shape.Height != room.Height
                    || floor.Rooms.Any(other => other.Index != room.Index && room.Origin.X < other.Origin.X + other.Width
                        && other.Origin.X < room.Origin.X + room.Width && room.Origin.Y < other.Origin.Y + other.Height
                        && other.Origin.Y < room.Origin.Y + room.Height))
                    throw new InvalidDataException("Clipped or overlapping authored dungeon rooms.");
            }
        }
        foreach (var (key, glyph) in Terrain)
        {
            var position = new WorldPosition((int)(key % WorldPlan.Size), (int)(key / WorldPlan.Size % WorldPlan.Size), (int)(key / WorldPlan.Size / WorldPlan.Size));
            if (key < 0 || key >= (long)WorldPlan.Size * WorldPlan.Size * WorldPlan.Floors || glyph is not ('b' or 'g' or 'i' or 'm' or 'c') || !Contains(position))
                throw new InvalidDataException("Invalid native dungeon terrain overlay.");
        }
        if (world.Tactics.Participants.Any(id => !actors.TryGetValue(id, out var actor) || !Contains(actor.Position))
            || BossExitReceipts.Any(id => !Floors.Any(f => f.Boss == id))
            || Victory && !Floors.Any(f => f.Boss > 0 && actors[f.Boss] is { Life: LifeState.Dead, Tactics.DefinitionId: "core:enemy/hollow_regent" }))
            throw new InvalidDataException("Invalid dungeon participant, reward receipt or victory.");
    }
}
