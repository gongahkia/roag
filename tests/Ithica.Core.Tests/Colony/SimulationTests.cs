using Ithica.Core.Colony;
using Xunit;

namespace Regolith.Tests;

public sealed class AtmosphereTests
{
    private static World Chamber(int size = 7)
    {
        var w = new World(size, size);
        Array.Fill(w.Tiles, Terrain.Wall);
        Array.Fill(w.Heat, 20);
        for (int y = 1; y < size - 1; y++)
        {
            for (int x = 1; x < size - 1; x++)
            {
                w.SetTerrain(w.Index(x, y), Terrain.Floor);
            }
        }

        return w;
    }
    [Fact]
    public void DiffusionIsLocalSymmetricAndConservesOxygen()
    {
        var w = Chamber();
        int center = w.Index(3, 3);
        w.Oxygen[center] = 1;
        w.Environment(new(0), .5f);
        Assert.Equal(1, w.Oxygen.Sum(), 5);
        Assert.Equal(.52, w.Oxygen[center], 5);
        foreach (int cell in new[] { center - 1, center + 1, center - 7, center + 7 })
        {
            Assert.Equal(.12, w.Oxygen[cell], 5);
        }

        Assert.Equal(0, w.Oxygen[w.Index(1, 3)]);
    }
    [Fact]
    public void DiffusionStaysBoundedOverManyUpdates()
    {
        var w = Chamber();
        var random = new RandomStream(9);
        foreach (int i in w.Floors)
        {
            w.Oxygen[i] = (float)random.NextDouble();
        }

        float total = w.Oxygen.Sum();
        for (int n = 0; n < 200; n++)
        {
            w.Environment(random, .4f);
        }

        Assert.Equal(total, w.Oxygen.Sum(), 3);
        Assert.All(w.Oxygen, x => Assert.InRange(x, 0, 1));
    }
    [Fact]
    public void BreachVentsAndRepairStopsLoss()
    {
        var w = Chamber();
        foreach (int i in w.Floors)
        {
            w.Oxygen[i] = .9f;
        }

        float initial = w.Oxygen.Sum();
        int hole = w.Index(3, 0);
        w.SetTerrain(hole, Terrain.Vacuum);
        var random = new RandomStream(3);
        for (int n = 0; n < 40; n++)
        {
            w.Environment(random, .4f);
        }

        Assert.True(w.Oxygen.Sum() < initial - 1);
        w.SetTerrain(hole, Terrain.Wall);
        float sealedTotal = w.Oxygen.Sum();
        for (int n = 0; n < 40; n++)
        {
            w.Environment(random, .4f);
        }

        Assert.Equal(sealedTotal, w.Oxygen.Sum(), 3);
    }
    [Fact]
    public void AirlockAllowsWalkingButBlocksAtmosphere()
    {
        var w = Chamber();
        for (int y = 1; y < 6; y++)
        {
            w.SetTerrain(w.Index(3, y), Terrain.Wall);
        }

        int door = w.Index(3, 3);
        w.SetTerrain(door, Terrain.Airlock);
        w.Oxygen[w.Index(2, 3)] = 1;
        for (int n = 0; n < 20; n++)
        {
            w.Environment(new(0), .5f);
        }

        Assert.True(w.Walkable(door));
        Assert.Equal(0, w.Oxygen[w.Index(4, 3)]);
    }
    [Fact]
    public void FireConsumesAirHeatsNeighborsAndDiesWithoutOxygen()
    {
        var w = Chamber();
        int center = w.Index(3, 3);
        w.Heat[center] = 80;
        w.Oxygen[center] = .8f;
        w.Fire[center] = .6f;
        w.Environment(new(4), .5f);
        Assert.True(w.Heat[center - 1] > 20);
        Assert.True(w.Oxygen.Sum() < .8f);
        Array.Clear(w.Oxygen);
        for (int n = 0; n < 12; n++)
        {
            w.Environment(new(4), .5f);
        }

        Assert.Equal(0, w.Fire[center]);
    }
    [Fact]
    public void AtmosphereUpdatesReuseMemory()
    {
        var w = Chamber(64);
        var random = new RandomStream(4);
        w.Environment(random, .5f);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int n = 0; n < 100; n++)
        {
            w.Environment(random, .5f);
        }

        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }
}

public sealed class ColonyTests
{
    private static Simulation Colony(int seed = 42) => new(seed, 48, 32);
    [Fact]
    public void WorldGenerationIsSeeded()
    {
        var a = Colony();
        var b = Colony();
        var c = Colony(81);
        Assert.Equal(a.World.Tiles, b.World.Tiles);
        Assert.NotEqual(a.World.Tiles, c.World.Tiles);
    }
    [Fact]
    public void StartsWithAdaOneContactPowerAndPressure()
    {
        var s = Colony();
        Assert.Single(s.Crew);
        Assert.Equal("Ada", s.Crew[0].Name);
        Assert.Single(s.Survivors);
        Assert.True(s.Pressure > .9f);
        Assert.Equal(12, s.Generated);
        Assert.Equal(11, s.Demand);
        Assert.All(s.Machines, m => Assert.True(m.Powered));
    }
    [Fact]
    public void BuildReservationAndCancelAreExact()
    {
        var s = Colony();
        var stock = s.Resources;
        int x = 24, y = 17;
        Assert.True(s.Designate(x, y, Tool.Build, ModuleKind.Solar).Success);
        Assert.Equal(stock.Metal - 14, s.Resources.Metal);
        Assert.False(s.Designate(x, y, Tool.Build, ModuleKind.Solar).Success);
        Assert.True(s.Designate(x, y, Tool.Cancel).Success);
        Assert.Equal(stock, s.Resources);
        Assert.Empty(s.Jobs);
    }
    [Fact]
    public void InvalidAndUnaffordableOrdersSpendNothing()
    {
        var s = Colony();
        s.Resources = new(0, 5, 5, 0);
        var stock = s.Resources;
        Assert.False(s.Designate(24, 16, Tool.Build, ModuleKind.Beacon).Success);
        Assert.False(s.Designate(-1, 0, Tool.Dig).Success);
        Assert.Equal(stock, s.Resources);
        Assert.False(s.Designate(34, 16, Tool.Build, ModuleKind.Farm).Success);
        Assert.False(s.Designate(34, 16, Tool.Build, ModuleKind.Floor).Success);
    }
    [Fact]
    public void ModulesRequireDeck()
    {
        var s = Colony();
        Assert.False(s.Designate(34, 16, Tool.Build, ModuleKind.Farm).Success);
        Assert.True(s.Designate(34, 16, Tool.Build, ModuleKind.Floor).Success);
    }
    [Fact]
    public void MiningDeliversResourcesAndCreatesDeck()
    {
        var s = Colony();
        float before = s.Resources.Crystal;
        int target = s.World.Index(36, 11);
        Assert.True(s.Designate(36, 11, Tool.Dig).Success);
        s.Step(120);
        Assert.Empty(s.Jobs);
        Assert.Equal(before + 10, s.Resources.Crystal);
        Assert.Equal(Terrain.Floor, s.World.Tiles[target]);
    }
    [Fact]
    public void CrewBuildAWorkingSolarArray()
    {
        var s = Colony();
        Assert.True(s.Designate(24, 17, Tool.Build, ModuleKind.Solar).Success);
        s.Step(70);
        var m = s.MachineAt(s.World.Index(24, 17));
        Assert.NotNull(m);
        Assert.Equal(ModuleKind.Solar, m.Kind);
        Assert.True(m.Powered);
        Assert.True(s.Generated > 12);
    }
    [Fact]
    public void RouterUsesAirlockAndAvoidsHull()
    {
        var s = Colony();
        var route = new List<int>();
        int target = s.World.Index(35, 16);
        Assert.True(s.Router.Find(s.World.Center, [target], route));
        Assert.Contains(s.World.Index(33, 16), route);
        Assert.All(route, i => Assert.True(s.World.Walkable(i)));
        Assert.False(s.Router.Find(s.World.Center, [s.World.Index(15, 16)], route));
    }
    [Fact]
    public void IsolatedConsumersCannotStealPower()
    {
        var s = Colony();
        int cell = s.World.Index(36, 23);
        s.World.SetTerrain(cell, Terrain.Floor);
        var machine = s.AddMachine(ModuleKind.Oxygen, cell);
        s.UpdatePower();
        Assert.False(machine.Powered);
        Assert.True(s.Machines.First(m => m.Kind == ModuleKind.Oxygen && m != machine).Powered);
    }
    [Fact]
    public void BatteriesStayWithTheirOwnElectricalIsland()
    {
        var s = Colony();
        int cell = s.World.Index(36, 23);
        for (int dx = 0; dx < 3; dx++)
        {
            s.World.SetTerrain(cell + dx, Terrain.Floor);
        }

        var battery = s.AddMachine(ModuleKind.Battery, cell, 10);
        var oxygen = s.AddMachine(ModuleKind.Oxygen, cell + 1);
        var original = s.Machines.First(m => m.Kind == ModuleKind.Battery && m != battery);
        s.UpdatePower(true);
        Assert.True(oxygen.Powered);
        Assert.True(battery.Energy < 10);
        Assert.True(original.Energy > 220);
        battery.Energy = 0;
        s.UpdatePower();
        Assert.False(oxygen.Powered);
    }
    [Fact]
    public void PowerInspectionDoesNotAdvanceTimeOrStoredEnergy()
    {
        var s = Colony();
        float before = s.Charge;
        for (int n = 0; n < 20; n++)
        {
            s.UpdatePower();
        }

        Assert.Equal(before, s.Charge);
        Assert.Equal(0, s.Tick);
    }
    [Fact]
    public void LifeSupportHasPriorityWhenBatteryIsEmpty()
    {
        var s = Colony();
        foreach (var m in s.Machines)
        {
            if (m.Kind == ModuleKind.Battery)
            {
                m.Energy = 0;
            }
        }

        var beacon = s.AddMachine(ModuleKind.Beacon, s.World.Index(24, 17));
        s.UpdatePower();
        Assert.True(s.Machines.First(m => m.Kind == ModuleKind.Oxygen).Powered);
        Assert.False(beacon.Powered);
    }
    [Fact]
    public void SurveyRevealsMoreTerrain()
    {
        var s = Colony();
        int cell = s.World.Index(39, 16);
        s.World.SetTerrain(cell, Terrain.Vacuum);
        int before = s.World.Discovered.Count(x => x > 0);
        Assert.True(s.Designate(39, 16, Tool.Survey).Success);
        s.Step(100);
        Assert.Empty(s.Jobs);
        Assert.True(s.World.Discovered.Count(x => x > 0) > before);
    }
    [Fact]
    public void NoLivingCrewEndsTheColony()
    {
        var s = Colony();
        s.Crew[0].Health = 0;
        s.Step();
        Assert.True(s.GameOver);
        Assert.False(s.Crew[0].Alive);
        long tick = s.Tick;
        s.Step(30);
        Assert.Equal(tick, s.Tick);
    }
    [Fact]
    public void WorkBlockedByChangedTerrainCanBeCancelled()
    {
        var s = Colony();
        s.Designate(24, 17, Tool.Build, ModuleKind.Solar);
        s.Step(1);
        var stock = s.Resources;
        Assert.True(s.Designate(24, 17, Tool.Cancel).Success);
        Assert.Empty(s.Jobs);
        Assert.All(s.Crew, c => Assert.Null(c.Job));
        Assert.True(s.Resources.Metal > stock.Metal);
    }
    [Fact]
    public void CharterCanBeAchievedThroughRescuesAndConstruction()
    {
        var s = Colony();
        Assert.True(s.Designate(36, 11, Tool.Dig).Success);
        for (int n = 0; n < 480; n++)
        {
            foreach (var contact in s.Survivors)
            {
                if (contact.State == ContactState.Stranded && s.ExpeditionFor(contact.Id) is null)
                {
                    s.Dispatch(contact.Id);
                }
            }

            s.Step();
        }
        Assert.True(s.RescuedCount >= 3);
        Assert.True(s.LivingCount >= 4);
        Assert.True(s.Designate(24, 17, Tool.Build, ModuleKind.Solar).Success);
        Assert.True(s.Designate(25, 17, Tool.Build, ModuleKind.Solar).Success);
        Assert.True(s.Designate(26, 17, Tool.Build, ModuleKind.Beacon).Success);
        for (int n = 0; n < 650 && !s.Victory; n++)
        {
            int cx = s.World.Width / 2, cy = s.World.Height / 2;
            for (int x = cx - 9; x <= cx + 9; x++)
            {
                foreach (int y in new[] { cy - 5, cy + 5 })
                {
                    if (s.World.Tiles[s.World.Index(x, y)] == Terrain.Vacuum)
                    {
                        s.Designate(x, y, Tool.Build, ModuleKind.Wall);
                    }
                }
            }

            s.Step();
        }
        Assert.True(s.Victory, System.Text.Json.JsonSerializer.Serialize(s.Summary()));
    }
}

public sealed class RescueTests
{
    private static Simulation Colony(int seed = 42) => new(seed, 48, 32);
    [Fact]
    public void RescueWalksOutEscortsBackAndRecruitsOnlyInsidePressure()
    {
        var s = Colony();
        Stock stock = s.Resources;
        Assert.True(s.Dispatch(0).Success);
        Assert.Equal(stock - Simulation.RescueCost, s.Resources);
        Assert.Single(s.Crew);
        bool sawEscort = false;
        for (int n = 0; n < 120 && s.RescuedCount == 0; n++)
        {
            s.Step();
            if (s.Survivors[0].State == ContactState.Escorted)
            {
                sawEscort = true;
                Assert.Single(s.Crew);
            }
        }
        Assert.True(sawEscort);
        Assert.Equal(1, s.RescuedCount);
        Assert.Equal(2, s.Crew.Count);
        Assert.True(s.World.Breathable(s.Crew[1].Cell));
        Assert.True(s.Crew[1].Rest);
    }
    [Fact]
    public void RepeatedDispatchDoesNotDuplicateExpeditionOrChargeTwice()
    {
        var s = Colony();
        s.Dispatch(0);
        var stock = s.Resources;
        Assert.False(s.Dispatch(0).Success);
        Assert.Single(s.Expeditions);
        Assert.Equal(stock, s.Resources);
    }
    [Fact]
    public void UnreadyAndUnaffordableRescuesSpendNothing()
    {
        var s = Colony();
        var stock = s.Resources;
        s.Crew[0].Health = 20;
        Assert.False(s.Dispatch(0).Success);
        Assert.Equal(stock, s.Resources);
        s.Crew[0].Health = 100;
        s.Resources = new(0, 0, 0, 0);
        Assert.False(s.Dispatch(0).Success);
        Assert.Empty(s.Expeditions);
    }
    [Fact]
    public void UnassistedSurvivorLosesOxygenThenDies()
    {
        var s = Colony();
        float health = s.Survivors[0].Health;
        s.Step(20);
        Assert.True(s.Survivors[0].Suit < 68);
        Assert.Equal(health, s.Survivors[0].Health);
        s.Survivors[0].Suit = .01f;
        s.Survivors[0].Health = .5f;
        s.Step();
        Assert.Equal(ContactState.Lost, s.Survivors[0].State);
        Assert.False(s.Dispatch(0).Success);
    }
    [Fact]
    public void LostContactReleasesRescuer()
    {
        var s = Colony();
        s.Dispatch(0);
        s.Survivors[0].Suit = .01f;
        s.Survivors[0].Health = .5f;
        s.Step();
        Assert.Equal(RescuePhase.Failed, s.Expeditions[0].Phase);
        Assert.Null(s.Crew[0].Expedition);
        Assert.True(s.Crew[0].Alive);
    }
    [Fact]
    public void LostRescuerLeavesContactForAnotherAttempt()
    {
        var s = Colony();
        s.Dispatch(0);
        s.Step(2);
        s.Crew[0].Health = 0;
        s.AddColonist("Ren", "Medic", "Patient", s.World.Center);
        s.Step();
        Assert.Equal(RescuePhase.Failed, s.Expeditions[0].Phase);
        Assert.Equal(ContactState.Stranded, s.Survivors[0].State);
        Assert.True(s.Dispatch(0).Success);
    }
    [Theory]
    [InlineData(7)]
    [InlineData(81)]
    [InlineData(8675309)]
    public void SpawnedContactsAreReachable(int seed)
    {
        var s = Colony(seed);
        var route = new List<int>();
        for (int n = 0; n < 5; n++)
        {
            var contact = s.Survivors[^1];
            Assert.True(s.Router.Find(s.World.Center, [contact.Cell], route));
            s.SpawnSurvivor();
        }
    }
    [Fact]
    public void NewDistressSignalsContinueAfterRecruitment()
    {
        var s = Colony();
        s.Dispatch(0);
        s.Step(180);
        Assert.Equal(1, s.RescuedCount);
        Assert.Contains(s.Survivors, p => p.Name == "Sol" && p.Active);
    }
}

public sealed class SaveTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(24)]
    [InlineData(60)]
    public void SaveResumeMatchesUninterruptedSimulation(int ticks)
    {
        var s = new Simulation(42, 48, 32);
        s.DemoOrders();
        s.Step(ticks);
        var loaded = Simulation.FromJson(s.StateJson());
        Assert.Equal(s.StateJson(), loaded.StateJson());
        s.Step(150);
        loaded.Step(150);
        Assert.Equal(s.StateJson(), loaded.StateJson());
    }
    [Fact]
    public void AtomicSaveLeavesNoTemporaryFile()
    {
        string dir = Path.Combine(Path.GetTempPath(), "regolith-tests-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(dir, "colony.json");
        try
        {
            var s = new Simulation();
            s.Save(path);
            s.Step(20);
            s.Save(path);
            Assert.Equal(s.StateJson(), Simulation.Load(path).StateJson());
            Assert.Single(Directory.GetFiles(dir));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"format\":\"regolith-csharp\",\"version\":200}")]
    [InlineData("{\"version\":2}")]
    public void InvalidOrLegacySavesAreRejected(string json) => Assert.Throws<InvalidDataException>(() => Simulation.FromJson(json));
    [Fact]
    public void IncompleteWorldCannotBeLoaded()
    {
        var s = new Simulation();
        string json = s.StateJson();
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        node["oxygen"] = new System.Text.Json.Nodes.JsonArray();
        Assert.Throws<InvalidDataException>(() => Simulation.FromJson(node.ToJsonString()));
    }
    [Fact]
    public void RandomStateIsExplicitAndRepeatable()
    {
        var a = new RandomStream(42);
        for (int n = 0; n < 200; n++)
        {
            a.NextUInt();
        }

        var b = new RandomStream(0) { State = a.State };
        for (int n = 0; n < 100; n++)
        {
            Assert.Equal(a.NextUInt(), b.NextUInt());
        }
    }
}
