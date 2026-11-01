using System.Numerics;
using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>Native conversion of the two imported cooperative abilities, saved with the world.</summary>
public sealed record CooperationTuning(int PartnerRange = 2, int AidRadius = 3,
    int RecoveryPerMana = 10, int TwinPower = 2, int AidPower = 2)
{
    public void Validate()
    {
        if (PartnerRange is < 1 or > 12 || AidRadius is < 1 or > 12 || RecoveryPerMana is < 0 or > 1000
            || TwinPower is < 0 or > 100 || AidPower is < 0 or > 100)
            throw new InvalidDataException("Invalid native cooperative ability tuning.");
    }
}

public sealed record CooperativeOption(int Choice, string Name, string Sprite, string Description,
    bool Enabled, string Reason, long Recovery, long HelperRecovery);

// Immediate-resolution provenance; never queued or kept as ambient combat state.
internal sealed record AttackAssistance(long Helper, int LeaderPower, int HelperPower);

public sealed partial class TacticalCombat
{
    private static string CooperationId(int choice) => choice switch
    { 1 => "ability.twin-strike", 2 => "ability.shared-aid", _ => "" };

    private AbilityDefinition? CooperationAbility(int choice) => CooperationId(choice) is { Length: > 0 } id
        && _world.Content.Game.CooperativeAbilities.Contains(id) ? _world.Content.Get<AbilityDefinition>(id) : null;

    public string CooperationPartnerReason(long helper)
    {
        if (Active is not { Life: LifeState.Alive, Tactics.Build: not null } actor || !Friendly(actor))
            return "Wait for a conscious party member's turn.";
        var partner = _world.State.Actors.FirstOrDefault(a => a.Id == helper);
        if (partner == null || partner.Id == actor.Id || !Friendly(partner) || partner.Tactics?.Build == null
            || !State.Participants.Contains(partner.Id)) return "Choose a different party member to assist.";
        if (partner.Life != LifeState.Alive || partner.Health <= 0) return "A downed or dead member cannot assist.";
        if (partner.Tactics.StunnedUntil > Clock) return "This member is stunned.";
        if (partner.Tactics.CooperationCommitted) return "This member already assisted; wait for their next turn.";
        if (partner.Position.Distance(actor.Position) > State.Cooperation.PartnerRange
            || !EnemySquads.Clear(actor.Position, partner.Position, SupportOpen))
            return $"Bring the helper within {State.Cooperation.PartnerRange} clear tiles on the same floor.";
        if (!State.Queue.Snapshot().Pending.Any(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == partner.Id))
            return "This member has no scheduled turn to contribute.";
        return "";
    }

    private long CooperationRecovery(Actor member, AbilityDefinition ability, bool helper)
    {
        var definition = ClassCatalog.Get(member.Tactics!.Build!.Class);
        int basis = helper || ability.Effect == "damage" ? definition.Costs.BasicAttack : definition.Costs.Special;
        int formerMana = helper ? ability.Cooperation!.PartnerMana : ability.Mana;
        return ActionTiming.Recovery(checked(basis + formerMana * State.Cooperation.RecoveryPerMana), EffectiveSpeed(member));
    }

    public IReadOnlyList<CooperativeOption> CooperativeOptions(long helper, long memberId = 0)
    {
        var member = memberId == 0 ? Active : _world.Party.FirstOrDefault(a => a.Id == memberId);
        string reason = member?.Id != State.ActiveActor ? "Wait for this member's turn to cooperate." : CooperationPartnerReason(helper);
        var partner = _world.State.Actors.FirstOrDefault(a => a.Id == helper);
        return Enumerable.Range(1, 2).Select(choice => (Choice: choice, Ability: CooperationAbility(choice)))
            .Where(entry => entry.Ability != null).Select(entry =>
        {
            var ability = entry.Ability!;
            return new CooperativeOption(entry.Choice, ability.Name, ability.Sprite!, ability.Description, reason.Length == 0, reason,
                member?.Tactics?.Build == null ? 0 : CooperationRecovery(member, ability, false),
                partner?.Tactics?.Build == null ? 0 : CooperationRecovery(partner, ability, true));
        }).ToArray();
    }

    private TacticalPreview PreviewCooperation(Actor actor, TacticalIntent intent)
    {
        if (CooperationAbility(intent.Choice) is not { } ability) return Invalid("Choose Twin strike or Shared aid.");
        string reason = CooperationPartnerReason(intent.Target);
        if (reason.Length > 0) return Invalid(reason);
        var helper = Actor(intent.Target);
        if (intent.Choice == 1)
        {
            var footprint = ClassFootprint(actor, new(TacticalAction.BasicAttack, intent.Facing));
            // The imported skill targets one enemy. Class range/delivery/element are
            // retained, while a body in front blocks selecting one behind it.
            var target = footprint.Cells.TakeWhile(ProjectileOpen).Select(p => BodyAt(p, actor.Id)).FirstOrDefault(a => a != null);
            if (target is not { Life: LifeState.Alive, Tactics: not null } || !HostileTo(actor, target)
                || !_world.VisionFrom(actor.Position).Contains(target.Position)) return Invalid("Aim at a visible enemy in this class's basic range.");
            int power = checked(actor.Power + State.Cooperation.TwinPower + helper.Power * ability.Cooperation!.PartnerPowerPercent / 100);
            return new(true, "", CooperationRecovery(actor, ability, false), [target.Position])
            { Damage = checked(power + (actor.Tactics!.Supplies.FocusActions > 0 ? 2 : 0)) };
        }
        var cells = AidRecipients(actor).Select(a => a.Position).ToArray();
        return new(true, "", CooperationRecovery(actor, ability, false), cells);
    }

    private IEnumerable<Actor> AidRecipients(Actor actor) => _world.Party.Where(a => a.Life == LifeState.Alive
        && a.Position.Distance(actor.Position) <= State.Cooperation.AidRadius
        && EnemySquads.Clear(actor.Position, a.Position, SupportOpen)).OrderBy(a => a.Id);

    private void CommitCooperation(Actor actor, TacticalIntent intent, TacticalPreview preview, long action)
    {
        var ability = CooperationAbility(intent.Choice)!; var helper = Actor(intent.Target);
        var ready = State.Queue.Snapshot().Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == helper.Id);
        long delay = CooperationRecovery(helper, ability, true);
        State.Queue.Delay(ready.Sequence, delay);
        helper.Tactics!.CooperationCommitted = true;
        Emit("CooperationAssisted", helper.Id, actor.Id, action, helper.Position, checked((int)delay), ability: ability.Id);
        int helperPower = helper.Power * ability.Cooperation!.PartnerPowerPercent / 100;
        if (intent.Choice == 1)
        {
            var basic = ClassFootprint(actor, new(TacticalAction.BasicAttack, intent.Facing));
            var footprint = basic with { AbilityId = ability.Id, Cells = preview.Cells };
            int leaderPower = checked(actor.Power + State.Cooperation.TwinPower);
            Strike(actor, action, footprint, checked(leaderPower + helperPower), actor.Position, null,
                assistance: new(helper.Id, leaderPower, helperPower));
            return;
        }
        int healingPower = checked(State.Cooperation.AidPower + (actor.Power + 1) / 2 + _world.EquipmentEffectPower(actor, "healing"));
        foreach (var recipient in AidRecipients(actor).ToArray())
        {
            int gain = Math.Min(checked(healingPower + helperPower), recipient.MaxHealth - recipient.Health);
            recipient.Health += gain;
            RecordCombatHealing(actor.Id, recipient.Id, gain, new(helper.Id, healingPower, helperPower));
            bool burning = recipient.Tactics!.Burning != null;
            RemoveBurning(recipient);
            if (burning) Emit("StatusCleansed", actor.Id, recipient.Id, action, recipient.Position, ability: ability.Id);
            // Curing a status never refunds already committed recovery, including
            // the two costs just paid. Downed bleed-out is not a curable status.
            recipient.Tactics.StunnedUntil = 0; recipient.Tactics.SnaredBy = 0;
            recipient.Tactics.EnemySnareDelay = 0; recipient.Tactics.WebRecoveryDelay = 0;
            recipient.Stress = Math.Max(0, recipient.Stress - 10);
            Emit("Healed", actor.Id, recipient.Id, action, recipient.Position, gain, ability: ability.Id);
            Emit("CooperativeAid", helper.Id, recipient.Id, action, recipient.Position, gain, ability: ability.Id);
        }
    }

    private static Dictionary<long, long> AssistanceShares(long leader, int amount, AttackAssistance? assistance)
        => assistance == null ? new() { [leader] = amount }
            : ContributionRewards.Allocate(amount, [(leader, new BigInteger(assistance.LeaderPower)),
                (assistance.Helper, new BigInteger(assistance.HelperPower))]);
}
