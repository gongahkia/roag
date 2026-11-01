using Microsoft.Xna.Framework;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private sealed record DungeonEffect(WorldPosition Position, string Kind, string Label, double Born);
    private readonly List<DungeonEffect> _dungeonEffects = [];
    private DungeonAudio? _dungeonAudio;
    private double _lastDungeonHit = -10;
    private (int Floor, bool Victory, bool Defeat, bool Boss)? _audioContext;
    private string _dungeonAudioScene = "";

    private void PlayDungeonSound(string kind)
    {
        if (_session.State.Dungeon == null || _uiFonts == null || _preferences.EffectsVolume <= 0) return;
        _dungeonAudio ??= new(); _dungeonAudio.Play(kind, _preferences.EffectsVolume);
    }

    private void UpdateDungeonPresentation(double elapsed)
    {
        if (_session.State.Dungeon is not { } run) return;
        _dungeonEffects.RemoveAll(e => _combatVisualTime - e.Born > 1.1 || e.Position.Floor != _session.State.Position.Floor);
        var floor = run.Floors.Single(f => f.Contains(_session.State.Position));
        bool boss = floor.Boss > 0 && _session.State.Actors.Any(a => a.Id == floor.Boss && a.Life == LifeState.Alive && _session.Visible.Contains(a.Position));
        var context = (_session.State.Position.Floor, run.Victory, _session.State.AwaitingContinuation, boss);
        if (_audioContext != context)
        {
            _audioContext = context;
            _dungeonAudioScene = run.Victory ? "victory" : _session.State.AwaitingContinuation ? "defeat"
                : run.Stage == 4 ? "final_boss" : (boss ? "elite/" : "explore/") + floor.Biome;
        }
        _dungeonAudio ??= new();
        _dungeonAudio.Update(_dungeonAudioScene, _preferences.MusicVolume, _preferences.EffectsVolume, !InputFocused, elapsed);
    }

    private void ShowDungeonEvent(TacticalEvent e)
    {
        if (_uiFonts == null) return; // Input-only clients have no loaded presentation resources.
        string? sample = e.Kind switch
        {
            "Moved" when _session.State.Party.Contains(e.Source) => "step", "TileEntered" when _session.State.Party.Contains(e.Source) => "terrain",
            "WindupStarted" or "EliteEngaged" or "ElitePhaseChanged" or "RegentPhaseChanged" => "enemy", "DamageTaken" when _session.State.Party.Contains(e.Target) => "hit",
            "BoonActivated" or "BoonGranted" or "StatIncreased" or "SecretDiscovered" or "SidequestCompleted" => "boon",
            "DoorOpened" or "VaultOpened" or "VaultKeyCollected" or "ChestClaimed" or "FloorChanged" or "CurrencyCollected" => "menu",
            "TerrainTriggered" => "terrain", "Died" or "Downed" => "hit", _ => null
        };
        if (sample != null) PlayDungeonSound(sample);
        string? label = e.Kind switch
        {
            "DamageTaken" => "-" + e.Amount, "Healed" => "+" + e.Amount, "DamageBlocked" => "BLOCK",
            "Downed" => "DOWN", "Died" => "FALLEN", "GuardRaised" => "GUARD", "LevelUp" => "LEVEL " + e.Amount,
            "EliteEngaged" => "BOSS", "ElitePhaseChanged" or "RegentPhaseChanged" => "PHASE 2", "Victory" => "VICTORY",
            "BoonActivated" => "BOON", "SupplyCollected" => "HEAL", "CurrencyCollected" => "+" + e.Amount + " GOLD", _ => null
        };
        if (e.Kind == "DamageTaken") _lastDungeonHit = _combatVisualTime;
        if (label != null || e.Kind == "AttackPerformed")
        {
            _dungeonEffects.Add(new(e.Position, e.Kind, label ?? "", _combatVisualTime));
            if (_dungeonEffects.Count > 64) _dungeonEffects.RemoveRange(0, _dungeonEffects.Count - 64);
        }
    }

    private Vector2 DungeonShake()
    {
        if (_session.State.Dungeon == null || !_preferences.Shake || _preferences.ReducedMotion) return Vector2.Zero;
        double age = _combatVisualTime - _lastDungeonHit;
        if (age < 0 || age > .16) return Vector2.Zero;
        float amount = (float)(2 * (1 - age / .16));
        return new(MathF.Sin((float)age * 117) * amount, MathF.Cos((float)age * 89) * amount);
    }

    private void DrawDungeonFeedback()
    {
        foreach (var group in _dungeonEffects.Where(e => _session.Visible.Contains(e.Position)).GroupBy(e => e.Position))
        {
            var rect = TileRect(group.Key); int row = 0;
            foreach (var e in group.TakeLast(3))
            {
                double age = _combatVisualTime - e.Born; float alpha = (float)Math.Clamp((1.1 - age) * 3, 0, 1);
                bool damage = e.Kind == "DamageTaken";
                Color tint = damage ? new(255, 132, 108) : e.Kind == "Healed" ? new(133, 236, 162) : Gold;
                if (_preferences.Flash && damage && age <= .12) FillClipped(rect, Color.White * .16f);
                if (_preferences.CombatEffects && age <= .25)
                {
                    int inset = _preferences.ReducedMotion ? 2 : 2 + (int)(age * 12);
                    FillClipped(new(rect.X + inset, rect.Y + inset, Math.Max(2, rect.Width - 2 * inset), 2), tint * alpha);
                    FillClipped(new(rect.X + inset, rect.Bottom - inset - 2, Math.Max(2, rect.Width - 2 * inset), 2), tint * alpha);
                }
                if (!_preferences.DamageNumbers || e.Label.Length == 0) continue;
                int width = e.Label.Length * (_uiFonts?.Advance ?? 8), y = rect.Top - 16 - row++ * 18 - (_preferences.ReducedMotion ? 0 : (int)(age * 14));
                FillClipped(new(rect.Center.X - width / 2 - 3, y - 2, width + 6, 18), Panel * alpha);
                Text(e.Label, rect.Center.X - width / 2, y, tint * alpha);
            }
        }
    }
}
