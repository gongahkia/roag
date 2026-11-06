using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public DungeonVault? DungeonVaultAt(WorldPosition position)
    {
        if (State.Tactics is not { } tactics) return null;
        for (int i = 0; i < tactics.Vaults.Nodes.Count; i++)
            if (tactics.Vaults.Nodes[i].Position == position) return tactics.Vaults.Nodes[i];
        return null;
    }

    internal Actor? SpawnVaultAmbush(TacticalCombat combat, WorldPosition vault)
    {
        var floor = DungeonFloorAt(vault) ?? throw new InvalidOperationException("The vault has no owning dungeon floor.");
        string definition = DungeonCatalog.Default.Biomes.FirstOrDefault(b => b.Id == floor.Biome)?.EnemyIds[0] ?? "core:enemy/mossbound_guard";
        foreach (var (dx, dy) in EncounterDirections)
        {
            var position = vault.Offset(dx, dy);
            if (CanEnter(position)) return SpawnDungeonEnemy(combat, definition, position);
        }
        return null;
    }

    private void PopulateDungeonVault(DungeonLayout layout, DungeonPopulation population, TacticalCombat combat)
    {
        var origin = population.Floor.Origin;
        var floor = population.Floor;
        int seed = RoagRandom.Derive(population.Seed, $"floor:{origin.Floor}/vault");
        bool Safe(WorldPosition p) => !DungeonQuestCell(p) && CanEnter(p) && Chunks.At(p) == Material.Floor && DungeonTerrainAt(p) == null && ObjectAt(p) == null
            && p.Distance(floor.Entry) >= 4 && p.Distance(floor.Exit) >= 3
            && !combat.State.Chests.Chests.Any(c => c.Position.Distance(p) < 2)
            && !combat.State.Doors.Nodes.Any(d => d.Position.Distance(p) < 2)
            && !combat.State.Secrets.Nodes.Any(s => s.Position.Distance(p) < 2 || s.Clue == p)
            && !combat.State.OptionalTerrain.Nodes.Any(n => n.Position == p);
        // A distant key must be attainable before opening the vault. Basic doors and
        // destructible barricades are paid routes; timed gates must have an alternative.
        var reachable = new HashSet<WorldPosition> { floor.Entry }; var queue = new Queue<WorldPosition>(); queue.Enqueue(floor.Entry);
        while (queue.TryDequeue(out var point))
            foreach (var (dx, dy) in EncounterDirections)
            {
                var next = point.Offset(dx, dy);
                if (!floor.Contains(next) || reachable.Contains(next) || DungeonGateAt(next) != null || DungeonQuestNodeAt(next)?.Node.Kind == DungeonBranchNodeKind.Gate
                    || !(GroundOpen(next) || DungeonDoorAt(next) != null || Chunks.At(next) == Material.Barricade)) continue;
                reachable.Add(next); queue.Enqueue(next);
            }
        var keys = new List<WorldPosition>();
        for (int y = 1; y < layout.Height - 1; y++) for (int x = 1; x < layout.Width - 1; x++)
        {
            var position = origin.Offset(x, y);
            if (reachable.Contains(position) && Safe(position)
                && EncounterDirections.Count(d => Materials.Walkable(Chunks.At(position.Offset(d.X, d.Y)))) >= 2) keys.Add(position);
        }
        keys.Sort((a, b) => RoagRandom.Derive(seed, "key:" + a.X + "," + a.Y).CompareTo(RoagRandom.Derive(seed, "key:" + b.X + "," + b.Y)));
        foreach (string role in new[] { "treasure", "landmark" })
            foreach (var candidate in keys.Where(p => floor.Rooms.Any(r => r.Role == role && r.Contains(p)))
                .OrderBy(p => RoagRandom.Derive(seed, "vault:" + p.X + "," + p.Y)))
            {
                if (EncounterDirections.Count(d => Materials.Walkable(Chunks.At(candidate.Offset(d.X, d.Y)))) < 3) continue;
                string marker = population.Marker($"floor:{origin.Floor}/vault:{candidate.X},{candidate.Y}");
                var eligible = keys.Where(k => k.Distance(candidate) >= 8).Select(k => (WorldPosition?)k).FirstOrDefault();
                if (eligible is not { } key || !combat.CanPlaceVault(marker, candidate, key)) continue;
                combat.SpawnVault(marker, candidate, key); return;
            }
    }
}
