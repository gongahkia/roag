using Roag.Core.Content;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public sealed record TalentPreviewObservation(ActionResult Result, int Value, CombatEvent[] Events, int[] TargetIds);
public sealed record TalentPreviewCase(string Effect, int Expected, int Difference, bool Passed, TalentPreviewObservation Baseline, TalentPreviewObservation Developed);

/// <summary>Exercises each authored talent bonus through actual party abilities, initiative or enemy attacks.</summary>
public static class TalentPreview
{
    public static object Run(ContentCatalog catalog, string id, int seed)
    {
        var definition = catalog.Get<TalentDefinition>(id);
        var cases = definition.Bonuses.Select(bonus =>
        {
            var baseline = Observe(catalog, definition, seed, bonus.Key, false);
            var developed = Observe(catalog, definition, seed, bonus.Key, true);
            int difference = bonus.Key == "mana-discount" ? baseline.Value - developed.Value : developed.Value - baseline.Value;
            return new TalentPreviewCase(bonus.Key, bonus.Value, difference, baseline.Result.Success && developed.Result.Success && difference == bonus.Value, baseline, developed);
        }).ToArray();
        return new { seed, definition, passed = cases.All(c => c.Passed), cases,
            setup = "Matched isolated four-companion encounters. Each case executes the real action resolver with and without the selected development; no player save is used." };
    }
    private static TalentPreviewObservation Observe(ContentCatalog catalog, TalentDefinition definition, int seed, string effect, bool developed)
    {
        var world = WorldSession.Create(seed, catalog); CampPreview.Prepare(world); var actor = world.Party.First();
        int role = definition.Class != null ? catalog.Get<ClassDefinition>(definition.Class).Role
            : effect is "healing" or "calm" ? 3 : effect is "burn-damage" or "all-target-damage" or "mana-discount" ? 2 : 0;
        actor.Role = role; actor.Level = Math.Max(1, definition.Level);
        // Keep generated dispositions out of the measured mechanic. A custom catalog may only
        // contain the talent being measured; its unsaved baseline then has no innate effect.
        string neutral = catalog.All<TalentDefinition>().FirstOrDefault(t => t.Acquisition == "innate" && !t.Bonuses.ContainsKey(effect))?.Id ?? "";
        foreach (var member in world.Party)
        {
            member.CombatTrait = neutral; member.Talents.Clear(); member.Body.Wounds = 0; member.Body.Fatigue = 0;
            member.BaseHealth = member.Health = 1000; member.BaseMana = member.Mana = 100;
            member.BasePower = 6; member.BaseArmor = 0; member.BaseSpeed = member.Id == actor.Id ? 28 : 10;
        }
        if (developed)
        {
            if (definition.Acquisition == "innate") actor.CombatTrait = definition.Id;
            else if (!world.ChooseProgression(actor.Id, definition.Id).Success) throw new InvalidDataException("Talent preview could not select " + definition.Id);
        }
        var enemy = world.State.Actors.First(a => a.Kind == ActorKind.Beast); enemy.EncounterLeader = enemy.Id;
        var second = world.State.Actors.First(a => a.Kind == ActorKind.Beast && a.Id != enemy.Id); second.EncounterLeader = enemy.Id;
        foreach (var (foe, offset) in new[] { enemy, second }.Select((foe, offset) => (foe, offset)))
        { foe.BaseHealth = foe.Health = 1000; foe.BasePower = 20; foe.BaseArmor = 0; foe.BaseSpeed = 1; foe.Position = world.State.Position.Offset(8 + offset, 0); }
        world.StartBattle(enemy.Id); var battle = world.State.Battle!; var target = battle.Units.First(u => !u.Friendly);
        int initiative = battle.Units[0].Speed;
        battle.Order = battle.Units.Select(u => u.Id).ToList(); battle.TurnIndex = 0;
        foreach (var unit in battle.Units) { unit.Burn = unit.Bleed = unit.Mark = unit.Stun = 0; unit.Guarding = false; }
        if (effect == "protect")
        {
            battle.Units[1].Health = 400;
            battle.Order = [target.Id, .. battle.Units.Where(u => u.Id != target.Id).Select(u => u.Id)];
            battle.Plans[target.Id] = new(catalog.Game.EnemyFallback, 1);
            var result = world.AdvanceEnemy();
            return new(result, world.State.CombatEvents.Where(e => e.Label == "Ally cover" && e.Source == actor.Id).Sum(e => e.Amount), world.State.CombatEvents.ToArray(), [1]);
        }
        if (effect == "initiative") return new(new(true, "Initiative established by the normal combat round."), initiative, world.State.CombatEvents.ToArray(), [0]);
        var skills = world.ActiveSkills();
        bool Damages(Skill skill) => skill.Effect is "damage" or "burn" or "bleed" or "mark" or "stun" or "dread" or "drain";
        int index = skills.Select((skill, i) => (skill, i)).First(p => effect switch
        {
            "healing" => p.skill.Effect is "heal" or "potion" or "cleanse",
            "calm" => p.skill.Effect == "calm",
            "mana-discount" => catalog.Get<AbilityDefinition>(p.skill.Id).Mana > definition.Bonuses[effect],
            "burn-damage" => p.skill.Effect == "burn",
            "all-target-damage" => p.skill.Target == TargetKind.AllEnemies && Damages(p.skill),
            "cooperative-damage" => p.skill.Cooperative && Damages(p.skill),
            _ => Damages(p.skill)
        }).i;
        var skill = skills[index];
        int targetId = skill.Target == TargetKind.Ally ? 1 : skill.Target == TargetKind.Self ? 0 : target.Id;
        int measuredId = skill.Target == TargetKind.AllAllies ? 1 : targetId;
        int partner = skill.Cooperative ? 3 : -1;
        if (effect == "wounded-damage") target.Health = 400;
        if (effect == "marked-damage") target.Mark = 2;
        if (effect == "stunned-damage") target.Stun = 2;
        if (effect is "healing" or "calm" or "mana-discount")
        {
            foreach (var unit in battle.Units) { unit.Health = 400; unit.Stress = 50; }
        }
        int mana = battle.Units[0].Mana, stress = battle.Units[measuredId].Stress;
        var preview = world.ActionPreview(index, targetId, partner);
        var action = world.Act(index, targetId, partner);
        int value = effect switch
        {
            "mana-discount" => mana - battle.Units[0].Mana,
            "healing" => world.State.CombatEvents.Where(e => e.Kind == "heal" && e.Target == battle.ActorIds[measuredId]).Sum(e => e.Amount),
            "calm" => stress - battle.Units[measuredId].Stress,
            _ => world.State.CombatEvents.FirstOrDefault(e => e.Kind == "damage" && e.Source == actor.Id && e.Label == skill.Name)?.Amount ?? 0
        };
        return new(action, value, world.State.CombatEvents.ToArray(), preview.Targets);
    }
}
