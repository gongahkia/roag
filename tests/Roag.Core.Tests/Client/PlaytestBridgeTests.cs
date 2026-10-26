using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Roag.Client;
using Xunit;

namespace Roag.Tests.Client;

public sealed class PlaytestBridgeTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "vagabonds-control-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_folder)) Directory.Delete(_folder, true); }
    private void Request(string id, string kind, long observation = 1, object? extra = null)
    {
        var value = JsonSerializer.SerializeToNode(new { schemaVersion = 1, id, kind, observation })!.AsObject();
        if (extra != null) foreach (var property in JsonSerializer.SerializeToNode(extra)!.AsObject()) value[property.Key] = property.Value?.DeepClone();
        File.WriteAllText(Path.Combine(_folder, "request.json"), value.ToJsonString());
    }
    private static Point[] Resolve(PlaytestRequest r) => r.Kind == "drag" ? [new(10, 20), new(70, 80)] : [new(10, 20)];
    private void Publish(PlaytestBridge bridge) => bridge.Publish("{\"tick\":0}", path => File.WriteAllBytes(path, [137, 80, 78, 71]));
    private JsonElement Response() => JsonDocument.Parse(File.ReadAllText(Path.Combine(_folder, "response.json"))).RootElement.Clone();

    [Fact]
    public void ClicksAndDragsProduceRealMouseEdgesAndFreezeAtObservationBoundaries()
    {
        var bridge = new PlaytestBridge(_folder); Assert.True(bridge.Holding); Publish(bridge);
        Request("click-1", "control");
        var first = bridge.Next(.1, Resolve, _ => { }); Assert.Equal(ButtonState.Released, first.LeftButton); Assert.False(bridge.Holding);
        Assert.True(bridge.RunsFrames); Assert.False(bridge.AdvancesWorldPresentation);
        Assert.Equal(ButtonState.Pressed, bridge.Next(.1, Resolve, _ => { }).LeftButton);
        Assert.Equal(ButtonState.Released, bridge.Next(.1, Resolve, _ => { }).LeftButton);
        Assert.True(bridge.NeedsObservation); Publish(bridge); Assert.True(bridge.Holding); Assert.Equal(2, bridge.Revision);
        Assert.False(bridge.AdvancesWorldPresentation);
        Request("drag-1", "drag", 2);
        var states = new List<MouseState> { bridge.Next(.1, Resolve, _ => { }) };
        while (!bridge.NeedsObservation) states.Add(bridge.Next(.1, Resolve, _ => { }));
        Assert.Equal(new Point(10, 20), states[0].Position); Assert.Equal(new Point(70, 80), states[^1].Position);
        Assert.Equal(7, states.Count(s => s.LeftButton == ButtonState.Pressed));
        Publish(bridge); Assert.True(Response().GetProperty("success").GetBoolean());
    }

    [Fact]
    public void StaleDuplicateMalformedAndInvalidRequestsHaveNoInputEffects()
    {
        var bridge = new PlaytestBridge(_folder); Publish(bridge); int resolutions = 0;
        Point[] Resolver(PlaytestRequest r) { resolutions++; return Resolve(r); }
        Request("stale", "control", 0); bridge.Next(.1, Resolver, _ => { });
        Assert.Equal(0, resolutions); Assert.Contains("Stale", Response().GetProperty("reason").GetString()); Assert.True(bridge.Holding);
        Request("once", "capture"); bridge.Next(.1, Resolver, _ => { }); Publish(bridge);
        Request("once", "capture", 2); bridge.Next(.1, Resolver, _ => { });
        Assert.Contains("Duplicate", Response().GetProperty("reason").GetString()); Assert.Equal(1, resolutions);
        Request("unknown", "teleport", 2); bridge.Next(.1, Resolver, _ => { }); Assert.False(Response().GetProperty("success").GetBoolean());
        Request("fields", "capture", 2, new { cheat = true }); bridge.Next(.1, Resolver, _ => { });
        Assert.False(Response().GetProperty("success").GetBoolean()); Assert.True(bridge.Holding);
        Assert.False(File.Exists(Path.Combine(_folder, "request.json")));
    }

    [Fact]
    public void ExplicitWaitAdvancesOnlyItsRequestedIntervalAndMarksAreDiagnostic()
    {
        var bridge = new PlaytestBridge(_folder); Publish(bridge);
        Request("wait-1", "wait", extra: new { seconds = .5 }); bridge.Next(.1, Resolve, _ => { });
        Assert.True(bridge.AdvancesWorldPresentation);
        Assert.True(bridge.RunsFrames); Assert.False(bridge.NeedsObservation); bridge.Next(.2, Resolve, _ => { }); bridge.Next(.2, Resolve, _ => { });
        Assert.False(bridge.NeedsObservation); bridge.Next(.11, Resolve, _ => { }); Assert.True(bridge.NeedsObservation); Publish(bridge);
        string? marked = null; Request("mark-1", "mark", 2, new { label = "Look here" }); bridge.Next(.1, Resolve, label => marked = label);
        Assert.Equal("Look here", marked); Assert.False(bridge.RunsFrames); Assert.True(bridge.NeedsObservation); Publish(bridge);
        Assert.False(bridge.AdvancesWorldPresentation);
        Request("too-long", "wait", 3, new { seconds = 300 }); bridge.Next(.1, Resolve, _ => { }); Assert.True(bridge.Holding);
        Assert.False(Response().GetProperty("success").GetBoolean());
    }

    [Fact]
    public void AutomationOptionsKeepPlayerSlotsOutsideTheirWritableScope()
    {
        string isolated = Path.Combine(_folder, "run");
        var options = Options.Parse(["--playtest-control", isolated]);
        Assert.True(options.Preview); Assert.Equal(Path.Combine(isolated, "campaign.json"), options.Save);
        Assert.Equal(Path.Combine(isolated, "recordings"), options.SessionRoot);
        Assert.Throws<ArgumentException>(() => Options.Parse(["--playtest-control", isolated, "--save", Path.Combine(_folder, "player.json")]));
        Assert.Throws<ArgumentException>(() => Options.Parse(["--playtest-control", isolated, "--scene", "battle"]));
        Assert.Throws<ArgumentException>(() => Options.Parse(["--playtest-control", isolated, "--world-headless"]));
        Assert.False(Options.Parse(["--session-root", _folder]).Preview);
        Assert.True(Options.Parse(["--no-recording"]).NoRecording);
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(isolated);
            File.WriteAllText(Path.Combine(_folder, "player.json"), "preserve");
            File.CreateSymbolicLink(Path.Combine(isolated, "linked.json"), Path.Combine(_folder, "player.json"));
            Assert.Throws<ArgumentException>(() => Options.Parse(["--playtest-control", isolated, "--save", Path.Combine(isolated, "linked.json")]));
        }
    }
}
