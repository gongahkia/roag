using Roag.Core.Content;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public sealed record ResourceView(WorldPosition Position, ResourceDefinition Definition, int Available, long RecoverIn, long RecoveryTicks = 0)
{
    public string Sprite => Available > 0 ? Definition.Sprite! : Definition.DepletedSprite;
    public string RecoveryDescription => Definition.RegrowTicks == 0 ? "Exhausted; this mineral deposit does not regrow."
        : $"Depleted; recovers in {Math.Max(1, (RecoverIn + 4) / 5)} game minutes.";
    public string Description => Available > 0 ? $"{Available}/{Definition.Capacity} gathers remain. " +
        (Definition.RegrowTicks == 0 ? "Finite deposit; no regrowth." : $"Current regrowth time: {Math.Max(1, ((RecoveryTicks > 0 ? RecoveryTicks : Definition.RegrowTicks) + WorldSession.TicksPerHour - 1) / WorldSession.TicksPerHour)} game hours after the last gather.") : RecoveryDescription;
}

public sealed partial class WorldSession
{
    private Dictionary<(Material, bool, Biome), (ResourceDefinition Definition, int Salt)[]>? _resourceCandidates;
    private void PrepareResourceCandidates()
    {
        var candidates = new Dictionary<(Material, bool, Biome), List<(ResourceDefinition, int)>>();
        foreach (var definition in Content.Game.Resources.Select(Content.Get<ResourceDefinition>).OrderBy(d => d.Id, StringComparer.Ordinal))
        {
            int salt = 137; foreach (char c in definition.Id) salt = unchecked(salt * 31 + c);
            foreach (var biome in definition.Biomes.Distinct())
            {
                var key = (definition.Terrain, definition.Underground, biome);
                if (!candidates.TryGetValue(key, out var group)) candidates[key] = group = [];
                group.Add((definition, salt));
            }
        }
        _resourceCandidates = candidates.ToDictionary(p => p.Key, p => p.Value.ToArray());
    }
    /// <summary>Pure coordinate query. Only harvested stock creates saved chunk data; looking never consumes RNG.</summary>
    public ResourceView? ResourceAt(WorldPosition p)
    {
        if (!Plan.Contains(p) || Chunks.IsEdited(p)) return null;
        var terrain = Chunks.At(p);
        if (terrain is not (Material.Tree or Material.Water or Material.Rock or Material.Grass or Material.Floor)) return null;
        if (ObjectAt(p, terrain) is { Solid: true } or { Kind: "discovery" or "loot" }) return null;
        if (_resourceCandidates == null) PrepareResourceCandidates();
        var biome = Chunks.Get(p.Chunk).Biomes[p.LocalIndex];
        if (!_resourceCandidates!.TryGetValue((terrain, p.Floor > 0, biome), out var candidates)) return null;
        foreach (var (definition, salt) in candidates)
        {
            if (WorldRandom.Hash(Seed, p.X, p.Y, salt ^ p.Floor) % 1000 >= definition.ChancePerThousand) continue;
            // Fish mark banks, not the middle of impassable lakes. A miner may tunnel to buried ore.
            if (terrain == Material.Water)
            {
                bool bank = false;
                foreach (var (dx, dy) in Directions) if (Materials.Walkable(Chunks.At(p.Offset(dx, dy)))) { bank = true; break; }
                if (!bank) continue;
            }
            var stock = Chunks.ResourceStockAt(p);
            bool recovered = stock == null || stock.RecoverAt > 0 && State.Tick >= stock.RecoverAt;
            return new(p, definition, recovered ? definition.Capacity : stock!.Remaining,
                stock == null || recovered ? 0 : Math.Max(0, stock.RecoverAt - State.Tick), RecoveryDelay(definition.RegrowTicks, WeatherAt(p).RecoveryPercent));
        }
        return null;
    }
    public string ResourceYieldDescription(ResourceDefinition definition) => string.Join(", ", definition.Yield.Select(y => $"{y.Value} {Content.Get<ItemDefinition>(y.Key).Name}"));
    private ActionResult QueueHarvest(WorldPosition p, bool designated = false)
    {
        string reason = TargetBlockReason(WorldCommandKind.Harvest, p, requireAdjacent: !designated);
        if (reason.Length > 0) return new(false, reason);
        var node = ResourceAt(p)!;
        return QueueWork(new() { Kind = WorkKind.Harvest, Definition = node.Definition.Id, Target = p,
            Before = Chunks.At(p), After = Chunks.At(p), Required = node.Definition.Work,
            Yield = node.Definition.Yield.ToDictionary(y => Content.Get<ItemDefinition>(y.Key).Stack, y => y.Value) });
    }
    private void HarvestResource(WorldPosition p)
    {
        var node = ResourceAt(p)!;
        int remaining = node.Available - 1;
        // Recovery begins only on exhaustion. Partial stock never duplicates through repeated inspections.
        bool recovering = remaining == 0 && node.Definition.RegrowTicks > 0;
        int percent = recovering ? WeatherAt(p).RecoveryPercent : 0;
        long recoverAt = recovering ? State.Tick + RecoveryDelay(node.Definition.RegrowTicks, percent) : 0;
        Chunks.Delta(p.Chunk).Resources[p.LocalIndex] = new(node.Definition.Id, remaining, recoverAt, recovering ? State.Tick : 0, percent);
        PressureHabitat(p, 8);
    }
    public double WorkMultiplier(Actor actor, WorkOrder order)
    {
        string activity = order.Kind == WorkKind.Discovery ? order.Method : order.Kind == WorkKind.Harvest ? Content.Get<ResourceDefinition>(order.Definition).Activity : order.Kind.ToString().ToLowerInvariant();
        return WorkActivityMultiplier(actor, activity) * (order.Kind is WorkKind.Forage or WorkKind.Harvest or WorkKind.Dig ? ClimateGathering(order.Target) : 1);
    }
    private double WorkActivityMultiplier(Actor actor, string activity)
    {
        int bonus = actor.Equipment.Values.Sum(e => Item(e.Item).WorkBonuses.GetValueOrDefault(activity));
        return 1 + Math.Min(200, bonus + EquipmentEffectPower(actor, activity)) / 100.0;
    }
}
