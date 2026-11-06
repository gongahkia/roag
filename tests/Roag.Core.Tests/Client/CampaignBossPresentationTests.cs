using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Roag.Graphics;
using Xunit;

namespace Roag.Core.Tests.Client;

public sealed class CampaignBossPresentationTests
{
    private static WorldSession World(CampaignBossKind kind, out CampaignBossArena arena)
    {
        var world = WorldSession.CreateNativeCampaign(42);
        arena = world.CreateCampaignBossArena(kind, new(512, 512, 1), 73, false,
            readyAt: world.DungeonCombat.Clock + 10000);
        var hero = world.DungeonMember; hero.Position = world.State.Position = arena.Center.Offset(0, 2);
        world.Reveal(); return world;
    }

    [Theory]
    [InlineData(CampaignBossKind.Bellkeeper, "Soundseek")]
    [InlineData(CampaignBossKind.SluiceMaw, "valves")]
    [InlineData(CampaignBossKind.Threadmother, "Reel In")]
    [InlineData(CampaignBossKind.OssuaryCaptain, "Shield faces")]
    public void NativeHoverUsesTheAdditionalIdentityAndPrivateReadableCounterplay(CampaignBossKind kind, string mechanic)
    {
        using var world = World(kind, out var arena);
        var boss = world.ActorById(arena.Boss);
        string before = JsonSerializer.Serialize(world.State);
        var hover = WorldInspection.Inspect(world, boss.Position);
        Assert.Equal(CampaignBossCatalog.Name(kind), hover.Title);
        Assert.Contains("HP ", hover.Detail); Assert.Contains("Guards this arena", hover.Detail);
        Assert.Contains(mechanic, hover.Detail);
        Assert.Equal(NativeAppearance.ForEnemy(CampaignBossCatalog.Id(kind)), boss.Sprite);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        world.DungeonMember.Position = world.State.Position = world.DungeonMember.Position with { Floor = 0 };
        world.Reveal();
        Assert.DoesNotContain("HP ", WorldInspection.Inspect(world, boss.Position).Detail);
        Assert.Null(world.InspectTacticalEnemy(boss.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeClickAndAdjacentInteractPayForTheActualValveAndRepeatedClicksAreFree(bool keyboard)
    {
        using var world = World(CampaignBossKind.SluiceMaw, out var arena);
        var valve = arena.Fixtures.First(f => f.Kind == CampaignBossFixtureKind.Valve);
        var hero = world.DungeonMember; hero.Position = world.State.Position = valve.Position.Offset(0, 1); world.Reveal();
        string before = JsonSerializer.Serialize(world.State);
        var hover = WorldInspection.Inspect(world, valve.Position);
        Assert.Contains("Channel #1", hover.Detail); Assert.Contains("one action", hover.Detail);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        void Field(string name, object value) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(client, value);
        object? Invoke(string name, params object[] args) => typeof(WorldGame).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, args);
        var camera = new MapCamera { Viewport = new(16, 84, 674, 490) };
        camera.Center = new(valve.Position.X + .5f, valve.Position.Y + .5f);
        Field("_session", world); Field("_camera", camera); Field("_map", camera.Viewport); Field("_roagAim", "move");
        var screen = camera.WorldToScreen(new(valve.Position.X + .5f, valve.Position.Y + .5f));
        var pointer = new Point((int)screen.X, (int)screen.Y);
        long clock = world.DungeonCombat.Clock;
        Invoke(keyboard ? "DungeonInteract" : "RoagClick", keyboard ? [] : [pointer]);
        Assert.True(arena.Channels[valve.Channel].Drained); Assert.True(world.DungeonCombat.Clock > clock);
        Assert.Contains("Valve opened", (string)typeof(WorldGame).GetField("_notice", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(client)!);
        before = JsonSerializer.Serialize(world.State);
        Invoke("RoagClick", pointer);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.Contains("already drained", (string)typeof(WorldGame).GetField("_notice", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(client)!);
        world.Visible.Remove(valve.Position);
        var remembered = WorldInspection.Inspect(world, valve.Position);
        Assert.Contains("remembered", remembered.Title); Assert.DoesNotContain("permanently drained", remembered.Detail);
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
