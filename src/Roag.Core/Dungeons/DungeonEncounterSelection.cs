using Roag.Core.Tactics;

namespace Roag.Core.Dungeons;

public sealed record IdeaEncounterSelection(string? Definition, int Seed);
public sealed record SquadEncounterSelection(EnemySquadKind Kind, int Followers);

public static class DungeonEncounterSelection
{
    private static readonly string[] IdeaPool = ["heavy_rammer", "hollow_wraith", "grave_cantor", "gulp_toad", "haunted_cache",
        "bone_splitter", "alarm_keeper", "web_weaver", "oath_twin_a", "rotating_sentinel", "mirror_hexer", "silt_worm", "ember_moth"];

    public static IdeaEncounterSelection Idea(int seed, string sourceFloor)
    {
        int derived = RoagRandom.Derive(seed, "idea-enemies:" + sourceFloor);
        return new(derived % 4 == 0 ? "core:enemy/" + IdeaPool[derived / 16 % IdeaPool.Length] : null, derived);
    }

    public static SquadEncounterSelection Squad(int seed, string biome, string sourceFloor)
    {
        EnemySquadKind[] pool = biome["core:biome/".Length..] switch
        {
            "overgrown_ruins" => [EnemySquadKind.CaptainAndFollowers],
            "fungal_hollows" => [EnemySquadKind.HealerRushwings],
            "sunken_aqueduct" => [EnemySquadKind.ShieldAndArcher],
            "ashen_bastion" => [EnemySquadKind.WardedBodyguards, EnemySquadKind.ShieldAndArcher, EnemySquadKind.CaptainAndFollowers],
            "glacial_observatory" => [EnemySquadKind.SnaresAndSiege],
            "fractured_archive" => [EnemySquadKind.WardedBodyguards, EnemySquadKind.SnaresAndSiege],
            "sundered_forge" => [EnemySquadKind.CaptainAndFollowers, EnemySquadKind.HealerRushwings],
            _ => throw new ArgumentException("Unknown original squad biome.", nameof(biome))
        };
        var random = new RoagRandom(RoagRandom.Derive(seed, "enemy-squad:" + sourceFloor));
        var kind = pool[random.NextOneBased(pool.Length) - 1];
        int followers = kind == EnemySquadKind.CaptainAndFollowers ? 1 + random.NextOneBased(4)
            : kind is EnemySquadKind.WardedBodyguards or EnemySquadKind.HealerRushwings ? 2 : 1;
        return new(kind, followers);
    }
}
