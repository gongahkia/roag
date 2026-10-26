using Roag.Core.Content;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>Only a won battle can call this. Each persistent enemy/group identity pays once.</summary>
    private EncounterReward GrantEncounterRewards(WorldBattle battle)
    {
        var defeated = battle.Units.Where(u => !u.Friendly && !u.Alive).Select(u => State.Actors.Single(a => a.Id == battle.ActorIds[u.Id])).ToArray();
        var sourceIds = defeated.Select(a => a.EncounterLeader == 0 ? a.Id : a.EncounterLeader).Distinct().Order().ToArray();
        var granted = new List<long>(); var themes = new List<string>();
        var items = new Dictionary<string, int>(StringComparer.Ordinal);
        int gold = 0, food = 0, experience = 0;
        foreach (long id in sourceIds)
        {
            if (State.RewardedEncounterIds.Contains(id)) continue;
            var source = State.Actors.Single(a => a.Id == id);
            if (source.Life != LifeState.Dead) continue;
            // Defeating the final guardian finishes the expedition even with surviving attendants.
            if (Content.Entries[source.Archetype] is not BossDefinition && source.EncounterLeader > 0
                && State.Actors.Any(a => a.EncounterLeader == id && a.Life != LifeState.Dead)) continue;
            var reward = RewardFor(source);
            gold += reward.Gold; food += reward.Food; experience += reward.Experience;
            foreach (var (itemId, count) in reward.Supplies)
            {
                string stack = Content.Get<ItemDefinition>(itemId).Stack;
                if (stack == "gold") gold += count;
                else if (stack == "food") food += count;
                else items[itemId] = items.GetValueOrDefault(itemId) + count;
            }
            if (reward.Signature is { } signature) AddEquipment(signature, id, 0);
            var random = WorldRandom.Stream(Seed, unchecked((int)id), unchecked((int)(id >> 32)), 7201);
            if (reward.EquipmentPool.Length > 0 && random.Next(100) < reward.EquipmentChancePercent)
                AddEquipment(reward.EquipmentPool[random.Next(reward.EquipmentPool.Length)], id, 1);
            themes.Add(reward.Name); granted.Add(id);
        }
        // Inventory stacks have a persisted upper bound. Report what was actually granted.
        gold = Math.Min(gold, Math.Max(0, 9999 - Count("gold")));
        food = Math.Min(food, Math.Max(0, 9999 - Count("food")));
        Add("gold", gold); Add("food", food);
        foreach (var (itemId, count) in items.ToArray())
        {
            string stack = Item(itemId).Stack;
            int accepted = Math.Min(count, Math.Max(0, 9999 - Count(stack)));
            if (accepted == 0) items.Remove(itemId);
            else { items[itemId] = accepted; Add(stack, accepted); }
        }
        var levels = new List<string>();
        foreach (var actor in Party.Where(a => a.Life == LifeState.Alive))
        {
            int oldLevel = actor.Level; actor.Experience += experience;
            while (actor.Level < 100 && actor.Experience >= actor.Level * 30)
            {
                actor.Experience -= actor.Level * 30; actor.Level++;
                actor.Health = Math.Min(actor.MaxHealth, actor.Health + 6); actor.Mana = Math.Min(actor.MaxMana, actor.Mana + 3);
            }
            if (actor.Level == 100) actor.Experience = Math.Min(actor.Experience, actor.Level * 30);
            if (actor.Level > oldLevel) levels.Add(actor.Name + " / level " + actor.Level);
        }
        State.RewardedEncounterIds.UnionWith(granted);
        string theme = string.Join(" / ", themes.Distinct());
        if (theme.Length > 240) theme = theme[..237] + "...";
        return new(gold, food, experience, levels.ToArray())
        {
            Theme = theme,
            Items = items.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new EncounterRewardItem(p.Key, p.Value)).ToArray(),
            Sources = granted.ToArray()
        };

        void AddEquipment(string basis, long source, int choice)
        {
            string item = EncounterEquipment(basis, source, choice);
            items[item] = items.GetValueOrDefault(item) + 1;
        }
    }

    private RewardDefinition RewardFor(Actor source)
    {
        string id = source.EncounterId.Length > 0 ? Content.Get<EncounterDefinition>(source.EncounterId).Reward
            : Content.Entries[source.Archetype] switch
            {
                BossDefinition { Reward.Length: > 0 } boss => boss.Reward,
                CharacterDefinition { Reward.Length: > 0 } character => character.Reward,
                _ => Content.Game.DefaultReward
            };
        return Content.Get<RewardDefinition>(id);
    }

    private string EncounterEquipment(string itemId, long source, int choice)
    {
        var basis = Content.Get<ItemDefinition>(itemId);
        if (basis.AffixPool.Length == 0) return itemId;
        string id = $"gear.7200.{source}.{choice}";
        if (State.GeneratedEquipment.TryGetValue(id, out var previous))
        {
            if (previous.Base != itemId) throw new InvalidDataException("Encounter equipment identity conflicts with its source.");
            return id;
        }
        var random = WorldRandom.Stream(Seed, unchecked((int)source), unchecked((int)(source >> 32)), 7210 + choice);
        var pool = basis.AffixPool.Where(a => !basis.Effects.Contains(a)).Order(StringComparer.Ordinal).ToList();
        if (pool.Count == 0) return itemId;
        int count = Math.Min(pool.Count, 1 + random.Next(2)); var affixes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            int index = random.Next(pool.Count); affixes.Add(pool[index]); pool.RemoveAt(index);
        }
        State.GeneratedEquipment.Add(id, new(itemId, affixes.ToArray())); return id;
    }
}
