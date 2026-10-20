using Ithica.Core.Expeditions;
using Xunit;

namespace Ithica.Tests.Expeditions;

public sealed class HazardTests
{
    [Fact]
    public void SourcesSpreadSynchronouslyWithoutReachingPastOneNeighborInOneTick()
    {
        var z = new Zone("test", "", "", 9, 9, Ground.Grass);
        Array.Fill(z.Environment.Fuel, (byte)6);
        int source = z.Index(new(4, 4));
        z.Environment.Cells[source] = HazardKind.Fire;
        z.Environment.Age[source] = 1;
        z.Environment.Tick(z, 42);
        Assert.Equal(2, z.Environment.Age[source]);
        Assert.Equal(5, z.Environment.Fuel[source]);
        for (int i = 0; i < z.Tiles.Length; i++)
            if (new Cell(i % 9, i / 9).Distance(new(4, 4)) > 1) Assert.Equal(HazardKind.None, z.Environment.Cells[i]);
        Assert.True(z.Environment.Cells.Count(h => h == HazardKind.Fire) > 1);
    }
    [Fact]
    public void FireEventuallyExhaustsFiniteFuel()
    {
        var z = new Zone("test", "", "", 9, 9, Ground.Grass);
        Array.Fill(z.Environment.Fuel, (byte)3);
        z.Environment.Cells[40] = HazardKind.Fire;
        z.Environment.Age[40] = 1;
        for (int i = 0; i < 160; i++) z.Environment.Tick(z, 42);
        Assert.DoesNotContain(HazardKind.Fire, z.Environment.Cells);
    }
    [Theory]
    [InlineData(42)]
    [InlineData(7)]
    [InlineData(1337)]
    public void HazardsNeverOccupyProtectedRoutesOrAlterConnectivity(int seed)
    {
        var zones = WorldBuilder.Create(seed);
        foreach (var z in zones)
        {
            var ground = (Ground[])z.Tiles.Clone();
            for (int tick = 0; tick < 100; tick++)
            {
                z.Environment.Tick(z, seed);
                for (int i = 0; i < ground.Length; i++)
                    if (z.Safe[i]) Assert.Equal(HazardKind.None, z.Environment.Cells[i]);
            }
            Assert.Equal(ground, z.Tiles);
        }
        Assert.True(WorldBuilder.Validate(zones).Valid);
    }
    [Fact]
    public void WaitingAdvancesActiveZoneOnlyAndFailedMovesDoNotAdvance()
    {
        var c = new Campaign();
        c.TravelTo("to-wilds");
        int before = c.Zone.Environment.Turns;
        for (int i = 0; i < 4; i++) Assert.True(c.Wait().Success);
        Assert.Equal(before + 4, c.Zone.Environment.Turns);
        Assert.Equal(1, c.Zone.Environment.Ticks);
        Assert.Equal(0, c.Zones[2].Environment.Turns);
        string state = c.StateJson();
        Assert.False(c.Move(1, 1).Success);
        Assert.Equal(state, c.StateJson());
        c.TravelTo("wilds-hounds");
        state = c.StateJson();
        Assert.False(c.Wait().Success);
        Assert.Equal(state, c.StateJson());
    }
    [Fact]
    public void TorchAtFullLightCanRemoveAHazardSourceAndPreventImmediateRegrowth()
    {
        var c = new Campaign();
        c.TravelTo("to-wilds");
        var z = c.Zone;
        Cell p = z.Neighbors(c.Position).First(z.Inside);
        int i = z.Index(p);
        z.Safe[i] = false;
        z.Environment.Cells[i] = z.Environment.Sources[i] = HazardKind.Corruption;
        z.Environment.Age[i] = 1;
        int torches = c.Torches;
        Assert.Equal(100, c.Light);
        Assert.True(c.UseTorch().Success);
        Assert.Equal(torches - 1, c.Torches);
        Assert.Equal(HazardKind.None, z.Environment.Cells[i]);
        Assert.Equal(HazardKind.None, z.Environment.Sources[i]);
        Assert.True(z.Environment.Suppressed[i] > 0);
        for (int tick = 0; tick < 4; tick++) z.Environment.Tick(z, c.Seed);
        Assert.Equal(HazardKind.None, z.Environment.Cells[i]);
    }
    [Fact]
    public void EnteringHazardsHasAReadableNonlethalCost()
    {
        var c = new Campaign();
        c.TravelTo("to-wilds");
        Cell target = c.Position + new Cell(1, 0);
        var z = c.Zone;
        z.Safe[z.Index(target)] = false;
        z.Environment.Cells[z.Index(target)] = HazardKind.Spores;
        z.Environment.Age[z.Index(target)] = 1;
        int health = c.Party[0].Health, stress = c.Party[0].Stress;
        Assert.True(c.Move(1, 0).Success);
        Assert.Equal(health - 1, c.Party[0].Health);
        Assert.Equal(stress + 1, c.Party[0].Stress);
        Assert.Contains(c.Journal, e => e.Text.Contains("spores"));
        foreach (var h in c.Party) h.Health = 1;
        c.Wait();
        Assert.All(c.Party, h => Assert.Equal(1, h.Health));
    }
}
