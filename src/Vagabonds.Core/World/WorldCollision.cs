using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed record WorldObject(string Name, string Sprite, string Kind, bool Solid, bool Removable = false);

public sealed partial class WorldSession
{
    private int _objectCount = -1;
    private Dictionary<WorldPosition, WorldDiscovery> _discoveryObjects = [];
    private Dictionary<WorldPosition, LootCache> _cacheObjects = [];
    private ChunkKey? _collisionChunk;
    private static readonly WorldObject Shrub = new("Shrub", "shrub", "decoration", false),
        Bones = new("Scattered bones", "bones", "decoration", false), Barrel = new("Barrel", "barrel", "decoration", true, true);

    public WorldObject? ObjectAt(WorldPosition p) => ObjectAt(p, null);
    private WorldObject? ObjectAt(WorldPosition p, Material? terrain)
    {
        if (!Plan.Contains(p)) return null;
        if (_objectCount != State.Discoveries.Count + State.Loot.Count)
        {
            _objectCount = State.Discoveries.Count + State.Loot.Count;
            _discoveryObjects = State.Discoveries.ToDictionary(d => d.Position);
            _cacheObjects = State.Loot.GroupBy(c => c.Position).ToDictionary(g => g.Key, g => g.First());
        }
        if (_discoveryObjects.TryGetValue(p, out var discovery))
        {
            var definition = Content.Get<DiscoveryDefinition>(discovery.Definition);
            return new(discovery.Name, DiscoverySprite(discovery), "discovery", definition.Sprite is not ("animal-bones" or "grave-slab"));
        }
        if (_cacheObjects.TryGetValue(p, out var cache)) return cache.Taken ? null : new(LootName(cache), LootSprite(cache), "loot", cache.SourceActor == 0);
        if (Chunks.IsEdited(p) || (terrain ?? Chunks.At(p)) is not (Material.Grass or Material.Earth or Material.Floor)) return null;
        int culture = Chunks.Get(p.Chunk).Cultures[p.LocalIndex];
        if (WorldRandom.Hash(Seed, p.X, p.Y, 999) % (culture < 0 ? 33 : 61) != 0) return null;
        return culture < 0 ? Shrub : p.Floor > 0 ? Bones : Barrel;
    }

    public Actor? OccupantAt(WorldPosition p, long except = 0, bool observedOnly = false) =>
        observedOnly && !Visible.Contains(p) ? null : State.Actors.FirstOrDefault(a => a.Id != except && a.Life != LifeState.Dead && a.Position == p);

    public bool GroundOpen(WorldPosition p)
    {
        if (!Plan.Contains(p)) return false;
        var terrain = Chunks.At(p);
        return Materials.Walkable(terrain) && terrain != Material.Camp && ObjectAt(p, terrain) is not { Solid: true };
    }
    public bool CanEnter(WorldPosition p, long actorId = 0, bool observedOnly = false) => GroundOpen(p) && OccupantAt(p, actorId, observedOnly) == null;
    public string MovementBlockReason(WorldPosition p, long actorId = 0, bool observedOnly = false)
    {
        if (!Plan.Contains(p)) return "The world ends here.";
        if (observedOnly && !Chunks.Seen(p)) return "Explore this ground first.";
        if (OccupantAt(p, actorId, observedOnly) is { } actor) return (Visible.Contains(p) ? actor.Name : "Someone") + " occupies this tile. Choose a free route or interact from nearby.";
        if (ObjectAt(p) is { Solid: true } prop) return prop.Name + " blocks this tile. " + (prop.Removable ? "Right-click to dismantle it." : "Interact from an adjacent tile.");
        return Chunks.At(p) switch
        {
            Material.Water => "Water blocks travel. Right-click adjacent ground to build a bridge.",
            Material.Rock or Material.Wall or Material.Tree => "This terrain blocks travel. Right-click to dig or clear it.",
            Material.Storage or Material.Camp => "This structure occupies the tile. Interact from beside it.", _ => ""
        };
    }

    public ulong OccupancyFingerprint()
    {
        ulong hash = 1469598103934665603;
        foreach (var actor in State.Actors.Where(a => a.Life != LifeState.Dead && Visible.Contains(a.Position)))
        { hash = unchecked((hash ^ (ulong)actor.Id) * 1099511628211); hash = unchecked((hash ^ (ulong)(actor.Position.X * 4096L + actor.Position.Y)) * 1099511628211); }
        foreach (var cache in State.Loot.Where(c => c.Taken)) hash = unchecked((hash ^ (ulong)(cache.Position.X * 4096L + cache.Position.Y)) * 1099511628211);
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
