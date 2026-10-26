using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roag.Core.Content;
using Roag.Core.Expeditions;
using Roag.Core.World;

namespace Roag.Client;

/// <summary>Stage coordinates are only presentation. They never affect legal targets or damage.</summary>
internal static class BattleStageLayout
{
    public static Rectangle Unit(Rectangle area, WorldBattle battle, CombatUnit unit)
    {
        int index = battle.Units.Take(unit.Id).Count(u => u.Friendly == unit.Friendly);
        int count = battle.Units.Count(u => u.Friendly == unit.Friendly);
        int wing = (area.Width - 120) / 2, width = (wing - 24) / 2;
        int rowHeight = (area.Height - 112) / 2;
        int side = unit.Friendly ? area.Left + 12 : area.Right - wing - 12;
        int column = index % 2;
        // Front fighters stand closer to the centre; stable hit areas do not follow their animations.
        if (unit.Friendly) column = 1 - column;
        int x = count == 1 ? side + (wing - width) / 2 : side + column * (width + 12);
        int y = area.Top + 106 + (count <= 2 ? rowHeight / 2 : index / 2 * rowHeight);
        return new(x, y, width, rowHeight - 6);
    }
}

internal sealed partial class WorldGame
{
    private WorldBattle? _intentBattle;
    private long _intentRevision = -1;
    private readonly Dictionary<int, CombatIntentPreview?> _intentCache = [];
    private CombatIntentPreview? BattleIntent(int id)
    {
        if (!ReferenceEquals(_intentBattle, _session.State.Battle) || _intentRevision != _session.State.NextCombatEvent)
        { _intentBattle = _session.State.Battle; _intentRevision = _session.State.NextCombatEvent; _intentCache.Clear(); }
        if (!_intentCache.TryGetValue(id, out var intent)) _intentCache[id] = intent = _session.EnemyActionPreview(id);
        return intent;
    }
    private Rectangle CombatArea => new(_map.X, _map.Y, _side.Right - _map.X, _map.Height);
    private Rectangle UnitRect(CombatUnit unit) => BattleStageLayout.Unit(CombatArea, DisplayBattle!, unit);
    private Rectangle CombatSpriteBox(CombatUnit unit)
    {
        var r = UnitRect(unit); int size = Math.Clamp((r.Height - 82) / 12 * 12, 60, 120);
        bool boss = DisplayBattle is { } b && _session.State.Actors.FirstOrDefault(a => a.Id == b.ActorIds[unit.Id]) is { } actor
            && _session.Content.Entries.GetValueOrDefault(actor.Archetype) is BossDefinition;
        if (boss) size += 12;
        return new(r.Center.X - size / 2, r.Top + Math.Max(0, r.Height - 86 - size), size, size);
    }
    private void DrawCombat()
    {
        if (DisplayBattle is not { } b) return;
        var area = CombatArea; DrawBattleBackdrop(area);
        Text($"ENCOUNTER / ROUND {b.Round}", area.X + 18, area.Y + 14, Gold);
        DrawInitiative(b, area);
        string label = Playback.Current is { } beat ? beat.Label
            : b.Active.Friendly ? b.Active.Name + " - choose an ability" : b.Active.Name + " is preparing an action";
        Text(Short(label, (area.Width - 40) / 8), area.X + 20, area.Y + 82, Playback.Busy ? Gold : Ink);
        var skills = Playback.Busy ? Array.Empty<Skill>() : _session.ActiveSkills();
        foreach (var unit in b.Units) DrawBattleFighter(b, unit, skills);
        DrawBattleAction(b); DrawCombatFeedback();
    }
    private void DrawBattleBackdrop(Rectangle area)
    {
        var position = _session.State.Position; bool underground = position.Floor != 0;
        var biome = _session.Plan.BiomeAt(position.X, position.Y);
        Color ground = underground ? new(31, 29, 38) : biome switch
        {
            Biome.Forest => new(24, 42, 37), Biome.Marsh => new(29, 42, 43), Biome.Badland => new(51, 37, 31),
            Biome.Snow => new(37, 48, 60), Biome.Highland => new(39, 42, 46), _ => new(32, 43, 37)
        };
        Fill(area, ground);
        for (int row = 0; row < 10; row++)
            Fill(new(area.X, area.Y + row * area.Height / 10, area.Width, area.Height / 10 + 1), Color.Lerp(new(14, 22, 29), ground, row / 9f));
        string scenery = underground ? "broken-column" : biome switch { Biome.Forest => "tree", Biome.Marsh => "willow", Biome.Badland => "dead-tree", Biome.Snow => "pine", _ => "boulder" };
        // Fixed seeded ornament placement: visual variety consumes no world/combat randomness.
        for (int i = 0; i < 12; i++)
        {
            uint hash = WorldRandom.Hash(_session.Seed, position.X / 64 + i, position.Y / 64);
            int size = 36 + (int)(hash % 3) * 12, x = area.X + 8 + i * (area.Width - 72) / 11;
            int y = area.Y + 100 + (int)(hash % 28);
            _sprites.Fit(_batch, scenery, new(x, y, size, size), Ink * .085f);
        }
        for (int i = 0; i < 18; i++)
        {
            int x = area.Left + 18 + i * (area.Width - 36) / 18;
            Fill(new(x, area.Bottom - 12 - i % 3 * 3, 14, 2), Ink * .06f);
        }
        Fill(new(area.X, area.Y, area.Width, 102), new Color(15, 23, 29) * .83f);
        Fill(new(area.X, area.Bottom - 2, area.Width, 2), Gold * .25f);
    }
    private void DrawInitiative(WorldBattle b, Rectangle area)
    {
        int step = Math.Min(86, (area.Width - 220) / Math.Max(1, b.Order.Count));
        for (int i = 0; i < b.Order.Count; i++)
        {
            var u = b.Units[b.Order[i]]; int x = area.X + 20 + i * step, y = area.Y + 41;
            bool spent = i < b.TurnIndex || b.Assisting.Contains(u.Id) || !u.Alive;
            Fill(new(x, y, step - 5, 34), u.Id == b.Active.Id ? new(70, 61, 39) : new(30, 39, 44));
            _sprites.Draw(_batch, u.Sprite, new(x + 4, y + 3, 28, 28), (u.Friendly ? Ink : new(224, 148, 130)) * (spent ? .25f : 1), effects: u.Friendly ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            Text((i + 1).ToString(), x + 37, y + 9, spent ? Muted * .5f : Gold);
            if (spent) Fill(new(x + 3, y + 17, step - 11, 2), Muted * .6f);
        }

    }
    private void DrawBattleFighter(WorldBattle b, CombatUnit u, IReadOnlyList<Skill> skills)
    {
        var r = UnitRect(u); var sprite = CombatSpriteBox(u); long actor = b.ActorIds[u.Id];
        bool active = b.Active.Id == u.Id && u.Alive;
        bool choosing = !Playback.Busy && _session.State.Battle?.Active.Friendly == true;
        bool helper = choosing && skills.ElementAtOrDefault(_skill)?.Cooperative == true && (_cooperativePartner < 0 || u.Friendly);
        bool hover = r.Contains(_mouse.Position) && _panel.Length == 0;
        bool valid = choosing && hover && (helper ? _session.CooperationPartnerReason(_skill, u.Id, skills).Length == 0 : _session.ActionPreview(_skill, u.Id, _cooperativePartner, skills).Reason.Length == 0);
        var actorRecord = _session.State.Actors.FirstOrDefault(a => a.Id == actor);
        var boss = actorRecord == null ? null : _session.Content.Entries.GetValueOrDefault(actorRecord.Archetype) as BossDefinition;
        uint signature = 0; if (boss != null) foreach (char c in boss.Family) signature = signature * 31 + c;
        Color[] bossColors = [new(216, 158, 131), new(165, 189, 219), new(194, 159, 210), new(194, 198, 145)];
        Color color = u.Friendly ? Ink : boss != null ? bossColors[signature % 4] : new(218, 157, 133);
        if (hover && !Playback.Busy) Fill(r, color * (valid ? .10f : .035f));
        Fill(new(sprite.Center.X - sprite.Width / 2, sprite.Bottom - 2, sprite.Width, 7), new Color(7, 13, 17) * .7f);
        if (active || _cooperativePartner == u.Id || hover && valid)
            Fill(new(sprite.Center.X - sprite.Width / 2 - 5, sprite.Bottom + 5, sprite.Width + 10, 3), _cooperativePartner == u.Id || hover && valid ? new(128, 211, 214) : Gold);
        if (boss != null && u.Alive)
        {
            var center = sprite.Center.ToVector2();
            _sprites.Fit(_batch, new[] { "mark", "arc", "skull", "ember" }[signature % 4], new(sprite.X - 12, sprite.Y - 12, sprite.Width + 24, sprite.Height + 24), color * .1f);
            BattleRing(center + new Vector2(0, sprite.Height / 2f), sprite.Width / 2f + 12, color * .3f);
        }
        float opacity = u.Alive ? 1 : .26f;
        if (!_reducedMotion && Playback.Current is { } focus && Playback.Phase is "travel" or "impact"
            && actor != focus.Source && actor != focus.Helper && !focus.Targets.Contains(actor)) opacity *= .45f;
        var moving = sprite;
        if (Playback.Current is { } beat && !_reducedMotion)
        {
            float phase = (float)Playback.PhaseProgress; bool source = actor == beat.Source || actor == beat.Helper || beat.Profile.Family == "retreat" && u.Friendly;
            int direction = u.Friendly ? 1 : -1;
            if (source)
            {
                float reach = beat.Profile.Family is "melee" or "cooperative" ? 28 : 10;
                float offset = Playback.Phase switch { "anticipation" => -6 * MathF.Sin(phase * MathF.PI), "travel" => reach * MathF.Sin(phase * MathF.PI / 2), "impact" => reach, _ => reach * (1 - phase) };
                moving.Offset((int)MathF.Round(direction * offset), 0);
                if (beat.Profile.Family == "retreat") moving.Offset((int)(-direction * 50 * Playback.Progress), 0);
            }
            if (beat.Targets.Contains(actor) && Playback.Phase == "impact" && beat.Events.Any(e => e.Target == actor && e.Kind == "damage"))
                moving.Offset((int)(-direction * 8 * MathF.Sin(phase * MathF.PI)), 0);
        }
        if (!u.Alive) moving.Offset(0, 8);
        _sprites.Draw(_batch, u.Sprite, moving, color * opacity, effects: u.Friendly ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
        if (!u.Alive) _sprites.Fit(_batch, "skull", new(sprite.Center.X - 12, sprite.Center.Y - 12, 24, 24), Muted);
        int textY = r.Bottom - 76;
        Text(Short(u.Name, Math.Max(8, r.Width / 8)), r.X, textY, u.Alive ? color : Muted);
        int hpWidth = r.Width - 4;
        Fill(new(r.X, textY + 19, hpWidth, 5), new(12, 20, 24));
        Fill(new(r.X, textY + 19, hpWidth * u.Health / Math.Max(1, u.MaxHealth), 5), u.Friendly ? new(119, 183, 143) : new(208, 119, 106));
        Text($"{u.Health}/{u.MaxHealth}", r.X, textY + 28, u.Alive ? Ink : Muted);
        Text($"{u.Mana} MP", r.Right - 68, textY + 28, new(137, 184, 208));
        int statusX = r.X;
        foreach (var condition in BattleConditions(u))
        {
            _sprites.Fit(_batch, condition.Sprite, new(statusX, textY + 47, 16, 16), condition.Tint);
            Text(condition.Count, statusX + 18, textY + 48, condition.Tint); statusX += condition.Count.Length * 8 + 22;
        }
        if (u.Friendly && statusX == r.X)
            Text(b.Assisting.Contains(u.Id) ? "Action committed" : u.Stress > 0 ? $"Stress {u.Stress}" : active ? Playback.Busy ? "Acting" : "Ready" : "", r.X, textY + 48, Muted);
        if (boss != null && u.Alive && statusX == r.X)
            Text(u.Health * 100 <= u.MaxHealth * boss.PhasePercent ? $"Below {boss.PhasePercent}% / Tactics" : "Guardian", r.X, textY + 48, color);
        if (!u.Friendly && !Playback.Busy && BattleIntent(u.Id) is { } intent)
        {
            int y = r.Top - 8;
            bool charging = u.Alive && b.Prepared.TryGetValue(u.Id, out var prepared) && prepared.ReadyRound > b.Round;
            bool muted = intent.Reason.Length > 0 && !charging;
            _sprites.Fit(_batch, intent.Icon, new(r.X, y, 20, 20), muted ? Muted : Gold);
            string range = intent.Targets.Length == 0 ? "" : intent.Targets.Min(t => t.Minimum) + "-" + intent.Targets.Max(t => t.Maximum);
            string caption = charging ? "Charging / stun" : intent.Reason.Length > 0 ? u.Alive ? "Spent / blocked" : "Defeated" : Short(intent.Name, 11) + (intent.Targets.Any(t => t.Maximum > 0) ? " " + range : "");
            Text(Short(caption, (r.Width - 26) / 8), r.X + 26, y + 4, muted ? Muted : Gold);
            if (intent.Reason.Length == 0 || charging)
                for (int i = 0; i < intent.Targets.Length; i++)
                    _sprites.Fit(_batch, intent.Targets[i].Sprite, new(r.X + 3 + i * 22, y + 23, 18, 18), Ink * .75f);
        }
    }
    private static IEnumerable<(string Sprite, string Count, Color Tint)> BattleConditions(CombatUnit u)
    {
        if (u.Guarding) yield return ("shield", "", new(146, 202, 215));
        if (u.Burn > 0) yield return ("ember", u.Burn.ToString(), new(237, 156, 106));
        if (u.Bleed > 0) yield return ("slash", u.Bleed.ToString(), new(230, 126, 122));
        if (u.Mark > 0) yield return ("mark", u.Mark.ToString(), Gold);
        if (u.Stun > 0) yield return ("spark", u.Stun.ToString(), new(198, 167, 226));
    }
    private Vector2 BattlePoint(WorldBattle b, long actor) => b.ActorIds.IndexOf(actor) is var i && i >= 0 ? CombatSpriteBox(b.Units[i]).Center.ToVector2() : CombatArea.Center.ToVector2();
    private void BattleLine(Vector2 a, Vector2 b, Color tint, int thickness = 3)
    {
        var delta = b - a;
        _batch.Draw(_pixel, a, null, tint, MathF.Atan2(delta.Y, delta.X), Vector2.Zero, new Vector2(delta.Length(), thickness), SpriteEffects.None, 0);
    }
    private void BattleRing(Vector2 point, float radius, Color tint)
    {
        for (int i = 0; i < 12; i++)
        {
            float angle = i * MathF.Tau / 12;
            var a = point + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .5f) * radius;
            var c = point + new Vector2(MathF.Cos(angle + MathF.Tau / 12), MathF.Sin(angle + MathF.Tau / 12) * .5f) * radius;
            BattleLine(a, c, tint, 2);
        }
    }
    private void DrawBattleAction(WorldBattle b)
    {
        if (Playback.Current is not { } beat) return;
        var profile = beat.Profile; var source = BattlePoint(b, beat.Source); float progress = (float)Playback.PhaseProgress;
        string phase = Playback.Phase; Color tint = profile.Tint;
        if (beat.Helper != 0)
            BattleLine(source, BattlePoint(b, beat.Helper), new Color(130, 207, 211) * .65f, 2);
        if (phase == "anticipation")
        {
            if (!_reducedMotion) BattleRing(source + new Vector2(0, 30), 20 + 14 * progress, tint * .65f);
            _sprites.Fit(_batch, profile.Sprite, new((int)source.X - 14, (int)source.Y - 58, 28, 28), tint);
            return;
        }
        foreach (long target in beat.Targets)
        {
            var destination = BattlePoint(b, target);
            if (phase == "travel" && !_reducedMotion)
            {
                if (profile.Family is "projectile" or "spell" or "dread")
                {
                    var point = Vector2.Lerp(source, destination, progress);
                    if (profile.Family == "spell") point.Y -= MathF.Sin(progress * MathF.PI) * 40;
                    BattleLine(Vector2.Lerp(source, point, .7f), point, tint * .6f, profile.Family == "projectile" ? 3 : 5);
                    if (profile.Id == "combat.projectile")
                    {
                        var forward = Vector2.Normalize(destination - source); var across = new Vector2(-forward.Y, forward.X);
                        BattleLine(point - forward * 20, point, tint, 3);
                        BattleLine(point - forward * 8 + across * 6, point, tint, 2);
                        BattleLine(point - forward * 8 - across * 6, point, tint, 2);
                    }
                    else _sprites.Fit(_batch, profile.Sprite, new((int)point.X - 12, (int)point.Y - 12, 24, 24), tint);
                }
                else if (profile.Family is "melee" or "cooperative")
                {
                    var point = Vector2.Lerp(source, destination, progress);
                    BattleLine(point - new Vector2(12, -12), point + new Vector2(12, -12), tint * .8f, 3);
                    if (beat.Helper != 0) BattleLine(Vector2.Lerp(BattlePoint(b, beat.Helper), destination, progress), destination, tint * .35f, 2);
                }
                else BattleRing(destination + new Vector2(0, 25), 12 + 30 * progress, tint * .7f);
            }
            if (phase is "impact" or "recovery")
            {
                float alpha = phase == "impact" ? 1 : Math.Max(0, 1 - progress * 1.8f);
                int size = _reducedMotion ? 36 : phase == "impact" ? 42 + (int)(12 * progress) : 54;
                _sprites.Fit(_batch, profile.Id == "combat.projectile" ? "slash" : profile.Sprite, new((int)destination.X - size / 2, (int)destination.Y - size / 2, size, size), tint * alpha);
                if (!_reducedMotion)
                {
                    BattleRing(destination + new Vector2(0, 25), 24 + 28 * progress, tint * (.6f * alpha));
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = i * MathF.Tau / 6; float distance = 24 + progress * 32;
                        var p = destination + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
                        Fill(new((int)p.X, (int)p.Y, 4, 4), tint * alpha);
                    }
                }
            }
        }
        if (phase is "impact" or "recovery")
            foreach (var cover in beat.Events.Where(e => e.Label is "Ally cover" or "Shield cover"))
            {
                var protectedPoint = BattlePoint(b, cover.Target);
                BattleLine(BattlePoint(b, cover.Source), protectedPoint, new(137, 201, 217), 2);
                _sprites.Fit(_batch, "shield", new((int)protectedPoint.X - 25, (int)protectedPoint.Y - 25, 50, 50), new(137, 201, 217));
            }
    }
}
