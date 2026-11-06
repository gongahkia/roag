using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Roag.Client;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class ExpeditionJournalTests
{
    [Theory]
    [InlineData(1024, 720)]
    [InlineData(1440, 900)]
    public void NativeMenuAndPagedExpeditionHistoryAreFreeAndStayInsideTheMinimumWindow(int width, int height)
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        for (int i = 0; i < 45; i++) world.State.Journal.Add("Travel report " + i + ": the old road leads past a ruined tower.");
        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        var controls = new List<WorldMouseControl>();
        void Field(string name, object? value) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(client, value);
        Field("_controls", controls); Field("_session", world); Field("_options", Options.Parse([])); Field("_panel", "dungeon-menu");
        var build = typeof(WorldGame).GetMethod("BuildDungeonControls", BindingFlags.Instance | BindingFlags.NonPublic)!;
        string before = JsonSerializer.Serialize(world.State); build.Invoke(client, [width, height]);
        Assert.Contains(controls, c => c.Id == "dungeon:journal");
        Assert.All(controls, c => Assert.True(new Rectangle(0, 0, width, height).Contains(c.Bounds), c.Id));
        Field("_panel", "dungeon-journal"); Field("_expeditionPage", 0); controls.Clear(); build.Invoke(client, [width, height]);
        Assert.False(controls.Single(c => c.Id == "dungeon:journal-prev").Enabled);
        Assert.True(controls.Single(c => c.Id == "dungeon:journal-next").Enabled);
        var box = new Rectangle(32, 84, width - 64, height - 202);
        var rows = Assert.IsType<JournalRow[]>(typeof(WorldGame).GetMethod("ExpeditionJournalRows", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, [box.Width - 108]));
        Assert.Contains(rows, r => r.Text.Contains("Ask residents", StringComparison.Ordinal));
        Assert.Contains(rows, r => r.Text.Contains("Travel report 44", StringComparison.Ordinal));
        Field("_expeditionPage", int.MaxValue); controls.Clear(); build.Invoke(client, [width, height]);
        Assert.True(controls.Single(c => c.Id == "dungeon:journal-prev").Enabled);
        Assert.False(controls.Single(c => c.Id == "dungeon:journal-next").Enabled);
        Assert.All(controls.Where(c => c.Id.StartsWith("dungeon:journal-", StringComparison.Ordinal) || c.Id == "dungeon:close"),
            c => Assert.True(box.Contains(c.Bounds), c.Id));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
}
