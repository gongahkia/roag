using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WorldCampingTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-camp-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static void Finish(WorldSession s)
    { for (int t = 0; t < 600 && s.State.Work.Count > 0; t++) s.AdvanceTicks(1); Assert.Empty(s.State.Work); }
    [Fact]
    public void DifferentCompanionsCookTreatAndWatchConcurrentlyWithExactSaveContinuation()
    {
        var s = WorldSession.Create(42); var camp = CampPreview.Prepare(s); var party = s.Party.ToArray();
        string[] duties = ["camp.apple-broth", "camp.treatment", "camp.watch"];
        for (int i = 0; i < duties.Length; i++) Assert.True(s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: party[i].Id, Item: duties[i])).Success);
        Assert.Equal(3, s.State.Work.Select(w => w.Worker).Distinct().Count()); Assert.Equal(3, s.State.Work.Select(w => w.Target).Distinct().Count());
        s.AdvanceTicks(10); Directory.CreateDirectory(_directory); string path = Path.Combine(_directory, "camp.json"); s.Save(path);
        var loaded = WorldSession.Load(path); loaded.Paused = false;
        for (int t = 0; t < 600 && s.State.Work.Count > 0; t++)
        { s.AdvanceTicks(1); loaded.AdvanceTicks(1); Assert.Equal(4, s.Party.Select(a => a.Position).Distinct().Count()); }
        Assert.Empty(s.State.Work); Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        Assert.All(s.Party, a => { Assert.Equal(0, a.Body.Wounds); Assert.Equal(a.MaxHealth, a.Health); Assert.Equal(2, s.PreparationPower(a, "burn-resist")); Assert.Equal(2, s.PreparationPower(a, "watch")); });
        Assert.All(party.Take(3), a => Assert.True(a.Body.WorkDone > 0));
    }
    [Fact]
    public void DuplicateDutiesMissingIngredientsAndInvalidCampsCostNothingAndCancellationRefundsOnce()
    {
        var s = WorldSession.Create(1337); var camp = CampPreview.Prepare(s); var hero = s.Party.First();
        s.State.Inventory["apple"] = 0; string before = JsonSerializer.Serialize(s.State);
        var rejected = s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: hero.Id, Item: "camp.apple-broth"));
        Assert.False(rejected.Success); Assert.Contains("Needs", rejected.Message); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        s.State.Inventory["apple"] = 4; Assert.True(s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: hero.Id, Item: "camp.apple-broth")).Success);
        Assert.Equal(2, s.Count("apple")); before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: s.Party.Last().Id, Item: "camp.honey-tea")).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State)); long id = s.State.Work.Single().Id;
        Assert.True(s.Submit(new(WorldCommandKind.CancelWork, WorkId: id)).Success); Assert.Equal(4, s.Count("apple"));
        Assert.False(s.Submit(new(WorldCommandKind.CancelWork, WorkId: id)).Success); Assert.Equal(4, s.Count("apple"));
        Assert.True(s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: hero.Id, Item: "camp.apple-broth")).Success);
        s.Chunks.Edit(camp, Material.Earth); s.AdvanceTicks(1); Assert.Empty(s.State.Work); Assert.Equal(4, s.Count("apple")); Assert.Empty(hero.Preparations);
    }
    [Fact]
    public void WatchWorkerContinuesWhileOthersRestAndPreparesTheNextBattle()
    {
        var s = WorldSession.Create(42); var camp = CampPreview.Prepare(s); var watcher = s.Party.Last();
        Assert.True(s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: watcher.Id, Item: "camp.watch")).Success);
        double fatigue = watcher.Body.Fatigue;
        Assert.True(s.Submit(new(WorldCommandKind.Camp)).Success); Assert.Empty(s.State.Work);
        Assert.True(watcher.Body.Fatigue > fatigue); Assert.True(watcher.Body.WorkDone > 0);
        Assert.All(s.Party, a => Assert.Equal(2, s.PreparationPower(a, "watch")));
        s.StartBattle(s.State.Actors.First(a => a.Kind == ActorKind.Beast).Id); var b = s.State.Battle!;
        Assert.All(b.Units.Where(u => u.Friendly && u.Id != b.Active.Id), u => Assert.True(u.Guarding));
        Assert.All(b.Units.Where(u => u.Friendly), u => Assert.Equal(s.State.Actors.Single(a => a.Id == b.ActorIds[u.Id]).Speed + 2, u.Speed));
    }
    [Fact]
    public void MealsModifyActualArmourBurnAndBleedAndExpireOnRunningWorldTime()
    {
        var s = WorldSession.Create(42); var camp = CampPreview.Prepare(s); var hero = s.Party.First();
        foreach (string id in new[] { "camp.apple-broth", "camp.cherry-eggs", "camp.mushroom-stew" })
        { Assert.True(s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: hero.Id, Item: id)).Success); Finish(s); }
        s.StartBattle(s.State.Actors.First(a => a.Kind == ActorKind.Beast).Id); var b = s.State.Battle!;
        Assert.Equal(hero.Armor + 2, b.Units[0].Armor);
        var enemy = b.Units.First(u => !u.Friendly); b.Order = [enemy.Id, 0]; b.TurnIndex = 0; b.Plans[enemy.Id] = new("ability.strike", 0);
        b.Units[0].Burn = b.Units[0].Bleed = 1; b.Units[0].Armor = 0;
        Assert.True(s.AdvanceEnemy().Success);
        Assert.Equal(1, s.State.CombatEvents.Last(e => e.Kind == "damage" && e.Label == "Burn").Amount);
        Assert.Equal(1, s.State.CombatEvents.Last(e => e.Kind == "damage" && e.Label == "Bleed").Amount);
        long tick = s.State.Tick; s.AdvanceTicks(100); Assert.Equal(tick, s.State.Tick);
        s.State.Battle = null; s.State.Tick = hero.Preparations.Max(p => p.Until); s.AdvanceTicks(1);
        Assert.Empty(hero.Preparations); Assert.Equal(0, s.PreparationPower(hero, "armor"));
    }
    [Fact]
    public void StoriesUseRealEventsRevealActualSitesAndCannotBeRepeatedForFree()
    {
        var s = WorldSession.Create(42); var camp = CampPreview.Prepare(s); var speaker = s.Party.Last();
        speaker.Memories.Add(new("fallen-friend", "A friend fell on the old road.", s.State.Tick, s.State.Tick + 1800, -8));
        int known = s.State.KnownSites.Count;
        Assert.True(s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: speaker.Id, Item: "camp.stories")).Success); Finish(s);
        Assert.Equal(known + 1, s.State.KnownSites.Count); Assert.Contains("friend", s.State.LastCampStory!.Text);
        Assert.Contains(s.Plan.Sites, site => s.State.KnownSites.Contains(site.Id) && s.State.LastCampStory.Text.Contains(site.Name, StringComparison.Ordinal));
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: speaker.Id, Item: "camp.stories")).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
        Directory.CreateDirectory(_directory); string path = Path.Combine(_directory, "story.json"); s.Save(path);
        Assert.Equal(s.State.LastCampStory, WorldSession.Load(path).State.LastCampStory);
    }
    [Fact]
    public void AutomaticStoryAssignmentsRespectEverySpeakersCooldown()
    {
        var s = WorldSession.Create(42); var camp = CampPreview.Prepare(s);
        foreach (var actor in s.Party) actor.Memories.Add(new("camp-story", "Already shared.", s.State.Tick, s.State.Tick + 600, 0));
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.CampActivity, camp, Item: "camp.stories")).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
        var eligible = s.Party.Last(); eligible.Memories.Clear();
        Assert.True(s.Submit(new(WorldCommandKind.CampActivity, camp, Item: "camp.stories")).Success);
        s.AdvanceTicks(1); Assert.Equal(eligible.Id, s.State.Work.Single().Worker); Finish(s);
        Assert.Equal(eligible.Id, s.State.LastCampStory!.Speaker);
    }
    [Theory]
    [InlineData("effect", "arbitrary-code")] [InlineData("role", "missing-handler")]
    public void CampContentRejectsUnregisteredMechanics(string field, string value)
    {
        var catalog = ContentCatalog.Default;
        var changed = ContentCatalog.Load(catalog.Entries.Values.Select(d =>
        {
            var node = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!;
            if (d.Id == "camp.watch") node[field] = value;
            return (File: d.Id, Text: node.ToJsonString());
        }));
        Assert.Contains(changed.Issues, i => i.Field == field);
        Assert.Contains(catalog.References, r => r.Source == "camp.apple-broth" && r.Target == "item.apple");
    }
    [Fact]
    public void LoadRejectsForgedPreparationsAndCampReservationChanges()
    {
        var s = WorldSession.Create(42); var camp = CampPreview.Prepare(s);
        Assert.True(s.Submit(new(WorldCommandKind.CampActivity, camp, ActorId: s.State.Party[0], Item: "camp.apple-broth")).Success);
        Directory.CreateDirectory(_directory); string path = Path.Combine(_directory, "bad.json"); s.Save(path);
        var json = JsonNode.Parse(File.ReadAllText(path))!; json["state"]!["work"]![0]!["reserved"]!["apple"] = 1;
        File.WriteAllText(path, json.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(path));
        s.Party.First().Preparations.Add(new("camp.apple-broth", "armor", 99, 100)); s.Save(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path));
    }
}
