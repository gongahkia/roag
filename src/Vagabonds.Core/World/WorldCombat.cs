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
        var battle = new WorldBattle();
        foreach (var a in Party.Where(a => a.Life == LifeState.Alive)) AddUnit(a, true);
        // A lone traveller meets one foe; larger parties can draw in nearby enemies.
        AddUnit(enemy, false);
        if (enemy.EncounterLeader > 0)
            foreach (var a in State.Actors.Where(a => a.Id != enemyId && a.EncounterLeader == enemy.EncounterLeader && a.Life == LifeState.Alive).Take(3)) AddUnit(a, false);
        else if (State.Party.Count > 1) foreach (var a in State.Actors.Where(a => a.Id != enemyId && a.EncounterLeader == 0 && a.Kind is ActorKind.Beast or ActorKind.Raider && a.Life == LifeState.Alive && a.Position.Distance(enemy.Position) < 8).Take(State.Party.Count - 1)) AddUnit(a, false);
        State.Battle = battle; NewCombatRound(); BeginCombatTurn();
        void AddUnit(Actor a, bool friendly)
        {
            int index = battle.Units.Count;
            battle.ActorIds.Add(a.Id);
            battle.Units.Add(new() { Id = index, HeroId = friendly ? index : -1, Name = a.Name, Sprite = a.Sprite, Health = a.Health, MaxHealth = a.MaxHealth, Mana = a.Mana, MaxMana = a.MaxMana, Stress = a.Stress, Armor = a.Armor, Speed = a.Speed, Power = a.Power, Position = new Cell(friendly ? 1 : 5, index % 4 + 1) });
        }
    }
    private int CombatRoll(int max)
    {
        var r = new RandomStream(State.CombatRandom); int roll = r.Next(max); State.CombatRandom = r.State; return roll;
    }
    private void NewCombatRound()
    {
        var b = State.Battle!;
        foreach (var unit in b.Units)
        {
            var actor = State.Actors.Single(a => a.Id == b.ActorIds[unit.Id]);
            unit.Power = actor.Power; unit.Speed = actor.Speed; unit.Armor = actor.Armor;
        }
        b.Order = b.Units.Where(u => u.Alive).Select(u => (u.Id, Roll: u.Speed + CombatRoll(3))).OrderByDescending(v => v.Roll).ThenBy(v => v.Id).Select(v => v.Id).ToList();
        b.TurnIndex = 0;
        PlanEnemies();
    }
    private void BeginCombatTurn()
    {
        int budget = 100;
        while (State.Battle is { } b && budget-- > 0)
        {
            var u = b.Active; u.Guarding = false;
            if (u.Burn > 0) { Damage(u, 3); u.Burn--; }
            if (u.Bleed > 0) { Damage(u, 3); u.Bleed--; }
            if (u.Mark > 0) u.Mark--;
            SyncCombat();
            if (CheckBattleEnd()) return;
            if (u.Alive && u.Stun == 0) return;
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
        return Class(actor).Abilities.Concat(Content.Game.CommonAbilities).Select(id => Content.Get<AbilityDefinition>(id).AsSkill()).ToArray();
    }
    public ActionResult Act(int skillIndex, int targetIndex = -1)
    {
        if (State.Battle is not { } b || !b.Active.Friendly) return new(false, "Wait for your turn.");
        var skills = ActiveSkills();
        if (skillIndex < 0 || skillIndex >= skills.Count) return new(false, "Choose an ability.");
        var skill = skills[skillIndex]; var u = b.Active;
        CombatUnit? target = targetIndex >= 0 && targetIndex < b.Units.Count ? b.Units[targetIndex] : null;
        if (skill.Target == TargetKind.Enemy && (target == null || target.Friendly || !target.Alive) || skill.Target == TargetKind.Ally && (target == null || !target.Friendly)) return new(false, "Choose a valid target.");
        if (u.Mana < skill.Mana || skill.Effect == "potion" && Count("medicine") == 0) return new(false, "Insufficient mana or medicine.");
        u.Mana -= skill.Mana;
        var actor = State.Actors.Single(a => a.Id == b.ActorIds[u.Id]);
        if (skill.Effect == "flee")
        {
            SyncCombat(); State.Battle = null;
            foreach (var a in Party) a.Stress = Math.Min(100, a.Stress + 5);
            var retreat = Directions.Select(d => State.Position.Offset(d.X * 5, d.Y * 5)).FirstOrDefault(p => Plan.Contains(p) && Materials.Walkable(Chunks.At(p)), State.Position);
            State.Position = retreat; foreach (var a in Party) a.Position = retreat;
            Reveal(); return new(true, "Retreated. Enemies retain their wounds.");
        }
        if (skill.Effect == "potion") Add("medicine", -1);
        var targets = skill.Target switch
        {
            TargetKind.AllEnemies => b.Units.Where(t => !t.Friendly && t.Alive).ToArray(),
            TargetKind.AllAllies => b.Units.Where(t => t.Friendly && t.Alive).ToArray(),
            TargetKind.Self => [u], _ => new[] { target! }
        };
        ResolveAbility(u, skill, targets, actor.Level - 1 + u.Power - 6);
        SyncCombat();
        if (!CheckBattleEnd()) { StepCombatOrder(); BeginCombatTurn(); }
        return new(true, $"{u.Name}: {skill.Name}.");
    }
    private void ResolveAbility(CombatUnit caster, Skill skill, IEnumerable<CombatUnit> targets, int bonus)
    {
        foreach (var t in targets)
        {
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
                Damage(t, skill.Power + bonus + CombatRoll(4) + (t.Mark > 0 ? 4 : 0) - (caster.Stress >= 60 ? 2 : 0));
                if (skill.Effect == "drain") caster.Health = Math.Min(caster.MaxHealth, caster.Health + Math.Max(1, (before - t.Health) / 2));
                if (skill.Effect == "dread") t.Stress = Math.Min(100, t.Stress + Math.Max(8, skill.Power));
                if (t.Alive)
                {
                    if (skill.Effect == "burn") t.Burn = skill.Duration;
                    if (skill.Effect == "bleed") t.Bleed = skill.Duration;
                    if (skill.Effect == "mark") t.Mark = skill.Duration;
                    if (skill.Effect == "stun") t.Stun = Math.Min(2, skill.Duration);
                }
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
    private void Damage(CombatUnit target, int amount)
    {
        int damage = Math.Max(1, amount - target.Armor); if (target.Guarding) damage = Math.Max(1, damage / 2);
        var actor = State.Actors.Single(a => a.Id == State.Battle!.ActorIds[target.Id]);
        if (damage >= Math.Max(8, target.MaxHealth / 3)) actor.Body.Wounds = Math.Min(10, actor.Body.Wounds + 1);
        target.Health = Math.Max(0, target.Health - damage);
        if (!target.Alive)
        {
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
        SyncCombat();
        bool victory = b.Units.Any(u => u.Friendly && u.Alive);
        if (b.TurnIndex == b.Order.Count - 1) AdvanceCombatClock();
        State.Battle = null;
        if (victory)
        {
            Add("gold", 12); Add("food", 3);
            foreach (var a in Party.Where(a => a.Life == LifeState.Alive))
            {
                a.Experience += 12;
                if (a.Experience >= a.Level * 30) { a.Experience -= a.Level * 30; a.Level++; a.Health = Math.Min(a.MaxHealth, a.Health + 6); a.Mana = Math.Min(a.MaxMana, a.Mana + 3); }
            }
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
}
