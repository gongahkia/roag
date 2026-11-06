using Roag.Core.World;

namespace Roag.Core.Dungeons;

/// <summary>Paid branch choices and the remaining original, diggable passage seals.</summary>
public sealed class DungeonComplexRoutes
{
    public Dictionary<int, string> Choices { get; set; } = [];
    public HashSet<long> UnbrokenSeals { get; set; } = [];

    internal static IReadOnlyList<DungeonRoute> Offers(DungeonFloorState floor)
        => DungeonRoutes.ForStage(floor.Stage, floor.Stage == 2 ? [DungeonRoutes.RouteId(floor.Biome)] : []);

    internal static DungeonFloorState Target(DungeonRunState run, DungeonFloorState source, DungeonRoute route)
        => run.Floors.Single(f => f.Stage == source.Stage + 1 && f.Origin.Floor % 2 == 1
            && f.Biome == (route.Biome ?? DungeonRunState.FinaleBiome));

    internal static DungeonComplexRoutes Create(DungeonComplexBlueprint blueprint)
        => new() { UnbrokenSeals = blueprint.Passages.SelectMany(p => p.Routes.Select(r => r.Barrier).Prepend(p.Barrier))
            .Select(DungeonRunState.CellKey).ToHashSet() };

    internal void Validate(WorldPlan plan, DungeonComplexState complex, WorldState world)
    {
        var run = complex.Run;
        var sources = run.Floors.Where(f => f.Stage < 4 && f.Origin.Floor % 2 == 0).ToArray();
        var expected = sources.SelectMany(f => Offers(f).Select(r => DungeonPassagePlanner.TargetSeal(Target(run, f, r)))
            .Prepend(DungeonPassagePlanner.SourceSeal(f))).Select(DungeonRunState.CellKey).ToHashSet();
        if (Choices == null || UnbrokenSeals == null || Choices.Count > sources.Length || !UnbrokenSeals.IsSubsetOf(expected))
            throw new InvalidDataException("Invalid physical route choice or original seal registry.");
        foreach (var (branch, id) in Choices)
        {
            var source = sources.SingleOrDefault(f => f.Branch == branch);
            var route = source == null ? null : Offers(source).SingleOrDefault(r => r.Id == id);
            if (!complex.Populated || source == null || route == null || !run.BossExitReceipts.Contains(source.Boss)
                || UnbrokenSeals.Contains(DungeonRunState.CellKey(DungeonPassagePlanner.SourceSeal(source)))
                || UnbrokenSeals.Contains(DungeonRunState.CellKey(DungeonPassagePlanner.TargetSeal(Target(run, source, route)))))
                throw new InvalidDataException("A physical route choice lacks its eligible elite receipt or opened seals.");
        }
        if (UnbrokenSeals.Count == 0) return;
        using var chunks = new ChunkCache(plan, world);
        foreach (long key in UnbrokenSeals)
        {
            var cell = new WorldPosition((int)(key % WorldPlan.Size), (int)(key / WorldPlan.Size % WorldPlan.Size),
                (int)(key / WorldPlan.Size / WorldPlan.Size));
            if (chunks.At(cell) != Material.Wall || world.Structures.Any(s => s.Position == cell))
                throw new InvalidDataException("A remaining original passage seal was changed without retiring its identity.");
        }
    }
}
