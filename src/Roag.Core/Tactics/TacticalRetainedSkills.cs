using System.Collections.Immutable;
using Roag.Core.Content;
using Roag.Core.Expeditions;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record RetainedSkillOption(string Boon, string Ability, string Name, string Sprite, string Description);

public sealed partial class TacticalCombat
{
    public IReadOnlyList<RetainedSkillOption> RetainedSkills(long memberId)
    {
        var member = _world.Party.FirstOrDefault(a => a.Id == memberId);
        if (member?.Tactics?.Build == null) return [];
        return RetainedBoons.All.Where(e => e.Active && member.Tactics.Boons.Inventory.Stacks.ContainsKey(e.Id)).Select(e =>
        {
            var skill = _world.Content.Get<AbilityDefinition>(e.Source);
            string strength = skill.Effect == "heal" ? $" Restores {RetainedSkillPower(member, e, skill)} HP."
                : skill.Effect == "calm" ? $" Removes {skill.Power + RetainedTalentPower(member, "calm")} stress."
                : skill.Effect == "rally" ? $" Wards {1 + Math.Min(20, member.Tactics.Boons.Inventory.Magnitude(e.Id).Power)} damage."
                : $" Base power {RetainedSkillPower(member, e, skill)}.";
            return new RetainedSkillOption(e.Id, e.Source, skill.Name, e.Sprite, RetainedSkillDescription(e.Source) + strength);
        }).ToArray();
    }

    private static int RetainedSkillRange(string ability) => ability switch
    { "ability.shield-bash" => 1, "ability.chain-arc" => 4, "ability.mend" or "ability.stand-fast" or "ability.absolution" => 3, _ => 6 };

    private static string RetainedSkillDescription(string ability) => ability switch
    {
        "ability.shield-bash" => "Strike an adjacent enemy and interrupt its attack with one turn of stun.",
        "ability.stand-fast" => "Nearby visible companions lose stress and gain a small damage ward until their next turn. Guard remains a separate facing action.",
        "ability.hunters-mark" => "Strike a visible enemy within six tiles and mark it for three of its turns; direct attacks gain four power.",
        "ability.volley" => "Strike every visible hostile within six clear tiles. Walls and floors block shots.",
        "ability.ember" => "Strike a visible enemy within six clear tiles and apply three burning ticks.",
        "ability.chain-arc" => "Strike every visible hostile within four clear tiles with lightning. Stormcallers spend one charge.",
        "ability.mend" => "Heal a conscious companion within three clear tiles and stop ongoing bleeding. Cannot revive or remove wounds.",
        "ability.absolution" => "Remove stress from visible companions within three clear tiles.",
        _ => throw new InvalidDataException("Unmapped retained active skill.")
    };

    private IEnumerable<Actor> RetainedSkillTargets(Actor actor, AbilityDefinition skill, long target)
    {
        int range = RetainedSkillRange(skill.Id);
        var candidates = skill.Target switch
        {
            TargetKind.AllAllies => _world.Party,
            TargetKind.AllEnemies => _world.State.Actors.Where(a => HostileTo(actor, a)),
            TargetKind.Self => new[] { actor },
            _ => _world.State.Actors.Where(a => a.Id == (target == 0 && skill.Target == TargetKind.Ally ? actor.Id : target))
        };
        bool friendly = skill.Target is TargetKind.Ally or TargetKind.AllAllies or TargetKind.Self;
        var sight = _world.VisionFrom(actor.Position);
        return candidates.Where(a => a is { Life: LifeState.Alive, Health: > 0, Tactics: not null }
            && (friendly ? Friendly(a) : HostileTo(actor, a)) && a.Position.Distance(actor.Position) <= range
            && sight.Contains(a.Position) && EnemySquads.Clear(actor.Position, a.Position, p => SupportOpen(p)
                && (friendly || skill.Id == "ability.chain-arc" || p == a.Position || BodyAt(p, actor.Id) == null))).OrderBy(a => a.Id);
    }

    private int RetainedSkillPower(Actor actor, RetainedBoon entry, AbilityDefinition skill)
    {
        int strength = (int)Math.Min(20, actor.Tactics!.Boons.Inventory.Magnitude(entry.Id).Power);
        return skill.Effect == "heal" ? checked((skill.Power + 2) / 3 + strength - 1
            + _world.EquipmentEffectPower(actor, "healing") + RetainedTalentPower(actor, "healing"))
            : checked(actor.Power + Math.Max(0, (skill.Power - 6 + 2) / 3) + strength - 1
                + (skill.Effect == "burn" ? _world.EquipmentEffectPower(actor, "burn-power") : 0));
    }

    private TacticalPreview PreviewRetainedSkill(Actor actor, TacticalIntent intent)
    {
        if (!Friendly(actor) || actor.Tactics?.Build == null || RetainedBoons.Find(intent.Item) is not { Active: true } entry
            || !actor.Tactics.Boons.Inventory.Stacks.ContainsKey(entry.Id)) return Invalid("Choose an active skill unlocked by this member's own boon.");
        var skill = _world.Content.Get<AbilityDefinition>(entry.Source);
        if (skill.Id == "ability.chain-arc" && actor.Tactics.Build.Class == ClassKind.Stormcaller && actor.Tactics.Resources.StormCharge == 0)
            return Invalid("Chain arc needs one of this Stormcaller's charges.");
        var targets = RetainedSkillTargets(actor, skill, intent.Target).ToArray();
        if (targets.Length == 0) return Invalid("Choose a conscious target within this skill's clear visible range.");
        if (skill.Effect == "heal" && targets.All(a => a.Health == a.MaxHealth && !a.Tactics!.Conditions.Any(c => c.Kind == NativeConditionKind.Bleed)))
            return Invalid("This conscious member needs neither healing nor bleeding treatment.");
        if (skill.Effect == "calm" && targets.All(a => a.Stress == 0)) return Invalid("No nearby visible companion needs calming.");
        int cost = checked(ClassCatalog.Get(actor.Tactics.Build.Class).Costs.Special
            + RetainedManaEquivalent(actor, skill.Mana) * State.Cooperation.RecoveryPerMana);
        return new(true, "", ActionTiming.Recovery(cost, EffectiveSpeed(actor)), targets.Select(a => a.Position).ToArray())
        { Damage = skill.Effect is "heal" or "calm" or "rally" ? null : RetainedSkillPower(actor, entry, skill) };
    }

    private void CommitRetainedSkill(Actor actor, TacticalIntent intent, long action)
    {
        var entry = RetainedBoons.Find(intent.Item)!; var skill = _world.Content.Get<AbilityDefinition>(entry.Source);
        var targets = RetainedSkillTargets(actor, skill, intent.Target).ToArray();
        int power = RetainedSkillPower(actor, entry, skill);
        if (skill.Id == "ability.chain-arc" && actor.Tactics!.Build!.Class == ClassKind.Stormcaller)
        { actor.Tactics.Resources.StormCharge--; Emit("StormChargeSpent", actor.Id, actor.Id, action, actor.Position, 1, ability: skill.Id); }
        Emit("RetainedSkillUsed", actor.Id, intent.Target, action, actor.Position, ability: skill.Id);
        if (skill.Effect is "heal" or "calm" or "rally")
        {
            foreach (var target in targets)
            {
                if (skill.Effect == "heal")
                { CleanseNativeConditions(target, complete: false); HealCampCompanion(actor, target, power, action, skill.Id); }
                else
                {
                    int before = target.Stress;
                    target.Stress = Math.Max(0, before - (skill.Effect == "calm" ? skill.Power : 8) - RetainedTalentPower(actor, "calm"));
                    Emit("StressChanged", actor.Id, target.Id, action, target.Position, before - target.Stress, ability: skill.Id);
                    if (skill.Effect == "rally") ApplyNativeCondition(target, NativeConditionKind.Brace, actor, action, skill.Id,
                        1 + (int)Math.Min(20, actor.Tactics!.Boons.Inventory.Magnitude(entry.Id).Power), 1);
                }
            }
            return;
        }
        var element = skill.Effect == "burn" ? DamageElement.Fire : skill.Id == "ability.chain-arc" ? DamageElement.Lightning : DamageElement.Physical;
        var delivery = skill.Target == TargetKind.AllEnemies ? AttackDelivery.Area : skill.Id == "ability.shield-bash" ? AttackDelivery.Melee : AttackDelivery.Projectile;
        long firstEvent = State.NextEvent;
        if (!Strike(actor, action, new(skill.Id, delivery, element, targets.Select(t => t.Position).ToArray()), power, actor.Position, null)) return;
        var hit = State.Events.Where(e => e.Sequence >= firstEvent && e.Kind == "HitConfirmed" && e.Source == actor.Id && e.Action == action && e.Ability == skill.Id)
            .Select(e => e.Target).ToHashSet();
        foreach (var target in targets.Where(a => a is { Life: LifeState.Alive, Health: > 0 } && hit.Contains(a.Id)))
        {
            if (skill.Effect == "mark") ApplyNativeCondition(target, NativeConditionKind.Mark, actor, action, skill.Id, skill.Power, skill.Duration);
            if (skill.Effect == "stun") StunNativeActor(actor, target, action, skill.Id, checked(Math.Min(2, skill.Duration) * Tuning.UnitsPerSecond));
            if (skill.Effect == "burn")
            {
                var e = CaptureBoonEvent(BoonTrigger.StatusApplied, actor.Id, target.Id, action, target.Position, skill.Id);
                Emit("RetainedBurnApplied", actor.Id, target.Id, action, target.Position, 3, ability: skill.Id);
                var activation = new BoonActivation(State.BoonEffects.Dispatch.NextActivationId++, entry.Id, actor.Id, BoonFaction(actor), actor.Position,
                    e.ChainId, e.RootActionId, e.Id, e.Ancestry.Add(new(actor.Id, entry.Id)), 3, e);
                WithBoonCause(BoonCause.From(activation), () => ApplyBurning(target, activation, 3, Tuning.UnitsPerSecond, skill.Duration));
            }
        }
    }

    internal void StunNativeActor(Actor source, Actor target, long action, string ability, int duration)
    {
        if (target is not { Life: LifeState.Alive, Tactics: { Invulnerable: false } tactics }) return;
        long until = Math.Max(tactics.StunnedUntil, checked(Clock + duration));
        long previous = State.Queue.Snapshot().Pending.Where(w => w.ActorId == target.Id && w.Kind == ScheduledKind.ActorReady).Select(w => w.Due).DefaultIfEmpty(Clock).Max();
        bool interrupted = State.Attacks.Any(a => a.Source == target.Id && !a.ClassGenerated);
        long ready = Math.Max(previous, until);
        RecordCombatControl(source.Id, target, Math.Max(ready - previous, interrupted ? ActionTiming.Recovery(tactics.MoveCost, EffectiveSpeed(target)) : 0));
        CancelAttacks(target.Id, source.Id, action, "stun"); State.Queue.CancelActor(target.Id, ScheduledKind.ActorReady);
        if (State.ActiveActor == target.Id) State.ActiveActor = 0;
        tactics.StunnedUntil = until;
        State.Queue.Schedule(ScheduledKind.ActorReady, ready, target.Id, Friendly(target));
        Emit("Stunned", source.Id, target.Id, action, target.Position, duration, ability: ability);
    }
}
