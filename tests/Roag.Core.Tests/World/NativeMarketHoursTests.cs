using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeMarketHoursTests
{
    private static TacticalIntent Buy => new(TacticalAction.Trade, Work: new(WorldCommandKind.Buy, Item: "food"));

    [Fact]
    public void ANewCampaignBeginsAtDawnWithZeroElapsedPlayAndStillPermitsItsInitialFixedClassChoice()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        Assert.Equal(6 * WorldSession.TicksPerHour, world.State.NativeCampaign!.StartedAt);
        Assert.Equal(world.State.NativeCampaign.StartedAt, world.State.Tick);
        Assert.Equal(world.State.Tick * world.State.Tactics!.Tuning.UnitsPerWorldTick, world.DungeonCombat.Clock);
        Assert.Equal(0, world.NativeElapsedTime); Assert.Equal(6, world.HourOfDay); Assert.False(world.IsNight);
        Assert.Contains("06:00", world.CalendarDescription);
        Assert.True(world.ChooseDungeonClass(ClassKind.Ranger).Success);
        var hero = world.Party.Single(); Assert.Equal(0, hero.Body.Hunger); Assert.Equal(0, hero.Body.Fatigue);
        Assert.True(world.DungeonCombat.Preview(Buy).Valid);
        long start = world.DungeonCombat.Clock;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.True(world.NativeElapsedTime > 0); Assert.Equal(world.DungeonCombat.Clock - start, world.NativeElapsedTime);
        Assert.False(world.ChooseDungeonClass(ClassKind.Adventurer).Success);
    }

    [Theory]
    [InlineData(0, false)] [InlineData(5, false)] [InlineData(6, true)]
    [InlineData(19, true)] [InlineData(20, false)] [InlineData(23, false)]
    public void MarketHoursUseTheActualWorldCalendarAndFreeQuotesNeverAdvanceIt(int hour, bool open)
    {
        using var world = NativeSocietyTests.AtTick(hour * WorldSession.TicksPerHour);
        var hero = world.Party.Single(); string before = JsonSerializer.Serialize(world.State);
        var quote = world.QuoteTrade(hero.Id, Buy.Work); var preview = world.DungeonCombat.Preview(Buy);
        Assert.Equal(open, quote.Valid); Assert.Equal(open, preview.Valid);
        if (!open)
        {
            Assert.Contains("06:00–20:00", quote.Reason);
            Assert.False(world.SubmitDungeon(Buy).Success);
        }
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void ACommittedSaleFinishesAcrossClosingButAnotherAttemptRejectsWithoutASecondPayment()
    {
        using var world = NativeSocietyTests.AtTick(20 * WorldSession.TicksPerHour - 1);
        for (int i = 0; i < 11; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(19, world.HourOfDay); Assert.True(world.DungeonCombat.Preview(Buy).Valid);
        int food = world.Count("food"); Assert.True(world.SubmitDungeon(Buy).Success);
        Assert.Equal(food + 1, world.Count("food")); Assert.Equal(20, world.HourOfDay);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(Buy).Success); Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void WaitingThroughDawnOpensTheMarketOnAnExactlyContinuingSave()
    {
        using var world = NativeSocietyTests.AtTick(6 * WorldSession.TicksPerHour - 1);
        Assert.False(world.DungeonCombat.Preview(Buy).Valid);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-market-dawn-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            foreach (var run in new[] { world, loaded })
            {
                for (int i = 0; i < 12; i++) Assert.True(run.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.Equal(6, run.HourOfDay); Assert.True(run.DungeonCombat.Preview(Buy).Valid);
            }
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            world.Save(root); using var dawn = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(dawn.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(-1)] [InlineData(1801)]
    public void InvalidCampaignStartCannotCreateNegativeOrFutureElapsedPlay(long started)
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        world.State.NativeCampaign!.StartedAt = started;
        string root = Path.Combine(Path.GetTempPath(), "roag-native-market-start-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root);
            Assert.Throws<InvalidDataException>(() => { using var invalid = WorldSession.Load(root); });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
