using System.Collections.Immutable;
using Roag.Core.Content;
using Roag.Core.Expeditions;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private IEnumerable<BehaviorRule> RetainedEnemyRules(Actor actor)
    {
        IEnumerable<BehaviorRule> rules = _world.Content.Entries[actor.Archetype] switch
        {
            BossDefinition b => (long)actor.Health * 100 <= (long)actor.MaxHealth * b.PhasePercent ? b.Enraged : b.Behaviors,
            CharacterDefinition c => c.Behaviors,
            _ => []
        };
        return actor.VariantId.Length == 0 ? rules : rules.Concat(_world.Content.Get<VariantDefinition>(actor.VariantId).Behaviors);
    }

    private static bool RetainedSupport(AbilityDefinition ability) => ability.Target is TargetKind.Ally or TargetKind.AllAllies or TargetKind.Self;
    private static int RetainedEnemyRange(AbilityDefinition ability) => ability.Target is TargetKind.AllEnemies ? 4
        : RetainedSupport(ability) ? 3 : ability.Id is "ability.strike" or "ability.rend" or "ability.sting" or "ability.nip" or "ability.interrupt" ? 1 : 6;

    private Actor[] RetainedCandidates(Actor actor, AbilityDefinition ability, BehaviorRule rule, Func<WorldPosition, bool>? mistBlocks)
    {
        bool support = RetainedSupport(ability);
        var sight = _world.VisionFrom(actor.Position);
        var candidates = ability.Target == TargetKind.Self ? new[] { actor }
            : _world.State.Actors.Where(a => State.Participants.Contains(a.Id)
                && (support ? a.Tactics?.Enemy != null && !HostileTo(actor, a)
                    && (actor.EncounterLeader == 0 || actor.EncounterLeader == a.EncounterLeader) : HostileTo(actor, a)));
        return candidates.Where(a => a is { Life: LifeState.Alive, Health: > 0, Tactics: not null }
                && a.Position.Distance(actor.Position) <= 11 && sight.Contains(a.Position)
                && (mistBlocks == null || !mistBlocks(a.Position))
                && (rule.Condition != "ally-wounded" || a.Health < a.MaxHealth)
                && (rule.Condition != "self-wounded" || a.Id == actor.Id && a.Health < a.MaxHealth)
                && (rule.Condition != "target-marked" || a.Tactics.Conditions.Any(c => c.Kind == NativeConditionKind.Mark)))
            .OrderBy(a => a.Id).ToArray();
    }

    private EnemyIntent PlanRetainedEnemyTurn(Actor actor, Func<WorldPosition, IReadOnlySet<WorldPosition>, Facing?> nextStep,
        Func<WorldPosition, bool>? mistBlocks)
    {
        var memory = actor.Tactics!.Enemy!.Retained!;
        if (!_world.IsHostile(actor)) return new(EnemyIntentKind.Wait);
        var random = new RoagRandom(memory.RandomState);
        long action = checked(memory.Actions + 1);
        var rules = RetainedEnemyRules(actor).Where(r => action >= r.FromRound && action % r.Every == 0
            && (long)actor.Health * 100 <= (long)actor.MaxHealth * r.BelowHealthPercent
            && (!r.NoRepeat || memory.LastAbility != r.Ability)
            && RetainedCandidates(actor, _world.Content.Get<AbilityDefinition>(r.Ability), r, mistBlocks).Length > 0).ToArray();
        var rule = new BehaviorRule { Ability = _world.Content.Game.EnemyFallback };
        if (rules.Length > 0)
        {
            int pick = random.NextOneBased(rules.Sum(r => r.Weight));
            foreach (var candidate in rules) { pick -= candidate.Weight; if (pick <= 0) { rule = candidate; break; } }
        }
        var ability = _world.Content.Get<AbilityDefinition>(rule.Ability);
        var candidates = RetainedCandidates(actor, ability, rule, mistBlocks);
        if (candidates.Length == 0) return new(EnemyIntentKind.Wait);
        var target = rule.Target switch
        {
            "weakest" => candidates.MinBy(a => (a.Health, a.Id))!,
            "strongest" => candidates.MaxBy(a => (a.Health, -a.Id))!,
            "stressed" => candidates.MaxBy(a => (a.Stress, -a.Id))!,
            _ => candidates[random.NextOneBased(candidates.Length) - 1]
        };
        var cast = new RetainedEnemyCast(target.Id, random.State);
        int range = RetainedEnemyRange(ability);
        bool support = RetainedSupport(ability);
        bool Open(WorldPosition p) => ProjectileOpen(p) && !_world.DungeonMistBlocks(p) && !(mistBlocks?.Invoke(p) ?? false);
        bool Clear(WorldPosition from, WorldPosition to)
        {
            if (!EnemySquads.Clear(from, to, Open)) return false;
            if (support || ability.Target == TargetKind.AllEnemies) return true;
            // A mirror can turn the shot before a body on its original line.
            // Commitment still consumes the actual marker through CommitWindup.
            if (range > 1 && RouteMirrors(actor, from, RetainedRay(from, to).ToArray(), AttackDelivery.Projectile, knownOnly: true).Reflection != null) return true;
            return EnemySquads.Clear(from, to, p => Open(p) && (p == to || BodyAt(p, actor.Id) == null));
        }
        if (target.Position.Distance(actor.Position) <= range && Clear(actor.Position, target.Position))
        {
            var cells = ability.Target is TargetKind.AllEnemies or TargetKind.AllAllies
                ? candidates.Where(a => a.Position.Distance(actor.Position) <= range && Clear(actor.Position, a.Position)).Select(a => a.Position)
                : !support && range > 1 ? RetainedRay(actor.Position, target.Position) : new[] { target.Position };
            return new(EnemyIntentKind.Windup, Ability: ability.Id,
                Delivery: support || ability.Target == TargetKind.AllEnemies ? AttackDelivery.Area : range == 1 ? AttackDelivery.Melee : AttackDelivery.Projectile,
                Cells: cells.ToImmutableArray(), Retained: cast);
        }
        var goals = new HashSet<WorldPosition>();
        for (int y = -range; y <= range; y++) for (int x = -range; x <= range; x++)
        {
            var p = target.Position.Offset(x, y);
            if (p.Distance(target.Position) <= range && _world.CanEnter(p, actor.Id) && PursuitAllowed(actor, p) && Clear(p, target.Position)) goals.Add(p);
        }
        return nextStep(actor.Position, goals) is { } facing
            ? new(EnemyIntentKind.Move, facing, Retained: cast) : new(EnemyIntentKind.Wait, Retained: cast);
    }

    private long? SelectRetainedEnemyTarget(Actor actor, Func<WorldPosition, bool>? mistBlocks)
    {
        var intent = PlanRetainedEnemyTurn(actor, (_, _) => null, mistBlocks);
        if (intent.Retained is { } cast && Friendly(Actor(cast.Target))) return cast.Target;
        var sight = _world.VisionFrom(actor.Position);
        return _world.Party.Where(a => a.Life == LifeState.Alive && a.Position.Distance(actor.Position) <= 11
                && sight.Contains(a.Position) && PursuitAllowed(actor, a.Position))
            .OrderBy(a => a.Position.Distance(actor.Position)).ThenBy(a => a.Id).Select(a => (long?)a.Id).FirstOrDefault();
    }

    private string RetainedEnemyTendency(Actor actor)
    {
        string Target(string value) => value switch
        {
            "weakest" => "lowest current HP (absolute HP, then identity)",
            "strongest" => "highest current HP (then identity)",
            "stressed" => "highest stress (then identity)",
            _ => "a random privately seen eligible target"
        };
        string phase = _world.Content.Entries[actor.Archetype] is BossDefinition b
            ? $"Switches to enraged abilities at {b.PhasePercent}% HP; healing above it restores the normal set. " : "";
        string rules = string.Join("; ", RetainedEnemyRules(actor).Select(r =>
            $"{_world.Content.Get<AbilityDefinition>(r.Ability).Name}: {Target(r.Target)}, weight {r.Weight}"
            + (r.Every > 1 ? $", every {r.Every} casts" : "")
            + (r.FromRound > 1 ? $", from cast {r.FromRound}" : "")
            + (r.BelowHealthPercent < 100 ? $", below {r.BelowHealthPercent}% HP" : "")
            + (r.Condition != "always" ? ", " + r.Condition.Replace('-', ' ') : "") + (r.NoRepeat ? ", never repeats its last released ability" : "")));
        return phase + rules + ". Only physically visible targets qualify. Movement does not advance the cast pattern."
            + " " + _world.WildlifeDescription(actor);
    }

    private void CommitRetainedEnemyCast(Actor actor, EnemyIntent intent)
    {
        var ability = _world.Content.Get<AbilityDefinition>(intent.Ability);
        var cast = intent.Retained ?? throw new InvalidOperationException("Missing imported cast decision.");
        int power = RetainedSupport(ability) ? (ability.Power + 2) / 3 : Math.Max(1, actor.Power + (int)Math.Ceiling((ability.Power - 6) / 3d));
        var guardFacing = actor.Tactics!.Facing;
        if (ability.Effect == "guard" && SelectRetainedEnemyTarget(actor, null) is { } threat)
        {
            var p = Actor(threat).Position; int dx = p.X - actor.Position.X, dy = p.Y - actor.Position.Y;
            guardFacing = Math.Abs(dx) > Math.Abs(dy) ? dx > 0 ? Facing.East : Facing.West : dy > 0 ? Facing.South : Facing.North;
        }
        CommitWindup(ability.Id, intent.Cells, intent.Delivery, ability.Effect == "burn" ? DamageElement.Fire : DamageElement.Physical,
            power, checked(100 * (1 + ability.PreparationTurns)), checked(100 + ability.Mana * State.Cooperation.RecoveryPerMana), cast.Target, retainedAbility: true);
        if (ability.Effect == "guard") actor.Tactics.Facing = guardFacing;
        actor.Tactics!.Enemy!.Retained!.RandomState = cast.RandomState;
        actor.Tactics.Enemy.Retained.Actions++;
    }

    private void ResolveRetainedEnemyAbility(LockedAttack attack)
    {
        var source = Actor(attack.Source);
        var ability = _world.Content.Get<AbilityDefinition>(attack.Ability);
        source.Tactics!.Enemy!.Retained!.LastAbility = ability.Id;
        Emit("RetainedSkillUsed", source.Id, attack.IntendedTarget, attack.Id, attack.Origin, ability: ability.Id);
        if (RetainedSupport(ability))
        {
            foreach (var target in _world.State.Actors.Where(a => a is { Life: LifeState.Alive, Tactics.Enemy: not null }
                         && (ability.Target != TargetKind.Self || a.Id == source.Id)
                         && attack.Cells.Contains(a.Position) && !HostileTo(source, a) && SupportOpen(a.Position)
                         && EnemySquads.Clear(attack.Origin, a.Position, SupportOpen)).ToArray())
            {
                if (ability.Effect is "heal" or "potion" or "cleanse")
                {
                    int before = target.Health; target.Health = Math.Min(target.MaxHealth, checked(before + attack.Damage));
                    Emit("Healed", source.Id, target.Id, attack.Id, target.Position, target.Health - before, ability: ability.Id);
                    CleanseNativeConditions(target, ability.Effect == "cleanse");
                    if (ability.Effect == "cleanse") { RemoveBurning(target); target.Tactics!.StunnedUntil = 0; target.Stress = Math.Max(0, target.Stress - 10); }
                }
                if (ability.Effect == "guard")
                {
                    // Facing was committed with the windup; moving prey cannot rotate it.
                    target.Tactics!.Guarding = true;
                    Emit("Guarded", source.Id, target.Id, attack.Id, target.Position, ability: ability.Id);
                }
                if (ability.Effect is "calm" or "rally")
                {
                    int before = target.Stress; target.Stress = Math.Max(0, before - (ability.Effect == "rally" ? 8 : ability.Power));
                    Emit("StressChanged", source.Id, target.Id, attack.Id, target.Position, before - target.Stress, ability: ability.Id);
                    if (ability.Effect == "rally") ApplyNativeCondition(target, NativeConditionKind.Brace, source, attack.Id, ability.Id, 2, 1);
                }
            }
            return;
        }
        long firstEvent = State.NextEvent;
        ResolveDamageBatch(attack with { Cells = RetainedImpactCells(attack).ToArray() });
        var hits = State.Events.Where(e => e.Sequence >= firstEvent && e.Kind == "HitConfirmed" && e.Action == attack.Id
            && e.Source == source.Id && e.Ability == ability.Id).ToArray();
        foreach (var hit in hits)
        {
            var target = Actor(hit.Target);
            if (ability.Effect == "drain" && source.Life == LifeState.Alive)
            {
                int before = source.Health; source.Health = Math.Min(source.MaxHealth, before + Math.Max(1, hit.Amount / 2));
                Emit("Healed", source.Id, source.Id, attack.Id, source.Position, source.Health - before, ability: ability.Id);
            }
            if (target is not { Life: LifeState.Alive, Health: > 0 }) continue;
            switch (ability.Effect)
            {
                case "bleed": ApplyNativeCondition(target, NativeConditionKind.Bleed, source, attack.Id, ability.Id, 3, ability.Duration); break;
                case "mark": ApplyNativeCondition(target, NativeConditionKind.Mark, source, attack.Id, ability.Id, ability.Power, ability.Duration); break;
                case "stun": StunNativeActor(source, target, attack.Id, ability.Id, checked(Math.Min(2, ability.Duration) * Tuning.UnitsPerSecond)); break;
                case "dread": ApplyNativeDread(source, target, attack.Id, ability.Id, Math.Max(8, ability.Power)); break;
                case "burn": ApplyNativeBurning(source, target, attack.Id, ability.Id, 3, ability.Duration); break;
            }
        }
    }

    private IEnumerable<WorldPosition> RetainedImpactCells(LockedAttack attack)
    {
        if (attack.Delivery != AttackDelivery.Projectile)
            return attack.Cells.Where(p => EnemySquads.Clear(attack.Origin, p, SupportOpen));
        // Follow the committed path, including any mirror turn. A newly built
        // wall or blocked diagonal stops travel; it cannot remove one cell and
        // allow the projectile to resume behind that obstruction.
        var cells = new List<WorldPosition>(); var previous = attack.Origin;
        foreach (var cell in attack.Cells)
        {
            if (!EnemySquads.Clear(previous, cell, p => SupportOpen(p) && (p == cell || BodyAt(p, attack.Source) == null))) break;
            cells.Add(cell); previous = cell;
        }
        return cells;
    }

    private static IEnumerable<WorldPosition> RetainedRay(WorldPosition from, WorldPosition to)
    {
        int dx = Math.Abs(to.X - from.X), dy = Math.Abs(to.Y - from.Y), sx = Math.Sign(to.X - from.X), sy = Math.Sign(to.Y - from.Y), error = dx - dy;
        while (from != to)
        {
            int twice = error * 2;
            if (twice > -dy) { error -= dy; from = from.Offset(sx, 0); }
            if (twice < dx) { error += dx; from = from.Offset(0, sy); }
            yield return from;
        }
    }

    private void ValidateRetainedWindup(Actor actor, string ability, bool retained)
    {
        if (retained != (actor.Tactics?.Enemy?.Retained != null)
            || retained && !RetainedAbilities(_world.Content, actor).Contains(ability, StringComparer.Ordinal))
            throw new ArgumentException("Imported cast does not match its source controller.");
    }

    private static IEnumerable<string> RetainedAbilities(ContentCatalog content, Actor actor)
    {
        var rules = content.Entries[actor.Archetype] switch
        {
            BossDefinition b => b.Behaviors.Concat(b.Enraged),
            CharacterDefinition c => c.Behaviors,
            _ => Enumerable.Empty<BehaviorRule>()
        };
        if (actor.VariantId.Length > 0) rules = rules.Concat(content.Get<VariantDefinition>(actor.VariantId).Behaviors);
        return rules.Select(r => r.Ability).Append(content.Game.EnemyFallback);
    }

    internal static void ValidateRetainedEnemies(WorldState world, ContentCatalog content)
    {
        foreach (var actor in world.Actors.Where(a => a.Tactics?.Enemy?.Retained != null))
        {
            if (actor.Tactics!.DefinitionId != RetainedEnemyCatalog.Id(actor.Archetype)
                || actor.Kind is not (ActorKind.Beast or ActorKind.Raider)
                || actor.Tactics.Enemy!.Retained!.LastAbility is { Length: > 0 } last && !RetainedAbilities(content, actor).Contains(last, StringComparer.Ordinal))
                throw new InvalidDataException("Imported enemy source identity/history mismatch.");
        }
        foreach (var attack in world.Tactics!.Attacks)
        {
            var source = world.Actors.FirstOrDefault(a => a.Id == attack.Source);
            if (attack.RetainedAbility != (source?.Tactics?.Enemy?.Retained != null)
                || attack.RetainedAbility && (attack.IntendedTarget <= 0 || !RetainedAbilities(content, source!).Contains(attack.Ability, StringComparer.Ordinal)
                    || attack.Support != null || attack.Advance != null || attack.Shove != null || attack.ClassGenerated))
                throw new InvalidDataException("Invalid imported locked cast.");
        }
    }
}
