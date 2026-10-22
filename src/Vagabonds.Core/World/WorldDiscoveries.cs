using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

/// <summary>Seeded places and short access trails, independent of visit order and simulation RNG.</summary>
public static class DiscoveryPlacement
{
    public static List<WorldDiscovery> Generate(WorldPlan plan, ContentCatalog content)
    {
        var result = new List<WorldDiscovery>();
        var profile = content.Get<GenerationDefinition>(content.Game.Generation);
        var pool = content.Game.Discoveries.Select(content.Get<DiscoveryDefinition>).ToArray();
        if (pool.Length == 0) return result;
        var rng = WorldRandom.Stream(plan.Seed, 0, salt: 5100);
        foreach (var town in plan.Towns)
        {
            var roads = plan.Roads.Where(r => r.FromTown == town.Id || r.ToTown == town.Id).ToArray();
            for (int i = 0; i < profile.SettlementFinds; i++)
            {
                string action = new[] { "camp", "clue", "cache", "encounter" }[i % 4];
                for (int attempt = 0; attempt < 32; attempt++)
                {
                    var road = roads[(i + attempt) % roads.Length];
                    int distance = 48 + i * 23 + attempt / 4 * 5;
                    int step = road.FromTown == town.Id ? distance : road.Length - distance;
                    var anchor = Along(road, Math.Clamp(step, 1, road.Length - 1));
                    var next = Along(road, Math.Clamp(step + 1, 1, road.Length));
                    int side = (attempt % 2 == 0 ? 1 : -1) * (10 + rng.Next(5));
                    var position = next.X != anchor.X ? anchor.Offset(0, side) : anchor.Offset(side, 0);
                    if (Add(position, anchor, town.Id, "settlement", action)) break;
                }
            }
        }
        foreach (var road in plan.Roads)
            for (int distance = 180; distance < road.Length - 160; distance += profile.RoadDiscoverySpacing)
            {
                var anchor = Along(road, distance); var next = Along(road, distance + 1);
                int side = (rng.Next(2) == 0 ? 1 : -1) * (10 + rng.Next(9));
                Add(next.X != anchor.X ? anchor.Offset(0, side) : anchor.Offset(side, 0), anchor,
                    distance < road.Length / 2 ? road.FromTown : road.ToTown, "road", null);
            }
        for (int i = 0, attempts = 0; i < profile.WildernessFinds && attempts < 4000; attempts++)
        {
            var position = new WorldPosition(80 + rng.Next(WorldPlan.Size - 160), 80 + rng.Next(WorldPlan.Size - 160));
            var anchor = plan.Roads.Select(r => Nearest(r, position)).MinBy(p => p.Distance(position));
            if (position.Distance(anchor) is < 35 or > 100 || plan.Towns.Any(t => position.Distance(t.Center) < 200)) continue;
            int town = plan.Towns.MinBy(t => t.Center.Distance(position))!.Id;
            if (Add(position, anchor, town, "wilderness", null)) i++;
        }
        return result;

        bool Add(WorldPosition position, WorldPosition anchor, int town, string context, string? action)
        {
            if (result.Count >= 1800 || !plan.Contains(position.Offset(-6, -6)) || !plan.Contains(position.Offset(6, 6))
                || result.Any(d => d.Position.Distance(position) < 24)) return false;
            if (Trail(anchor, position).Any(p => plan.Towns.Any(t => Near(p, t.Center)) || plan.Sites.Any(s => Near(p, s.Center)))) return false;
            var choices = pool.Where(d => action == null || d.Action == action).ToArray();
            if (choices.Length == 0) choices = pool;
            var definition = choices[rng.Next(choices.Length)];
            result.Add(new()
            {
                Id = "discovery-" + result.Count, Definition = definition.Id,
                Name = SampleLibrary.Default.Cultures[plan.Towns[town].Culture].Name + " " + definition.Name,
                Position = position, Anchor = anchor, Rotation = rng.Next(4), Context = context, TownId = town,
                SiteId = plan.Sites.MinBy(s => s.Center.Distance(position))!.Id,
                WitnessId = 1000 + town * 16 + rng.Next(8)
            });
            return true;
        }
        static bool Near(WorldPosition p, WorldPosition center) => Math.Abs(p.X - center.X) < 39 && Math.Abs(p.Y - center.Y) < 39;
    }
    public static WorldPosition Along(RoadPlan road, int distance)
    {
        int horizontal = Math.Abs(road.B.X - road.A.X);
        return distance <= horizontal ? road.A.Offset(Math.Sign(road.B.X - road.A.X) * distance, 0)
            : new(road.B.X, road.A.Y + Math.Sign(road.B.Y - road.A.Y) * (distance - horizontal));
    }
    private static WorldPosition Nearest(RoadPlan road, WorldPosition p)
    {
        var horizontal = new WorldPosition(Math.Clamp(p.X, Math.Min(road.A.X, road.B.X), Math.Max(road.A.X, road.B.X)), road.A.Y);
        var vertical = new WorldPosition(road.B.X, Math.Clamp(p.Y, Math.Min(road.A.Y, road.B.Y), Math.Max(road.A.Y, road.B.Y)));
        return horizontal.Distance(p) < vertical.Distance(p) ? horizontal : vertical;
    }
    public static IEnumerable<WorldPosition> Trail(WorldPosition anchor, WorldPosition position)
    {
        var p = anchor; yield return p;
        while (p.X != position.X) { p = p.Offset(Math.Sign(position.X - p.X), 0); yield return p; }
        while (p.Y != position.Y) { p = p.Offset(0, Math.Sign(position.Y - p.Y)); yield return p; }
    }
    public static Dictionary<WorldPosition, Material> Terrain(IEnumerable<WorldDiscovery> discoveries, ContentCatalog content) => Terrain(discoveries, content, out _);
    public static Dictionary<WorldPosition, Material> Terrain(IEnumerable<WorldDiscovery> discoveries, ContentCatalog content, out int accessRepairs)
    {
        var places = discoveries.ToArray(); accessRepairs = 0;
        var overlay = new Dictionary<WorldPosition, Material>();
        foreach (var d in places) foreach (var p in Trail(d.Anchor, d.Position)) overlay[p] = Material.Earth;
        foreach (var d in places)
        {
            var layout = content.Get<DiscoveryDefinition>(d.Definition).Layout; int radius = layout.Length / 2;
            for (int y = 0; y < layout.Length; y++) for (int x = 0; x < layout.Length; x++)
            {
                int dx = x - radius, dy = y - radius;
                for (int turn = 0; turn < d.Rotation; turn++) (dx, dy) = (-dy, dx);
                overlay[d.Position.Offset(dx, dy)] = layout[y][x] switch { '#' => Material.Wall, '~' => Material.Water, 'C' => Material.Camp, _ => Material.Earth };
            }
        }
        // A later local layout may cross an earlier spur. Reopen access after every layout
        // is composed, preserving camp cells and letting persistent player edits win later.
        foreach (var d in places) foreach (var p in Trail(d.Anchor, d.Position))
            if (!Materials.Walkable(overlay[p])) { overlay[p] = Material.Earth; accessRepairs++; }
        return overlay;
    }
}

public sealed partial class WorldSession
{
    private static void ValidateDiscoveries(WorldPlan plan, WorldState state)
    {
        if (state.Discoveries == null || state.Discoveries.Count > 2048 || state.KnownSites == null || state.KnownSites.Any(id => id < 0 || id >= plan.Sites.Count)
            || state.Discoveries.Select(d => d?.Id).Distinct().Count() != state.Discoveries.Count
            || state.Discoveries.Select(d => d?.Position).Distinct().Count() != state.Discoveries.Count) throw new InvalidDataException("Invalid discoveries.");
        foreach (var d in state.Discoveries)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Name) || d.Name.Length > 180 || d.Definition == null
                || !ContentCatalog.Default.Entries.TryGetValue(d.Definition, out var definition) || definition is not DiscoveryDefinition template
                || !plan.Contains(d.Position.Offset(-5, -5)) || !plan.Contains(d.Position.Offset(5, 5)) || d.Position.Floor != 0
                || !plan.Contains(d.Anchor) || d.Anchor.Floor != 0 || d.Anchor.Distance(d.Position) > 110 || d.Rotation is < 0 or > 3
                || d.Context is not ("settlement" or "road" or "wilderness") || d.TownId is < 0 or >= 24 || d.SiteId is < 0 or >= 48
                || !state.Actors.Any(a => a.Id == d.WitnessId && a.Kind == ActorKind.Person && a.HomeTown == d.TownId)
                || d.Enemies == null || d.Enemies.Length != template.Enemies.Length || d.Enemies.Distinct().Count() != d.Enemies.Length
                || d.Enemies.Any(id => !state.Actors.Any(a => a.Id == id && a.Kind is ActorKind.Beast or ActorKind.Raider))
                || d.Visited && !d.Known || d.Outcome == null) throw new InvalidDataException("Invalid discovery references or placement.");
            if (template.Situation is { } situation)
            {
                if (d.Outcome.Length > 0 && (!d.Known || !d.Visited || d.Outcome is not ("fight" or "tunnel") && !situation.Approaches.Any(a => a.Action == d.Outcome))
                    || d.Outcome == "fight" && d.Enemies.Any(id => state.Actors.Single(a => a.Id == id).Life != LifeState.Dead)
                    || d.Engaged && d.Outcome is not ("" or "tunnel")) throw new InvalidDataException("Invalid discovery outcome.");
            }
            else if (d.Outcome.Length > 0 || d.Engaged) throw new InvalidDataException("This discovery has no situation outcome.");
            var cache = state.Loot.FirstOrDefault(c => c.Id == d.Id);
            if ((template.Supplies.Count > 0 || template.Rewards.Length > 0) && (cache == null || cache.Position != d.Position)
                || cache != null && cache.Taken && (template.Situation != null ? d.Outcome.Length == 0 : d.Enemies.Any(id => state.Actors.Single(a => a.Id == id).Life == LifeState.Alive))) throw new InvalidDataException("Invalid guarded reward.");
        }
        var actors = state.Actors.Select(a => a.Id).ToHashSet();
        if (state.CombatEvents == null || state.CombatEvents.Count > 128 || state.NextCombatEvent is < 1 or > 1000000000000
            || state.CombatEvents.Any(e => e == null || e.Sequence < 1 || e.Sequence >= state.NextCombatEvent || e.Round is < 0 or > 100000
                || e.Kind is not ("ability" or "damage" or "heal" or "status" or "expire" or "skip" or "fall" or "round" or "reward" or "retreat" or "injury")
                || e.Label == null || e.Detail == null || e.Label.Length > 180 || e.Detail.Length > 1000 || e.Amount is < 0 or > 10000
                || e.Source != 0 && !actors.Contains(e.Source) || e.Target != 0 && !actors.Contains(e.Target))
            || !state.CombatEvents.Select(e => e.Sequence).SequenceEqual(state.CombatEvents.Select(e => e.Sequence).Order().Distinct())) throw new InvalidDataException("Invalid combat feedback.");
        if (state.LastEncounterReward is { } reward && (reward.Gold is < 0 or > 9999 || reward.Food is < 0 or > 9999 || reward.Experience is < 0 or > 9999
            || reward.LevelUps == null || reward.LevelUps.Length > 4 || reward.LevelUps.Any(n => string.IsNullOrWhiteSpace(n) || n.Length > 180))) throw new InvalidDataException("Invalid encounter reward.");
    }
    private void PopulateDiscoveries()
    {
        State.Discoveries = DiscoveryPlacement.Generate(Plan, Content);
        for (int index = 0; index < State.Discoveries.Count; index++)
        {
            var d = State.Discoveries[index]; var definition = Content.Get<DiscoveryDefinition>(d.Definition);
            var ids = new List<long>();
            for (int i = 0; i < definition.Enemies.Length; i++)
            {
                long id = 50000 + index * 4 + i;
                var offset = new[] { (-1, 0), (1, 0), (0, 1) }[i];
                var actor = ActorFactory.Create(Content, definition.Enemies[i], Seed, id, d.Position.Offset(offset.Item1, offset.Item2), d.TownId, Plan.Towns[d.TownId].Culture, Plan.Towns[d.TownId].FactionId);
                actor.EncounterLeader = 50000 + index * 4; State.Actors.Add(actor); ids.Add(id);
            }
            d.Enemies = ids.ToArray();
            if (definition.Supplies.Count > 0 || definition.Rewards.Length > 0)
                State.Loot.Add(new() { Id = d.Id, Position = d.Position, Contents = definition.Supplies.ToDictionary(p => Content.Get<ItemDefinition>(p.Key).Stack, p => p.Value), Choices = RewardChoices(definition.Rewards, index, 5200) });
        }
        Reveal();
    }
    private string[] RewardChoices(IEnumerable<string> pool, int id, int salt)
    {
        var gear = pool.Distinct().Select(Content.Get<ItemDefinition>).OrderBy(i => WorldRandom.Hash(Seed, id, i.Id.Aggregate(0, (a, c) => unchecked(a * 31 + c)), salt)).ToArray();
        var choices = gear.GroupBy(i => i.Slot).Select(g => g.First()).Take(3).ToList();
        choices.AddRange(gear.Where(i => choices.All(c => c.Id != i.Id)).Take(3 - choices.Count));
        return choices.Select((item, index) => GenerateEquipment(item.Id, id, salt, index)).ToArray();
    }
    public string DiscoveryText(WorldDiscovery d)
    {
        var site = Plan.Sites[d.SiteId]; var town = Plan.Towns[d.TownId];
        var guardian = State.Actors.First(a => a.Id == 40000 + site.Id * 8);
        return Content.Get<DiscoveryDefinition>(d.Definition).Text.Replace("{town}", town.Name).Replace("{faction}", State.Factions[town.FactionId].Name)
            .Replace("{site}", site.Name).Replace("{guardian}", guardian.Name).Replace("{witness}", State.Actors.Single(a => a.Id == d.WitnessId).Name)
            .Replace("{direction}", Direction(d.Position, site.Center));
    }
    public static string Direction(WorldPosition from, WorldPosition to)
    {
        int dx = to.X - from.X, dy = to.Y - from.Y;
        if (Math.Abs(dx) > Math.Abs(dy) * 2) return dx > 0 ? "east" : "west";
        if (Math.Abs(dy) > Math.Abs(dx) * 2) return dy > 0 ? "south" : "north";
        return (dy >= 0 ? "south" : "north") + (dx >= 0 ? "east" : "west");
    }
    public bool DiscoveryGuarded(WorldDiscovery d) => Situation(d) != null ? d.Outcome.Length == 0 : d.Enemies.Any(id => State.Actors.Any(a => a.Id == id && a.Life == LifeState.Alive));
    public bool DiscoveryComplete(WorldDiscovery d) => d.Visited && !DiscoveryGuarded(d) && (State.Loot.FirstOrDefault(c => c.Id == d.Id)?.Taken ?? true);
    private ActionResult VisitDiscovery(WorldPosition target)
    {
        var d = State.Discoveries.FirstOrDefault(d => d.Position == target);
        if (d == null || State.Position.Distance(target) > 1 || !Chunks.Seen(target)) return new(false, "Approach an observed landmark first.");
        d.Known = true; State.KnownSites.Add(d.SiteId);
        if (!d.Visited) { d.Visited = true; Log(d.Name + ": " + DiscoveryText(d)); }
        return new(true, DiscoveryText(d));
    }
    private void ObserveDiscoveries()
    {
        foreach (var d in State.Discoveries.Where(d => !d.Known && Visible.Contains(d.Position))) d.Known = true;
        foreach (var s in Plan.Sites.Where(s => Visible.Contains(s.Center))) State.KnownSites.Add(s.Id);
    }
    private ActionResult LocalRumour(Actor actor)
    {
        var candidates = State.Discoveries.Where(d => d.TownId == actor.HomeTown).OrderBy(d => d.Known).ThenBy(d => d.Position.Distance(Plan.Towns[actor.HomeTown].Center)).ThenBy(d => d.Id).ToArray();
        if (candidates.Length == 0) return new(false, "No local places are recorded here.");
        var d = candidates.FirstOrDefault(d => d.WitnessId == actor.Id && !d.Known) ?? candidates[0]; d.Known = true;
        string text = $"{actor.Name}: {d.Name} lies {d.Position.Distance(State.Position)} tiles {Direction(State.Position, d.Position)} ({d.Position.X}, {d.Position.Y}). {DiscoveryText(d)}";
        Log(text); return new(true, text);
    }
}
