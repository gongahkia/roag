using Roag.Core.Expeditions;
using Xunit;

namespace Roag.Tests.Expeditions;

public sealed class RoutingTests
{
    private static Campaign Map(params Cell[] offsets)
    {
        var campaign = new Campaign();
        var zone = campaign.Zone;
        zone.Objects.Clear();
        Array.Fill(zone.Tiles, Ground.Wall);
        Array.Clear(zone.Seen);
        Array.Clear(zone.Visible);
        foreach (var offset in offsets.Prepend(new(0, 0)))
        {
            Cell cell = campaign.Position + offset;
            zone.Set(cell, Ground.Stone);
            zone.Seen[zone.Index(cell)] = zone.Visible[zone.Index(cell)] = true;
        }
        return campaign;
    }

    private static Campaign TwoRoutes() => Map(new(1, 0), new(2, 0), new(0, 1), new(1, 1), new(2, 1));

    [Fact]
    public void DiscoveredDestinationDoesNotAllowAnUndiscoveredShortcut()
    {
        var campaign = TwoRoutes();
        Cell origin = campaign.Position;
        int hidden = campaign.Zone.Index(origin + new Cell(1, 0));
        campaign.Zone.Seen[hidden] = campaign.Zone.Visible[hidden] = false;
        string before = campaign.StateJson();

        Assert.Equal(new[] { origin + new Cell(0, 1), origin + new Cell(1, 1), origin + new Cell(2, 1), origin + new Cell(2, 0) },
            campaign.Route(origin + new Cell(2, 0)));
        Assert.Equal(before, campaign.StateJson());
    }

    [Fact]
    public void NoDiscoveredPathReturnsNoRoute()
    {
        var campaign = Map(new(1, 0), new(2, 0));
        int hidden = campaign.Zone.Index(campaign.Position + new Cell(1, 0));
        campaign.Zone.Seen[hidden] = campaign.Zone.Visible[hidden] = false;
        Assert.Empty(campaign.Route(campaign.Position + new Cell(2, 0)));
    }

    [Fact]
    public void UndiscoveredAndOutOfBoundsDestinationsAreRejected()
    {
        var campaign = Map(new Cell(1, 0));
        Cell destination = campaign.Position + new Cell(1, 0);
        campaign.Zone.Seen[campaign.Zone.Index(destination)] = false;
        Assert.Empty(campaign.Route(destination));
        Assert.Empty(campaign.Route(new(-1, 0)));
        Assert.Empty(campaign.Route(new(campaign.Zone.Width, 0)));
    }

    [Fact]
    public void UnseenHazardChangesDoNotChangeTheRoute()
    {
        var campaign = TwoRoutes();
        Cell origin = campaign.Position, shortcut = origin + new Cell(1, 0), destination = origin + new Cell(2, 0);
        int hidden = campaign.Zone.Index(shortcut);
        campaign.Zone.Visible[hidden] = false;
        var expected = campaign.Route(destination);

        campaign.Zone.Environment.Cells[hidden] = HazardKind.Spores;
        string before = campaign.StateJson();
        Assert.Equal(new[] { shortcut, destination }, expected);
        Assert.Equal(expected, campaign.Route(destination));
        Assert.Equal(before, campaign.StateJson());
    }

    [Fact]
    public void VisibleHazardsAreAvoidedWhenADiscoveredAlternativeExists()
    {
        var campaign = TwoRoutes();
        Cell origin = campaign.Position, hazard = origin + new Cell(1, 0);
        campaign.Zone.Environment.Cells[campaign.Zone.Index(hazard)] = HazardKind.Fire;
        var route = campaign.Route(origin + new Cell(2, 0));
        Assert.Equal(4, route.Count);
        Assert.DoesNotContain(hazard, route);
        Assert.All(route, p => Assert.True(campaign.Zone.Seen[campaign.Zone.Index(p)]));
    }

    [Fact]
    public void VisibleHazardsRemainTraversableWhenTheyAreTheOnlyKnownRoute()
    {
        var campaign = Map(new(1, 0), new(2, 0));
        Cell hazard = campaign.Position + new Cell(1, 0), destination = campaign.Position + new Cell(2, 0);
        campaign.Zone.Environment.Cells[campaign.Zone.Index(hazard)] = HazardKind.Spores;
        Assert.Equal(new[] { hazard, destination }, campaign.Route(destination));
    }

    [Fact]
    public void ABlockingObjectCanBeApproachedOnlyThroughDiscoveredCells()
    {
        var campaign = Map(new(1, 0), new(2, 0));
        Cell approach = campaign.Position + new Cell(1, 0), destination = campaign.Position + new Cell(2, 0);
        campaign.Zone.Objects.Add(new("chest", "Chest", ObjectKind.Chest, destination, "chest", true));
        Assert.Equal(new[] { approach }, campaign.Route(destination));
        campaign.Zone.Seen[campaign.Zone.Index(approach)] = false;
        Assert.Empty(campaign.Route(destination));
    }

    [Fact]
    public void AuditedSeedCannotRouteThroughUnexploredCells()
    {
        var campaign = new Campaign(42);
        Assert.True(campaign.TravelTo("to-wilds").Success);
        Cell destination = new(11, 10);
        Assert.True(campaign.Zone.Seen[campaign.Zone.Index(destination)]);
        Assert.All(campaign.Route(destination), p => Assert.True(campaign.Zone.Seen[campaign.Zone.Index(p)]));
    }
}
