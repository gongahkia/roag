namespace Roag.Core.Dungeons;

public sealed record DungeonRoute(string Id, string Name, string Description, string? Biome);

public static class DungeonRoutes
{
    public static IReadOnlyList<DungeonRoute> ForStage(int stage, IReadOnlyList<string> history)
    {
        if (stage == 3) return [new("core:route/gateway", "Enter the Final Sanctum", "Face the Hollow Regent", null)];
        if (stage is not (1 or 2)) return [];
        var biomes = DungeonCatalog.Default.Biomes;
        int[] indices = stage == 1 ? [1, 2, 4] : history.FirstOrDefault() switch
        {
            "core:route/fungal_hollows" => [2, 3, 5],
            "core:route/sunken_aqueduct" => [1, 3, 6],
            _ => [1, 5, 6]
        };
        return indices.Select(i => biomes[i]).Where(b => !history.Contains(RouteId(b.Id)))
            .Select(b => new DungeonRoute(RouteId(b.Id), b.Name, b.Description, b.Id)).ToArray();
    }

    public static string RouteId(string biome) => biome.Replace("core:biome/", "core:route/", StringComparison.Ordinal);
}
