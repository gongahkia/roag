using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public string CooperationPartnerReason(int skillIndex, int partnerIndex, IReadOnlyList<Skill>? skills = null)
    {
        skills ??= ActiveSkills();
        if (skillIndex < 0 || skillIndex >= skills.Count || !skills[skillIndex].Cooperative || State.Battle is not { } battle) return "Choose a cooperative ability first.";
        var partner = battle.Units.ElementAtOrDefault(partnerIndex);
        if (partner == null || !partner.Friendly || partner.Id == battle.Active.Id) return "Choose a different companion to assist.";
        if (!partner.Alive) return "A downed companion cannot assist.";
        if (partner.Stun > 0) return partner.Name + " is stunned.";
        if (battle.Assisting.Contains(partner.Id)) return partner.Name + " already committed this action to another ability.";
        if (battle.Order.IndexOf(partner.Id) <= battle.TurnIndex) return partner.Name + " has no action remaining this round.";
        return partner.Mana < skills[skillIndex].PartnerMana ? $"{partner.Name} needs {skills[skillIndex].PartnerMana} MP; {partner.Mana} available." : "";
    }
    public string CooperationDescription(Skill skill) => !skill.Cooperative ? "" :
        $"Choose a ready companion. Costs {skill.Mana} MP from the active traveller and {skill.PartnerMana} MP from the helper, consuming both actions this round. " +
        (skill.PartnerPowerPercent > 0 ? $"Adds {skill.PartnerPowerPercent}% of the helper's power. " : "") +
        "A helper must be conscious, unstunned and still awaiting their turn.";
}
