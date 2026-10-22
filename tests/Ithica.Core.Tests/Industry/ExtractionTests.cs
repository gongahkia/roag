using System.Text.Json.Nodes;
using Ithica.Core;
using Ithica.Core.Colony;
using Ithica.Core.Diagnostics;
using Xunit;
using Game = Ithica.Core.Campaign;

namespace Ithica.Tests.Industry;

public sealed class ExtractionTests
{
    private static (Game Game, Machine Rig, DeepDeposit Deposit) Operation(bool secondResident = false)
    {
        var g = new Game(42); var s = g.Home; s.IncidentsEnabled = false;
        s.Resources = new(1000, 1000, 1000, 1000, 10, 100);
        s.EnableExtraction(); var d = s.Extraction!.Deposits[0]; d.Surveyed = true;
        int cx = s.World.X(d.Cell), cy = s.World.Y(d.Cell);
        for (int y = cy - 2; y <= cy + 2; y++) for (int x = cx - 2; x <= cx + 2; x++)
        {
            int cell = s.World.Index(x, y);
            s.World.SetTerrain(cell, Math.Abs(x - cx) == 2 || Math.Abs(y - cy) == 2 ? Terrain.Wall : Terrain.Floor);
            s.World.Oxygen[cell] = .9f; s.World.Heat[cell] = 19;
        }
        s.World.SetTerrain(s.World.Index(cx - 2, cy), Terrain.Airlock);
        s.AddMachine(ModuleKind.Rtg, s.World.Index(cx + 1, cy + 1));
        s.AddMachine(ModuleKind.Oxygen, s.World.Index(cx + 1, cy));
        s.AddMachine(ModuleKind.Bunk, s.World.Index(cx - 1, cy - 1));
        var rig = s.AddMachine(ModuleKind.Drill, d.Cell);
        s.Crew[0].Cell = s.World.Index(cx - 1, cy);
        if (secondResident) s.AddColonist("Inez", "Medic", "Steadfast", s.World.Center);
        s.UpdatePower();
        return (g, rig, d);
    }
    private static void Until(Game g, Func<bool> done, int limit = 600)
    {
        for (int t = 0; !done() && t < limit; t++) CampaignScenarios.Require(g.StepColony());
        Assert.True(done(), "Operation did not reach its expected result.");
    }

    [Theory]
    [InlineData(-7)] [InlineData(-1)] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(7)] [InlineData(42)] [InlineData(1337)]
    public void OrdinaryExpeditionsAndConstructionProduceOreAnUpgradeAndAComponent(int seed)
    {
        var g = ExtractionScenario.Run(seed);
        Assert.True(g.Home.Extraction!.Deposits[0].Extracted >= 8);
        Assert.Contains(g.Home.Machines, m => m.Kind == ModuleKind.Battery && m.Reinforced);
        Assert.Contains(g.Home.Machines, m => m.Kind == ModuleKind.Fabricator);
        Assert.True(g.Home.Resources.Components >= 1);
        Assert.Contains(g.Home.Crew, c => c.Memories.Any(m => m.Key == "fabrication"));
        Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Fact]
    public void SurveyAndConstructionValidateWithoutSpendingOrDrawingRandomness()
    {
        var g = new Game(); var s = g.Home;
        var original = g.StateJson();
        Assert.False(s.Designate(s.World.Width / 2, s.World.Height / 2, Tool.Build, ModuleKind.Drill).Success);
        Assert.False(g.OrderComponent(s.World.Center).Success);
        Assert.False(g.SetWorkPriority(0, WorkGroup.Mining, 4).Success);
        Assert.False(g.SetShift(0, (CrewShift)99).Success);
        Assert.Equal(original, g.StateJson());
        ulong rng = s.Random.State, storyRng = s.Story!.RandomState;
        s.EnableExtraction();
        Assert.Equal(rng, s.Random.State); Assert.Equal(storyRng, s.Story.RandomState);
        var d = s.Extraction!.Deposits[0];
        CampaignScenarios.Require(s.Designate(s.World.X(d.Cell), s.World.Y(d.Cell), Tool.Survey));
        Until(g, () => s.JobAt(d.Cell) is null);
        Assert.True(d.Surveyed);
        CampaignScenarios.Require(s.Designate(s.World.X(d.Cell), s.World.Y(d.Cell), Tool.Build, ModuleKind.Floor));
        Until(g, () => s.JobAt(d.Cell) is null);
        CampaignScenarios.Require(s.Designate(s.World.X(d.Cell), s.World.Y(d.Cell), Tool.Build, ModuleKind.Drill));
        Until(g, () => s.JobAt(d.Cell) is null);
        Assert.NotNull(s.MachineAt(d.Cell));
    }

    [Theory]
    [InlineData("power")] [InlineData("pressure")] [InlineData("refuge")] [InlineData("temperature")] [InlineData("switch")]
    public void UnsupportedOperationsDoNotProduceOre(string missing)
    {
        var (g, rig, d) = Operation(); var s = g.Home;
        if (missing == "power") s.Machines.Single(m => m.Kind == ModuleKind.Rtg && m.Cell != s.Machines.First(m => m.Kind == ModuleKind.Rtg).Cell).Fault = MachineFault.Offline;
        if (missing == "pressure") for (int i = 0; i < s.World.Count; i++) s.World.Oxygen[i] = 0;
        if (missing == "refuge") s.World.Oxygen[s.Machines.Single(m => m.Kind == ModuleKind.Bunk && s.World.Distance(m.Cell, d.Cell) < 3).Cell] = 0;
        if (missing == "temperature") s.World.Heat[d.Cell] = 60;
        if (missing == "switch") CampaignScenarios.Require(g.SetIndustryEnabled(d.Cell, false));
        s.UpdatePower(); Assert.NotNull(s.IndustrialBlock(rig));
        CampaignScenarios.Require(g.OrderExtraction(d.Cell));
        float ore = s.Resources.Ore;
        // A single tick cannot restore the missing support or complete a batch.
        CampaignScenarios.Require(g.StepColony());
        Assert.Equal(ore, s.Resources.Ore); Assert.Equal(0, d.Extracted);
    }

    [Fact]
    public void ExtractionUsesRealLaborAndStopsAtTheFiniteLayer()
    {
        var (g, rig, d) = Operation(); float ore = g.Home.Resources.Ore;
        CampaignScenarios.Require(g.OrderExtraction(d.Cell));
        Until(g, () => d.Extracted >= 4);
        Assert.Equal(ore + 4, g.Home.Resources.Ore); Assert.Equal(20, d.Remaining);
        Assert.True(g.Home.Crew[0].WorkTicks > 0); Assert.True(rig.Condition < 100);
        while (d.Remaining > 0)
        {
            if (rig.Condition < 55)
            {
                CampaignScenarios.Require(g.OrderMaintenance(d.Cell));
                Until(g, () => g.Home.JobAt(d.Cell) is null);
                CampaignScenarios.Require(g.OrderExtraction(d.Cell));
            }
            CampaignScenarios.Require(g.StepColony());
        }
        Assert.Equal(24, d.Extracted); Assert.Equal(ore + 24, g.Home.Resources.Ore);
        Assert.Null(g.Home.JobAt(d.Cell));
        string snapshot = g.StateJson(); Assert.False(g.OrderExtraction(d.Cell).Success); Assert.Equal(snapshot, g.StateJson());
        CampaignScenarios.Require(g.OrderDeepening(d.Cell)); Until(g, () => g.Home.JobAt(d.Cell) is null);
        Assert.Equal(2, d.Depth); Assert.Equal(48, d.Remaining); Assert.Equal(12, g.Home.PowerDraw(rig));
        Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Fact]
    public void CautiousPolicyPreventsTheInjuryThatWornDrillingCanCause()
    {
        var (safe, rig, d) = Operation(); rig.Condition = 25.1f;
        CampaignScenarios.Require(safe.OrderExtraction(d.Cell));
        CampaignScenarios.Require(safe.StepColony(8));
        Assert.False(safe.Home.Crew[0].Incapacitated); Assert.Equal(0, d.Extracted);
        var (risk, riskyRig, riskyDeposit) = Operation(true); riskyRig.Condition = 25.1f;
        CampaignScenarios.Require(risk.SetDrillingRisk(0, true));
        CampaignScenarios.Require(risk.OrderExtraction(riskyDeposit.Cell));
        Until(risk, () => risk.Home.Crew[0].Incapacitated);
        Assert.Equal(MachineFault.Offline, riskyRig.Fault); Assert.True(risk.Home.Crew[0].Health < 100);
        Assert.Contains(risk.Home.Crew[0].Memories, m => m.Key == "drilling-injury");
        Assert.Contains(risk.Home.Crew[1].Memories, m => m.Key == "unsafe-drilling");
        Assert.Equal(risk.StateJson(), Game.FromJson(risk.StateJson()).StateJson());
    }

    [Fact]
    public void RecoveryRequiresAnotherResidentAndCarriesThemThroughTheActualRoute()
    {
        var (g, rig, d) = Operation(true); var s = g.Home; var injured = s.Crew[0];
        rig.Condition = 25.1f; CampaignScenarios.Require(g.SetDrillingRisk(0, true));
        CampaignScenarios.Require(g.OrderExtraction(d.Cell)); Until(g, () => injured.Incapacitated);
        int oldCell = injured.Cell;
        var oxygen = s.Machines.Single(m => m.Kind == ModuleKind.Oxygen && s.World.Distance(m.Cell, d.Cell) < 3); oxygen.Enabled = false;
        for (int y = s.World.Y(d.Cell) - 1; y <= s.World.Y(d.Cell) + 1; y++)
            for (int x = s.World.X(d.Cell) - 1; x <= s.World.X(d.Cell) + 1; x++) s.World.Oxygen[s.World.Index(x, y)] = 0;
        s.UpdatePower();
        CampaignScenarios.Require(g.OrderResidentRescue(injured.Id));
        Assert.True(injured.Incapacitated); Assert.Equal(oldCell, injured.Cell);
        Until(g, () => s.Crew[1].CarryingResidentId == injured.Id);
        Assert.Equal(injured.Cell, s.Crew[1].Cell);
        string saved = g.StateJson(); var loaded = Game.FromJson(saved); Assert.Equal(saved, loaded.StateJson());
        CampaignScenarios.Require(g.StepColony()); CampaignScenarios.Require(loaded.StepColony()); Assert.Equal(g.StateJson(), loaded.StateJson());
        Until(g, () => !injured.Incapacitated);
        Assert.True(s.World.Breathable(injured.Cell, .5f)); Assert.NotEqual(oldCell, injured.Cell);
        Assert.True(injured.Rest); Assert.Contains(injured.Memories, m => m.Key == "rescued");
        Assert.True(injured.Bonds.Single(b => b.OtherId == 1).Trust >= 18);
        Assert.Equal(MachineFault.Offline, rig.Fault); // Carrying a person does not repair equipment.
    }

    [Theory]
    [InlineData(WorkKind.Fabricate)] [InlineData(WorkKind.Reinforce)] [InlineData(WorkKind.Deepen)] [InlineData(WorkKind.Rescue)]
    public void PaidOrdersRefundExactlyOnceWhenCanceled(WorkKind kind)
    {
        var (g, rig, d) = Operation(true); var s = g.Home; int cell = d.Cell;
        if (kind == WorkKind.Fabricate) { cell = s.World.Index(s.World.X(d.Cell), s.World.Y(d.Cell) + 1); s.AddMachine(ModuleKind.Fabricator, cell); s.UpdatePower(); }
        if (kind == WorkKind.Deepen) { d.Remaining = 0; d.Extracted = 24; }
        if (kind == WorkKind.Rescue) { s.Crew[0].Incapacitated = true; cell = s.Crew[0].Cell; }
        var before = s.Resources;
        CampaignScenarios.Require(kind switch { WorkKind.Fabricate => g.OrderComponent(cell), WorkKind.Reinforce => g.OrderReinforcement(cell), WorkKind.Deepen => g.OrderDeepening(cell), _ => g.OrderResidentRescue(0) });
        var job = s.JobAt(cell)!; Assert.NotEqual(before, s.Resources);
        Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
        s.Cancel(job); s.Cancel(job); Assert.Equal(before, s.Resources);
    }

    [Fact]
    public void DestroyingAReservedFabricatorRefundsAndReleasesItsWorker()
    {
        var (g, _, d) = Operation(); var s = g.Home;
        int cell = s.World.Index(s.World.X(d.Cell), s.World.Y(d.Cell) + 1);
        s.AddMachine(ModuleKind.Fabricator, cell); s.UpdatePower(); var before = s.Resources;
        CampaignScenarios.Require(g.OrderComponent(cell)); s.Meteor(cell);
        Assert.Null(s.JobAt(cell)); Assert.Equal(before, s.Resources);
        Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Fact]
    public void ComponentRecipeNeedsLaborConsumesItsReservationOnceAndDoesNotRepeat()
    {
        var (g, rig, d) = Operation(); var s = g.Home;
        int cell = s.World.Index(s.World.X(d.Cell), s.World.Y(d.Cell) + 1);
        s.AddMachine(ModuleKind.Fabricator, cell); CampaignScenarios.Require(g.SetIndustryEnabled(rig.Cell, false));
        var before = s.Resources; CampaignScenarios.Require(g.OrderComponent(cell));
        Assert.Equal(before - Simulation.ComponentRecipe, s.Resources);
        CampaignScenarios.Require(g.AssignRest(0, 24)); CampaignScenarios.Require(g.StepColony(20));
        Assert.Equal(before.Components, s.Resources.Components);
        Until(g, () => s.JobAt(cell) is null);
        Assert.Equal(before.Components + 1, s.Resources.Components);
        float ore = s.Resources.Ore; CampaignScenarios.Require(g.StepColony(80));
        Assert.Equal(ore, s.Resources.Ore); Assert.Equal(before.Components + 1, s.Resources.Components);
    }

    [Fact]
    public void ReinforcedLifeSupportSavesWaterAndReinforcedBatteriesSaveTheirLargerCapacity()
    {
        var (g, _, d) = Operation(); var s = g.Home;
        var oxygen = s.Machines.Single(m => m.Kind == ModuleKind.Oxygen && s.World.Distance(m.Cell, d.Cell) < 3);
        CampaignScenarios.Require(g.OrderReinforcement(oxygen.Cell)); Until(g, () => s.JobAt(oxygen.Cell) is null);
        Assert.True(oxygen.Reinforced);
        var control = Game.FromJson(g.StateJson()); control.Home.MachineAt(oxygen.Cell)!.Reinforced = false;
        CampaignScenarios.Require(g.StepColony(32)); CampaignScenarios.Require(control.StepColony(32));
        Assert.True(s.Resources.Water > control.Home.Resources.Water + .15f);
        var battery = s.Machines.Single(m => m.Kind == ModuleKind.Battery);
        CampaignScenarios.Require(g.OrderReinforcement(battery.Cell)); Until(g, () => s.JobAt(battery.Cell) is null);
        Assert.True(battery.Reinforced); Assert.Equal(360, Simulation.BatteryCapacity(battery));
        battery.Energy = 300; s.UpdatePower(); Assert.True(s.Capacity >= 360);
        Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Fact]
    public void ExtendedWornDrillingCreatesFatigueInjuryAndMemoriesFromActualWork()
    {
        var (g, _, d) = Operation(); var c = g.Home.Crew[0];
        CampaignScenarios.Require(g.SetShift(0, CrewShift.Extended)); CampaignScenarios.Require(g.SetDrillingRisk(0, true));
        CampaignScenarios.Require(g.OrderExtraction(d.Cell)); Until(g, () => c.Incapacitated, 400);
        Assert.True(c.WorkTicks >= 120); Assert.True(c.Energy < 35);
        Assert.Contains(c.Memories, m => m.Key == "overworked"); Assert.Contains(c.Memories, m => m.Key == "drilling-injury");
        Assert.True(d.Extracted > 0);
    }

    [Fact]
    public void FinalLayerHasNoFourthLayerOrDuplicateOre()
    {
        var (g, rig, d) = Operation(); d.Depth = 3; d.Extracted = 144; d.Remaining = 0;
        g.Home.UpdatePower();
        string saved = g.StateJson(); Assert.False(g.OrderDeepening(d.Cell).Success); Assert.False(g.OrderExtraction(d.Cell).Success);
        Assert.Equal(saved, g.StateJson()); Assert.Equal(16, g.Home.PowerDraw(rig));
        Assert.Equal(saved, Game.FromJson(saved).StateJson());
    }

    [Fact]
    public void ACarrierDeathDropsTheResidentAndAllowsAnotherPaidRecovery()
    {
        var (g, _, d) = Operation(true); var s = g.Home; var target = s.Crew[0]; var carrier = s.Crew[1];
        target.Incapacitated = true;
        carrier.Cell = target.Cell; carrier.CarryingResidentId = target.Id; carrier.Health = .01f; carrier.Hunger = 100;
        int dropped = target.Cell; CampaignScenarios.Require(g.StepColony());
        Assert.False(carrier.Alive); Assert.Null(carrier.CarryingResidentId); Assert.True(target.Incapacitated); Assert.Equal(dropped, target.Cell);
        CampaignScenarios.Require(g.OrderResidentRescue(target.Id));
        Assert.NotNull(s.JobAt(target.Cell)); Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Fact]
    public void WornRigServiceInterruptsExtractionAndLeavesTheOperatorSafe()
    {
        var (g, rig, d) = Operation(); CampaignScenarios.Require(g.OrderExtraction(d.Cell)); CampaignScenarios.Require(g.StepColony(12));
        Assert.True(g.Home.JobAt(d.Cell)!.Progress > 0);
        float ore = g.Home.Resources.Ore; CampaignScenarios.Require(g.OrderMaintenance(d.Cell));
        Assert.Equal(WorkKind.Maintain, g.Home.JobAt(d.Cell)!.Kind);
        Until(g, () => g.Home.JobAt(d.Cell) is null);
        Assert.InRange(rig.Condition, 99, 100); Assert.Equal(ore, g.Home.Resources.Ore); Assert.False(g.Home.Crew[0].Incapacitated);
        CampaignScenarios.Require(g.OrderExtraction(d.Cell)); Until(g, () => g.Home.Resources.Ore > ore);
    }

    [Fact]
    public void DepthThreeRequiresItsActualPowerAndStillProducesItsLargerBatch()
    {
        var (g, rig, d) = Operation(); var s = g.Home;
        d.Depth = 3; d.Remaining = 72; d.Extracted = 72; s.UpdatePower();
        Assert.False(rig.Powered); Assert.Equal(16, s.PowerDraw(rig));
        int cell = s.World.Index(s.World.X(d.Cell) - 1, s.World.Y(d.Cell) + 1);
        s.AddMachine(ModuleKind.Rtg, cell); s.UpdatePower(); Assert.True(rig.Powered);
        float ore = s.Resources.Ore; CampaignScenarios.Require(g.OrderExtraction(d.Cell)); Until(g, () => d.Extracted > 72);
        Assert.Equal(ore + 8, s.Resources.Ore); Assert.Equal(64, d.Remaining);
        Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Fact]
    public void ReinforcedSuitServiceChangesTheNextDepartureProjection()
    {
        var g = new Game(); var s = g.Home;
        var rack = s.AddMachine(ModuleKind.SuitRack, s.World.Center); rack.Reinforced = true; s.UpdatePower();
        CampaignScenarios.Require(g.Prepare("rescue-0")); CampaignScenarios.Require(g.Depart([0]));
        Assert.Equal(.5f, s.Crew[0].SuitEfficiency); Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Theory]
    [InlineData("deposit")] [InlineData("priority")] [InlineData("shift")] [InlineData("parts")] [InlineData("carrier")]
    public void InvalidIndustryOrWorkStateIsRejected(string field)
    {
        var (g, _, d) = Operation(); CampaignScenarios.Require(g.OrderExtraction(d.Cell));
        var json = JsonNode.Parse(g.StateJson())!; var h = json["home"]!;
        switch (field)
        {
            case "deposit": h["extraction"]!["deposits"]![0]!["remaining"] = -1; break;
            case "priority": h["crew"]![0]!["work"]!["priorities"]![0] = 4; break;
            case "shift": h["crew"]![0]!["work"]!["shift"] = 99; break;
            case "parts": h["jobs"]![0]!["reserved"] = true; break;
            case "carrier": h["crew"]![0]!["carryingResidentId"] = 0; break;
        }
        Assert.Throws<InvalidDataException>(() => Game.FromJson(json.ToJsonString()));
    }

    [Fact]
    public void HistoricalSavesWithoutWorkAndExtractionFieldsKeepTheirStateAndDefaultPolicies()
    {
        var g = new Game(); var json = JsonNode.Parse(g.StateJson())!; var home = json["home"]!;
        home.AsObject().Remove("extraction"); home["resources"]!.AsObject().Remove("ore");
        foreach (var c in home["crew"]!.AsArray()) { c!.AsObject().Remove("work"); c.AsObject().Remove("incapacitated"); c.AsObject().Remove("carryingResidentId"); }
        foreach (var m in home["machines"]!.AsArray()) { m!.AsObject().Remove("reinforced"); m.AsObject().Remove("enabled"); }
        var loaded = Game.FromJson(json.ToJsonString());
        Assert.Null(loaded.Home.Extraction); Assert.Equal(0, loaded.Home.Resources.Ore);
        Assert.Equal(CrewShift.Flexible, loaded.Home.Crew[0].Work.Shift); Assert.All(loaded.Home.Machines, m => Assert.True(m.Enabled));
        Assert.Equal(g.Home.Random.State, loaded.Home.Random.State); Assert.Equal(g.Home.Resources, loaded.Home.Resources);
    }

    [Fact]
    public void ActiveMiningAndStaffingContinueExactlyAfterSaving()
    {
        var (g, _, d) = Operation(true); CampaignScenarios.Require(g.SetWorkPriority(1, WorkGroup.Mining, 0));
        CampaignScenarios.Require(g.OrderExtraction(d.Cell)); CampaignScenarios.Require(g.StepColony(12));
        var loaded = Game.FromJson(g.StateJson());
        for (int n = 0; n < 400; n++) { CampaignScenarios.Require(g.StepColony()); CampaignScenarios.Require(loaded.StepColony()); Assert.Equal(g.StateJson(), loaded.StateJson()); }
    }

    [Fact]
    public void AwayResidentsDoNotOperateHomeIndustryAndFieldDecisionsStayFrozen()
    {
        var (g, _, d) = Operation(); CampaignScenarios.Require(g.OrderExtraction(d.Cell));
        CampaignScenarios.Require(g.Prepare("rescue-0")); CampaignScenarios.Require(g.Depart([0]));
        var saved = g.StateJson(); Assert.False(g.SetShift(0, CrewShift.Rest).Success); Assert.Equal(saved, g.StateJson());
        g.UpdateRealtime(60); Assert.Equal(saved, g.StateJson());
        for (int n = 0; n < 16; n++) CampaignScenarios.Require(g.Wait());
        Assert.Equal(0, d.Extracted); Assert.Equal(0, g.Home.JobAt(d.Cell)!.Progress); Assert.Equal(0, g.Home.Crew[0].WorkTicks);
    }
}
