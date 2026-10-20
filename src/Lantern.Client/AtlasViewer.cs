using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Regolith.Graphics;

namespace Lantern.Client;

public sealed partial class LanternGame
{
    private sealed class AtlasEntry
    {
        public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Pack { get; set; } = ""; public string Source { get; set; } = "";
        public int[] Rect { get; set; } = []; public string Category { get; set; } = ""; public string Family { get; set; } = ""; public string Layer { get; set; } = "";
        public string Confidence { get; set; } = ""; public string Basis { get; set; } = ""; public string Role { get; set; } = ""; public string Adaptation { get; set; } = "";
        public List<string> Ideas { get; set; } = []; public Dictionary<string, List<string>> Usage { get; set; } = [];
        public string Search => Id + " " + Name + " " + Category + " " + Family + " " + Role + " " + Adaptation + " " + string.Join(" ", Ideas) + " " + string.Join(" ", Usage.Values.SelectMany(x => x));
    }
    private List<AtlasEntry> _entries = [], _filtered = [];
    private string _search = "";
    private int _packFilter, _familyFilter, _usageFilter, _atlasSelected, _atlasColumns = 6, _atlasPageSize = 30, _detailScroll;
    private bool _atlasMask;
    private static readonly string[] Packs = ["ALL PACKS", "urizen", "godot-node-icons", "input-prompts"];
    private string[] Families => new[] { "ALL FAMILIES" }.Concat(_entries.Select(e => e.Family).Distinct().Order()).ToArray();
    private static readonly string[] Usages = ["ANY USAGE", "RPG BOUND", "COLONY BOUND", "UNUSED IN BOTH"];
    private void FilterAtlas()
    {
        var families = Families;
        string family = families[Math.Min(_familyFilter, families.Length - 1)];
        _filtered = _entries.Where(e => (_packFilter == 0 || e.Pack == Packs[_packFilter]) && (_familyFilter == 0 || e.Family == family) && (_usageFilter switch { 1 => e.Usage["rpg"].Count > 0, 2 => e.Usage["colony"].Count > 0, 3 => e.Usage.Values.All(v => v.Count == 0), _ => true }) && (_search.Length == 0 || e.Search.Contains(_search, StringComparison.OrdinalIgnoreCase))).ToList();
        _atlasSelected = 0;
        _detailScroll = 0;
    }
    private void AtlasInput()
    {
        if (Pressed(Keys.Escape))
        {
            if (_search.Length > 0)
            {
                _search = "";
                FilterAtlas();
            }
            else
                _atlas = false;
        }
        if (Pressed(Keys.Back) && _search.Length > 0)
        {
            _search = _search[..^1];
            FilterAtlas();
        }
        if (Pressed(Keys.F3))
        {
            _packFilter = (_packFilter + 1) % Packs.Length;
            FilterAtlas();
        }
        if (Pressed(Keys.F6) || Pressed(Keys.Tab))
        {
            _familyFilter = (_familyFilter + 1) % Families.Length;
            FilterAtlas();
        }
        if (Pressed(Keys.F7))
        {
            _usageFilter = (_usageFilter + 1) % Usages.Length;
            FilterAtlas();
        }
        if (Pressed(Keys.F4))
            _atlasMask = !_atlasMask;
        int offset = Pressed(Keys.Right) ? 1 : Pressed(Keys.Left) ? -1 : Pressed(Keys.Down) ? _atlasColumns : Pressed(Keys.Up) ? -_atlasColumns : Pressed(Keys.PageDown) ? _atlasPageSize : Pressed(Keys.PageUp) ? -_atlasPageSize : 0;
        if (offset != 0)
        {
            _atlasSelected = Math.Clamp(_atlasSelected + offset, 0, Math.Max(0, _filtered.Count - 1));
            _detailScroll = 0;
        }
        int wheel = _mouse.ScrollWheelValue - _oldMouse.ScrollWheelValue;
        if (wheel != 0)
        {
            if (_mouse.X > GraphicsDevice.Viewport.Width - 360)
                _detailScroll = Math.Max(0, _detailScroll - Math.Sign(wheel) * 48);
            else
            {
                _atlasSelected = Math.Clamp(_atlasSelected - Math.Sign(wheel) * _atlasPageSize, 0, Math.Max(0, _filtered.Count - 1));
                _detailScroll = 0;
            }
        }
    }
    private void DrawAtlas(int width, int height)
    {
        Text("THE SPRITE ATLAS", 22, 22, Ink, 2);
        Text("EXACT CROPS / LIVE BINDINGS / HONEST UNCERTAINTY", 22, 55, Muted);
        if (Button(new(width - 180, 18, 158, 44), "BACK TO GAME", selected: true))
            _atlas = false;
        int gridWidth = width - 394;
        Fill(new(22, 86, gridWidth, 42), Panel);
        Text(_search.Length > 0 ? _search : "Type to search sprites, roles, IDs, or ideas...", 34, 101, _search.Length > 0 ? Ink : Muted);
        int filterWidth = (gridWidth - 16) / 3;
        if (Button(new(22, 136, filterWidth, 42), Short(Packs[_packFilter].ToUpperInvariant(), filterWidth / 8 - 2), tip: "Pack [F3]"))
        {
            _packFilter = (_packFilter + 1) % Packs.Length;
            FilterAtlas();
        }
        if (Button(new(30 + filterWidth, 136, filterWidth, 42), Short(Families[_familyFilter].ToUpperInvariant(), filterWidth / 8 - 2), tip: "Family [F6 / Tab]"))
        {
            _familyFilter = (_familyFilter + 1) % Families.Length;
            FilterAtlas();
        }
        if (Button(new(38 + filterWidth * 2, 136, filterWidth, 42), Usages[_usageFilter], tip: "Runtime usage [F7]"))
        {
            _usageFilter = (_usageFilter + 1) % Usages.Length;
            FilterAtlas();
        }
        _atlasColumns = Math.Max(4, (gridWidth + 8) / 100);
        int tileWidth = (gridWidth - (_atlasColumns - 1) * 8) / _atlasColumns, rows = Math.Max(1, (height - 256) / 94);
        _atlasPageSize = _atlasColumns * rows;
        int start = _atlasSelected / _atlasPageSize * _atlasPageSize;
        for (int n = start; n < Math.Min(_filtered.Count, start + _atlasPageSize); n++)
        {
            var e = _filtered[n];
            int slot = n - start;
            var r = new Rectangle(22 + slot % _atlasColumns * (tileWidth + 8), 192 + slot / _atlasColumns * 94, tileWidth, 86);
            if (Button(r, "", selected: n == _atlasSelected))
            {
                _atlasSelected = n;
                _detailScroll = 0;
            }
            _sprites.DrawSprite(_batch, e.Id, new(r.Center.X - 22, r.Y + 6, 44, 44), _atlasMask ? Gold : Color.White, !_atlasMask);
            Text(Short(e.Name, Math.Max(4, (tileWidth - 10) / 8)), r.X + 5, r.Y + 55, Ink);
            Text(e.Confidence == "high" ? "NAMED" : "REVIEW", r.X + 5, r.Y + 71, e.Confidence == "high" ? Green : Muted);
        }
        int footer = height - 58;
        if (Button(new(22, footer, 112, 42), "PREVIOUS", enabled: start > 0))
            _atlasSelected = Math.Max(0, start - _atlasPageSize);
        if (Button(new(142, footer, 112, 42), "NEXT", enabled: start + _atlasPageSize < _filtered.Count))
            _atlasSelected = Math.Min(_filtered.Count - 1, start + _atlasPageSize);
        Text($"{_filtered.Count:0000} / {_entries.Count:0000} SPRITES", 274, footer + 7, Muted);
        Text("F3 PACK / F4 PALETTE / F6 FAMILY / F7 USAGE", 274, footer + 25, Muted);
        var panel = new Rectangle(width - 354, 86, 332, height - 108);
        Fill(panel, Panel);
        if (_filtered.Count == 0)
        {
            Wrap("No matching artwork. Clear the search with Esc or change a filter.", panel.X + 18, panel.Y + 30, panel.Width - 36, Muted);
            return;
        }
        var entry = _filtered[_atlasSelected];
        Fill(new(panel.X + 16, panel.Y + 14, panel.Width - 32, 144), Background);
        _sprites.DrawSprite(_batch, entry.Id, new(panel.Center.X - 60, panel.Y + 26, 120, 120), _atlasMask ? Gold : Color.White, !_atlasMask);
        if (Button(new(panel.X + 16, panel.Y + 166, panel.Width - 32, 40), _atlasMask ? "PREVIEW / WARM MASK" : "PREVIEW / ORIGINAL", tip: "Toggle preview palette [F4]"))
            _atlasMask = !_atlasMask;
        var clip = new Rectangle(panel.X + 12, panel.Y + 218, panel.Width - 24, panel.Height - 230);
        _batch.End();
        GraphicsDevice.ScissorRectangle = clip;
        _batch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp, null, _scissor);
        float x = clip.X + 4, y = clip.Y - _detailScroll, w = clip.Width - 8;
        y += Wrap(entry.Name.ToUpperInvariant(), x, y, w, Ink, 1.2f) + 14;
        y += Wrap(entry.Id, x, y, w, Gold) + 12;
        y += Wrap(entry.Basis + " / " + entry.Confidence, x, y, w, Muted) + 12;
        y += Wrap(entry.Source + "\n[" + string.Join(",", entry.Rect) + "]", x, y, w, Muted) + 18;
        foreach (var game in new[] { "colony", "rpg" })
        {
            Text(game.ToUpperInvariant() + " BINDINGS", x, y, Gold);
            y += 23;
            y += Wrap(entry.Usage[game].Count > 0 ? string.Join(", ", entry.Usage[game]) : "Unused by this game.", x, y, w, Muted) + 16;
        }
        Text("RPG INTERPRETATION", x, y, Gold);
        y += 23;
        y += Wrap(entry.Role, x, y, w, Ink) + 16;
        Text("ADAPTATION / LIMITS", x, y, Gold);
        y += 23;
        y += Wrap(entry.Adaptation, x, y, w, Muted) + 18;
        Text("FUTURE IDEAS", x, y, Gold);
        y += 23;
        y += Wrap(string.Join("\n\n", entry.Ideas), x, y, w, Muted);
        int maxScroll = Math.Max(0, (int)(y + _detailScroll - clip.Bottom) + 16);
        _detailScroll = Math.Min(_detailScroll, maxScroll);
        _batch.End();
        GraphicsDevice.ScissorRectangle = new(0, 0, width, height);
        _batch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp);
    }
}
