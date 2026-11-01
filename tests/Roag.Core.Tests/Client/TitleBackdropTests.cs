using System.Text.Json;
using Microsoft.Xna.Framework;
using Roag.Client;
using Xunit;

namespace Roag.Core.Tests.Client;

public sealed class TitleBackdropTests
{
    [Theory]
    [InlineData(37)]
    [InlineData(42)]
    [InlineData(1337)]
    public void DisposableWorldRunsNativeCombatAndReplacesEndedScenesWithoutASave(int seed)
    {
        using var backdrop = new TitleBackdrop(seed);
        backdrop.Resize(new(0, 0, 1440, 900));
        for (int i = 0; i < 600; i++)
        {
            backdrop.Update(.1, true, false);
            Assert.Null(backdrop.World.SavePath);
            Assert.InRange(backdrop.Effects.Count, 0, 64);
        }
        Assert.True(backdrop.Actions > 20);
        Assert.True(backdrop.Hits > 0, "The background must resolve actual native damage.");
        Assert.True(backdrop.Scenes > 1);
        Assert.NotEqual(Vector2.Zero, backdrop.Camera);
    }

    [Fact]
    public void HoldingOrUnfocusingPausesTheDemoAndReducedMotionKeepsTheCameraStill()
    {
        using var backdrop = new TitleBackdrop(37);
        backdrop.Resize(new(0, 0, 1024, 720)); backdrop.Update(0, true, true);
        string before = JsonSerializer.Serialize(backdrop.World.State);
        var camera = backdrop.Camera;
        for (int i = 0; i < 50; i++) backdrop.Update(.1, false, false);
        Assert.Equal(before, JsonSerializer.Serialize(backdrop.World.State));
        Assert.Equal(camera, backdrop.Camera); Assert.Equal(0, backdrop.Actions);
        for (int i = 0; i < 20; i++) backdrop.Update(.1, true, true);
        Assert.True(backdrop.Actions > 0); Assert.Equal(camera, backdrop.Camera);
    }

    [Fact]
    public void NormalLaunchUsesTitleWhileExplicitRunAndDiagnosticCommandsKeepTheirEntryPoints()
    {
        Assert.True(Options.Parse([]).ShowTitleScreen);
        Assert.True(Options.Parse(["--title", "--new"]).ShowTitleScreen);
        Assert.True(Options.Parse(["--title", "--load"]).ShowTitleScreen);
        foreach (var args in new[] { new[] { "--new" }, ["--load"], ["--skip-intro"],
            ["--scene", "world"], ["--frames", "3"], ["--scene", "town"], ["--world-smoke"] })
            Assert.False(Options.Parse(args).ShowTitleScreen);
    }
}
