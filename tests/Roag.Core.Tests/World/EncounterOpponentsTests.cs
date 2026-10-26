using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class EncounterOpponentsTests
{
    internal static (WorldSession World, long[] Enemies) Cluster(int partySize = 4)
    {
        var world = WorldSession.Create(42); CampPreview.Prepare(world);
        foreach (long id in world.State.Party.Skip(partySize).ToArray())
        { world.State.Party.Remove(id); world.State.Actors.Single(a => a.Id == id).Recruited = false; }
        var offsets = new[] { (5, 0), (6, 0), (5, 1), (6, 1), (4, 0), (13, 0) };
        var ids = new List<long>();
        foreach (var (x, y) in offsets)
        {
            long id = world.State.NextActorId++; ids.Add(id);
            var position = world.State.Position.Offset(x, y); world.Chunks.Edit(position, Material.Floor);
            world.State.Actors.Add(ActorFactory.Create(world.Content, world.Content.Game.Beast, world.Seed, id, position, 0));
        }
        world.Reveal(); return (world, ids.ToArray());
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(4)]
    public void NearbyOpponentsHaveTheSameBoundedMembershipForEveryPartySize(int size)
    {
        var (world, ids) = Cluster(size);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Equal(ids.Take(4), world.EncounterOpponents(ids[0]).Select(a => a.Id));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        world.Party.First().Level = 40;
        Assert.Equal(ids.Take(4), world.EncounterOpponents(ids[0]).Select(a => a.Id));
        world.StartBattle(ids[0]);
        var battle = world.State.Battle!;
        Assert.Equal(size, battle.Units.Count(u => u.Friendly));
        Assert.Equal(ids.Take(4), battle.Units.Where(u => !u.Friendly).Select(u => battle.ActorIds[u.Id]));
    }

    [Theory]
    [InlineData(1)] [InlineData(4)]
    public void ContactingAnyAuthoredMemberKeepsTheWholeGroupAndExcludesOtherFoes(int size)
    {
        var world = WorldSession.Create(42); var fixture = EncounterGroupPreview.Prepare(world, "encounter.cinder-retinue");
        world.State.Battle = null;
        foreach (long id in world.State.Party.Skip(size).ToArray())
        { world.State.Party.Remove(id); world.State.Actors.Single(a => a.Id == id).Recruited = false; }
        long target = fixture.Members[2];
        Assert.Equal(fixture.Members.Order(), world.EncounterOpponents(target).Select(a => a.Id).Order());
        world.StartBattle(target);
        Assert.Equal(fixture.Members.Order(), world.State.Battle!.ActorIds.Skip(size).Order());
    }
}
