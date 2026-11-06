using Roag.Core.Content;
using Roag.Core.Dungeons;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum DungeonVaultOutcome { Cache, Clue, Boon, Ambush }
public sealed record DungeonVault(long Id, string Marker, WorldPosition Position, WorldPosition KeyPosition, int Seed,
    bool KeyCollected = false, bool Opened = false, DungeonVaultOutcome? Outcome = null, Facing? Clue = null,
    long CurrencyDrop = 0, long HealthDrop = 0, long Chest = 0, long Ambush = 0);

public sealed class DungeonVaultState
{
    public long NextVault { get; set; } = 1;
    public StateList<DungeonVault> Nodes { get; set; } = [];

    internal void Validate(WorldPlan plan, WorldState world, ContentCatalog? content)
    {
        if (NextVault <= 0 || Nodes == null || Nodes.Any(n => n == null)
            || Nodes.Select(n => n.Id).Distinct().Count() != Nodes.Count || Nodes.Select(n => n.Marker).Distinct(StringComparer.Ordinal).Count() != Nodes.Count
            || Nodes.Select(n => WorldSession.SnapshotDungeonFloorAt(world, n.Position)).Distinct().Count() != Nodes.Count)
            throw new InvalidDataException("Invalid keyed vault registry.");
        var dropReceipts = Nodes.SelectMany(v => new[] { v.CurrencyDrop, v.HealthDrop }).Where(id => id > 0).ToArray();
        var ambushReceipts = Nodes.Where(v => v.Ambush > 0).Select(v => v.Ambush).ToArray();
        if (dropReceipts.Distinct().Count() != dropReceipts.Length || ambushReceipts.Distinct().Count() != ambushReceipts.Length)
            throw new InvalidDataException("Vault rewards cannot share historical receipts.");
        using var chunks = Nodes.Count == 0 ? null : new ChunkCache(plan, world, content ?? ContentCatalog.Default);
        var state = world.Tactics!; long previous = 0;
        foreach (var vault in Nodes)
        {
            var ambush = world.Actors.FirstOrDefault(a => a.Id == vault.Ambush);
            var floor = WorldSession.SnapshotDungeonFloorAt(world, vault.Position);
            var biome = floor?.Biome;
            string enemy = DungeonCatalog.Default.Biomes.FirstOrDefault(b => b.Id == biome)?.EnemyIds[0] ?? "core:enemy/mossbound_guard";
            if (vault.Id <= previous || vault.Id >= NextVault || string.IsNullOrWhiteSpace(vault.Marker)
                || !plan.Contains(vault.Position) || !plan.Contains(vault.KeyPosition) || vault.Position.Floor != vault.KeyPosition.Floor
                || vault.Position.Distance(vault.KeyPosition) < 8 || floor == null || !floor.Contains(vault.KeyPosition)
                || chunks!.At(vault.Position) != Material.Floor || chunks.At(vault.KeyPosition) != Material.Floor
                || vault.Seed <= 0 || vault.Seed >= RoagRandom.Modulus || vault.Opened != vault.Outcome.HasValue || vault.Opened && !vault.KeyCollected
                || vault.Outcome is { } outcome && (!Enum.IsDefined(outcome) || (int)outcome != vault.Seed % 4)
                || vault.Clue is { } clue && (!Enum.IsDefined(clue) || !AttackGeometry.Cardinal(clue) || vault.Outcome != DungeonVaultOutcome.Clue)
                || vault.CurrencyDrop < 0 || vault.HealthDrop < 0 || vault.Chest < 0 || vault.Ambush < 0
                || (vault.CurrencyDrop > 0) != (vault.Outcome is DungeonVaultOutcome.Cache or DungeonVaultOutcome.Clue)
                || (vault.HealthDrop > 0) != (vault.Outcome == DungeonVaultOutcome.Cache)
                || (vault.Chest > 0) != (vault.Outcome == DungeonVaultOutcome.Boon)
                || vault.Ambush > 0 && (vault.Outcome != DungeonVaultOutcome.Ambush || vault.Ambush >= world.NextActorId)
                || ambush != null && (ambush.Tactics?.Enemy == null || ambush.Tactics.DefinitionId != enemy)
                || vault.CurrencyDrop >= state.Loot.NextDrop || vault.HealthDrop >= state.Loot.NextDrop
                || vault.CurrencyDrop > 0 && vault.CurrencyDrop == vault.HealthDrop
                || vault.CurrencyDrop > 0 && state.Loot.Drops.Any(d => d.Id == vault.CurrencyDrop
                    && (d.Kind != GroundLootKind.Currency || d.Amount != (vault.Outcome == DungeonVaultOutcome.Cache ? 5 : 4) || d.DefeatedActor != 0))
                || vault.HealthDrop > 0 && state.Loot.Drops.Any(d => d.Id == vault.HealthDrop && (d.Kind != GroundLootKind.Health || d.Amount != 10 || d.DefeatedActor != 0))
                || vault.Chest > 0 && !state.Chests.Chests.Any(c => c.Id == vault.Chest && c.Position == vault.Position
                    && c.Marker == "review:vault:" + vault.Marker && c.Definition == "core:chest/free_elite"))
                throw new InvalidDataException("Invalid vault key, seeded outcome, physical tile or one-time reward receipt.");
            previous = vault.Id;
        }
    }
}

public sealed partial class TacticalCombat
{
    public bool CanPlaceVault(string marker, WorldPosition position, WorldPosition key)
        => _world.DungeonFloorAt(position) is { } floor && !string.IsNullOrWhiteSpace(marker) && floor.Contains(key) && position.Distance(key) >= 8
            && !State.Vaults.Nodes.Any(v => v.Marker == marker || floor.Contains(v.Position))
            && VaultGroundOpen(position) && VaultGroundOpen(key) && TerrainRemovalPreservesRoutes(position, includeTimedGates: false);

    private bool VaultGroundOpen(WorldPosition p) => _world.CanEnter(p) && _world.Chunks.At(p) == Material.Floor
        && _world.DungeonTerrainAt(p) == null && _world.ObjectAt(p) == null && !ProtectedForcedLanding(p)
        && !State.OptionalTerrain.Nodes.Any(n => n.Position == p) && !State.Chests.Chests.Any(c => c.Position == p);

    public DungeonVault SpawnVault(string marker, WorldPosition position, WorldPosition key)
    {
        if (!CanPlaceVault(marker, position, key))
            throw new ArgumentException("A vault needs its own clear room position, distant floor key and routes that remain accessible while locked.");
        int seed = RoagRandom.Derive((int)(WorldRandom.Hash(_world.Seed, 0, salt: 9403) % (RoagRandom.Modulus - 1)) + 1, marker);
        var vault = new DungeonVault(State.Vaults.NextVault++, marker, position, key, seed);
        State.Vaults.Nodes.Add(vault); return vault;
    }

    private TacticalPreview PreviewVault(Actor actor, long id)
    {
        var vault = State.Vaults.Nodes.SingleOrDefault(v => v.Id == id);
        if (vault == null || vault.Opened || !Friendly(actor) || actor.Tactics?.Build is not { } build
            || actor.Position.Distance(vault.Position) != 1 || !_world.VisionFrom(actor.Position).Contains(vault.Position))
            return Invalid("Stand beside a visible locked vault.");
        if (!vault.KeyCollected) return Invalid("Find this floor's vault key.");
        return new(true, "", ActionTiming.Recovery(ClassCatalog.Get(build.Class).Costs.Move, EffectiveSpeed(actor)), [vault.Position]) { Vault = vault.Id };
    }

    private void ResolveVaultEvent(BoonEvent e)
    {
        if (e.Kind != BoonTrigger.TileEntered || e.SourceId is not > 0 || !Friendly(Actor(e.SourceId.Value))) return;
        for (int i = 0; i < State.Vaults.Nodes.Count; i++)
        {
            var vault = State.Vaults.Nodes[i]; if (vault.KeyCollected || e.Position != vault.KeyPosition) continue;
            State.Vaults.Nodes[i] = vault with { KeyCollected = true };
            Emit("VaultKeyCollected", e.SourceId.Value, 0, e.RootActionId ?? 0, vault.KeyPosition);
        }
    }

    private void OpenVault(Actor actor, long id, long action)
    {
        int index = State.Vaults.Nodes.FindIndex(v => v.Id == id); var vault = State.Vaults.Nodes[index];
        var outcome = (DungeonVaultOutcome)(vault.Seed % 4);
        AttackGeometry.TryFacing(vault.Position.X - actor.Position.X, vault.Position.Y - actor.Position.Y, out var facing); actor.Tactics!.Facing = facing;
        vault = vault with { Opened = true, Outcome = outcome }; State.Vaults.Nodes[index] = vault;
        Emit("VaultOpened", actor.Id, 0, action, vault.Position, ability: outcome.ToString());
        switch (outcome)
        {
            case DungeonVaultOutcome.Cache:
                vault = vault with { CurrencyDrop = SpawnLoot(GroundLootKind.Currency, 5, vault.Position, actor.Id, action).Id,
                    HealthDrop = SpawnLoot(GroundLootKind.Health, 10, vault.Position, actor.Id, action).Id };
                break;
            case DungeonVaultOutcome.Clue:
                vault = vault with { CurrencyDrop = SpawnLoot(GroundLootKind.Currency, 4, vault.Position, actor.Id, action).Id };
                var floor = _world.DungeonFloorAt(vault.Position);
                var secret = State.Secrets.Nodes.FirstOrDefault(s => !s.Revealed && floor?.Contains(s.Position) == true);
                if (secret != null)
                {
                    int dx = secret.Position.X - actor.Position.X, dy = secret.Position.Y - actor.Position.Y;
                    var direction = Math.Abs(dx) >= Math.Abs(dy) ? dx < 0 ? Facing.West : Facing.East : dy < 0 ? Facing.North : Facing.South;
                    vault = vault with { Clue = direction };
                    Emit("VaultClueFound", actor.Id, 0, action, vault.Position, ability: direction.ToString());
                }
                break;
            case DungeonVaultOutcome.Boon:
                vault = vault with { Chest = SpawnBoonChest("core:chest/free_elite", "review:vault:" + vault.Marker, vault.Position).Id };
                break;
            case DungeonVaultOutcome.Ambush:
                vault = vault with { Ambush = _world.SpawnVaultAmbush(this, vault.Position)?.Id ?? 0 };
                break;
        }
        State.Vaults.Nodes[index] = vault; _world.Reveal();
    }
}
