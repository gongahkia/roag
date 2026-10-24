using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Vagabonds.Client;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.Generation;
using Vagabonds.Core.World;
using Vagabonds.Graphics;
using Xunit;

namespace Vagabonds.Tests.Client;

public sealed class WorldClientTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-world-client-" + Guid.NewGuid().ToString("N"));
    private string Slot => Path.Combine(_root, "campaign.json");
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    [Fact]
    public void OrdinaryLaunchResumesWorldAndMissingSlotStartsAloneWithoutWriting()
    {
        var fresh = WorldStartup.Open(Options.Parse(["--save", Slot, "--seed", "1337"]));
        Assert.False(fresh.Resumed); var s = Assert.IsType<WorldSession>(fresh.Session); Assert.Single(s.Party); Assert.False(s.Paused); Assert.False(File.Exists(Slot));
        s.Paused = false; s.AdvanceTicks(47); s.Save(Slot); byte[] before = File.ReadAllBytes(Slot);
        var resumed = WorldStartup.Open(Options.Parse(["--save", Slot]));
        Assert.True(resumed.Resumed); var loaded = Assert.IsType<WorldSession>(resumed.Session);
        Assert.Equal(1337, loaded.Seed); Assert.Equal(47, loaded.State.Tick); Assert.False(loaded.Paused); Assert.Equal(before, File.ReadAllBytes(Slot));
    }
    [Fact]
    public void LegacySavesStillDispatchToTheirOriginalRules()
    {
        var old = new Campaign(1337); old.TravelTo("to-wilds"); old.TravelTo("iron-cache"); old.Save(Slot);
        var loaded = WorldStartup.Open(Options.Parse(["--save", Slot]));
        Assert.True(loaded.Resumed); Assert.Equal(old.StateJson(), Assert.IsType<LegacyCampaignSession>(loaded.Session).Campaign.StateJson());
    }
    [Theory]
    [InlineData("--new")]
    [InlineData("--scene", "world")]
    [InlineData("--frames", "2")]
    [InlineData("--world-smoke")]
    [InlineData("--ui-report", "artifacts/test-ui.json")]
    public void ExplicitFreshOrPreviewDoesNotTouchExistingProgress(params string[] flags)
    {
        WorldSession.Create(1337).Save(Slot); byte[] before = File.ReadAllBytes(Slot);
        var started = WorldStartup.Open(Options.Parse([.. flags, "--save", Slot, "--seed", "7"]));
        Assert.False(started.Resumed); Assert.Equal(7, started.Session.Seed); Assert.Equal(before, File.ReadAllBytes(Slot));
        Assert.Equal(Options.Parse(flags).Preview, Assert.IsType<WorldSession>(started.Session).Paused);
    }
    private static FieldInfo Field(string name) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
    private WorldGame Client(WorldSession session)
    {
        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        Field("_options").SetValue(client, Options.Parse(["--save", Slot])); Field("_session").SetValue(client, session);
        Field("_route").SetValue(client, new Queue<WorldPosition>(new[] { session.State.Position.Offset(1, 0) }));
        Field("_camera").SetValue(client, new MapCamera()); Field("_autoSave").SetValue(client, false);
        Field("_combatFloats").SetValue(client, Activator.CreateInstance(Field("_combatFloats").FieldType));
        Field("_panel").SetValue(client, "inventory"); return client;
    }
    private static void Reload(WorldGame client) => typeof(WorldGame).GetMethod("TryLoad", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, null);
    [Theory]
    [InlineData("field")]
    [InlineData("legacy")]
    [InlineData("invalid")]
    [InlineData("malformed")]
    [InlineData("missing")]
    [InlineData("chunk")]
    public void FailedWorldF9PreservesCurrentSessionRoutePanelAndAutosave(string kind)
    {
        Directory.CreateDirectory(_root);
        if (kind == "field") Campaign.CreateField(42, LunarGenerator.Create("salvage", 42, false, false)).Save(Slot);
        else if (kind == "legacy") new Campaign(42).Save(Slot);
        else if (kind == "chunk") { WorldSession.Create(42).Save(Slot); File.AppendAllText(Directory.GetFiles(Slot + ".chunks")[0], " "); }
        else if (kind != "missing") File.WriteAllText(Slot, kind == "invalid" ? "{}" : "{");
        var s = WorldSession.Create(1337); string before = JsonSerializer.Serialize(s.State); var client = Client(s);
        Reload(client);
        Assert.Same(s, Field("_session").GetValue(client)); Assert.Equal(before, JsonSerializer.Serialize(s.State));
        Assert.False((bool)Field("_autoSave").GetValue(client)!); Assert.Single((Queue<WorldPosition>)Field("_route").GetValue(client)!);
        Assert.Equal("inventory", Field("_panel").GetValue(client)); Assert.StartsWith("Load failed:", (string)Field("_notice").GetValue(client)!);
    }
    [Fact]
    public void SuccessfulWorldLoadResumesTimeAndClearsPendingInput()
    {
        var s = WorldSession.Create(1337); s.Save(Slot); var client = Client(WorldSession.Create(7));
        Reload(client);
        var loaded = Assert.IsType<WorldSession>(Field("_session").GetValue(client)); Assert.Equal(1337, loaded.Seed); Assert.False(loaded.Paused);
        Assert.True((bool)Field("_autoSave").GetValue(client)!); Assert.Empty((Queue<WorldPosition>)Field("_route").GetValue(client)!); Assert.Equal("", Field("_panel").GetValue(client));
    }
    [Fact]
    public void InvalidExistingWorldNeverSilentlyStartsOver()
    {
        Directory.CreateDirectory(_root); File.WriteAllText(Slot, "{}");
        Assert.Throws<InvalidDataException>(() => WorldStartup.Open(Options.Parse(["--save", Slot])));
        Assert.Equal("{}", File.ReadAllText(Slot));
    }
}
