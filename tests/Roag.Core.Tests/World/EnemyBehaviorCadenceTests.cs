using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class EnemyBehaviorCadenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-cadence-" + Guid.NewGuid().ToString("N"));
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static ContentCatalog Rules(string json, string role = "attrition")
    {
        var catalog = ContentCatalog.Load(ContentCatalog.Default.Entries.Values.Select(definition =>
        {
            var node = JsonSerializer.SerializeToNode<ContentDefinition>(definition, ContentCatalog.Json)!;
            if (definition.Id == "character.cinder-channeler") node["behaviors"] = JsonNode.Parse(json);
            if (definition is EncounterDefinition encounter)
                for (int i = 0; i < encounter.Members.Length; i++)
                    if (encounter.Members[i] == "character.cinder-channeler") node["roles"]![i] = role;
            return (File: ContentCatalog.Default.Source(definition.Id), Text: node.ToJsonString());
        }));
        Assert.Empty(catalog.Issues); return catalog;
    }
    private static (WorldSession World, int Enemy) Fixture(ContentCatalog? catalog = null)
    {
        var world = WorldSession.Create(42, catalog); var fixture = EncounterGroupPreview.Prepare(world);
        return (world, world.State.Battle!.ActorIds.IndexOf(fixture.Leader));
    }
    private static void Health(WorldSession world, int id, int health)
    {
        var battle = world.State.Battle!; battle.Units[id].Health = health;
        world.State.Actors.Single(a => a.Id == battle.ActorIds[id]).Health = health;
    }

    [Fact]
    public void NonRepeatRemembersExecutedActionsAndPersistsTheNextAlternative()
    {
        var catalog = Rules("""[{"ability":"ability.hex","noRepeat":true}]""");
        var (world, enemy) = Fixture(catalog); var battle = world.State.Battle!;
        Assert.Empty(battle.LastEnemyActions); Assert.Equal("ability.hex", battle.Plans[enemy].Ability);
        Assert.True(world.Act(3).Success); Assert.True(world.AdvanceEnemy().Success);
        Assert.Equal("ability.hex", battle.LastEnemyActions[enemy]);
        world.Save(Slot); var loaded = WorldSession.Load(Slot, catalog);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        WorldEncounterGroupTests.GuardUntil(world, b => b.Round == 2 && b.Active.Id == enemy);
        WorldEncounterGroupTests.GuardUntil(loaded, b => b.Round == 2 && b.Active.Id == enemy);
        Assert.Equal(catalog.Game.EnemyFallback, battle.Plans[enemy].Ability);
        Assert.True(world.AdvanceEnemy().Success); Assert.True(loaded.AdvanceEnemy().Success);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        WorldEncounterGroupTests.GuardUntil(world, b => b.Round == 3 && b.Active.Id == enemy);
        Assert.Equal("ability.hex", battle.Plans[enemy].Ability);
    }

    [Fact]
    public void AStunnedUnexecutedIntentDoesNotConsumeItsNonRepeatOpportunity()
    {
        var catalog = Rules("""[{"ability":"ability.hex","noRepeat":true}]""");
        var (world, enemy) = Fixture(catalog); var battle = world.State.Battle!;
        world.Party.First().Role = 0;
        Assert.True(world.Act(1, enemy).Success);
        Assert.Contains(world.State.CombatEvents, e => e.Kind == "skip" && e.Label == "Stunned");
        Assert.False(battle.LastEnemyActions.ContainsKey(enemy));
        WorldEncounterGroupTests.GuardUntil(world, b => b.Round == 2 && b.Active.Id == enemy);
        Assert.Equal("ability.hex", battle.Plans[enemy].Ability);
        Assert.True(world.AdvanceEnemy().Success); Assert.Equal("ability.hex", battle.LastEnemyActions[enemy]);
    }

    [Fact]
    public void PreparationCountsAsAnActionButItsPendingReleaseBypassesNonRepeat()
    {
        var (world, enemy) = Fixture(); var battle = world.State.Battle!;
        Assert.True(world.Act(3).Success); Assert.True(world.AdvanceEnemy().Success);
        Assert.Equal("ability.cinder-surge", battle.LastEnemyActions[enemy]); Assert.True(battle.Prepared.ContainsKey(enemy));
        WorldEncounterGroupTests.GuardUntil(world, b => b.Round == 2 && b.Active.Id == enemy);
        Assert.Equal("ability.cinder-surge", battle.Plans[enemy].Ability); Assert.False(battle.Plans[enemy].Preparing);
        Assert.True(world.AdvanceEnemy().Success);
        Assert.Contains(world.State.CombatEvents, e => e.Kind == "damage" && e.Label == "Cinder surge");
        WorldEncounterGroupTests.GuardUntil(world, b => b.Round == 3 && b.Active.Id == enemy);
        Assert.Equal(world.Content.Game.EnemyFallback, battle.Plans[enemy].Ability);
    }

    [Fact]
    public void SelfWoundedHealingSelectsTheCasterRatherThanAWeakerAlly()
    {
        var catalog = Rules("""[{"ability":"ability.mend","condition":"self-wounded","target":"weakest"}]""", "support");
        var (world, enemy) = Fixture(catalog); var battle = world.State.Battle!;
        int ally = battle.Units.Single(u => !u.Friendly && u.Id != enemy).Id;
        Health(world, ally, 5);
        WorldEncounterGroupTests.GuardUntil(world, b => b.Round == 2 && b.Active.Id == enemy);
        Assert.Equal(catalog.Game.EnemyFallback, battle.Plans[enemy].Ability);
        Health(world, enemy, 100);
        Assert.Equal(catalog.Game.EnemyFallback, battle.Plans[enemy].Ability); // Already declared plans stay fixed.
        WorldEncounterGroupTests.GuardUntil(world, b => b.Round == 3 && b.Active.Id == enemy);
        Assert.Equal("ability.mend", battle.Plans[enemy].Ability); Assert.Equal(enemy, battle.Plans[enemy].Target);
        int mana = battle.Units[enemy].Mana;
        Assert.True(world.AdvanceEnemy().Success); Assert.True(battle.Units[enemy].Health > 100);
        Assert.Equal(mana - catalog.Get<AbilityDefinition>("ability.mend").Mana, battle.Units[enemy].Mana);
        Assert.Equal(5, battle.Units[ally].Health);
    }

    [Fact]
    public void MarkedTargetRuleSelectsMarkedOpponentAndDoesNotRetargetAfterMarkChanges()
    {
        var catalog = Rules("""[{"ability":"ability.arrow","condition":"target-marked","target":"weakest"}]""", "attacker");
        var (world, enemy) = Fixture(catalog); var battle = world.State.Battle!;
        Assert.Equal(catalog.Game.EnemyFallback, battle.Plans[enemy].Ability);
        // Keep the bowman's independent marking out of this fixed-target experiment.
        var bowman = battle.Units.Single(u => !u.Friendly && u.Id != enemy); bowman.Mana = 0;
        world.State.Actors.Single(a => a.Id == battle.ActorIds[bowman.Id]).Mana = 0;
        Health(world, 0, 15); battle.Units[3].Mark = 2;
        WorldEncounterGroupTests.GuardUntil(world, b => b.Round == 2 && b.Active.Id == enemy);
        Assert.Equal("ability.arrow", battle.Plans[enemy].Ability); Assert.Equal(3, battle.Plans[enemy].Target);
        battle.Units[3].Mark = 0; battle.Units[0].Mark = 3;
        long last = world.State.CombatEvents.Last().Sequence;
        Assert.True(world.AdvanceEnemy().Success);
        var damage = Assert.Single(world.State.CombatEvents, e => e.Sequence > last && e.Kind == "damage" && e.Source == battle.ActorIds[enemy]);
        Assert.Equal(battle.ActorIds[3], damage.Target);
    }

    [Theory]
    [InlineData("friendly")] [InlineData("unknown")] [InlineData("missing-preparation-history")]
    public void ForgedActionHistoryIsRejected(string fault)
    {
        var (world, enemy) = Fixture(); Assert.True(world.Act(3).Success); Assert.True(world.AdvanceEnemy().Success); world.Save(Slot);
        var json = JsonNode.Parse(File.ReadAllText(Slot))!; var history = json["state"]!["battle"]!["lastEnemyActions"]!.AsObject();
        if (fault == "friendly") history["0"] = "ability.strike";
        if (fault == "unknown") history[enemy.ToString()] = "ability.missing";
        if (fault == "missing-preparation-history") history.Clear();
        File.WriteAllText(Slot, json.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
}
