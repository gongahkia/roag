using System.Text.Json;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private sealed record ConversationSettings(float VoiceVolume = .45f, bool ReducedMotion = false, double CombatSpeed = 1, float CombatVolume = .4f);
    private string SettingsPath => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_options.Save))!, "presentation.json");
    private void LoadConversationSettings()
    {
        if (_options.Preview) return;
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var settings = JsonSerializer.Deserialize<ConversationSettings>(File.ReadAllText(SettingsPath));
            if (settings != null && float.IsFinite(settings.VoiceVolume))
            { _voiceVolume = Math.Clamp(settings.VoiceVolume, 0, 1); _reducedMotion = settings.ReducedMotion;
                _combatSpeed = settings.CombatSpeed == 2 ? 2 : 1;
                _combatVolume = float.IsFinite(settings.CombatVolume) ? Math.Clamp(settings.CombatVolume, 0, 1) : .4f; }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { /* Keep usable defaults. */ }
    }
    private void SaveConversationSettings()
    {
        if (_options.Preview) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            string temporary = SettingsPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new ConversationSettings(_voiceVolume, _reducedMotion, _combatSpeed, _combatVolume)));
            File.Move(temporary, SettingsPath, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { _notice = "Presentation settings apply this session; they could not be saved."; }
    }
}
