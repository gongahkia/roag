using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Roag.Client;
using Roag.Core.Dungeons;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class SpecialDungeonPresentationTests
{
    [Theory]
    [InlineData(1024, 720)]
    [InlineData(1440, 900)]
    public void ActivationIsExplicitPaidAndLocalWhileReviewAndDefaultCancellationAreFree(int width, int height)
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var complex = world.InstallDungeonComplex(DungeonComplexPlanner.Generate(world.Plan, DungeonParcelPlanner.Reserve(world.Plan, 1).Single()));
        complex.KnownEntrance = true; complex.Special = new() { Aftermath = SpecialDungeonAftermath.LethalCollapse };
        var seal = WorldSession.SpecialDungeonActivation(complex);
        var hero = world.DungeonMember;
        hero.Position = world.State.Position = WorldSession.Directions.Select(d => seal.Offset(d.X, d.Y)).First(p => world.CanEnter(p, hero.Id));
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        var controls = new List<WorldMouseControl>();
        void Field(string name, object? value) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(client, value);
        Field("_controls", controls); Field("_session", world); Field("_options", Options.Parse([])); Field("_panel", "dungeon-specials");
        var build = typeof(WorldGame).GetMethod("BuildDungeonControls", BindingFlags.Instance | BindingFlags.NonPublic)!;
        string before = JsonSerializer.Serialize(world.State); build.Invoke(client, [width, height]);
        Assert.True(controls.Single(c => c.Id == "dungeon:special-review").Enabled);
        Assert.Null(complex.Special.ActivatedAt);
        Field("_panel", "dungeon-special-confirm"); controls.Clear(); build.Invoke(client, [width, height]);
        Assert.True(controls.Single(c => c.Id == "dungeon:special-activate").Enabled);
        Assert.True(controls.FindIndex(c => c.Id == "dungeon:special-cancel") < controls.FindIndex(c => c.Id == "dungeon:special-activate"));
        var box = new Rectangle(32, 84, width - 64, height - 202);
        Assert.All(controls.Where(c => c.Id.StartsWith("dungeon:special-", StringComparison.Ordinal)), c => Assert.True(box.Contains(c.Bounds), c.Id));
        Assert.Contains("Lethal collapse", Assert.Single(world.KnownSpecialDungeons()).Warning);
        Assert.Equal("Unstable extraction seal", WorldInspection.Inspect(world, seal).Title);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(Roag.Core.Tactics.AttackGeometry.TryFacing(seal.X - hero.Position.X, seal.Y - hero.Position.Y, out var facing));
        Assert.True(world.SubmitDungeon(new(Roag.Core.Tactics.TacticalAction.Move, facing)).Success);
        before = JsonSerializer.Serialize(world.State); controls.Clear(); build.Invoke(client, [width, height]);
        Assert.False(controls.Single(c => c.Id == "dungeon:special-activate").Enabled);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
}
