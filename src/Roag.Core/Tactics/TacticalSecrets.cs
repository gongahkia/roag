using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum DungeonSecretKind { CrackedCache, MossSigil }
public sealed record DungeonSecret(long Id, string Marker, DungeonSecretKind Kind, WorldPosition Position,
    WorldPosition? Wall = null, WorldPosition? Cache = null, bool Revealed = false, long Reward = 0)
{
    public WorldPosition Clue => Wall ?? Position;
}

public sealed class TacticalSecretState
{
    public long NextSecret { get; set; } = 1;
    public List<DungeonSecret> Nodes { get; set; } = [];

    internal void Validate(WorldPlan plan, WorldState world, ContentCatalog? content)
    {
        if (NextSecret <= 0 || Nodes == null || Nodes.Any(n => n == null) || Nodes.Count > 64
            || Nodes.Select(n => n.Id).Distinct().Count() != Nodes.Count || Nodes.Select(n => n.Marker).Distinct(StringComparer.Ordinal).Count() != Nodes.Count
            || Nodes.Select(n => n.Clue).Distinct().Count() != Nodes.Count)
            throw new InvalidDataException("Invalid native secret registry.");
        using var chunks = Nodes.Count == 0 ? null : new ChunkCache(plan, world, content ?? ContentCatalog.Default);
        long previous = 0;
        foreach (var node in Nodes)
        {
            if (node.Id <= previous || node.Id >= NextSecret || string.IsNullOrWhiteSpace(node.Marker) || !Enum.IsDefined(node.Kind)
                || !plan.Contains(node.Position) || world.Dungeon != null && !world.Dungeon.Contains(node.Position)
                || node.Revealed != (node.Reward > 0) || node.Reward < 0
                || node.Kind == DungeonSecretKind.MossSigil && (node.Wall != null || node.Cache != null || node.Reward >= world.Tactics!.Loot.NextDrop))
                throw new InvalidDataException("Invalid native secret identity or reward receipt.");
            if (node.Kind == DungeonSecretKind.CrackedCache)
            {
                if (node.Wall is not { } wall || node.Cache is not { } cache || wall.Floor != node.Position.Floor || cache.Floor != wall.Floor
                    || wall.Distance(cache) != 1 || !plan.Contains(wall) || !plan.Contains(cache)
                    || world.Dungeon != null && (!world.Dungeon.Contains(wall) || !world.Dungeon.Contains(cache))
                    || node.Revealed && !world.Tactics!.Chests.Chests.Any(c => c.Id == node.Reward && c.Marker == "secret:" + node.Marker
                        && c.Definition == "core:chest/free_elite" && c.Position == cache)
                    || !node.Revealed && (chunks!.At(wall) != Material.Wall || chunks.At(cache) != Material.Wall))
                    throw new InvalidDataException("Invalid cracked-cache wall, hidden alcove or chest receipt.");
            }
            previous = node.Id;
        }
    }
}

public sealed partial class TacticalCombat
{
    public DungeonSecret SpawnSecret(DungeonSecretKind kind, string marker, WorldPosition position,
        WorldPosition? wall = null, WorldPosition? cache = null)
    {
        if (!Enum.IsDefined(kind) || string.IsNullOrWhiteSpace(marker) || !_world.Plan.Contains(position)
            || _world.State.Dungeon != null && !_world.State.Dungeon.Contains(position)
            || State.Secrets.Nodes.Any(n => n.Marker == marker || n.Clue == (wall ?? position)))
            throw new ArgumentException("A secret needs its own stable clue on the physical floor.");
        if (kind == DungeonSecretKind.CrackedCache)
        {
            if (wall is not { } w || cache is not { } c || w.Floor != position.Floor || c.Floor != w.Floor || w.Distance(c) != 1
                || !_world.Plan.Contains(w) || !_world.Plan.Contains(c) || _world.State.Dungeon != null && (!_world.State.Dungeon.Contains(w) || !_world.State.Dungeon.Contains(c))
                || _world.Chunks.At(w) != Material.Wall || _world.Chunks.At(c) != Material.Wall)
                throw new ArgumentException("A cracked secret preserves its wall and hidden one-cell alcove until an explicit sword strike.");
        }
        else if (wall != null || cache != null || !_world.CanEnter(position) || ProtectedForcedLanding(position)
            || _world.ObjectAt(position) != null || _world.DungeonTerrainAt(position) != null
            || State.OptionalTerrain.Nodes.Any(n => n.Position == position))
            throw new ArgumentException("A movement sigil needs clear plain ground without replacing another fixture.");
        var node = new DungeonSecret(State.Secrets.NextSecret++, marker, kind, position, wall, cache);
        State.Secrets.Nodes.Add(node); return node;
    }

    private void ResolveSecretEvent(BoonEvent e)
    {
        if (e.SourceId is not > 0 || !Friendly(Actor(e.SourceId.Value)) || e.Kind is not (BoonTrigger.AttackPerformed or BoonTrigger.TileEntered)) return;
        for (int i = 0; i < State.Secrets.Nodes.Count; i++)
        {
            var secret = State.Secrets.Nodes[i]; if (secret.Revealed) continue;
            bool match = secret.Kind == DungeonSecretKind.CrackedCache
                ? e.Kind == BoonTrigger.AttackPerformed && !e.Bump && e.AbilityId == "core:ability/basic_attack" && e.TargetCells.Contains(secret.Wall!.Value)
                : e.Kind == BoonTrigger.TileEntered && e.Position == secret.Position;
            if (!match) continue;
            long action = e.RootActionId ?? 0; var owner = Actor(e.SourceId.Value);
            if (secret.Kind == DungeonSecretKind.CrackedCache)
            { _world.Chunks.Edit(secret.Wall!.Value, Material.Floor); _world.Chunks.Edit(secret.Cache!.Value, Material.Floor); }
            else
            { _world.Chunks.Edit(secret.Position, Material.Floor); _world.State.Dungeon?.Terrain.Remove(Roag.Core.Dungeons.DungeonRunState.CellKey(secret.Position)); }
            // Reveal before spawning so later placement cannot see an unopened wall.
            State.Secrets.Nodes[i] = secret with { Revealed = true };
            Emit("SecretDiscovered", owner.Id, 0, action, secret.Position, ability: secret.Kind.ToString());
            long reward;
            if (secret.Kind == DungeonSecretKind.CrackedCache)
            {
                reward = SpawnBoonChest("core:chest/free_elite", "secret:" + secret.Marker, secret.Cache!.Value).Id;
                Emit("SecretRewardSpawned", owner.Id, 0, action, secret.Cache.Value, ability: "free_chest");
            }
            else
            { reward = SpawnLoot(GroundLootKind.Currency, 4, secret.Position, owner.Id, action).Id; CollectLootAt(owner, secret.Position, action); }
            State.Secrets.Nodes[i] = State.Secrets.Nodes[i] with { Reward = reward };
            _world.Reveal();
        }
    }
}
