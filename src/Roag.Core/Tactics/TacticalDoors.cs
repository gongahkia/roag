using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>Identity only: the actual chunk owns open/closed collision and sight.</summary>
public sealed record TacticalDoor(long Id, string Marker, WorldPosition Position);

public sealed class TacticalDoorState
{
    public long NextDoor { get; set; } = 1;
    public List<TacticalDoor> Nodes { get; set; } = [];

    internal void Validate(WorldPlan plan, WorldState world, ContentCatalog? content)
    {
        if (NextDoor <= 0 || Nodes == null || Nodes.Any(d => d == null)
            || Nodes.Select(d => d.Id).Distinct().Count() != Nodes.Count
            || Nodes.Select(d => d.Marker).Distinct(StringComparer.Ordinal).Count() != Nodes.Count
            || Nodes.Select(d => d.Position).Distinct().Count() != Nodes.Count)
            throw new InvalidDataException("Invalid native door registry.");
        using var chunks = Nodes.Count == 0 ? null : new ChunkCache(plan, world, content ?? ContentCatalog.Default);
        long previous = 0;
        foreach (var door in Nodes)
        {
            if (door.Id <= previous || door.Id >= NextDoor || string.IsNullOrWhiteSpace(door.Marker)
                || !plan.Contains(door.Position) || world.Dungeon != null && !world.Dungeon.Contains(door.Position)
                || chunks!.At(door.Position) is not (Material.Door or Material.ClosedDoor))
                throw new InvalidDataException("Native door identity differs from its physical tile.");
            previous = door.Id;
        }
    }
}

public sealed partial class TacticalCombat
{
    public TacticalDoor SpawnDoor(string marker, WorldPosition position)
    {
        if (string.IsNullOrWhiteSpace(marker) || !_world.CanEnter(position) || ProtectedForcedLanding(position)
            || _world.ObjectAt(position) != null || _world.DungeonTerrainAt(position) != null
            || State.Doors.Nodes.Any(d => d.Marker == marker || d.Position == position)
            || State.OptionalTerrain.Nodes.Any(n => n.Position == position))
            throw new ArgumentException("A door needs a unique clear socket without replacing terrain content.");
        var door = new TacticalDoor(State.Doors.NextDoor++, marker, position);
        _world.Chunks.Edit(position, Material.ClosedDoor); State.Doors.Nodes.Add(door); return door;
    }

    private TacticalPreview PreviewDoor(Actor actor, long id)
    {
        var door = State.Doors.Nodes.SingleOrDefault(d => d.Id == id);
        if (door == null || !Friendly(actor) || actor.Tactics?.Build is not { } build || actor.Position.Distance(door.Position) != 1
            || _world.Chunks.At(door.Position) != Material.ClosedDoor || !_world.VisionFrom(actor.Position).Contains(door.Position))
            return Invalid("Stand beside a visible closed door.");
        return new(true, "", ActionTiming.Recovery(ClassCatalog.Get(build.Class).Costs.Move, EffectiveSpeed(actor)), [door.Position]) { Door = door.Id };
    }

    private void OpenDoor(Actor actor, long id, long action)
    {
        var door = State.Doors.Nodes.Single(d => d.Id == id);
        AttackGeometry.TryFacing(door.Position.X - actor.Position.X, door.Position.Y - actor.Position.Y, out var facing);
        actor.Tactics!.Facing = facing;
        _world.Chunks.Edit(door.Position, Material.Door); _world.Reveal();
        Emit("DoorOpened", actor.Id, 0, action, door.Position);
    }
}
