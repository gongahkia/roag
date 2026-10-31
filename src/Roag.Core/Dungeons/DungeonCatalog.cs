using System.Collections.Frozen;
using System.Text.Json;
using Roag.Core.Tactics;

namespace Roag.Core.Dungeons;

public sealed record RoomPoint(int X, int Y);
public sealed record RoomSocket(int X, int Y, string Facing, int Width);
public sealed record RoomStructure(int X, int Y, string Kind);
public sealed record DungeonRoom(string Id, string[] Roles, string[] Rows, RoomSocket[] Sockets,
    Dictionary<string, RoomPoint[]> Markers, RoomPoint[] Editable, int[] Rotations, bool Mirror,
    RoomStructure[]? Structures = null)
{
    public int Width => Rows[0].Length;
    public int Height => Rows.Length;

    public PlacedRoom Transform(int turns, bool mirror)
    {
        if (!Rotations.Contains(turns) || mirror && !Mirror) throw new ArgumentException("Room transformation is not permitted.");
        (int X, int Y, string? Facing) Point(int x, int y, string? facing = null)
        {
            if (mirror)
            {
                x = Width + 1 - x;
                facing = facing switch { "E" => "W", "W" => "E", _ => facing };
            }
            int w = Width, h = Height;
            for (int t = 0; t < turns; t++)
            {
                (x, y) = (h + 1 - y, x); (w, h) = (h, w);
                facing = facing switch { "N" => "E", "E" => "S", "S" => "W", "W" => "N", _ => facing };
            }
            return (x, y, facing);
        }
        int width = turns % 2 == 0 ? Width : Height, height = turns % 2 == 0 ? Height : Width;
        var grid = Enumerable.Range(0, height).Select(_ => new char[width]).ToArray();
        for (int y = 1; y <= Height; y++) for (int x = 1; x <= Width; x++)
        {
            var p = Point(x, y); grid[p.Y - 1][p.X - 1] = Rows[y - 1][x - 1];
        }
        RoomPoint Mark(RoomPoint p) { var next = Point(p.X, p.Y); return new(next.X, next.Y); }
        return new(Id, Roles.ToArray(), grid.Select(r => new string(r)).ToArray(), Sockets.Select(s =>
        {
            var p = Point(s.X, s.Y, s.Facing); return new RoomSocket(p.X, p.Y, p.Facing!, s.Width);
        }).ToArray(), Markers.ToDictionary(p => p.Key, p => p.Value.Select(Mark).ToArray()),
            Editable.Select(Mark).ToArray(), (Structures ?? []).Select(s =>
            {
                var p = Point(s.X, s.Y); return new RoomStructure(p.X, p.Y, s.Kind);
            }).ToArray(), turns, mirror);
    }
}

public sealed record PlacedRoom(string Id, string[] Roles, string[] Rows, RoomSocket[] Sockets,
    Dictionary<string, RoomPoint[]> Markers, RoomPoint[] Editable, RoomStructure[] Structures, int Turns, bool Mirror)
{
    public int Width => Rows[0].Length;
    public int Height => Rows.Length;
}

public sealed record DungeonBiome(string Id, string Name, string Description, string Strategy, string EliteId,
    string MapId, string FloorId, string[] EnemyIds, string RoomPrefix);
public sealed record DungeonGeneration(int Width, int Height, int RoomMin, int RoomMax, int StandardChests);
public sealed record FinalSanctum(string Id, string FloorId, int Width, int Height, int StartX, int StartY, int BossX, int BossY, string[] Rows);

/// <summary>Original authored content only. Layout and gameplay execute handwritten C#.</summary>
public sealed class DungeonCatalog
{
    private sealed record Document(int SchemaVersion, string SourceRevision, DungeonRoom[] Rooms,
        string[] RoomOrder, DungeonBiome[] Biomes, DungeonGeneration Generation, FinalSanctum Finale);
    private static readonly Lazy<DungeonCatalog> BuiltIn = new(LoadBuiltIn);
    public static DungeonCatalog Default => BuiltIn.Value;
    public IReadOnlyDictionary<string, DungeonRoom> Rooms { get; }
    public IReadOnlyList<DungeonBiome> Biomes { get; }
    public IReadOnlyList<string> RoomOrder { get; }
    public DungeonGeneration Generation { get; }
    public FinalSanctum Finale { get; }

    private DungeonCatalog(Document document)
    {
        if (document.SchemaVersion != 1 || document.SourceRevision != "d498fe0889050d2a23f2ff86d80db6084ed6467a"
            || document.Rooms == null || document.Biomes == null || document.RoomOrder == null
            || document.Generation == null || document.Finale == null || document.Rooms.Length != 147 || document.Biomes.Length != 7)
            throw new InvalidDataException("Incomplete original dungeon catalog.");
        Rooms = document.Rooms.ToFrozenDictionary(r => r.Id, StringComparer.Ordinal);
        Biomes = Array.AsReadOnly(document.Biomes); RoomOrder = Array.AsReadOnly(document.RoomOrder);
        Generation = document.Generation; Finale = document.Finale;
        foreach (var room in Rooms.Values)
        {
            if (room.Rows.Length < 3 || room.Rows[0].Length < 3 || room.Rows.Any(row => row.Length != room.Width || row.Any(c => !"#.Bbcgim~".Contains(c)))
                || room.Rotations.Length == 0 || room.Rotations.Any(t => t is < 0 or > 3) || room.Roles.Length == 0
                || room.Sockets.Any(s => !Inside(s.X, s.Y) || s.Facing is not ("N" or "E" or "S" or "W") || s.Width != 1)
                || room.Markers.Values.SelectMany(p => p).Concat(room.Editable).Any(p => !Inside(p.X, p.Y))
                || (room.Structures ?? []).Any(p => !Inside(p.X, p.Y)))
                throw new InvalidDataException("Invalid authored room: " + room.Id);
            bool Inside(int x, int y) => x >= 1 && x <= room.Width && y >= 1 && y <= room.Height;
        }
        foreach (var biome in Biomes)
        {
            foreach (string id in biome.EnemyIds.Append(biome.EliteId)) _ = EnemyCatalog.Default.Get(id);
            foreach (string id in RoomOrder) if (!Rooms.ContainsKey(biome.RoomPrefix + id[(id.LastIndexOf('/') + 1)..]))
                throw new InvalidDataException("Missing biome room variant: " + biome.Id + "/" + id);
        }
        if (Finale.Rows.Length != Finale.Height || Finale.Rows.Any(r => r.Length != Finale.Width))
            throw new InvalidDataException("Invalid final sanctum.");
    }

    public IReadOnlyList<DungeonRoom> ForRole(string biomeId, string role)
    {
        var biome = Biomes.Single(b => b.Id == biomeId);
        return RoomOrder.Select(id => Rooms[biome.RoomPrefix + id[(id.LastIndexOf('/') + 1)..]])
            .Where(r => r.Roles.Contains(role)).ToArray();
    }

    private static DungeonCatalog LoadBuiltIn()
    {
        using var stream = typeof(DungeonCatalog).Assembly.GetManifestResourceStream("Roag.Tactics.Dungeons.json")
            ?? throw new InvalidDataException("Missing native dungeon content.");
        var document = JsonSerializer.Deserialize<Document>(stream, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, RespectRequiredConstructorParameters = true,
            RespectNullableAnnotations = true
        }) ?? throw new InvalidDataException("Missing dungeon document.");
        return new(document);
    }
}
