using System.Text;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public static class ExplorationPreview
{
    public static object Discovery(ContentCatalog content, string id, int seed)
    {
        _ = content.Get<DiscoveryDefinition>(id);
        content = SimulationPreview.WithGame(content, g => g["discoveries"] = new System.Text.Json.Nodes.JsonArray(id));
        var world = WorldSession.Create(seed, content); var d = world.State.Discoveries[0];
        // Explicit isolated preview fixture. Commands and rewards still use the real handlers.
        world.State.Position = d.Position.Offset(0, 1); foreach (var a in world.Party) a.Position = world.State.Position; world.Reveal();
        long before = world.State.Tick; uint random = world.State.CombatRandom;
        var visit = world.Submit(new(WorldCommandKind.Discover, d.Position));
        var cache = world.State.Loot.FirstOrDefault(c => c.Id == d.Id);
        Vagabonds.Core.Expeditions.ActionResult? claim = cache == null ? null : world.Submit(new(WorldCommandKind.Loot, d.Position, Item: cache.Choices.FirstOrDefault() ?? ""));
        return new { seed, fixture = "Party placed beside a real generated landmark; no player save.", discovery = d,
            text = world.DiscoveryText(d), visit, claim, cache, guarded = world.DiscoveryGuarded(d), world.State.Inventory,
            unchangedClockAndRandom = before == world.State.Tick && random == world.State.CombatRandom };
    }
    public static (object Report, string Svg) Audit(ContentCatalog content, int seed, int samples)
    {
        if (samples is < 1 or > 32) throw new ArgumentException("Audit 1..32 worlds.");
        var rows = new List<object>(); bool passed = true;
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='960' height='{Math.Ceiling(samples / 2.0) * 500}' viewBox='0 0 960 {Math.Ceiling(samples / 2.0) * 500}'><rect width='100%' height='100%' fill='#10191d'/>");
        for (int i = 0; i < samples; i++)
        {
            int currentSeed = unchecked(seed + i); var world = WorldSession.Create(currentSeed, content); var finds = world.State.Discoveries;
            var terrain = DiscoveryPlacement.Terrain(finds, content, out int accessRepairs);
            var inaccessible = finds.Where(d => DiscoveryPlacement.Trail(d.Anchor, d.Position).Any(p => !terrain.TryGetValue(p, out var material) || !Materials.Walkable(material))).Select(d => d.Id).ToArray();
            var unserved = world.Plan.Towns.Where(t => finds.Count(d => d.TownId == t.Id && d.Context == "settlement") < content.Get<GenerationDefinition>(content.Game.Generation).SettlementFinds).Select(t => t.Id).ToArray();
            passed &= inaccessible.Length == 0 && unserved.Length == 0;
            rows.Add(new { seed = currentSeed, discoveries = finds.Count, byContext = finds.GroupBy(d => d.Context).ToDictionary(g => g.Key, g => g.Count()),
                byTemplate = finds.GroupBy(d => d.Definition).ToDictionary(g => g.Key, g => g.Count()), accessRepairs, unservedTowns = unserved, blockedAccess = inaccessible,
                nearestStartingFind = finds.Where(d => d.TownId == 0).Min(d => d.Position.Distance(world.Plan.Towns[0].Center)),
                ordinaryEnemyKinds = finds.SelectMany(d => d.Enemies).Select(id => world.State.Actors.Single(a => a.Id == id).Archetype).Distinct().Order().ToArray(),
                rewardChoices = world.State.Loot.Count(c => c.Choices.Length == 3), residentChunks = world.Chunks.ResidentCount });
            int ox = i % 2 * 480 + 20, oy = i / 2 * 500 + 40;
            svg.Append($"<g transform='translate({ox} {oy})'><text y='-15' fill='#edc775' font-family='monospace'>Seed {currentSeed}: {finds.Count} discoveries</text><rect width='440' height='440' fill='#1a2927'/>");
            double Scale(int p) => p * 440.0 / WorldPlan.Size;
            string N(double v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            foreach (var r in world.Plan.Roads) svg.Append($"<path d='M {N(Scale(r.A.X))} {N(Scale(r.A.Y))} H {N(Scale(r.B.X))} V {N(Scale(r.B.Y))}' fill='none' stroke='#788178' stroke-width='.65'/>");
            foreach (var d in finds) svg.Append($"<circle cx='{N(Scale(d.Position.X))}' cy='{N(Scale(d.Position.Y))}' r='1.8' fill='{(d.Context == "settlement" ? "#ebc075" : d.Context == "road" ? "#7ccbd2" : "#b59cdb")}'/>");
            foreach (var t in world.Plan.Towns) svg.Append($"<rect x='{N(Scale(t.Center.X) - 2)}' y='{N(Scale(t.Center.Y) - 2)}' width='4' height='4' fill='#f5ead7'/>");
            svg.Append("<text y='461' fill='#c8d5d5' font-family='monospace' font-size='10'>Gold: local / Teal: road / Violet: wild / White: town</text></g>");
        }
        svg.Append("</svg>");
        return (new { passed, samples, worlds = rows, scope = "Seeded density, local coverage, short access trails, templates, enemies and reward choices; enjoyment needs playtesting." }, svg.ToString());
    }
}
