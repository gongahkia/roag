using System.Text.Json.Nodes;
using Vagabonds.Core.Expeditions;
using Xunit;

namespace Vagabonds.Tests.Expeditions;

public sealed class GenerationTests
{
    [Fact]
    public void HundredsOfSeedsHaveReachableObjectivesAndNoGateBypass()
    {
        foreach (int seed in Enumerable.Range(-128, 256).Concat([int.MinValue, int.MaxValue, 1337]))
        {
            var zones = WorldBuilder.Create(seed);
            var validation = WorldBuilder.Validate(zones);
            Assert.True(validation.Valid, $"Seed {seed}: {string.Join("; ", validation.Errors)}");
            Assert.All(zones, z => Assert.False(z.Generation.UsedFallback, $"Seed {seed}: {z.Generation.FallbackReason}"));
            var crypt = zones[2];
            Assert.Equal(35, crypt.Generation.Rooms.Count);
            foreach (var room in crypt.Generation.Rooms)
                foreach (var (dx, dy, bit, opposite) in new[] { (1, 0, 2, 8), (0, 1, 4, 1) })
                {
                    var neighbor = crypt.Generation.Rooms.SingleOrDefault(r => r.X == room.X + dx && r.Y == room.Y + dy);
                    if (neighbor is not null) Assert.Equal((room.Sockets & bit) != 0, (neighbor.Sockets & opposite) != 0);
                }
        }
    }
    [Fact]
    public void SeedsChangeTerrainConnectionsRoomsAndObjectiveLocationsReproducibly()
    {
        var a = new Campaign(42);
        Assert.Equal(a.StateJson(), new Campaign(42).StateJson());
        var b = new Campaign(7);
        Assert.False(a.Zones[1].Tiles.SequenceEqual(b.Zones[1].Tiles));
        Assert.False(a.Zones[2].Tiles.SequenceEqual(b.Zones[2].Tiles));
        Assert.False(a.Zones[2].Generation.Rooms.SequenceEqual(b.Zones[2].Generation.Rooms));
        Assert.NotEqual(a.Zones[1].Objects.Single(o => o.Id == "iron-cache").Position, b.Zones[1].Objects.Single(o => o.Id == "iron-cache").Position);
        Assert.Equal(a.Random.State, new Campaign(42).Random.State); // world RNG does not consume combat RNG
    }
    [Fact]
    public void ExhaustedWfcBudgetUsesPlayableGraphFallback()
    {
        var zones = WorldBuilder.Create(42, new(WfcAttempts: 0));
        Assert.True(zones[2].Generation.UsedFallback);
        Assert.True(WorldBuilder.Validate(zones).Valid);
        Assert.All(zones[2].Generation.Rooms, r => Assert.Equal("cloister", r.Variant));
    }
    [Fact]
    public void WfcPropagatesConstraintsAndRejectsContradictions()
    {
        WfcTile[] tiles = [new("red", 0, 1, 0, 1), new("blue", 0, 2, 0, 2)];
        var solver = new WaveFunctionCollapse(tiles);
        var result = solver.Solve(4, 1, (i, t) => i != 0 || t.Id == "blue", 42);
        Assert.NotNull(result);
        Assert.All(result.Tiles, t => Assert.Equal(1, t));
        Assert.Null(solver.Solve(2, 1, (i, t) => i == 0 ? t.Id == "red" : t.Id == "blue", 42));
        Assert.Null(solver.Solve(2, 1, (_, _) => false, 42));
    }
    [Fact]
    public void CellularSmoothingReadsAnUnchangedPreviousGeneration()
    {
        bool[] initial = new bool[25];
        initial[6] = initial[7] = initial[11] = true;
        var original = (bool[])initial.Clone();
        var next = CellularAutomata.Smooth(initial, 5, 5, 1, birth: 3, survive: 2);
        Assert.True(next[12]); // three neighbors cause birth
        Assert.True(next[6]); // two neighbors cause survival
        Assert.Equal(original, initial);
        Assert.Equal(next, CellularAutomata.Smooth(initial, 5, 5, 1, 3, 2));
    }
    [Fact]
    public void ValidatorDetectsAReachableBypassAroundTheGate()
    {
        var zones = WorldBuilder.Create(42);
        var z = zones[2];
        var gate = z.Objects.Single(o => o.Id == "iron-gate").Position;
        for (int x = gate.X - 1; x <= gate.X + 1; x++) z.Set(new(x, gate.Y - 1), Ground.Stone);
        var result = WorldBuilder.Validate(zones);
        Assert.False(result.Valid);
        Assert.Contains(result.Errors, e => e.Contains("before opening"));
    }
    [Fact]
    public void SavedMapsAreAuthoritativeAndNotRegeneratedFromTheSeed()
    {
        var c = new Campaign(42);
        c.Zone.Set(c.Position, Ground.Path);
        c.Zone.GroundSprites[c.Zone.Index(c.Position)] = "grit";
        var loaded = Campaign.FromJson(c.StateJson());
        Assert.Equal(Ground.Path, loaded.Zone.At(loaded.Position));
        Assert.Equal("grit", loaded.Zone.GroundSprites[loaded.Zone.Index(loaded.Position)]);
        Assert.Equal(c.StateJson(), loaded.StateJson());
    }
    [Fact]
    public void AbbeyBattleFormationStaysInsideItsRoom()
    {
        var c = new Campaign(1337);
        c.TravelTo("to-wilds");
        c.TravelTo("to-crypt");
        c.TravelTo("abbey-hollows");
        Assert.NotNull(c.Battle);
        var center = c.Zone.Objects.Single(o => o.Id == "abbey-hollows").Position;
        Assert.All(c.Battle.Units, u => Assert.True(u.Position.Distance(center) <= 2));
        Assert.Equal(c.Battle.Units.Count, c.Battle.Units.Select(u => u.Position).Distinct().Count());
    }
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1337)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void GeneratedExpeditionAndEvolvingEnvironmentSaveContinueExactly(int seed)
    {
        var c = new Campaign(seed);
        c.TravelTo("to-wilds");
        for (int i = 0; i < 20; i++) c.Wait();
        Assert.Equal(5, c.Zone.Environment.Ticks);
        var restored = Campaign.FromJson(c.StateJson());
        Assert.Equal(c.StateJson(), restored.StateJson());
        for (int i = 0; i < 12; i++) { c.Wait(); restored.Wait(); }
        Assert.Equal(c.StateJson(), restored.StateJson());
    }
    [Fact]
    public void VersionOneSaveRetainsAuthoredMapAndCanBeResaved()
    {
        var json = JsonNode.Parse(new Campaign().StateJson())!;
        json["version"] = 1;
        json.AsObject().Remove("zones");
        json["seen"] = new JsonArray(new JsonArray(Enumerable.Repeat(false, 36 * 26).Select(b => JsonValue.Create(b)).ToArray()),
            new JsonArray(Enumerable.Repeat(false, 48 * 32).Select(b => JsonValue.Create(b)).ToArray()),
            new JsonArray(Enumerable.Repeat(false, 48 * 34).Select(b => JsonValue.Create(b)).ToArray()));
        var legacy = Campaign.FromJson(json.ToJsonString());
        Assert.Equal(new Cell(34, 16), legacy.Zones[2].Objects.Single(o => o.Id == "iron-gate").Position);
        Assert.All(legacy.Zones, z => Assert.Equal(1, z.Generation.Version));
        legacy.TravelTo("to-wilds");
        legacy.TravelTo("wilds-hounds");
        var loaded = Campaign.FromJson(legacy.StateJson());
        Assert.Equal(legacy.StateJson(), loaded.StateJson());
        legacy.AutoBattle(); loaded.AutoBattle();
        Assert.Equal(legacy.StateJson(), loaded.StateJson());
    }
    [Theory]
    [InlineData("tiles")]
    [InlineData("hazards")]
    [InlineData("safe")]
    [InlineData("groundSprites")]
    public void TruncatedMapArraysAreRejected(string field)
    {
        var json = JsonNode.Parse(new Campaign().StateJson())!;
        json["zones"]![1]![field] = new JsonArray();
        Assert.Throws<InvalidDataException>(() => Campaign.FromJson(json.ToJsonString()));
    }
}
