using System.Diagnostics;
using Microsoft.Xna.Framework;
using Roag.Core.Expeditions;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private readonly DungeonFeedback _dungeonFeedback = new();
    private long _dungeonPresentationStamp;
    private DungeonAudio? _dungeonAudio;
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
        long now = Stopwatch.GetTimestamp();
        double visualElapsed = _dungeonPresentationStamp == 0 ? 0 : Stopwatch.GetElapsedTime(_dungeonPresentationStamp, now).TotalSeconds;
        _dungeonPresentationStamp = now;
        // Native effects continue through menus, independently of paid game time.
        _dungeonFeedback.Advance(visualElapsed, InputFocused || _playtest != null, _session.State.Position.Floor);
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
            "AttackPerformed" when _session.State.Party.Contains(e.Source) => "attack",
            "DamageTaken" => "hit", "BoonGranted" or "VaultOpened" => "chest",
            "BoonActivated" or "StatIncreased" or "SecretDiscovered" or "SidequestCompleted" => "boon",
            "CampAlarm" => "enemy", "ActivityStopped" => "menu", "Revived" => "boon", "Recruited" or "RescueNoticed" => "menu",
            "DoorOpened" or "VaultKeyCollected" or "FloorChanged" or "CurrencyCollected" => "menu",
            "FoodEaten" or "ProvisionsCollected" => "menu", "StarvationDamage" => "hit",
            "TerrainTriggered" => "terrain", "Died" or "Downed" => "hit", _ => null
        };
        if (sample != null) PlayDungeonSound(sample);
        string? label = e.Kind switch
        {
            "DamageTaken" or "StarvationDamage" => "-" + e.Amount, "Healed" or "WellFedHealing" => "+" + e.Amount, "DamageBlocked" => "BLOCK",
            "CampAlarm" => "ALARM", "ActivityStopped" => e.Ability.StartsWith("completed", StringComparison.Ordinal) ? "DONE" : "AWAKE",
            "FoodEaten" => "FED", "ProvisionsCollected" => "+" + e.Amount + " FOOD",
            "Downed" => "DOWN", "Died" => "FALLEN", "GuardRaised" => "GUARD", "LevelUp" => "LEVEL " + e.Amount,
            "Revived" => "REVIVED +" + e.Amount, "Recruited" => "JOINED", "AllyCovered" => "COVER " + e.Amount, "RescueNoticed" => "AID",
            "EliteEngaged" => "BOSS", "ElitePhaseChanged" or "RegentPhaseChanged" => "PHASE 2", "Victory" => "VICTORY",
            "SupplyCollected" when e.Ability == "ability.medicine" || _session.State.Tactics!.Events.Any(p => p.Kind == "FoodEaten" && p.Action == e.Action && p.Source == e.Source) => null,
            "BoonActivated" => "BOON", "SupplyCollected" => "HEAL", "CurrencyCollected" => "+" + e.Amount + " GOLD", _ => null
        };
        var source = _session.State.Actors.FirstOrDefault(a => a.Id == e.Source);
        WorldPosition? visibleSource = source != null && _session.Visible.Contains(source.Position) ? source.Position : null;
        var visual = e.Kind == "StarvationDamage" ? e with { Kind = "DamageTaken" } : e.Kind == "WellFedHealing" ? e with { Kind = "Healed" } : e;
        _dungeonFeedback.Observe(visual, label, visibleSource != null ? source!.Tactics?.Facing ?? Facing.South : Facing.South,
            visibleSource, _session.State.Party.Contains(e.Target));
    }

    private Vector2 DungeonShake() => _session.State.Dungeon == null ? Vector2.Zero : _dungeonFeedback.Shake(_preferences);

    private void DrawDungeonFeedback()
    {
        if (_preferences.Flash && _preferences.CombatEffects)
        {
            var hurt = _dungeonFeedback.Effects.LastOrDefault(e => e.Kind == "DamageTaken" && e.PlayerHit);
            if (hurt != null && _dungeonFeedback.Time - hurt.Born < .4)
            {
                float alpha = (float)(1 - (_dungeonFeedback.Time - hurt.Born) / .4) * .55f;
                var red = new Color(255, 95, 80) * alpha;
                FillClipped(new(_map.X, _map.Y, _map.Width, 4), red); FillClipped(new(_map.X, _map.Bottom - 4, _map.Width, 4), red);
                FillClipped(new(_map.X, _map.Y, 4, _map.Height), red); FillClipped(new(_map.Right - 4, _map.Y, 4, _map.Height), red);
            }
        }
        foreach (var group in _dungeonFeedback.Effects.Where(e => _session.Visible.Contains(e.Position)).GroupBy(e => e.Position))
        {
            var rect = TileRect(group.Key); int row = 0;
            if (!_map.Intersects(rect)) continue;
            foreach (var e in group)
            {
                double age = _dungeonFeedback.Time - e.Born; float alpha = (float)Math.Clamp((1.1 - age) * 3, 0, 1);
                bool damage = e.Kind == "DamageTaken";
                Color tint = damage ? new(255, 132, 108) : e.Kind is "Healed" or "Revived" ? new(133, 236, 162) : Gold;
                if (_preferences.CombatEffects) DrawDungeonImpact(e, rect, age, tint);
                if (!_preferences.DamageNumbers || e.Label.Length == 0 || row >= 3) continue;
                int width = e.Label.Length * (_uiFonts?.Advance ?? 8), y = rect.Top - 16 - row++ * 18 - (_preferences.ReducedMotion ? 0 : (int)(age * 14));
                int x = Math.Clamp(rect.Center.X - width / 2, _map.Left + 3, Math.Max(_map.Left + 3, _map.Right - width - 3));
                y = Math.Clamp(y, _map.Top + 3, _map.Bottom - 18);
                FillClipped(new(x - 3, y - 2, width + 6, 18), Panel * alpha);
                Text(e.Label, x, y, tint * alpha);
            }
        }
    }

    private void DrawDungeonLine(Vector2 start, Vector2 end, Color color, int stroke = 2)
    {
        int steps = Math.Clamp((int)Vector2.Distance(start, end) / 2, 1, 96);
        for (int i = 0; i <= steps; i++)
        {
            var p = Vector2.Lerp(start, end, i / (float)steps);
            FillClipped(new((int)p.X, (int)p.Y, stroke, stroke), color);
        }
    }

    private void DrawDungeonImpact(DungeonEffect effect, Rectangle tile, double age, Color color)
    {
        var center = tile.Center.ToVector2();
        if (effect.Kind == "AttackPerformed" && age < .3)
        {
            float progress = (float)(age / .3), alpha = 1 - progress;
            float angle = MathF.Atan2(effect.Direction.Y, effect.Direction.X);
            bool ranged = effect.Ability.Contains("bolt", StringComparison.Ordinal) || effect.Ability.Contains("shot", StringComparison.Ordinal)
                || effect.Ability.Contains("volley", StringComparison.Ordinal) || effect.Ability.Contains("vial", StringComparison.Ordinal);
            if (ranged)
            {
                float reach = tile.Width * (_preferences.ReducedMotion ? .55f : .4f + progress * .55f);
                var end = center + effect.Direction * reach;
                DrawDungeonLine(center + effect.Direction * 8, end, Gold * alpha, 3);
                var cross = new Vector2(-effect.Direction.Y, effect.Direction.X) * 5;
                DrawDungeonLine(end - cross, end + cross, Color.White * alpha);
            }
            else
            {
                float sweep = _preferences.ReducedMotion ? 0 : progress * 1.1f;
                float radius = tile.Width * .65f;
                for (int i = 0; i < 8; i++)
                {
                    float a = angle - 1.2f + sweep + i * .17f, b = a + .17f;
                    DrawDungeonLine(center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius,
                        center + new Vector2(MathF.Cos(b), MathF.Sin(b)) * radius, Gold * alpha, 3);
                }
            }
        }
        bool treasure = effect.Kind is "ChestClaimed" or "VaultOpened";
        if (effect.Kind is not ("DamageTaken" or "DamageBlocked") && !treasure) return;
        double duration = treasure ? .8 : .36;
        if (age >= duration) return;
        float t = (float)(age / duration), fade = 1 - t;
        int rays = treasure ? 12 : 7;
        for (int i = 0; i < rays; i++)
        {
            float angle = i * MathHelper.TwoPi / rays + ((effect.Position.X + effect.Position.Y * 3) % 7) * .31f;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            float travel = _preferences.ReducedMotion ? 9 : 5 + MathF.Sqrt(t) * (treasure ? 42 : 22);
            var p = center + direction * travel;
            if (treasure && !_preferences.ReducedMotion) p.Y += t * t * 16;
            int size = treasure ? 3 : 2;
            FillClipped(new((int)p.X, (int)p.Y, size, size), (treasure ? Gold : color) * fade);
            if (!_preferences.ReducedMotion) DrawDungeonLine(p, p - direction * (treasure ? 5 : 7) * fade, (treasure ? Gold : color) * fade);
        }
        if (effect.Kind == "DamageBlocked")
            _sprites.Fit(_batch, "shield", new(tile.Center.X - 10, tile.Center.Y - 10, 20, 20), Gold * fade);
    }

    private Rectangle DungeonActorPose(Actor actor, Rectangle bounds)
    {
        if (_session.State.Dungeon == null || !_preferences.CombatEffects || _preferences.ReducedMotion) return bounds;
        var hit = _dungeonFeedback.Effects.LastOrDefault(e => e.Kind == "DamageTaken" && e.Target == actor.Id && e.Position == actor.Position && _dungeonFeedback.Time - e.Born < .24);
        if (hit != null)
        {
            float t = (float)((_dungeonFeedback.Time - hit.Born) / .24), kick = MathF.Sin(t * MathF.PI);
            var away = hit.From is { } from ? new Vector2(hit.Position.X - from.X, hit.Position.Y - from.Y) : Vector2.Zero;
            if (away.LengthSquared() > 0) away.Normalize();
            bounds.Offset((int)(away.X * kick * 6), (int)(away.Y * kick * 6));
            int squash = (int)(kick * 3);
            return new(bounds.X - squash, bounds.Y + squash, bounds.Width + squash * 2, bounds.Height - squash);
        }
        var attack = _dungeonFeedback.Effects.LastOrDefault(e => e.Kind == "AttackPerformed" && e.Source == actor.Id && e.Position == actor.Position && _dungeonFeedback.Time - e.Born < .22);
        if (attack != null)
        {
            float lean = MathF.Sin((float)((_dungeonFeedback.Time - attack.Born) / .22) * MathF.PI) * 5;
            bounds.Offset((int)(attack.Direction.X * lean), (int)(attack.Direction.Y * lean));
        }
        return bounds;
    }

    private Color DungeonActorTint(long actor, Color normal)
    {
        if (_session.State.Dungeon == null || !_preferences.Flash) return normal;
        var hit = _dungeonFeedback.Effects.LastOrDefault(e => e.Kind == "DamageTaken" && e.Target == actor && _dungeonFeedback.Time - e.Born < .12);
        return hit == null ? normal : Color.Lerp(normal, Color.White, (float)(1 - (_dungeonFeedback.Time - hit.Born) / .12));
    }

    private void DrawDungeonReward()
    {
        if (_session.State.Dungeon == null || _panel.Length > 0 || _dungeonFeedback.Reward is not { } reward) return;
        double age = _dungeonFeedback.Time - reward.Born;
        float alpha = (float)Math.Clamp(Math.Min(age / .08, (2 - age) / .4), 0, 1);
        int width = Math.Min(480, _map.Width - 24), slide = _preferences.ReducedMotion || !_preferences.CombatEffects ? 0 : (int)(10 * Math.Exp(-age * 16));
        var box = new Rectangle(_map.Center.X - width / 2, _map.Y + 16 - slide, width, 74);
        Fill(box, Panel * alpha); Fill(new(box.X, box.Y, box.Width, 2), Gold * alpha);
        Fill(new(box.X, box.Bottom - 2, box.Width, 2), Gold * alpha);
        _sprites.Fit(_batch, NativeAppearance.ForBoon(reward.Boon), new(box.X + 12, box.Y + 13, 48, 48), Gold * alpha);
        Text("BOON CLAIMED", box.X + 76, box.Y + 13, Gold * alpha);
        Text(BoonCatalog.Default.Get(reward.Boon).Name, box.X + 76, box.Y + 37, Ink * alpha);
    }

    private void DrawDungeonCelebration(int width, int height)
    {
        if (_session.State.Dungeon == null || !_preferences.CombatEffects || _dungeonFeedback.Celebration is not { } burst) return;
        double age = _dungeonFeedback.Time - burst.Born;
        var area = _panel == "dungeon-chest" ? PanelBox(width, height) : _map;
        int count = burst.Claimed ? 40 : 24;
        for (int side = 0; side < 2; side++) for (int i = 0; i < count; i++)
        {
            float t = (float)(age - i * .012);
            if (t is < 0 or > 1.65f) continue;
            int seed = (i * 73 + side * 29) % 101, size = 14 + i % 3 * 4;
            float direction = side == 0 ? 1 : -1;
            float speed = Math.Min(area.Width * .52f, 220 + seed * 2);
            float x = (side == 0 ? area.Left + 8 : area.Right - 8);
            float y = area.Bottom - 28 - seed % 4 * 14;
            if (_preferences.ReducedMotion)
            {
                if (i >= 4) continue;
                x += direction * (18 + i % 2 * 24); y = area.Center.Y - 35 + i * 24;
            }
            else
            {
                x += direction * speed * t;
                y += -(280 + seed * 1.5f) * t + 280 * t * t;
            }
            float alpha = Math.Clamp(t / .045f, 0, 1) * Math.Clamp((1.65f - t) / .45f, 0, 1);
            float spin = _preferences.ReducedMotion ? 1 : .2f + .8f * MathF.Abs(MathF.Cos(t * 13 + i));
            int coinWidth = Math.Max(3, (int)(size * spin));
            var coin = new Rectangle((int)x - coinWidth / 2, (int)y - size / 2, coinWidth, size);
            Fill(coin with { X = coin.X + 2, Y = coin.Y + 3 }, new Color(54, 32, 9) * alpha);
            var tint = _preferences.Flash && !_preferences.ReducedMotion && MathF.Sin(t * 11 + i) > .85f
                ? new Color(255, 245, 184) : new Color(255, 199, 62);
            _sprites.Draw(_batch, "coin", coin, tint * alpha);
        }
    }
}
