using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed record CombatActionPreview(string Name, string Reason, string Description, int Minimum, int Maximum, int[] Targets);
public sealed record CombatTargetPreview(int UnitId, long ActorId, string Name, string Sprite, int Minimum, int Maximum, string Effect);
public sealed record CombatIntentPreview(int UnitId, string AbilityId, string Name, string Icon, string Description, string Reason,
    int Mana, TargetKind Target, CombatTargetPreview[] Targets);
public sealed record GearComparison(string Slot, string Previous, int Health, int Power, int Armor, int Speed);

public sealed partial class WorldSession
{
    public string SkillUnavailable(int index)
    {
        var skills = ActiveSkills();
        if (index < 0 || index >= skills.Count) return "Wait for a conscious companion's turn.";
        var skill = skills[index];
        if (State.Battle!.Active.Mana < skill.Mana) return $"Needs {skill.Mana} MP; {State.Battle.Active.Mana} available.";
        if (skill.Cooperative && !State.Battle.Units.Any(u => CooperationPartnerReason(index, u.Id).Length == 0)) return "Needs a conscious, unstunned companion with enough MP and an action still ahead this round.";
        return skill.Effect == "potion" && Count("medicine") == 0 ? "Needs one medicine in the shared pack." : "";
    }
    public CombatActionPreview ActionPreview(int index, int targetIndex = -1, int partnerIndex = -1)
    {
        string reason = SkillUnavailable(index);
        var skills = ActiveSkills();
        if (index < 0 || index >= skills.Count) return new("Ability", reason, "", 0, 0, []);
        var skill = skills[index]; var b = State.Battle!; var caster = b.Active;
        var target = b.Units.ElementAtOrDefault(targetIndex);
        if (reason.Length == 0 && skill.Cooperative) reason = CooperationPartnerReason(index, partnerIndex);
        if (reason.Length == 0 && skill.Target == TargetKind.Enemy && (target == null || target.Friendly || !target.Alive)) reason = "Choose a living enemy.";
        if (reason.Length == 0 && skill.Target == TargetKind.Ally && (target == null || !target.Friendly)) reason = "Choose a companion.";
        var targets = skill.Target switch
        {
            TargetKind.AllEnemies => b.Units.Where(t => !t.Friendly && t.Alive).ToArray(),
            TargetKind.AllAllies => b.Units.Where(t => t.Friendly && t.Alive).ToArray(),
            TargetKind.Self => [caster], _ => target == null ? [] : new[] { target }
        };
        string description = skill.Description + " " + skill.Target + "; " + skill.Mana + " MP." + (skill.Cooperative ? " " + CooperationDescription(skill) : "");
        int minimum = 0, maximum = 0;
        if (reason.Length == 0 && targets.Length > 0)
        {
            var actor = State.Actors.Single(a => a.Id == b.ActorIds[caster.Id]);
            int bonus = actor.Level - 1 + caster.Power - 6
                + (skill.Cooperative ? b.Units[partnerIndex].Power * skill.PartnerPowerPercent / 100 : 0); var t = targets[0];
            var outcomes = PreviewCombatTargets(caster, skill, targets, bonus);
            if (skill.Effect is "heal" or "potion" or "cleanse")
            {
                minimum = outcomes.Min(o => o.Minimum); maximum = outcomes.Max(o => o.Maximum);
                description += targets.Length > 1 ? $" Restores {minimum}-{maximum} HP per companion; removes bleed." : $" Restores {minimum} HP to {t.Name}; removes bleed.";
            }
            else if (skill.Effect is not ("guard" or "rally" or "calm" or "flee"))
            {
                minimum = outcomes.Min(o => o.Minimum); maximum = outcomes.Max(o => o.Maximum);
                description += targets.Length == 1 ? $" {minimum}-{maximum} damage to {t.Name}." : " Hits every living enemy; armour and guard apply separately.";
            }
            if (skill.Effect is "burn" or "bleed" or "mark" or "stun") description += $" Applies {skill.Effect} for {(skill.Effect == "stun" ? Math.Min(2, skill.Duration) : skill.Duration)} turns if the target survives.";
            if (skill.Effect == "cleanse") description += " Also clears burn, mark and stun; reduces stress by 10.";
            if (skill.Effect is "guard" or "rally") description += " Halves incoming damage after armour until that fighter's next turn.";
            if (skill.Effect == "flee") description += " Retreats five tiles when space permits; enemies retain wounds.";
        }
        return new(skill.Name, reason, description, minimum, maximum, targets.Select(t => t.Id).ToArray());
    }
    /// <summary>The saved plan, evaluated against current defenses without rolling or changing the battle.</summary>
    public CombatIntentPreview? EnemyActionPreview(int unitId)
    {
        if (State.Battle is not { } b || b.Units.ElementAtOrDefault(unitId) is not { Friendly: false } caster
            || !b.Plans.TryGetValue(unitId, out var plan)) return null;
        var skill = Content.Get<AbilityDefinition>(plan.Ability).AsSkill();
        var target = b.Units.ElementAtOrDefault(plan.Target);
        var targets = skill.Target switch
        {
            TargetKind.AllEnemies => b.Units.Where(t => t.Friendly && t.Alive).ToArray(),
            TargetKind.AllAllies => b.Units.Where(t => !t.Friendly && t.Alive).ToArray(),
            TargetKind.Self => [caster], _ => target == null ? [] : new[] { target }
        };
        string reason = !caster.Alive ? "Defeated."
            : b.Order.IndexOf(unitId) < b.TurnIndex ? "Already acted this round."
            : caster.Stun > 0 ? "Stunned; will lose this action unless cleansed."
            : targets.Length == 0 || skill.Target is TargetKind.Enemy or TargetKind.Ally && target is not { Alive: true }
                ? "The planned target has fallen; this action will not retarget." : "";
        var outcomes = PreviewCombatTargets(caster, skill, targets, caster.Power - 6);
        if (reason.Length > 0) outcomes = outcomes.Select(o => o with { Minimum = 0, Maximum = 0 }).ToArray();
        string description = skill.Description + " " + skill.Mana + " MP. The plan is fixed for this round; ranges use current defenses and may change before the turn.";
        if (skill.Effect is "burn" or "bleed" or "mark" or "stun")
            description += $" Applies {skill.Effect} for {(skill.Effect == "stun" ? Math.Min(2, skill.Duration) : skill.Duration)} turns if the target survives.";
        if (skill.Effect == "drain") description += " Recovers half the damage dealt, at least 1 HP, up to maximum health.";
        if (skill.Effect == "dread") description += " Also raises stress; camp preparations and equipment can reduce this.";
        if (skill.Effect is "guard" or "rally") description += " Halves incoming damage after armour until that fighter's next turn.";
        return new(unitId, skill.Id, skill.Name, skill.Icon, description, reason, skill.Mana, skill.Target, outcomes);
    }
    private (int Damage, int Resistance) CombatDamageBeforeProtection(CombatUnit target, int amount, string label)
    {
        int resistance = label == "Burn" ? PreparationPower(CombatActor(target), "burn-resist")
            : label == "Bleed" ? PreparationPower(CombatActor(target), "bleed-resist") : 0;
        int damage = Math.Max(resistance > 0 ? 0 : 1, amount - target.Armor - resistance);
        if (target.Guarding && damage > 0) damage = Math.Max(1, damage / 2);
        return (damage, resistance);
    }
    private CombatTargetPreview[] PreviewCombatTargets(CombatUnit caster, Skill skill, CombatUnit[] targets, int bonus)
    {
        var b = State.Battle!;
        bonus += AbilityEquipmentBonus(CombatActor(caster), skill);
        var minimum = new int[targets.Length]; var maximum = new int[targets.Length];
        if (skill.Effect is "heal" or "potion" or "cleanse")
            for (int i = 0; i < targets.Length; i++) minimum[i] = maximum[i] = Math.Clamp(skill.Power + bonus, 0, targets[i].MaxHealth - targets[i].Health);
        else if (skill.Effect == "calm")
            for (int i = 0; i < targets.Length; i++) minimum[i] = maximum[i] = Math.Min(skill.Power, targets[i].Stress);
        else if (skill.Effect is not ("guard" or "rally" or "flee") && targets.Length > 0)
        {
            Array.Fill(minimum, int.MaxValue);
            int[] cover = b.Units.Select(u => EquipmentEffectPower(CombatActor(u), "protect")).ToArray();
            // At most four targets and four rolls each. Enumerating the possibilities keeps group
            // previews honest when a shield is spent or a protector falls earlier in the same attack.
            Visit(0, b.Units.Select(u => u.Health).ToArray(), b.Units.Select(u => u.Stress).ToArray(), new(b.ShieldUsed));
            void Visit(int index, int[] health, int[] stress, HashSet<int> used)
            {
                if (index == targets.Length) return;
                var target = targets[index];
                for (int roll = 0; roll < 4; roll++)
                {
                    int[] nextHealth = (int[])health.Clone(), nextStress = (int[])stress.Clone(); var nextUsed = new HashSet<int>(used);
                    int amount = skill.Power + bonus + roll + (target.Mark > 0 ? 4 : 0) - (stress[caster.Id] >= 60 ? 2 : 0);
                    int damage = CombatDamageBeforeProtection(target, amount, skill.Name).Damage;
                    if (damage > 1 && health[target.Id] * 2 <= target.MaxHealth)
                    {
                        var protector = b.Units.Where(u => u.Id != target.Id && u.Friendly == target.Friendly && health[u.Id] > 0 && !used.Contains(u.Id) && cover[u.Id] > 0)
                            .OrderByDescending(u => cover[u.Id]).ThenBy(u => u.Id).FirstOrDefault();
                        if (protector != null) { damage = Math.Max(1, damage - cover[protector.Id]); nextUsed.Add(protector.Id); }
                    }
                    int lost = Math.Min(health[target.Id], damage);
                    minimum[index] = Math.Min(minimum[index], lost); maximum[index] = Math.Max(maximum[index], lost);
                    nextHealth[target.Id] = Math.Max(0, health[target.Id] - damage);
                    if (nextHealth[target.Id] == 0 && target.Friendly)
                        foreach (var friend in b.Units.Where(u => u.Friendly && nextHealth[u.Id] > 0)) nextStress[friend.Id] = Math.Min(100, nextStress[friend.Id] + 10);
                    if (skill.Effect == "drain") nextHealth[caster.Id] = Math.Min(caster.MaxHealth, nextHealth[caster.Id] + Math.Max(1, lost / 2));
                    Visit(index + 1, nextHealth, nextStress, nextUsed);
                }
            }
        }
        return targets.Select((t, i) => new CombatTargetPreview(t.Id, b.ActorIds[t.Id], t.Name, t.Sprite, minimum[i], maximum[i], skill.Effect.Length == 0 ? "damage" : skill.Effect)).ToArray();
    }
    public static string StatusDescription(CombatUnit unit) => string.Join(", ", new[]
    {
        unit.Guarding ? "Guard: half damage until next turn" : "",
        unit.Burn > 0 ? $"Burn {unit.Burn}: 3 damage before armour each turn" : "",
        unit.Bleed > 0 ? $"Bleed {unit.Bleed}: 3 damage before armour each turn" : "",
        unit.Mark > 0 ? $"Mark {unit.Mark}: incoming attacks gain 4 power" : "",
        unit.Stun > 0 ? $"Stun {unit.Stun}: skips turns" : ""
    }.Where(s => s.Length > 0));
    public GearComparison CompareGear(Actor actor, string id)
    {
        var item = Item(id); var old = actor.Equipment.GetValueOrDefault(item.Slot);
        return new(item.Slot, old == null ? "empty" : Item(old.Item).Name,
            item.Health - (old?.Health ?? 0), item.Power - (old?.Power ?? 0), item.Armor - (old?.Armor ?? 0), item.Speed - (old?.Speed ?? 0));
    }
    public string InteractionBlockReason(long actorId, string interactionId)
    {
        if (State.Endgame.Victory || IsInCombat || State.AwaitingContinuation || DeathPending) return "Finish the encounter or expedition first.";
        var interaction = Interactions(actorId).FirstOrDefault(i => i.Id == interactionId);
        var actor = State.Actors.FirstOrDefault(a => a.Id == actorId);
        if (interaction == null || actor == null || actor.Position.Distance(State.Position) > interaction.Distance) return "Approach someone who offers this interaction.";
        string cost = MissingSupplies(interaction.Cost); if (cost.Length > 0) return cost;
        if (interaction.Action == "recruit" && State.Party.Count >= 4) return "Four travellers already fill the active party.";
        if (interaction.Action == "heal" && !Party.Any(a => a.Life == LifeState.Alive && (a.Health < a.MaxHealth || a.Body.Wounds > 0))) return "No conscious traveller needs treatment.";
        return "";
    }
    private string MissingSupplies(Dictionary<string, int> costs)
    {
        var missing = costs.Where(c => Count(Content.Get<ItemDefinition>(c.Key).Stack) < c.Value)
            .Select(c => $"{c.Value} {Content.Get<ItemDefinition>(c.Key).Name} ({Count(Content.Get<ItemDefinition>(c.Key).Stack)} carried)").ToArray();
        return missing.Length == 0 ? "" : "Needs " + string.Join(", ", missing) + ".";
    }
    public string RecipeSuppliesReason(Material material)
    {
        var recipe = Content.Game.Recipes.Select(Content.Get<RecipeDefinition>).FirstOrDefault(r => r.Output == null && r.Material == material);
        return recipe == null ? "No construction recipe is available." : MissingSupplies(recipe.Cost);
    }
    public string TargetBlockReason(WorldCommandKind kind, WorldPosition target, Material build = Material.Wall, bool requireAdjacent = true)
    {
        if (!Plan.Contains(target) || !Chunks.Seen(target)) return "Choose observed ground first.";
        if (State.Position.Distance(target) > (requireAdjacent ? 1 : 100)) return requireAdjacent ? "Approach adjacent ground before using this tool." : "Choose tiles within 100 steps on this floor.";
        var before = Chunks.At(target); var owned = State.Structures.FirstOrDefault(s => s.Position == target);
        if (before is Material.StairsDown or Material.StairsUp) return "Stairs are protected.";
        if (State.Work.Any(w => w.Target == target)) return "A work order already occupies this cell.";
        if (State.Work.Count >= 64) return "The work list is full.";
        var prop = ObjectAt(target);
        if (prop is { Kind: "discovery" or "loot" } && kind is WorldCommandKind.Build or WorldCommandKind.Dig) return "Investigate this landmark or recover its supplies first; it cannot be dismantled.";
        if (kind == WorldCommandKind.Harvest)
        {
            var resource = ResourceAt(target);
            return resource == null ? "Choose a resource such as a fruit tree, fishing bank or mineral deposit."
                : resource.Available > 0 ? "" : resource.RecoveryDescription;
        }
        if (kind == WorldCommandKind.Build)
        {
            if (prop is { Solid: true }) return "Clear the solid object before building here.";
            if (owned != null || before == build) return "Dismantle the existing structure before replacing it.";
            if (!GroundOpen(target) && before != Material.Water || before == Material.Water && build != Material.Bridge) return "Build on clear ground; water requires a bridge.";
            if ((!Materials.Walkable(build) || build == Material.Camp) && State.Actors.Any(a => a.Life != LifeState.Dead && a.Position == target)) return "Someone is standing on the construction site.";
            return RecipeSuppliesReason(build);
        }
        if (kind == WorldCommandKind.Dig)
        {
            if (prop is not { Removable: true } && owned == null && before is not (Material.Rock or Material.Wall or Material.Tree or Material.Door or Material.Storage or Material.Camp)) return "Choose rock, a tree, a wall or a removable structure.";
            if (before == Material.Storage && State.Chunks.FirstOrDefault(c => c.Key == target.Chunk)?.Storage.GetValueOrDefault(target.LocalIndex)?.Values.Any(v => v > 0) == true) return "Empty the storage before dismantling it.";
            return "";
        }
        if (kind == WorldCommandKind.Repair)
        {
            if (owned == null) return "Choose an owned structure.";
            if (owned.Condition >= 95) return "This structure does not need repairs.";
            string stack = owned.Material is Material.Wall or Material.Floor ? "stone" : "wood";
            return Count(stack) < 1 ? "Repairs need one " + stack + "." : "";
        }
        return "";
    }
    public string CommandBlockReason(WorldCommand command)
    {
        if (DeathPending) return "Save the recorded deaths before continuing.";
        if (State.Endgame.Victory || State.AwaitingContinuation) return "This expedition has ended.";
        if (IsInCombat) return "Finish the encounter first.";
        var actor = State.Actors.FirstOrDefault(a => a.Id == command.ActorId);
        switch (command.Kind)
        {
            case WorldCommandKind.Build: case WorldCommandKind.Dig: case WorldCommandKind.Repair: case WorldCommandKind.Harvest: return TargetBlockReason(command.Kind, command.Target, command.Material);
            case WorldCommandKind.ApproachDiscovery: return State.Discoveries.FirstOrDefault(d => d.Position == command.Target) is { } discovery ? SituationApproachReason(discovery, command.Item) : "Choose an observed landmark.";
            case WorldCommandKind.CampActivity: return CampActivityReason(command.Target, command.Item, command.ActorId);
            case WorldCommandKind.AssignWork:
                var order = State.Work.FirstOrDefault(w => w.Id == command.WorkId);
                if (order == null || actor == null || actor.Life != LifeState.Alive || actor.Kind != ActorKind.Person) return "Select a pending order and a living worker.";
                if (State.Work.Any(w => w.Id != order.Id && w.Worker == actor.Id)) return actor.Name + " already has another work order.";
                if (actor.Position.Distance(order.Target) > 100) return "This worker is too far from the order.";
                break;
            case WorldCommandKind.Camp:
                if (!NearbyCamp(State.Position)) return "Approach a serviceable camp first.";
                if (Count("food") < State.Party.Count) return $"Rest needs {State.Party.Count} food; {Count("food")} carried.";
                break;
            case WorldCommandKind.Craft:
                var recipe = Content.Game.Recipes.Select(Content.Get<RecipeDefinition>).FirstOrDefault(r => r.Id == command.Item && r.Output != null);
                if (recipe == null) return "Choose a camp crafting recipe.";
                if (!NearbyCamp(State.Position)) return "Approach a serviceable camp first.";
                string missing = MissingSupplies(recipe.Cost); if (missing.Length > 0) return missing;
                var camp = new[] { State.Position }.Concat(Directions.Select(d => State.Position.Offset(d.X, d.Y))).First(p => Chunks.At(p) == Material.Camp);
                if (State.Work.Any(w => w.Target == camp)) return "This camp already has a work order.";
                break;
            case WorldCommandKind.Forage:
                if (Party.First(a => a.Life == LifeState.Alive).Body.ForageAfter > State.Tick) return "These gathering grounds need an hour to recover.";
                if (State.Work.Any(w => w.Target == State.Position)) return "A work order already occupies this cell.";
                break;
            case WorldCommandKind.Equip:
                if (actor == null || !State.Party.Contains(actor.Id) || actor.Life != LifeState.Alive) return "Choose a conscious companion.";
                if (!TryItem(command.Item, out var item) || item.Slot == "none") return "Choose equipment.";
                if (Count(item.Stack) < 1) return "No " + item.Name + " in the shared pack.";
                break;
            case WorldCommandKind.Unequip:
                if (actor == null || !State.Party.Contains(actor.Id) || !actor.Equipment.ContainsKey(command.Item)) return "The selected equipment slot is empty.";
                break;
            case WorldCommandKind.Revive:
                if (actor == null || !State.Party.Contains(actor.Id) || actor.Life != LifeState.Downed) return "Choose a downed companion.";
                if (Count("medicine") < 1) return "Revival needs one medicine.";
                break;
            case WorldCommandKind.Abandon:
                if (actor == null || !State.Party.Contains(actor.Id) || actor.Life != LifeState.Downed) return "Only a downed companion can be abandoned.";
                break;
            case WorldCommandKind.Dismiss:
                if (actor == null || !State.Party.Contains(actor.Id) || actor.Life != LifeState.Alive) return "Choose a conscious companion.";
                if (!Party.Any(a => a.Id != actor.Id && a.Life == LifeState.Alive)) return "Another conscious traveller must remain in the party.";
                if (State.Position.Distance(Plan.Towns[actor.HomeTown].Center) > 28) return "Return to " + Plan.Towns[actor.HomeTown].Name + " to part ways.";
                break;
        }
        if (command.Kind is WorldCommandKind.Craft or WorldCommandKind.Forage && State.Work.Count >= 64) return "The work list is full.";
        return "";
    }
}
