using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using System.Text.Json;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Roag.Core.Tests.World;

public sealed class OpenCampaignConstructionTests(ITestOutputHelper output)
{
    [Fact]
    public void FreshWorldConstructsAllFourFullComplexesAndRetainsTheOriginalLivingWorldWithoutRevealingThem()
    {
        using var reference = WorldSession.CreateNativeCampaign(42);
        var startup = Stopwatch.StartNew();
        using var world = WorldSession.CreateOpenCampaign(42);
        startup.Stop();
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
            var home = world.CampaignFinale!.Guardians.FirstOrDefault(g => g.Actor == old.Id || g.Actor == old.EncounterLeader);
            if (home == null) Assert.Equal(old.Position, actual.Position);
            else
            {
                var original = reference.ActorById(home.Actor);
                Assert.Equal(home.Spawn.Offset(old.Position.X - original.Position.X, old.Position.Y - original.Position.Y), actual.Position);
            }
            Assert.Equal(old.Health, actual.Health); Assert.Equal(old.MaxHealth, actual.MaxHealth);
            Assert.Equal(old.Kind, actual.Kind); Assert.Equal(old.Name, actual.Name);
            Assert.Equal(old.Archetype, actual.Archetype); Assert.Equal(old.VariantId, actual.VariantId);
            Assert.Equal(old.EncounterLeader, actual.EncounterLeader); Assert.Equal(old.Tactics?.DefinitionId, actual.Tactics?.DefinitionId);
            Assert.Equal(JsonSerializer.Serialize(old.Tactics), JsonSerializer.Serialize(actual.Tactics));
            Assert.Equal(JsonSerializer.Serialize(old.Equipment), JsonSerializer.Serialize(actual.Equipment));
            Assert.Equal(JsonSerializer.Serialize(old.Cargo), JsonSerializer.Serialize(actual.Cargo));
        });
        Assert.Equal(JsonSerializer.Serialize(reference.State.Loot), JsonSerializer.Serialize(world.State.Loot));
        Assert.Equal(JsonSerializer.Serialize(reference.State.GeneratedEquipment), JsonSerializer.Serialize(world.State.GeneratedEquipment));
        Assert.Equal(48, world.CampaignFinale!.Guardians.Count);
        Assert.Equal(24, world.CampaignFinale.Guardians.Select(g => g.SiteId).Distinct().Count());
        Assert.Equal(20, world.Plan.Sites.Count(s => world.RetainedGuardiansAt(s.Id).Count == 0 && world.GeneratedBossSites.All(b => b.SiteId != s.Id)));
        Assert.True(world.ChooseDungeonClass(ClassKind.Hookrunner).Success);
        var waits = Stopwatch.StartNew();
        for (int i = 0; i < 4; i++)
        {
            long clock = world.DungeonCombat.Clock;
            var preview = world.DungeonCombat.Preview(new(TacticalAction.Wait));
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(clock + preview.Recovery, world.DungeonCombat.Clock);
            Assert.Equal(world.DungeonMember.Id, world.State.Tactics.ActiveActor);
        }
        waits.Stop();
        output.WriteLine($"full_world_startup_ms={startup.Elapsed.TotalMilliseconds:F2}; four_paid_waits_ms={waits.Elapsed.TotalMilliseconds:F2}; actors={world.State.Actors.Count}; participants={world.State.Tactics.Participants.Count}");
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
