using Roag.Core.Content;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private static readonly (int X, int Y)[] CampaignChambers = [(-18, -18), (18, -18), (-18, 18), (18, 18)];

    /// <summary>Fresh construction only. Loading and reentry never invoke this method.</summary>
    internal void PopulateGeneratedCampaign()
    {
        if (State.NativeCampaign is not { Finale: null, Complexes.Count: 4 } campaign
            || State.Tactics is not { NextAction: 1 } tactics || State.Dungeon != null
            || tactics.Queue.Clock != campaign.StartedAt * tactics.Tuning.UnitsPerWorldTick)
            throw new InvalidOperationException("Generate final roles only in a new complete open campaign.");
        string previousObjective = FinalObjective;
        var finale = new GeneratedCampaignFinale { Role = GeneratedCampaignFinale.SelectRole(Seed) };
        var strongholds = GeneratedCampaignFinale.Strongholds(Plan);
        // Preserve all original seeded actors, attendants, abilities, rewards and
        // source-site caches. Two separate depths share each designated stronghold.
        var families = Plan.Sites.Select(source =>
        {
            long id = 40000 + source.Id * 8;
            var boss = ActorById(id);
            return (Source: source.Id, Boss: boss, Members: State.Actors.Where(a => a.Id == id || a.EncounterLeader == id).ToArray());
        }).ToArray();
        var moving = families.SelectMany(f => f.Members).Select(a => a.Id).ToHashSet();
        foreach (var family in families)
        {
            int siteId = strongholds[family.Source % strongholds.Length];
            int floor = family.Source < 24 ? 4 : 6;
            var site = Plan.Sites[siteId]; var previous = family.Boss.Position;
            var deltas = family.Members.Select(a => (a.Position.X - previous.X, a.Position.Y - previous.Y)).ToArray();
            var placed = false;
            foreach (var (dx, dy) in CampaignChambers)
            {
                var center = site.Center.Offset(dx, dy) with { Floor = floor };
                var positions = deltas.Select(d => center.Offset(d.Item1, d.Item2)).ToArray();
                var cells = positions.SelectMany(p => new[] { p, p.Offset(-1, 0), p.Offset(1, 0), p.Offset(0, -1), p.Offset(0, 1) })
                    .Concat(CampaignPassage(site, center)).Distinct().ToArray();
                if (!CampaignPaintFits(cells, moving)) continue;
                foreach (var cell in cells) PaintCampaignPassage(cell);
                for (int i = 0; i < family.Members.Length; i++) family.Members[i].Position = positions[i];
                finale.Guardians.Add(new(family.Boss.Id, family.Source, siteId, center));
                placed = true; break;
            }
            if (!placed) throw new InvalidDataException("No unobserved physical home fits a retained guardian family.");
        }
        var candidates = Plan.Sites.Where(s => !strongholds.Contains(s.Id))
            .OrderBy(s => WorldRandom.Hash(Seed, s.Id, salt: 9651)).ThenBy(s => s.Id).ToArray();
        foreach (var kind in Enum.GetValues<CampaignBossKind>())
        {
            bool placed = false;
            foreach (var site in candidates.Where(s => finale.BossSites.All(b => b.SiteId != s.Id)))
            {
                foreach (var (dx, dy) in CampaignChambers)
                {
                    var center = site.Center.Offset(dx, dy) with { Floor = 1 };
                    int side = dx < 0 ? 7 : -7;
                    var passage = CampaignPassage(site, center.Offset(side, 0)).ToArray();
                    var footprint = Enumerable.Range(-7, 15).SelectMany(y => Enumerable.Range(-7, 15).Select(x => center.Offset(x, y))).ToArray();
                    if (!CampaignPaintFits(footprint.Concat(passage), new HashSet<long>())) continue;
                    // The arena installer does its own complete registered-state
                    // preflight. Only after acceptance carve the external approach.
                    var arena = CreateCampaignBossArena(kind, center, GeneratedCampaignFinale.SeedFor(Seed, "campaign-arena:" + kind),
                        GeneratedCampaignFinale.BossKind(finale.Role) == kind);
                    foreach (var cell in passage.Where(p => !arena.Contains(p))) PaintCampaignPassage(cell);
                    finale.BossSites.Add(new(kind, arena.Id, site.Id, site.Center.Offset(2, 0)));
                    placed = true; break;
                }
                if (placed) break;
            }
            if (!placed) throw new InvalidDataException("No unobserved physical site fits an approved major boss arena.");
        }
        if (finale.Role == CampaignFinalRole.HollowRegent)
        {
            int index = GeneratedCampaignFinale.SeedFor(Seed, "campaign-final-regent:v1") % campaign.Complexes.Count;
            var complex = campaign.Complexes[index];
            finale.RegentComplex = complex.Parcel.Id;
            finale.FinalActor = complex.Run.Floors.Single(f => f.Stage == 4).Boss;
            // SiteId remains a valid legacy map reference; native text uses the
            // actual complex identity and actor coordinates, not this nearest site.
            finale.FinalSite = Plan.Sites.OrderBy(s => s.Center.Distance(ActorById(finale.FinalActor).Position with { Floor = 0 })).ThenBy(s => s.Id).First().Id;
        }
        else
        {
            var selected = finale.BossSites.Single(s => s.Kind == GeneratedCampaignFinale.BossKind(finale.Role));
            finale.FinalActor = tactics.CampaignBosses!.Arenas.Single(a => a.Id == selected.Arena).Boss;
            finale.FinalSite = selected.SiteId;
        }
        campaign.Finale = finale;
        State.Endgame = new() { ActorId = finale.FinalActor, SiteId = finale.FinalSite };
        if (finale.Role == CampaignFinalRole.HollowRegent) DungeonCombat.StrengthenFinalRegent(FinalBoss);
        // Replace only the obsolete fresh-generation objective, retaining every
        // other imported story, source identity and reward opportunity.
        State.Journal.RemoveAll(line => line.EndsWith(": " + previousObjective, StringComparison.Ordinal));
        Log(FinalObjective);
        finale.Validate(Plan, State, Content, State.Actors.ToDictionary(a => a.Id));
    }

    private static IEnumerable<WorldPosition> CampaignPassage(SitePlan site, WorldPosition destination)
    {
        var cell = site.Center.Offset(-2, 0) with { Floor = destination.Floor };
        yield return cell;
        // Turn beside the up-stair; never route through the down-stair at +2,0.
        while (cell.Y != destination.Y) { cell = cell.Offset(0, Math.Sign(destination.Y - cell.Y)); yield return cell; }
        while (cell.X != destination.X) { cell = cell.Offset(Math.Sign(destination.X - cell.X), 0); yield return cell; }
    }

    private bool CampaignPaintFits(IEnumerable<WorldPosition> cells, IReadOnlySet<long> moving)
    {
        var paint = cells.ToHashSet(); var reserved = CampaignBossPlacementReservations();
        foreach (var actor in State.Actors.Where(a => moving.Contains(a.Id))) reserved.Remove(actor.Position);
        foreach (var cell in paint)
        {
            if (!Plan.Contains(cell) || Chunks.Seen(cell) || Chunks.IsEdited(cell) || reserved.Contains(cell)
                || ObjectAt(cell) is { Kind: not "decoration" }) return false;
        }
        return true;
    }

    private void PaintCampaignPassage(WorldPosition cell)
    {
        if (Chunks.At(cell) is not (Material.StairsUp or Material.StairsDown)) Chunks.Edit(cell, Material.Floor);
    }
}
