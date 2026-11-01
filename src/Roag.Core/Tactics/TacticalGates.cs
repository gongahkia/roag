using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>Chunks own closure; the registry owns the plate connection and timed work.</summary>
public sealed record DungeonGate(long Id, string Marker, WorldPosition Position, long CloseAt = 0, long Schedule = 0);

public sealed class DungeonGateState
{
    public long NextGate { get; set; } = 1;
    public List<DungeonGate> Nodes { get; set; } = [];

    internal void Validate(WorldPlan plan, WorldState world, ContentCatalog? content)
    {
        if (NextGate <= 0 || Nodes == null || Nodes.Any(n => n == null)
            || Nodes.Select(n => n.Id).Distinct().Count() != Nodes.Count
            || Nodes.Select(n => n.Marker).Distinct(StringComparer.Ordinal).Count() != Nodes.Count
            || Nodes.Select(n => n.Position).Distinct().Count() != Nodes.Count)
            throw new InvalidDataException("Invalid physical gate registry.");
        var state = world.Tactics!; var queue = state.Queue.Snapshot();
        using var chunks = Nodes.Count == 0 ? null : new ChunkCache(plan, world, content ?? ContentCatalog.Default);
        long previous = 0;
        foreach (var gate in Nodes)
        {
            var plates = state.RoomFeatures.Nodes.Where(n => n.Kind == DungeonRoomFeatureKind.PressurePlate && n.Gate == gate.Id).ToArray();
            if (gate.Id <= previous || gate.Id >= NextGate || string.IsNullOrWhiteSpace(gate.Marker) || !plan.Contains(gate.Position)
                || world.Dungeon != null && !world.Dungeon.Contains(gate.Position) || gate.CloseAt < 0 || gate.Schedule < 0
                || plates.Length != 1 || plates[0].Position.Distance(gate.Position) != 1
                || chunks!.At(gate.Position) != (gate.CloseAt == 0 ? Material.ClosedDoor : Material.Door)
                || gate.CloseAt == 0 && gate.Schedule != 0 || gate.CloseAt > queue.Clock && (gate.Schedule == 0 || gate.CloseAt - queue.Clock > 300)
                || gate.Schedule > 0 && queue.Pending.Count(w => Matches(w, gate)) != 1
                || gate.CloseAt > 0 && gate.Schedule == 0 && !world.Actors.Any(a => a.Life != LifeState.Dead && a.Position == gate.Position
                    || a.Tactics?.Resources.Bulwark?.Position == gate.Position))
                throw new InvalidDataException("Invalid gate tile, plate, occupied closure or saved timed work.");
            previous = gate.Id;
        }
        if (state.RoomFeatures.Nodes.Any(n => n.Gate > 0 && !Nodes.Any(g => g.Id == n.Gate))
            || queue.Pending.Any(w => w.Kind == ScheduledKind.GateClosure && !Nodes.Any(g => Matches(w, g))))
            throw new InvalidDataException("Orphaned timed gate closure.");
    }

    internal static bool Matches(ScheduledAction work, DungeonGate gate) => work.Kind == ScheduledKind.GateClosure
        && work.ActorId == 0 && work.Sequence == gate.Schedule && work.Due == gate.CloseAt && work.ReferenceId == gate.Id;
}

public sealed partial class TacticalCombat
{
    public bool CanPlaceTimedGate(string marker, WorldPosition gate, WorldPosition plate)
        => !string.IsNullOrWhiteSpace(marker) && gate.Distance(plate) == 1 && _world.CanEnter(gate)
            && _world.Chunks.At(gate) == Material.Floor && _world.DungeonTerrainAt(gate) == null
            && _world.ObjectAt(gate) == null && !ProtectedForcedLanding(gate)
            && !State.Chests.Chests.Any(n => n.Position == gate) && !State.OptionalTerrain.Nodes.Any(n => n.Position == gate)
            && !State.Gates.Nodes.Any(n => n.Position == gate || n.Marker == marker)
            && CanPlaceRoomFeature(DungeonRoomFeatureKind.PressurePlate, marker + "/plate", plate)
            && TerrainRemovalPreservesRoutes(gate);

    public DungeonGate SpawnTimedGate(string marker, WorldPosition position, WorldPosition plate)
    {
        if (!CanPlaceTimedGate(marker, position, plate))
            throw new ArgumentException("A timed gate needs an adjacent accessible plate and must preserve every reachable route.");
        var gate = new DungeonGate(State.Gates.NextGate++, marker, position);
        State.Gates.Nodes.Add(gate);
        SpawnRoomFeature(DungeonRoomFeatureKind.PressurePlate, marker + "/plate", plate, gate: gate.Id);
        _world.Chunks.Edit(position, Material.ClosedDoor); return gate;
    }

    private void OpenTimedGate(long id, long source, long action)
    {
        int index = State.Gates.Nodes.FindIndex(g => g.Id == id); var gate = State.Gates.Nodes[index];
        if (gate.Schedule > 0) State.Queue.Cancel(gate.Schedule);
        long due = checked(Clock + 300);
        long schedule = State.Queue.Schedule(ScheduledKind.GateClosure, due, referenceId: gate.Id).Sequence;
        State.Gates.Nodes[index] = gate with { CloseAt = due, Schedule = schedule };
        _world.Chunks.Edit(gate.Position, Material.Door); _world.Reveal();
        Emit("TimedGateOpened", source, 0, action, gate.Position);
    }

    private void ResolveGateClosure(ScheduledAction work)
    {
        int index = State.Gates.Nodes.FindIndex(g => DungeonGateState.Matches(work, g));
        if (index < 0) throw new InvalidDataException("Orphaned timed gate closure.");
        State.Gates.Nodes[index] = State.Gates.Nodes[index] with { Schedule = 0 };
        SettleOverdueGates();
    }

    private void SettleOverdueGates()
    {
        for (int i = 0; i < State.Gates.Nodes.Count; i++)
        {
            var gate = State.Gates.Nodes[i];
            if (gate.CloseAt == 0 || gate.CloseAt > Clock || gate.Schedule != 0
                || _world.OccupantAt(gate.Position) != null || _world.TacticalBlockAt(gate.Position)) continue;
            _world.Chunks.Edit(gate.Position, Material.ClosedDoor);
            State.Gates.Nodes[i] = gate with { CloseAt = 0 }; _world.Reveal();
            Emit("TimedGateClosed", 0, 0, 0, gate.Position);
        }
    }
}
