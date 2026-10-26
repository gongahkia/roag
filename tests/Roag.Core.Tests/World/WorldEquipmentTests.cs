using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WorldEquipmentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-equipment-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    [Fact]
    public void SeededEquipmentCombinesBoundedAuthoredEffectsWithoutConsumingCombatRandomness()
    {
        var a = WorldSession.Create(42); var b = WorldSession.Create(42); var c = WorldSession.Create(1337);
        Assert.Equal(JsonSerializer.Serialize(a.State.GeneratedEquipment), JsonSerializer.Serialize(b.State.GeneratedEquipment));
        Assert.NotEqual(JsonSerializer.Serialize(a.State.GeneratedEquipment), JsonSerializer.Serialize(c.State.GeneratedEquipment));
        Assert.True(a.State.GeneratedEquipment.Count > 300);
        Assert.True(a.State.GeneratedEquipment.Values.Select(g => g.Base + ":" + string.Join(",", g.Affixes)).Distinct().Count() > 20);
        uint rng = a.State.CombatRandom; string before = JsonSerializer.Serialize(a.State);
        foreach (var (id, generated) in a.State.GeneratedEquipment)
        {
            Assert.InRange(generated.Affixes.Length, 1, 2); Assert.Equal(generated.Affixes.Length, generated.Affixes.Distinct().Count());
            var item = a.Item(id); Assert.NotEmpty(a.EquipmentDescription(item));
            Assert.All(generated.Affixes, effect => Assert.Contains(effect, a.Content.Get<ItemDefinition>(generated.Base).AffixPool));
        }
        Assert.Equal(rng, a.State.CombatRandom); Assert.Equal(before, JsonSerializer.Serialize(a.State));
    }
    [Fact]
    public void DungeonCachesUseTheirGuardiansThemeAndReserveSignatureGearForVictory()
    {
        var s = WorldSession.Create(42);
        foreach (var site in s.Plan.Sites)
        {
            var guardian = s.State.Actors.Single(a => a.Id == 40000 + site.Id * 8);
            var boss = s.Content.Get<BossDefinition>(guardian.Archetype);
            var reward = s.Content.Get<RewardDefinition>(boss.Reward);
            foreach (var cache in s.State.Loot.Where(c => c.Id.StartsWith($"site-{site.Id}-", StringComparison.Ordinal)))
            foreach (var choice in cache.Choices)
            {
                string basis = s.State.GeneratedEquipment.TryGetValue(choice, out var generated) ? generated.Base : choice;
                Assert.Contains(basis, reward.EquipmentPool);
                Assert.DoesNotContain("signature", s.Item(choice).Tags);
            }
        }
    }
    [Fact]
    public void ChosenVariantKeepsIdentityThroughEquipmentStorageAndSaveWithoutRerollingOrDuplication()
    {
        var s = WorldSession.Create(42); var d = s.State.Discoveries.First(d => d.Enemies.Length == 0 && s.State.Loot.Any(c => c.Id == d.Id && c.Choices.Length == 3));
        var cache = s.State.Loot.Single(c => c.Id == d.Id); var hero = s.Party.First(); var item = s.Item(cache.Choices[0]);
        s.State.Position = hero.Position = d.Position.Offset(0, 1); s.Chunks.Edit(hero.Position, Material.Floor); s.Reveal();
        Assert.True(s.Submit(new(WorldCommandKind.Loot, d.Position, Item: item.Id)).Success);
        Assert.True(s.Submit(new(WorldCommandKind.Equip, ActorId: hero.Id, Item: item.Id)).Success); Assert.Equal(0, s.Count(item.Stack));
        s.Save(Slot); var loaded = WorldSession.Load(Slot); Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        Assert.Equal(item.Effects, loaded.Item(item.Id).Effects); Assert.Equal(item.Name, loaded.Item(item.Id).Name);
        Assert.True(loaded.Submit(new(WorldCommandKind.Unequip, ActorId: hero.Id, Item: item.Slot)).Success);
        var storage = loaded.State.Position.Offset(1, 0); loaded.Chunks.Edit(storage, Material.Storage);
        Assert.True(loaded.Submit(new(WorldCommandKind.Store, storage, Item: item.Stack)).Success); Assert.Equal(0, loaded.Count(item.Stack));
        loaded.Save(Slot); loaded = WorldSession.Load(Slot);
        Assert.True(loaded.Submit(new(WorldCommandKind.Take, storage, Item: item.Stack)).Success); Assert.Equal(1, loaded.Count(item.Stack));
        Assert.False(loaded.Submit(new(WorldCommandKind.Loot, d.Position, Item: item.Id)).Success); Assert.Equal(1, loaded.Count(item.Stack));
        loaded.State.Inventory[item.Stack] = 2; loaded.Save(Slot); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
    [Fact]
    public void StaffEffectAddsActualBurnDamageAndItsPreviewMatches()
    {
        ContentCatalog WithoutEffect() => ContentCatalog.Load(ContentCatalog.Default.Entries.Values.Select(d =>
        {
            var node = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!;
            if (d.Id == "item.ember-staff") node["effects"] = new JsonArray();
            return (File: ContentCatalog.Default.Source(d.Id), Text: node.ToJsonString());
        }));
        int Attack(ContentCatalog content)
        {
            var s = WorldSession.Create(42, content); var hero = s.Party.First(); hero.Role = 2; hero.BaseSpeed = 30; hero.Health = hero.BaseHealth = 1000;
            s.State.Inventory["item.ember-staff"] = 1; Assert.True(s.Submit(new(WorldCommandKind.Equip, ActorId: hero.Id, Item: "item.ember-staff")).Success);
            var foe = s.State.Actors.First(a => a.Kind == ActorKind.Beast); foe.Health = foe.BaseHealth = 1000; s.StartBattle(foe.Id);
            int index = s.ActiveSkills().Select((skill, i) => (skill, i)).First(p => p.skill.Effect == "burn").i;
            var target = s.State.Battle!.Units.First(u => !u.Friendly); var preview = s.ActionPreview(index, target.Id);
            Assert.True(s.Act(index, target.Id).Success);
            int damage = s.State.CombatEvents.First(e => e.Kind == "damage").Amount; Assert.InRange(damage, preview.Minimum, preview.Maximum); return damage;
        }
        Assert.Equal(Attack(WithoutEffect()) + 3, Attack(ContentCatalog.Default));
    }
    [Fact]
    public void ShieldProtectsAnotherWoundedAllyOncePerRoundAndSavesItsUsage()
    {
        var s = WorldSession.Create(42); CampPreview.Prepare(s); var guardian = s.Party.First();
        foreach (var a in s.Party) { a.BaseHealth = 300; a.Health = a.MaxHealth; a.Body.Wounds = 0; }
        var injured = s.Party.ElementAt(1); injured.Health = 100;
        s.State.Inventory["item.wardens-shield"] = 1; Assert.True(s.Submit(new(WorldCommandKind.Equip, ActorId: guardian.Id, Item: "item.wardens-shield")).Success);
        var foes = s.State.Actors.Where(a => a.Kind == ActorKind.Beast && a.EncounterLeader == 0).Take(2).ToArray();
        foes[1].Position = foes[0].Position.Offset(1, 0); foreach (var foe in foes) { foe.BasePower = 20; foe.BaseHealth = foe.Health = 1000; }
        s.StartBattle(foes[0].Id); var battle = s.State.Battle!;
        battle.Order = battle.Units.OrderBy(u => u.Friendly ? 1 : 0).ThenBy(u => u.Id).Select(u => u.Id).ToList(); battle.TurnIndex = 0;
        foreach (var foe in battle.Units.Where(u => !u.Friendly)) battle.Plans[foe.Id] = new("ability.strike", 1);
        Assert.True(s.AdvanceEnemy().Success); Assert.Contains(0, battle.ShieldUsed);
        Assert.Equal(3, s.State.CombatEvents.Single(e => e.Label == "Ally cover").Amount);
        s.Save(Slot); var loaded = WorldSession.Load(Slot);
        Assert.True(s.AdvanceEnemy().Success); Assert.True(loaded.AdvanceEnemy().Success);
        Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        Assert.Single(s.State.CombatEvents, e => e.Label == "Ally cover");
        int round = battle.Round;
        for (int t = 0; t < 20 && battle.Round == round; t++) { if (battle.Active.Friendly) s.Act(3); else s.AdvanceEnemy(); }
        Assert.True(battle.Round > round); Assert.Empty(battle.ShieldUsed);
    }
    [Fact]
    public void GeneratedToolEffectsAugmentTheAuthoredToolWithoutRequiringIt()
    {
        var s = WorldSession.Create(42); var p = ResourcePreview.Prepare(s, "resource.iron-deposit"); var hero = s.Party.First();
        double weather = s.WorkMultiplier(hero, new() { Kind = WorkKind.Dig, Target = p });
        var entry = s.State.GeneratedEquipment.First(g => g.Value.Base == "item.pickaxe" && g.Value.Affixes.Contains("affix.delver"));
        s.State.Inventory[entry.Key] = 1; Assert.True(s.Submit(new(WorldCommandKind.Equip, ActorId: hero.Id, Item: entry.Key)).Success);
        Assert.Equal(1.85 * weather, s.WorkMultiplier(hero, new() { Kind = WorkKind.Dig, Target = p }), 10);
        Assert.True(s.DesignateWork(WorldCommandKind.Dig, [p]).Queued > 0); s.AdvanceTicks(1); var order = s.State.Work.Single();
        Assert.Equal(s.WorkRate(hero) * 1.85 * weather, order.Progress, 6);
        Assert.True(s.Submit(new(WorldCommandKind.Unequip, ActorId: hero.Id, Item: "tool")).Success);
        Assert.Equal(weather, s.WorkMultiplier(hero, order));
    }
    [Theory]
    [InlineData("affix.mending", "heal", 3)] [InlineData("affix.steadfast", "dread", 4)]
    public void HealingAndResolveEffectsChangeRealCombatOutcomes(string affix, string effect, int difference)
    {
        int Outcome(bool modified)
        {
            var s = WorldSession.Create(42); var hero = s.Party.First(); hero.Role = 3; hero.BaseSpeed = 30;
            string id = "item.heartstone";
            if (modified) { id = "gear.999.0.0"; s.State.GeneratedEquipment.Add(id, new("item.heartstone", [affix])); }
            s.State.Inventory[s.Item(id).Stack] = 1; Assert.True(s.Submit(new(WorldCommandKind.Equip, ActorId: hero.Id, Item: id)).Success);
            hero.Health = 10; hero.Stress = 20;
            var foe = s.State.Actors.First(a => a.Kind == ActorKind.Beast); foe.BasePower = 1; s.StartBattle(foe.Id);
            var b = s.State.Battle!; var enemy = b.Units.First(u => !u.Friendly);
            if (effect == "heal")
            {
                int index = s.ActiveSkills().Select((skill, i) => (skill, i)).First(p => p.skill.Effect == "heal").i;
                var preview = s.ActionPreview(index, 0); Assert.True(s.Act(index, 0).Success);
                int amount = s.State.CombatEvents.First(e => e.Kind == "heal").Amount; Assert.Equal(preview.Minimum, amount); return amount;
            }
            b.Order = [enemy.Id, 0]; b.TurnIndex = 0; b.Plans[enemy.Id] = new("ability.dread", 0);
            Assert.True(s.AdvanceEnemy().Success); return 100 - hero.Stress;
        }
        Assert.Equal(Outcome(false) + difference, Outcome(true));
    }
    [Theory]
    [InlineData("item.pickaxe", "affix.prospector", "resource.iron-deposit", 1.85)]
    [InlineData("item.fishing-rod", "affix.angler", "resource.fishing-bank", 2.0)]
    public void ResourceWorkEffectsApplyToActualProgress(string basis, string affix, string resource, double multiplier)
    {
        var s = WorldSession.Create(42); var p = ResourcePreview.Prepare(s, resource); var hero = s.Party.First();
        double weather = s.WorkMultiplier(hero, new() { Kind = WorkKind.Harvest, Definition = resource, Target = p });
        const string id = "gear.999.0.0"; s.State.GeneratedEquipment.Add(id, new(basis, [affix])); s.State.Inventory[id] = 1;
        Assert.True(s.Submit(new(WorldCommandKind.Equip, ActorId: hero.Id, Item: id)).Success);
        Assert.Equal(1, s.DesignateWork(WorldCommandKind.Harvest, [p]).Queued); s.AdvanceTicks(1);
        Assert.Equal(s.WorkRate(hero) * multiplier * weather, s.State.Work.Single().Progress, 6);
    }
    [Fact]
    public void InvalidAffixesAndForgedEffectsAreRejected()
    {
        var s = WorldSession.Create(42); s.Save(Slot); var node = JsonNode.Parse(File.ReadAllText(Slot))!;
        var first = node["state"]!["generatedEquipment"]!.AsObject().First(); first.Value!["affixes"] = new JsonArray("affix.unknown");
        File.WriteAllText(Slot, node.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
        var catalog = ContentCatalog.Load(s.Content.Entries.Values.Select(d =>
        {
            var n = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!;
            if (d.Id == "affix.sentinel") n["effect"] = "unregistered";
            if (d.Id == "item.ember-staff") n["effects"] = new JsonArray("affix.sentinel");
            return (File: d.Id, Text: n.ToJsonString());
        }));
        Assert.Contains(catalog.Issues, i => i.Field == "effect"); Assert.Contains(catalog.Issues, i => i.Field == "effects");
        Assert.Contains(s.Content.References, r => r.Source == "item.wardens-shield" && r.Target == "affix.sentinel");
    }
}
