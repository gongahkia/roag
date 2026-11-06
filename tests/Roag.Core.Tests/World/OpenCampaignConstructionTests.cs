using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using System.Text.Json;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class OpenCampaignConstructionTests
{
    [Fact]
    public void FreshWorldConstructsAllFourFullComplexesAndRetainsTheOriginalLivingWorldWithoutRevealingThem()
    {
        using var reference = WorldSession.CreateNativeCampaign(42);
        using var world = WorldSession.CreateOpenCampaign(42);
        Assert.Equal(JsonSerializer.Serialize(reference.Plan), JsonSerializer.Serialize(world.Plan));
        Assert.Null(world.State.Dungeon);
        Assert.Equal(reference.DungeonMember.Position, world.DungeonMember.Position);
        Assert.Equal(reference.DungeonCombat.Clock, world.DungeonCombat.Clock);
        Assert.Equal(1, world.State.Tactics!.NextAction);
        Assert.Equal(reference.State.Chunks.Sum(c => c.Discovered.Count), world.State.Chunks.Sum(c => c.Discovered.Count));
        Assert.Equal(reference.Visible.OrderBy(DungeonRunState.CellKey), world.Visible.OrderBy(DungeonRunState.CellKey));
        var complexes = world.State.NativeCampaign!.Complexes!;
        Assert.Equal(4, complexes.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, complexes.Select(c => c.Parcel.Id));
        Assert.All(complexes, c => { Assert.True(c.Populated); Assert.NotNull(c.Routes); Assert.Equal(19, c.Run.Floors.Count); });
        Assert.Equal(180, world.State.Tactics.Chests.Chests.Count);
        Assert.Equal(reference.NativeAdventurers.Count + 80, world.NativeAdventurers.Count);
        foreach (var complex in complexes)
        {
            var entrance = DungeonComplexPlanner.GenerateGeometry(world.Plan, complex.Parcel).SurfaceEntrance;
            Assert.False(world.Chunks.Seen(entrance));
            Assert.NotNull(world.StairLanding(entrance));
        }
        Assert.All(reference.State.Actors, old =>
        {
            var actual = world.ActorById(old.Id);
            Assert.Equal(old.Position, actual.Position); Assert.Equal(old.Health, actual.Health);
            Assert.Equal(old.Kind, actual.Kind); Assert.Equal(old.Name, actual.Name);
        });
        Assert.True(world.ChooseDungeonClass(ClassKind.Hookrunner).Success);
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
