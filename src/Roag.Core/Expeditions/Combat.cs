namespace Roag.Core.Expeditions;

public sealed partial class Campaign
{
    private void StartEncounter(WorldObject encounter)
    {
        var battle = new Battle { EncounterId = encounter.Id, Name = encounter.Name, Boss = encounter.Id == "abbey-abbot" || FieldMode && encounter.Id.EndsWith("-heavy", StringComparison.Ordinal) };
        var occupied = new HashSet<Cell>();
        int radius = ZoneId == "crypt" ? 2 : 6;
        var arena = new HashSet<Cell> { encounter.Position };
        var frontier = new Queue<Cell>();
        frontier.Enqueue(encounter.Position);
        while (frontier.TryDequeue(out var cell))
            foreach (var n in Zone.Neighbors(cell))
                if (n.Distance(encounter.Position) <= radius && Walkable(n) && arena.Add(n)) frontier.Enqueue(n);
        Cell Place(Cell desired)
        {
            var best = arena.Where(p => !occupied.Contains(p)).OrderBy(p => p.Distance(desired)).ThenBy(p => p.Y).ThenBy(p => p.X).First();
            occupied.Add(best);
            return best;
        }
        foreach (var h in Party)
        {
            battle.Units.Add(new()
            {
                Id = battle.Units.Count,
                HeroId = h.Id,
                Name = h.Name,
                Sprite = h.Sprite,
                Position = Place(encounter.Position + new Cell(-4 - battle.Units.Count % 2 * 2, battle.Units.Count / 2 * 3 - 2)),
                Health = h.Health,
                MaxHealth = h.MaxHealth,
                Mana = h.Mana,
                MaxMana = h.MaxMana,
                Stress = h.Stress,
                Armor = h.Armor,
                Speed = h.Speed,
                Power = h.Level - 1
            });
        }

        (string Name, string Sprite, int Health, int Power, int Speed, int Armor)[] enemies = FieldMode ? FieldEnemies(encounter.Id) : encounter.Id switch
        {
            "wilds-hounds" => [("Briar hound", "wolf", 23, 6, 7, 0), ("Briar hound", "wolf", 23, 6, 6, 0)],
            "wilds-raiders" => [("Ashwood raider", "bandit", 34, 8, 6, 2), ("Grief wisp", "wisp", 23, 5, 5, 0)],
            "abbey-hollows" => [("Oathless guard", "skeleton", 30, 8, 5, 2), ("Oathless guard", "skeleton", 30, 8, 4, 2), ("Grief wisp", "wisp", 20, 4, 6, 0)],
            "abbey-slimes" => [("Cistern slime", "slime", 28, 6, 3, 0), ("Cistern slime", "slime", 28, 6, 4, 0)],
            _ => [("Hollow Abbot", "abbot", 112, 12, 5, 3), ("Bell keeper", "skeleton", 30, 7, 4, 2), ("Grief wisp", "wisp", 24, 5, 6, 0)]
        };
        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            battle.Units.Add(new()
            {
                Id = battle.Units.Count,
                Name = e.Name,
                Sprite = e.Sprite,
                Health = e.Health,
                MaxHealth = e.Health,
                Power = e.Power,
                Speed = e.Speed,
                Armor = e.Armor,
                Position = Place(encounter.Position + new Cell(3, i * 3 - 2))
            });
        }
        Battle = battle;
        Note("Encounter / " + encounter.Name + ". Read enemy intentions before choosing an action.");
        NewRound();
    }
    private void NewRound()
    {
        var b = Battle!;
        b.Order = b.Units.Where(u => u.Alive).Select(u => (Unit: u, Tie: Random.Next(3))).OrderByDescending(x => x.Unit.Speed + x.Tie).ThenBy(x => x.Unit.Id).Select(x => x.Unit.Id).ToList();
        b.TurnIndex = 0;
        foreach (var enemy in b.Units.Where(u => !u.Friendly && u.Alive))
        {
            var targets = b.Units.Where(u => u.Friendly && u.Alive).ToArray();
            if (targets.Length == 0)
            {
                break;
            }

            enemy.IntentTarget = targets[Random.Next(targets.Length)].Id;
            enemy.Intent = enemy.Sprite switch
            {
                "abbot" => b.Round % 3 == 0 ? IntentKind.Sweep : b.Round % 3 == 2 ? IntentKind.Heal : IntentKind.Bleed,
                "wisp" => IntentKind.Dread,
                "wolf" when b.Round % 2 == 0 => IntentKind.Bleed,
                "bandit" when b.Round % 3 == 0 => IntentKind.Guard,
                _ => IntentKind.Strike
            };
        }
        BeginTurn();
    }
    private void BeginTurn()
    {
        if (Battle is not { } b)
        {
            return;
        }

        var active = b.Active;
        if (!active.Alive)
        {
            Advance();
            return;
        }
        active.Guarding = false;
        if (active.Burn > 0)
        {
            active.Burn--;
            Damage(active, 3, "burn", false);
        }
        if (active.Bleed > 0)
        {
            active.Bleed--;
            Damage(active, 3, "bleed", false);
        }
        if (CheckEnd())
        {
            return;
        }

        if (!active.Alive)
        {
            Advance();
            return;
        }
        if (active.Mark > 0)
        {
            active.Mark--;
        }

        if (active.Stun > 0)
        {
            active.Stun--;
            Note(active.Name + " loses a turn to stun.");
            Advance();
        }
    }
    private void Advance()
    {
        if (CheckEnd() || Battle is not { } b)
        {
            return;
        }

        b.TurnIndex++;
        if (b.TurnIndex >= b.Order.Count)
        {
            b.Round++;
            NewRound();
        }
        else
        {
            BeginTurn();
        }
    }
    private void Emit(CombatUnit target, int amount, string kind, string text)
    {
        CombatEvents.Add(new(++_sequence, target.Id, amount, kind, text));
        if (CombatEvents.Count > 24)
        {
            CombatEvents.RemoveAt(0);
        }

        Note(text);
    }
    private void Damage(CombatUnit target, int amount, string source, bool armor = true)
    {
        int value = Math.Max(1, amount - (armor ? target.Armor : 0));
        if (target.Guarding)
        {
            value = Math.Max(1, (value + 1) / 2);
        }

        target.Health = Math.Max(0, target.Health - value);
        Emit(target, value, "damage", target.Name + " takes " + value + " " + source + " damage.");
        if (!target.Alive)
        {
            target.Burn = target.Bleed = target.Stun = 0;
            if (target.Friendly)
            {
                foreach (var u in Battle!.Units.Where(u => u.Friendly && u.Alive))
                {
                    u.Stress = Math.Min(100, u.Stress + 10);
                }
            }
        }
    }
    public string IntentText(CombatUnit enemy)
    {
        if (Battle is null)
        {
            return "";
        }

        string target = Battle.Units[enemy.IntentTarget].Name;
        return enemy.Intent switch
        {
            IntentKind.Sweep => FieldMode ? "SWEEP / all residents" : "BELL TOLL / all allies",
            IntentKind.Heal => FieldMode ? "RECOVER / threats" : "BLESSING / heal enemies",
            IntentKind.Dread => "DREAD / " + target,
            IntentKind.Bleed => "REND / " + target,
            IntentKind.Guard => "BRACE / self",
            _ => "STRIKE / " + target
        };
    }
    public ActionResult Act(int skillIndex, int? targetId = null)
    {
        if (Battle is not { } b || !b.Active.Friendly)
        {
            return new(false, "Wait for your companion's turn.");
        }

        if (skillIndex < 0 || skillIndex > 5)
        {
            return new(false, "Unknown action.");
        }

        var actor = b.Active;
        var hero = Party.Single(h => h.Id == actor.HeroId);
        var skill = FieldMode ? Skills.FieldGet(hero.Role, skillIndex) : Skills.Get(hero.Role, skillIndex);
        if (actor.Mana < skill.Mana)
        {
            return new(false, "Not enough mana for " + skill.Name + ".");
        }

        if (skill.Effect == "potion" && Potions <= 0)
        {
            return new(false, "The party has no potions left.");
        }

        var target = targetId is int id && id >= 0 && id < b.Units.Count ? b.Units[id] : null;
        if (skill.Target == TargetKind.Enemy && (target is null || target.Friendly || !target.Alive))
        {
            return new(false, "Choose a living enemy on the map or in the enemy list.");
        }

        if (skill.Target == TargetKind.Ally && (target is null || !target.Friendly))
        {
            return new(false, "Choose a companion on the map or a party card.");
        }

        actor.Mana -= skill.Mana;
        if (skill.Effect == "flee")
        {
            foreach (var u in b.Units.Where(u => u.Friendly))
            {
                u.Stress = Math.Min(100, u.Stress + 5);
            }

            SyncParty();
            Battle = null;
            Position = PreviousPosition;
            Reveal();
            return Result(true, "The party retreats. The encounter remains; +5 stress each.");
        }
        IEnumerable<CombatUnit> targets = skill.Target switch
        {
            TargetKind.AllAllies => b.Units.Where(u => u.Friendly),
            TargetKind.AllEnemies => b.Units.Where(u => !u.Friendly && u.Alive),
            TargetKind.Self => [actor],
            _ => [target!]
        };
        if (skill.Effect == "potion")
        {
            Potions--;
        }

        Note(actor.Name + " uses " + skill.Name + ".");
        foreach (var victim in targets.ToArray())
        {
            if (skill.Effect is "heal" or "potion")
            {
                int before = victim.Health;
                victim.Health = Math.Min(victim.MaxHealth, victim.Health + skill.Power + hero.Level - 1);
                victim.Bleed = 0;
                Emit(victim, victim.Health - before, "heal", victim.Name + " recovers " + (victim.Health - before) + " health.");
            }
            else if (skill.Effect == "calm")
            {
                victim.Stress = Math.Max(0, victim.Stress - skill.Power);
                Emit(victim, skill.Power, "calm", victim.Name + " finds composure.");
            }
            else if (skill.Effect is "guard" or "rally")
            {
                if (victim.Alive)
                {
                    victim.Guarding = true;
                }

                if (skill.Effect == "rally")
                {
                    victim.Stress = Math.Max(0, victim.Stress - 8);
                }
            }
            else
            {
                int power = skill.Power + actor.Power + Random.Next(4) + (victim.Mark > 0 ? 4 : 0) - (actor.Stress >= 80 ? 2 : 0);
                Damage(victim, power, skill.Effect == "burn" ? "fire" : "strike");
                if (victim.Alive)
                {
                    switch (skill.Effect)
                    {
                        case "burn":
                            victim.Burn = 3;
                            break;
                        case "mark":
                            victim.Mark = 3;
                            break;
                        case "stun":
                            victim.Stun = 1;
                            break;
                    }
                }
            }
        }
        Advance();
        return new(true, actor.Name + " / " + skill.Name);
    }
    public ActionResult AdvanceEnemy()
    {
        if (Battle is not { } b || b.Active.Friendly)
        {
            return new(false, "A companion is ready.");
        }

        var actor = b.Active;
        var target = b.Units[actor.IntentTarget];
        if (!target.Alive)
        {
            target = b.Units.First(u => u.Friendly && u.Alive);
        }

        Note(actor.Name + " / " + IntentText(actor));
        if (actor.Intent == IntentKind.Guard)
        {
            actor.Guarding = true;
        }
        else if (actor.Intent == IntentKind.Heal)
        {
            foreach (var ally in b.Units.Where(u => !u.Friendly && u.Alive))
            {
                int before = ally.Health;
                ally.Health = Math.Min(ally.MaxHealth, ally.Health + 7);
                Emit(ally, ally.Health - before, "heal", ally.Name + " receives the crooked blessing.");
            }
        }
        else
        {
            var targets = actor.Intent == IntentKind.Sweep ? b.Units.Where(u => u.Friendly && u.Alive).ToArray() : [target];
            foreach (var victim in targets)
            {
                Damage(victim, (actor.Intent == IntentKind.Sweep ? 9 : actor.Power) + Random.Next(3), actor.Intent == IntentKind.Dread ? "dread" : "strike");
                victim.Stress = Math.Min(100, victim.Stress + (actor.Intent == IntentKind.Dread ? 12 : actor.Intent == IntentKind.Sweep ? 10 : 3) + (Light < 25 ? 3 : 0));
                if (victim.Alive && actor.Intent == IntentKind.Bleed)
                {
                    victim.Bleed = 2;
                }
            }
        }
        Advance();
        return new(true, actor.Name + " acted.");
    }
    private void SyncParty()
    {
        foreach (var unit in Battle!.Units.Where(u => u.Friendly))
        {
            var h = Party.Single(h => h.Id == unit.HeroId);
            h.Health = unit.Health;
            h.Mana = unit.Mana;
            h.Stress = unit.Stress;
        }
    }
    private bool CheckEnd()
    {
        if (Battle is not { } b)
        {
            return true;
        }

        if (!b.Units.Any(u => u.Friendly && u.Alive))
        {
            SyncParty();
            foreach (var h in Party)
            {
                h.Health = Math.Max(1, h.MaxHealth / 2);
                h.Mana = Math.Max(h.Mana, h.MaxMana / 3);
                h.Stress = Math.Min(100, h.Stress + 15);
            }
            int lost = Gold / 4;
            Gold -= lost;
            Battle = null;
            if (FieldMode) RetreatRequested = true;
            else ZoneId = "hearth";
            Position = PreviousPosition = FieldMode ? Zone.Objects.Single(o => o.Kind == ObjectKind.Return).Position : new(18, 16);
            Light = 100;
            Reveal();
            Note(FieldMode ? "RETREAT / Injured, you withdraw toward the shelter." : "RETREAT / The party is carried to Hearth. Lost " + lost + " gold. The road remains open; heal and try again.");
            return true;
        }
        if (!b.Units.Any(u => !u.Friendly && u.Alive))
        {
            SyncParty();
            Completed.Add(b.EncounterId);
            int reward = b.Boss ? 55 : 18, experience = b.Boss ? 45 : 20;
            if (!FieldMode) { Gold += reward; Potions++; }
            foreach (var h in Party)
            {
                h.Experience += experience;
                h.Stress = Math.Max(0, h.Stress - 6);
                while (h.Experience >= h.Level * 30)
                {
                    h.Experience -= h.Level * 30;
                    h.Level++;
                    h.Health = Math.Min(h.MaxHealth, h.Health + 6);
                    h.Mana = Math.Min(h.MaxMana, h.Mana + 3);
                    Note(h.Name + " reaches level " + h.Level + ".");
                }
            }
            Note(FieldMode ? "The movement stops. +" + experience + " experience each." : "VICTORY / " + b.Name + ". +" + reward + " gold, +" + experience + " XP each, +1 potion.");
            Battle = null;
            return true;
        }
        return false;
    }
    /// <summary>Deterministic input driver for headless verification; uses real paid actions.</summary>
    public void AutoBattle(int turnLimit = 500)
    {
        for (int turn = 0; Battle is { } b && turn < turnLimit; turn++)
        {
            if (!b.Active.Friendly)
            {
                AdvanceEnemy();
                continue;
            }
            var actor = b.Active;
            var hero = Party.Single(h => h.Id == actor.HeroId);
            var hurt = b.Units.Where(u => u.Friendly && u.Health < u.MaxHealth - 16).OrderBy(u => u.Health).FirstOrDefault();
            if (hurt is not null && hero.Role == 3 && actor.Mana >= 3)
            {
                Act(1, hurt.Id);
                continue;
            }
            if (hurt is not null && hurt.Health < 12 && Potions > 0)
            {
                Act(4, hurt.Id);
                continue;
            }
            int skill = hero.Role switch
            {
                2 when actor.Mana >= 5 && b.Units.Count(u => !u.Friendly && u.Alive) > 1 => 2,
                2 when actor.Mana >= 3 => 1,
                1 when actor.Mana >= 4 && b.Units.Count(u => !u.Friendly && u.Alive) > 1 => 2,
                _ => 0
            };
            Act(skill, b.Units.First(u => !u.Friendly && u.Alive).Id);
        }
        if (Battle is not null)
        {
            throw new InvalidOperationException("Battle exceeded its verification turn limit.");
        }
    }
}
