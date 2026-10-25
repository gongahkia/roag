using System.Text.Json;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class SpatialIndexTests
{
    [Fact]
    public void TileRegionAndIdentityQueriesTrackEveryMutationWithoutChangingState()
    {
        var world = WorldCollisionTests.Arena(); var p = world.State.Position;
        var a = world.State.Actors[0]; var b = world.State.Actors[1];
        a.Position = p.Offset(2, 0); b.Position = p.Offset(3, 0);
        Compare();
        b.Position = a.Position; Compare(); Assert.Same(a, world.OccupantAt(a.Position));
        a.Life = LifeState.Dead; Compare(); Assert.Same(b, world.OccupantAt(b.Position));
        b.Life = LifeState.Downed; Compare(); Assert.Same(b, world.OccupantAt(b.Position));
        b.Position = b.Position with { Floor = 1 }; Compare();
        b.Position = p.Offset(32, 16); Compare();
        long beforeId = b.Id; b.Id = 7777777; Assert.Same(b, world.ActorById(b.Id)); Assert.Throws<InvalidOperationException>(() => world.ActorById(beforeId));
        var added = new Actor { Id = 8888888, Position = p.Offset(1, 0), Life = LifeState.Alive };
        world.State.Actors.Insert(0, added); Compare(); Assert.Same(added, world.ActorById(added.Id));
        world.State.Actors[0] = new() { Id = 9999999, Position = p.Offset(2, 0), Life = LifeState.Alive }; Compare();
        world.State.Actors.RemoveAt(0); Compare();
        world.State.Actors.AddRange([added]); Compare();
        world.State.Actors = new ActorCollection { added }; Compare();
        world.State.Actors.Clear(); Compare();

        void Compare()
        {
            string state = JsonSerializer.Serialize(world.State);
            foreach (var cell in new[] { p, p.Offset(1, 0), p.Offset(2, 0), p.Offset(3, 0), p.Offset(32, 16), p.Offset(3, 0) with { Floor = 1 } })
                Assert.Same(world.State.Actors.FirstOrDefault(x => x.Life != LifeState.Dead && x.Position == cell), world.OccupantAt(cell));
            foreach (int radius in new[] { 0, 1, 16, 100 })
                Assert.Equal(world.State.Actors.Where(x => x.Life != LifeState.Dead && x.Position.Distance(p) <= radius), world.ActorsNear(p, radius));
            Assert.Equal(state, JsonSerializer.Serialize(world.State));
        }
    }

    [Fact]
    public void HiddenActorsStayPrivateAndQueriesPreserveSavedContinuation()
    {
        var world = WorldCollisionTests.Arena(); var p = world.State.Position;
        var actor = world.State.Actors[0]; actor.Position = p.Offset(50, 0);
        Assert.DoesNotContain(actor.Position, world.Visible); Assert.Null(world.OccupantAt(actor.Position, observedOnly: true));
        Assert.Same(actor, world.OccupantAt(actor.Position));
        var directory = Path.Combine(Path.GetTempPath(), "vagabonds-spatial-" + Guid.NewGuid().ToString("N"));
        try
        {
            string file = Path.Combine(directory, "world.json"); world.Save(file);
            var loaded = WorldSession.Load(file);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            world.Paused = loaded.Paused = false;
            for (int i = 0; i < 20; i++)
            {
                world.ActorsNear(world.State.Position, 100); world.AdvanceTicks(1); loaded.AdvanceTicks(1);
                Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void ExactTileChecksDoNotAllocateOrScanDistantActors()
    {
        var world = WorldCollisionTests.Arena(); var p = world.State.Position;
        world.OccupantAt(p);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10_000; i++) Assert.NotNull(world.OccupantAt(p));
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1024);
    }
}
