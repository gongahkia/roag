using Ithica.Core;
using Ithica.Core.Colony;
using Ithica.Core.Diagnostics;
using Xunit;
using Game = Ithica.Core.Campaign;

namespace Ithica.Tests.Industry;

public sealed class WorkforceTests
{
    private static Game Shelter()
    {
        var g = new Game(); g.Home.IncidentsEnabled = false; return g;
    }
    [Theory]
    [InlineData(CrewShift.Flexible, true, true)]
    [InlineData(CrewShift.Day, true, false)]
    [InlineData(CrewShift.Night, false, true)]
    [InlineData(CrewShift.Extended, true, true)]
    [InlineData(CrewShift.Rest, false, false)]
    public void ShiftBoundariesFollowTheSharedClock(CrewShift shift, bool atStart, bool at240)
    {
        var g = Shelter(); var c = g.Home.Crew[0];
        if (shift != CrewShift.Flexible) CampaignScenarios.Require(g.SetShift(c.Id, shift));
        Assert.Equal(atStart, g.Home.OnShift(c));
        CampaignScenarios.Require(g.StepColony(240)); Assert.Equal(at240, g.Home.OnShift(c));
        CampaignScenarios.Require(g.StepColony(160));
        Assert.Equal(shift is CrewShift.Flexible or CrewShift.Night, g.Home.OnShift(c));
        Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Fact]
    public void DisabledWorkStaysUnassignedAndRestCanBeProtected()
    {
        var g = Shelter(); var s = g.Home; var c = s.Crew[0];
        CampaignScenarios.Require(g.SetWorkPriority(0, WorkGroup.Construction, 0));
        int cell = s.World.Center + 2;
        CampaignScenarios.Require(s.Designate(s.World.X(cell), s.World.Y(cell), Tool.Build, ModuleKind.Solar));
        CampaignScenarios.Require(g.StepColony(32)); Assert.Null(c.Job); Assert.Equal(0, s.JobAt(cell)!.Progress);
        CampaignScenarios.Require(g.SetWorkPriority(0, WorkGroup.Construction, 1));
        CampaignScenarios.Require(g.StepColony(8)); Assert.NotNull(c.Job);
        CampaignScenarios.Require(g.AssignRest(0, 120)); Assert.Null(c.Job); Assert.True(c.Rest);
        Assert.Contains(c.Memories, m => m.Key == "heard");
        float pausedProgress = s.JobAt(cell)!.Progress;
        long until = c.BreakUntil; CampaignScenarios.Require(g.StepColony(119)); Assert.True(c.Rest); Assert.True(s.Tick < until);
        Assert.Equal(pausedProgress, s.JobAt(cell)!.Progress);
        string snap = g.StateJson(); Assert.False(g.AssignRest(0, 0).Success); Assert.Equal(snap, g.StateJson());
    }

    [Fact]
    public void HigherPriorityMaintenanceInterruptsConstructionAndCompletesFirst()
    {
        var g = Shelter(); var s = g.Home; var c = s.Crew[0];
        CampaignScenarios.Require(g.SetWorkPriority(0, WorkGroup.Maintenance, 1));
        var pump = s.Machines.Single(m => m.Kind == ModuleKind.Oxygen);
        int cell = s.World.Index(s.World.X(c.Cell) + 1, s.World.Y(c.Cell));
        CampaignScenarios.Require(s.Designate(s.World.X(cell), s.World.Y(cell), Tool.Build, ModuleKind.Medbay));
        CampaignScenarios.Require(g.StepColony(8)); var construction = s.JobAt(cell)!; Assert.Equal(construction.Id, c.Job);
        pump.Condition = 70; CampaignScenarios.Require(g.OrderMaintenance(pump.Cell));
        for (int n = 0; n < 80 && s.JobAt(pump.Cell) is not null; n++) CampaignScenarios.Require(g.StepColony());
        Assert.Null(s.JobAt(pump.Cell)); Assert.True(pump.Condition > 99);
        Assert.NotNull(s.JobAt(cell)); Assert.True(construction.Progress < construction.Required);
        Assert.Equal(g.StateJson(), Game.FromJson(g.StateJson()).StateJson());
    }

    [Fact]
    public void PriorityChangeReleasesTheCurrentAssignmentWithoutDestroyingItsProgress()
    {
        var g = Shelter(); var s = g.Home;
        int cell = s.World.Index(s.World.X(s.Crew[0].Cell) + 1, s.World.Y(s.Crew[0].Cell));
        CampaignScenarios.Require(s.Designate(s.World.X(cell), s.World.Y(cell), Tool.Build, ModuleKind.Solar));
        CampaignScenarios.Require(g.StepColony(8)); float progress = s.JobAt(cell)!.Progress; Assert.True(progress > 0);
        CampaignScenarios.Require(g.SetWorkPriority(0, WorkGroup.Construction, 0));
        Assert.Null(s.Crew[0].Job); Assert.Null(s.JobAt(cell)!.Assigned);
        CampaignScenarios.Require(g.StepColony(16)); Assert.Equal(progress, s.JobAt(cell)!.Progress);
    }

    [Fact]
    public void OffDutyAndProtectedBreakResidentsCannotBeDispatched()
    {
        var g = Shelter(); CampaignScenarios.Require(g.SetShift(0, CrewShift.Rest));
        CampaignScenarios.Require(g.Prepare("rescue-0")); string before = g.StateJson();
        Assert.False(g.Depart([0]).Success); Assert.Equal(before, g.StateJson());
        CampaignScenarios.Require(g.CancelPreparation()); CampaignScenarios.Require(g.SetShift(0, CrewShift.Flexible));
        CampaignScenarios.Require(g.StepColony(10)); CampaignScenarios.Require(g.AssignRest(0));
        CampaignScenarios.Require(g.Prepare("rescue-0")); before = g.StateJson();
        Assert.False(g.Depart([0]).Success); Assert.Equal(before, g.StateJson());
    }

    [Fact]
    public void StaffingCommandsDoNotAdvanceTimeOrRandomness()
    {
        var g = Shelter(); long tick = g.CampaignTick; ulong home = g.Home.Random.State, story = g.Home.Story!.RandomState;
        CampaignScenarios.Require(g.SetWorkPriority(0, WorkGroup.Mining, 1)); CampaignScenarios.Require(g.SetShift(0, CrewShift.Extended));
        CampaignScenarios.Require(g.SetDrillingRisk(0, true)); CampaignScenarios.Require(g.AssignRest(0));
        Assert.Equal(tick, g.CampaignTick); Assert.Equal(home, g.Home.Random.State); Assert.Equal(story, g.Home.Story.RandomState);
        string snap = g.StateJson(); Assert.False(g.SetWorkPriority(0, WorkGroup.Mining, 1).Success); Assert.False(g.SetDrillingRisk(0, true).Success);
        Assert.Equal(snap, g.StateJson());
    }
}
