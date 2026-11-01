using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Client;

public sealed class DungeonFeedbackTests
{
    [Theory]
    [InlineData(1024, 720)]
    [InlineData(1440, 900)]
    public void AllClassCardsHaveTheirPortraitAndDescriptionInsideAResponsiveGrid(int width, int height)
    {
        using var world = WorldSession.CreateDungeonRun(42);
        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        void Field(string name, object value) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(client, value);
        var controls = new List<WorldMouseControl>();
        Field("_controls", controls); Field("_session", world); Field("_panel", "dungeon-class");
        string before = JsonSerializer.Serialize(world.State);
        typeof(WorldGame).GetMethod("BuildDungeonControls", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, [width, height]);
        var cards = controls.Where(c => c.Id.StartsWith("dungeon:class:", StringComparison.Ordinal)).ToArray();
        Assert.Equal(15, cards.Length);
        var panel = new Rectangle(32, 84, width - 64, height - 202);
        foreach (var kind in Enum.GetValues<ClassKind>())
        {
            var card = cards.Single(c => c.Id == "dungeon:class:" + (int)kind);
            Assert.Equal(NativeAppearance.ForClass(kind), card.Sprite); Assert.False(string.IsNullOrWhiteSpace(card.Detail));
            Assert.True(panel.Contains(card.Bounds));
            var icon = (Rectangle)typeof(WorldGame).GetMethod("ControlSpriteBox", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [card])!;
            Assert.True(card.Bounds.Contains(icon));
            // Even the widest bundled font fits each intentionally short line.
            Assert.All(card.Detail.Split('\n'), line => Assert.True(line.Length * 9 <= card.Bounds.Right - icon.Right - 20));
        }
        for (int i = 0; i < cards.Length; i++) for (int j = i + 1; j < cards.Length; j++) Assert.False(cards[i].Bounds.Intersects(cards[j].Bounds));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void FeedbackAndChestRevealUseOnlyPresentationTimeAndRespectReducedMotion()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        string before = JsonSerializer.Serialize(world.State); var p = world.State.Position;
        var feedback = new DungeonFeedback(); feedback.OpenChest();
        feedback.Observe(new(1, 0, "DamageTaken", 2, 1, 1, p, 4), "-4", Facing.East, null, true);
        feedback.Observe(new(2, 0, "BoonGranted", 1, 1, 1, p, Ability: "core:boon/storm_conductor"), null, Facing.East, p, false);
        feedback.Advance(.03, true, p.Floor);
        Assert.True(feedback.Time > feedback.ChestOpened); Assert.NotNull(feedback.Reward);
        Assert.Null(Assert.Single(feedback.Effects).From); // no invented offscreen source/tracer
        Assert.NotEqual(Vector2.Zero, feedback.Shake(new()));
        Assert.Equal(Vector2.Zero, feedback.Shake(new(ReducedMotion: true)));
        Assert.Equal(Vector2.Zero, feedback.Shake(new(Shake: false)));
        double stopped = feedback.Time; feedback.Advance(.1, false, p.Floor); Assert.Equal(stopped, feedback.Time);
        for (int i = 0; i < 25; i++) feedback.Advance(.1, true, p.Floor);
        Assert.Empty(feedback.Effects); Assert.Null(feedback.Reward);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void FeedbackIsBoundedAndOldFloorEffectsAreRetired()
    {
        var feedback = new DungeonFeedback(); var p = new WorldPosition(128, 128, 1);
        for (int i = 0; i < 100; i++) feedback.Observe(new(i, 0, "AttackPerformed", 1, 0, i, p), null, Facing.East, p, false);
        Assert.Equal(64, feedback.Effects.Count);
        feedback.Advance(.01, true, 2); Assert.Empty(feedback.Effects);
        feedback.OpenChest(); feedback.Reset(); Assert.True(feedback.ChestOpened < 0); Assert.Equal(0, feedback.Time);
    }
}
