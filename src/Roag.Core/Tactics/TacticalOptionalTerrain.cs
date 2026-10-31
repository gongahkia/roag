using Roag.Core.World;
using Roag.Core.Content;

namespace Roag.Core.Tactics;

public enum OptionalTerrainKind { HeatVent, PushFloor, FragileCrossing }
public enum OptionalTerrainPhase { Cooldown, Warning, Ready, Intact, Armed, Closed }
public sealed record OptionalTerrainNode(long Id, string Marker, OptionalTerrainKind Kind, WorldPosition Position,
    Facing Facing, OptionalTerrainPhase Phase, long Due = 0, long Schedule = 0);

public sealed class OptionalTerrainState
{
    public long NextNode { get; set; } = 1;
    public List<OptionalTerrainNode> Nodes { get; set; } = [];

    internal void Validate(WorldPlan plan, WorldState world, ContentCatalog? content)
    {
        var tactics = world.Tactics!;
        var timeline = tactics.Queue.Snapshot();
        if (NextNode <= 0 || Nodes == null || Nodes.Count > 192 || Nodes.Any(n => n == null)
            || Nodes.Select(n => n.Id).Distinct().Count() != Nodes.Count
            || Nodes.Select(n => n.Position).Distinct().Count() != Nodes.Count
            || Nodes.Select(n => n.Marker).Distinct(StringComparer.Ordinal).Count() != Nodes.Count)
            throw new InvalidDataException("Invalid optional terrain registry.");
        using var terrain = Nodes.Count == 0 ? null : new ChunkCache(plan, world, content);
        foreach (var node in Nodes)
        {
            if (node.Id <= 0 || node.Id >= NextNode || string.IsNullOrWhiteSpace(node.Marker) || !plan.Contains(node.Position)
                || !Enum.IsDefined(node.Kind) || !Enum.IsDefined(node.Phase) || !Enum.IsDefined(node.Facing) || !AttackGeometry.Cardinal(node.Facing)
                || (node.Kind switch
                {
                    OptionalTerrainKind.HeatVent => node.Phase is not (OptionalTerrainPhase.Cooldown or OptionalTerrainPhase.Warning)
                        || (node.Schedule == 0 ? node.Phase != OptionalTerrainPhase.Cooldown || node.Due != 0
                            : node.Schedule < 0 || node.Due < timeline.Clock || node.Due - timeline.Clock > (node.Phase == OptionalTerrainPhase.Warning ? 100 : 300)
                                || timeline.Pending.Count(w => Matches(w, node)) != 1),
                    OptionalTerrainKind.PushFloor => node.Phase != OptionalTerrainPhase.Ready || node.Schedule != 0 || node.Due != 0,
                    OptionalTerrainKind.FragileCrossing => node.Phase is not (OptionalTerrainPhase.Intact or OptionalTerrainPhase.Armed or OptionalTerrainPhase.Closed)
                        || node.Schedule != 0 || node.Due != 0,
                    _ => true
                })) throw new InvalidDataException("Invalid optional terrain phase or scheduled work.");
            var material = terrain!.At(node.Position);
            if (world.Dungeon != null && !world.Dungeon.Contains(node.Position)
                || (node.Phase == OptionalTerrainPhase.Closed ? material != Material.Water
                    : !Materials.Walkable(material) || material is Material.StairsDown or Material.StairsUp))
                throw new InvalidDataException("Optional terrain differs from its physical floor.");
        }
        if (timeline.Pending.Any(w => w.Kind == ScheduledKind.TerrainCycle && !Nodes.Any(n => Matches(w, n))))
            throw new InvalidDataException("Orphaned optional terrain work.");
    }

    internal static bool Matches(ScheduledAction work, OptionalTerrainNode node) => work.Kind == ScheduledKind.TerrainCycle
        && work.ActorId == 0 && work.ReferenceId == node.Id && work.Sequence == node.Schedule && work.Due == node.Due;
}

public sealed partial class TacticalCombat
{
    private readonly Dictionary<(long Action, long Actor), int> _pushFloorCounts = [];

    public bool CanPlaceOptionalTerrain(OptionalTerrainKind kind, string marker, WorldPosition position, Facing facing = Facing.South)
        => Enum.IsDefined(kind) && Enum.IsDefined(facing) && AttackGeometry.Cardinal(facing) && !string.IsNullOrWhiteSpace(marker)
            && _world.GroundOpen(position) && _world.DungeonTerrainAt(position) == null && !ProtectedForcedLanding(position)
            && !State.OptionalTerrain.Nodes.Any(n => n.Position == position || n.Marker == marker)
            && !State.Services.Nodes.Any(n => n.Position == position)
            && !State.Secrets.Nodes.Any(n => !n.Revealed && n.Clue == position)
            && !State.RoomFeatures.Nodes.Any(n => n.Position == position)
            && (kind != OptionalTerrainKind.FragileCrossing || _world.State.Dungeon?.Floors.Any(f => f.Entry == position || f.Exit == position) != true
                && TerrainRemovalPreservesRoutes(position));

    public OptionalTerrainNode PlaceOptionalTerrain(OptionalTerrainKind kind, string marker, WorldPosition position, Facing facing = Facing.South)
    {
        if (!CanPlaceOptionalTerrain(kind, marker, position, facing))
            throw new ArgumentException("Optional terrain needs unique clear unprotected ground and a safe alternate route.");
        var phase = kind switch { OptionalTerrainKind.HeatVent => OptionalTerrainPhase.Cooldown,
            OptionalTerrainKind.PushFloor => OptionalTerrainPhase.Ready, _ => OptionalTerrainPhase.Intact };
        var node = new OptionalTerrainNode(State.OptionalTerrain.NextNode++, marker, kind, position, facing, phase);
        State.OptionalTerrain.Nodes.Add(node); return node;
    }

    private void StartOptionalTerrain(long action)
    {
        for (int i = 0; i < State.OptionalTerrain.Nodes.Count; i++)
        {
            var node = State.OptionalTerrain.Nodes[i];
            if (node.Kind != OptionalTerrainKind.HeatVent || node.Schedule != 0) continue;
            State.OptionalTerrain.Nodes[i] = ScheduleTerrain(node with { Phase = OptionalTerrainPhase.Warning }, checked(Clock + 100));
            Emit("HeatVentWarned", 0, 0, action, node.Position);
        }
    }

    private OptionalTerrainNode ScheduleTerrain(OptionalTerrainNode node, long due)
        => node with { Due = due, Schedule = State.Queue.Schedule(ScheduledKind.TerrainCycle, due, referenceId: node.Id).Sequence };

    private void ResolveTerrainCycle(ScheduledAction work)
    {
        int index = State.OptionalTerrain.Nodes.FindIndex(n => OptionalTerrainState.Matches(work, n));
        if (index < 0) throw new InvalidDataException("Missing scheduled terrain fixture.");
        var node = State.OptionalTerrain.Nodes[index];
        ResolveAtomically(RootCause(0), () =>
        {
            if (node.Phase == OptionalTerrainPhase.Cooldown)
            {
                State.OptionalTerrain.Nodes[index] = ScheduleTerrain(node with { Phase = OptionalTerrainPhase.Warning }, checked(Clock + 100));
                Emit("HeatVentWarned", 0, 0, _cause!.RootActionId ?? _cause.ChainId, node.Position);
            }
            else
            {
                long action = _cause!.RootActionId ?? _cause.ChainId;
                State.OptionalTerrain.Nodes[index] = ScheduleTerrain(node with { Phase = OptionalTerrainPhase.Cooldown }, checked(Clock + 300));
                Emit("HeatVentBurst", 0, 0, action, node.Position);
                ResolveDamageBatch(new(action, 0, node.Position, [node.Position], "core:terrain/heat_vent", AttackDelivery.Area,
                    DamageElement.Fire, 2, Clock, DamageCause.GroundHazard, IgnoresTerrain: true));
            }
        });
    }

    private void ResolveOptionalTerrainEntry(BoonEvent e)
    {
        if (e.Kind != BoonTrigger.TileEntered || e.SourceId is not > 0 || e.Position is not { } cell) return;
        int index = State.OptionalTerrain.Nodes.FindIndex(n => n.Position == cell);
        if (index < 0) return;
        var node = State.OptionalTerrain.Nodes[index]; var actor = Actor(e.SourceId.Value);
        if (actor is not { Life: LifeState.Alive, Health: > 0 }) return;
        long action = e.RootActionId ?? 0;
        if (node.Kind == OptionalTerrainKind.FragileCrossing && node.Phase == OptionalTerrainPhase.Intact)
        {
            State.OptionalTerrain.Nodes[index] = node with { Phase = OptionalTerrainPhase.Armed };
            Emit("FragileArmed", actor.Id, actor.Id, action, cell);
        }
        else if (node.Kind == OptionalTerrainKind.PushFloor && actor.Position == cell)
        {
            var key = (action, actor.Id); int count = _pushFloorCounts.GetValueOrDefault(key);
            if (count >= 4) { Emit("PushFloorBlocked", 0, actor.Id, action, cell, ability: "chain limit"); return; }
            _pushFloorCounts[key] = count + 1;
            bool moved = TryShove(actor, node.Facing, 0, action);
            Emit(moved ? "PushFloorTriggered" : "PushFloorBlocked", 0, actor.Id, action, cell);
        }
    }

    private void SettleFragileTerrain()
    {
        for (int i = 0; i < State.OptionalTerrain.Nodes.Count; i++)
        {
            var node = State.OptionalTerrain.Nodes[i];
            if (node.Phase != OptionalTerrainPhase.Armed || node.Kind != OptionalTerrainKind.FragileCrossing) continue;
            var p = node.Position;
            if (_world.OccupantAt(p) != null || _world.ObjectAt(p) is { Solid: true }
                || State.Loot.Drops.Any(d => d.CarriedBy == 0 && d.Position == p) || State.Corpses.Any(c => Actor(c.Actor).Position == p)
                || State.EnemySnares.Any(s => s.Position == p)
                || State.Services.Nodes.Any(n => n.Position == p)
                || _world.State.Actors.Any(a => a.Tactics?.Supplies.Utility?.Marker?.Position == p)
                || _world.State.Actors.Any(a => a.Tactics?.Resources is { } r && (r.Decoy == p || r.Snare == p || r.Spirit == p
                    || r.Beacon?.Position == p || r.Sentry?.Position == p || r.Bulwark?.Position == p))) continue;
            if (!TerrainRemovalPreservesRoutes(p)) continue; // Later physical edits/props can invalidate the original proof.
            _world.Chunks.Edit(p, Material.Water);
            State.OptionalTerrain.Nodes[i] = node with { Phase = OptionalTerrainPhase.Closed };
            Emit("FragileCollapsed", 0, 0, 0, p);
        }
    }

    private IEnumerable<LockedAttack> TerrainWarnings() => State.OptionalTerrain.Nodes.Where(n => n.Phase == OptionalTerrainPhase.Warning)
        .Select(n => new LockedAttack(0, 0, n.Position, [n.Position], "core:terrain/heat_vent", AttackDelivery.Area,
            DamageElement.Fire, 2, n.Due, DamageCause.GroundHazard));
}
