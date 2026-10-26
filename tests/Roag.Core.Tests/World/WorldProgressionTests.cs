using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WorldProgressionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-progression-" + Guid.NewGuid().ToString("N"));
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static string State(WorldSession world) => JsonSerializer.Serialize(world.State);
    private static WorldSession Ready(int role = 0, int level = 2)
    {
        var world = WorldSession.Create(42); var actor = world.Party.First(); actor.Role = role; actor.Level = level;
        actor.BaseSpeed = 30; actor.BaseHealth = actor.Health = 1000; actor.Body.Wounds = 0;
        return world;
    }
    public static IEnumerable<object[]> Talents() => ContentCatalog.Default.All<TalentDefinition>().Select(t => new object[] { t.Id });

    [Fact]
    public void GeneratedPeopleHaveStableVariedCombatDispositionsWithoutChangingOtherIdentity()
    {
        var catalog = ContentCatalog.Default; var people = Enumerable.Range(1, 96)
            .Select(i => ActorFactory.Create(catalog, catalog.Game.Villager, 42, i, new(100, 100), 0)).ToArray();
        Assert.All(people, p => Assert.Equal("innate", catalog.Get<TalentDefinition>(p.CombatTrait).Acquisition));
        Assert.Equal(6, people.Select(p => p.CombatTrait).Distinct().Count());
        Assert.All(people.GroupBy(p => p.Role), group => Assert.True(group.Select(p => p.CombatTrait).Distinct().Count() > 1));
        var same = ActorFactory.Create(catalog, catalog.Game.Villager, 42, people[0].Id, new(100, 100), 0);
        Assert.Equal(JsonSerializer.Serialize(people[0]), JsonSerializer.Serialize(same));
        var world = WorldSession.Create(42); string before = State(world);
        foreach (var actor in world.State.Actors) WorldSession.AssignCombatTrait(catalog, actor, world.Seed);
        Assert.Equal(before, State(world));
        Assert.All(world.State.Actors.Where(a => a.Kind != ActorKind.Person), a => Assert.Empty(a.CombatTrait));
    }
    [Fact]
    public void EachClassHasTwoChoicesAtEachMilestoneAndInspectionCostsNothing()
    {
        var world = Ready(); var hero = world.Party.First();
        foreach (var role in world.Content.Classes)
        {
            hero.Role = role.Role; string before = State(world); var choices = world.ProgressionChoices(hero.Id);
            Assert.Equal(4, choices.Count); Assert.Equal(2, choices.Count(t => t.Level == 2)); Assert.Equal(2, choices.Count(t => t.Level == 4));
            Assert.All(choices, choice => Assert.Equal(role.Id, choice.Class));
            Assert.All(choices.Where(t => t.Level == 4), choice => Assert.Contains("level 4", world.ProgressionChoiceReason(hero.Id, choice.Id)));
            Assert.Equal(before, State(world));
        }
        Assert.Empty(world.ProgressionChoices(-1));
    }
    [Theory]
    [InlineData("missing")] [InlineData("outsider")] [InlineData("foreign-class")]
    [InlineData("unknown")] [InlineData("innate")] [InlineData("null")] [InlineData("early")]
    [InlineData("downed")] [InlineData("combat")] [InlineData("already")]
    [InlineData("alternative")] [InlineData("ended")]
    public void InvalidDevelopmentsCannotSpendTimeRandomnessSuppliesOrAChoice(string invalid)
    {
        var world = Ready(); var actor = world.Party.First(); long id = actor.Id; string talent = "talent.warden-bulwark";
        switch (invalid)
        {
            case "missing": id = -1; break;
            case "outsider": id = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited).Id; break;
            case "foreign-class": talent = "talent.ranger-stalker"; break;
            case "unknown": talent = "talent.missing"; break;
            case "innate": talent = actor.CombatTrait; break;
            case "null": talent = null!; break;
            case "early": actor.Level = 1; break;
            case "downed": actor.Life = LifeState.Downed; actor.Health = 0; break;
            case "combat": world.StartBattle(world.State.Actors.First(a => a.Kind == ActorKind.Beast).Id); break;
            case "already": Assert.True(world.ChooseProgression(id, talent).Success); break;
            case "alternative": Assert.True(world.ChooseProgression(id, "talent.warden-breaker").Success); break;
            case "ended": world.State.Endgame.Victory = true; break;
        }
        string before = State(world); string reason = world.ProgressionChoiceReason(id, talent);
        Assert.NotEmpty(reason); var result = world.ChooseProgression(id, talent);
        Assert.False(result.Success); Assert.Equal(reason, result.Message); Assert.Equal(before, State(world));
        var submitted = world.Submit(new(WorldCommandKind.ChooseProgression, ActorId: id, Item: talent));
        Assert.False(submitted.Success); Assert.NotEmpty(submitted.Message); Assert.Equal(before, State(world));
    }
    [Fact]
    public void IndependentMilestonesPersistAndCombatContinuesExactlyAfterLoading()
    {
        var world = Ready(level: 4); var actor = world.Party.First();
        Assert.True(world.ChooseProgression(actor.Id, "talent.warden-bulwark").Success);
        Assert.True(world.ChooseProgression(actor.Id, "talent.warden-avenger").Success);
        Assert.Equal(2, actor.Talents.Count); Assert.Equal(0, world.State.Tick);
        var enemy = world.State.Actors.First(a => a.Kind == ActorKind.Beast); enemy.BaseHealth = enemy.Health = 1000; enemy.BaseSpeed = 1;
        world.StartBattle(enemy.Id); var b = world.State.Battle!; var target = b.Units.First(u => !u.Friendly); target.Health = enemy.Health = 400;
        Assert.Equal(6, world.ActiveSkills().Count);
        world.Save(Slot); var restored = WorldSession.Load(Slot); Assert.Equal(State(world), State(restored));
        var forecast = world.ActionPreview(0, target.Id); int hp = target.Health;
        Assert.True(world.Act(0, target.Id).Success); Assert.True(restored.Act(0, target.Id).Success);
        Assert.InRange(hp - target.Health, forecast.Minimum, forecast.Maximum); Assert.Equal(State(world), State(restored));
    }
    [Fact]
    public void RecruitDismissAndRecruitAgainKeepTheSamePersonAndTheirDevelopments()
    {
        var world = WorldSession.Create(42); var resident = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited && a.HomeTown == 0);
        resident.Position = world.State.Position.Offset(1, 0); world.Chunks.Edit(resident.Position, Material.Floor); world.Reveal();
        string innate = resident.CombatTrait; Assert.True(world.Submit(new(WorldCommandKind.Recruit, ActorId: resident.Id)).Success);
        resident.Level = 2; string choice = world.ProgressionChoices(resident.Id).First(t => t.Level == 2).Id;
        Assert.True(world.ChooseProgression(resident.Id, choice).Success);
        Assert.True(world.Submit(new(WorldCommandKind.Dismiss, ActorId: resident.Id)).Success);
        resident.Position = world.State.Position.Offset(1, 0); world.Chunks.Edit(resident.Position, Material.Floor); world.Reveal();
        Assert.True(world.Submit(new(WorldCommandKind.Recruit, ActorId: resident.Id)).Success);
        Assert.Equal(innate, resident.CombatTrait); Assert.Equal(new[] { choice }, resident.Talents);
    }
    [Theory]
    [MemberData(nameof(Talents))]
    public void EveryAuthoredBonusChangesItsRealCombatOutcomeByTheAdvertisedAmount(string id)
    {
        var result = JsonSerializer.SerializeToNode(TalentPreview.Run(ContentCatalog.Default, id, 42), ContentCatalog.Json)!;
        Assert.True(result["passed"]!.GetValue<bool>(), result.ToJsonString());
    }
    [Theory]
    [InlineData("talent.finisher", 0)] [InlineData("talent.keen-eye", 0)]
    [InlineData("talent.opportunist", 0)] [InlineData("talent.comrade", 0)]
    [InlineData("talent.arcanist-cinder-adept", 2)] [InlineData("talent.ranger-volley-master", 1)]
    public void ConditionalTalentsDoNotBoostAnUnqualifiedOpeningAttack(string id, int role)
    {
        var baseline = Strike(false); var developed = Strike(true);
        Assert.Equal(baseline, developed);

        (int Damage, uint Random) Strike(bool developed)
        {
            var world = Ready(role); var actor = world.Party.First(); actor.CombatTrait = "talent.quick-footed";
            if (developed)
            {
                if (world.Content.Get<TalentDefinition>(id).Acquisition == "innate") actor.CombatTrait = id;
                else Assert.True(world.ChooseProgression(actor.Id, id).Success);
            }
            var enemy = world.State.Actors.First(a => a.Kind == ActorKind.Beast);
            enemy.BaseHealth = enemy.Health = 1000; enemy.BaseSpeed = 1;
            world.StartBattle(enemy.Id); var battle = world.State.Battle!;
            var target = battle.Units.First(u => !u.Friendly); int health = target.Health;
            var preview = world.ActionPreview(0, target.Id);
            Assert.True(world.Act(0, target.Id).Success);
            Assert.InRange(health - target.Health, preview.Minimum, preview.Maximum);
            return (health - target.Health, world.State.CombatRandom);
        }
    }
    [Theory]
    [InlineData("unknown")][InlineData("innate-as-choice")][InlineData("duplicate")]
    [InlineData("same-milestone")][InlineData("wrong-class")][InlineData("future-level")]
    [InlineData("missing-trait")][InlineData("choice-as-trait")]
    public void ForgedProgressionIsRejectedOnLoad(string invalid)
    {
        var world = Ready(); var actor = world.Party.First(); Assert.True(world.ChooseProgression(actor.Id, "talent.warden-bulwark").Success);
        world.Save(Slot); var json = JsonNode.Parse(File.ReadAllText(Slot))!;
        var saved = json["state"]!["actors"]!.AsArray().Single(a => a!["id"]!.GetValue<long>() == actor.Id)!;
        switch (invalid)
        {
            case "unknown": saved["talents"] = new JsonArray("talent.missing"); break;
            case "innate-as-choice": saved["talents"] = new JsonArray(actor.CombatTrait); break;
            case "duplicate": saved["talents"] = new JsonArray("talent.warden-bulwark", "talent.warden-bulwark"); break;
            case "same-milestone": saved["talents"] = new JsonArray("talent.warden-bulwark", "talent.warden-breaker"); break;
            case "wrong-class": saved["talents"] = new JsonArray("talent.ranger-stalker"); break;
            case "future-level": saved["talents"] = new JsonArray("talent.warden-avenger"); break;
            case "missing-trait": saved["combatTrait"] = ""; break;
            case "choice-as-trait": saved["combatTrait"] = "talent.warden-bulwark"; break;
        }
        File.WriteAllText(Slot, json.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
}
