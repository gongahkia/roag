using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ithica.Core.Colony;
using Ithica.Graphics;

namespace Ithica.Client;

public sealed partial class IthicaGame
{
    private int _overlay;
    private static readonly string[] TerrainSprites = ["vacuum", "rock", "ice", "ore", "crystal", "floor", "wall", "airlock"];
    private static readonly Color[] TerrainColors = [new(62, 73, 78), new(107, 119, 121), new(135, 207, 225), new(219, 165, 95), new(183, 151, 211), new(72, 101, 105), new(170, 189, 185), new(226, 184, 107)];
    private void Fill(Rectangle rect, Color color)
    {
        if (rect.Width > 0 && rect.Height > 0)
        {
            _batch.Draw(_pixel, rect, color);
        }
    }
    private void Outline(Rectangle r, Color color, int width = 1)
    {
        Fill(new(r.X, r.Y, r.Width, width), color);
        Fill(new(r.X, r.Bottom - width, r.Width, width), color);
        Fill(new(r.X, r.Y, width, r.Height), color);
        Fill(new(r.Right - width, r.Y, width, r.Height), color);
    }
    private void Line(Vector2 a, Vector2 b, Color color, float width = 1)
    {
        _batch.Draw(_pixel, a, null, color, MathF.Atan2(b.Y - a.Y, b.X - a.X), Vector2.Zero, new Vector2(Vector2.Distance(a, b), width), SpriteEffects.None, 0);
    }
    private void Ring(Vector2 center, float radius, Color color, int segments = 16)
    {
        for (int i = 0; i < segments; i++)
        {
            float a = i * MathF.Tau / segments, b = (i + 1) * MathF.Tau / segments;
            Line(center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius, center + new Vector2(MathF.Cos(b), MathF.Sin(b)) * radius, color);
        }
    }
    private void Text(string value, float x, float y, Color color, float scale = 1) => _sprites.Text(_batch, value, new(x, y), color, scale);
    private float Wrap(string value, float x, float y, float width, Color color, float scale = 1) => _sprites.Wrapped(_batch, value, new(x, y), width, color, scale);
    private Rectangle CellRect(int cell)
    {
        var p = _camera.WorldToScreen(new(_sim.World.X(cell), _sim.World.Y(cell)));
        var q = p + new Vector2(_camera.CellSize);
        return new((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y), (int)MathF.Ceiling(q.X) - (int)MathF.Floor(p.X), (int)MathF.Ceiling(q.Y) - (int)MathF.Floor(p.Y));
    }
    private static Color RoleColor(string role) => role switch { "Engineer" => Amber, "Miner" => Blue, "Botanist" => Accent, "Medic" => Danger, _ => Ink };
    private static Color LevelColor(NoticeLevel level) => level switch { NoticeLevel.Good => Accent, NoticeLevel.Warning => Amber, NoticeLevel.Danger => Danger, _ => Muted };
    private void DrawMap()
    {
        var world = _sim.World;
        var view = _camera.Viewport;
        Fill(view, new Color(15, 21, 27));
        var a = _camera.ScreenToWorld(new(view.Left, view.Top));
        var b = _camera.ScreenToWorld(new(view.Right, view.Bottom));
        int minX = Math.Clamp((int)MathF.Floor(a.X), 0, world.Width), maxX = Math.Clamp((int)MathF.Ceiling(b.X), 0, world.Width);
        int minY = Math.Clamp((int)MathF.Floor(a.Y), 0, world.Height), maxY = Math.Clamp((int)MathF.Ceiling(b.Y), 0, world.Height);
        if (_overlay == 3)
        {
            _sim.UpdatePower();
        }

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                int cell = world.Index(x, y);
                Rectangle r = CellRect(cell);
                Terrain tile = world.Tiles[cell];
                int hash = unchecked((x * 73856093) ^ (y * 19349663) ^ _sim.Seed) & int.MaxValue;
                if (world.Discovered[cell] == 0)
                {
                    if (hash % 29 == 0)
                    {
                        Fill(new(r.Center.X, r.Center.Y, 1, 1), new Color(47, 57, 63));
                    }

                    continue;
                }
                if (tile == Terrain.Vacuum)
                {
                    Fill(r, new Color(28 + hash % 4, 35 + hash % 4, 41 + hash % 4));
                    if (hash % 3 == 0)
                    {
                        _sprites.Draw(_batch, "vacuum", r, TerrainColors[0] * .35f);
                    }
                }
                else if (World.Solid(tile) && tile != Terrain.Wall)
                {
                    Fill(r, new Color(36, 45, 51));
                    _sprites.Draw(_batch, "rock", r, TerrainColors[1] * (.65f + hash % 3 * .08f));
                    if (tile is Terrain.Ice or Terrain.Ore or Terrain.Crystal)
                    {
                        var deposit = r;
                        deposit.Inflate(-(int)(_camera.CellSize * .12f), -(int)(_camera.CellSize * .12f));
                        _sprites.Draw(_batch, TerrainSprites[(int)tile], deposit, TerrainColors[(int)tile]);
                    }
                }
                else if (tile == Terrain.Floor)
                {
                    Fill(r, new Color(31, 48, 53));
                    _sprites.Draw(_batch, "floor", r, TerrainColors[5] * .64f);
                    if (_camera.CellSize >= 18)
                    {
                        Outline(r, Color.White * .025f);
                    }
                }
                else
                {
                    Fill(r, new Color(31, 43, 48));
                    _sprites.Draw(_batch, TerrainSprites[(int)tile], r, TerrainColors[(int)tile]);
                    if (tile == Terrain.Airlock)
                    {
                        Outline(r, Amber * .3f);
                    }
                }
                if (_overlay > 0 && tile == Terrain.Floor)
                {
                    Color color = _overlay switch
                    {
                        1 => Color.Lerp(Danger, Accent, world.Oxygen[cell]),
                        2 => Color.Lerp(Blue, Danger, Math.Clamp((world.Heat[cell] + 30) / 100, 0, 1)),
                        _ => _sim.CachedNetworkAt(cell) >= 0 ? (_sim.CachedNetworkAt(cell) == _sim.CachedNetworkAt(world.Center) ? Accent : Purple) : Danger
                    };
                    Fill(r, color * .38f);
                }
            }
        }

        foreach (var d in _sim.Extraction?.Deposits ?? [])
        {
            var r = CellRect(d.Cell);
            if (!r.Intersects(view) || _sim.MachineAt(d.Cell) is not null) continue;
            Outline(r, d.Surveyed ? Amber : Purple);
            var inset = r; inset.Inflate(-4, -4);
            _sprites.Fit(_batch, "icon-survey", inset, d.Surveyed ? Amber : Purple);
        }
        foreach (var m in _sim.Machines)
        {
            var r = CellRect(m.Cell);
            if (!r.Intersects(view) || world.Discovered[m.Cell] == 0)
            {
                continue;
            }

            var icon = r;
            icon.Inflate(-2, -2);
            Color color = m.Kind switch
            {
                ModuleKind.Farm => Accent,
                ModuleKind.Bunk => new Color(180, 162, 126),
                ModuleKind.Solar => Blue,
                ModuleKind.Medbay => Danger,
                ModuleKind.Beacon => Purple,
                _ => Amber
            };
            _sprites.Draw(_batch, Blueprint.Get(m.Kind).Sprite, icon, m.Powered ? color : Muted * .45f);
            if (_sim.StoryEnabled && m.Fault != MachineFault.None) Outline(r, Danger);
            if (_camera.CellSize >= 18)
            {
                Fill(new(r.Right - 5, r.Bottom - 5, 3, 3), m.Powered ? Accent : Danger);
            }
        }
        foreach (var job in _sim.Jobs)
        {
            var r = CellRect(job.Cell);
            if (!r.Intersects(view))
            {
                continue;
            }

            Color color = job.Kind == WorkKind.Build ? Accent : Amber;
            Fill(r, color * .15f);
            Outline(r, color * .75f);
            if (_camera.CellSize >= 18)
            {
                string icon = job.Kind switch
                {
                    WorkKind.Build => Blueprint.Get(job.Blueprint).Sprite,
                    WorkKind.Survey => "icon-survey",
                    WorkKind.Maintain => "icon-build",
                    _ => "icon-mine"
                };
                var inset = r;
                inset.Inflate(-4, -4);
                _sprites.Fit(_batch, icon, inset, color * .65f);
                Fill(new(r.X, r.Bottom - 2, (int)(r.Width * Math.Clamp(job.Progress / job.Required, 0, 1)), 2), color);
            }
        }
        if (_selectedCrew is int selected && selected < _sim.Crew.Count)
        {
            var c = _sim.Crew[selected];
            int previous = c.Cell;
            for (int n = c.PathCursor; n < Math.Min(c.Path.Count, c.PathCursor + 180); n++)
            {
                int next = c.Path[n];
                var p = _camera.WorldToScreen(CellCenter(previous));
                var q = _camera.WorldToScreen(CellCenter(next));
                if (view.Contains(p) || view.Contains(q))
                {
                    Line(p, q, Accent * .3f, 2);
                }

                previous = next;
            }
        }
        foreach (var c in _sim.Crew)
        {
            if (c.Alive && c.Id != _selectedCrew)
            {
                DrawColonist(c);
            }
        }

        if (_selectedCrew is int id && id < _sim.Crew.Count && _sim.Crew[id].Alive)
        {
            DrawColonist(_sim.Crew[id]);
        }

        foreach (var s in _sim.Survivors)
        {
            if (!s.Active)
            {
                continue;
            }

            Vector2 p = _camera.WorldToScreen(CellCenter(s.Cell));
            if (!view.Contains(p))
            {
                continue;
            }

            if (s.State == ContactState.Stranded)
            {
                int size = Math.Max(18, (int)_camera.CellSize);
                var box = new Rectangle((int)p.X - size / 2, (int)p.Y - size / 2, size, size);
                Fill(box, Background * .85f);
                _sprites.Fit(_batch, "survivor", box, Danger);
                Outline(box, Danger * .7f);
                Ring(p, Math.Max(18, _camera.CellSize * .7f), Danger * (.4f + .18f * (float)Math.Sin(_elapsed * 2)));
            }
            else
            {
                Ring(p, Math.Max(14, _camera.CellSize * .6f), Amber * .75f);
            }

            string label = s.Name.ToUpperInvariant() + " / " + (s.State == ContactState.Escorted ? "ESCORT" : $"O2 {s.Suit:0}%");
            int width = (int)SpriteLibrary.Measure(label) + 12;
            var tag = new Rectangle((int)p.X - width / 2, (int)p.Y - (int)Math.Max(22, _camera.CellSize) - 14, width, 20);
            Fill(tag, Background * .95f);
            Text(label, tag.X + 6, tag.Y + 2, s.State == ContactState.Stranded ? Danger : Amber);
        }
        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                int i = world.Index(x, y);
                if (world.Fire[i] <= 0)
                {
                    continue;
                }

                var r = CellRect(i);
                if (!r.Intersects(view))
                {
                    continue;
                }

                Fill(r, Danger * .3f);
                _sprites.Draw(_batch, "fire", r, Amber);
            }
        }

        if (_anchor is int anchor)
        {
            var first = CellRect(anchor);
            var last = CellRect(_cursor);
            var area = new Rectangle(Math.Min(first.Left, last.Left), Math.Min(first.Top, last.Top), Math.Abs(first.X - last.X) + first.Width, Math.Abs(first.Y - last.Y) + first.Height);
            Fill(area, Amber * .08f);
            Outline(area, Amber, 2);
        }
        var cursor = CellRect(_cursor);
        if (cursor.Intersects(view))
        {
            Outline(cursor, _tool == Tool.Inspect ? Accent : Amber, 2);
            if (_tool == Tool.Build && world.Discovered[_cursor] > 0)
            {
                _sprites.Draw(_batch, Blueprint.Get(_blueprint).Sprite, cursor, (_sim.Resources.Covers(Blueprint.Get(_blueprint).Cost) ? Accent : Danger) * .5f);
            }
        }
        if (_modal.Length == 0 && view.Contains(_mouse.Position))
        {
            var pos = _camera.ScreenToWorld(MousePoint);
            int x = (int)Math.Floor(pos.X), y = (int)Math.Floor(pos.Y);
            if (world.Inside(x, y))
            {
                Outline(CellRect(world.Index(x, y)), Color.White * .3f);
            }
        }
        Fill(new(view.X + 12, view.Y + 12, 260, 24), Background * .9f);
        Text(_overlay switch
        {
            1 => "OXYGEN / CELL PRESSURE",
            2 => "TEMPERATURE / LOCAL HEAT",
            3 => "POWER / CONNECTED DECK",
            _ => "SHELTER / SURFACE SECTOR"
        }, view.X + 20, view.Y + 17, _overlay > 0 ? Accent : Muted);
        if (_tool != Tool.Inspect)
        {
            string label = _tool == Tool.Build ? "PLACE / " + Blueprint.Get(_blueprint).Name.ToUpperInvariant() : _tool == Tool.Dig ? "DESIGNATE / MINE + SALVAGE" : _tool == Tool.Survey ? "DESIGNATE / SURVEY" : "DESIGNATE / CANCEL WORK";
            int width = (int)SpriteLibrary.Measure(label) + 20;
            Fill(new(view.X + 12, view.Bottom - 40, width, 27), Background * .95f);
            Text(label, view.X + 22, view.Bottom - 34, Amber);
        }
        if (_sim.GameOver)
        {
            var banner = new Rectangle(view.Center.X - 230, view.Center.Y - 50, 460, 100);
            Fill(banner, Background * .98f);
            Outline(banner, Danger * .6f);
            Text("THE OUTPOST HAS FALLEN SILENT", banner.X + 20, banner.Y + 20, Danger);
            Text("N: new colony   Shift+L: load", banner.X + 20, banner.Y + 56, Ink);
        }
    }
    private void DrawColonist(Colonist c)
    {
        if (c.Location != ResidentLocation.Home) return;
        Vector2 pos = _camera.WorldToScreen(_crewPositions.Length > c.Id && _crewPositions[c.Id] != Vector2.Zero ? _crewPositions[c.Id] : CellCenter(c.Cell));
        int size = Math.Max(10, (int)_camera.CellSize);
        var box = new Rectangle((int)pos.X - size / 2, (int)pos.Y - size / 2, size, size);
        if (!box.Intersects(_camera.Viewport))
        {
            return;
        }

        string sprite = c.Role == "Engineer" ? "engineer" : c.Role == "Miner" ? "miner" : "crew";
        Color color = RoleColor(c.Role);
        _sprites.Fit(_batch, sprite, box, color);
        if (!_sim.World.Breathable(c.Cell) && size >= 18)
        {
            Ring(new(pos.X, pos.Y - size * .25f), size * .2f, Ink * .7f, 12);
        }

        if (c.Id == _selectedCrew)
        {
            var selected = box;
            selected.Inflate(3, 3);
            Outline(selected, Accent);
            Text(c.Name.ToUpperInvariant(), box.X, box.Y - 17, Ink);
        }
        if (c.Health < 60 || c.Suit < 30)
        {
            Fill(new(box.X, box.Bottom + 2, (int)(size * Math.Clamp(Math.Min(c.Health, c.Suit) / 100, 0, 1)), 2), Danger);
        }
    }
}
