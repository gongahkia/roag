using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeResourceAccountingTests
{
    [Fact]
    public void PaidPackMealsMedicineAndFireKeepTotalConsumptionWithoutChargingRegionalStock()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var hero = world.Party.Single();
        long stock = world.AccountedResources(), flow = world.RegionalResourceFlow, consumed = world.State.ResourcesConsumed;
        var before = world.State.Inventory.ToDictionary();
        hero.Body.Hunger = 40;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Eat)).Success);
        hero.Health -= 4;
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseMedicine)).Success);
        // A clear existing founder tile avoids generated props without moving
        // the expedition to invented supplies or bypassing its paid actions.
        Assert.Null(world.ObjectAt(hero.Position));
        Assert.True(world.SubmitDungeon(new(TacticalAction.LightCampFire)).Success);
        Assert.Equal(before["food"] - 1, world.Count("food")); Assert.Equal(before["medicine"] - 1, world.Count("medicine"));
        Assert.Equal(before["wood"] - 1, world.Count("wood"));
        Assert.Equal(consumed + 3, world.State.ResourcesConsumed);
        Assert.Equal(3, world.State.NativeCampaign!.PackResourcesConsumed);
        Assert.Equal(stock + world.RegionalResourceFlow - flow, world.AccountedResources());
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void BuyingThenEatingPreservesRegionalExportAndPackConsumptionAsSeparateFacts()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var hero = world.Party.Single();
        long stock = world.AccountedResources(), flow = world.RegionalResourceFlow, exported = world.State.ResourcesExported;
        int food = world.Count("food");
        Assert.True(world.SubmitDungeon(new(TacticalAction.Trade, Work: new(WorldCommandKind.Buy, Item: "food", Amount: 2))).Success);
        Assert.Equal(exported + 2, world.State.ResourcesExported);
        hero.Body.Hunger = 40;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Eat)).Success);
        Assert.Equal(food + 1, world.Count("food"));
        Assert.Equal(1, world.State.NativeCampaign!.PackResourcesConsumed);
        Assert.Equal(stock + world.RegionalResourceFlow - flow, world.AccountedResources());
    }

    [Fact]
    public void RecruitmentTransfersActualPersonalCargoAcrossTheRegionalBoundaryExactlyOnce()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var hero = world.Party.Single();
        var companion = world.State.Actors.Single(a => a.Id == world.NativeAdventurers[0].Actor);
        // Explicit finite cargo/approach setup isolates the ownership boundary.
        companion.Cargo["medicine"] = 2;
        hero.Position = world.State.Position = new[] { companion.Position.Offset(0, -1), companion.Position.Offset(1, 0),
            companion.Position.Offset(0, 1), companion.Position.Offset(-1, 0) }.First(p => world.CanEnter(p, hero.Id));
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        long stock = world.AccountedResources(), flow = world.RegionalResourceFlow, exported = world.State.ResourcesExported;
        int medicine = world.Count("medicine");
        Assert.True(world.SubmitDungeon(new(TacticalAction.Recruit, Target: companion.Id)).Success);
        Assert.Empty(companion.Cargo); Assert.Equal(medicine + 2, world.Count("medicine"));
        Assert.Equal(exported + 2, world.State.ResourcesExported);
        Assert.Equal(stock + world.RegionalResourceFlow - flow, world.AccountedResources());
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Recruit, Target: companion.Id)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void EarlierWorldsOmitZeroLedgerAndPaidConsumptionContinuesExactlyAfterSaving()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var hero = world.Party.Single();
        Assert.DoesNotContain("\"PackResourcesConsumed\"", JsonSerializer.Serialize(world.State), StringComparison.Ordinal);
        string directory = Path.Combine(Path.GetTempPath(), "roag-native-ledger-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "campaign.json");
        try
        {
            world.Save(path);
            using (var old = WorldSession.Load(path)) Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(old.State));
            hero.Body.Hunger = 40; Assert.True(world.SubmitDungeon(new(TacticalAction.Eat)).Success);
            world.Save(path); using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(world.RegionalResourceFlow, loaded.RegionalResourceFlow);
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(-1)] [InlineData(1)]
    public void InvalidPackConsumptionLedgerRejectsBeforeItCanAlterRegionalAccounting(long count)
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        Assert.Equal(0, world.State.ResourcesConsumed);
        world.State.NativeCampaign!.PackResourcesConsumed = count;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
    }
}
