using Ithica.Core;
using Ithica.Core.Colony;
using Ithica.Core.Diagnostics;
using Xunit;
using Game = Ithica.Core.Campaign;

namespace Ithica.Tests.Integration;

public sealed class ContinuationTests
{
    [Theory]
    [InlineData("colony")] [InlineData("preparation")] [InlineData("surface")]
    [InlineData("battle")] [InlineData("arrival")] [InlineData("structure")]
    public void EveryPhaseResumesWithoutRegeneratingOrAdvancing(string scene)
    {
        var game = new Game(); CampaignScenarios.OpenScene(game, scene);
        var restored = Game.FromJson(game.StateJson());
        Assert.Equal(game.StateJson(), restored.StateJson());
        if (game.Phase != CampaignPhase.Colony)
        {
            restored.UpdateRealtime(3600);
            Assert.Equal(game.StateJson(), restored.StateJson());
        }
    }

    [Fact]
    public void ColonyAccumulatesEightTicksPerSecondAndPreservesItsRemainder()
    {
        var a = new Game(); var b = new Game(); a.Clock.Paused = b.Clock.Paused = false;
        for (int i = 0; i < 16; i++) a.UpdateRealtime(.0625);
        for (int i = 0; i < 8; i++) b.UpdateRealtime(.125);
        Assert.Equal(8, a.CampaignTick); Assert.Equal(a.StateJson(), b.StateJson());
        a.UpdateRealtime(.0625); var saved = Game.FromJson(a.StateJson());
        a.UpdateRealtime(.0625); saved.UpdateRealtime(.0625);
        Assert.Equal(a.StateJson(), saved.StateJson());
    }

    [Fact]
    public void ReturnChargesTravelOnceAndPreservesThePriorPauseAndSpeed()
    {
        var game = CampaignTests.Outside(); game.Clock.Speed = 3; game.Clock.Paused = false;
        long before = game.CampaignTick; CampaignScenarios.Require(game.ReturnHome());
        Assert.Equal(before + ActionCosts.Travel, game.CampaignTick);
        game.UpdateRealtime(60); Assert.Equal(before + ActionCosts.Travel, game.CampaignTick);
        CampaignScenarios.Require(game.DismissArrival());
        Assert.False(game.Clock.Paused); Assert.Equal(3, game.Clock.Speed); Assert.Equal(0, game.Clock.Accumulator);
    }

    [Fact]
    public void MedicineAndOxygenSuppliesSpendActualTimeAndCannotBeAppliedTwiceForFree()
    {
        var game = CampaignTests.Outside(); var resident = game.Home.Crew[0]; resident.Health = 50;
        long before = game.CampaignTick; int kits = game.Pack.Medicine;
        CampaignScenarios.Require(game.Treat(resident.Id));
        Assert.Equal(before + ActionCosts.Treatment, game.CampaignTick); Assert.Equal(kits - 1, game.Pack.Medicine);
        Assert.Equal(74, resident.Health, 3);
        int water = game.Pack.Water; before = game.CampaignTick;
        CampaignScenarios.Require(game.Refill());
        Assert.Equal(before + ActionCosts.Refill, game.CampaignTick); Assert.Equal(water - 1, game.Pack.Water);
        Assert.InRange(resident.Suit, 99, 100);
        string saved = game.StateJson(); Assert.Equal(saved, Game.FromJson(saved).StateJson());
    }

    [Fact]
    public void ExhaustionCausesOneInjuredReturnRatherThanEndlessZeroOxygenTravel()
    {
        var game = CampaignTests.Outside(); var resident = game.Home.Crew[0];
        resident.Health = 5; resident.Suit = 0;
        long tick = game.CampaignTick;
        CampaignScenarios.Require(game.Refill());
        Assert.Equal(CampaignPhase.Arrival, game.Phase);
        Assert.Equal(tick + ActionCosts.Refill + ActionCosts.Travel, game.CampaignTick);
        Assert.Equal(ResidentLocation.Home, resident.Location); Assert.True(resident.Rest); Assert.True(resident.Alive);
        Assert.Equal(default, game.Cargo); Assert.Empty(game.PartyIds);
        Assert.Equal(game.StateJson(), Game.FromJson(game.StateJson()).StateJson());
    }

    [Fact]
    public void InactiveSitesFreezeAndRevisitsPreserveClearedEncountersAndLoot()
    {
        var game = new Game(); CampaignScenarios.FirstLoop(game);
        var site = game.Sites.Single(s => s.Id == "salvage");
        string state = site.Session!.StateJson();
        CampaignScenarios.Require(game.StepColony(80)); Assert.Equal(state, site.Session.StateJson());
        CampaignScenarios.Require(game.Prepare(site.Id)); CampaignScenarios.Require(game.Depart([0]));
        Assert.Same(site.Session, game.ActiveField);
        Assert.Contains("presence", game.ActiveField!.Completed); Assert.Contains("component", game.ActiveField.Completed);
        CampaignScenarios.Approach(game, "component", interact: false);
        long tick = game.CampaignTick;
        Assert.False(game.Interact("component").Success); Assert.Equal(tick, game.CampaignTick); Assert.Equal(0, game.Cargo.Components);
    }

    [Fact]
    public void DeepSiteRepairOpensSealPersistsAndProducesWaterAtHome()
    {
        var game = new Game(); CampaignScenarios.OpenScene(game, "structure");
        Assert.All(game.Party, c => Assert.Equal(.65f, c.SuitEfficiency));
        CampaignScenarios.Approach(game, "seal", interact: false);
        string before = game.StateJson(); Assert.False(game.Interact("seal").Success); Assert.Equal(before, game.StateJson());
        CampaignScenarios.Approach(game, "terminal");
        Assert.True(game.SelectedSite!.Secured);
        var restored = Game.FromJson(game.StateJson()); Assert.Equal(game.StateJson(), restored.StateJson());
        int water = game.Pack.Water; Assert.False(game.Interact("terminal").Success); Assert.Equal(water, game.Pack.Water);
        CampaignScenarios.Approach(game, "seal"); CampaignScenarios.Approach(game, "presence-heavy");
        CampaignScenarios.Approach(game, "component"); CampaignScenarios.Approach(game, "return");
        CampaignScenarios.Require(game.DismissArrival());
        Assert.Equal(1, game.Home.Resources.Components); Assert.True(game.Sites.Single(s => s.Id == "below").Secured);
        float homeWater = game.Home.Resources.Water; CampaignScenarios.Require(game.StepColony(64));
        Assert.True(game.Home.Resources.Water > homeWater); Assert.Equal(game.StateJson(), Game.FromJson(game.StateJson()).StateJson());
    }

    [Fact]
    public void NonSequentialResidentIdsAlsoSurviveCombatAndInjuryContinuation()
    {
        var game = new Game(); CampaignScenarios.Rescue(game); CampaignScenarios.Require(game.StepColony(80));
        for (int i = game.Home.Crew.Count; i < 20; i++) game.Home.AddColonist("Resident " + i, "Miner", "Quiet", game.Home.World.Center);
        CampaignScenarios.Require(game.Prepare("salvage")); CampaignScenarios.Require(game.Depart([7, 19]));
        CampaignScenarios.Approach(game, "presence", fight: false);
        Assert.Equal(new[] { 7, 19 }, game.ActiveField!.Battle!.Units.Where(u => u.Friendly).Select(u => u.HeroId));
        Assert.Equal(game.StateJson(), Game.FromJson(game.StateJson()).StateJson());
        CampaignScenarios.Fight(game);
        Assert.Contains(game.Party, c => c.Health < 100); Assert.Equal(game.StateJson(), Game.FromJson(game.StateJson()).StateJson());
    }

    [Fact]
    public void LegacyColonyImportLeavesTheOriginalFileUnchanged()
    {
        string file = Path.Combine(Path.GetTempPath(), "ithica-import-" + Guid.NewGuid().ToString("N") + ".json");
        string target = file + ".ithica.json";
        try
        {
            var original = new Simulation(13); original.Step(20); original.Save(file);
            string source = File.ReadAllText(file); var imported = Game.ImportColony(file); imported.Save(target);
            Assert.Equal(source, File.ReadAllText(file)); Assert.Equal(original.Tick, imported.CampaignTick);
            Assert.Equal(original.World.Tiles, imported.Home.World.Tiles); Assert.Equal(original.Random.State, imported.Home.Random.State);
            Assert.Equal(imported.StateJson(), Game.Load(target).StateJson());
        }
        finally { File.Delete(file); File.Delete(target); }
    }

    [Fact]
    public void CancelingAComponentBlueprintRefundsTheComponentAsWellAsBasicStock()
    {
        var game = new Game(); CampaignScenarios.Rescue(game); CampaignScenarios.Require(game.StepColony(80));
        CampaignScenarios.Require(game.Prepare("salvage")); CampaignScenarios.Require(game.Depart([0]));
        CampaignScenarios.Approach(game, "presence"); CampaignScenarios.Approach(game, "component"); CampaignScenarios.Approach(game, "return");
        CampaignScenarios.Require(game.DismissArrival());
        var stock = game.Home.Resources; int cell = game.Home.World.Floors.First(i => game.Home.MachineAt(i) is null && game.Home.JobAt(i) is null);
        CampaignScenarios.Require(game.OrderSuitService(game.Home.World.X(cell), game.Home.World.Y(cell)));
        CampaignScenarios.Require(game.Home.Designate(game.Home.World.X(cell), game.Home.World.Y(cell), Tool.Cancel));
        Assert.Equal(stock, game.Home.Resources);
    }
}
