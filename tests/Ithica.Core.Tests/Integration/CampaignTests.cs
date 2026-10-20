using System.Text.Json.Nodes;
using Ithica.Core;
using Ithica.Core.Colony;
using Ithica.Core.Expeditions;
using Ithica.Core.Generation;
using Xunit;
using Game = Ithica.Core.Campaign;

namespace Ithica.Tests.Integration;

public sealed class CampaignTests
{
    internal static void Ok(Result result) => Assert.True(result.Success, result.Message);
    internal static Game Outside(int seed = 42)
    {
        var g = new Game(seed); Ok(g.Prepare("rescue-0")); Ok(g.Depart([0])); return g;
    }
    internal static void Fight(Game g)
    {
        for (int i = 0; g.ActiveField?.Battle is { } b && i < 100; i++)
            Ok(g.Act(0, b.Units.First(u => !u.Friendly && u.Alive).Id));
        Assert.Null(g.ActiveField?.Battle);
    }
    internal static void Go(Game g, string id, bool interact = true)
    {
        for (int attempts = 0; attempts < 20; attempts++)
        {
            var f = g.ActiveField!;
            var obj = f.Zone.Objects.Single(o => o.Id == id);
            if (f.Position.Distance(obj.Position) <= 1)
            {
                if (interact) { Ok(g.Interact(id)); if (g.ActiveField?.Battle is not null) Fight(g); }
                return;
            }
            var route = f.Route(obj.Position);
            Assert.NotEmpty(route);
            foreach (var p in route)
            {
                Ok(g.Move(p.X - f.Position.X, p.Y - f.Position.Y));
                if (g.ActiveField?.Battle is not null) { Fight(g); break; }
                if (f.Position.Distance(obj.Position) <= 1) break;
            }
        }
        throw new InvalidOperationException("Travel did not finish: " + id);
    }
    internal static void Rescue(Game g)
    {
        Ok(g.Prepare("rescue-0")); Ok(g.Depart([0])); Go(g, "survivor"); Go(g, "return"); Ok(g.DismissArrival());
    }

    [Fact]
    public void ThinkingInspectionAndInvalidActionsDoNotChangeTheCampaign()
    {
        var g = Outside(); string before = g.StateJson();
        for (int i = 0; i < 200; i++) g.UpdateRealtime(60);
        _ = g.ActiveField!.Route(new(12, 16)); _ = g.Summary();
        Assert.False(g.Move(2, 0).Success); Assert.False(g.Act(99).Success);
        Assert.False(g.Interact("unknown").Success); Assert.False(g.Treat(0).Success);
        Assert.Equal(before, g.StateJson());
    }

    [Fact]
    public void OneActionTicksHomeAndAwayNeedsExactlyOnce()
    {
        var g = Outside(); var resident = g.Home.Crew[0];
        long tick = g.CampaignTick; float hunger = resident.Hunger, energy = resident.Energy, suit = resident.Suit;
        int cell = resident.Cell, jobs = resident.CompletedWork;
        Ok(g.Wait());
        Assert.Equal(tick + ActionCosts.Wait, g.CampaignTick);
        Assert.Equal(hunger + .12f, resident.Hunger, 4); Assert.Equal(energy - .06f, resident.Energy, 4);
        Assert.Equal(suit - .19f, resident.Suit, 4); Assert.Equal(cell, resident.Cell); Assert.Equal(jobs, resident.CompletedWork);
        Assert.Equal(1, g.Home.LivingCount); Assert.False(g.Home.GameOver);
    }

    [Fact]
    public void PreparationCancelAndModeChangesHaveNoBacklogOrSupplyCharge()
    {
        var g = new Game(); g.Clock.Paused = false; g.Clock.Speed = 8; g.UpdateRealtime(.1);
        var stock = g.Home.Resources; long tick = g.CampaignTick;
        Ok(g.Prepare("rescue-0")); Assert.Equal(0, g.Clock.Accumulator);
        g.UpdateRealtime(600); Ok(g.CancelPreparation());
        Assert.Equal(tick, g.CampaignTick); Assert.Equal(stock, g.Home.Resources);
        Assert.Equal(8, g.Clock.Speed); Assert.False(g.Clock.Paused);
        g.UpdateRealtime(.01); Assert.Equal(tick, g.CampaignTick);
    }

    [Fact]
    public void InvalidDepartureDoesNotReserveOrReleaseAnything()
    {
        var g = new Game(); Ok(g.Prepare("rescue-0")); string state = g.StateJson();
        Assert.False(g.Depart([0, 0]).Success); Assert.False(g.Depart([500]).Success);
        Assert.False(g.Depart([0], new() { Food = 101 }).Success);
        Assert.Equal(state, g.StateJson());
    }

    [Fact]
    public void SharedWorkersReleaseHomeOrdersOnDeparture()
    {
        var g = new Game(); var c = g.Home.Crew[0];
        int target = g.Home.World.Center + 2; g.Home.World.SetTerrain(target, Terrain.Ore);
        Ok(g.Home.Designate(g.Home.World.X(target), g.Home.World.Y(target), Tool.Dig));
        Ok(g.Prepare("rescue-0")); Ok(g.Depart([0]));
        for (int i = 0; i < 20; i++) Ok(g.Wait());
        Assert.Null(c.Job); Assert.Null(g.Home.JobAt(target)!.Assigned); Assert.Equal(0, c.CompletedWork);
    }

    [Fact]
    public void RescueReturnsTheSamePersonAndDoesNotDuplicateRecruitment()
    {
        var g = Outside(); var original = g.Home.Crew[0]; Go(g, "survivor");
        string state = g.StateJson(); Assert.False(g.Interact("survivor").Success); Assert.Equal(state, g.StateJson());
        Go(g, "return"); Assert.Same(original, g.Home.Crew[0]);
        Assert.Equal(CampaignPhase.Arrival, g.Phase); Assert.Equal(2, g.Home.LivingCount); Assert.Equal(1, g.Home.RescuedCount);
        Assert.True(g.Home.Crew[1].Rest); Assert.Equal("Mika", g.Home.Crew[1].Name);
        string arrived = g.StateJson(); g.UpdateRealtime(100); Assert.Equal(arrived, g.StateJson());
        Assert.False(g.ReturnHome().Success); Assert.Equal(2, g.Home.Crew.Count);
        Ok(g.DismissArrival()); Assert.Contains(g.Sites, s => s.Id == "salvage");
    }

    [Fact]
    public void VariablePartyUsesStableIdsRatherThanFormationIndices()
    {
        var g = new Game();
        for (int i = 1; i < 20; i++) g.Home.AddColonist("Resident " + i, "Miner", "Quiet", g.Home.World.Center);
        Ok(g.Prepare("rescue-0")); Ok(g.Depart([7, 19]));
        Assert.Equal(new[] { 7, 19 }, g.ActiveField!.Party.Select(h => h.Id));
        var restored = Game.FromJson(g.StateJson()); Assert.Equal(g.StateJson(), restored.StateJson());
        Ok(g.Wait()); Ok(restored.Wait()); Assert.Equal(g.StateJson(), restored.StateJson());
    }

    [Theory]
    [InlineData(0)] [InlineData(42)] [InlineData(-13)]
    public void FullRescueSalvageAndBuiltUpgradeLoopUsesActualCommands(int seed)
    {
        var g = new Game(seed); Rescue(g);
        Ok(g.StepColony(80));
        Ok(g.Prepare("salvage")); Ok(g.Depart([0])); Go(g, "presence"); Go(g, "component");
        var before = g.StateJson(); Assert.False(g.Interact("component").Success); Assert.Equal(before, g.StateJson());
        Go(g, "return"); Ok(g.DismissArrival()); Assert.Equal(1, g.Home.Resources.Components);
        int cell = g.Home.World.Floors.First(i => g.Home.World.Breathable(i) && g.Home.MachineAt(i) is null && g.Home.JobAt(i) is null);
        Ok(g.OrderSuitService(g.Home.World.X(cell), g.Home.World.Y(cell)));
        for (int i = 0; !g.SupportUpgrade && i < 200; i++) Ok(g.StepColony());
        Assert.True(g.SupportUpgrade); Assert.Equal(0, g.Home.Resources.Components);
        Assert.Contains(g.Sites, s => s.Id == "below");
        Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Fact]
    public void SaveResumesAnEncounterWithoutRepeatingCostsOrRerollingMaps()
    {
        var g = new Game(); Rescue(g); Ok(g.StepColony(80)); Ok(g.Prepare("salvage")); Ok(g.Depart([0]));
        Go(g, "presence", false); Ok(g.Interact("presence")); Assert.NotNull(g.ActiveField!.Battle);
        var restored = Game.FromJson(g.StateJson()); Assert.Equal(g.StateJson(), restored.StateJson());
        while (g.ActiveField.Battle is { } battle)
        {
            int target = battle.Units.First(u => !u.Friendly && u.Alive).Id;
            Ok(g.Act(0, target)); Ok(restored.Act(0, target)); restored.UpdateRealtime(80);
            Assert.Equal(g.StateJson(), restored.StateJson());
        }
        Assert.True(g.Home.Crew[0].Health < 100);
    }

    [Fact]
    public void CorruptedRosterAndClockSnapshotsAreRejected()
    {
        var g = Outside(); var node = JsonNode.Parse(g.StateJson())!;
        node["partyIds"] = new JsonArray(999); Assert.Throws<InvalidDataException>(() => Game.FromJson(node.ToJsonString()));
        node = JsonNode.Parse(g.StateJson())!; node["clock"]!["speed"] = 99;
        Assert.Throws<InvalidDataException>(() => Game.FromJson(node.ToJsonString()));
    }

    [Fact]
    public void LunarGenerationPreservesRoutesAndSealPrerequisites()
    {
        for (int seed = -25; seed < 25; seed++)
            foreach (bool structure in new[] { false, true })
            {
                var zone = LunarGenerator.Create("test", seed, structure, false);
                var validation = LunarGenerator.Validate(zone); Assert.True(validation.Valid, string.Join("; ", validation.Errors));
                Assert.DoesNotContain(zone.Tiles, g => g is Ground.Grass or Ground.Tree or Ground.Water);
            }
    }
}
