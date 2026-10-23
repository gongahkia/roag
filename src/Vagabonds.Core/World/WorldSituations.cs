using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    private int _situationCount = -1;
    private Dictionary<long, WorldDiscovery> _situationActors = [];
    private Dictionary<WorldPosition, WorldDiscovery> _situationWalls = [];
    public SituationRules? Situation(WorldDiscovery discovery) => Content.Get<DiscoveryDefinition>(discovery.Definition).Situation;
    public static WorldPosition DiscoveryCell(WorldDiscovery discovery, Cell offset)
    {
        int x = offset.X, y = offset.Y;
        for (int turn = 0; turn < discovery.Rotation; turn++) (x, y) = (-y, x);
        return discovery.Position.Offset(x, y);
    }
    public static WorldPosition? SituationFeature(WorldDiscovery discovery, Cell[] candidates)
    {
        var approach = DiscoveryPlacement.Trail(discovery.Anchor, discovery.Position).ToHashSet();
        return candidates.Select(c => (WorldPosition?)DiscoveryCell(discovery, c)).FirstOrDefault(p => !approach.Contains(p!.Value));
    }
    public WorldPosition? DiscoveryBypass(WorldDiscovery discovery) => Situation(discovery) is { } situation ? SituationFeature(discovery, situation.TunnelWalls) : null;
    private void IndexSituations()
    {
        if (_situationCount == State.Discoveries.Count) return;
        _situationCount = State.Discoveries.Count; _situationActors = []; _situationWalls = [];
        foreach (var d in State.Discoveries.Where(d => Situation(d) != null))
        {
            foreach (long id in d.Enemies) _situationActors[id] = d;
            if (DiscoveryBypass(d) is { } wall) _situationWalls[wall] = d;
        }
    }
    public WorldDiscovery? SituationForActor(Actor actor) { IndexSituations(); return _situationActors.GetValueOrDefault(actor.Id); }
    public WorldDiscovery? SituationAtWall(WorldPosition p) { IndexSituations(); return _situationWalls.GetValueOrDefault(p); }
    public string DiscoverySprite(WorldDiscovery d) => d.Outcome.Length > 0 && Situation(d) is { } situation ? situation.ResolvedSprite : Content.Get<DiscoveryDefinition>(d.Definition).Sprite!;
    public string SituationDescription(WorldDiscovery d)
    {
        if (Situation(d) is not { } situation) return "";
        if (d.Outcome.Length > 0) return d.Outcome switch
        {
            "disarm" => "The trap and its dormant ward are disabled. Recover the storeroom's supplies.",
            "negotiate" => "The toll keepers accepted your terms and opened the crossing. Your reward is available.",
            "repair" => situation.Kind == "spring" ? "Clean water flows again; the growth has receded. Recover the supplies." : "The bridge is repaired. The keepers honour their offer of supplies.",
            "tunnel" => "Your excavation opened a side route. Its supplies are accessible; any provoked guards remain dangerous.",
            _ => "The defenders have fallen. The local hazard is cleared and its supplies are available."
        };
        var wall = DiscoveryBypass(d)!.Value;
        return (situation.Kind switch
        {
            "trap" => "A trapped latch controls a dormant ward. Disarm the mechanism or force it in combat.",
            "spring" => "Poisonous growth fouls this spring. Purify it with supplies or fight the creature feeding the contamination.",
            _ => "The toll keepers want payment or a repaired bridge. Negotiate, help them or challenge them."
        }) + $" Alternatively, dig the side wall at ({wall.X}, {wall.Y}) to open a bypass. You can leave and explore elsewhere at any time.";
    }
    public string ApproachCost(DiscoveryApproach approach) => approach.Cost.Count == 0 ? "No supplies" : string.Join(", ", approach.Cost.Select(p => p.Value + " " + Content.Get<ItemDefinition>(p.Key).Name));
    public string SituationApproachReason(WorldDiscovery d, string action, bool requireAdjacent = true)
    {
        var approach = Situation(d)?.Approaches.FirstOrDefault(a => a.Action == action);
        if (approach == null) return "Choose an available approach.";
        if (!d.Known || !Chunks.Seen(d.Position)) return "Explore this place first.";
        if (IsInCombat) return "Finish the encounter first.";
        if (d.Outcome.Length > 0) return "This place is already resolved; its reward can be claimed once.";
        if (requireAdjacent && State.Position.Distance(d.Position) > 1) return "Approach this landmark first.";
        if (State.Work.Any(w => w.Target == d.Position)) return "An order is already addressing this place.";
        if (approach.Work > 0 && State.Work.Count >= 64) return "The work list is full.";
        if (approach.Cost.Any(p => Count(Content.Get<ItemDefinition>(p.Key).Stack) < p.Value)) return "Needs " + ApproachCost(approach) + ".";
        return "";
    }
    private ActionResult ApproachDiscovery(WorldPosition p, string action)
    {
        var d = State.Discoveries.FirstOrDefault(d => d.Position == p);
        if (d == null) return new(false, "Choose an observed landmark.");
        string reason = SituationApproachReason(d, action);
        if (reason.Length > 0) return new(false, reason);
        var approach = Situation(d)!.Approaches.Single(a => a.Action == action);
        var cost = approach.Cost.ToDictionary(p => Content.Get<ItemDefinition>(p.Key).Stack, p => p.Value);
        if (approach.Work > 0)
            return QueueWork(new() { Kind = WorkKind.Discovery, Definition = d.Definition, Method = action, Target = d.Position,
                Before = Chunks.At(d.Position), After = Chunks.At(d.Position), Required = approach.Work, Reserved = cost });
        foreach (var (item, amount) in cost) Add(item, -amount);
        ResolveSituation(d, action); return new(true, SituationDescription(d));
    }
    private bool SituationWorkValid(WorkOrder order) => State.Discoveries.Any(d => d.Position == order.Target && d.Definition == order.Definition && d.Outcome.Length == 0
        && Situation(d)?.Approaches.Any(a => a.Action == order.Method && a.Work > 0) == true);
    private void ResolveSituation(WorldDiscovery d, string outcome)
    {
        if (d.Outcome.Length > 0) return;
        d.Outcome = outcome; d.Visited = d.Known = true; State.KnownSites.Add(d.SiteId);
        if (outcome != "tunnel") d.Engaged = false;
        if (outcome != "tunnel" && Situation(d) is { BridgeCells.Length: > 0 } rules && SituationFeature(d, rules.BridgeCells) is { } bridge)
            Chunks.Edit(bridge, Material.Bridge);
        Log(d.Name + ": " + SituationDescription(d));
    }
    private void CompleteSituationWork(WorkOrder order) => ResolveSituation(State.Discoveries.Single(d => d.Position == order.Target), order.Method);
    private void CheckExcavatedBypass(WorkOrder order)
    {
        if (order.Before == Material.Wall && Materials.Walkable(order.After) && SituationAtWall(order.Target) is { Outcome.Length: 0 } discovery)
            ResolveSituation(discovery, "tunnel");
    }
    private void ResolveDefeatedSituations()
    {
        foreach (var d in State.Discoveries.Where(d => d.Outcome.Length == 0 && Situation(d) != null && d.Enemies.All(id => State.Actors.Single(a => a.Id == id).Life == LifeState.Dead)))
            ResolveSituation(d, "fight");
    }
}
