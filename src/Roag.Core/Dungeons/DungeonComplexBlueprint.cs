using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Core.Dungeons;

/// <summary>A construction artifact. Live terrain is materialized into world chunks.</summary>
public sealed record DungeonFloorBlueprint(int Branch, int Stage, DungeonLayout Layout, WorldPosition Origin, bool Elite)
{
    public WorldPosition Entry => Origin.Offset(Layout.Start.X, Layout.Start.Y);
    public WorldPosition Exit => Origin.Offset(Layout.Elite.X, Layout.Elite.Y);

    public DungeonFloorState Describe(long boss = 0) => new(Stage, Layout.Biome, Origin, Layout.Width, Layout.Height,
        Layout.Rooms.Select(r => new DungeonRoomSite(r.Index, r.Shape.Id, Origin.Offset(r.X, r.Y),
            r.Shape.Width, r.Shape.Height, r.Shape.Turns, r.Shape.Mirror, r.Role, Layout.CriticalRooms.Contains(r.Index))).ToArray(),
        Entry, Exit, boss, Branch);

    public WorldRoomId? RoomAt(WorldPosition position, int place)
    {
        foreach (var room in Layout.Rooms)
            if (position.Floor == Origin.Floor && position.X >= Origin.X + room.X && position.Y >= Origin.Y + room.Y
                && position.X < Origin.X + room.X + room.Shape.Width && position.Y < Origin.Y + room.Y + room.Shape.Height)
                return new(RoomArea.Dungeon, place, position.Floor, Branch * 16 + room.Index + 1);
        return null;
    }
}

public sealed record DungeonBranchLink(int From, int To, string Route);

/// <summary>All original route alternatives have fixed physical coordinates before exploration.</summary>
public sealed record DungeonComplexBlueprint(DungeonParcel Parcel, int Seed, DungeonFloorBlueprint[] Floors,
    DungeonFloorState Finale, DungeonBranchLink[] Links, DungeonStairPassage[] Passages)
{
    public const int FinalBranch = 40;
    public WorldPosition SurfaceEntrance => Floors.Single(f => f.Stage == 1 && !f.Elite).Entry.Offset(4, 0) with { Floor = 0 };
}

public static class DungeonComplexPlanner
{
    public static DungeonComplexBlueprint Generate(WorldPlan plan, DungeonParcel parcel)
    {
        var blueprint = GenerateGeometry(plan, parcel);
        return blueprint with { Passages = DungeonPassagePlanner.Generate(blueprint) };
    }

    internal static DungeonComplexBlueprint GenerateGeometry(WorldPlan plan, DungeonParcel parcel)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(parcel);
        if (plan.FloorCount < 8 || parcel.Id < 1 || parcel.Width < DungeonParcelPlanner.Width || parcel.Height < DungeonParcelPlanner.Height
            || !plan.Contains(new(parcel.X, parcel.Y)) || !plan.Contains(new(parcel.X + parcel.Width - 1, parcel.Y + parcel.Height - 1, 7)))
            throw new ArgumentException("A complex needs a full-sized in-world seven-depth parcel.", nameof(parcel));
        int seed = (int)(WorldRandom.Hash(plan.Seed, parcel.Id, salt: 9441) % (RoagRandom.Modulus - 1)) + 1;
        var floors = new List<DungeonFloorBlueprint>();
        var links = new List<DungeonBranchLink>();
        var biomes = DungeonCatalog.Default.Biomes;
        int Branch(int stage, string biome) => stage * 10 + Enumerable.Range(0, biomes.Count).Single(i => biomes[i].Id == biome);

        AddPair(1, biomes[0].Id, 0);
        var firstRoutes = DungeonRoutes.ForStage(1, []);
        for (int slot = 0; slot < firstRoutes.Count; slot++)
        {
            var route = firstRoutes[slot]; int branch = Branch(2, route.Biome!);
            AddPair(2, route.Biome!, slot);
            links.Add(new(Branch(1, biomes[0].Id), branch, route.Id));
            foreach (var next in DungeonRoutes.ForStage(2, [route.Id]))
                links.Add(new(branch, Branch(3, next.Biome!), next.Id));
        }
        var laterBiomes = links.Where(l => l.From / 10 == 2).Select(l => l.To % 10).Distinct().Order().ToArray();
        for (int slot = 0; slot < laterBiomes.Length; slot++)
        {
            int biome = laterBiomes[slot]; AddPair(3, biomes[biome].Id, slot);
            links.Add(new(Branch(3, biomes[biome].Id), DungeonComplexBlueprint.FinalBranch, "core:route/gateway"));
        }
        var map = DungeonCatalog.Default.Finale;
        var origin = new WorldPosition(parcel.X + (parcel.Width - map.Width) / 2, parcel.Y + (parcel.Height - map.Height) / 2, 7);
        var finale = new DungeonFloorState(4, DungeonRunState.FinaleBiome, origin, map.Width, map.Height,
            [new(0, map.Id, origin, map.Width, map.Height, 0, false)],
            origin.Offset(map.StartX - 1, map.StartY - 1), origin.Offset(map.BossX - 1, map.BossY - 1), Branch: DungeonComplexBlueprint.FinalBranch);
        return new(parcel, seed, floors.ToArray(), finale, links.ToArray(), []);

        void AddPair(int stage, string biome, int slot)
        {
            if (slot >= DungeonParcelPlanner.Columns * DungeonParcelPlanner.Rows)
                throw new InvalidDataException("Original route alternatives exceed the reserved physical slots.");
            int stageSeed = stage == 1 ? seed : RoagRandom.Derive(seed, "stage:" + stage);
            var upper = DungeonLayoutGenerator.Generate(stageSeed, biome);
            var lower = DungeonLayoutGenerator.Generate(stage == 1 ? unchecked(seed + 1) : RoagRandom.Derive(stageSeed, "lower"), biome);
            int dx = upper.Elite.X - 4 - lower.Start.X, dy = upper.Elite.Y - lower.Start.Y;
            var at = new WorldPosition(parcel.X + DungeonParcelPlanner.Clearance + slot % DungeonParcelPlanner.Columns * DungeonParcelPlanner.SlotWidth - Math.Min(0, dx),
                parcel.Y + DungeonParcelPlanner.Clearance + slot / DungeonParcelPlanner.Columns * DungeonParcelPlanner.SlotHeight - Math.Min(0, dy), stage * 2 - 1);
            int branch = Branch(stage, biome);
            floors.Add(new(branch, stage, upper, at, false));
            floors.Add(new(branch, stage, lower, at.Offset(dx, dy) with { Floor = at.Floor + 1 }, true));
        }
    }
}
