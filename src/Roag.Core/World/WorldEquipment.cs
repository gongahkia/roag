using System.Diagnostics.CodeAnalysis;
using Roag.Core.Content;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private readonly Dictionary<string, ItemDefinition> _resolvedEquipment = [];
    public bool TryItem(string id, [NotNullWhen(true)] out ItemDefinition? item)
    {
        if (Content.Entries.GetValueOrDefault(id) is ItemDefinition authored) { item = authored; return true; }
        if (_resolvedEquipment.TryGetValue(id, out item)) return true;
        if (!State.GeneratedEquipment.TryGetValue(id, out var generated)) { item = null; return false; }
        item = ResolveEquipment(Content, id, generated); _resolvedEquipment[id] = item; return true;
    }
    public ItemDefinition Item(string id) => TryItem(id, out var item) ? item : throw new InvalidDataException("Unknown equipment or item: " + id);
    private static ItemDefinition ResolveEquipment(ContentCatalog catalog, string id, GeneratedEquipment generated)
    {
        var basis = catalog.Get<ItemDefinition>(generated.Base);
        return new() { Id = id, Name = string.Join(" ", generated.Affixes.Select(a => catalog.Get<AffixDefinition>(a).Name).Append(basis.Name)),
            Description = basis.Description, Sprite = basis.Sprite, Tags = basis.Tags, Stack = id, Slot = basis.Slot,
            Health = basis.Health, Power = basis.Power, Armor = basis.Armor, Speed = basis.Speed, Value = basis.Value + generated.Affixes.Length * 8,
            WorkBonuses = new(basis.WorkBonuses), Effects = basis.Effects.Concat(generated.Affixes).ToArray() };
    }
    public IEnumerable<ItemDefinition> CarriedEquipment() => Content.All<ItemDefinition>().Where(i => i.Slot != "none" && (!i.Tags.Contains("signature") || Count(i.Stack) > 0 || Party.Any(a => a.Equipment.Values.Any(e => e.Item == i.Id))))
        .Concat(State.GeneratedEquipment.Keys.Where(id => Count(id) > 0 || Party.Any(a => a.Equipment.Values.Any(e => e.Item == id))).Order(StringComparer.Ordinal).Select(Item));
    public string EquipmentDescription(ItemDefinition item) => string.Join(" ", new[] { item.Description }.Concat(item.Effects.Select(id => Content.Get<AffixDefinition>(id).Description)).Where(s => s.Length > 0));
    public string EquipmentEffectNames(ItemDefinition item) => string.Join(", ", item.Effects.Select(id => Content.Get<AffixDefinition>(id).Name));
    public string EquipmentSummary(ItemDefinition item) => string.Join("; ", item.Effects.Select(Content.Get<AffixDefinition>).Select(e => e.Effect switch
    {
        "protect" => "Ally cover " + e.Power, "burn-power" => "Burn power +" + e.Power, "healing" => "Healing +" + e.Power,
        "resolve" => "Dread stress -" + e.Power, _ => e.Effect + " +" + e.Power + "%"
    }));
    public int EquipmentEffectPower(Actor actor, string effect) => Math.Min(effect is "dig" or "mine" or "fish" ? 200 : 12,
        actor.Equipment.Values.SelectMany(e => Item(e.Item).Effects).Select(Content.Get<AffixDefinition>).Where(e => e.Effect == effect).Sum(e => e.Power));
    private int AbilityEquipmentBonus(Actor actor, Skill skill) => skill.Effect == "burn" ? EquipmentEffectPower(actor, "burn-power")
        : skill.Effect is "heal" or "potion" or "cleanse" ? EquipmentEffectPower(actor, "healing") : 0;
    private CombatUnit? ShieldProtector(CombatUnit target) => target.Health * 2 > target.MaxHealth || State.Battle == null ? null
        : State.Battle.Units.Where(u => u.Id != target.Id && u.Friendly == target.Friendly && u.Alive && !State.Battle.ShieldUsed.Contains(u.Id) && ProtectionPower(CombatActor(u)) > 0)
            .OrderByDescending(u => ProtectionPower(CombatActor(u))).ThenBy(u => u.Id).FirstOrDefault();
    private string GenerateEquipment(string itemId, int cacheId, int salt, int choice)
    {
        var basis = Content.Get<ItemDefinition>(itemId);
        if (basis.AffixPool.Length == 0) return itemId;
        string id = $"gear.{salt}.{cacheId}.{choice}";
        var rng = WorldRandom.Stream(Seed, cacheId, choice, salt + 91);
        var pool = basis.AffixPool.Where(a => !basis.Effects.Contains(a)).Order(StringComparer.Ordinal).ToList();
        int count = Math.Min(pool.Count, 1 + rng.Next(2)); var selected = new List<string>();
        for (int i = 0; i < count; i++) { int index = rng.Next(pool.Count); selected.Add(pool[index]); pool.RemoveAt(index); }
        State.GeneratedEquipment.Add(id, new(itemId, selected.ToArray())); return id;
    }
}
