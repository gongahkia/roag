using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private EnemyIntent RetainedIdleTurn(Actor actor)
    {
        if (_world.State.NativeCampaign == null) return new(EnemyIntentKind.Wait);
        var ecology = _world.Ecology(actor);
        if (ecology == null && actor.EncounterLeader > 0) return new(EnemyIntentKind.Wait);
        (Facing Facing, WorldPosition Cell)[] Neighbours() => new[] { Facing.North, Facing.East, Facing.South, Facing.West }
            .Select(f => { var (x, y) = AttackGeometry.Offset(f); return (Facing: f, Cell: actor.Position.Offset(x, y)); })
            .Where(p => _world.CanEnter(p.Cell, actor.Id)).ToArray();
        if (ecology?.Behavior == "flee")
        {
            var nearby = _world.Party.Where(p => p.Life == LifeState.Alive && p.Position.Distance(actor.Position) <= ecology.Notice).ToArray();
            var sight = nearby.Length == 0 ? null : _world.VisionFrom(actor.Position);
            var threat = nearby.Where(p => sight!.Contains(p.Position))
                .OrderBy(p => p.Position.Distance(actor.Position)).ThenBy(p => p.Id).FirstOrDefault();
            if (threat != null)
            {
                var escapes = Neighbours().Where(p => p.Cell.Distance(threat.Position) > actor.Position.Distance(threat.Position))
                    .OrderByDescending(p => p.Cell.Distance(threat.Position)).ThenBy(p => p.Cell.Distance(actor.Habitat ?? actor.Position)).ToArray();
                return escapes.Length == 0 ? new(EnemyIntentKind.Wait) : new(EnemyIntentKind.Move, escapes[0].Facing);
            }
        }
        if (actor.Migration?.Target != null || actor.Habitat is { } && actor.Position.Distance(actor.Habitat.Value) > 2)
        {
            var home = actor.Migration?.Target ?? actor.Habitat!.Value;
            var goals = new[] { home, home.Offset(0, -1), home.Offset(1, 0), home.Offset(0, 1), home.Offset(-1, 0) }
                .Where(p => _world.CanEnter(p, actor.Id)).ToHashSet();
            var step = _world.Navigate(actor, goals, p => p.Floor == actor.Position.Floor);
            return step == null ? new(EnemyIntentKind.Wait) : new(step.Action == TacticalAction.UseStairs ? EnemyIntentKind.UseStairs : EnemyIntentKind.Move, step.Facing);
        }
        // Authored guardians/attendants hold their site. Unaffiliated creatures
        // wander, while habitat species stay within two tiles of their home.
        if (actor.EncounterLeader > 0 || ecology == null && _world.Content.Entries[actor.Archetype] is Roag.Core.Content.BossDefinition)
            return new(EnemyIntentKind.Wait);
        // The retained world wandered once per five 12-second world ticks.
        // Phase that cadence by identity; combat/flee/search decisions above
        // keep their immediate paid turns, independent of the camera.
        if ((Clock / Tuning.UnitsPerSecond + actor.Id) % 60 != 0) return new(EnemyIntentKind.Wait);
        var choices = Neighbours().Where(p => actor.Habitat is not { } home || p.Cell.Distance(home) <= 2).ToArray();
        uint roll = WorldRandom.Hash(_world.Seed, unchecked((int)actor.Id), unchecked((int)(Clock / Tuning.UnitsPerSecond)), 11401);
        return choices.Length == 0 || roll % 3 != 0 ? new(EnemyIntentKind.Wait) : new(EnemyIntentKind.Move, choices[roll % choices.Length].Facing);
    }
}
