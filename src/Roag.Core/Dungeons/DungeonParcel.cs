using Roag.Core.World;

namespace Roag.Core.Dungeons;

/// <summary>A physical column reserved before any complex terrain or actors are placed.</summary>
public sealed record DungeonParcel(int Id, int X, int Y, int Width, int Height)
{
    public bool Contains(WorldPosition position) => position.X >= X && position.X < X + Width
        && position.Y >= Y && position.Y < Y + Height;

    public bool Overlaps(DungeonParcel other, int clearance = 0)
        => X < other.X + other.Width + clearance && other.X < X + Width + clearance
            && Y < other.Y + other.Height + clearance && other.Y < Y + Height + clearance;
}

/// <summary>Pure geography planning; never edits chunks, reveals terrain or relocates inhabitants.</summary>
public static class DungeonParcelPlanner
{
    // Six slots accommodate all original route alternatives at either paired
    // depth. Each slot also reserves the maximum uncompressed stair offset
    // between two independently generated 140 x 90 layouts.
    public const int Columns = 3, Rows = 2, Clearance = 16;
    public static int SlotWidth => DungeonCatalog.Default.Generation.Width * 2 + Clearance;
    public static int SlotHeight => DungeonCatalog.Default.Generation.Height * 2 + Clearance;
    public static int Width => Columns * SlotWidth + Clearance * 2;
    public static int Height => Rows * SlotHeight + Clearance * 2;

    public static IReadOnlyList<DungeonParcel> Reserve(WorldPlan plan, int count = 4)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.FloorCount < 8) throw new ArgumentException("Roag complexes require seven physical underground floors.", nameof(plan));
        if (count is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(count));
        var random = WorldRandom.Stream(plan.Seed, 0, salt: 9440);
        var result = new List<DungeonParcel>();
        var occupied = plan.Towns.Select(t => t.Center).Concat(plan.Sites.Select(s => s.Center))
            .Select((p, i) => new DungeonParcel(i, p.X - 32, p.Y - 32, 64, 64)).ToArray();
        for (int id = 1; id <= count; id++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < 10000; attempt++)
            {
                var parcel = new DungeonParcel(id,
                    Clearance + random.Next(WorldPlan.Size - Width - Clearance * 2 + 1),
                    Clearance + random.Next(WorldPlan.Size - Height - Clearance * 2 + 1), Width, Height);
                if (occupied.Any(p => parcel.Overlaps(p, Clearance)) || result.Any(p => parcel.Overlaps(p, Clearance))) continue;
                result.Add(parcel); placed = true; break;
            }
            if (!placed) throw new InvalidDataException("Cannot reserve separated full-sized dungeon complexes.");
        }
        return result;
    }
}
