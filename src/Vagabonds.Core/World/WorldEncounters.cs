using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public static string[] DiscoveryEnemies(ContentCatalog content, DiscoveryDefinition definition) => definition.Encounter.Length > 0
        ? content.Get<EncounterDefinition>(definition.Encounter).Members : definition.Enemies;
    public string[] DiscoveryEnemies(DiscoveryDefinition definition) => DiscoveryEnemies(Content, definition);

    private void PopulateEncounterGroups()
    {
        // Plans are pure. Generate actual ground and actors only when travelling onto a floor.
        // This keeps starting a world from eagerly generating all 144 dungeon footprints.
        EnsureLocalEncounterPopulation();
    }

    public string EncounterDescription(long actorId)
    {
        var actor = State.Actors.FirstOrDefault(a => a.Id == actorId);
        if (actor == null) return "";
        if (actor.EncounterId.Length > 0)
        {
            var group = Content.Get<EncounterDefinition>(actor.EncounterId);
            return group.Name + " / " + group.Tier + ": " + group.Tactics;
        }
        var leader = actor.EncounterLeader == 0 ? actor : State.Actors.FirstOrDefault(a => a.Id == actor.EncounterLeader);
        return leader != null && Content.Entries[leader.Archetype] is BossDefinition boss ? boss.Tactics + " " + boss.Weakness : "";
    }

    private bool EncounterBehaviorAvailable(CombatUnit enemy, BehaviorRule rule) => rule.Condition switch
    {
        "always" => true,
        "ally-wounded" => State.Battle!.Units.Any(u => u.Friendly == enemy.Friendly && u.Alive && u.Health < u.MaxHealth),
        "self-wounded" => enemy.Health < enemy.MaxHealth,
        "target-marked" => State.Battle!.Units.Any(u => u.Friendly != enemy.Friendly && u.Alive && u.Mark > 0),
        _ => false
    };

    private bool TryPreparedPlan(CombatUnit enemy, out PlannedAbility plan)
    {
        var battle = State.Battle!;
        if (battle.Prepared.TryGetValue(enemy.Id, out var prepared))
        {
            // Preserve the exact target. A fallen target wastes the release instead of silently retargeting.
            plan = new(prepared.Ability, prepared.Target, Preparing: prepared.ReadyRound > battle.Round);
            return true;
        }
        plan = null!; return false;
    }

    /// <summary>True means this enemy spent its turn preparing or lost its interrupted release.</summary>
    private bool HandleEnemyPreparation(CombatUnit enemy, PlannedAbility plan, Skill skill)
    {
        var battle = State.Battle!;
        if (plan.Interrupted)
        {
            battle.Prepared.Remove(enemy.Id);
            Feedback("skip", enemy, enemy, "Preparation interrupted", 0, enemy.Name + " lost the prepared action. No mana was spent.", skill.Id);
            return true;
        }
        if (plan.Preparing)
        {
            if (!battle.Prepared.ContainsKey(enemy.Id)) battle.Prepared[enemy.Id] = new(skill.Id, plan.Target, battle.Round + Math.Max(1, skill.PreparationTurns));
            Feedback("prepare", enemy, enemy, "Preparing " + skill.Name, 0,
                enemy.Name + " prepares " + skill.Name + " for next round. Stun interrupts it; guard reduces the impact. Mana is paid on release.", skill.Id);
            return true;
        }
        battle.Prepared.Remove(enemy.Id);
        if (enemy.Mana < skill.Mana)
        {
            Feedback("skip", enemy, enemy, "Not enough mana", 0, enemy.Name + " cannot release " + skill.Name + ". No mana was spent.", skill.Id);
            return true;
        }
        return false;
    }

    private void InterruptPreparation(CombatUnit unit)
    {
        if (State.Battle is not { } battle || unit.Friendly) return;
        bool pending = battle.Prepared.Remove(unit.Id);
        if (battle.Plans.TryGetValue(unit.Id, out var plan) && !plan.Interrupted && (pending || plan.Preparing))
        {
            battle.Plans[unit.Id] = plan with { Interrupted = true };
            Feedback("expire", unit, unit, "Preparation broken", 0,
                unit.Name + " loses " + Content.Get<AbilityDefinition>(plan.Ability).Name + ". Cleansing the stun will not restore that preparation.", plan.Ability);
        }
    }

    private static void ValidateEncounterState(WorldState state, ContentCatalog catalog)
    {
        if (state.RewardedEncounterIds == null || state.RewardedEncounterIds.Count > 10000)
            throw new InvalidDataException("Invalid encounter reward records.");
        foreach (var actor in state.Actors)
        {
            if (actor.EncounterId == null || actor.EncounterId.Length > 0 &&
                (!catalog.Entries.TryGetValue(actor.EncounterId, out var entry) || entry is not EncounterDefinition || actor.EncounterLeader == 0))
                throw new InvalidDataException("Invalid authored encounter identity.");
        }
        foreach (var group in state.Actors.Where(a => a.EncounterId.Length > 0).GroupBy(a => a.EncounterLeader))
        {
            var members = group.OrderBy(a => a.Id).ToArray();
            var definition = catalog.Get<EncounterDefinition>(members[0].EncounterId);
            if (members[0].Id != group.Key || members.Any(a => a.EncounterId != definition.Id || a.Kind is not (ActorKind.Beast or ActorKind.Raider))
                || !members.Select(a => a.Archetype).SequenceEqual(definition.Members)
                || state.Actors.Any(a => a.EncounterLeader == group.Key && a.EncounterId != definition.Id))
                throw new InvalidDataException("Authored encounter membership does not match its definition.");
        }
        foreach (long id in state.RewardedEncounterIds)
        {
            var source = state.Actors.FirstOrDefault(a => a.Id == id);
            if (source == null || source.Life != LifeState.Dead || source.Kind is not (ActorKind.Beast or ActorKind.Raider)
                || source.EncounterLeader != 0 && source.EncounterLeader != id)
                throw new InvalidDataException("Invalid rewarded encounter source.");
            if (source.EncounterId.Length > 0 && state.Actors.Any(a => a.EncounterLeader == id && a.Life != LifeState.Dead))
                throw new InvalidDataException("An incomplete encounter cannot have granted its reward.");
        }
        if (state.LastEncounterReward is { } reward)
        {
            if (reward.Theme == null || reward.Theme.Length > 240 || reward.Items == null || reward.Items.Length > 32
                || reward.Items.Any(i => i == null || i.Item == null || i.Count is < 1 or > 9999 ||
                    !(catalog.Entries.TryGetValue(i.Item, out var item) && item is ItemDefinition) && !state.GeneratedEquipment.ContainsKey(i.Item))
                || reward.Items.Select(i => i.Item).Distinct().Count() != reward.Items.Length
                || reward.Sources == null || reward.Sources.Length > 4 || reward.Sources.Distinct().Count() != reward.Sources.Length
                || reward.Sources.Any(id => !state.RewardedEncounterIds.Contains(id)))
                throw new InvalidDataException("Invalid thematic encounter reward.");
        }
        if (state.Battle is not { } battle) return;
        if (battle.LastEnemyActions == null || battle.LastEnemyActions.Count > 4
            || battle.LastEnemyActions.Any(pair => pair.Key < 0 || pair.Key >= battle.Units.Count || battle.Units[pair.Key].Friendly
                || pair.Value == null || !catalog.Entries.TryGetValue(pair.Value, out var action) || action is not AbilityDefinition ability
                || ability.Effect is "flee" or "potion" || ability.Cooperation != null))
            throw new InvalidDataException("Invalid enemy action history.");
        if (battle.Prepared == null || battle.Prepared.Count > 4) throw new InvalidDataException("Invalid prepared attacks.");
        foreach (var (id, prepared) in battle.Prepared)
            if (id < 0 || id >= battle.Units.Count || battle.Units[id].Friendly || !battle.Units[id].Alive || prepared == null
                || prepared.Ability == null || !catalog.Entries.TryGetValue(prepared.Ability, out var entry) || entry is not AbilityDefinition { PreparationTurns: 1 }
                || prepared.Target < 0 || prepared.Target >= battle.Units.Count || battle.Units[prepared.Target].Friendly == battle.Units[id].Friendly
                || prepared.ReadyRound < battle.Round || prepared.ReadyRound > battle.Round + 1
                || !battle.Plans.TryGetValue(id, out var plan) || plan.Ability != prepared.Ability || plan.Target != prepared.Target || plan.Interrupted
                || plan.Preparing != (prepared.ReadyRound > battle.Round)
                || battle.LastEnemyActions.GetValueOrDefault(id) != prepared.Ability)
                throw new InvalidDataException("Invalid prepared attack continuation.");
        foreach (var (id, plan) in battle.Plans)
        {
            var ability = catalog.Get<AbilityDefinition>(plan.Ability);
            if (ability.PreparationTurns == 0 && (plan.Preparing || plan.Interrupted)
                || ability.PreparationTurns > 0 && !plan.Preparing && !plan.Interrupted && battle.Units[id].Alive
                    && battle.Order.IndexOf(id) >= battle.TurnIndex && !battle.Prepared.ContainsKey(id))
                throw new InvalidDataException("Enemy intention is missing its preparation.");
        }
    }
}
