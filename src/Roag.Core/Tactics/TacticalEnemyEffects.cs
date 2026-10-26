using System.Collections.Immutable;
using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

internal static class EnemyAttackRules
{
    public static void Validate(EnemySupport? support, EnemyAdvance? advance)
    {
        if (support != null && advance != null
            || support != null && (support.Kind switch
            {
                EnemySupportKind.Heal => support.Power is < 1 or > 20 || support.Definition != "" || support.MaximumActive != 0 || support.Target < 0,
                EnemySupportKind.Summon => support.Power != 0 || support.Definition != "core:enemy/ruin_skitter" || support.MaximumActive is < 1 or > 3 || support.Target != 0,
                EnemySupportKind.Ward or EnemySupportKind.Snare => support.Power != 0 || support.Definition != "" || support.MaximumActive != 0 || support.Target != 0,
                _ => true
            })
            || advance != null && (!Enum.IsDefined(advance.Facing) || !AttackGeometry.Cardinal(advance.Facing) || advance.Steps is < 1 or > 3))
            throw new InvalidDataException("Invalid enemy support or advance.");
    }
}

public sealed partial class TacticalCombat
{
    private void ResolveEnemySupport(LockedAttack attack)
    {
        var owner = Actor(attack.Source); var support = attack.Support!;
        Emit("SupportResolved", owner.Id, 0, attack.Id, owner.Position, ability: attack.Ability);
        if (support.Kind == EnemySupportKind.Heal)
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
