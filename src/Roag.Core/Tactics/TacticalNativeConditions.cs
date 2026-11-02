using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum NativeConditionKind { Bleed, Mark, Brace }
public sealed record NativeCondition(NativeConditionKind Kind, long Source, long Action, string Ability, int Power, int Turns);
internal sealed record MarkAssistance(long Source, int Power);

public sealed partial class TacticalCombat
{
    internal void ApplyNativeCondition(Actor target, NativeConditionKind kind, Actor source, long action, string ability, int power, int turns)
    {
        if (target is not { Life: LifeState.Alive, Tactics: not null }) return;
        var previous = target.Tactics.Conditions.FirstOrDefault(c => c.Kind == kind);
        target.Tactics.Conditions.RemoveAll(c => c.Kind == kind);
        target.Tactics.Conditions.Add(new(kind, source.Id, action, ability, Math.Max(power, previous?.Power ?? 0), turns));
        // These are imported ability conditions, distinct from original boon
        // Burning and from the five-own-turn permanent-death countdown.
        Emit("ConditionApplied", source.Id, target.Id, action, target.Position, turns, ability: ability);
    }

    private bool ResolveNativeConditions(Actor actor)
    {
        if (actor.Tactics!.Conditions.Count == 0) return true;
        var before = actor.Life;
        foreach (var condition in actor.Tactics.Conditions.ToArray())
        {
            actor.Tactics.Conditions.Remove(condition);
            if (condition.Turns > 1) actor.Tactics.Conditions.Add(condition with { Turns = condition.Turns - 1 });
            else Emit("ConditionExpired", condition.Source, actor.Id, condition.Action, actor.Position, ability: condition.Ability);
            if (condition.Kind != NativeConditionKind.Bleed) continue;
            int power = Math.Max(1, condition.Power - _world.PreparationPower(actor, "bleed-resist"));
            ResolveAttack(new(condition.Action, condition.Source, Actor(condition.Source).Position, [actor.Position], condition.Ability,
                AttackDelivery.Area, DamageElement.Physical, power, Clock, DamageCause.OngoingEffect, PersistsWithoutOwner: true));
            if (actor.Life == LifeState.Dead || actor.Life != before) return false;
        }
        return true;
    }

    internal void ApplyNativeDread(Actor source, Actor target, long action, string ability, int stress)
    {
        int amount = Math.Max(0, stress - _world.PreparationPower(target, "focus") * 3 - _world.EquipmentEffectPower(target, "resolve"));
        int before = target.Stress;
        target.Stress = Math.Min(100, before + amount);
        Emit("StressChanged", source.Id, target.Id, action, target.Position, target.Stress - before, ability: ability);
    }

    internal void CleanseNativeConditions(Actor target, bool complete)
        => target.Tactics!.Conditions.RemoveAll(c => c.Kind == NativeConditionKind.Bleed || complete && c.Kind == NativeConditionKind.Mark);

    private EquipmentCover? ConsumeNativeBrace(Actor target, int damage)
    {
        var brace = target.Tactics?.Conditions.FirstOrDefault(c => c.Kind == NativeConditionKind.Brace);
        if (brace == null || damage <= 0) return null;
        int absorbed = Math.Min(damage, brace.Power);
        target.Tactics!.Conditions.Remove(brace);
        if (brace.Power > absorbed) target.Tactics.Conditions.Add(brace with { Power = brace.Power - absorbed });
        return new(brace.Source, absorbed);
    }

    internal static void ValidateNativeConditions(WorldState world, IReadOnlyDictionary<long, Actor> actors)
    {
        static bool SourceMatches(NativeCondition c) => ContentCatalog.Default.Entries.GetValueOrDefault(c.Ability) is AbilityDefinition ability
            && c.Turns <= ability.Duration && (c.Kind switch
            {
                NativeConditionKind.Bleed => ability.Effect == "bleed",
                NativeConditionKind.Mark => ability.Effect == "mark",
                NativeConditionKind.Brace => ability.Effect == "rally",
                _ => false
            });
        foreach (var actor in actors.Values)
        {
            if (actor.Tactics is not { } tactics) continue;
            if (tactics.Conditions == null || tactics.Conditions.Count > 3
                || tactics.Conditions.Select(c => c?.Kind).Distinct().Count() != tactics.Conditions.Count
                || tactics.Conditions.Any(c => c == null || !Enum.IsDefined(c.Kind) || c.Turns is < 1 or > 12 || c.Power is < 1 or > 100
                    || c.Action < 1 || c.Action >= world.Tactics!.NextAction || !actors.ContainsKey(c.Source)
                    || !world.Tactics.Participants.Contains(c.Source) || !world.Tactics.Participants.Contains(actor.Id)
                    || !SourceMatches(c)
                    || actor.Life == LifeState.Dead))
                throw new InvalidDataException("Invalid native ability condition or owner.");
        }
    }
}
