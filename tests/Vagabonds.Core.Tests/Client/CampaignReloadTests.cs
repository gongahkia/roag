using System.Reflection;
using System.Runtime.CompilerServices;
using Vagabonds.Client;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.Generation;
using Vagabonds.Graphics;
using Xunit;

namespace Vagabonds.Tests.Client;

public sealed class CampaignReloadTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-reload-" + Guid.NewGuid().ToString("N"));
    private string Save => Path.Combine(_directory, "campaign.json");
    private static FieldInfo Field(string name) => typeof(VagabondsGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    private VagabondsGame Client(Campaign campaign, bool autosave)
    {
        // Exercise the actual F9 handler in CI without opening an SDL window.
        var client = (VagabondsGame)RuntimeHelpers.GetUninitializedObject(typeof(VagabondsGame));
        GC.SuppressFinalize(client);
        Field("_options").SetValue(client, Options.Parse(["--save", Save]));
        Field("_campaign").SetValue(client, campaign);
        Field("_autoSave").SetValue(client, autosave);
        Field("_route").SetValue(client, new List<Cell> { campaign.Position + new Cell(1, 0) });
        Field("_routeObject").SetValue(client, "wood-camp");
        Field("_lastActive").SetValue(client, 7);
        Field("_eventWatermark").SetValue(client, 123);
        Field("_camera").SetValue(client, new MapCamera());
        Field("_popups").SetValue(client, Activator.CreateInstance(Field("_popups").FieldType));
        return client;
    }

    private static void Reload(VagabondsGame client) =>
        typeof(VagabondsGame).GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, null);

    [Theory]
    [InlineData("field", false)]
    [InlineData("field", true)]
    [InlineData("invalid", false)]
    [InlineData("invalid", true)]
    [InlineData("malformed", false)]
    [InlineData("malformed", true)]
    [InlineData("missing", false)]
    [InlineData("missing", true)]
    public void FailedF9LoadPreservesCampaignRouteAndAutosave(string kind, bool autosave)
    {
        Directory.CreateDirectory(_directory);
        if (kind == "field") Campaign.CreateField(42, LunarGenerator.Create("salvage", 42, false, false)).Save(Save);
        else if (kind != "missing") File.WriteAllText(Save, kind == "invalid" ? "{}" : "{");
        byte[]? source = File.Exists(Save) ? File.ReadAllBytes(Save) : null;
        var campaign = new Campaign(1337);
        campaign.TravelTo("to-wilds");
        campaign.TravelTo("wilds-hounds");
        string before = campaign.StateJson();
        var client = Client(campaign, autosave);
        var route = (List<Cell>)Field("_route").GetValue(client)!;
        var pending = route.ToArray();

        Reload(client);

        Assert.Same(campaign, Field("_campaign").GetValue(client));
        Assert.Equal(before, campaign.StateJson());
        Assert.Equal(autosave, (bool)Field("_autoSave").GetValue(client)!);
        Assert.Equal(pending, route);
        Assert.Equal("wood-camp", Field("_routeObject").GetValue(client));
        Assert.Equal(7, Field("_lastActive").GetValue(client));
        Assert.Equal(123, Field("_eventWatermark").GetValue(client));
        Assert.StartsWith("Load failed: ", (string)Field("_toast").GetValue(client)!);
        if (source is null) Assert.False(File.Exists(Save));
        else Assert.Equal(source, File.ReadAllBytes(Save));
    }

    [Fact]
    public void SuccessfulF9LoadRestoresProgressAndResumesAutosaving()
    {
        var saved = new Campaign(1337);
        saved.TravelTo("to-wilds");
        saved.TravelTo("iron-cache");
        saved.TravelTo("wilds-hounds");
        saved.Save(Save);
        var client = Client(new Campaign(7), false);

        Reload(client);

        var loaded = (Campaign)Field("_campaign").GetValue(client)!;
        Assert.Equal(saved.StateJson(), loaded.StateJson());
        Assert.True((bool)Field("_autoSave").GetValue(client)!);
        Assert.Empty((List<Cell>)Field("_route").GetValue(client)!);
        Assert.Null(Field("_routeObject").GetValue(client));
        Assert.Equal(-1, Field("_lastActive").GetValue(client));
        Assert.Equal("Expedition restored.", Field("_toast").GetValue(client));
        saved.AutoBattle();
        loaded.AutoBattle();
        Assert.Equal(saved.StateJson(), loaded.StateJson());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
