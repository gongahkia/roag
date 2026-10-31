using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record WorldObject(string Name, string Sprite, string Kind, bool Solid, bool Removable = false);

public sealed partial class WorldSession
{
    private int _objectCount = -1;
    private Dictionary<WorldPosition, WorldDiscovery> _discoveryObjects = [];
    private Dictionary<WorldPosition, LootCache> _cacheObjects = [];
    private ChunkKey? _collisionChunk;
    private static readonly WorldObject Shrub = new("Shrub", "shrub", "decoration", false),
        Bones = new("Scattered bones", "bones", "decoration", false), Barrel = new("Barrel", "barrel", "decoration", true, true);

    private static readonly WorldObject Bulwark = new("Geomancer bulwark", "boulder", "geomancer-bulwark", true);
    public WorldObject? ObjectAt(WorldPosition p) => TacticalBlockAt(p) ? Bulwark : ObjectAt(p, null);
    internal bool TacticalBlockAt(WorldPosition p, long exceptOwner = 0)
    {
        // Collision is a hot path, including worlds without native actors. Avoid a
        // capturing predicate allocation on every expanded pathfinding tile.
        if (State.Tactics == null) return false;
        // ActorCollection exposes an interface enumerator, so index iteration also
        // avoids boxing that enumerator in native worlds.
        for (int i = 0; i < State.Actors.Count; i++)
        {
            var actor = State.Actors[i];
            if (actor.Id != exceptOwner && actor.Tactics?.Resources.Bulwark?.Position == p) return true;
        }
        return false;
    }
    private WorldObject? ObjectAt(WorldPosition p, Material? terrain)
    {
        if (!Plan.Contains(p)) return null;
        if (TryExplicitObject(p, out var placed)) return placed;
        return GeneratedObjectAt(Plan, Chunks, p, terrain);
    }
    private static WorldObject? GeneratedObjectAt(WorldPlan plan, ChunkCache chunks, WorldPosition p, Material? terrain)
    {
        if (chunks.IsEdited(p) || (terrain ?? chunks.At(p)) is not (Material.Grass or Material.Earth or Material.Floor)) return null;
        int culture = chunks.Get(p.Chunk).Cultures[p.LocalIndex];
        if (WorldRandom.Hash(plan.Seed, p.X, p.Y, 999) % (culture < 0 ? 33 : 61) != 0) return null;
        return culture < 0 ? Shrub : p.Floor > 0 ? Bones : Barrel;
    }
    private static bool DiscoverySolid(ContentCatalog content, WorldDiscovery discovery)
        => content.Get<DiscoveryDefinition>(discovery.Definition).Sprite is not ("animal-bones" or "grave-slab");
    private static bool LootSolid(LootCache cache) => !cache.Taken && cache.SourceActor == 0 && !IsWorkSupplyCache(cache);

    /// <summary>Read-only saved-world collision, using the same prop rules as the live session.</summary>
    internal static bool SnapshotProjectileOpen(WorldPlan plan, WorldState state, ContentCatalog content, ChunkCache chunks, WorldPosition p)
    {
        if (!plan.Contains(p) || Materials.Opaque(chunks.At(p))
            || state.Actors.Any(a => a.Tactics?.Resources.Bulwark?.Position == p)) return false;
        var discovery = state.Discoveries.FirstOrDefault(d => d.Position == p);
        if (discovery != null) return !DiscoverySolid(content, discovery);
        var cache = state.Loot.FirstOrDefault(c => c.Position == p);
        if (cache != null) return !LootSolid(cache);
        return GeneratedObjectAt(plan, chunks, p, null) is not { Solid: true };
    }
    private bool TryExplicitObject(WorldPosition p, out WorldObject? placed)
    {
        if (_objectCount != State.Discoveries.Count + State.Loot.Count)
        {
            _objectCount = State.Discoveries.Count + State.Loot.Count;
            _discoveryObjects = State.Discoveries.ToDictionary(d => d.Position);
            _cacheObjects = State.Loot.GroupBy(c => c.Position).ToDictionary(g => g.Key, g => g.First());
        }
        if (_discoveryObjects.TryGetValue(p, out var discovery))
        {
            placed = new(discovery.Name, DiscoverySprite(discovery), "discovery", DiscoverySolid(Content, discovery)); return true;
        }
        if (_cacheObjects.TryGetValue(p, out var cache))
        { placed = cache.Taken ? null : new(LootName(cache), LootSprite(cache), "loot", LootSolid(cache)); return true; }
        placed = null; return false;
    }

    public Actor? OccupantAt(WorldPosition p, long except = 0, bool observedOnly = false) =>
        observedOnly && !Visible.Contains(p) ? null : ActorIndex.Occupant(p, except);

    public bool GroundOpen(WorldPosition p) => GroundOpen(p, 0);
    internal bool GroundOpen(WorldPosition p, long ignoredBulwarkOwner)
    {
        if (!Plan.Contains(p) || TacticalBlockAt(p, ignoredBulwarkOwner)) return false;
        var terrain = Chunks.At(p);
        return Materials.Walkable(terrain) && terrain != Material.Camp && ObjectAt(p, terrain) is not { Solid: true };
    }
    public bool CanEnter(WorldPosition p, long actorId = 0, bool observedOnly = false) => GroundOpen(p)
        && (actorId == 0 || ActorById(actorId).Tactics?.Enemy?.PursuitBoundary is not { } boundary || boundary.Contains(p))
        && OccupantAt(p, actorId, observedOnly) == null;
    public string MovementBlockReason(WorldPosition p, long actorId = 0, bool observedOnly = false)
    {
        if (!Plan.Contains(p)) return "The world ends here.";
        if (observedOnly && !Chunks.Seen(p)) return "Explore this ground first.";
        if (OccupantAt(p, actorId, observedOnly) is { } actor) return (Visible.Contains(p) ? actor.Name : "Someone") + " occupies this tile. Choose a free route or interact from nearby.";
        if (ObjectAt(p) is { Solid: true } prop) return prop.Name + " blocks this tile. " + (prop.Removable ? "Right-click to dismantle it." : "Interact from an adjacent tile.");
        return Chunks.At(p) switch
        {
            Material.Water => "Water blocks travel. Right-click adjacent ground to build a bridge.",
            Material.Rock or Material.Wall or Material.Barricade or Material.Tree => "This terrain blocks travel. Right-click to dig or clear it.",
            Material.Storage or Material.Camp => "This structure occupies the tile. Interact from beside it.", _ => ""
        };
    }

    public ulong OccupancyFingerprint()
    {
        ulong hash = 1469598103934665603;
        foreach (var actor in State.Actors.Where(a => a.Life != LifeState.Dead && Visible.Contains(a.Position)))
        { hash = unchecked((hash ^ (ulong)actor.Id) * 1099511628211); hash = unchecked((hash ^ (ulong)(actor.Position.X * 4096L + actor.Position.Y)) * 1099511628211); }
        foreach (var cache in State.Loot.Where(c => c.Taken)) hash = unchecked((hash ^ (ulong)(cache.Position.X * 4096L + cache.Position.Y)) * 1099511628211);
        foreach (var actor in State.Actors.Where(a => a.Tactics?.Resources.Bulwark is { } block && Visible.Contains(block.Position)))
        {
            var p = actor.Tactics!.Resources.Bulwark!.Position;
            hash = unchecked((hash ^ (ulong)actor.Id) * 1099511628211);
            hash = unchecked((hash ^ (ulong)(p.X * 4096L + p.Y)) * 1099511628211);
        }
        return hash;
    }

    private bool TryFreeCell(WorldPosition anchor, HashSet<WorldPosition> occupied, out WorldPosition cell, int radius = 12)
    {
        // Placement only (spawning/old save repair); walking always uses connected paths.
        for (int distance = 0; distance <= radius; distance++)
            for (int dy = -distance; dy <= distance; dy++)
                foreach (int dx in new[] { distance - Math.Abs(dy), -(distance - Math.Abs(dy)) }.Distinct())
                {
                    var p = anchor.Offset(dx, dy);
                    if (occupied.Contains(p) || !GroundOpen(p)) continue;
                    cell = p; return true;
                }
        cell = anchor; return false;
    }

    private void PrepareLocalCollision(bool force = false)
    {
        if (!force && _collisionChunk == State.Position.Chunk) return;
        _collisionChunk = State.Position.Chunk;
        var local = State.Actors.Where(a => a.Life != LifeState.Dead && a.Position.Distance(State.Position) <= 100)
            .OrderBy(a => a.Recruited ? 0 : 1).ThenBy(a => State.Party.IndexOf(a.Id)).ThenBy(a => a.Id).ToArray();
        var localIds = local.Select(a => a.Id).ToHashSet();
        var occupied = State.Actors.Where(a => a.Life != LifeState.Dead && !localIds.Contains(a.Id)).Select(a => a.Position).ToHashSet();
        foreach (var actor in local)
        {
            if (occupied.Contains(actor.Position) || !GroundOpen(actor.Position))
            {
                if (!TryFreeCell(actor.Position, occupied, out var free)) throw new InvalidDataException("No free cell for " + actor.Name + ".");
                actor.Position = free;
                if (State.Party.FirstOrDefault() == actor.Id) State.Position = free;
            }
            occupied.Add(actor.Position);
        }
    }

    private void MoveCaravanSafely(Actor caravan, WorldPosition desired)
    {
        if (caravan.Position.Distance(State.Position) <= 120 || desired.Distance(State.Position) <= 120)
        {
            // Commit one cardinal step per tick so interpolation also follows the path.
            var step = LocalStep(caravan.Position, desired, caravan.Id);
            if (step != caravan.Position && CanEnter(step, caravan.Id)) caravan.Position = step;
        }
        else if (OccupantAt(desired, caravan.Id) == null) caravan.Position = desired;
    }
}
