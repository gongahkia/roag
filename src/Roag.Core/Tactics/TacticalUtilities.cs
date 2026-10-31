using System.Collections.Immutable;
using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum UtilityKind { CleanseFlask, EscapeScroll, ReturnToken }
public sealed record UtilityMarker(WorldPosition Position, WorldRoomId Room);
public sealed record CarriedUtility(UtilityKind Kind, UtilityMarker? Marker = null);

/// <summary>One carried charge and action-counted conditions, owned by the actual member.</summary>
public sealed class TacticalSupplies
{
    public CarriedUtility? Utility { get; set; }
    public int FocusActions { get; set; }
    public int WardActions { get; set; }

    internal void Validate()
    {
        if (FocusActions is < 0 or > 3 || WardActions is < 0 or > 3 || FocusActions > 0 && WardActions > 0
            || Utility is { } utility && (!Enum.IsDefined(utility.Kind)
                || utility.Marker != null && utility.Kind != UtilityKind.ReturnToken))
            throw new InvalidDataException("Invalid carried utility or prepared supply condition.");
    }

    internal static void ValidateMarkers(WorldPlan plan, WorldState world, ContentCatalog? content)
    {
        if (!world.Actors.Any(a => a.Tactics?.Supplies.Utility?.Marker != null)) return;
        using var chunks = new ChunkCache(plan, world, content ?? ContentCatalog.Default);
        WorldRoomId? Room(WorldPosition p) => world.Dungeon != null ? world.Dungeon.RoomAt(p) : chunks.Generator.RoomAt(p);
        foreach (var actor in world.Actors)
            if (actor.Tactics?.Supplies.Utility?.Marker is { } marker
                && (!plan.Contains(marker.Position) || marker.Position.Floor != actor.Position.Floor
                    || Room(marker.Position) != marker.Room || Room(actor.Position) != marker.Room
                    || !Materials.Walkable(chunks.At(marker.Position))))
                throw new InvalidDataException("Invalid planted utility room or terrain.");
    }
}

public sealed partial class TacticalCombat
{
    private static readonly (int X, int Y)[] UtilityDirections = [(0, -1), (1, 0), (0, 1), (-1, 0)];

    private TacticalPreview PreviewUtility(Actor actor, TacticalIntent intent)
    {
        var utility = actor.Tactics!.Supplies.Utility;
        if (!Friendly(actor) || utility == null) return Invalid("The carried utility slot is empty.");
        IReadOnlyList<WorldPosition> cells = [];
        if (utility.Kind == UtilityKind.CleanseFlask)
        {
            if (actor.Tactics.Burning == null) return Invalid("Not Burning; the charge is retained.");
        }
        else if (utility.Kind == UtilityKind.EscapeScroll)
        {
            if (!AttackGeometry.Cardinal(intent.Facing) || intent.Choice is < 1 or > 3)
                return Invalid("Choose a cardinal direction and one to three tiles.");
            var (dx, dy) = AttackGeometry.Offset(intent.Facing);
            cells = Enumerable.Range(1, intent.Choice).Select(i => actor.Position.Offset(dx * i, dy * i)).ToArray();
            var sight = _world.VisionFrom(actor.Position);
            if (cells.Any(p => !sight.Contains(p) || !_world.CanEnter(p, actor.Id) || ProtectedForcedLanding(p)))
                return Invalid("The whole path needs visible clear ground without stairs or exits.");
        }
        else if (utility.Marker is not { } marker)
        {
            if (_world.RoomAt(actor.Position) == null) return Invalid("Plant the token inside a room.");
        }
        else
        {
            if (_world.RoomAt(actor.Position) != marker.Room || !_world.VisionFrom(actor.Position).Contains(marker.Position)
                || !_world.CanEnter(marker.Position, actor.Id) || ProtectedForcedLanding(marker.Position)
                || actor.Position == marker.Position) return Invalid("The marker must be visible, empty and in this room.");
            var seen = new HashSet<WorldPosition> { actor.Position };
            var queue = new Queue<(WorldPosition Cell, int Distance)>(); queue.Enqueue((actor.Position, 0));
            bool reachable = false;
            while (queue.TryDequeue(out var next))
            {
                if (next.Cell == marker.Position) { reachable = true; break; }
                if (next.Distance == 4) continue;
                foreach (var (dx, dy) in UtilityDirections)
                {
                    var cell = next.Cell.Offset(dx, dy);
                    if (_world.RoomAt(cell) == marker.Room && _world.Chunks.Seen(cell) && _world.CanEnter(cell, actor.Id)
                        && !ProtectedForcedLanding(cell) && seen.Add(cell)) queue.Enqueue((cell, next.Distance + 1));
                }
            }
            if (!reachable) return Invalid("The marker needs a known clear path of one to four steps.");
            cells = [marker.Position];
        }
        int extra = utility.Kind == UtilityKind.EscapeScroll
            ? checked(TerrainMovementExtra(cells, TacticalAction.Dash) + SiltMovementExtra(cells, TacticalAction.Dash)) : 0;
        return new(true, "", ActionTiming.Recovery(checked(100 + extra), EffectiveSpeed(actor)), cells);
    }

    private void CommitUtility(Actor actor, TacticalPreview preview, long action)
    {
        var supplies = actor.Tactics!.Supplies; var utility = supplies.Utility!;
        if (utility.Kind == UtilityKind.ReturnToken && utility.Marker == null)
        {
            supplies.Utility = utility with { Marker = new(actor.Position, _world.RoomAt(actor.Position)!.Value) };
            Emit("ReturnTokenPlanted", actor.Id, actor.Id, action, actor.Position); return;
        }
        supplies.Utility = null;
        if (utility.Kind == UtilityKind.CleanseFlask)
        {
            RemoveBurning(actor);
            Emit("StatusCleansed", actor.Id, actor.Id, action, actor.Position, ability: UtilityId(utility.Kind));
        }
        Emit("UtilityUsed", actor.Id, actor.Id, action, actor.Position, ability: UtilityId(utility.Kind));
        if (preview.Cells.Count == 0) return;
        // The original token/scroll both publish a Dash-tagged Moved event. A token's
        // path contains only its landing; it does not touch intervening cells.
        var origin = actor.Position;
        foreach (var cell in preview.Cells) _world.MoveTacticalActor(actor, cell);
        const string ability = "core:ability/dash";
        var moved = CaptureBoonEvent(BoonTrigger.Moved, actor.Id, 0, action, actor.Position, ability, origin)
            with { Path = preview.Cells.ToImmutableArray() };
        Emit("Moved", actor.Id, actor.Id, action, actor.Position, ability: ability, from: origin, snapshot: moved);
        var from = origin;
        foreach (var cell in preview.Cells)
        {
            Emit("TileEntered", actor.Id, actor.Id, action, cell, from: from,
                snapshot: CaptureBoonEvent(BoonTrigger.TileEntered, actor.Id, 0, action, cell, from: from) with { SourcePosition = cell });
            CollectLootAt(actor, cell, action); from = cell;
        }
    }

    public static string UtilityId(UtilityKind kind) => kind switch
    {
        UtilityKind.CleanseFlask => "core:utility/cleanse_flask", UtilityKind.EscapeScroll => "core:utility/escape_scroll",
        UtilityKind.ReturnToken => "core:utility/return_token", _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private void ExpireUtilityMarker(Actor actor, WorldPosition entered, long action)
    {
        if (actor.Tactics?.Supplies.Utility?.Marker is not { } marker || _world.RoomAt(entered) == marker.Room) return;
        actor.Tactics.Supplies.Utility = null;
        Emit("ReturnTokenExpired", actor.Id, actor.Id, action, entered);
    }
}
