using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum DungeonRoomFeatureKind { Ore, Brazier, Ward, Pillar, Overgrowth, Mushroom, MineRail, Crystal }
public sealed record DungeonRoomFeature(long Id, string Marker, DungeonRoomFeatureKind Kind, WorldPosition Position,
    bool Used = false, long LitUntil = 0, int Variant = 1);

public sealed class DungeonRoomFeatureState
{
    public long NextFeature { get; set; } = 1;
    public List<DungeonRoomFeature> Nodes { get; set; } = [];

    public bool MistCleared(WorldPosition position, long clock)
    {
        for (int i = 0; i < Nodes.Count; i++)
            if (Nodes[i] is { Kind: DungeonRoomFeatureKind.Brazier } fire && fire.LitUntil > clock
                && fire.Position.Distance(position) <= 2) return true;
        return false;
    }

    internal void Validate(WorldPlan plan, WorldState world, ContentCatalog? content)
    {
        if (NextFeature <= 0 || Nodes == null || Nodes.Count > 1024 || Nodes.Any(n => n == null)
            || Nodes.Select(n => n.Id).Distinct().Count() != Nodes.Count
            || Nodes.Select(n => n.Marker).Distinct(StringComparer.Ordinal).Count() != Nodes.Count
            || Nodes.Select(n => n.Position).Distinct().Count() != Nodes.Count)
            throw new InvalidDataException("Invalid finite room feature registry.");
        using var chunks = Nodes.Count == 0 ? null : new ChunkCache(plan, world, content ?? ContentCatalog.Default);
        long previous = 0;
        foreach (var node in Nodes)
        {
            if (node.Id <= previous || node.Id >= NextFeature || string.IsNullOrWhiteSpace(node.Marker) || !Enum.IsDefined(node.Kind)
                || !plan.Contains(node.Position) || world.Dungeon != null && !world.Dungeon.Contains(node.Position)
                || node.Variant is < 1 or > 2 || node.LitUntil < 0
                || node.Kind != DungeonRoomFeatureKind.Brazier && node.LitUntil != 0
                || node.Used && node.Kind is not (DungeonRoomFeatureKind.Ore or DungeonRoomFeatureKind.Ward)
                || chunks!.At(node.Position) != (node.Kind == DungeonRoomFeatureKind.Pillar ? Material.Pillar : Material.Floor))
                throw new InvalidDataException("Invalid room feature identity, physical tile or finite state.");
            previous = node.Id;
        }
    }
}

public sealed partial class TacticalCombat
{
    public bool CanPlaceRoomFeature(DungeonRoomFeatureKind kind, string marker, WorldPosition position)
        => Enum.IsDefined(kind) && !string.IsNullOrWhiteSpace(marker) && _world.CanEnter(position)
            && _world.Chunks.At(position) == Material.Floor && _world.DungeonTerrainAt(position) == null
            && _world.ObjectAt(position) == null && !ProtectedForcedLanding(position)
            && !State.Chests.Chests.Any(n => n.Position == position)
            && !State.OptionalTerrain.Nodes.Any(n => n.Position == position)
            && !State.RoomFeatures.Nodes.Any(n => n.Position == position || n.Marker == marker)
            && (kind != DungeonRoomFeatureKind.Pillar || TerrainRemovalPreservesRoutes(position));

    public DungeonRoomFeature SpawnRoomFeature(DungeonRoomFeatureKind kind, string marker, WorldPosition position, int variant = 1)
    {
        if (variant is < 1 or > 2 || !CanPlaceRoomFeature(kind, marker, position))
            throw new ArgumentException("A room feature needs its own clear plain ground without sealing an accessible route.");
        var node = new DungeonRoomFeature(State.RoomFeatures.NextFeature++, marker, kind, position, Variant: variant);
        State.RoomFeatures.Nodes.Add(node);
        if (kind == DungeonRoomFeatureKind.Pillar) { _world.Chunks.Edit(position, Material.Pillar); _world.Reveal(); }
        return node;
    }

    private void ResolveRoomFeatureEvent(BoonEvent e)
    {
        if (e.Kind is not (BoonTrigger.TileEntered or BoonTrigger.AttackPerformed)) return;
        bool member = e.SourceId is > 0 && Friendly(Actor(e.SourceId.Value)); long action = e.RootActionId ?? 0;
        for (int i = 0; i < State.RoomFeatures.Nodes.Count; i++)
        {
            var node = State.RoomFeatures.Nodes[i];
            if (node.Kind == DungeonRoomFeatureKind.Ore && !node.Used && e.Kind == BoonTrigger.AttackPerformed && e.TargetCells.Contains(node.Position))
            {
                State.RoomFeatures.Nodes[i] = node with { Used = true };
                Emit("OreBroken", e.SourceId ?? 0, 0, action, node.Position);
                SpawnLoot(GroundLootKind.Currency, 3, node.Position, e.SourceId ?? 0, action);
            }
            else if (member && e.Kind == BoonTrigger.TileEntered && e.Position == node.Position)
            {
                if (node.Kind == DungeonRoomFeatureKind.Ward && !node.Used)
                {
                    State.RoomFeatures.Nodes[i] = node with { Used = true };
                    // Unlike charm-well choices, a found ward does not erase an existing focus.
                    Actor(e.SourceId!.Value).Tactics!.Supplies.WardActions = 4;
                    Emit("WardFlaskCollected", e.SourceId.Value, 0, action, node.Position);
                }
                else if (node.Kind == DungeonRoomFeatureKind.Brazier)
                {
                    State.RoomFeatures.Nodes[i] = node with { LitUntil = checked(Clock + 300) };
                    Emit("BrazierLit", e.SourceId!.Value, 0, action, node.Position); _world.Reveal();
                }
            }
        }
    }

    public static string RoomFeatureName(DungeonRoomFeatureKind kind) => kind switch
    {
        DungeonRoomFeatureKind.Ore => "Breakable ore", DungeonRoomFeatureKind.Brazier => "Mist-clearing brazier",
        DungeonRoomFeatureKind.Ward => "Ward flask", DungeonRoomFeatureKind.Pillar => "Stone pillar",
        DungeonRoomFeatureKind.Overgrowth => "Overgrowth", DungeonRoomFeatureKind.Mushroom => "Mushrooms",
        DungeonRoomFeatureKind.MineRail => "Mine rail", DungeonRoomFeatureKind.Crystal => "Crystal growth",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
