using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class CaravanCargoTests
{
    [Theory]
    [InlineData("food", 1)]
    [InlineData("wood", 1)]
    [InlineData("food", 9998)]
    [InlineData("food", 9999)]
    public void DepartureKeepsExistingCargoAndTakesOnlyWhatFits(string stack, int carried)
    {
        using var world = WorldSession.Create(42); var road = world.Plan.Roads[0];
        var caravan = world.State.Actors.First(a => a.Kind == ActorKind.Caravan);
        caravan.HomeTown = road.FromTown; caravan.DestinationTown = road.ToTown;
        caravan.Position = world.Plan.Towns[road.FromTown].Center.Offset(0, 10);
        caravan.Traveling = false; caravan.Route.Clear(); caravan.NextTravelTick = 1;
        caravan.Body.Hunger = caravan.Body.Fatigue = 0; caravan.Cargo.Clear(); caravan.Cargo[stack] = carried;
        var origin = world.State.Towns[road.FromTown]; var destination = world.State.Towns[road.ToTown];
        foreach (string item in origin.Stocks.Keys.ToArray()) { origin.Stocks[item] = 5; destination.Stocks[item] = 5; }
        origin.Stocks[stack] = 50;
        long resources = world.AccountedResources(), flow = world.RegionalResourceFlow;
        world.Paused = false; Assert.Equal(1, world.AdvanceTicks(1));
        int collected = Math.Min(12, 9999 - carried);
        Assert.Equal(carried + collected, caravan.Cargo[stack]); Assert.Equal(50 - collected, origin.Stocks[stack]);
        Assert.Equal(collected > 0, caravan.Traveling);
        Assert.Equal(resources + world.RegionalResourceFlow - flow, world.AccountedResources());
    }
}
