using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.World;
using Vagabonds.Tests.Content;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class SimulationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-simulation-" + Guid.NewGuid().ToString("N"));
    private string Slot => Path.Combine(_directory, "slot.json");
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static void Wait(WorldSession s, int ticks = 50) => Assert.True(s.Submit(new(WorldCommandKind.Wait, Amount: ticks)).Success);
    [Fact]
    public void SharedNeedsUseTheSameRatesAndRecruitmentRetainsTheBody()
    {
        var s = WorldSession.Create(42); var hero = s.Party.Single();
        var npc = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        npc.Position = s.State.Position; npc.TraitId = hero.TraitId; npc.Level = hero.Level;
        foreach (var a in new[] { hero, npc }) { a.Body.Hunger = 40; a.Body.Fatigue = 20; a.Body.Wounds = 2; }
        int food = s.Count("food"); Wait(s, 5);
        Assert.Equal(hero.Body.Hunger, npc.Body.Hunger); Assert.Equal(hero.Body.Fatigue, npc.Body.Fatigue);
        Assert.Equal(food - 1, s.Count("food")); Assert.Equal(2, npc.Body.Wounds);
        npc.Position = s.State.Position;
        string body = JsonSerializer.Serialize(npc.Body);
        Assert.True(s.Submit(new(WorldCommandKind.Recruit, ActorId: npc.Id)).Success);
        Assert.Equal(body, JsonSerializer.Serialize(npc.Body)); Assert.Same(npc, s.Party.Last());
        Assert.Equal(npc.BasePower - 1, npc.Power); Assert.Contains(npc.Memories, m => m.Key == "recruited");
        Assert.True(s.Submit(new(WorldCommandKind.Dismiss, ActorId: npc.Id)).Success);
        Assert.False(npc.Recruited); Assert.Equal(body, JsonSerializer.Serialize(npc.Body)); Assert.Single(s.State.Actors, a => a.Id == npc.Id);
    }
    [Fact]
    public void ConstructionReservesCostsBeforeTimeAndPartialCancellationRefundsExactlyOnce()
    {
        var s = WorldSession.Create(42); var p = s.State.Position.Offset(1, 0); s.Chunks.Edit(p, Material.Floor);
        int stone = s.Count("stone"); uint rng = s.State.CombatRandom;
        Assert.True(s.Submit(new(WorldCommandKind.Build, p, Material: Material.Wall)).Success);
        var order = Assert.Single(s.State.Work); Assert.Equal(stone - 2, s.Count("stone")); Assert.Equal(Material.Floor, s.Chunks.At(p));
        Assert.Equal(0, s.State.Tick); Assert.Equal(rng, s.State.CombatRandom); Wait(s, 5);
        Assert.InRange(order.Progress, .1, order.Required - .1);
        Assert.True(s.Submit(new(WorldCommandKind.CancelWork, WorkId: order.Id)).Success); Assert.Equal(stone, s.Count("stone"));
        string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.CancelWork, WorkId: order.Id)).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
    [Fact]
    public void AssignedResidentReachesWorkAndRemainsTheSameWorkerAfterRecruitment()
    {
        var s = WorldSession.Create(42); var p = s.State.Position.Offset(1, 0);
        for (int x = -1; x < 7; x++) s.Chunks.Edit(s.State.Position.Offset(x, 0), Material.Floor);
        var worker = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); worker.Position = p.Offset(4, 0);
        Assert.True(s.Submit(new(WorldCommandKind.Build, p, Material: Material.Wall)).Success);
        var order = Assert.Single(s.State.Work); order.Required = 1000;
        Assert.True(s.Submit(new(WorldCommandKind.AssignWork, ActorId: worker.Id, WorkId: order.Id)).Success);
        Wait(s, 30); Assert.True(order.Progress > 0); Assert.InRange(worker.Position.Distance(p), 0, 1);
        double progress = order.Progress; var workingCell = worker.Position;
        Assert.InRange(worker.Position.Distance(s.State.Position), 0, 2);
        Assert.True(s.Submit(new(WorldCommandKind.Recruit, ActorId: worker.Id)).Success);
        Assert.Equal(workingCell, worker.Position);
        Wait(s, 5); Assert.True(order.Progress > progress); Assert.Equal(worker.Id, order.Worker);
        Assert.Single(s.State.Actors, a => a.Id == worker.Id);
    }
    [Fact]
    public void ChangedTargetsCancelAndReleaseTheirReservations()
    {
        var s = WorldSession.Create(42); var p = s.State.Position.Offset(1, 0); s.Chunks.Edit(p, Material.Floor); int stone = s.Count("stone");
        s.Submit(new(WorldCommandKind.Build, p, Material: Material.Wall)); s.Chunks.Edit(p, Material.Rock); Wait(s, 1);
        Assert.Empty(s.State.Work); Assert.Equal(stone, s.Count("stone")); Wait(s, 5); Assert.Equal(stone, s.Count("stone"));
    }
    [Fact]
    public void AuthoredWorkAndCostsDriveConstructionAndExactDismantlingRefunds()
    {
        var content = ContentTests.Edited("recipe.wall", n => { n["cost"] = JsonNode.Parse("{\"item.gold\":3}"); n["work"] = 8; });
        var s = WorldSession.Create(42, content); var p = s.State.Position.Offset(1, 0); s.Chunks.Edit(p, Material.Floor); int gold = s.Count("gold");
        Assert.True(s.Submit(new(WorldCommandKind.Build, p, Material: Material.Wall)).Success);
        Assert.Equal(8, Assert.Single(s.State.Work).Required); Wait(s, 15); Assert.Equal(Material.Wall, s.Chunks.At(p));
        Assert.Equal(gold - 3, s.Count("gold")); s.State.Inventory["food"] = 0;
        Assert.True(s.Submit(new(WorldCommandKind.Dig, p)).Success); Wait(s, 30);
        Assert.Equal(gold, s.Count("gold")); Assert.Equal(Material.Floor, s.Chunks.At(p)); Assert.Empty(s.State.Structures);
    }
    [Fact]
    public void WorkNeedsMemoriesAndIncidentRandomnessContinueExactlyThroughASave()
    {
        var s = WorldSession.Create(42); var p = s.State.Position.Offset(1, 0); s.Chunks.Edit(p, Material.Floor);
        var hero = s.Party.First(); hero.Body.Hunger = 30; hero.Body.Fatigue = 40; hero.Body.Wounds = 2;
        var npc = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited); npc.Position = s.State.Position;
        s.PerformInteraction(npc.Id, "interaction.stories"); s.Submit(new(WorldCommandKind.Build, p, Material: Material.Wall)); Wait(s, 3);
        s.Save(Slot); var loaded = WorldSession.Load(Slot); Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        Wait(s, 300); Wait(loaded, 300); Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
    }
    [Fact]
    public void RestAdvancesSharedTimeAndDangerInterruptsWaiting()
    {
        var s = WorldSession.Create(42, ContentTests.Edited("game.vagabonds", n => n["incidents"] = new JsonArray())); s.Submit(new(WorldCommandKind.Build, s.State.Position, Material: Material.Camp)); Wait(s);
        var hero = s.Party.First(); hero.Health = 10; hero.Body.Fatigue = 80; hero.Body.Wounds = 2; s.State.Inventory["food"] = 10;
        long before = s.State.Tick; Assert.True(s.Submit(new(WorldCommandKind.Camp)).Success);
        Assert.Equal(before + 300, s.State.Tick); Assert.True(hero.Health > 10); Assert.True(hero.Body.Fatigue < 50); Assert.Equal(1, hero.Body.Wounds);
        var enemy = s.State.Actors.First(a => a.Kind == ActorKind.Beast); enemy.Position = s.State.Position.Offset(1, 0);
        before = s.State.Tick; Wait(s, 300); Assert.True(s.IsInCombat); Assert.InRange(s.State.Tick - before, 1, 5);
    }
    [Fact]
    public void WornCampsCanBeRepairedAndRecipesProduceActualSupplies()
    {
        var s = WorldSession.Create(42); s.Submit(new(WorldCommandKind.Build, s.State.Position, Material: Material.Camp)); Wait(s);
        var camp = Assert.Single(s.State.Structures); camp.Condition = 20; string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Camp)).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        Assert.True(s.Submit(new(WorldCommandKind.Repair, camp.Position)).Success); Wait(s, 30); Assert.True(camp.Condition > 95);
        int medicine = s.Count("medicine"); Assert.True(s.Submit(new(WorldCommandKind.Craft, Item: "recipe.field-medicine")).Success);
        Wait(s); Assert.Equal(medicine + 1, s.Count("medicine"));
    }
    [Fact]
    public void FoodGatheringTakesWorkAndHasARecoveryInterval()
    {
        var s = WorldSession.Create(42); int food = s.Count("food"); Assert.True(s.Submit(new(WorldCommandKind.Forage)).Success);
        Assert.Equal(food, s.Count("food")); Wait(s); Assert.Equal(food + 2, s.Count("food"));
        string before = JsonSerializer.Serialize(s.State); Assert.False(s.Submit(new(WorldCommandKind.Forage)).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
    [Fact]
    public void EveryIncidentAndRecipePreviewExercisesARegisteredRuntimeHandler()
    {
        var content = Vagabonds.Core.Content.ContentCatalog.Default;
        foreach (string id in content.Game.Incidents)
        {
            var report = JsonSerializer.SerializeToNode(SimulationPreview.Incident(content, id, 42), Vagabonds.Core.Content.ContentCatalog.Json)!;
            Assert.True(report["happened"]!.GetValue<bool>());
        }
        foreach (string id in content.Game.Recipes)
        {
            var report = JsonSerializer.SerializeToNode(SimulationPreview.Recipe(content, id, 42), Vagabonds.Core.Content.ContentCatalog.Json)!;
            Assert.True(report["result"]!["success"]!.GetValue<bool>(), id);
            Assert.Empty(report["pending"]!.AsArray());
        }
        Assert.NotEmpty(ContentTests.Edited("simulation.shared", n => n["hungerPerTick"] = -1).Issues);
        Assert.NotEmpty(ContentTests.Edited("incident.helping-hands", n => n["effect"] = "invented-handler").Issues);
    }
    [Theory]
    [InlineData("work", "[null]")]
    [InlineData("structures", "[null]")]
    [InlineData("incidentRandom", "0")]
    [InlineData("incidentAfter", "null")]
    public void NewSimulationStateRejectsMalformedSaves(string field, string value)
    {
        var s = WorldSession.Create(42); s.Save(Slot); var root = JsonNode.Parse(File.ReadAllText(Slot))!;
        root["state"]![field] = JsonNode.Parse(value); File.WriteAllText(Slot, root.ToJsonString());
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
}
