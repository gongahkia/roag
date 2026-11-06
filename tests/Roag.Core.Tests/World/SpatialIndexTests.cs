using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class SpatialIndexTests
{
    [Fact]
    public void TacticalIndexesTrackReplacementDefinitionsAndEveryBulwarkOwnerWithoutSavingDerivedState()
    {
        using var world = WorldCollisionTests.Arena(); var p = world.State.Position;
        world.State.Tactics = new();
        var a = world.State.Actors[0]; var b = world.State.Actors[1];
        a.Tactics = EnemyCatalog.Default.Get(EnemyOaths.A).CreateTactics();
        b.Tactics = EnemyCatalog.Default.Get(EnemyOaths.B).CreateTactics();
        Compare();
        a.Tactics.Resources.Bulwark = new(p, 1, 1, 100); Compare();
        b.Tactics.Resources.Bulwark = new(p, 2, 2, 100); Compare();
        a.Tactics.Resources.Bulwark = null; Compare();
        b.Tactics.Resources.Bulwark = new(p.Offset(1, 0), 2, 2, 100); Compare();
        b.Tactics.Resources = new() { Bulwark = new(p, 3, 3, 100) }; Compare();
        b.Life = LifeState.Dead; Compare(); // Live collision historically retains every owner's block.
        b.Id = 9999999; Compare();
        a.Tactics.DefinitionId = "core:enemy/ruin_skitter"; Compare();
        b.Tactics = EnemyCatalog.Default.Get(EnemyOaths.A).CreateTactics(); Compare();
        a.Tactics = null; Compare();
        var added = new Actor { Id = 8888888, Tactics = EnemyCatalog.Default.Get(EnemyOaths.B).CreateTactics() };
        added.Tactics.Resources.Bulwark = new(p, 4, 4, 100);
        world.State.Actors.Insert(0, added); Compare();
        world.State.Actors.RemoveAt(0); Compare();
        world.State.Actors = new ActorCollection { a }; Compare();
        world.State.Actors.Clear(); Compare();

        void Compare()
        {
            string before = JsonSerializer.Serialize(world.State);
            Assert.Equal(world.State.Actors.Where(x => EnemyOaths.IsTwin(x.Tactics?.DefinitionId ?? "")), world.OathTwins);
            foreach (var cell in new[] { p, p.Offset(1, 0), p with { Floor = 1 } })
                foreach (long except in new[] { 0L, a.Id, b.Id })
                    Assert.Equal(world.State.Actors.Any(x => x.Id != except && x.Tactics?.Resources.Bulwark?.Position == cell),
                        world.TacticalBlockAt(cell, except));
            Assert.Equal(before, JsonSerializer.Serialize(world.State));
        }
    }

    [Fact]
    public void OptionalIdentityLookupPreservesMissingAndFirstOwnerSemanticsWhileStrictLookupRejectsDuplicates()
    {
        using var world = WorldCollisionTests.Arena();
        var a = world.State.Actors[0]; var b = world.State.Actors[1];
        Assert.Null(world.FindActor(-1)); Assert.Same(a, world.FindActor(a.Id));
        b.Id = a.Id; Assert.Same(a, world.FindActor(a.Id));
        Assert.Throws<InvalidOperationException>(() => world.ActorById(a.Id));
        world.State.Actors.Remove(a); Assert.Same(b, world.FindActor(b.Id));
        world.State.Actors.Remove(b); Assert.Null(world.FindActor(b.Id));
    }

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
