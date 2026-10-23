using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class WorldEncounterGroupTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-groups-" + Guid.NewGuid().ToString("N"));
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

    internal static ContentCatalog Changed(string id, Action<JsonObject> change)
    {
        var catalog = ContentCatalog.Load(ContentCatalog.Default.Entries.Values.Select(d =>
        {
            var node = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!.AsObject();
            if (d.Id == id) change(node);
            return (File: ContentCatalog.Default.Source(d.Id), Text: node.ToJsonString());
        }));
        Assert.Empty(catalog.Issues); return catalog;
    }
    internal static void GuardUntil(WorldSession world, Func<WorldBattle, bool> condition)
    {
        for (int i = 0; i < 80; i++)
        {
            var battle = world.State.Battle!; Assert.NotNull(battle);
            if (condition(battle)) return;
            Assert.True((battle.Active.Friendly ? world.Act(3) : world.AdvanceEnemy()).Success);
        }
        Assert.Fail("Combat did not reach the expected scheduled turn.");
    }
    private static (WorldSession World, int Charger) Prepared(ContentCatalog? catalog = null)
    {
        var world = WorldSession.Create(42, catalog); var fixture = EncounterGroupPreview.Prepare(world);
        var battle = world.State.Battle!; int charger = battle.ActorIds.IndexOf(fixture.Leader);
        Assert.True(world.Act(3).Success); Assert.Equal(charger, battle.Active.Id);
        Assert.True(world.AdvanceEnemy().Success); return (world, charger);
    }

    [Fact]
    public void AuthoredSiteGroupsAreReproducibleVariedAndHaveFixedDanger()
    {
        var world = WorldSession.Create(42); var again = WorldSession.Create(42);
        Assert.Empty(world.State.EncounterPopulations);
        var plans = world.Plan.Sites.SelectMany(s => world.EncounterPopulationPlan(s.Id)).ToArray();
        Assert.Equal(world.Plan.Sites.Count * 3, plans.Length);
        Assert.True(plans.Select(p => p.EncounterId).Distinct().Count() >= 6);
        Assert.Equal(new[] { 1, 2, 3 }, plans.Select(p => p.Floor).Distinct().Order());
        foreach (var plan in plans.Take(6))
        { world.EnsureEncounterPopulation(plan.SiteId, plan.Floor); again.EnsureEncounterPopulation(plan.SiteId, plan.Floor); }
        var groups = world.State.Actors.Where(a => a.Id is >= 70000 and < 80000).GroupBy(a => a.EncounterLeader).ToArray();
        Assert.Equal(6, groups.Length);
        foreach (var group in groups)
        {
            var definition = world.Content.Get<EncounterDefinition>(group.First().EncounterId);
            Assert.Equal(definition.Members, group.OrderBy(a => a.Id).Select(a => a.Archetype));
            foreach (var actor in group)
            {
                Assert.Equal(world.Content.Get<CharacterDefinition>(actor.Archetype).Health, actor.MaxHealth);
                var same = again.State.Actors.Single(a => a.Id == actor.Id);
                Assert.Equal(actor.Position, same.Position); Assert.Equal(actor.Archetype, same.Archetype);
            }
        }
        world.Party.First().Level = 40;
        Assert.All(groups.SelectMany(g => g), a => Assert.Equal(world.Content.Get<CharacterDefinition>(a.Archetype).Health, a.MaxHealth));
        Assert.Contains(world.State.Discoveries, d => world.Content.Get<DiscoveryDefinition>(d.Definition).Encounter.Length > 0);
    }

    [Fact]
    public void PreparationCostsNoManaOrDamageAndReleaseSurvivesExactReload()
    {
        var (world, charger) = Prepared(); var battle = world.State.Battle!; var enemy = battle.Units[charger];
        Assert.Equal(enemy.MaxMana, enemy.Mana);
        Assert.Contains(world.State.CombatEvents, e => e.Kind == "prepare" && e.AbilityId == "ability.cinder-surge");
        Assert.DoesNotContain(world.State.CombatEvents, e => e.Kind == "damage");
        Assert.True(battle.Prepared.ContainsKey(charger));
        world.Save(Slot); var loaded = WorldSession.Load(Slot);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        GuardUntil(world, b => b.Round == 2 && b.Active.Id == charger);
        GuardUntil(loaded, b => b.Round == 2 && b.Active.Id == charger);
        Assert.False(battle.Plans[charger].Preparing); int mana = enemy.Mana;
        Assert.True(world.AdvanceEnemy().Success); Assert.True(loaded.AdvanceEnemy().Success);
        Assert.Equal(mana - 4, enemy.Mana); Assert.False(battle.Prepared.ContainsKey(charger));
        Assert.Equal(4, world.State.CombatEvents.Count(e => e.Kind == "damage" && e.Label == "Cinder surge"));
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
    }

    [Fact]
    public void StunCancelsPreparationEvenWhenAnotherEnemyCleansesItBeforeRelease()
    {
        var catalog = Changed("ability.purify", node => node["target"] = "Ally");
        var (world, charger) = Prepared(catalog); var battle = world.State.Battle!;
        Assert.Equal(0, world.Party.Single(a => a.Id == battle.ActorIds[battle.Active.Id]).Role);
        Assert.True(world.Act(1, charger).Success);
        Assert.True(battle.Plans[charger].Interrupted); Assert.False(battle.Prepared.ContainsKey(charger));
        int mana = battle.Units[charger].Mana;
        GuardUntil(world, b => !b.Active.Friendly);
        battle.Plans[battle.Active.Id] = new("ability.purify", charger);
        Assert.True(world.AdvanceEnemy().Success);
        Assert.Equal(0, battle.Units[charger].Stun);
        Assert.Contains(world.State.CombatEvents, e => e.Label == "Stun removed");
        Assert.False(battle.Plans[charger].Preparing); Assert.Equal(world.Content.Game.EnemyFallback, battle.Plans[charger].Ability);
        Assert.False(battle.Prepared.ContainsKey(charger));
        world.Save(Slot); var loaded = WorldSession.Load(Slot, catalog);
        GuardUntil(loaded, b => b.Active.Id == charger);
        Assert.True(loaded.AdvanceEnemy().Success);
        Assert.Equal(mana, loaded.State.Battle!.Units[charger].Mana);
        Assert.DoesNotContain(loaded.State.CombatEvents, e => e.Kind == "damage" && e.Label == "Cinder surge");
    }

    [Fact]
    public void AnUnaffordablePreparedReleaseIsSpentWithoutNegativeManaOrDamage()
    {
        var (world, charger) = Prepared(); var battle = world.State.Battle!;
        battle.Units[charger].Mana = 0; world.State.Actors.Single(a => a.Id == battle.ActorIds[charger]).Mana = 0;
        GuardUntil(world, b => b.Round == 2 && b.Active.Id == charger);
        var result = world.AdvanceEnemy(); Assert.True(result.Success); Assert.Contains("not enough mana", result.Message);
        Assert.Equal(0, battle.Units[charger].Mana);
        Assert.False(battle.Prepared.ContainsKey(charger));
        Assert.Contains(world.State.CombatEvents, e => e.Kind == "skip" && e.Label == "Not enough mana");
        Assert.DoesNotContain(world.State.CombatEvents, e => e.Kind == "damage" && e.Label == "Cinder surge");
    }

    [Fact]
    public void PreparedSingleTargetAttacksKeepAFallenTargetAndDoNotRetarget()
    {
        var catalog = Changed("ability.cinder-surge", node => node["target"] = "Enemy");
        var (world, charger) = Prepared(catalog); var battle = world.State.Battle!;
        int target = battle.Prepared[charger].Target;
        battle.Units[target].Health = 0;
        var actor = world.State.Actors.Single(a => a.Id == battle.ActorIds[target]); actor.Health = 0; actor.Life = LifeState.Downed;
        // If the fallen character was about to act, skip their fixture turn as normal scheduling would.
        if (battle.Active.Id == target) battle.TurnIndex++;
        GuardUntil(world, b => b.Round == 2 && b.Active.Id == charger);
        Assert.Equal(target, battle.Plans[charger].Target);
        Assert.True(world.AdvanceEnemy().Success); Assert.Equal(8, battle.Units[charger].Mana);
        Assert.DoesNotContain(world.State.CombatEvents, e => e.Kind == "damage" && e.Label == "Cinder surge");
    }

    [Fact]
    public void SupportHealsOnlyWhenAWoundedAllyExistsAndUsesFiniteMana()
    {
        var catalog = Changed("character.vow-mender", node => node["behaviors"]!.AsArray().RemoveAt(1));
        var world = WorldSession.Create(42, catalog); var fixture = EncounterGroupPreview.Prepare(world, "encounter.road-wardens");
        var battle = world.State.Battle!; int mender = battle.ActorIds.IndexOf(fixture.Leader);
        Assert.Equal("ability.strike", battle.Plans[mender].Ability);
        battle.Units[mender].MaxHealth = battle.Units[mender].Health = 40;
        var healer = world.State.Actors.Single(a => a.Id == fixture.Leader); healer.BaseHealth = healer.Health = 40;
        var bruiser = battle.Units.Single(u => !u.Friendly && u.Id != mender); bruiser.Health = 90;
        world.State.Actors.Single(a => a.Id == battle.ActorIds[bruiser.Id]).Health = 90;
        GuardUntil(world, b => b.Round == 2 && b.Active.Id == mender);
        Assert.Equal("ability.mend", battle.Plans[mender].Ability); Assert.Equal(bruiser.Id, battle.Plans[mender].Target);
        int mana = battle.Units[mender].Mana, health = bruiser.Health;
        Assert.True(world.AdvanceEnemy().Success); Assert.True(bruiser.Health > health);
        Assert.Equal(mana - catalog.Get<AbilityDefinition>("ability.mend").Mana, battle.Units[mender].Mana);
    }

    [Theory]
    [InlineData("readyRound", 99)] [InlineData("target", 999)]
    public void MalformedPreparedContinuationIsRejected(string field, int value)
    {
        var (world, _) = Prepared(); world.Save(Slot);
        var json = JsonNode.Parse(File.ReadAllText(Slot))!;
        json["state"]!["battle"]!["prepared"]!.AsObject().First().Value![field] = value;
        File.WriteAllText(Slot, json.ToJsonString());
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }

    [Theory]
    [InlineData("wrong-stage")] [InlineData("unprepared-release")] [InlineData("ordinary-preparation")]
    public void ForgedPreparationStagesAreRejected(string fault)
    {
        var (world, charger) = Prepared(); world.Save(Slot);
        var json = JsonNode.Parse(File.ReadAllText(Slot))!; var battle = json["state"]!["battle"]!;
        var plan = battle["plans"]![charger.ToString()]!;
        if (fault == "wrong-stage") plan["preparing"] = false;
        if (fault == "unprepared-release")
        {
            battle["prepared"]!.AsObject().Clear(); plan["preparing"] = false;
            // Move back to the caster's scheduled slot: a pending release needs prior preparation.
            battle["turnIndex"] = world.State.Battle!.Order.IndexOf(charger);
        }
        if (fault == "ordinary-preparation")
        { battle["prepared"]!.AsObject().Clear(); plan["ability"] = "ability.strike"; }
        File.WriteAllText(Slot, json.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
}
