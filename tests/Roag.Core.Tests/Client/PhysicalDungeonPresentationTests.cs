using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Roag.Client;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class PhysicalDungeonPresentationTests
{
    [Fact]
    public void PhysicalBranchMapAndExitInspectionUseItsActualDimensionsAndOwnedEliteReceipt()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var parcel = DungeonParcelPlanner.Reserve(world.Plan, 1).Single();
        var complex = world.InstallDungeonComplex(DungeonComplexPlanner.Generate(world.Plan, parcel));
        var floor = complex.Run.Floors.First(f => f.Stage == 2 && f.Boss > 0);
        var boss = world.ActorById(floor.Boss); var hero = world.DungeonMember;
        var adjacent = new[] { floor.Exit.Offset(0, -1), floor.Exit.Offset(1, 0), floor.Exit.Offset(0, 1), floor.Exit.Offset(-1, 0) };
        boss.Position = adjacent.First(p => world.CanEnter(p, boss.Id));
        hero.Position = world.State.Position = adjacent.First(p => world.CanEnter(p, hero.Id));
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.Contains(floor.Exit, world.Visible);
        Assert.Null(world.State.Dungeon); Assert.Equal(1, complex.Run.Stage);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Equal("Sealed stage exit", WorldInspection.Inspect(world, floor.Exit).Title);
        var map = DungeonMap.Build(world);
        Assert.Equal(floor.Origin, map.Origin); Assert.Equal(floor.Width, map.Width); Assert.Equal(floor.Height, map.Height);
        Assert.All(map.Cells, c => Assert.True(world.Chunks.Seen(c.Position)));

        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        void Field(string name, object value) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(client, value);
        Field("_session", world); Field("_map", new Rectangle(16, 84, 674, 490));
        var bounds = (Rectangle)typeof(WorldGame).GetMethod("DungeonMapBounds", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, null)!;
        Assert.True(bounds.Width >= Math.Ceiling(map.Width * 1.3f) + 16);
        Assert.True(bounds.Height >= Math.Ceiling(map.Height * 1.3f) + 32);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));

        var combat = world.DungeonCombat;
        combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [boss.Position], "test:physical-exit",
            AttackDelivery.Area, DamageElement.Physical, 100000, combat.Clock, IgnoresTerrain: true));
        Assert.Contains(boss.Id, complex.Run.BossExitReceipts);
        Assert.Equal("Stage exit: choose a route", WorldInspection.Inspect(world, floor.Exit).Title);
        world.Visible.Remove(floor.Exit);
        Assert.Equal("Stage exit (remembered)", WorldInspection.Inspect(world, floor.Exit).Title);
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
