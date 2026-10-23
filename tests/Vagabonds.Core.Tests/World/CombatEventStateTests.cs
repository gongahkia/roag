using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class CombatEventStateTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-event-state-" + Guid.NewGuid().ToString("N"));
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static WorldSession Fight(int role = 2, ContentCatalog? content = null)
    {
        var world = WorldSession.Create(42, content); var hero = world.Party.First();
        hero.Role = role; hero.BaseSpeed = 30; hero.BaseHealth = hero.Health = 1000; hero.Mana = hero.MaxMana;
        var enemy = world.State.Actors.First(a => a.Kind == ActorKind.Beast);
        enemy.BaseSpeed = 1; enemy.BaseHealth = enemy.Health = 1000;
        enemy.Position = world.State.Position.Offset(8, 0); world.Chunks.Edit(enemy.Position, Material.Floor);
        world.StartBattle(enemy.Id); Assert.True(world.State.Battle!.Active.Friendly); return world;
    }
    [Fact]
    public void DamageAndStatusEventsCaptureTheirOwnMomentAndDoNotChangeWithTheUnit()
    {
        var world = Fight(); var b = world.State.Battle!; var enemy = b.Units.First(u => !u.Friendly);
        var ability = world.ActiveSkills()[1]; Assert.Equal("burn", ability.Effect);
        Assert.True(world.Act(1, enemy.Id).Success);
        var action = Assert.Single(world.State.CombatEvents, e => e.Kind == "ability");
        Assert.Equal(ability.Id, action.AbilityId); Assert.Null(action.TargetState);
        var hit = Assert.Single(world.State.CombatEvents, e => e.Kind == "damage" && e.Label == ability.Name);
        var applied = Assert.Single(world.State.CombatEvents, e => e.Kind == "status" && e.Label.StartsWith("Burn ", StringComparison.Ordinal));
        Assert.Equal(1000 - hit.Amount, hit.TargetState!.Health); Assert.Equal(0, hit.TargetState.Burn);
        Assert.Equal(ability.Duration, applied.TargetState!.Burn); Assert.Equal(hit.TargetState.Health, applied.TargetState.Health);
        Assert.True(applied.Sequence > hit.Sequence); Assert.Equal(ability.Duration - 1, enemy.Burn);
        enemy.Health = 1; enemy.Burn = 0;
        Assert.Equal(1000 - hit.Amount, hit.TargetState.Health); Assert.Equal(ability.Duration, applied.TargetState.Burn);
    }
    [Fact]
    public void HealingAndGuardEventsIncludeThePaidManaAndActualTargetState()
    {
        var healer = Fight(3); var b = healer.State.Battle!; var unit = b.Active;
        unit.Health = 20; healer.Party.First().Health = 20; int mana = unit.Mana;
        var skill = healer.ActiveSkills()[1]; Assert.Equal("heal", skill.Effect);
        Assert.True(healer.Act(1, unit.Id).Success);
        var heal = Assert.Single(healer.State.CombatEvents, e => e.Kind == "heal");
        Assert.Equal(20 + heal.Amount, heal.TargetState!.Health); Assert.Equal(mana - skill.Mana, heal.TargetState.Mana);
        Assert.Equal(unit.Stress, heal.TargetState.Stress); Assert.Equal(unit.Guarding, heal.TargetState.Guarding);

        var guard = Fight(); var guarded = guard.State.Battle!.Active;
        Assert.True(guard.Act(3).Success);
        var cover = Assert.Single(guard.State.CombatEvents, e => e.Label == "Guard" && e.Kind == "status");
        Assert.True(cover.TargetState!.Guarding); Assert.Equal(guarded.Health, cover.TargetState.Health);
        var enemy = guard.State.Battle!.Active; Assert.False(enemy.Friendly);
        string planned = guard.State.Battle.Plans[enemy.Id].Ability;
        Assert.True(guard.AdvanceEnemy().Success);
        Assert.Contains(guard.State.CombatEvents, e => e.Kind == "ability" && e.AbilityId == planned);
    }
    [Fact]
    public void OldCompatibleEventsOmittingOptionalFieldsLoadAndContinueExactly()
    {
        var world = Fight(); Assert.True(world.Act(3).Success);
        world.State.CombatEvents = world.State.CombatEvents.Select(e => e with { TargetState = null, AbilityId = "" }).ToList();
        world.Save(Slot); var json = JsonNode.Parse(File.ReadAllText(Slot))!;
        foreach (var entry in json["state"]!["combatEvents"]!.AsArray())
        { entry!.AsObject().Remove("targetState"); entry.AsObject().Remove("abilityId"); }
        File.WriteAllText(Slot, json.ToJsonString());
        var loaded = WorldSession.Load(Slot);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        Assert.All(loaded.State.CombatEvents, e => { Assert.Null(e.TargetState); Assert.Empty(e.AbilityId); });
        Assert.True(world.AdvanceEnemy().Success); Assert.True(loaded.AdvanceEnemy().Success);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        Assert.Contains(loaded.State.CombatEvents, e => e.TargetState != null);
    }
    [Fact]
    public void NewStructuredEventsSurviveAnExactSaveRoundTrip()
    {
        var world = Fight(); Assert.True(world.Act(1, world.State.Battle!.Units.First(u => !u.Friendly).Id).Success);
        world.Save(Slot); var loaded = WorldSession.Load(Slot);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        Assert.Equal(ContentCatalog.Default.Hash, loaded.Content.Hash);
        Assert.Contains(loaded.State.CombatEvents, e => e.AbilityId == "ability.ember");
    }
    [Theory]
    [InlineData("health", -1)] [InlineData("mana", 10001)] [InlineData("stress", 101)]
    [InlineData("burn", -1)] [InlineData("bleed", 1001)] [InlineData("mark", -1)] [InlineData("stun", 1001)]
    public void MalformedTargetSnapshotsAreRejected(string field, int value)
    {
        var world = Fight(); Assert.True(world.Act(3).Success); world.Save(Slot);
        var json = JsonNode.Parse(File.ReadAllText(Slot))!;
        var snapshot = json["state"]!["combatEvents"]!.AsArray().First(e => e!["targetState"] != null)!["targetState"]!;
        snapshot[field] = value; File.WriteAllText(Slot, json.ToJsonString());
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
    [Theory]
    [InlineData("ability.unknown")] [InlineData("item.ember-staff")] [InlineData(null)]
    public void UnknownWrongKindAndNullAbilityIdentitiesAreRejected(string? abilityId)
    {
        var world = Fight(); Assert.True(world.Act(3).Success); world.Save(Slot);
        var json = JsonNode.Parse(File.ReadAllText(Slot))!;
        json["state"]!["combatEvents"]!.AsArray().First(e => e!["kind"]!.GetValue<string>() == "ability")!["abilityId"] = abilityId;
        File.WriteAllText(Slot, json.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
    [Fact]
    public void CustomAuthoredAbilityIdentitiesValidateAgainstTheSelectedCatalog()
    {
        var custom = JsonSerializer.SerializeToNode<ContentDefinition>(ContentCatalog.Default.Get<AbilityDefinition>("ability.strike"), ContentCatalog.Json)!;
        custom["id"] = "ability.preview-strike";
        var catalog = ContentCatalog.Load(ContentCatalog.Default.Entries.Values.Select(d =>
            (File: ContentCatalog.Default.Source(d.Id), Text: JsonSerializer.Serialize<ContentDefinition>(d, ContentCatalog.Json)))
            .Append((File: "ability/preview-strike.json", Text: custom.ToJsonString())));
        Assert.Empty(catalog.Issues); var world = Fight(content: catalog); Assert.True(world.Act(3).Success);
        world.State.Battle!.Plans[world.State.Battle.Active.Id] = new("ability.preview-strike", 0);
        Assert.True(world.AdvanceEnemy().Success); world.Save(Slot);
        var loaded = WorldSession.Load(Slot, catalog);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        Assert.Contains(loaded.State.CombatEvents, e => e.AbilityId == "ability.preview-strike");
    }
}
