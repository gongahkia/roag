using System.Collections.Immutable;
using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

internal static class EnemyAttackRules
{
    public static void Validate(EnemySupport? support, EnemyAdvance? advance, EnemyShove? shove = null)
    {
        if ((support != null ? 1 : 0) + (advance != null ? 1 : 0) + (shove != null ? 1 : 0) > 1
            || support != null && (support.Kind == EnemySupportKind.Gulp ? support.Drop <= 0 : support.Drop != 0)
            || support != null && (support.Kind == EnemySupportKind.Revive ? support.Corpse <= 0 : support.Corpse != 0)
            || support != null && (support.Kind == EnemySupportKind.Web ? support.Web is not { Valid: true } : support.Web != null)
            || shove != null && (!Enum.IsDefined(shove.Facing) || !AttackGeometry.Cardinal(shove.Facing))
            || support != null && (support.Kind switch
            {
                EnemySupportKind.Heal => support.Power is < 1 or > 20 || support.Definition != "" || support.MaximumActive != 0 || support.Target < 0,
                EnemySupportKind.Summon => support.Power != 0 || support.Definition != "core:enemy/ruin_skitter" || support.MaximumActive is < 1 or > 3 || support.Target != 0,
                EnemySupportKind.Ward or EnemySupportKind.Snare or EnemySupportKind.Phase or EnemySupportKind.Gulp or EnemySupportKind.Revive or EnemySupportKind.Alarm or EnemySupportKind.Web
                    => support.Power != 0 || support.Definition != "" || support.MaximumActive != 0 || support.Target != 0,
                _ => true
            })
            || advance != null && (!Enum.IsDefined(advance.Facing) || !AttackGeometry.Cardinal(advance.Facing) || advance.Steps is < 1 or > 3))
            throw new InvalidDataException("Invalid enemy support or advance.");
    }

    public static bool PhaseGeometry(WorldPosition from, IReadOnlyList<WorldPosition> cells) => cells.Count == 1
        && cells[0].Floor == from.Floor && cells[0].Distance(from) == 2 && (cells[0].X == from.X || cells[0].Y == from.Y);

    public static bool SpecialIdentityValid(string definition, string ability, WorldPosition origin,
        IReadOnlyList<WorldPosition>? cells, EnemySupport? support, EnemyShove? shove)
    {
        if (shove != null || ability == "core:ability/rammer_shove")
        {
            if (definition != "core:enemy/heavy_rammer" || ability != "core:ability/rammer_shove" || shove == null
                || cells?.Count != 1 || cells[0] != shove.Cell) return false;
            var (dx, dy) = AttackGeometry.Offset(shove.Facing);
            return shove.Cell == origin.Offset(dx, dy);
        }
        if (support?.Kind == EnemySupportKind.Phase || ability == "core:ability/wraith_phase")
            return definition == "core:enemy/hollow_wraith" && ability == "core:ability/wraith_phase"
                && support?.Kind == EnemySupportKind.Phase && cells != null && PhaseGeometry(origin, cells);
        if (support?.Kind == EnemySupportKind.Gulp || ability == "core:ability/toad_gulp")
            return definition == "core:enemy/gulp_toad" && ability == "core:ability/toad_gulp"
                && support?.Kind == EnemySupportKind.Gulp && cells?.Count == 1
                && cells[0].Floor == origin.Floor && cells[0].Distance(origin) <= 1;
        if (support?.Kind == EnemySupportKind.Revive || ability == "core:ability/cantor_revive")
            return definition == "core:enemy/grave_cantor" && ability == "core:ability/cantor_revive"
                && support?.Kind == EnemySupportKind.Revive && cells?.Count == 1 && cells[0].Floor == origin.Floor;
        if (support?.Kind == EnemySupportKind.Alarm || ability == "core:ability/keeper_alarm")
            return definition == "core:enemy/alarm_keeper" && ability == "core:ability/keeper_alarm"
                && support?.Kind == EnemySupportKind.Alarm && cells?.Count is >= 1 and <= 2
                && cells.Distinct().Count() == cells.Count && cells.All(p => p.Floor == origin.Floor && p.Distance(origin) == 1);
        if (support?.Kind == EnemySupportKind.Web || ability == "core:ability/weaver_web")
            return definition == "core:enemy/web_weaver" && ability == "core:ability/weaver_web"
                && support is { Kind: EnemySupportKind.Web, Web: { Valid: true } span }
                && span.A.Floor == origin.Floor && cells != null && span.Cells().SequenceEqual(cells);
        return true;
    }
}

public sealed partial class TacticalCombat
{
    private void ResolveEnemySupport(LockedAttack attack)
    {
        var owner = Actor(attack.Source); var support = attack.Support!;
        Emit("SupportResolved", owner.Id, 0, attack.Id, owner.Position, ability: attack.Ability);
        if (support.Kind == EnemySupportKind.Gulp)
            SwallowLoot(owner, support.Drop, attack.Cells[0], attack.Id);
        else if (support.Kind == EnemySupportKind.Revive)
            ResolveRevival(owner, support.Corpse, attack.Cells[0], attack.Id);
        else if (support.Kind == EnemySupportKind.Alarm)
            ResolveAlarm(owner, attack);
        else if (support.Kind == EnemySupportKind.Web)
            ResolveWeb(owner, attack);
        else if (support.Kind == EnemySupportKind.Heal)
        {
            foreach (var cell in attack.Cells)
            {
                var target = _world.OccupantAt(cell, owner.Id);
                if (target is not { Life: LifeState.Alive, Tactics: not null } || HostileTo(owner, target)
                    || support.Target != 0 && support.Target != target.Id) continue;
                int gain = Math.Min(support.Power, target.MaxHealth - target.Health);
                if (gain <= 0) continue;
                target.Health += gain;
                Emit("Healed", owner.Id, target.Id, attack.Id, cell, gain, ability: attack.Ability);
            }
        }
        else if (support.Kind == EnemySupportKind.Ward)
        {
            owner.Tactics!.Enemy!.WardReady = true;
            Emit("SquadWardRaised", owner.Id, 0, attack.Id, owner.Position);
        }
        else if (support.Kind == EnemySupportKind.Phase)
        {
            var landing = attack.Cells[0];
            if (!EnemyAttackRules.PhaseGeometry(owner.Position, attack.Cells) || !_world.CanEnter(landing, owner.Id) || ProtectedForcedLanding(landing))
                Emit("PhaseBlocked", owner.Id, 0, attack.Id, landing);
            else
            {
                var from = owner.Position; _world.MoveTacticalActor(owner, landing);
                Emit("Displaced", owner.Id, owner.Id, attack.Id, landing, ability: "phase", forced: true, from: from);
                Emit("TileEntered", owner.Id, owner.Id, attack.Id, landing, forced: true, from: from);
            }
        }
        else if (support.Kind == EnemySupportKind.Snare)
        {
            foreach (var cell in attack.Cells)
                if (State.EnemySnares.Count(s => s.Source == owner.Id) < EnemySquads.SnareCap
                    && !State.EnemySnares.Any(s => s.Position == cell) && _world.CanEnter(cell))
                {
                    State.EnemySnares.Add(new(owner.Id, cell));
                    Emit("EnemySnarePlaced", owner.Id, 0, attack.Id, cell);
                }
        }
        else if (support.Kind == EnemySupportKind.Summon)
        {
            int active = _world.State.Actors.Count(a => a.Life == LifeState.Alive && a.Tactics?.Enemy?.SummonedBy == owner.Id);
            foreach (var cell in attack.Cells)
            {
                if (active >= support.MaximumActive || owner.Tactics!.Enemy!.Summons >= 3) break;
                if (!_world.CanEnter(cell)) continue;
                // Native rules own stats/AI. This content entry supplies the retained universal-atlas appearance,
                // without borrowing the ordinary wildlife spider's independent ecology or loot policy.
                var spawned = ActorFactory.Create(_world.Content, "character.roag-ruin-skitter", _world.Seed,
                    _world.State.NextActorId++, cell, owner.HomeTown, owner.Culture, owner.FactionId);
                _world.State.Actors.Add(spawned);
                RegisterEnemy(spawned, support.Definition, checked(Clock + 1));
                spawned.Tactics!.Enemy!.SummonedBy = owner.Id;
                owner.Tactics!.Enemy!.Summons++; active++;
                Emit("EnemySummoned", owner.Id, spawned.Id, attack.Id, cell, ability: support.Definition);
            }
        }
    }

    private void ResolveEnemyAdvance(LockedAttack attack)
    {
        var actor = Actor(attack.Source); if (actor.Life != LifeState.Alive) return;
        var advance = attack.Advance!; var from = actor.Position; var (dx, dy) = AttackGeometry.Offset(advance.Facing);
        var path = new List<WorldPosition>();
        for (int i = 0; i < advance.Steps; i++)
        {
            var next = actor.Position.Offset(dx, dy); if (!_world.CanEnter(next, actor.Id)) break;
            actor.Position = next; path.Add(next);
        }
        if (path.Count == 0) return;
        Emit("Moved", actor.Id, actor.Id, attack.Id, actor.Position, from: from,
            snapshot: CaptureBoonEvent(BoonTrigger.Moved, actor.Id, actor.Id, attack.Id, actor.Position, from: from)
                with { Path = path.ToImmutableArray() });
        foreach (var cell in path)
        {
            Emit("TileEntered", actor.Id, actor.Id, attack.Id, cell, from: from);
            from = cell;
        }
    }
}
