using Roag.Core.Content;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>Uses a separate coordinate hash, preserving names, class selection and all saved RNG streams.</summary>
    public static void AssignCombatTrait(ContentCatalog catalog, Actor actor, int seed)
    {
        if (actor.Kind != ActorKind.Person) return;
        var pool = catalog.All<TalentDefinition>().Where(t => t.Acquisition == "innate").ToArray();
        if (pool.Length == 0) throw new InvalidDataException("Generated people need at least one authored innate combat talent.");
        uint hash = WorldRandom.Hash(seed, unchecked((int)actor.Id), unchecked((int)(actor.Id >> 32)), 7201);
        actor.CombatTrait = pool[hash % pool.Length].Id;
    }
    public IEnumerable<TalentDefinition> CombatTalents(Actor actor)
    {
        if (actor.CombatTrait.Length > 0) yield return Content.Get<TalentDefinition>(actor.CombatTrait);
        foreach (string id in actor.Talents) yield return Content.Get<TalentDefinition>(id);
    }
    public int TalentPower(Actor actor, string effect) => CombatTalents(actor).Sum(t => t.Bonuses.GetValueOrDefault(effect));
    public int ProtectionPower(Actor actor) => Math.Min(12, EquipmentEffectPower(actor, "protect") + TalentPower(actor, "protect"));
    public Skill ApplyProgressionSkill(Actor actor, Skill skill)
    {
        int bonus = skill.Effect is "heal" or "potion" or "cleanse" ? TalentPower(actor, "healing")
            : skill.Effect == "calm" ? TalentPower(actor, "calm") : 0;
        return skill with { Power = skill.Power + bonus,
            Mana = skill.Mana == 0 ? 0 : Math.Max(1, skill.Mana - TalentPower(actor, "mana-discount")) };
    }
    public int ProgressionDamageBonus(CombatUnit caster, CombatUnit target, Skill skill)
    {
        if (skill.Effect is "heal" or "potion" or "cleanse" or "calm" or "guard" or "rally" or "flee") return 0;
        var actor = CombatActor(caster); int bonus = 0;
        if (target.Health * 2 <= target.MaxHealth) bonus += TalentPower(actor, "wounded-damage");
        if (target.Mark > 0) bonus += TalentPower(actor, "marked-damage");
        if (target.Stun > 0) bonus += TalentPower(actor, "stunned-damage");
        if (skill.Target == TargetKind.AllEnemies) bonus += TalentPower(actor, "all-target-damage");
        if (skill.Effect == "burn") bonus += TalentPower(actor, "burn-damage");
        if (skill.Cooperative) bonus += TalentPower(actor, "cooperative-damage");
        return bonus;
    }
    /// <summary>Includes locked alternatives so the player can plan without spending anything.</summary>
    public IReadOnlyList<TalentDefinition> ProgressionChoices(long actorId)
    {
        var actor = State.Actors.FirstOrDefault(a => a.Id == actorId && a.Kind == ActorKind.Person);
        if (actor == null) return [];
        string classId = Class(actor).Id;
        return Content.All<TalentDefinition>().Where(t => t.Acquisition == "choice" && t.Class == classId)
            .OrderBy(t => t.Level).ThenBy(t => t.Id, StringComparer.Ordinal).ToArray();
    }
    public string ProgressionChoiceReason(long actorId, string id)
    {
        if (State.Endgame.Victory || State.AwaitingContinuation || DeathPending) return "Finish the expedition decision first.";
        if (IsInCombat) return "Choose a development between encounters.";
        var actor = Party.FirstOrDefault(a => a.Id == actorId && a.Kind == ActorKind.Person);
        if (actor == null) return "Choose a traveller in the active party.";
        if (actor.Life != LifeState.Alive) return "Revive this traveller before choosing a development.";
        if (string.IsNullOrWhiteSpace(id) || !Content.Entries.TryGetValue(id, out var entry) || entry is not TalentDefinition { Acquisition: "choice" } talent)
            return "Choose an authored class development.";
        if (talent.Class != Class(actor).Id) return "This development belongs to another class.";
        if (actor.Level < talent.Level) return "Available at level " + talent.Level + ".";
        if (actor.Talents.Contains(id)) return "This development is already learned.";
        var selected = actor.Talents.Select(Content.Get<TalentDefinition>).FirstOrDefault(t => t.Level == talent.Level);
        return selected == null ? "" : "The level " + talent.Level + " choice is already committed to " + selected.Name + ".";
    }
    public ActionResult ChooseProgression(long actorId, string id)
    {
        string reason = ProgressionChoiceReason(actorId, id);
        if (reason.Length > 0) return new(false, reason);
        var actor = Party.Single(a => a.Id == actorId); var talent = Content.Get<TalentDefinition>(id);
        actor.Talents.Add(id);
        Diagnose("progression", new { actor = actorId, talent = id, actor.Level });
        Log(actor.Name + " chose " + talent.Name + " at level " + talent.Level + ". " + talent.Description);
        return new(true, actor.Name + " learned " + talent.Name + ". This development is permanent.");
    }
    public static void ValidateProgression(WorldState state, ContentCatalog catalog)
    {
        foreach (var actor in state.Actors)
        {
            if (actor.CombatTrait == null || actor.Talents == null || actor.Talents.Count > 2 || actor.Talents.Distinct().Count() != actor.Talents.Count)
                throw new InvalidDataException("Invalid companion developments.");
            if (actor.Kind != ActorKind.Person)
            {
                if (actor.CombatTrait.Length > 0 || actor.Talents.Count > 0) throw new InvalidDataException("Only people have generated combat talents.");
                continue;
            }
            if (!catalog.Entries.TryGetValue(actor.CombatTrait, out var innate) || innate is not TalentDefinition { Acquisition: "innate" })
                throw new InvalidDataException("Invalid generated combat trait.");
            string classId = catalog.Classes.Single(c => c.Role == actor.Role).Id;
            var levels = new HashSet<int>();
            foreach (string id in actor.Talents)
            {
                if (id == null || !catalog.Entries.TryGetValue(id, out var definition) || definition is not TalentDefinition { Acquisition: "choice" } choice
                    || choice.Class != classId || choice.Level > actor.Level || !levels.Add(choice.Level))
                    throw new InvalidDataException("Invalid class development selection.");
            }
        }
    }
}
