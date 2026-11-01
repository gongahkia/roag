using Microsoft.Xna.Framework;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private PresentationPreferences _preferences = new();
    private string DungeonPreferencesPath => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_options.Save))!, "dungeon-presentation.json");
    private void LoadDungeonPreferences()
    {
        if (_session.State.Dungeon == null) return;
        _preferences = PresentationPreferences.Load(DungeonPreferencesPath); ApplyDungeonPreferences();
    }
    private void ApplyDungeonPreferences()
    {
        if (_session.State.Dungeon == null) return;
        _reducedMotion = _preferences.ReducedMotion; _combatVolume = _preferences.EffectsVolume;
        _uiFonts?.Select(_preferences.Font.ToString(), _preferences.TextSize.ToString());
        if (_combatVolume == 0) _combatAudio?.Stop();
        _terrainLayerDirty = true;
    }
    private void SaveDungeonPreferences()
    {
        ApplyDungeonPreferences();
        if (_options.Preview && _options.PlaytestDirectory == null) return;
        try { _preferences.Save(DungeonPreferencesPath); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { _notice = "Presentation settings apply this session; they could not be saved."; }
    }
    private Color NativeContrast(Color color)
    {
        if (_session.State.Dungeon == null || !_preferences.HighContrast) return color;
        return color == Ink ? Color.White : color == Muted ? new(204, 214, 228) : color == Gold ? new(255, 231, 103)
            : color == Panel ? new(8, 12, 18) : color;
    }
    private void ChangeDungeonPreference(string key)
    {
        static int Volume(int value) => value >= 100 ? 0 : Math.Min(100, value + 25);
        _preferences = key switch
        {
            "master" => _preferences with { Master = Volume(_preferences.Master) },
            "music" => _preferences with { Music = Volume(_preferences.Music) },
            "effects" => _preferences with { Effects = Volume(_preferences.Effects) },
            "shake" => _preferences with { Shake = !_preferences.Shake }, "flash" => _preferences with { Flash = !_preferences.Flash },
            "combat-effects" => _preferences with { CombatEffects = !_preferences.CombatEffects },
            "numbers" => _preferences with { DamageNumbers = !_preferences.DamageNumbers },
            "font" => _preferences with { Font = _preferences.Font == UiFontFace.Pixel ? UiFontFace.Sans : UiFontFace.Pixel },
            "text-size" => _preferences with { TextSize = (UiTextSize)(((int)_preferences.TextSize + 1) % 3) },
            "contrast" => _preferences with { HighContrast = !_preferences.HighContrast },
            "motion" => _preferences with { ReducedMotion = !_preferences.ReducedMotion }, _ => _preferences
        };
        SaveDungeonPreferences();
    }
}
