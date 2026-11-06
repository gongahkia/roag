using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Roag.Client;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class NativeProfileClientTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "roag-profile-client-" + Guid.NewGuid().ToString("N"));
    private string Slot => Path.Combine(_directory, "campaign.json");
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

    [Fact]
    public void NormalFreshStartupEnrollsAndExplicitPreviewRemainsIneligibleUnlessRequested()
    {
        using var fresh = Assert.IsType<WorldSession>(WorldStartup.Open(Options.Parse(["--new", "--seed", "42", "--save", Slot])).Session);
        Assert.True(fresh.HasPermanentProfile); Assert.Equal(Slot + ".profile", fresh.ProfileDirectory);
        Assert.False(File.Exists(Slot)); // Starting/reserving a profile is separate from replacing a world.
        using var preview = Assert.IsType<WorldSession>(WorldStartup.Open(Options.Parse(["--new", "--frames", "1", "--save", Slot])).Session);
        Assert.False(preview.HasPermanentProfile);
        string custom = Path.Combine(_directory, "explicit-profile");
        using var explicitProfile = Assert.IsType<WorldSession>(WorldStartup.Open(Options.Parse(["--new", "--frames", "1", "--save", Slot, "--profile", custom])).Session);
        Assert.True(explicitProfile.HasPermanentProfile); Assert.Equal(custom, explicitProfile.ProfileDirectory);
        explicitProfile.Save(Slot); byte[] before = File.ReadAllBytes(Slot);
        using var loaded = Assert.IsType<WorldSession>(WorldStartup.Open(Options.Parse(["--load", "--save", Slot, "--profile", custom])).Session);
        Assert.Equal(JsonSerializer.Serialize(explicitProfile.State), JsonSerializer.Serialize(loaded.State)); Assert.Equal(before, File.ReadAllBytes(Slot));
    }

    [Fact]
    public void AutomatedProfilesStayInsideTheIsolatedControlDirectoryAndRejectSymlinks()
    {
        string control = Path.Combine(_directory, "control");
        var options = Options.Parse(["--playtest-control", control]); Assert.Equal(options.Save + ".profile", options.ProfileDirectory);
        Assert.Throws<ArgumentException>(() => Options.Parse(["--playtest-control", control, "--profile", Path.Combine(_directory, "real-profile")]));
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(control); Directory.CreateDirectory(Path.Combine(_directory, "real-profile"));
            string link = Path.Combine(control, "linked-profile"); Directory.CreateSymbolicLink(link, Path.Combine(_directory, "real-profile"));
            Assert.Throws<ArgumentException>(() => Options.Parse(["--playtest-control", control, "--profile", link]));
        }
    }

    [Theory]
    [InlineData(1024, 720)]
    [InlineData(1440, 900)]
    public void AchievementPagesExposeAllTwentyTwoEntriesWithFreeBoundedControls(int width, int height)
    {
        using var world = WorldSession.CreateDungeonRun(42); world.EnablePermanentProfile(Slot);
        var client = (WorldGame)RuntimeHelpers.GetUninitializedObject(typeof(WorldGame)); GC.SuppressFinalize(client);
        var controls = new List<WorldMouseControl>();
        void Field(string name, object? value) => typeof(WorldGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(client, value);
        Field("_controls", controls); Field("_session", world); Field("_options", Options.Parse(["--save", Slot]));
        Field("_panel", "dungeon-profile");
        var build = typeof(WorldGame).GetMethod("BuildDungeonControls", BindingFlags.Instance | BindingFlags.NonPublic)!;
        string before = JsonSerializer.Serialize(world.State);
        for (int page = 0; page < 8; page++)
        {
            Field("_profilePage", page); controls.Clear(); build.Invoke(client, [width, height]);
            Assert.Equal(page > 0, controls.Single(c => c.Id == "dungeon:profile-prev").Enabled);
            Assert.Equal(page < 7, controls.Single(c => c.Id == "dungeon:profile-next").Enabled);
            Assert.All(controls, c => Assert.True(new Rectangle(0, 0, width, height).Contains(c.Bounds), c.Id));
        }
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
}
