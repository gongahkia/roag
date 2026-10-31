using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum DungeonContractStatus { Offered, Active, Failed, Completed }
public sealed class DungeonContract
{
    public WorldRoomId Room { get; set; }
    public long[] Targets { get; set; } = [];
    public HashSet<long> Defeated { get; set; } = [];
    public long Owner { get; set; }
    public DungeonContractStatus Status { get; set; }
    public string Failure { get; set; } = "";

    internal void Validate(WorldPlan plan, WorldState world, DungeonService node)
    {
        if (!Enum.IsDefined(Status) || Targets == null || Targets.Length == 0 || Targets.Length > 64
            || !Targets.SequenceEqual(Targets.Distinct().Order()) || Targets.Any(id => id <= 0 || id >= world.NextActorId)
            || Defeated == null || Defeated.Any(id => !Targets.Contains(id)) || Failure == null
            || (Status == DungeonContractStatus.Offered ? Owner != 0 : !world.Party.Contains(Owner))
            || (Status == DungeonContractStatus.Completed ? node.UsedBy != Owner || Defeated.Count != Targets.Length : node.UsedBy != 0)
            || Room.Area != RoomArea.Dungeon || Room.Floor != node.Position.Floor || world.Dungeon?.RoomAt(node.Position) != Room
            || Status is DungeonContractStatus.Offered or DungeonContractStatus.Active && Targets.Any(id => !world.Actors.Any(a => a.Id == id)))
            throw new InvalidDataException("Invalid room contract targets, owner, progress or receipt.");
        if (Status == DungeonContractStatus.Active)
        {
            var owner = world.Actors.Single(a => a.Id == Owner);
            if (world.Dungeon?.RoomAt(owner.Position) != Room) throw new InvalidDataException("Active contract owner has left its room.");
        }
    }
}

public sealed partial class TacticalCombat
{
    // Completion is checked after the owning action's causal event/effect queue empties,
    // before its chain history is pruned. Contract gold remains in that same transaction.
    private long _contractCompletionActor, _contractCompletionAction;

    private long[] DungeonContractTargets(WorldRoomId room) => _world.State.Actors.Where(a => a is { Life: LifeState.Alive, Health: > 0,
            Tactics: { Enemy: not null, RewardExempt: false } } && _world.RoomAt(a.Position) == room
            && a.Tactics.DefinitionId != EnemySentinel.Definition
            && EnemyCatalog.Default.Get(a.Tactics.DefinitionId).Ai is not (EnemyBrainKind.Warden or EnemyBrainKind.BiomeElite
                or EnemyBrainKind.Pursuer or EnemyBrainKind.HollowRegent)).Select(a => a.Id).Order().ToArray();

    internal bool HasDungeonContractTargets(WorldRoomId room) => DungeonContractTargets(room).Length > 0;

    private void FailContract(DungeonService node, string reason, long action)
    {
        var contract = node.Contract!;
        contract.Status = DungeonContractStatus.Failed; contract.Failure = reason;
        Emit("ContractChanged", contract.Owner, contract.Owner, action, node.Position, ability: reason);
    }

    private void BeforeContractAction(Actor actor, TacticalIntent intent, TacticalPreview preview, long action)
    {
        if (preview.Cells.Count == 0 || intent.Action is not (TacticalAction.Dash or TacticalAction.UseUtility)) return;
        foreach (var node in State.Services.Nodes)
            if (node.Contract is { Status: DungeonContractStatus.Active } c && c.Owner == actor.Id)
                FailContract(node, "movement ability used", action);
    }

    private void ResolveContractEvent(BoonEvent e)
    {
        foreach (var node in State.Services.Nodes)
        {
            if (node.Contract is not { Status: DungeonContractStatus.Active } contract) continue;
            // Every native stair, return and forced displacement publishes the actual
            // recipient's TileEntered event; display-only Displaced is not a boon event.
            bool moved = e.Kind == BoonTrigger.TileEntered && e.SourceId == contract.Owner;
            if (moved && e.Position is { } cell && _world.RoomAt(cell) != contract.Room)
            { FailContract(node, "left the room", e.RootActionId ?? 0); continue; }
            if (e.Kind != BoonTrigger.EntityKilled || e.TargetId is not { } victim) continue;
            var actor = Actor(victim); long original = actor.Tactics?.Enemy?.RevivalOf is > 0 ? actor.Tactics.Enemy.RevivalOf : victim;
            if (!contract.Targets.Contains(original)) continue;
            if (e.SourceFaction != "player") FailContract(node, "target defeated by another faction", e.RootActionId ?? 0);
            else contract.Defeated.Add(original);
        }
    }

    private void QueueContractCompletion(Actor owner, long action)
    {
        if (State.Services.Nodes.Count == 0) return;
        if (!State.Services.Nodes.Any(n => n.Contract is { Status: DungeonContractStatus.Active } c && c.Owner == owner.Id)) return;
        _contractCompletionActor = owner.Id; _contractCompletionAction = action;
    }

    private void CompleteContracts()
    {
        var owner = Actor(_contractCompletionActor); long action = _contractCompletionAction;
        _contractCompletionActor = 0; _contractCompletionAction = 0;
        if (owner is not { Life: LifeState.Alive, Health: > 0 }) return;
        foreach (var node in State.Services.Nodes.ToArray())
        {
            if (node.Contract is not { Status: DungeonContractStatus.Active } contract || contract.Owner != owner.Id
                || contract.Defeated.Count != contract.Targets.Length) continue;
            bool unresolved = contract.Targets.Any(id => _world.State.Actors.Any(a => a.Id == id && a.Life != LifeState.Dead
                    || a.Life != LifeState.Dead && a.Tactics?.Enemy?.RevivalOf == id)
                || State.Corpses.Any(c => c.Actor == id));
            if (unresolved) continue;
            contract.Status = DungeonContractStatus.Completed;
            int index = State.Services.Nodes.FindIndex(n => n.Id == node.Id);
            State.Services.Nodes[index] = node with { UsedBy = owner.Id };
            _world.State.Inventory["gold"] = checked(_world.Count("gold") + 8);
            Emit("ContractChanged", owner.Id, owner.Id, action, node.Position, 8, ability: "completed");
            var snapshot = CaptureBoonEvent(BoonTrigger.CurrencyCollected, owner.Id, 0, action, node.Position) with { Amount = 8 };
            Emit("CurrencyCollected", owner.Id, 0, action, node.Position, 8, snapshot: snapshot);
        }
    }

    public bool IsContractTarget(long actorId) => State.Services.Nodes.Any(n => n.Contract is { Status: DungeonContractStatus.Active } c
        && (c.Targets.Contains(actorId) || Actor(actorId).Tactics?.Enemy?.RevivalOf is { } original && c.Targets.Contains(original)));
}
