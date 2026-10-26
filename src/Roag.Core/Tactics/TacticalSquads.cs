using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private Actor? SquadLeader(Actor member)
    {
        if (member.Tactics?.Enemy?.Squad is not { } squad) return null;
        return _world.State.Actors.FirstOrDefault(a => a.Id == squad.Leader && a.Life == LifeState.Alive
            && a.Position.Floor == member.Position.Floor && a.Tactics?.DefinitionId == EnemySquads.Roster(squad.Kind).Leader
            && a.Tactics.Enemy?.Squad?.Id == squad.Id);
    }

    private bool SupportReaches(Actor source, Actor target, int radius) => source.Tactics!.StunnedUntil <= Clock
        && source.Position.Distance(target.Position) <= radius
        && EnemySquads.Clear(source.Position, target.Position, ProjectileOpen);

    private Actor? SquadRallySource(Actor member) => member.Tactics?.DefinitionId == "core:enemy/banner_follower"
        && SquadLeader(member) is { } leader && SupportReaches(leader, member, EnemySquads.RallyRadius) ? leader : null;

    private EnemyProtection? SquadProtection(Actor target, Actor source, bool direct)
    {
        if (target.Tactics?.DefinitionId == "core:enemy/bodyguard_brute" && SquadLeader(target) is { } leader
            && leader.Tactics!.Enemy!.WardReady && SupportReaches(leader, target, EnemySquads.WardRadius))
            return new("forcefield", leader.Id);
        if (!direct || target.Tactics is not { DefinitionId: "core:enemy/shield_bearer", Enemy: { ShieldLowered: false } memory }
            || target.Tactics.StunnedUntil > Clock || source.Position.Floor != target.Position.Floor) return null;
        var (fx, fy) = AttackGeometry.Offset(memory.ShieldFacing);
        long dx = source.Position.X - target.Position.X, dy = source.Position.Y - target.Position.Y;
        long forward = dx * fx + dy * fy;
        return forward > 0 && forward >= Math.Abs(dx * fy - dy * fx) ? new("directional shield", target.Id) : null;
    }

    private void TriggerEnemySnares(Actor victim, long action, WorldPosition entered)
    {
        if (victim.Life != LifeState.Alive || victim.Tactics == null) return;
        for (int i = State.EnemySnares.Count - 1; i >= 0; i--)
        {
            var snare = State.EnemySnares[i];
            if (snare.Source == victim.Id || snare.Position != entered) continue;
            State.EnemySnares.RemoveAt(i);
            var ready = Friendly(victim) ? State.Queue.Snapshot().Pending.FirstOrDefault(w => w.ActorId == victim.Id && w.Kind == ScheduledKind.ActorReady) : null;
            if (ready != null) State.Queue.Delay(ready.Sequence, EnemySquads.SnareDelay);
            else victim.Tactics.EnemySnareDelay = EnemySquads.SnareDelay;
            Emit("EnemySnareTriggered", snare.Source, victim.Id, action, entered);
            // Movement batches may have already placed a dasher beyond the crossed trap.
            // The victim is still hit, without moving it back or damaging an unrelated occupant.
            ResolveAttack(new(action, snare.Source, entered, [victim.Position], "core:terrain/enemy_snare",
                AttackDelivery.Melee, DamageElement.Physical, 1, Clock, DamageCause.GroundHazard, IgnoresTerrain: true));
        }
    }
}
