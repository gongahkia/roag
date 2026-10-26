using System.Text.Json.Nodes;
using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record SituationObservation(int Seed, string Definition, string Method, WorldPosition Position, string Outcome,
    bool ActionAccepted, int RemainingOrders, bool RewardClaimed, bool RepeatRejected, Material SideWall, Material? Bridge, long Tick);
public sealed record SituationAudit(bool Passed, int Seeds, int LayoutsChecked, string[] BrokenAlternatives, SituationObservation[] Observations, string Interpretation);

public static class SituationPreview
{
    public static WorldDiscovery Prepare(WorldSession world, string id)
    {
        var discovery = world.State.Discoveries.First(d => d.Definition == id);
        var origin = discovery.Position.Offset(0, 1);
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited && !discovery.Enemies.Contains(a.Id) && a.Position.Distance(origin) < 100))
            actor.Position = world.Plan.Towns[(actor.HomeTown + 12) % 24].Center with { Floor = actor.Position.Floor };
        world.State.Position = world.Party.First().Position = origin; world.State.WorkRally = origin;
        world.RevealSurface(origin.X - 11, origin.Y - 11, origin.X + 12, origin.Y + 12);
        foreach (var cost in world.Situation(discovery)!.Approaches.SelectMany(a => a.Cost))
        {
            string stack = world.Content.Get<ItemDefinition>(cost.Key).Stack;
            world.State.Inventory[stack] = Math.Max(world.Count(stack), cost.Value);
        }
        world.Submit(new(WorldCommandKind.Discover, discovery.Position)); world.Paused = true;
        return discovery;
    }
    public static SituationObservation RunMethod(ContentCatalog catalog, string id, string method, int seed)
    {
        if (!catalog.Game.Discoveries.Contains(id)) catalog = SimulationPreview.WithGame(catalog, g => g["discoveries"] = new JsonArray(id));
        var world = WorldSession.Create(seed, catalog); var d = Prepare(world, id); var hero = world.Party.First();
        bool accepted;
        if (method == "fight")
        {
            hero.Role = 0; hero.BaseHealth = hero.Health = 200; hero.BasePower = 30; hero.BaseSpeed = 30;
            world.StartBattle(d.Enemies[0]); accepted = world.IsInCombat;
            for (int turn = 0; turn < 60 && world.State.Battle is { } battle; turn++)
            {
                if (battle.Active.Friendly) world.Act(0, battle.Units.First(u => !u.Friendly && u.Alive).Id);
                else world.AdvanceEnemy();
            }
        }
        else if (method == "tunnel")
        {
            var wall = world.DiscoveryBypass(d)!.Value;
            var origin = wall.Offset(Math.Sign(wall.X - d.Position.X), Math.Sign(wall.Y - d.Position.Y));
            world.State.Position = hero.Position = origin; world.State.WorkRally = origin; world.Reveal();
            accepted = world.DesignateWork(WorldCommandKind.Dig, [wall]).Queued == 1;
        }
        else accepted = world.Submit(new(WorldCommandKind.ApproachDiscovery, d.Position, Item: method)).Success;
        world.Paused = false;
        for (int tick = 0; tick < 600 && world.State.Work.Count > 0 && !world.IsInCombat; tick++) world.AdvanceTicks(1);
        world.Paused = true;
        // Put the isolated preview back at the actual cache to exercise its normal claim checks.
        world.State.Position = hero.Position = d.Position.Offset(0, 1); world.State.WorkRally = world.State.Position; world.Reveal();
        var cache = world.State.Loot.Single(c => c.Id == d.Id);
        bool claimed = world.Submit(new(WorldCommandKind.Loot, d.Position, Item: cache.Choices[0])).Success;
        bool repeatRejected = !world.Submit(new(WorldCommandKind.Loot, d.Position, Item: cache.Choices[0])).Success;
        var bridge = WorldSession.SituationFeature(d, world.Situation(d)!.BridgeCells);
        return new(seed, id, method, d.Position, d.Outcome, accepted, world.State.Work.Count, claimed, repeatRejected,
            world.Chunks.At(world.DiscoveryBypass(d)!.Value), bridge is { } p ? world.Chunks.At(p) : null, world.State.Tick);
    }
    public static SituationObservation[] Run(ContentCatalog catalog, string id, int seed)
    {
        var situation = catalog.Get<DiscoveryDefinition>(id).Situation ?? throw new ArgumentException("Choose a situation discovery.");
        return situation.Approaches.Select(a => a.Action).Concat(["fight", "tunnel"]).Select(method => RunMethod(catalog, id, method, seed)).ToArray();
    }
    public static SituationAudit Audit(ContentCatalog catalog, int seed, int samples)
    {
        if (samples is < 1 or > 16) throw new ArgumentException("Audit 1..16 worlds.");
        var observations = new List<SituationObservation>();
        var broken = new List<string>(); int checkedLayouts = 0;
        for (int n = 0; n < samples; n++)
        {
            int current = unchecked(seed + n); var world = WorldSession.Create(current, catalog);
            foreach (var d in world.State.Discoveries.Where(d => world.Situation(d) != null))
            {
                checkedLayouts++; var rules = world.Situation(d)!;
                if (world.DiscoveryBypass(d) is not { } wall || world.Chunks.At(wall) != Material.Wall
                    || WorldSession.SituationFeature(d, rules.BridgeCells) is { } bridge && world.Chunks.At(bridge) != Material.Water)
                    broken.Add(current + ":" + d.Id);
            }
            foreach (var id in catalog.Game.Discoveries.Where(id => catalog.Get<DiscoveryDefinition>(id).Situation != null))
                observations.AddRange(Run(catalog, id, current));
        }
        bool passed = observations.Count > 0 && broken.Count == 0 && observations.All(o => o.ActionAccepted && o.Outcome == o.Method && o.RemainingOrders == 0 && o.RewardClaimed && o.RepeatRejected
            && (o.Method != "tunnel" || Materials.Walkable(o.SideWall)) && (o.Bridge == null || o.Method == "tunnel" || o.Bridge == Material.Bridge));
        return new(passed, samples, checkedLayouts, broken.ToArray(), observations.ToArray(), "All generated side walls/bridge sites plus real reserved work, combat, excavation and reward handlers. Isolated fixtures supply authored costs and strengthen the test fighter; this verifies alternatives, not their balance.");
    }
}
