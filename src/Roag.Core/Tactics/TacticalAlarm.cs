using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

internal static class EnemyAlarmRules
{
    public static void Validate(TacticalWorldState state, IReadOnlyDictionary<long, Actor> actors)
    {
        var counts = new Dictionary<long, int>();
        foreach (var actor in actors.Values)
            if (actor.Tactics?.Enemy is { CalledBy: > 0 } memory)
            {
                if (memory.CalledBy >= actor.Id || !actors.TryGetValue(memory.CalledBy, out var caller)
                    || caller.Tactics is not { DefinitionId: "core:enemy/alarm_keeper", Enemy.AlarmSpent: true }
                    || counts.GetValueOrDefault(caller.Id) >= 2)
                    throw new InvalidDataException("Invalid alarm reinforcement ancestry or population.");
                counts[caller.Id] = counts.GetValueOrDefault(caller.Id) + 1;
            }
        foreach (var attack in state.Attacks)
            if (attack.Support?.Kind == EnemySupportKind.Alarm
                && (!actors.TryGetValue(attack.Source, out var caller)
                    || caller.Tactics is not { DefinitionId: "core:enemy/alarm_keeper", Enemy.AlarmSpent: true }
                    || counts.ContainsKey(caller.Id)))
                throw new InvalidDataException("Invalid committed alarm budget.");
    }
}

public sealed partial class TacticalCombat
{
    private void ResolveAlarm(Actor owner, LockedAttack attack)
    {
        foreach (var cell in attack.Cells)
        {
            if (cell.Floor != owner.Position.Floor || cell.Distance(owner.Position) != 1
                || !_world.CanEnter(cell) || ProtectedForcedLanding(cell))
            {
                Emit("AlarmBlocked", owner.Id, 0, attack.Id, cell);
                continue;
            }
            var called = ActorFactory.Create(_world.Content, "character.roag-ruin-skitter", _world.Seed,
                _world.State.NextActorId++, cell, owner.HomeTown, owner.Culture, owner.FactionId);
            _world.State.Actors.Add(called);
            RegisterEnemy(called, "core:enemy/ruin_skitter", checked(Clock + 1), rewardExempt: true);
            called.Tactics!.Enemy!.CalledBy = owner.Id;
            Emit("AlarmReinforcement", owner.Id, called.Id, attack.Id, cell);
        }
    }
}
