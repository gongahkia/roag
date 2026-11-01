using Roag.Core.Content;
using Roag.Core.Dungeons;
using Roag.Core.World;
using System.Text.Json.Serialization;

namespace Roag.Core.Tactics;

public sealed record DungeonQuestNode(DungeonBranchNodeKind Kind, WorldPosition Position, bool Activated = false);
public sealed record DungeonCorridorQuest(long Id, string Marker, DungeonBranchKind Kind, WorldPosition[] Cells,
    DungeonQuestNode[] Nodes, long Target = 0, bool EnemyDefeated = false)
{
    [JsonIgnore] public bool Opened => Kind == DungeonBranchKind.Hub
        ? Nodes.Where(n => n.Kind == DungeonBranchNodeKind.Brazier).All(n => n.Activated)
        : Nodes.Any(n => n.Kind is DungeonBranchNodeKind.Clue or DungeonBranchNodeKind.Key or DungeonBranchNodeKind.Plate && n.Activated);
    [JsonIgnore] public bool Ready => Opened && (Kind != DungeonBranchKind.FalseEnd || EnemyDefeated);
    [JsonIgnore] public bool Completed => Nodes.Any(n => n.Kind == DungeonBranchNodeKind.Reward && n.Activated);
}

public sealed class DungeonQuestState
{
    public long NextQuest { get; set; } = 1;
    public List<DungeonCorridorQuest> Nodes { get; set; } = [];

    internal void Validate(WorldPlan plan, WorldState world, ContentCatalog? content)
    {
        if (NextQuest <= 0 || Nodes == null || Nodes.Any(n => n == null) || Nodes.Count > 64
            || Nodes.Select(n => n.Id).Distinct().Count() != Nodes.Count || Nodes.Select(n => n.Marker).Distinct(StringComparer.Ordinal).Count() != Nodes.Count)
            throw new InvalidDataException("Invalid corridor quest registry.");
        using var chunks = Nodes.Count == 0 ? null : new ChunkCache(plan, world, content ?? ContentCatalog.Default);
        long previous = 0; var occupied = new HashSet<WorldPosition>();
        foreach (var quest in Nodes)
        {
            if (quest.Id <= previous || quest.Id >= NextQuest || string.IsNullOrWhiteSpace(quest.Marker) || !Enum.IsDefined(quest.Kind)
                || quest.Cells == null || quest.Cells.Length == 0 || quest.Cells.Length > 64 || quest.Nodes == null || quest.Nodes.Any(n => n == null)
                || quest.Cells.Distinct().Count() != quest.Cells.Length || quest.Nodes.Select(n => n.Position).Distinct().Count() != quest.Nodes.Length
                || quest.Cells.Any(p => !plan.Contains(p) || p.Floor != quest.Cells[0].Floor || !occupied.Add(p) || world.Dungeon != null && !world.Dungeon.Contains(p))
                || quest.Nodes.Any(n => !Enum.IsDefined(n.Kind) || !quest.Cells.Contains(n.Position)))
                throw new InvalidDataException("Invalid corridor quest identity or physical footprint.");
            var expected = DungeonLayoutGenerator.DesignBranch(quest.Kind, new(0, 0), 1, 0, 1);
            if (!expected.Nodes.Select(n => n.Kind).SequenceEqual(quest.Nodes.Select(n => n.Kind)) || !OriginalShape(quest)
                || quest.Nodes.Any(n => n.Kind is DungeonBranchNodeKind.Gate or DungeonBranchNodeKind.Enemy && n.Activated)
                || quest.Completed && !quest.Ready
                || quest.Kind == DungeonBranchKind.FalseEnd && (quest.Target <= 0 || quest.Target >= world.NextActorId
                    || !quest.EnemyDefeated && !world.Actors.Any(a => a.Id == quest.Target && a.Life == LifeState.Alive && a.Tactics?.Enemy != null)
                    || quest.EnemyDefeated && world.Actors.Any(a => a.Id == quest.Target && a.Life != LifeState.Dead))
                || quest.Kind != DungeonBranchKind.FalseEnd && (quest.Target != 0 || quest.EnemyDefeated))
                throw new InvalidDataException("Invalid corridor quest progress or target receipt.");
            foreach (var node in quest.Nodes)
                if (chunks!.At(node.Position) != (node.Kind == DungeonBranchNodeKind.Gate ? quest.Opened ? Material.Door : Material.ClosedDoor : Material.Floor))
                    throw new InvalidDataException("Corridor quest fixture disagrees with physical terrain.");
            previous = quest.Id;
        }
    }

    private static bool OriginalShape(DungeonCorridorQuest quest)
    {
        foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) }) foreach (int side in new[] { -1, 1 })
        {
            var shape = DungeonLayoutGenerator.DesignBranch(quest.Kind, new(0, 0), dx, dy, side);
            int x = quest.Nodes[0].Position.X - shape.Nodes[0].Position.X, y = quest.Nodes[0].Position.Y - shape.Nodes[0].Position.Y;
            WorldPosition At(RoomPoint p) => new(x + p.X, y + p.Y, quest.Cells[0].Floor);
            if (shape.Cells.Length == quest.Cells.Length && shape.Cells.Select(At).ToHashSet().SetEquals(quest.Cells)
                && shape.Nodes.Select(n => At(n.Position)).SequenceEqual(quest.Nodes.Select(n => n.Position))) return true;
        }
        return false;
    }
}

public sealed partial class TacticalCombat
{
    public DungeonCorridorQuest SpawnCorridorQuest(string marker, DungeonBranch branch, WorldPosition origin, long target = 0)
    {
        if (string.IsNullOrWhiteSpace(marker) || !Enum.IsDefined(branch.Kind) || State.Quests.Nodes.Any(q => q.Marker == marker)
            || branch.Kind == DungeonBranchKind.FalseEnd != (target > 0)) throw new ArgumentException("Invalid corridor quest identity or target.");
        WorldPosition At(RoomPoint p) => origin.Offset(p.X, p.Y);
        var cells = branch.Cells.Select(At).ToArray();
        var nodes = branch.Nodes.Select(n => new DungeonQuestNode(n.Kind, At(n.Position))).ToArray();
        if (cells.Any(p => !_world.Plan.Contains(p) || _world.Chunks.At(p) != Material.Floor || _world.DungeonTerrainAt(p) != null
            || State.Quests.Nodes.Any(q => q.Cells.Contains(p)))
            || nodes.Any(n => _world.ObjectAt(n.Position) != null || n.Kind != DungeonBranchNodeKind.Enemy && _world.OccupantAt(n.Position) != null)
            || target > 0 && (Actor(target).Tactics?.Enemy == null || Actor(target).Life != LifeState.Alive
                || !nodes.Any(n => n.Kind == DungeonBranchNodeKind.Enemy && n.Position == Actor(target).Position)))
            throw new ArgumentException("A corridor quest preserves its physical branch and actual enemy.");
        var quest = new DungeonCorridorQuest(State.Quests.NextQuest++, marker, branch.Kind, cells, nodes, target);
        State.Quests.Nodes.Add(quest);
        foreach (var node in nodes.Where(n => n.Kind == DungeonBranchNodeKind.Gate)) _world.Chunks.Edit(node.Position, Material.ClosedDoor);
        return quest;
    }

    private void ResolveCorridorQuestEvent(BoonEvent e)
    {
        bool entered = e.Kind == BoonTrigger.TileEntered && e.SourceId is > 0 && Friendly(Actor(e.SourceId.Value));
        bool killed = e.Kind == BoonTrigger.EntityKilled;
        if (!entered && !killed) return;
        for (int i = 0; i < State.Quests.Nodes.Count; i++)
        {
            var before = State.Quests.Nodes[i]; var quest = before; long action = e.RootActionId ?? 0, source = e.SourceId ?? 0;
            if (killed && quest.Target > 0 && quest.Target == e.TargetId && !quest.EnemyDefeated) quest = quest with { EnemyDefeated = true };
            if (entered)
            {
                int index = Array.FindIndex(quest.Nodes, n => n.Position == e.Position && !n.Activated
                    && n.Kind is not (DungeonBranchNodeKind.Gate or DungeonBranchNodeKind.Enemy));
                if (index >= 0)
                {
                    var node = quest.Nodes[index];
                    if (node.Kind == DungeonBranchNodeKind.Reward && !quest.Ready) continue;
                    var nodes = quest.Nodes.ToArray(); nodes[index] = node with { Activated = true }; quest = quest with { Nodes = nodes };
                    string kind = node.Kind switch { DungeonBranchNodeKind.Clue => "SidequestClueFound", DungeonBranchNodeKind.Key => "SidequestKeyFound",
                        DungeonBranchNodeKind.Plate => "SidequestPlatePressed", DungeonBranchNodeKind.Brazier => "SidequestBrazierLit", _ => "SidequestCompleted" };
                    if (node.Kind == DungeonBranchNodeKind.Reward)
                    {
                        int balance = checked(_world.State.Inventory.GetValueOrDefault("gold") + 5); _world.State.Inventory["gold"] = balance;
                        Emit(kind, source, 0, action, node.Position, 5, ability: quest.Kind.ToString());
                        var snapshot = CaptureBoonEvent(BoonTrigger.CurrencyCollected, source, 0, action, node.Position) with { Amount = 5 };
                        Emit("CurrencyCollected", source, 0, action, node.Position, 5, snapshot: snapshot);
                    }
                    else Emit(kind, source, 0, action, node.Position, ability: quest.Kind.ToString());
                }
            }
            State.Quests.Nodes[i] = quest;
            if (!before.Opened && quest.Opened)
                foreach (var gate in quest.Nodes.Where(n => n.Kind == DungeonBranchNodeKind.Gate))
                {
                    _world.Chunks.Edit(gate.Position, Material.Door); _world.Reveal();
                    Emit("SidequestGateOpened", source, 0, action, gate.Position, ability: quest.Kind.ToString());
                }
            if (!before.Ready && quest.Ready)
                Emit("SidequestRewardReady", source, 0, action, quest.Nodes.Single(n => n.Kind == DungeonBranchNodeKind.Reward).Position, ability: quest.Kind.ToString());
        }
    }
}
