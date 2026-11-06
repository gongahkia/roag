using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework.Input;
using Roag.Client;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Client;

public sealed class GeneratedCampaignInputTests
{
    internal static void AssertContinuedPaidInput(WorldSession world)
    {
        Assert.True(world.State.Endgame.Victory); Assert.False(world.ExpeditionEndedByVictory);
        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        static FieldInfo Field(string name) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
        object? Invoke(string name, params object[] arguments)
            => typeof(WorldGame).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, arguments);
        Field("_session").SetValue(client, world); Field("_panel").SetValue(client, ""); Field("_roagAim").SetValue(client, "move");
        string before = JsonSerializer.Serialize(world.State);
        Invoke("HandleRoagKeyboard", new KeyboardState(Keys.B), true);
        Assert.Equal("dungeon-party", Field("_panel").GetValue(client));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Field("_panel").SetValue(client, ""); Invoke("HandleRoagKeyboard", new KeyboardState(), true);
        long clock = world.DungeonCombat.Clock;
        Invoke("HandleRoagKeyboard", new KeyboardState(Keys.Space), true);
        Assert.True(world.DungeonCombat.Clock > clock); Assert.True(world.DungeonMember.Health > 0);
        Assert.True(world.State.Endgame.Victory);
    }

    [Fact]
    public void LegacyVictoryStillEndsItsExpeditionWhileNativeVictoryIsAContinuingMilestone()
    {
        using var legacy = WorldSession.Create(42); legacy.State.Endgame.Victory = true;
        Assert.True(legacy.ExpeditionEndedByVictory); Assert.True(legacy.Frozen);
        using var native = WorldSession.CreateDungeonRun(42); native.State.Endgame.Victory = true;
        native.Paused = false; native.Focused = true;
        Assert.False(native.ExpeditionEndedByVictory); Assert.False(native.Frozen);
        // Input-only predicate setup. Actual final deaths/saves/paid keyboard
        // continuation run through AssertContinuedPaidInput in generated worlds.
    }
}
