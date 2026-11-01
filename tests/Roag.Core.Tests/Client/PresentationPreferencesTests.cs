using Roag.Client;
using Xunit;

namespace Roag.Core.Tests.Client;

public sealed class PresentationPreferencesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "roag-preferences-" + Guid.NewGuid().ToString("N"));
    private string Slot => Path.Combine(_root, "presentation.json");

    [Fact]
    public void MissingAndDamagedSettingsFallBackWithoutCreatingFilesOrChangingTheRun()
    {
        Assert.Equal(new(), PresentationPreferences.Load(Slot)); Assert.False(Directory.Exists(_root));
        Directory.CreateDirectory(_root);
        foreach (string invalid in new[] { "null", "{", "{\"master\":101}", "{\"master\":1.5}", "{\"format\":2}", "{\"font\":\"other\"}", "{\"font\":0}", "{\"unexpected\":true}" })
        {
            File.WriteAllText(Slot, invalid); Assert.Equal(new(), PresentationPreferences.Load(Slot)); Assert.Equal(invalid, File.ReadAllText(Slot));
        }
    }

    [Fact]
    public void PreferencesPersistIndependentlyAndMasterMuteCoversBothChannels()
    {
        var value = new PresentationPreferences(Master: 50, Effects: 80, Music: 60, Shake: false, Flash: false,
            CombatEffects: false, DamageNumbers: false, Font: UiFontFace.Sans, TextSize: UiTextSize.Large, HighContrast: true, ReducedMotion: true);
        value.Save(Slot); Assert.Equal(value, PresentationPreferences.Load(Slot)); Assert.Single(Directory.GetFiles(_root));
        Assert.Equal(.4f, value.EffectsVolume); Assert.Equal(.3f, value.MusicVolume);
        Assert.Equal(0, (value with { Master = 0 }).EffectsVolume); Assert.Equal(0, (value with { Master = 0 }).MusicVolume);
        Assert.False(value.Animate);
        Assert.Throws<InvalidDataException>(() => (value with { Effects = -1 }).Save(Slot)); Assert.Equal(value, PresentationPreferences.Load(Slot));
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
