using System.Text.Json.Nodes;
using Ithica.Core;
using Ithica.Core.Colony;
using Ithica.Core.Diagnostics;
using Xunit;
using Game = Ithica.Core.Campaign;

namespace Ithica.Tests.Story;

public sealed class StationStoryTests
{
    private static Game Shelter()
    {
        var game = new Game(); game.Home.IncidentsEnabled = false;
        return game;
    }
    private static Incident Open(Game game, string definition)
    {
        var s = game.Home.Story!;
        var d = Simulation.IncidentLibrary.Single(d => d.Id == definition);
        foreach (var other in Simulation.IncidentLibrary.Where(d => d.Id != definition)) s.Cooldowns[other.Id] = 100000;
        s.NextBeat = Math.Max(game.CampaignTick + 1, d.MinTick);
        game.Home.IncidentsEnabled = true;
        CampaignScenarios.Require(game.StepColony((int)(s.NextBeat - game.CampaignTick)));
        game.Home.IncidentsEnabled = false;
        return Assert.Single(s.Pending, i => i.DefinitionId == definition);
    }
    private static Machine Pump(Game game) => game.Home.Machines.Single(m => m.Kind == ModuleKind.Oxygen);
    private static void Finish(Game game, int cell, int limit = 180)
    {
        for (int n = 0; game.Home.JobAt(cell) is not null && n < limit; n++) CampaignScenarios.Require(game.StepColony());
        Assert.Null(game.Home.JobAt(cell));
    }

    [Fact]
    public void DeferredPumpWarningLeaksRealWaterAndPaidWorkRestoresIt()
    {
        var game = Shelter(); var i = Open(game, "pump"); var m = Pump(game);
        CampaignScenarios.Require(game.RespondToIncident(i.Id, "defer"));
        Assert.Equal(MachineFault.Leak, m.Fault);
        var healthy = Game.FromJson(game.StateJson()); Pump(healthy).Fault = MachineFault.None;
        float start = game.Home.Resources.Water;
        CampaignScenarios.Require(game.StepColony(12)); CampaignScenarios.Require(healthy.StepColony(12));
        Assert.True(healthy.Home.Resources.Water > game.Home.Resources.Water + .4f);
        Assert.True(game.Home.Resources.Water < start);
        var before = game.Home.Resources;
        CampaignScenarios.Require(game.OrderMaintenance(m.Cell));
        Assert.Equal(before - new Stock(6, 0, 0, 1), game.Home.Resources);
        Assert.Equal(MachineFault.Leak, m.Fault); // Queuing is not instant repair.
        Finish(game, m.Cell);
        Assert.Equal(MachineFault.None, m.Fault); Assert.InRange(m.Condition, 99, 100);
        Assert.Contains(game.Home.Crew[0].Memories, m => m.Key == "repair");
    }

    [Fact]
    public void PreventiveServiceAvoidsTheFaultAndIsCheaper()
    {
        var game = Shelter(); var i = Open(game, "pump"); var before = game.Home.Resources;
        CampaignScenarios.Require(game.RespondToIncident(i.Id, "service"));
        Assert.Equal(before - new Stock(2, 0, 0, 0), game.Home.Resources);
        Finish(game, Pump(game).Cell);
        CampaignScenarios.Require(game.StepColony((int)(i.DueAt - game.CampaignTick + 1)));
        Assert.Equal(MachineFault.None, Pump(game).Fault);
        Assert.Empty(game.Home.Story!.Followups);
        Assert.Contains(game.Home.Log, n => n.Text.Contains("without a leak"));
    }

    [Theory]
    [InlineData("pump")] [InlineData("relay")]
    public void DirectServiceOfAWarnedModulePreventsItsPendingFault(string definition)
    {
        var game = Shelter(); var i = Open(game, definition);
        CampaignScenarios.Require(game.OrderMaintenance(i.Cell!.Value));
        Finish(game, i.Cell.Value);
        Assert.DoesNotContain(game.Home.Story!.Pending, p => p.Id == i.Id);
        CampaignScenarios.Require(game.StepColony((int)(i.DueAt - game.CampaignTick + 1)));
        Assert.Equal(MachineFault.None, game.Home.MachineAt(i.Cell.Value)!.Fault);
    }

    [Fact]
    public void IgnoringAWarningUsesLogicalTimeAndStillLeavesARepairableMachine()
    {
        var game = Shelter(); var i = Open(game, "pump");
        string frozen = game.StateJson(); game.UpdateRealtime(3600); Assert.Equal(frozen, game.StateJson());
        CampaignScenarios.Require(game.StepColony((int)(i.DueAt - game.CampaignTick)));
        Assert.Equal(MachineFault.Leak, Pump(game).Fault);
        Assert.Contains(game.Home.Story!.History, h => h.Id == i.Id && h.Outcome.StartsWith("No response"));
        Assert.True(game.Home.Story.RecoveryUntil > game.CampaignTick);
    }

    [Fact]
    public void AQueuedPreventiveJobDoesNotSatisfyADeadlineWithoutAWorker()
    {
        var game = Shelter(); var i = Open(game, "pump");
        CampaignScenarios.Require(game.RespondToIncident(i.Id, "service"));
        game.Home.Crew[0].Rest = true; game.Home.Crew[0].Energy = 0; game.Home.Crew[0].BreakUntil = i.DueAt + 100;
        CampaignScenarios.Require(game.StepColony((int)(i.DueAt - game.CampaignTick)));
        Assert.Equal(MachineFault.Leak, Pump(game).Fault);
        game.Home.Crew[0].BreakUntil = 0; game.Home.Crew[0].Rest = false;
        var resources = game.Home.Resources;
        Finish(game, Pump(game).Cell);
        Assert.True(game.Home.Resources.Crystal <= resources.Crystal - 1); // Major repair upgrades the reserved parts.
    }

    [Fact]
    public void ServiceCancellationRefundsExactlyOnce()
    {
        var game = Shelter(); Pump(game).Condition = 40;
        var before = game.Home.Resources;
        CampaignScenarios.Require(game.OrderMaintenance(Pump(game).Cell));
        var job = Assert.Single(game.Home.Jobs);
        game.Home.Cancel(job); game.Home.Cancel(job);
        Assert.Equal(before, game.Home.Resources);
    }

    [Fact]
    public void DestroyedServiceTargetRefundsItsReservationAndReleasesItsWorker()
    {
        var game = Shelter(); var m = Pump(game); m.Condition = 40;
        CampaignScenarios.Require(game.OrderMaintenance(m.Cell));
        var before = game.Home.Resources;
        game.Home.Meteor(m.Cell); CampaignScenarios.Require(game.StepColony());
        Assert.Null(game.Home.JobAt(m.Cell)); Assert.Null(game.Home.Crew[0].Job);
        Assert.Equal(before.Metal + 6, game.Home.Resources.Metal);
        Assert.Equal(before.Crystal + 1, game.Home.Resources.Crystal);
    }

    [Fact]
    public void OfflineGenerationAndLifeSupportReallyStopProducing()
    {
        var game = Shelter(); var rtg = game.Home.Machines.Single(m => m.Kind == ModuleKind.Rtg);
        rtg.Fault = MachineFault.Offline; Pump(game).Fault = MachineFault.Offline;
        game.Home.UpdatePower(); Assert.Equal(0, game.Home.Generated); Assert.False(Pump(game).Powered);
        var before = game.Home.Resources.Water; game.Home.Step();
        Assert.InRange(before - game.Home.Resources.Water, 0, .02f); // Farms only; no oxygen water draw.
    }

    [Fact]
    public void ConditionFallsFromUseAndPoorConditionsAccelerateIt()
    {
        var game = Shelter(); var normal = Pump(game); var harsh = Game.FromJson(game.StateJson());
        harsh.Home.StormUntil = 100;
        CampaignScenarios.Require(game.StepColony(50)); CampaignScenarios.Require(harsh.StepColony(50));
        Assert.True(normal.Condition < 100); Assert.True(Pump(harsh).Condition < normal.Condition);
    }

    [Fact]
    public void FatigueDisputeUsesActualWorkAndRestChangesLaborAndTrust()
    {
        var game = Shelter(); game.Home.AddColonist("Sol", "Botanist", "Optimist", game.Home.World.Center);
        var tired = game.Home.Crew[0]; tired.WorkTicks = 160; tired.Energy = 38;
        var i = Open(game, "workload");
        Assert.Equal(tired.Id, i.ResidentId);
        CampaignScenarios.Require(game.RespondToIncident(i.Id, "mediate"));
        Assert.True(tired.BreakUntil > game.CampaignTick); Assert.True(tired.Rest); Assert.Null(tired.Job);
        Assert.Contains(tired.Memories, m => m.Key == "heard");
        Assert.True(Assert.Single(tired.Bonds).Trust > 0);
        Assert.Equal(game.StateJson(), Game.FromJson(game.StateJson()).StateJson());
    }

    [Fact]
    public void DismissedDisputesPersistAsResentmentAndDistrust()
    {
        var game = Shelter(); game.Home.AddColonist("Sol", "Botanist", "Optimist", game.Home.World.Center);
        var tired = game.Home.Crew[0]; tired.WorkTicks = 160; tired.Energy = 38;
        var i = Open(game, "workload"); CampaignScenarios.Require(game.RespondToIncident(i.Id, "dismiss"));
        Assert.Contains(tired.Memories, m => m.Key == "resentment" && m.OtherId == 1);
        Assert.True(Assert.Single(tired.Bonds).Trust < 0);
        Assert.True(game.Home.Forecast.Recovering);
    }

    [Fact]
    public void TradeAndPositiveChoicesCannotBeClaimedTwice()
    {
        var game = Shelter(); var i = Open(game, "hauler"); var before = game.Home.Resources;
        CampaignScenarios.Require(game.RespondToIncident(i.Id, "trade"));
        Assert.Equal(before + new Stock(-12, 24, 0, 0), game.Home.Resources);
        string once = game.StateJson(); Assert.False(game.RespondToIncident(i.Id, "trade").Success); Assert.Equal(once, game.StateJson());
    }

    [Fact]
    public void InvalidOrUnaffordableResponsesDoNotConsumeTimeRandomnessOrStock()
    {
        var game = Shelter(); var i = Open(game, "hauler"); game.Home.Resources = default;
        string before = game.StateJson();
        Assert.False(game.RespondToIncident(i.Id, "trade").Success);
        Assert.False(game.RespondToIncident(i.Id, "unknown").Success);
        Assert.False(game.RespondToIncident(-1, "decline").Success);
        Assert.False(game.OrderMaintenance(-1).Success);
        Assert.Equal(before, game.StateJson());
    }

    [Fact]
    public void SeedCultureNeedsAFunctioningFarmAtTheFollowup()
    {
        var game = Shelter(); var i = Open(game, "seed");
        CampaignScenarios.Require(game.RespondToIncident(i.Id, "cultivate"));
        var failed = Game.FromJson(game.StateJson()); failed.Home.MachineAt(i.Cell!.Value)!.Fault = MachineFault.Offline;
        CampaignScenarios.Require(game.StepColony(72)); CampaignScenarios.Require(failed.StepColony(72));
        Assert.Contains(game.Home.Log, n => n.Text.Contains("+8 food"));
        Assert.Contains(failed.Home.Log, n => n.Text.Contains("culture failed"));
        Assert.Empty(game.Home.Story!.Followups);
    }

    [Fact]
    public void LockerRewardRequiresACompletedSurveyAndTransfersOnlyOnce()
    {
        var game = Shelter(); var i = Open(game, "cache"); var before = game.Home.Resources;
        CampaignScenarios.Require(game.RespondToIncident(i.Id, "survey"));
        Assert.Equal(before, game.Home.Resources);
        Assert.Single(game.Home.Story!.Followups);
        Finish(game, i.Cell!.Value);
        Assert.Empty(game.Home.Story.Followups);
        Assert.Contains(game.Home.Log, n => n.Text.Contains("recovered the old locker"));
        var metal = game.Home.Resources.Metal; CampaignScenarios.Require(game.StepColony(20)); Assert.Equal(metal, game.Home.Resources.Metal);
    }

    [Fact]
    public void SavedWarningsJobsSocialStateAndDirectorContinueExactly()
    {
        var game = Shelter(); var i = Open(game, "pump"); CampaignScenarios.Require(game.RespondToIncident(i.Id, "service"));
        CampaignScenarios.Require(game.StepColony(8)); var loaded = Game.FromJson(game.StateJson());
        Assert.Equal(game.StateJson(), loaded.StateJson());
        game.Home.IncidentsEnabled = loaded.Home.IncidentsEnabled = true;
        game.Home.Resources = loaded.Home.Resources = new Stock(500, 500, 500, 50);
        for (int n = 0; n < 600; n++)
        {
            CampaignScenarios.Require(game.StepColony()); CampaignScenarios.Require(loaded.StepColony());
            Assert.Equal(game.StateJson(), loaded.StateJson());
        }
    }

    [Fact]
    public void NewDirectorDoesNotConsumeEnvironmentRandomnessAtSelection()
    {
        var game = Shelter(); var i = Open(game, "hauler");
        ulong before = game.Home.Random.State;
        CampaignScenarios.Require(game.RespondToIncident(i.Id, "decline"));
        Assert.Equal(before, game.Home.Random.State);
        var a = Shelter(); var b = Shelter(); a.Home.IncidentsEnabled = true;
        // No new distress spawn before tick 180. First selection uses only its independent stream.
        CampaignScenarios.Require(a.StepColony(120)); CampaignScenarios.Require(b.StepColony(120));
        Assert.Equal(a.Home.Random.State, b.Home.Random.State);
        Assert.NotEqual(a.Home.Story!.RandomState, b.Home.Story!.RandomState);
    }

    [Fact]
    public void RecoveryWindowPreventsNewHostileEvents()
    {
        var game = Shelter(); game.Home.Story!.RecoveryUntil = 10000;
        game.Home.Resources = new Stock(500, 500, 500, 50); game.Home.IncidentsEnabled = true;
        for (int n = 0; n < 1000; n++)
        {
            CampaignScenarios.Require(game.StepColony());
            Assert.All(game.Home.Story.Pending, i => Assert.Equal(0, Simulation.IncidentLibrary.Single(d => d.Id == i.DefinitionId).Severity));
        }
    }

    [Fact]
    public void FieldDecisionsFreezeWarningsAndRejectStationOrdersWithoutMutation()
    {
        var game = Shelter(); var i = Open(game, "hauler");
        CampaignScenarios.Require(game.Prepare("rescue-0")); CampaignScenarios.Require(game.Depart([0]));
        string before = game.StateJson(); game.UpdateRealtime(3600);
        Assert.False(game.RespondToIncident(i.Id, "trade").Success); Assert.False(game.OrderMaintenance(Pump(game).Cell).Success);
        Assert.Equal(before, game.StateJson());
        CampaignScenarios.Require(game.Wait()); Assert.True(game.CampaignTick > i.OpenedAt);
        Assert.Equal(game.StateJson(), Game.FromJson(game.StateJson()).StateJson());
    }

    [Fact]
    public void LegacySavesWithoutStoryFieldsMigrateWithoutLosingTheColony()
    {
        var game = Shelter(); var json = JsonNode.Parse(game.StateJson())!;
        json["home"]!.AsObject().Remove("story");
        foreach (var resident in json["home"]!["crew"]!.AsArray())
            foreach (string field in new[] { "disposition", "memories", "bonds", "breakUntil", "workTicks" }) resident!.AsObject().Remove(field);
        foreach (var m in json["home"]!["machines"]!.AsArray()) { m!.AsObject().Remove("condition"); m.AsObject().Remove("fault"); }
        var restored = Game.FromJson(json.ToJsonString());
        Assert.Equal(game.Home.Random.State, restored.Home.Random.State); Assert.Equal(game.Home.Resources, restored.Home.Resources);
        Assert.NotNull(restored.Home.Story); Assert.NotNull(restored.Home.Crew[0].Disposition); Assert.All(restored.Home.Machines, m => Assert.Equal(100, m.Condition));
    }


    [Fact]
    public void OrdinaryCommandsConnectARealFaultRepairWorkAndCrewRequest()
    {
        var game = StationScenario.Run();
        Assert.Contains(game.Home.Crew, c => c.WorkTicks > 0 && c.Memories.Any(m => m.Key == "heard"));
        Assert.Contains(game.Home.Story!.History, h => h.DefinitionId == "pump");
        Assert.True(game.Home.Machines.Count(m => m.Kind == ModuleKind.Solar) >= 3);
    }

    public static IEnumerable<object[]> EveryResponse() => Simulation.IncidentLibrary.SelectMany(d => d.Choices.Select(c => new object[] { d.Id, c.Id }));

    [Theory]
    [MemberData(nameof(EveryResponse))]
    public void EveryAuthoredResponseResolvesAndRoundTripsItsConsequences(string definition, string response)
    {
        var game = Shelter();
        game.Home.AddColonist("Sol", "Botanist", "Optimist", game.Home.World.Center);
        game.Home.AddColonist("Inez", "Medic", "Steadfast", game.Home.World.Center);
        game.Home.Resources = new Stock(1000, 1000, 1000, 100);
        game.Home.Story!.Budget = 3;
        if (definition is "fatigue" or "workload") { game.Home.Crew[0].WorkTicks = 160; game.Home.Crew[0].Energy = 38; }
        if (definition == "solidarity") game.Home.Remember(game.Home.Crew[0], "repair", "I kept the shelter running.", 8);
        var i = Open(game, definition);
        Assert.True(game.Home.IncidentChoiceAvailable(i.Id, response).Success);
        CampaignScenarios.Require(game.RespondToIncident(i.Id, response));
        Assert.DoesNotContain(game.Home.Story.Pending, p => p.Id == i.Id);
        Assert.Contains(game.Home.Story.History, h => h.Id == i.Id);
        Assert.Equal(game.StateJson(), Game.FromJson(game.StateJson()).StateJson());
    }

    [Fact]
    public void NearbySeedsProduceVariedFirstIncidents()
    {
        var kinds = new HashSet<string>();
        for (int seed = 0; seed < 16; seed++)
        {
            var game = new Game(seed); CampaignScenarios.Require(game.StepColony(120));
            kinds.Add(Assert.Single(game.Home.Story!.Pending).DefinitionId);
        }
        Assert.True(kinds.Count >= 3, "Adjacent seeds must vary their first incident, not merely later draws.");
    }

    [Fact]
    public void LongRunHonorsCooldownsFamilyLimitsAndHostileStreakCaps()
    {
        var game = new Game(2); game.Home.Resources = new Stock(5000, 5000, 5000, 500);
        var opened = new Dictionary<int, Incident>(); var last = new Dictionary<string, long>(); int hostile = 0;
        for (int t = 0; t < 2600; t++)
        {
            CampaignScenarios.Require(game.StepColony());
            var state = game.Home.Story!;
            Assert.InRange(state.Pending.Count, 0, 3);
            Assert.Equal(state.Pending.Count, state.Pending.Select(i => Simulation.IncidentLibrary.Single(d => d.Id == i.DefinitionId).Family).Distinct().Count());
            foreach (var i in state.Pending.Where(i => !opened.ContainsKey(i.Id)))
            {
                opened.Add(i.Id, i); var d = Simulation.IncidentLibrary.Single(d => d.Id == i.DefinitionId);
                if (last.TryGetValue(d.Id, out long previous)) Assert.True(i.OpenedAt - previous >= d.Cooldown);
                last[d.Id] = i.OpenedAt; hostile = d.Severity > 0 ? hostile + 1 : 0;
                Assert.InRange(hostile, 0, 2);
            }
        }
        Assert.True(opened.Count >= 6);
        Assert.True(opened.Values.Select(i => i.DefinitionId).Distinct().Count() >= 3);
    }

    [Fact]
    public void DeferredWearTripSavesTheSamePowerStateThatLoadingReconstructs()
    {
        var game = Shelter(); Pump(game).Condition = 15.001f;
        CampaignScenarios.Require(game.StepColony());
        Assert.Equal(MachineFault.Offline, Pump(game).Fault); Assert.False(Pump(game).Powered);
        Assert.Equal(game.StateJson(), Game.FromJson(game.StateJson()).StateJson());
    }

    [Fact]
    public void SocialContactAtHomeCannotGiveAnAwayResidentLocalRestOrFriendship()
    {
        var game = Shelter(); game.Home.AddColonist("Sol", "Botanist", "Optimist", game.Home.World.Center);
        CampaignScenarios.Require(game.Prepare("rescue-0")); CampaignScenarios.Require(game.Depart([0]));
        var away = game.Home.Crew[0]; away.Energy = 25; away.Rest = true;
        for (int n = 0; n < 120; n++) CampaignScenarios.Require(game.Wait());
        Assert.True(away.Energy < 25); Assert.Empty(away.Bonds); Assert.Equal(0, away.WorkTicks);
    }

    [Theory]
    [InlineData("budget")] [InlineData("pending")] [InlineData("memory")] [InlineData("bond")] [InlineData("condition")]
    public void MalformedNewStateIsRejected(string field)
    {
        var game = Shelter(); Open(game, "pump"); var json = JsonNode.Parse(game.StateJson())!;
        switch (field)
        {
            case "budget": json["home"]!["story"]!["budget"] = -1; break;
            case "pending": json["home"]!["story"]!["pending"]![0]!["cell"] = null; break;
            case "memory": json["home"]!["crew"]![0]!["memories"] = null; break;
            case "bond": json["home"]!["crew"]![0]!["bonds"] = JsonNode.Parse("[{\"otherId\":0,\"trust\":1}]"); break;
            case "condition": json["home"]!["machines"]![0]!["condition"] = -1; break;
        }
        Assert.Throws<InvalidDataException>(() => Game.FromJson(json.ToJsonString()));
    }
}
