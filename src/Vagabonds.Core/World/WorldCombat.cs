using Vagabonds.Core.Expeditions;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public void StartBattle(long enemyId)
    {
        if (IsInCombat || State.AwaitingContinuation || State.Endgame.Victory) return;
        var enemy = State.Actors.FirstOrDefault(a => a.Id == enemyId && a.Life == LifeState.Alive && a.Kind is ActorKind.Raider or ActorKind.Beast);
        if (enemy == null) return;
        if (SituationForActor(enemy) is { } discovery)
        { if (discovery.Outcome is not ("" or "tunnel")) return; discovery.Engaged = true; }
        if (Ecology(enemy) is { Behavior: not "flee" }) enemy.ProvokedUntil = State.Tick + 75;
        State.CombatEvents.Clear(); State.LastEncounterReward = null;
        var battle = new WorldBattle();
        foreach (var a in Party.Where(a => a.Life == LifeState.Alive)) AddUnit(a, true);
        // A lone traveller meets one foe; larger parties can draw in nearby enemies.
        AddUnit(enemy, false);
        if (enemy.EncounterLeader > 0)
            foreach (var a in State.Actors.Where(a => a.Id != enemyId && a.EncounterLeader == enemy.EncounterLeader && a.Life == LifeState.Alive).Take(3)) AddUnit(a, false);
        else if (State.Party.Count > 1) foreach (var a in State.Actors.Where(a => a.Id != enemyId && a.EncounterLeader == 0 && IsHostile(a) && a.Position.Distance(enemy.Position) < 8).Take(State.Party.Count - 1)) AddUnit(a, false);
        State.Battle = battle; NewCombatRound(); BeginCombatTurn();
        void AddUnit(Actor a, bool friendly)
        {
            int index = battle.Units.Count;
            battle.ActorIds.Add(a.Id);
            battle.Units.Add(new() { Id = index, HeroId = friendly ? index : -1, Name = a.Name, Sprite = a.Sprite, Health = a.Health, MaxHealth = a.MaxHealth, Mana = a.Mana, MaxMana = a.MaxMana, Stress = a.Stress, Armor = a.Armor + PreparationPower(a, "armor"), Speed = a.Speed + PreparationPower(a, "watch"), Guarding = PreparationPower(a, "watch") > 0, Power = a.Power, Position = new Cell(friendly ? 1 : 5, index % 4 + 1) });
        }
    }
    private int CombatRoll(int max)
    {
        var r = new RandomStream(State.CombatRandom); int roll = r.Next(max); State.CombatRandom = r.State; return roll;
    }
    private void NewCombatRound()
    {
        var b = State.Battle!; b.ShieldUsed.Clear(); b.Assisting.Clear();
        foreach (var unit in b.Units)
        {
            var actor = State.Actors.Single(a => a.Id == b.ActorIds[unit.Id]);
            unit.Power = actor.Power; unit.Speed = actor.Speed + PreparationPower(actor, "watch"); unit.Armor = actor.Armor + PreparationPower(actor, "armor");
        }
        b.Order = b.Units.Where(u => u.Alive).Select(u => (u.Id, Roll: u.Speed + CombatRoll(3))).OrderByDescending(v => v.Roll).ThenBy(v => v.Id).Select(v => v.Id).ToList();
        b.TurnIndex = 0;
        PlanEnemies();
        Feedback("round", null, null, "Round " + b.Round, 0, "Enemy intentions are fixed for this round.");
    }
    private void BeginCombatTurn()
    {
        int budget = 100;
        while (State.Battle is { } b && budget-- > 0)
        {
            var u = b.Active; u.Guarding = false; bool assisted = b.Assisting.Remove(u.Id);
            if (u.Burn > 0) { Damage(u, 3, label: "Burn"); u.Burn = Math.Max(0, u.Burn - 1); if (u.Burn == 0) Feedback("expire", u, u, "Burn ended"); }
            if (u.Bleed > 0) { Damage(u, 3, label: "Bleed"); u.Bleed = Math.Max(0, u.Bleed - 1); if (u.Bleed == 0) Feedback("expire", u, u, "Bleed ended"); }
            if (u.Mark > 0 && --u.Mark == 0) Feedback("expire", u, u, "Mark ended");
            SyncCombat();
            if (CheckBattleEnd()) return;
            if (u.Alive && u.Stun == 0 && !assisted) return;
            if (u.Alive) Feedback("skip", u, u, assisted ? "Assisted" : "Stunned", 0, u.Name + (assisted ? " already spent this action assisting a companion." : " loses this turn."));
            u.Stun = Math.Max(0, u.Stun - 1); StepCombatOrder();
        }
    }
    private void StepCombatOrder()
    {
        var b = State.Battle!;
        b.TurnIndex++;
        if (b.TurnIndex >= b.Order.Count) { AdvanceCombatClock(); b.Round++; NewCombatRound(); }
    }
    public IReadOnlyList<Skill> ActiveSkills()
    {
        if (State.Battle is not { } b || !b.Active.Friendly) return [];
        var actor = State.Actors.Single(a => a.Id == b.ActorIds[b.Active.Id]);
        return Class(actor).Abilities.Concat(Content.Game.CommonAbilities).Concat(b.Units.Count(u => u.Friendly) > 1 ? Content.Game.CooperativeAbilities : [])
            .Select(id => Content.Get<AbilityDefinition>(id).AsSkill()).ToArray();
    }
    public ActionResult Act(int skillIndex, int targetIndex = -1, int partnerIndex = -1)
    {
        if (State.Battle is not { } b || !b.Active.Friendly) return new(false, "Wait for your turn.");
        var skills = ActiveSkills();
        if (skillIndex < 0 || skillIndex >= skills.Count) return new(false, "Choose an ability.");
        var skill = skills[skillIndex]; var u = b.Active;
        CombatUnit? target = targetIndex >= 0 && targetIndex < b.Units.Count ? b.Units[targetIndex] : null;
        string blocked = ActionPreview(skillIndex, targetIndex, partnerIndex).Reason;
        if (blocked.Length > 0) return new(false, blocked);
        u.Mana -= skill.Mana;
        CombatUnit? partner = skill.Cooperative ? b.Units[partnerIndex] : null;
        if (partner != null)
        {
            partner.Mana -= skill.PartnerMana; b.Assisting.Add(partner.Id);
            Feedback("ability", partner, partner, "Assists " + skill.Name, skill.PartnerMana,
                partner.Name + " joins " + u.Name + ": " + skill.PartnerMana + " MP and this round's action.");
        }
        var actor = State.Actors.Single(a => a.Id == b.ActorIds[u.Id]);
        if (skill.Effect == "flee")
        {
            Feedback("retreat", u, null, "Retreated", 0, "Enemies retain their wounds.");
            SyncCombat(); State.Battle = null;
            foreach (var a in Party) a.Stress = Math.Min(100, a.Stress + 5);
            foreach (var direction in Directions)
            {
                var retreat = State.Position.Offset(direction.X * 5, direction.Y * 5);
                if (!TryPartyPlacement(retreat, out var positions)) continue;
                ApplyPartyPlacement(retreat, positions);
                if (State.WorkRally != null) State.WorkRally = retreat;
                break;
            }
            StopPartyWorkApproach(); Reveal(); return new(true, "Retreated. Enemies retain their wounds.");
        }
        if (skill.Effect == "potion") Add("medicine", -1);
        var targets = skill.Target switch
        {
            TargetKind.AllEnemies => b.Units.Where(t => !t.Friendly && t.Alive).ToArray(),
            TargetKind.AllAllies => b.Units.Where(t => t.Friendly && t.Alive).ToArray(),
            TargetKind.Self => [u], _ => new[] { target! }
        };
        ResolveAbility(u, skill, targets, actor.Level - 1 + u.Power - 6 + (partner?.Power ?? 0) * skill.PartnerPowerPercent / 100);
        SyncCombat();
        if (!CheckBattleEnd()) { StepCombatOrder(); BeginCombatTurn(); }
        return new(true, $"{u.Name}" + (partner == null ? "" : " and " + partner.Name) + $": {skill.Name}.");
    }
    private void ResolveAbility(CombatUnit caster, Skill skill, IEnumerable<CombatUnit> targets, int bonus)
    {
        bonus += AbilityEquipmentBonus(CombatActor(caster), skill);
        Feedback("ability", caster, null, skill.Name, 0, caster.Name + " uses " + skill.Name + ".", abilityId: skill.Id);
        foreach (var t in targets)
        {
            int hp = t.Health, stress = t.Stress, burn = t.Burn, bleed = t.Bleed, mark = t.Mark, stun = t.Stun;
            bool guarding = t.Guarding;
            if (skill.Effect is "heal" or "potion" or "cleanse")
            {
                t.Health = Math.Min(t.MaxHealth, t.Health + skill.Power + bonus); t.Bleed = 0;
                if (skill.Effect == "cleanse") { t.Burn = t.Stun = t.Mark = 0; t.Stress = Math.Max(0, t.Stress - 10); }
            }
            else if (skill.Effect == "calm") t.Stress = Math.Max(0, t.Stress - skill.Power);
            else if (skill.Effect is "guard" or "rally") { t.Guarding = true; if (skill.Effect == "rally") t.Stress = Math.Max(0, t.Stress - 8); }
            else
            {
                int before = t.Health;
                Damage(t, skill.Power + bonus + CombatRoll(4) + (t.Mark > 0 ? 4 : 0) - (caster.Stress >= 60 ? 2 : 0), caster, skill.Name);
                if (skill.Effect == "drain")
                {
                    int old = caster.Health; caster.Health = Math.Min(caster.MaxHealth, caster.Health + Math.Max(1, (before - t.Health) / 2));
                    Feedback("heal", caster, caster, "Drain", caster.Health - old, caster.Name + " recovers " + (caster.Health - old) + " HP.");
                }
                if (skill.Effect == "dread") t.Stress = Math.Min(100, t.Stress + Math.Max(0, Math.Max(8, skill.Power) - PreparationPower(CombatActor(t), "focus") * 3 - EquipmentEffectPower(CombatActor(t), "resolve")));
                if (t.Alive)
                {
                    if (skill.Effect == "burn") t.Burn = skill.Duration;
                    if (skill.Effect == "bleed") t.Bleed = skill.Duration;
                    if (skill.Effect == "mark") t.Mark = skill.Duration;
                    if (skill.Effect == "stun") t.Stun = Math.Min(2, skill.Duration);
                }
            }
            if (t.Health > hp) Feedback("heal", caster, t, skill.Name, t.Health - hp, t.Name + " recovers " + (t.Health - hp) + " HP.");
            if (t.Stress != stress) Feedback("status", caster, t, "Stress " + (t.Stress - stress).ToString("+0;-0;0"), Math.Abs(t.Stress - stress), t.Name + ": stress " + t.Stress + ".");
            Status("Burn", burn, t.Burn); Status("Bleed", bleed, t.Bleed); Status("Mark", mark, t.Mark); Status("Stun", stun, t.Stun);
            if (t.Guarding != guarding) Feedback("status", caster, t, "Guard", 0, t.Name + " guards: incoming damage is halved after armour.");
            void Status(string name, int old, int current)
            {
                if (old != current) Feedback(current == 0 ? "expire" : "status", caster, t, current == 0 ? name + " removed" : name + " " + current, current, t.Name + ": " + name + (current == 0 ? " removed." : " for " + current + " turns."));
            }
        }
    }
    public ActionResult AdvanceEnemy()
    {
        if (State.Battle is not { } b || b.Active.Friendly) return new(false, "It is your turn.");
        var u = b.Active;
        var planned = b.Plans[u.Id]; var ability = Content.Get<AbilityDefinition>(planned.Ability); var skill = ability.AsSkill();
        var target = b.Units[planned.Target];
        // Intent is fixed at the start of the round. A fallen target wastes this action.
        var targets = skill.Target switch
        {
            TargetKind.AllEnemies => b.Units.Where(t => t.Friendly && t.Alive).ToArray(),
            TargetKind.AllAllies => b.Units.Where(t => !t.Friendly && t.Alive).ToArray(),
            TargetKind.Self => [u], _ => target.Alive ? new[] { target } : []
        };
        u.Mana -= skill.Mana;
        ResolveAbility(u, skill, targets, u.Power - 6);
        SyncCombat();
        if (!CheckBattleEnd()) { StepCombatOrder(); BeginCombatTurn(); }
        return new(true, $"{u.Name}: {skill.Name}.");
    }
    private void Damage(CombatUnit target, int amount, CombatUnit? source = null, string label = "Hit")
    {
        var (damage, resistance) = CombatDamageBeforeProtection(target, amount, label);
        if (resistance > 0) Feedback("status", target, target, "Meal protection", resistance, target.Name + " resists " + label.ToLowerInvariant() + " with camp preparation.");
        if (source != null && damage > 1 && ShieldProtector(target) is { } protector)
        {
            int blocked = Math.Min(damage - 1, EquipmentEffectPower(CombatActor(protector), "protect")); damage -= blocked;
            State.Battle!.ShieldUsed.Add(protector.Id);
            Feedback("status", protector, target, "Shield cover", blocked, protector.Name + " blocks " + blocked + " damage for wounded " + target.Name + ". Once per round.");
        }
        var actor = State.Actors.Single(a => a.Id == State.Battle!.ActorIds[target.Id]);
        if (damage >= Math.Max(8, target.MaxHealth / 3))
        {
            int wounds = actor.Body.Wounds; actor.Body.Wounds = Math.Min(10, wounds + 1);
            if (wounds != actor.Body.Wounds) Feedback("injury", source, target, "Wound", 1, target.Name + " suffers a lasting wound.");
        }
        int lost = Math.Min(target.Health, damage);
        target.Health = Math.Max(0, target.Health - damage);
        Feedback("damage", source, target, label, lost, target.Name + " takes " + lost + " damage" + (target.Guarding ? " (guarding)." : "."));
        if (!target.Alive)
        {
            Feedback("fall", source, target, target.Friendly ? "Downed" : "Defeated", 0, target.Name + (target.Friendly ? " is downed." : " falls."));
            target.Burn = target.Bleed = target.Stun = target.Mark = 0;
            if (target.Friendly) foreach (var friend in State.Battle!.Units.Where(u => u.Friendly && u.Alive)) friend.Stress = Math.Min(100, friend.Stress + 10);
        }
    }
    private void SyncCombat()
    {
        if (State.Battle is not { } b) return;
        foreach (var u in b.Units)
        {
            var a = State.Actors.Single(a => a.Id == b.ActorIds[u.Id]);
            a.Health = u.Health; a.Mana = u.Mana; a.Stress = u.Stress;
            if (a.Life == LifeState.Alive && !u.Alive)
            {
                a.Body.Wounds = Math.Min(10, a.Body.Wounds + 1);
                if (!u.Friendly) LeaveWildlifeRemains(a);
                foreach (var other in State.Actors.Where(p => p.Id != a.Id && p.Life == LifeState.Alive && (p.FriendId == a.Id || p.Trust.GetValueOrDefault(a.Id) > 0 || p.Recruited && a.Recruited))) Remember(other, "fallen-friend", a.Name + " fell in the fighting.", -8, a.Id);
            }
            a.Life = u.Alive ? LifeState.Alive : u.Friendly ? LifeState.Downed : LifeState.Dead;
        }
    }
    private bool CheckBattleEnd()
    {
        var b = State.Battle!;
        bool finalDefeated = b.ActorIds.Contains(State.Endgame.ActorId) && !b.Units[b.ActorIds.IndexOf(State.Endgame.ActorId)].Alive;
        if (!finalDefeated && b.Units.Any(u => u.Friendly && u.Alive) && b.Units.Any(u => !u.Friendly && u.Alive)) return false;
        SyncCombat(); ResolveDefeatedSituations();
        bool victory = b.Units.Any(u => u.Friendly && u.Alive);
        if (b.TurnIndex == b.Order.Count - 1) AdvanceCombatClock();
        State.Battle = null;
        if (victory)
        {
            bool wildlife = b.Units.Where(u => !u.Friendly).All(u => Ecology(State.Actors.Single(a => a.Id == b.ActorIds[u.Id])) != null);
            int gold = wildlife ? 0 : 12, food = wildlife ? 0 : 3, experience = wildlife ? 6 : 12;
            Add("gold", gold); Add("food", food);
            var levels = new List<string>();
            foreach (var a in Party.Where(a => a.Life == LifeState.Alive))
            {
                a.Experience += experience;
                if (a.Experience >= a.Level * 30) { a.Experience -= a.Level * 30; a.Level++; a.Health = Math.Min(a.MaxHealth, a.Health + 6); a.Mana = Math.Min(a.MaxMana, a.Mana + 3); levels.Add(a.Name + " / level " + a.Level); }
            }
            State.LastEncounterReward = new(gold, food, experience, levels.ToArray());
            Feedback("reward", null, null, "Victory", experience, (wildlife ? "6 experience to each conscious survivor. Recover ingredients from the remains." : "12 gold, 3 food; 12 experience to each conscious survivor.") + (levels.Count > 0 ? " " + string.Join(", ", levels) : ""));
            if (State.Actors.Single(a => a.Id == State.Endgame.ActorId).Life == LifeState.Dead)
            { State.Endgame.Victory = true; Paused = true; Log("The final threat has fallen. Your expedition is complete."); }
            Log("Encounter won. Fallen companions can be revived with medicine or deliberately abandoned.");
        }
        else
        {
            foreach (var a in Party) { a.Life = LifeState.Dead; a.Health = 0; Log($"{a.Name} died in the party wipe."); }
            var delta = Chunks.Delta(State.Position.Chunk);
            delta.Storage[State.Position.LocalIndex] = new(State.Inventory);
            Chunks.Edit(State.Position, Material.Storage); State.Inventory = new() { ["food"] = 8, ["wood"] = 12, ["stone"] = 8, ["medicine"] = 2, ["gold"] = 10 };
            State.AwaitingContinuation = true; CommitDeaths();
        }
        return true;
    }
    private void Feedback(string kind, CombatUnit? source, CombatUnit? target, string label, int amount = 0, string detail = "", string abilityId = "")
    {
        long ActorId(CombatUnit? unit) => unit == null || State.Battle == null ? 0 : State.Battle.ActorIds[unit.Id];
        var targetState = target == null ? null : new CombatantFeedbackState(target.Health, target.Mana, target.Stress, target.Guarding, target.Burn, target.Bleed, target.Mark, target.Stun);
        State.CombatEvents.Add(new(State.NextCombatEvent++, State.Battle?.Round ?? 0, kind, ActorId(source), ActorId(target), label, amount, detail, targetState, abilityId));
        if (State.CombatEvents.Count > 128) State.CombatEvents.RemoveAt(0);
    }
}
