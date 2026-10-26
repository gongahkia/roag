using System.Text.Json;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WorldDesignationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-orders-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    [Fact]
    public void DigSelectionNeedsNoUpfrontFoodAndCancellationCannotCreateSupplies()
    {
        var s = WorldCollisionTests.Arena(); var origin = s.State.Position; s.State.Inventory["food"] = 4;
        var targets = Enumerable.Range(2, 3).Select(x => origin.Offset(x, 0)).ToArray();
        foreach (var p in targets) s.Chunks.Edit(p, Material.Rock);
        var unknown = origin.Offset(80, 0); string chunks = JsonSerializer.Serialize(s.State.Chunks);
        var invalid = s.DesignateWork(WorldCommandKind.Dig, [unknown]); Assert.Equal(0, invalid.Queued);
        Assert.Equal(chunks, JsonSerializer.Serialize(s.State.Chunks));
        uint random = s.State.CombatRandom; long tick = s.State.Tick;
        var result = s.DesignateWork(WorldCommandKind.Dig, targets.Concat(targets));
        Assert.Equal(3, result.Queued); Assert.Equal(0, result.Skipped); Assert.Equal(4, s.Count("food"));
        Assert.Equal(random, s.State.CombatRandom); Assert.Equal(tick, s.State.Tick);
        long id = s.State.Work.First().Id; Assert.True(s.Submit(new(WorldCommandKind.CancelWork, WorkId: id)).Success);
        Assert.Equal(4, s.Count("food")); string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.CancelWork, WorkId: id)).Success); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        Assert.Equal(0, s.DesignateWork(WorldCommandKind.Build, Enumerable.Range(0, 257).Select(x => origin.Offset(x, 0))).Queued);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
    [Fact]
    public void PartyApproachesAndDigsASeamInOrderWithExactSaveContinuation()
    {
        var s = WorldCollisionTests.Arena(); var origin = s.State.Position;
        for (int y = -8; y <= 8; y++) for (int x = -8; x <= 8; x++) s.Chunks.Edit(origin.Offset(x, y), Material.Wall);
        for (int x = 0; x < 4; x++) s.Chunks.Edit(origin.Offset(x, 0), Material.Floor);
        var seam = Enumerable.Range(4, 3).Select(x => origin.Offset(x, 0)).ToArray();
        foreach (var p in seam) s.Chunks.Edit(p, Material.Rock);
        s.State.Inventory["food"] = 100; s.Reveal(); s.Paused = false;
        Assert.Equal(3, s.DesignateWork(WorldCommandKind.Dig, seam.Reverse()).Queued);
        Assert.Equal(2, s.AdvanceTicks(2));
        var slot = Path.Combine(_directory, "work.json"); s.Save(slot); var loaded = WorldSession.Load(slot); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        for (int i = 0; i < 160 && s.State.Work.Count > 0; i++)
        {
            var old = s.State.Position; s.AdvanceTicks(1); loaded.AdvanceTicks(1);
            Assert.InRange(old.Distance(s.State.Position), 0, 1);
            Assert.Equal(JsonSerializer.Serialize(s.State), JsonSerializer.Serialize(loaded.State));
        }
        Assert.Empty(s.State.Work); Assert.All(seam, p => Assert.Equal(Material.Earth, s.Chunks.At(p)));
    }
    [Fact]
    public void StoppingWorkTravelKeepsReservationsAndUnreachableOrdersCannotTeleportTheParty()
    {
        var s = WorldCollisionTests.Arena(); var origin = s.State.Position; var p = origin.Offset(4, 0); s.Chunks.Edit(p, Material.Rock);
        foreach (var d in WorldSession.Directions) s.Chunks.Edit(origin.Offset(d.X, d.Y), Material.Wall);
        Assert.Equal(1, s.DesignateWork(WorldCommandKind.Dig, [p]).Queued); s.Paused = false;
        s.AdvanceTicks(1); Assert.Equal(origin, s.State.Position);
        int food = s.Count("food"); s.StopPartyWorkApproach(); var order = Assert.Single(s.State.Work);
        Assert.False(order.PartyApproach); Assert.Equal(food, s.Count("food"));
        s.Chunks.Edit(origin.Offset(1, 0), Material.Floor); s.AdvanceTicks(1); Assert.Equal(origin, s.State.Position);
        s.Submit(new(WorldCommandKind.ConfigureWork, WorkId: order.Id, Item: "resume")); s.AdvanceTicks(1); Assert.NotEqual(origin, s.State.Position);
    }
    [Fact]
    public void ConstructionCannotFinishOnAnActorWhoArrivesAfterDesignation()
    {
        var s = WorldCollisionTests.Arena(); var target = s.State.Position.Offset(1, 0);
        Assert.Equal(1, s.DesignateWork(WorldCommandKind.Build, [target], Material.Camp).Queued);
        var person = s.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        // A downed occupant is stationary; resting residents now walk home.
        person.Position = target; person.Life = LifeState.Downed; person.Health = 0;
        s.Submit(new(WorldCommandKind.Wait, Amount: 50)); Assert.Equal(Material.Floor, s.Chunks.At(target));
        Assert.Equal(target, person.Position);
        Assert.Equal("Clear the target cell", s.State.Work.Single().Status);
        person.Position = target.Offset(1, 0); s.Submit(new(WorldCommandKind.Wait, Amount: 50));
        Assert.Empty(s.State.Work); Assert.Equal(Material.Camp, s.Chunks.At(target));
    }
}
