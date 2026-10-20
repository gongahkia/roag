using Ithica.Core.Colony;
using Xunit;

namespace Ithica.Tests.Story;

public sealed class StationReportTests
{
    [Fact]
    public void ObservableReportDoesNotRevealHiddenPacingState()
    {
        var game = new Ithica.Core.Campaign(); string before = StationReports.Conditions(game.Home);
        game.Home.Story!.RecoveryUntil = 10000; game.Home.Story.Budget = 0; game.Home.Story.HostileStreak = 2;
        Assert.Equal(before, StationReports.Conditions(game.Home));
        Assert.Contains("AIR", before); Assert.Contains("WATER", before); Assert.Contains("POWER", before);
        foreach (string word in new[] { "DIRECTOR", "STRAIN", "EXPOSURE", "RECOVERY", "BUDGET", "WATCHFUL" }) Assert.DoesNotContain(word, before);
    }
    [Fact]
    public void HistoricalHandlerIdsAndDecisionNumbersAreTranslatedWithoutEditingSavedData()
    {
        var record = new IncidentRecord(1, "pump", 120, "No response; defer / follow-up text");
        string displayed = StationReports.Outcome(record);
        Assert.Contains("Keep it running", displayed); Assert.DoesNotContain("defer", displayed);
        Assert.Equal("No response; defer / follow-up text", record.Outcome);
        string notice = StationReports.Notice("A vibration in life support / decision #1, 96 ticks to respond. Open incidents [I].");
        Assert.DoesNotContain("decision #", notice); Assert.Contains("station reports", notice);
        Assert.Equal("A vibration in life support: Keep it running", StationReports.Notice("A vibration in life support: defer"));
        Assert.Equal("Hydroponics harvest: +3 food.", StationReports.Notice("Hydroponics harvest: +3 food."));
    }
    [Fact]
    public void NewMessagesDescribeTheSituationAndResponsesInPlayerLanguage()
    {
        var game = new Ithica.Core.Campaign(2); game.StepColony(120);
        var incident = Assert.Single(game.Home.Story!.Pending); Assert.Equal("pump", incident.DefinitionId);
        Assert.Contains(game.Home.Log, n => n.Text.Contains("Reports [I]"));
        Assert.DoesNotContain(game.Home.Log, n => n.Text.Contains("decision #") || n.Text.Contains("RECOVERY WINDOW"));
        Assert.True(game.RespondToIncident(incident.Id, "defer").Success);
        Assert.Equal("Keep it running", Assert.Single(game.Home.Story.History).Outcome);
    }
}
