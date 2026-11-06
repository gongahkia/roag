using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public bool DungeonQuestCell(WorldPosition position)
    {
        if (State.Tactics is not { } tactics) return false;
        for (int i = 0; i < tactics.Quests.Nodes.Count; i++)
            if (tactics.Quests.Nodes[i].Cells.Contains(position)) return true;
        return false;
    }

    public (DungeonCorridorQuest Quest, DungeonQuestNode Node)? DungeonQuestNodeAt(WorldPosition position)
    {
        if (State.Tactics is not { } tactics) return null;
        for (int i = 0; i < tactics.Quests.Nodes.Count; i++)
        {
            var quest = tactics.Quests.Nodes[i];
            for (int n = 0; n < quest.Nodes.Length; n++)
            {
                var node = quest.Nodes[n];
                if (node.Position == position && node.Kind != DungeonBranchNodeKind.Enemy) return (quest, node);
            }
        }
        return null;
    }

    private int PopulateDungeonCorridors(DungeonLayout layout, DungeonPopulation population, TacticalCombat combat, string[] species, int serial)
    {
        var origin = population.Floor.Origin;
        // Quest cells belong to the optional branch; ordinary alcove encounters
        // and later fixtures cannot replace them or their target.
        int count = 0;
        foreach (var corridor in layout.Corridors)
        {
            if (count == 2) break;
            if (corridor.Cells.Length < 24 || corridor.Alcoves.Length == 0) continue;
            var p = corridor.Alcoves[(corridor.Alcoves.Length - 1) / 2]; var position = origin.Offset(p.X, p.Y);
            if (!CanEnter(position) || layout.Branch?.Cells.Contains(p) == true || position.Distance(origin.Offset(layout.Start.X, layout.Start.Y)) < 8) continue;
            SpawnDungeonEnemy(combat, species[serial++ % species.Length], position); count++;
        }
        if (layout.Branch is { } branch)
        {
            long target = 0;
            var enemy = branch.Nodes.SingleOrDefault(n => n.Kind == DungeonBranchNodeKind.Enemy);
            if (enemy != null) target = SpawnDungeonEnemy(combat, species[serial++ % species.Length], origin.Offset(enemy.Position.X, enemy.Position.Y)).Id;
            combat.SpawnCorridorQuest(population.Marker($"floor:{origin.Floor}/branch:{branch.Corridor}"), branch, origin, target);
        }
        return serial;
    }
}
