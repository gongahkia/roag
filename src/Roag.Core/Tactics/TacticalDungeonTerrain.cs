using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    // One contact per actor/action transaction, shared by forced entries and Dash.
    // This is event-drain context, not additional saved world state.
    private readonly HashSet<(long Action, long Actor)> _terrainContacts = [];

    private int TerrainMovementExtra(IReadOnlyList<WorldPosition> cells, TacticalAction action)
        => checked(cells.Count(p => _world.DungeonTerrainAt(p) == 'g') * (action == TacticalAction.Dash ? 20 : 40));

    private void ResolveTerrainEvent(BoonEvent e)
    {
        ResolveOptionalTerrainEntry(e);
        if (e.Position is not { } position) return;
        long action = e.RootActionId ?? 0;
        if (e.Kind == BoonTrigger.TileEntered && _world.DungeonTerrainAt(position) == 'b' && e.SourceId is > 0
            && _terrainContacts.Add((action, e.SourceId.Value)))
        {
            var target = Actor(e.SourceId.Value);
            if (target is not { Life: LifeState.Alive, Health: > 0 }) return;
            Emit("TerrainTriggered", 0, target.Id, action, position, ability: "core:terrain/bramble_bed");
            ResolveAttack(new(action, 0, position, [target.Position], "core:terrain/bramble_bed", AttackDelivery.Area,
                DamageElement.Bramble, 1, Clock, DamageCause.GroundHazard, IgnoresTerrain: true));
        }
        else if (e.Kind == BoonTrigger.DamageTaken && e.DamageType == DamageElement.Lightning && e.ActualLoss > 0)
        {
            WorldPosition? crystal = null;
            for (int y = -1; y <= 1 && crystal == null; y++) for (int x = -1; x <= 1; x++)
                if (_world.DungeonTerrainAt(position.Offset(x, y)) == 'c') { crystal = position.Offset(x, y); break; }
            if (crystal is not { } center) return;
            var target = _world.State.Actors.Where(a => a.Id != e.TargetId && a.Life == LifeState.Alive && a.Health > 0
                    && a.Position.Floor == center.Floor && BoonFaction(a) != e.SourceFaction
                    && Math.Max(Math.Abs(a.Position.X - center.X), Math.Abs(a.Position.Y - center.Y)) <= 1)
                .OrderBy(a => a.Position.Distance(center)).ThenBy(a => a.Id).FirstOrDefault();
            if (target == null) return;
            Emit("TerrainTriggered", e.SourceId ?? 0, target.Id, action, center, ability: "core:terrain/resonant_crystal");
            ResolveAttack(new(action, e.SourceId ?? 0, center, [target.Position], "core:terrain/crystal_arc", AttackDelivery.Area,
                DamageElement.CrystalArc, 1, Clock, DamageCause.GroundHazard, PersistsWithoutOwner: true, IgnoresTerrain: true));
        }
    }
}
