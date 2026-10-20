using Lantern.Core;
using Microsoft.Xna.Framework;
using Regolith.Graphics;

namespace Lantern.Client;

public sealed partial class LanternGame
{
    private void Fill(Rectangle r, Color color)
    {
        if (r.Width > 0 && r.Height > 0)
        {
            _batch.Draw(_pixel, r, color);
        }
    }
    private void Outline(Rectangle r, Color color, int thickness = 1)
    {
        Fill(new(r.X, r.Y, r.Width, thickness), color);
        Fill(new(r.X, r.Bottom - thickness, r.Width, thickness), color);
        Fill(new(r.X, r.Y, thickness, r.Height), color);
        Fill(new(r.Right - thickness, r.Y, thickness, r.Height), color);
    }
    private void Text(string text, float x, float y, Color color, float scale = 1) => _sprites.Text(_batch, text, new(x, y), color, scale);
    private float Wrap(string text, float x, float y, float width, Color color, float scale = 1) => _sprites.Wrapped(_batch, text, new(x, y), width, color, scale);
    private static string Short(string value, int count) => value.Length <= count ? value : value[..Math.Max(0, count - 3)] + "...";
    private Rectangle CellRect(Cell p)
    {
        var top = _camera.WorldToScreen(new(p.X, p.Y));
        var bottom = _camera.WorldToScreen(new(p.X + 1, p.Y + 1));
        return new((int)MathF.Round(top.X), (int)MathF.Round(top.Y), (int)MathF.Round(bottom.X) - (int)MathF.Round(top.X), (int)MathF.Round(bottom.Y) - (int)MathF.Round(top.Y));
    }
    private void Tile(string sprite, Rectangle r, Color color, bool? original = null)
    {
        bool showOriginal = original ?? _original;
        _sprites.Draw(_batch, sprite, r, showOriginal ? Color.White * (color.A / 255f) : color, showOriginal);
    }
    private void DrawWorld()
    {
        Fill(_map, new(14, 19, 20));
        var zone = _campaign.Zone;
        var a = _camera.ScreenToWorld(new(_map.Left, _map.Top));
        var b = _camera.ScreenToWorld(new(_map.Right, _map.Bottom));
        int x0 = Math.Max(0, (int)Math.Floor(a.X)), y0 = Math.Max(0, (int)Math.Floor(a.Y)), x1 = Math.Min(zone.Width - 1, (int)Math.Ceiling(b.X)), y1 = Math.Min(zone.Height - 1, (int)Math.Ceiling(b.Y));
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                Cell p = new(x, y);
                int i = zone.Index(p);
                var r = CellRect(p);
                if (!zone.Seen[i])
                {
                    Fill(r, new(14, 17, 20));
                    continue;
                }
                Ground g = zone.At(p);
                bool bright = zone.Visible[i] || zone.Id == "hearth";
                float alpha = bright ? 1 : .37f;
                int hash = (int)(unchecked((uint)(x * 97 + y * 37 + _campaign.Seed)) % 31);
                Color bg;
                Color ink;
                string sprite;
                switch (g)
                {
                    case Ground.Grass:
                    case Ground.Tree:
                        bg = new(29, 42, 33);
                        ink = new(66, 85, 53);
                        sprite = hash % 2 == 0 ? "grass" : "grass-alt";
                        break;
                    case Ground.Path:
                        bg = new(56, 48, 35);
                        ink = new(112, 94, 63);
                        sprite = "path";
                        break;
                    case Ground.Water:
                        bg = new(19, 38, 47);
                        ink = new(64, 107, 118);
                        sprite = "water";
                        break;
                    case Ground.Wall:
                        bg = new(45, 43, 42);
                        ink = zone.Id == "hearth" ? new(134, 112, 77) : new(116, 114, 109);
                        sprite = "wall";
                        break;
                    case Ground.Wood:
                        bg = new(61, 46, 33);
                        ink = new(126, 94, 59);
                        sprite = "wood";
                        break;
                    case Ground.Bridge:
                        bg = new(32, 43, 43);
                        ink = new(139, 113, 74);
                        sprite = "bridge";
                        break;
                    default:
                        bg = new(41, 41, 44);
                        ink = new(87, 87, 85);
                        sprite = hash % 3 == 0 ? "stone-alt" : "stone";
                        break;
                }
                if (g != Ground.Tree) sprite = zone.GroundSprites[i] ?? sprite;
                Fill(r, bg * alpha);
                if (g != Ground.Grass || hash % 3 == 0 || zone.GroundSprites[i] == "grit")
                {
                    Tile(sprite, r, ink * alpha);
                }

                if (g == Ground.Wall)
                {
                    Fill(new(r.X, r.Bottom - 3, r.Width, 3), Color.Black * .28f);
                }
                if (g == Ground.Tree)
                {
                    Fill(new(r.X + 3, r.Y + 4, r.Width - 4, r.Height - 2), Color.Black * .25f);
                    Tile(zone.GroundSprites[i] ?? (hash % 3 == 0 ? "pine" : "tree"), r, new Color(86, 116, 75) * alpha);
                }
                else if (g == Ground.Grass && hash < 3 && _campaign.ObjectAt(p) is null)
                {
                    Tile(hash == 0 ? "flower" : hash == 1 ? "shrub" : "mushroom", r, (hash == 0 ? Gold : Green) * alpha * .75f);
                }
                if (bright && zone.Environment.Cells[i] is var hazard && hazard != HazardKind.None)
                {
                    var rules = GenerationRules.Default.Hazards;
                    Color color = hazard == HazardKind.Fire ? Red : hazard == HazardKind.Spores ? Green : Purple;
                    string icon = hazard == HazardKind.Fire ? rules.FireSprite : hazard == HazardKind.Spores ? rules.SporesSprite : rules.CorruptionSprite;
                    Fill(r, color * .19f);
                    Outline(new(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), color * .55f);
                    Tile(icon, r, color);
                    if (r.Contains(_mouse.Position))
                        _hover = hazard + " / " + (hazard == HazardKind.Corruption ? "+3 stress per step." : "1 health and +1 stress per step.") + " Spreads every 4 exploration turns. T clears nearby hazards; . waits. Roads and room crosses are safe.";
                }
            }
        }

        for (int n = _routeCursor; n < _route.Count; n++)
        {
            var s = _camera.WorldToScreen(Point(_route[n]));
            Fill(new((int)s.X - 2, (int)s.Y - 2, 4, 4), Gold * .45f);
        }
        foreach (var o in zone.Objects)
        {
            if (!zone.Seen[zone.Index(o.Position)])
            {
                continue;
            }

            bool done = _campaign.Completed.Contains(o.Id);
            if (o.Kind == ObjectKind.Encounter && (done || _campaign.Battle?.EncounterId == o.Id))
            {
                continue;
            }

            if (o.Kind == ObjectKind.Relic && done)
            {
                continue;
            }

            string sprite = o.Kind == ObjectKind.Chest && done ? "chest-open" : o.Kind == ObjectKind.Gate && done ? "gate-open" : o.Sprite;
            var r = CellRect(o.Position);
            float light = zone.Visible[zone.Index(o.Position)] || zone.Id == "hearth" ? 1 : .45f;
            Color color = o.Kind switch
            {
                ObjectKind.Encounter => Red,
                ObjectKind.Chest => Gold,
                ObjectKind.Relic => new(248, 200, 118),
                ObjectKind.Talk or ObjectKind.Inn or ObjectKind.Merchant => Ink,
                ObjectKind.Shrine => Blue,
                ObjectKind.Portal => Gold,
                ObjectKind.Camp => new(225, 142, 78),
                _ => new(147, 137, 118)
            };
            if (o.Kind is ObjectKind.Relic or ObjectKind.Camp or ObjectKind.Shrine)
            {
                var glow = r;
                glow.Inflate(6, 6);
                Fill(glow, color * .055f);
            }
            Tile(sprite, r, color * light);
            var screen = _camera.WorldToScreen(Point(o.Position));
            if (_map.Contains(_mouse.Position) && Vector2.DistanceSquared(screen, _mouse.Position.ToVector2()) < 22 * 22)
            {
                _hover = o.Name + " / " + o.Detail;
            }

            if (o.Kind == ObjectKind.Encounter && !done)
            {
                Outline(new(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), Red * .6f);
            }
            if (o.Kind is ObjectKind.Portal && _camera.CellSize >= 24 && o.Position.Distance(_campaign.Position) > 2)
            {
                string label = o.Destination == "crypt" ? "ABBEY" : o.Destination == "wilds" ? "ASHWOOD" : "HEARTH";
                Label(label, r.Center.X, r.Bottom + 4, Gold);
            }
        }
        if (_campaign.Battle is { } battle)
        {
            foreach (var unit in battle.Units)
            {
                var r = CellRect(unit.Position);
                Color color = unit.Friendly ? HeroColors[unit.HeroId] : unit.Sprite == "abbot" ? Purple : Red;
                var shadow = r;
                shadow.Inflate(-3, -3);
                Fill(new(shadow.X, shadow.Bottom - 6, shadow.Width, 7), Color.Black * .35f);
                if (!unit.Alive)
                {
                    Tile("bones", r, Muted * .4f);
                    continue;
                }
                if (unit.Id == battle.Active.Id)
                {
                    var ring = r;
                    ring.Inflate(4, 4);
                    Outline(ring, color, 2);
                    Fill(new(ring.X, ring.Bottom + 3, ring.Width, 2), color);
                }
                if (unit.Id == _target)
                {
                    var ring = r;
                    ring.Inflate(8, 8);
                    Outline(ring, Gold * .75f);
                }
                Tile(unit.Sprite, r, color);
                Fill(new(r.X - 4, r.Y - 7, r.Width + 8, 3), Color.Black * .7f);
                Fill(new(r.X - 4, r.Y - 7, (int)((r.Width + 8) * unit.Health / (float)unit.MaxHealth), 3), color);
                if (_camera.CellSize >= 20)
                {
                    Label(Short(unit.Name, 15), r.Center.X, r.Bottom + 8, color);
                }

                if (unit.Guarding)
                {
                    Tile("shield", new(r.Right - 8, r.Y - 8, 16, 16), Blue);
                }

                if (unit.Burn > 0)
                {
                    Tile("ember", new(r.X - 8, r.Y - 8, 14, 14), Gold);
                }

                if (unit.Mark > 0)
                {
                    Outline(r, Gold);
                }

                if (unit.Stun > 0)
                {
                    Label("STUN", r.Center.X, r.Y - 26, Blue);
                }
            }
        }
        else
        {
            for (int n = 3; n >= 0; n--)
            {
                var h = _campaign.Party[n];
                if (h.Health <= 0)
                {
                    continue;
                }

                var center = _camera.WorldToScreen(_positions[n]);
                int size = (int)_camera.CellSize;
                var r = new Rectangle((int)(center.X - size / 2), (int)(center.Y - size / 2), size, size);
                Fill(new(r.X + 3, r.Bottom - 5, r.Width - 6, 6), Color.Black * .3f);
                Tile(h.Sprite, r, HeroColors[n]);
                if (n == _selectedHero)
                {
                    var outline = r;
                    outline.Inflate(2, 2);
                    Outline(outline, HeroColors[n] * .7f);
                }
            }
        }
        foreach (var popup in _popups)
        {
            var p = _camera.WorldToScreen(Point(popup.Cell));
            p.Y -= 24 + (float)(.8 - (popup.Until - _time)) * 22;
            Label(popup.Text, (int)p.X, (int)p.Y, popup.Color);
        }
        // Lantern shading is legible through fog and palette as well as this edge treatment.
        if (zone.Id == "crypt")
        {
            for (int n = 0; n < 14; n++)
            {
                int inset = n * 3;
                Outline(new(_map.X + inset, _map.Y + inset, _map.Width - inset * 2, _map.Height - inset * 2), Color.Black * (.12f * (1 - _campaign.Light / 130f)));
            }
        }
    }
    private void Label(string text, int x, int y, Color color)
    {
        int w = (int)SpriteLibrary.Measure(text) + 10;
        Fill(new(x - w / 2, y - 2, w, 17), Background * .9f);
        Text(text, x - w / 2 + 5, y, color);
    }
}
