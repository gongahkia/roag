using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeWorldClockTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PaidNativeTimeKeepsResidentRoutinesAliveDuringAndOutsideLocalCombat(bool nearbyThreat)
    {
        using var world = WorldSession.Create(42);
        var hero = world.Party.Single(); var home = world.Plan.Towns[0].Center;
        hero.Position = world.State.Position = home;
        var enemy = world.State.Actors.First(a => a.Kind == ActorKind.Raider && a.EncounterLeader == 0);
        enemy.Position = home.Offset(nearbyThreat ? 2 : 30, 0);
        for (int y = -2; y <= 2; y++) for (int x = -1; x <= 31; x++) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        // Avoid changing the health/cargo authorities of existing named residents.
        var resident = world.State.Actors.First(a => !a.Recruited && a.Kind == ActorKind.Person && a.HomeTown == 0);
        var combat = world.CreateTacticalSimulation();
        combat.Register(hero, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42) });
        combat.RegisterEnemy(enemy, "core:enemy/mossbound_guard", 10000);
        Assert.Same(hero, combat.AdvanceToDecision());
        Assert.Equal(nearbyThreat, world.IsInCombat);
        Assert.False(world.IsActorInCombat(resident.Id)); Assert.Null(resident.Routine);
        world.Paused = false;
        string before = JsonSerializer.Serialize(world.State);
        Assert.Equal(0, world.AdvanceFrame(TimeSpan.FromSeconds(1)));
        Assert.Equal(0, world.AdvanceTicks(50));
        Assert.False(world.Submit(new(WorldCommandKind.Wait)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        for (int i = 0; i < 12; i++)
        {
            Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
            Assert.Same(hero, combat.AdvanceToDecision());
        }
        Assert.Equal(1200, combat.Clock); Assert.Equal(1, world.State.Tick);
        Assert.NotNull(resident.Routine);
        Assert.Null(world.State.Battle); Assert.False(world.State.RoagCombat);
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
