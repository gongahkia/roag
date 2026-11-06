using System.Text.Json.Serialization;
using Roag.Core.Content;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public enum CampaignFinalRole { HollowRegent, Bellkeeper, SluiceMaw, Threadmother, OssuaryCaptain }

public sealed record GeneratedBossSite(CampaignBossKind Kind, long Arena, int SiteId, WorldPosition Entrance);
public sealed record RetainedGuardianHome(long Actor, int SourceSite, int SiteId, WorldPosition Spawn);

/// <summary>Generation receipts and final identity. Actors and chunks own all live combat state.</summary>
public sealed class GeneratedCampaignFinale
{
    public int Version { get; set; } = 1;
    public CampaignFinalRole Role { get; set; }
    public long FinalActor { get; set; }
    public int FinalSite { get; set; }
    public int RegentComplex { get; set; }
    public List<GeneratedBossSite> BossSites { get; set; } = [];
    public List<RetainedGuardianHome> Guardians { get; set; } = [];
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? VictoryAt { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long Victor { get; set; }

    public static CampaignFinalRole SelectRole(int seed)
        => (CampaignFinalRole)(SeedFor(seed, "campaign-final-role:v1") % 5);

    internal static int SeedFor(int seed, string label)
        => RoagRandom.Derive((int)(unchecked((uint)seed) % (RoagRandom.Modulus - 1)) + 1, label);

    internal static int[] Strongholds(WorldPlan plan) => plan.Sites
        .OrderBy(s => WorldRandom.Hash(plan.Seed, s.Id, salt: 9650)).ThenBy(s => s.Id).Take(24).Select(s => s.Id).ToArray();

    internal static CampaignBossKind? BossKind(CampaignFinalRole role) => role switch
    {
        CampaignFinalRole.HollowRegent => null, CampaignFinalRole.Bellkeeper => CampaignBossKind.Bellkeeper,
        CampaignFinalRole.SluiceMaw => CampaignBossKind.SluiceMaw, CampaignFinalRole.Threadmother => CampaignBossKind.Threadmother,
        CampaignFinalRole.OssuaryCaptain => CampaignBossKind.OssuaryCaptain,
        _ => throw new InvalidDataException("Unknown generated final role.")
    };

    internal static EnemyDefinition FinalRegentDefinition => EnemyCatalog.Default.Get("core:enemy/hollow_regent") with
    { Name = "Hollow Regent / Final Guardian", Hp = 124, Damage = 8, Speed = 100, Windup = 90, Recovery = 115, XpReward = 100, CurrencyDrop = 30 };

    internal void Validate(WorldPlan plan, WorldState world, ContentCatalog content, IReadOnlyDictionary<long, Actor> actors)
    {
        if (Version != 1 || !Enum.IsDefined(Role) || Role != SelectRole(plan.Seed) || world.NativeCampaign == null
            || world.Dungeon != null || world.Tactics == null || FinalActor <= 0 || FinalSite is < 0 or >= 48
            || world.Endgame == null || world.Endgame.ActorId != FinalActor || world.Endgame.SiteId != FinalSite
            || !actors.TryGetValue(FinalActor, out var final) || final.Tactics?.Enemy == null || world.Party.Contains(FinalActor)
            || BossSites == null || BossSites.Count != 4 || BossSites.Any(s => s == null)
            || BossSites.Select(s => s.Kind).Distinct().Count() != 4 || BossSites.Select(s => s.Arena).Distinct().Count() != 4
            || BossSites.Select(s => s.SiteId).Distinct().Count() != 4 || Guardians == null || Guardians.Count != 48
            || Guardians.Any(g => g == null) || Guardians.Select(g => g.SourceSite).Distinct().Count() != 48)
            throw new InvalidDataException("Invalid generated campaign final identity or placement receipts.");
        var strongholds = Strongholds(plan);
        var families = RetainedFamilies(plan, content);
        foreach (var home in Guardians)
        {
            if (home.SourceSite is < 0 or >= 48 || home.Actor != 40000 + home.SourceSite * 8
                || home.SiteId != strongholds[home.SourceSite % 24] || home.Spawn.Floor != (home.SourceSite < 24 ? 4 : 6)
                || !ChamberAt(plan.Sites[home.SiteId], home.Spawn) || !actors.TryGetValue(home.Actor, out var guardian)
                || content.Entries.GetValueOrDefault(guardian.Archetype) is not BossDefinition definition
                || guardian.Tactics?.Enemy?.Retained == null || guardian.EncounterLeader != home.Actor
                || guardian.Archetype != families[home.SourceSite].Definition || guardian.VariantId != families[home.SourceSite].Variant
                || actors.Values.Count(a => a.Id != home.Actor && a.EncounterLeader == home.Actor) != definition.Minions.Length
                || definition.Minions.Where((id, i) => !actors.TryGetValue(home.Actor + i + 1, out var minion)
                    || minion.Archetype != id || minion.EncounterLeader != home.Actor).Any())
                throw new InvalidDataException("Invalid retained guardian family relocation.");
            // Caches remain at the source sites, with their original unique themed choices.
            for (int floor = 0; floor < plan.FloorCount; floor++)
                if (!world.Loot.Any(c => c.Id == $"site-{home.SourceSite}-{floor}"
                    && c.Position == (plan.Sites[home.SourceSite].Center.Offset(0, 2) with { Floor = floor })))
                    throw new InvalidDataException("A retained site cache opportunity was removed or relocated.");
        }
        var store = world.Tactics.CampaignBosses ?? throw new InvalidDataException("Missing generated arena registry.");
        foreach (var site in BossSites)
        {
            var arena = store.Arenas.SingleOrDefault(a => a.Id == site.Arena);
            if (!Enum.IsDefined(site.Kind) || site.SiteId is < 0 or >= 48 || strongholds.Contains(site.SiteId)
                || arena == null || arena.Kind != site.Kind || !ChamberAt(plan.Sites[site.SiteId], arena.Center)
                || arena.Seed != SeedFor(plan.Seed, "campaign-arena:" + site.Kind)
                || arena.Center.Floor != 1 || site.Entrance != plan.Sites[site.SiteId].Center.Offset(2, 0)
                || arena.FinalRole != (BossKind(Role) == site.Kind))
                throw new InvalidDataException("Invalid generated major boss site or final role.");
        }
        var finalArenas = store.Arenas.Where(a => a.FinalRole).ToArray();
        if (Role == CampaignFinalRole.HollowRegent)
        {
            var complex = world.NativeCampaign.Complexes?.SingleOrDefault(c => c.Parcel.Id == RegentComplex);
            var complexes = world.NativeCampaign.Complexes?.OrderBy(c => c.Parcel.Id).ToArray();
            if (complex?.Populated != true || !complex.Run.Floors.Any(f => f.Stage == 4 && f.Boss == FinalActor)
                || complexes == null || complexes[SeedFor(plan.Seed, "campaign-final-regent:v1") % complexes.Length] != complex
                || finalArenas.Length != 0 || final.Tactics.DefinitionId != "core:enemy/hollow_regent"
                || final.Tactics.Enemy.Retained != null || final.Tactics.Enemy.CampaignBoss != null
                || final.Tactics.InnateStats != FinalRegentDefinition.CreateTactics().InnateStats
                || final.Tactics.MoveCost != FinalRegentDefinition.MoveCost || final.Tactics.ExperienceReward != 100
                || final.Tactics.CurrencyDrop != 30 || final.Tactics.RegularEnemy || !final.Tactics.ImmuneToPull
                || final.Tactics.Invulnerable || final.Tactics.RewardExempt || final.Tactics.HealthDrop || final.Tactics.FoodDrop)
                throw new InvalidDataException("Invalid stronger final Regent role.");
        }
        else if (RegentComplex != 0 || finalArenas.Length != 1 || finalArenas[0].Boss != FinalActor
            || !BossSites.Any(s => s.SiteId == FinalSite && s.Arena == finalArenas[0].Id))
            throw new InvalidDataException("Invalid selected final arena.");
        if (VictoryAt is { } at ? !world.Endgame.Victory || at < world.NativeCampaign.StartedAt * world.Tactics.Tuning.UnitsPerWorldTick
                || at > world.Tactics.Queue.Clock || final.Life != LifeState.Dead || !final.Tactics.RewardsClaimed
                || !world.Party.Contains(Victor) || !actors.TryGetValue(Victor, out var victor) || victor.Tactics?.Build == null
            : world.Endgame.Victory || Victor != 0)
            throw new InvalidDataException("Invalid one-time campaign victory receipt.");
    }

    private static bool ChamberAt(SitePlan site, WorldPosition position)
        => Math.Abs(position.X - site.Center.X) == 18 && Math.Abs(position.Y - site.Center.Y) == 18;

    // Reproduce only the preserved adventure identity stream; this does not
    // create actors, loot, visibility, tactical RNG or new encounter decisions.
    private static (string Definition, string Variant)[] RetainedFamilies(WorldPlan plan, ContentCatalog content)
    {
        var random = WorldRandom.Stream(plan.Seed, 0, salt: 4100); var pool = content.Game.Bosses.ToArray();
        for (int i = pool.Length - 1; i > 0; i--) { int pick = random.Next(i + 1); (pool[i], pool[pick]) = (pool[pick], pool[i]); }
        random.Next(plan.Sites.Count); // The preserved imported final-site draw.
        return plan.Sites.Select(site =>
        {
            var definition = content.Get<BossDefinition>(pool[site.Id % pool.Length]);
            string variant = definition.Variants[random.Next(definition.Variants.Length)]; random.Next(3);
            return (definition.Id, variant);
        }).ToArray();
    }
}
