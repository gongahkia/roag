using System.Text.Json;
using Roag.Client;
using Roag.Core.Tests.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class FixedEncounterInspectionTests
{
    [Fact]
    public void HoverUsesTheActualNearbyGroupButOnlyCountsVisibleFoes()
    {
        var (world, ids) = EncounterOpponentsTests.Cluster();
        var target = world.State.Actors.Single(a => a.Id == ids[0]);
        foreach (long id in ids.Skip(1).Take(3))
        {
            var foe = world.State.Actors.Single(a => a.Id == id);
            foe.BaseHealth = foe.Health = 1000; foe.BasePower = 200;
            world.Visible.Remove(foe.Position);
        }
        string before = JsonSerializer.Serialize(world.State);
        var obscured = WorldInspection.Inspect(world, target.Position);
        Assert.DoesNotContain("overwhelming", obscured.Detail);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        foreach (long id in ids.Take(4)) world.Visible.Add(world.State.Actors.Single(a => a.Id == id).Position);
        var revealed = WorldInspection.Inspect(world, target.Position);
        Assert.Contains("Visible threat estimate: overwhelming", revealed.Detail);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
}
