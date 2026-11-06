using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    internal static int DungeonContentLimit(WorldState world, int perComplex)
        => checked(perComplex * Math.Max(1, world.NativeCampaign?.Complexes?.Count ?? 0));

    internal static DungeonComplexState? SnapshotComplexAt(WorldState world, WorldPosition position)
    {
        if (world.NativeCampaign?.Complexes is not { } complexes) return null;
        for (int i = 0; i < complexes.Count; i++) if (complexes[i].Contains(position)) return complexes[i];
        return null;
    }

    internal static DungeonRunState? SnapshotDungeonAt(WorldState world, WorldPosition position)
        => world.Dungeon ?? SnapshotComplexAt(world, position)?.Run;

    /// <summary>Authored progression at an actual location; live inspection still requires the member's vision.</summary>
    public DungeonRunState? DungeonAt(WorldPosition position) => SnapshotDungeonAt(State, position);
    internal static DungeonFloorState? SnapshotDungeonFloorAt(WorldState world, WorldPosition position)
        => SnapshotDungeonAt(world, position)?.Floors.FirstOrDefault(f => f.Contains(position));
    public DungeonFloorState? DungeonFloorAt(WorldPosition position) => SnapshotDungeonFloorAt(State, position);
    public IEnumerable<DungeonFloorState> DungeonFloors => State.Dungeon?.Floors
        ?? State.NativeCampaign?.Complexes?.SelectMany(c => c.Run.Floors) ?? [];

    internal DungeonRunState? DungeonBossOwner(long actor)
    {
        if (State.Dungeon is { } bounded && bounded.Floors.Any(f => f.Boss == actor)) return bounded;
        if (State.NativeCampaign?.Complexes is { } complexes)
            for (int i = 0; i < complexes.Count; i++)
                if (complexes[i].Run.Floors.Any(f => f.Boss == actor)) return complexes[i].Run;
        return null;
    }

    internal static WorldRoomId? SnapshotAuthoredRoomAt(WorldState world, WorldPosition position)
        => SnapshotCampaignBossRoomAt(world, position)
            ?? (world.Dungeon is { } bounded ? bounded.RoomAt(position) : SnapshotComplexAt(world, position)?.RoomAt(position));

    internal static bool SnapshotRoomTargetExists(WorldState world, WorldPosition source, WorldRoomId target)
    {
        var floor = SnapshotDungeonAt(world, source)?.Floors.FirstOrDefault(f => f.Contains(source));
        int place = SnapshotComplexAt(world, source)?.Parcel.Id ?? 0;
        return floor?.Rooms.Any(r => floor.RoomAt(r.Origin, place) == target) == true;
    }

    internal static WorldRoomId? SnapshotRoomAt(WorldState world, ChunkGenerator generator, WorldPosition position)
    {
        if (SnapshotCampaignBossRoomAt(world, position) is { } arena) return arena;
        if (world.Dungeon is { } bounded) return bounded.RoomAt(position);
        if (SnapshotComplexAt(world, position) is { } complex) return complex.RoomAt(position);
        return generator.RoomAt(position);
    }

    internal DungeonComplexState InstallDungeonComplex(DungeonComplexBlueprint blueprint)
    {
        var complex = InstallDungeonComplexTerrain(blueprint);
        PopulateDungeonComplex(blueprint, complex);
        return complex;
    }

    internal void PopulateDungeonComplex(DungeonComplexBlueprint blueprint, DungeonComplexState complex)
    {
        if (State.NativeCampaign?.Complexes?.Contains(complex) != true || complex.Parcel != blueprint.Parcel
            || complex.Seed != blueprint.Seed || complex.Populated || State.Tactics is not { NextAction: 1 } tactics
            || tactics.Queue.Clock != State.NativeCampaign.StartedAt * tactics.Tuning.UnitsPerWorldTick)
            throw new InvalidOperationException("Only a new, registered physical complex can receive its initial contents.");
        var combat = DungeonCombat;
        foreach (var planned in blueprint.Floors)
        {
            var floor = complex.Run.Floors.Single(f => f.Branch == planned.Branch && f.Origin.Floor == planned.Origin.Floor);
            PopulateDungeonFloor(planned.Layout, new(complex.Run, floor, complex.Seed, complex.Parcel.Id), combat, planned.Elite);
        }
        int finalIndex = complex.Run.Floors.FindIndex(f => f.Stage == 4);
        var finale = complex.Run.Floors[finalIndex];
        var regent = SpawnDungeonEnemy(combat, "core:enemy/hollow_regent", finale.Exit);
        complex.Run.Floors[finalIndex] = finale with { Boss = regent.Id };
        complex.Populated = true;
    }

    /// <summary>Initial construction only; never called on route choice or reentry.</summary>
    internal DungeonComplexState InstallDungeonComplexTerrain(DungeonComplexBlueprint blueprint)
    {
        if (State.NativeCampaign is not { } campaign || State.Dungeon != null)
            throw new InvalidOperationException("A physical complex needs a native world campaign.");
        if (State.Tactics is not { NextAction: 1 } tactics
            || tactics.Queue.Clock != campaign.StartedAt * tactics.Tuning.UnitsPerWorldTick)
            throw new InvalidOperationException("Physical complexes must be constructed before the first paid action.");
        if (campaign.Complexes?.Any(c => c.Parcel.Id == blueprint.Parcel.Id || c.Parcel.Overlaps(blueprint.Parcel)) == true)
            throw new InvalidOperationException("This physical complex is already installed or overlaps another.");
        var complex = new DungeonComplexState(blueprint.Parcel, blueprint.Seed,
            new() { Floors = blueprint.Floors.Select(f => f.Describe()).Append(blueprint.Finale).ToList() })
        { Routes = DungeonComplexRoutes.Create(blueprint) };
        complex.Validate(Plan, State.Actors.ToDictionary(a => a.Id));

        // Prepare the entire paint before changing the world. Adjacent corridor
        // walls cannot overwrite another planned passage or an authored room.
        var tiles = new Dictionary<WorldPosition, Material>();
        foreach (var floor in blueprint.Floors)
            for (int y = 0; y < floor.Layout.Height; y++) for (int x = 0; x < floor.Layout.Width; x++)
            {
                char glyph = floor.Layout.Rows[y][x]; var position = floor.Origin.Offset(x, y);
                tiles[position] = glyph switch { '#' => Material.Wall, '~' => Material.Water, 'B' => Material.Barricade, _ => Material.Floor };
                if (glyph is 'b' or 'g' or 'i' or 'm' or 'c') complex.Run.Terrain[DungeonRunState.CellKey(position)] = glyph;
            }
        var finale = blueprint.Finale; var map = DungeonCatalog.Default.Finale;
        for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
            tiles[finale.Origin.Offset(x, y)] = map.Rows[y][x] == '#' ? Material.Wall : Material.Floor;
        var passages = blueprint.Passages.SelectMany(p => p.UpperCells.Concat(p.Routes.SelectMany(r => r.Cells))).Distinct().ToArray();
        foreach (var cell in passages) tiles[cell] = Material.Floor;
        foreach (var cell in passages)
            foreach (var (dx, dy) in EncounterDirections)
            {
                var neighbor = cell.Offset(dx, dy);
                if (!tiles.ContainsKey(neighbor) && blueprint.Parcel.Contains(neighbor)) tiles[neighbor] = Material.Wall;
            }
        foreach (var floor in blueprint.Floors.Where(f => !f.Elite))
        {
            var lower = blueprint.Floors.Single(f => f.Branch == floor.Branch && f.Elite);
            tiles[floor.Exit] = Material.StairsDown; tiles[lower.Entry] = Material.StairsUp;
        }
        foreach (var passage in blueprint.Passages)
        {
            tiles[passage.Top] = Material.StairsDown; tiles[passage.Bottom] = Material.StairsUp;
            tiles[passage.Barrier] = Material.Wall;
            foreach (var route in passage.Routes) tiles[route.Barrier] = Material.Wall;
        }
        var entrance = blueprint.SurfaceEntrance;
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) tiles[entrance.Offset(x, y)] = Material.Floor;
        tiles[entrance] = Material.StairsDown;
        tiles[blueprint.Floors.Single(f => f.Stage == 1 && !f.Elite).Entry] = Material.StairsUp;
        // Installation is generation, not an excavation/repair operation. It must
        // reject existing exploration, modifications and inhabitants atomically.
        if (tiles.Keys.Any(p => !Plan.Contains(p) || Chunks.IsEdited(p) || Chunks.Seen(p))
            || State.Actors.Any(a => tiles.ContainsKey(a.Position) || a.Tactics?.Resources.Bulwark is { } block && tiles.ContainsKey(block.Position))
            || State.Discoveries.Any(d => tiles.ContainsKey(d.Position)) || State.Loot.Any(c => tiles.ContainsKey(c.Position))
            || State.Structures.Any(s => tiles.ContainsKey(s.Position)) || State.Work.Any(w => tiles.ContainsKey(w.Target)))
            throw new InvalidOperationException("A physical complex cannot replace observed, changed or inhabited terrain.");
        foreach (var (position, material) in tiles) Chunks.Edit(position, material);
        (campaign.Complexes ??= []).Add(complex);
        return complex;
    }
}
