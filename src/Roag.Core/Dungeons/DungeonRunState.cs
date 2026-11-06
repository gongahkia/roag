using Roag.Core.World;
using Roag.Core.Tactics;
using System.Text.Json.Serialization;

namespace Roag.Core.Dungeons;

public sealed record DungeonRoomSite(int Index, string Definition, WorldPosition Origin, int Width, int Height, int Turns, bool Mirror,
    string Role = "", bool Critical = false)
{
    public bool Contains(WorldPosition p) => p.Floor == Origin.Floor && p.X >= Origin.X && p.X < Origin.X + Width
        && p.Y >= Origin.Y && p.Y < Origin.Y + Height;
}
public sealed record DungeonFloorState(int Stage, string Biome, WorldPosition Origin, int Width, int Height,
    DungeonRoomSite[] Rooms, WorldPosition Entry, WorldPosition Exit, long Boss = 0,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int Branch = 0)
{
    public bool Contains(WorldPosition p) => p.Floor == Origin.Floor && p.X >= Origin.X && p.X < Origin.X + Width
        && p.Y >= Origin.Y && p.Y < Origin.Y + Height;

    public WorldRoomId? RoomAt(WorldPosition position, int place = 0)
    {
        if (!Contains(position)) return null;
        var room = Rooms.FirstOrDefault(r => r.Contains(position));
        return room == null ? null : new(RoomArea.Dungeon, place, position.Floor, Branch * 16 + room.Index + 1);
    }
}

public sealed record DungeonRecruit(long Actor, ClassKind Class);

/// <summary>Bounded expedition metadata. Chunks and persistent actors own terrain and combat state.</summary>
public sealed class DungeonRunState
{
    public const string FinaleBiome = "core:biome/final_sanctum";
    public int Stage { get; set; } = 1;
    public List<string> RouteHistory { get; set; } = [];
    public List<DungeonFloorState> Floors { get; set; } = [];
    public HashSet<long> BossExitReceipts { get; set; } = [];
    public HashSet<long> PendingEliteRewards { get; set; } = [];
    public Dictionary<long, char> Terrain { get; set; } = [];
    public bool Victory { get; set; }
    public List<DungeonRecruit> Recruits { get; set; } = [];
    /// <summary>Follower to group leader. Missing members take individual commands; groups never form chains.</summary>
    public Dictionary<long, long> Following { get; set; } = [];

    public bool Contains(WorldPosition p)
    {
        for (int i = 0; i < Floors.Count; i++) if (Floors[i].Contains(p)) return true;
        return false;
    }
    public static long CellKey(WorldPosition p) => (long)p.Floor * WorldPlan.Size * WorldPlan.Size + (long)p.Y * WorldPlan.Size + p.X;
    public WorldRoomId? RoomAt(WorldPosition p, int place = 0)
    {
        var floor = Floors.FirstOrDefault(f => f.Contains(p));
        return floor?.RoomAt(p, place);
    }

    internal void Validate(WorldPlan plan, WorldState world, IReadOnlyDictionary<long, Actor> actors)
    {
        if (Stage is < 1 or > 4 || RouteHistory == null || RouteHistory.Count != Stage - 1 || Floors == null || Floors.Count != (Stage == 4 ? 7 : Stage * 2)
            || Floors.Any(f => f == null) || Floors.Select(f => f.Origin.Floor).Distinct().Count() != Floors.Count
            || Terrain == null || Terrain.Count > 100000 || BossExitReceipts == null || PendingEliteRewards == null
            || world.Tactics is not { DrivesWorldClock: true } || world.Party.Count < 1
            || world.Party.Count(id => actors.TryGetValue(id, out var member) && member.Life != LifeState.Dead) > 4
            || world.Party.Any(id => !actors.TryGetValue(id, out var member) || member.Tactics?.Build == null || !world.Tactics.Participants.Contains(id))
            || world.RoagCombat || world.RoagCombatState.Length != 0)
            throw new InvalidDataException("Invalid native expedition state.");
        if (Recruits == null || Recruits.Count > 8 || Recruits.Any(r => r == null)
            || Recruits.Select(r => r.Actor).Distinct().Count() != Recruits.Count
            || Recruits.Any(r => !Enum.IsDefined(r.Class) || !actors.TryGetValue(r.Actor, out var actor)
                || actor.Kind != ActorKind.Person || !Contains(actor.Position)
                || actor.Recruited != world.Party.Contains(actor.Id)
                || (actor.Recruited ? actor.Tactics?.Build == null || actor.Tactics.Resident != null
                    : actor.Tactics != null && actor.Tactics.Resident == null)
                || actor.Tactics?.Build is { } build && build.Class != r.Class))
            throw new InvalidDataException("Invalid fixed-class dungeon recruit.");
        if (Following == null || Following.Any(pair => pair.Key == pair.Value || !world.Party.Contains(pair.Key)
            || !world.Party.Contains(pair.Value) || Following.ContainsKey(pair.Value)
            || !actors.TryGetValue(pair.Key, out var follower) || follower.Life == LifeState.Dead
            || !actors.TryGetValue(pair.Value, out var leader) || leader.Life == LifeState.Dead))
            throw new InvalidDataException("Invalid dungeon following group.");
        for (int i = 0; i < RouteHistory.Count; i++)
            if (!DungeonRoutes.ForStage(i + 1, RouteHistory.Take(i).ToArray()).Any(r => r.Id == RouteHistory[i]))
                throw new InvalidDataException("Invalid selected biome route.");
        foreach (var floor in Floors)
        {
            bool finale = floor.Origin.Floor == 7;
            int expectedStage = finale ? 4 : (floor.Origin.Floor + 1) / 2;
            string expectedBiome = finale ? FinaleBiome : expectedStage == 1 ? DungeonCatalog.Default.Biomes[0].Id
                : RouteHistory.ElementAtOrDefault(expectedStage - 2)?.Replace("core:route/", "core:biome/", StringComparison.Ordinal) ?? "";
            if (floor.Branch != 0 || floor.Origin.Floor is < 1 or > 7 || floor.Stage != expectedStage || floor.Biome != expectedBiome
                || floor.Width != (finale ? 23 : 140) || floor.Height != (finale ? 17 : 90) || !plan.Contains(floor.Origin)
                || !plan.Contains(floor.Origin.Offset(floor.Width - 1, floor.Height - 1)) || !floor.Contains(floor.Entry) || !floor.Contains(floor.Exit)
                || floor.Rooms == null || (finale ? floor.Rooms.Length != 1 : floor.Rooms.Length is < 8 or > 12)
                || floor.Rooms.Any(r => r == null) || floor.Rooms.Select(r => r.Index).Distinct().Count() != floor.Rooms.Length
                || floor.Boss < 0 || (floor.Boss > 0) != (finale || floor.Origin.Floor % 2 == 0)
                || floor.Boss > 0 && (!actors.TryGetValue(floor.Boss, out var boss) || boss.Tactics?.Enemy == null
                    || boss.Tactics.DefinitionId != (finale ? "core:enemy/hollow_regent" : DungeonCatalog.Default.Biomes.Single(b => b.Id == expectedBiome).EliteId)))
                throw new InvalidDataException("Invalid native dungeon footprint or anchors.");
            if (finale)
            {
                var map = DungeonCatalog.Default.Finale; var room = floor.Rooms[0];
                if (room.Definition != map.Id || room.Origin != floor.Origin || room.Width != map.Width || room.Height != map.Height
                    || room.Turns != 0 || room.Mirror || floor.Entry != floor.Origin.Offset(map.StartX - 1, map.StartY - 1)
                    || floor.Exit != floor.Origin.Offset(map.BossX - 1, map.BossY - 1))
                    throw new InvalidDataException("Invalid authored final sanctum.");
                continue;
            }
            foreach (var room in floor.Rooms)
            {
                if (room == null || room.Index < 0 || !DungeonCatalog.Default.Rooms.TryGetValue(room.Definition, out var definition)
                    || !definition.Rotations.Contains(room.Turns) || room.Mirror && !definition.Mirror
                    || !definition.Roles.Contains(room.Role)
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
            if (key < 0 || key >= (long)WorldPlan.Size * WorldPlan.Size * plan.FloorCount || glyph is not ('b' or 'g' or 'i' or 'm' or 'c') || !Contains(position))
                throw new InvalidDataException("Invalid native dungeon terrain overlay.");
        }
        if (world.Tactics.Participants.Any(id => !actors.TryGetValue(id, out var actor) || !Contains(actor.Position))
            || Floors.Select(f => f.Origin.Floor).Order().Where((floor, index) => floor != index + 1).Any()
            || BossExitReceipts.Any(id => id <= 0 || !Floors.Any(f => f.Boss == id) || !actors.TryGetValue(id, out var boss) || boss.Life != LifeState.Dead)
            || PendingEliteRewards.Any(id => !BossExitReceipts.Contains(id) || !Floors.Any(f => f.Boss == id && f.Stage < 4)
                || world.Tactics.Chests.Chests.Any(c => c.Marker == "elite:stage:" + Floors.Single(f => f.Boss == id).Stage))
            || Victory && !Floors.Any(f => f.Boss > 0 && actors[f.Boss] is { Life: LifeState.Dead, Tactics.DefinitionId: "core:enemy/hollow_regent" }))
            throw new InvalidDataException("Invalid dungeon participant, reward receipt or victory.");
    }
}
