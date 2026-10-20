using Vagabonds.Client;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.Generation;
using Xunit;

namespace Vagabonds.Tests.Client;

public sealed class CampaignStartupTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-startup-" + Guid.NewGuid().ToString("N"));
    private string Save => Path.Combine(_directory, "campaign.json");

    private Campaign ExistingCampaign()
    {
        var campaign = new Campaign(1337);
        campaign.TravelTo("to-wilds");
        campaign.TravelTo("iron-cache");
        campaign.Save(Save);
        return campaign;
    }

    [Fact]
    public void OrdinaryLaunchResumesExistingProgressAndCanSaveItsContinuation()
    {
        var original = ExistingCampaign();
        byte[] before = File.ReadAllBytes(Save);
        var options = Options.Parse(["--save", Save]);
        var startup = CampaignStartup.Open(options);

        Assert.True(startup.Resumed);
        Assert.False(options.Preview);
        Assert.Equal(original.StateJson(), startup.Campaign.StateJson());
        Assert.Equal(before, File.ReadAllBytes(Save));
        startup.Campaign.Wait();
        original.Wait();
        startup.Campaign.Save(Save);
        Assert.Equal(original.StateJson(), Campaign.Load(Save).StateJson());
    }

    [Fact]
    public void MissingSaveStartsTheRequestedSeedWithoutWritingAFile()
    {
        var startup = CampaignStartup.Open(Options.Parse(["--save", Save, "--seed", "7"]));
        Assert.False(startup.Resumed);
        Assert.Equal(new Campaign(7).StateJson(), startup.Campaign.StateJson());
        Assert.False(File.Exists(Save));
    }

    [Fact]
    public void ExplicitNewStartsOverWithoutWritingUntilSave()
    {
        ExistingCampaign();
        byte[] before = File.ReadAllBytes(Save);
        var startup = CampaignStartup.Open(Options.Parse(["--new", "--seed", "7", "--save", Save]));
        Assert.False(startup.Resumed);
        Assert.Equal(new Campaign(7).StateJson(), startup.Campaign.StateJson());
        Assert.Equal(before, File.ReadAllBytes(Save));
    }

    [Theory]
    [InlineData("--scene", "town")]
    [InlineData("--scene", "forest")]
    [InlineData("--scene", "atlas")]
    [InlineData("--frames", "3")]
    [InlineData("--smoke-test")]
    [InlineData("--demo")]
    public void PreviewsDoNotResumeOrChangeTheExistingSave(params string[] args)
    {
        ExistingCampaign();
        byte[] before = File.ReadAllBytes(Save);
        var options = Options.Parse(["--save", Save, .. args]);
        var startup = CampaignStartup.Open(options);
        Assert.True(options.Preview);
        Assert.False(startup.Resumed);
        Assert.Equal(42, startup.Campaign.Seed);
        Assert.Equal(before, File.ReadAllBytes(Save));
    }

    [Fact]
    public void ExplicitLoadCanCaptureTheExistingCampaign()
    {
        var original = ExistingCampaign();
        var options = Options.Parse(["--load", "--frames", "3", "--save", Save]);
        var startup = CampaignStartup.Open(options);
        Assert.True(options.Preview);
        Assert.True(startup.Resumed);
        Assert.Equal(original.StateJson(), startup.Campaign.StateJson());
    }

    [Fact]
    public void InvalidExistingSaveDoesNotSilentlyStartOver()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Save, "{}");
        Assert.Throws<InvalidDataException>(() => CampaignStartup.Open(Options.Parse(["--save", Save])));
        Assert.Equal("{}", File.ReadAllText(Save));
    }

    [Fact]
    public void ExplicitLoadRequiresAnExistingSave()
    {
        Directory.CreateDirectory(_directory);
        Assert.Throws<FileNotFoundException>(() => CampaignStartup.Open(Options.Parse(["--load", "--save", Save])));
        Assert.False(File.Exists(Save));
    }

    [Theory]
    [InlineData("--load", "--new")]
    [InlineData("--new", "--load")]
    public void ConflictingLaunchModesAreRejected(string first, string second) =>
        Assert.Throws<ArgumentException>(() => Options.Parse([first, second]));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StartupRejectsLunarSessionsWithoutChangingTheSave(bool explicitLoad)
    {
        Campaign.CreateField(42, LunarGenerator.Create("salvage", 42, false, false)).Save(Save);
        byte[] before = File.ReadAllBytes(Save);
        var options = Options.Parse(explicitLoad ? ["--save", Save, "--load"] : ["--save", Save]);
        var error = Assert.Throws<InvalidDataException>(() => CampaignStartup.Open(options));
        Assert.Contains("lunar field session", error.Message);
        Assert.Equal(before, File.ReadAllBytes(Save));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
