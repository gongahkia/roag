using System.Text.Json;
using Roag.Client;
using Roag.Core.Tests.World;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class FixedEncounterInspectionTests
{
    [Fact]
    public void RetainedCreatureInspectionUsesItsOwnDefinitionAndExposesItsTargetingTendency()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy?.Retained != null
            && world.Content.Entries[a.Archetype] is BossDefinition);
        // Explicit inspection fixture: place the observer on actual clear adjacent
        // ground and use private sight, without claiming a natural site journey.
        var hero = world.Party.Single();
        hero.Position = new[] { enemy.Position.Offset(0, -1), enemy.Position.Offset(1, 0),
            enemy.Position.Offset(0, 1), enemy.Position.Offset(-1, 0) }.First(p => world.CanEnter(p, hero.Id));
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        string before = JsonSerializer.Serialize(world.State);
        var intent = Assert.IsType<Roag.Core.Tactics.EnemyInspection>(world.InspectTacticalEnemy(enemy.Id));
        var hover = WorldInspection.Inspect(world, enemy.Position);
        Assert.Contains(intent.Tendency, hover.Detail); Assert.Contains("HP ", hover.Detail);
        Assert.Contains(world.Content.Get<BossDefinition>(enemy.Archetype).Weakness, hover.Detail);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
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
