using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public Actor FinalBoss => State.Actors.Single(a => a.Id == State.Endgame.ActorId);
    public string FinalObjective => State.Endgame.Victory ? "The world's final threat has fallen. This expedition is complete."
        : $"{FinalBoss.Name} waits beneath {Plan.Sites[State.Endgame.SiteId].Name} ({FinalBoss.Position.X}, {FinalBoss.Position.Y}), depth {FinalBoss.Position.Floor}. No seal or quest bars your way.";
    private void PopulateAdventures()
    {
        var rng = WorldRandom.Stream(Seed, 0, salt: 4100);
        var pool = Content.Game.Bosses.ToArray();
        for (int i = pool.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); (pool[i], pool[j]) = (pool[j], pool[i]); }
        int finalSite = rng.Next(Plan.Sites.Count);
        var gear = Content.All<ItemDefinition>().Where(i => i.Slot != "none" && !i.Tags.Contains("signature")).ToArray();
        foreach (var site in Plan.Sites)
        {
            var definition = Content.Get<BossDefinition>(pool[site.Id % pool.Length]);
            var theme = definition.Reward.Length > 0 ? Content.Get<RewardDefinition>(definition.Reward).EquipmentPool : [];
            var siteGear = theme.Length > 0 ? theme.Select(Content.Get<ItemDefinition>).ToArray() : gear;
            string variantId = definition.Variants[rng.Next(definition.Variants.Length)];
            int floor = 1 + rng.Next(3); long id = 40000 + site.Id * 8;
            var position = site.Center.Offset(0, -1) with { Floor = floor };
            var boss = ActorFactory.CreateBoss(Content, definition.Id, variantId, id, position, site.Id % 24, site.Culture, site.Id % 6);
            State.Actors.Add(boss);
            for (int i = 0; i < definition.Minions.Length; i++)
            {
                var minion = ActorFactory.Create(Content, definition.Minions[i], Seed, id + i + 1, position.Offset(i - 1, 1), site.Id % 24, site.Culture, site.Id % 6);
                minion.EncounterLeader = id; State.Actors.Add(minion);
            }
            if (site.Id == finalSite) State.Endgame = new() { ActorId = id, SiteId = site.Id };
            for (int depth = 0; depth < WorldPlan.Floors; depth++)
            {
                var lootRandom = WorldRandom.Stream(Seed, site.Id, depth, 4200);
                var contents = new Dictionary<string, int> { ["gold"] = 8 + lootRandom.Next(16), ["food"] = 2 + lootRandom.Next(4), ["medicine"] = 1 };
                if (depth == 0 && siteGear.Length > 0) contents[siteGear[lootRandom.Next(siteGear.Length)].Stack] = 1;
                State.Loot.Add(new() { Id = $"site-{site.Id}-{depth}", Position = site.Center.Offset(0, 2) with { Floor = depth }, Contents = contents,
                    Choices = depth == 0 ? [] : RewardChoices(siteGear.Select(g => g.Id), site.Id * 4 + depth, 4201) });
            }
        }
        Log(FinalObjective);
    }
    private ActionResult Loot(WorldPosition target, string choice)
    {
        var cache = State.Loot.FirstOrDefault(c => c.Position == target && !c.Taken);
        if (cache == null || State.Position.Distance(target) > 1) return new(false, "Approach an unopened cache.");
        var discovery = State.Discoveries.FirstOrDefault(d => d.Id == cache.Id);
        if (discovery != null && DiscoveryGuarded(discovery)) return new(false, "Living foes still guard these supplies.");
        if (IsWorkSupplyCache(cache))
        {
            int picked = 0;
            foreach (var (stack, count) in cache.Contents.ToArray())
            {
                int amount = Math.Min(count, Math.Max(0, 9999 - Count(stack)));
                Add(stack, amount); cache.Contents[stack] -= amount; picked += amount;
            }
            cache.Taken = cache.Contents.Values.All(v => v == 0);
            return new(picked > 0, picked > 0 ? "Recovered " + picked + " supplies." + (cache.Taken ? "" : " The remaining bundle stays here.") : "Make room in the pack before recovering this supply bundle.");
        }
        if (cache.Choices.Length > 0 && !cache.Choices.Contains(choice)) return new(false, "Choose one of the three offered pieces of equipment.");
        foreach (var (item, amount) in cache.Contents) Add(item, amount);
        if (cache.Choices.Length > 0) { var item = Item(choice); Add(item.Stack, 1); cache.Selected = choice; }
        cache.Taken = true;
        string recovered = "Recovered " + string.Join(", ", cache.Contents.Select(p => p.Value + " " + (Content.All<ItemDefinition>().FirstOrDefault(i => i.Stack == p.Key)?.Name ?? p.Key)))
            + (cache.Selected is { } selected ? "; chose " + Item(selected).Name : "") + ".";
        Log(recovered); return new(true, recovered);
    }
    private ActionResult Equip(long actorId, string itemId)
    {
        var actor = Party.FirstOrDefault(a => a.Id == actorId && a.Life == LifeState.Alive);
        if (actor == null || !TryItem(itemId, out var item) || item.Slot == "none" || Count(item.Stack) < 1)
            return new(false, "Choose carried equipment and a living companion.");
        if (actor.Equipment.TryGetValue(item.Slot, out var previous)) Add(Item(previous.Item).Stack, 1);
        Add(item.Stack, -1); actor.Equipment[item.Slot] = new(item.Id, item.Health, item.Power, item.Armor, item.Speed);
        actor.Health = Math.Min(actor.Health, actor.MaxHealth);
        return new(true, $"{actor.Name} equipped {item.Name}.");
    }
    public string EnemyIntent(int unitId)
    {
        if (EnemyActionPreview(unitId) is not { } intent) return "";
        if (intent.Reason.Length > 0) return intent.Name + ": " + intent.Reason;
        string target = intent.Target switch { TargetKind.AllEnemies => "party", TargetKind.AllAllies => "all foes", TargetKind.Self => "self", _ => intent.Targets.FirstOrDefault()?.Name.Split(' ')[0] ?? "fallen target" };
        return intent.Name + " > " + target;
    }
    private IEnumerable<BehaviorRule> EnemyRules(Actor actor, CombatUnit unit, int round)
    {
        IEnumerable<BehaviorRule> rules = [];
        if (Content.Entries[actor.Archetype] is BossDefinition boss)
            rules = unit.Health * 100 <= unit.MaxHealth * boss.PhasePercent ? boss.Enraged : boss.Behaviors;
        else if (Content.Entries[actor.Archetype] is CharacterDefinition character) rules = character.Behaviors;
        if (actor.VariantId.Length > 0) rules = rules.Concat(Content.Get<VariantDefinition>(actor.VariantId).Behaviors);
        return rules.Where(r => round >= r.FromRound && round % r.Every == 0 && unit.Health * 100 <= unit.MaxHealth * r.BelowHealthPercent
            && Content.Get<AbilityDefinition>(r.Ability).Mana <= unit.Mana && EncounterBehaviorAvailable(unit, r)
            && (!r.NoRepeat || State.Battle!.LastEnemyActions.GetValueOrDefault(unit.Id) != r.Ability));
    }
    private void PlanEnemies()
    {
        var b = State.Battle!; b.Plans.Clear();
        foreach (var enemy in b.Units.Where(u => !u.Friendly && u.Alive))
        {
            if (TryPreparedPlan(enemy, out var prepared))
            {
                b.Plans[enemy.Id] = prepared; enemy.IntentTarget = prepared.Target; enemy.Intent = IntentKind.Strike;
                continue;
            }
            var actor = State.Actors.Single(a => a.Id == b.ActorIds[enemy.Id]);
            var rules = EnemyRules(actor, enemy, b.Round).ToArray();
            var rule = new BehaviorRule { Ability = Content.Game.EnemyFallback };
            if (rules.Length > 0)
            {
                int pick = CombatRoll(rules.Sum(r => r.Weight));
                foreach (var candidate in rules) { pick -= candidate.Weight; if (pick < 0) { rule = candidate; break; } }
            }
            var ability = Content.Get<AbilityDefinition>(rule.Ability);
            var candidates = b.Units.Where(u => u.Alive && (ability.Target is TargetKind.Ally or TargetKind.AllAllies ? !u.Friendly : u.Friendly)).ToArray();
            if (rule.Condition == "ally-wounded" && ability.Target is TargetKind.Ally or TargetKind.AllAllies)
                candidates = candidates.Where(u => u.Health < u.MaxHealth).ToArray();
            if (rule.Condition == "self-wounded" && ability.Target is TargetKind.Ally or TargetKind.AllAllies)
                candidates = [enemy];
            if (rule.Condition == "target-marked" && ability.Target is TargetKind.Enemy or TargetKind.AllEnemies)
                candidates = candidates.Where(u => u.Mark > 0).ToArray();
            CombatUnit target = ability.Target == TargetKind.Self ? enemy : rule.Target switch
            {
                "weakest" => candidates.MinBy(u => (u.Health, u.Id))!,
                "strongest" => candidates.MaxBy(u => (u.Health, -u.Id))!,
                "stressed" => candidates.MaxBy(u => (u.Stress, -u.Id))!,
                _ => candidates[CombatRoll(candidates.Length)]
            };
            b.Plans[enemy.Id] = new(ability.Id, target.Id, Preparing: ability.PreparationTurns > 0); enemy.IntentTarget = target.Id;
            enemy.Intent = ability.Effect switch { "guard" or "rally" => IntentKind.Guard, "bleed" => IntentKind.Bleed, _ => IntentKind.Strike };
        }
    }
}
