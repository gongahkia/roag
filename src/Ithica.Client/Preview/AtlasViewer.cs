using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ithica.Graphics;
using AtlasEntry = Ithica.Client.ArtworkEntry;

namespace Ithica.Client.Preview;

public sealed partial class PreviewGame
{
    private List<AtlasEntry> _entries = [], _filtered = [];
    private string _search = "";
    private int _packFilter, _familyFilter, _colorFilter, _sortFilter, _layerFilter, _compositionFilter, _usageFilter;
    private int _atlasSelected, _atlasColumns = 6, _atlasPageSize = 30, _detailScroll;
    private bool _atlasMask, _atlasShowGame, _atlasHelp;
    private ArtworkQuery _atlasQuery = new("");
    private string[] _atlasTypes = [], _atlasLayers = [], _atlasCompositions = [];
    private static readonly string[] Packs = ["ALL PACKS", "urizen", "godot-node-icons", "input-prompts"];
    private static readonly string[] AtlasColors = ["ANY COLOR", .. ArtworkQuery.Colors];
    private static readonly string[] Usages = ["ANY USAGE", "ITHICA BOUND", "PREVIEW BOUND", "COLONY BOUND", "UNBOUND"];

    private void FilterAtlas()
    {
        if (_atlasTypes.Length == 0)
        {
            _atlasTypes = ["ANY TYPE", .. _entries.Select(e => e.Asset.Type).Distinct().Order(StringComparer.Ordinal)];
            _atlasLayers = ["ANY LAYER", .. _entries.SelectMany(e => e.Asset.Layers).Distinct().Order(StringComparer.Ordinal)];
            _atlasCompositions = ["ANY COMPOSITION", .. _entries.Select(e => e.Asset.Composition.Kind).Distinct().Order(StringComparer.Ordinal)];
        }
        _atlasQuery = new(_search, _atlasShowGame);
        var matches = _entries.Where(e => (_packFilter == 0 || e.Pack == Packs[_packFilter])
            && (_familyFilter == 0 || e.Asset.Type == _atlasTypes[_familyFilter])
            && (_colorFilter == 0 || e.Asset.Appearance.ColorCoverage.GetValueOrDefault(AtlasColors[_colorFilter]) >= .05)
            && (_layerFilter == 0 || e.Asset.Layers.Contains(_atlasLayers[_layerFilter]))
            && (_compositionFilter == 0 || e.Asset.Composition.Kind == _atlasCompositions[_compositionFilter])
            && (_usageFilter switch
            {
                1 => e.IthicaStatus == "bound",
                2 => e.Usage.GetValueOrDefault("rpg")?.Count > 0,
                3 => e.Usage.GetValueOrDefault("colony")?.Count > 0,
                4 => e.Usage.Values.All(v => v.Count == 0),
                _ => true
            }) && _atlasQuery.Matches(e));
        _filtered = ArtworkQuery.Order(matches, _atlasQuery.Sort ?? ArtworkQuery.Sorts[_sortFilter], _atlasQuery.Descending);
        _atlasSelected = 0;
        _detailScroll = 0;
    }
    private void ResetAtlas()
    {
        _search = "";
        _packFilter = _familyFilter = _colorFilter = _sortFilter = _layerFilter = _compositionFilter = _usageFilter = 0;
        _atlasShowGame = false;
        FilterAtlas();
    }
    private void CycleAtlas(ref int value, int count)
    {
        bool backwards = _keys.IsKeyDown(Keys.LeftShift) || _keys.IsKeyDown(Keys.RightShift);
        value = (value + count + (backwards ? -1 : 1)) % count;
        FilterAtlas();
    }
    private void AtlasInput()
    {
        if (Pressed(Keys.Escape))
        {
            if (_search.Length > 0) { _search = ""; FilterAtlas(); }
            else _atlas = false;
        }
        if (Pressed(Keys.Back) && _search.Length > 0) { _search = _search[..^1]; FilterAtlas(); }
        if (Pressed(Keys.F1)) _atlasHelp = !_atlasHelp;
        if (Pressed(Keys.F3)) CycleAtlas(ref _packFilter, Packs.Length);
        if (Pressed(Keys.F6) || Pressed(Keys.Tab)) CycleAtlas(ref _familyFilter, _atlasTypes.Length);
        if (Pressed(Keys.F7)) CycleAtlas(ref _usageFilter, Usages.Length);
        if (Pressed(Keys.F8)) CycleAtlas(ref _colorFilter, AtlasColors.Length);
        if (Pressed(Keys.F9)) ResetAtlas();
        if (Pressed(Keys.F10)) CycleAtlas(ref _sortFilter, ArtworkQuery.Sorts.Length);
        if (Pressed(Keys.F4)) _atlasMask = !_atlasMask;
        int offset = Pressed(Keys.Right) ? 1 : Pressed(Keys.Left) ? -1 : Pressed(Keys.Down) ? _atlasColumns : Pressed(Keys.Up) ? -_atlasColumns : Pressed(Keys.PageDown) ? _atlasPageSize : Pressed(Keys.PageUp) ? -_atlasPageSize : 0;
        if (offset != 0)
        {
            _atlasSelected = Math.Clamp(_atlasSelected + offset, 0, Math.Max(0, _filtered.Count - 1));
            _detailScroll = 0;
        }
        int wheel = _mouse.ScrollWheelValue - _oldMouse.ScrollWheelValue;
        if (wheel != 0)
        {
            if (_mouse.X > GraphicsDevice.Viewport.Width - 360) _detailScroll = Math.Max(0, _detailScroll - Math.Sign(wheel) * 48);
            else
            {
                _atlasSelected = Math.Clamp(_atlasSelected - Math.Sign(wheel) * _atlasPageSize, 0, Math.Max(0, _filtered.Count - 1));
                _detailScroll = 0;
            }
        }
    }

    private void DrawAtlas(int width, int height)
    {
        Text("THE ARTWORK ATLAS", 22, 22, Ink, 2);
        Text("NEUTRAL ART / MEASURED COLORS / SOURCE EVIDENCE", 22, 55, Muted);
        if (Button(new(width - 180, 18, 158, 44), "BACK TO GAME", selected: true)) _atlas = false;
        int gridWidth = width - 394;
        Fill(new(22, 86, gridWidth, 42), Panel);
        int textWidth = Math.Max(12, (gridWidth - 28) / 8);
        string text = _search.Length > 0 ? _search : "Search art, IDs or subject:ghost confidence:high";
        if (text.Length > textWidth) text = "< " + text[^(textWidth - 2)..];
        Text(text, 34, 101, _search.Length > 0 ? Ink : Muted);
        int filterWidth = (gridWidth - 24) / 4;
        string Label(string value) => Short(value.ToUpperInvariant(), Math.Max(8, filterWidth / 8 - 2));
        Rectangle Box(int column, int y) => new(22 + column * (filterWidth + 8), y, filterWidth, 42);
        if (Button(Box(0,136), Label(Packs[_packFilter]), tip: "Pack [F3]; Shift cycles backward")) CycleAtlas(ref _packFilter, Packs.Length);
        if (Button(Box(1,136), Label("TYPE: " + (_familyFilter == 0 ? "ANY" : _atlasTypes[_familyFilter])), tip: "Artwork type [F6 / Tab]")) CycleAtlas(ref _familyFilter, _atlasTypes.Length);
        if (Button(Box(2,136), Label("COLOR: " + (_colorFilter == 0 ? "ANY" : AtlasColors[_colorFilter])), tip: "Foreground color at 5% minimum [F8]")) CycleAtlas(ref _colorFilter, AtlasColors.Length);
        if (Button(Box(3,136), Label("SORT: " + (_atlasQuery.Sort ?? ArtworkQuery.Sorts[_sortFilter])), tip: "Sort [F10]; query sort: overrides this control")) CycleAtlas(ref _sortFilter, ArtworkQuery.Sorts.Length);
        if (Button(Box(0,186), Label("LAYER: " + (_layerFilter == 0 ? "ANY" : _atlasLayers[_layerFilter])), tip: "Suggested layer")) CycleAtlas(ref _layerFilter, _atlasLayers.Length);
        if (Button(Box(1,186), Label(_atlasCompositions[_compositionFilter]), tip: "Composition; fragments do not imply sockets")) CycleAtlas(ref _compositionFilter, _atlasCompositions.Length);
        if (Button(Box(2,186), Label(Usages[_usageFilter]), tip: "Optional game usage [F7]")) CycleAtlas(ref _usageFilter, Usages.Length);
        if (Button(Box(3,186), Label(_atlasShowGame ? "GAME NOTES: ON" : "GAME NOTES: OFF"), selected: _atlasShowGame, tip: "Include game notes in text search and details"))
        {
            _atlasShowGame = !_atlasShowGame;
            FilterAtlas();
        }
        Text(_atlasQuery.Error.Length > 0 ? Short(_atlasQuery.Error, textWidth) : "F1 QUERY HELP / F9 RESET / SHIFT REVERSES FILTERS", 22, 240, _atlasQuery.Error.Length > 0 ? Gold : Muted);

        _atlasColumns = Math.Max(4, (gridWidth + 8) / 100);
        int tileWidth = (gridWidth - (_atlasColumns - 1) * 8) / _atlasColumns;
        int rows = Math.Max(1, (height - 326) / 94);
        _atlasPageSize = _atlasColumns * rows;
        int start = _atlasSelected / _atlasPageSize * _atlasPageSize;
        for (int n = start; n < Math.Min(_filtered.Count, start + _atlasPageSize); n++)
        {
            var e = _filtered[n];
            int slot = n - start;
            var rect = new Rectangle(22 + slot % _atlasColumns * (tileWidth + 8), 264 + slot / _atlasColumns * 94, tileWidth, 86);
            if (Button(rect, "", selected: n == _atlasSelected)) { _atlasSelected = n; _detailScroll = 0; }
            _sprites.FitSprite(_batch, e.Id, new(rect.Center.X - 22, rect.Y + 6, 44, 44), _atlasMask ? Gold : Color.White, !_atlasMask);
            int letters = Math.Max(4, (tileWidth - 10) / 8);
            Text(Short(e.Asset.Name, letters), rect.X + 5, rect.Y + 55, Ink);
            Text(Short(e.Asset.Type + " " + e.Asset.Appearance.PrimaryColor, letters), rect.X + 5, rect.Y + 71, Muted);
        }
        int footer = height - 58;
        if (Button(new(22, footer, 112, 42), "PREVIOUS", enabled: start > 0)) _atlasSelected = Math.Max(0, start - _atlasPageSize);
        if (Button(new(142, footer, 112, 42), "NEXT", enabled: start + _atlasPageSize < _filtered.Count)) _atlasSelected = Math.Min(_filtered.Count - 1, start + _atlasPageSize);
        Text($"{_filtered.Count:0000} / {_entries.Count:0000} SPRITES", 274, footer + 7, Muted);
        Text("F3 PACK / F6 TYPE / F8 COLOR / F10 SORT", 274, footer + 25, Muted);

        var panel = new Rectangle(width - 354, 86, 332, height - 108);
        Fill(panel, Panel);
        if (_filtered.Count == 0)
        {
            Wrap((_atlasQuery.Error.Length > 0 ? _atlasQuery.Error : "No matching artwork.") + " F9 resets filters; Esc clears typed text.", panel.X + 18, panel.Y + 30, panel.Width - 36, Muted);
            return;
        }
        var entry = _filtered[_atlasSelected];
        var a = entry.Asset;
        Fill(new(panel.X + 16, panel.Y + 14, panel.Width - 32, 144), Background);
        _sprites.FitSprite(_batch, entry.Id, new(panel.Center.X - 60, panel.Y + 26, 120, 120), _atlasMask ? Gold : Color.White, !_atlasMask);
        if (Button(new(panel.X + 16, panel.Y + 166, panel.Width - 32, 40), _atlasMask ? "PREVIEW / WARM MASK" : "PREVIEW / ORIGINAL", tip: "Preview palette only [F4]")) _atlasMask = !_atlasMask;
        if (Button(new(panel.X + 16, panel.Y + 214, panel.Width - 32, 40), _atlasHelp ? "CLOSE QUERY HELP [F1]" : "QUERY HELP [F1]", selected: _atlasHelp))
        {
            _atlasHelp = !_atlasHelp; _detailScroll = 0;
        }

        var clip = new Rectangle(panel.X + 12, panel.Y + 266, panel.Width - 24, panel.Height - 278);
        _batch.End();
        GraphicsDevice.ScissorRectangle = clip;
        _batch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp, null, _scissor);
        float x = clip.X + 4, y = clip.Y - _detailScroll, w = clip.Width - 8;
        void Detail(string label, string value)
        {
            Text(label.ToUpperInvariant(), x, y, Gold); y += 23;
            y += Wrap(value, x, y, w, Muted) + 16;
        }
        if (_atlasHelp)
        {
            Detail("Artwork queries", "Free words must all match neutral art descriptions. Add typed filters; repeated subject/color terms must all match.");
            Detail("Examples", "subject:ghost confidence:high\ncolor:green type:actor\nlayer:object composition:fragment_candidate\nhex:#feffff\nprimary:blue sort:name desc:true\ncolor:white coverage:0.3");
            Detail("Confidence", "confidence: sets minimum subject confidence. source: and visual: match original identity and visual classification confidence separately.");
            Detail("Optional game notes", "game:true includes game casting/proposals. Game notes are otherwise excluded. Usage is a separate optional filter.");
            Detail("Controls", "F3 pack, F6/Tab type, F8 color, F10 sort. Shift reverses cycling. F7 usage, F4 preview mask, F9 reset. Arrows select; wheel/pages browse; wheel over this panel scrolls.");
            Detail("All typed keys", string.Join(", ", ArtworkQuery.Keys));
            Detail("Exports", "The offline assets/atlas.html exports neutral metadata, exact crop PNGs and layered scenes. The canonical assets/catalog.json and tools/query_assets.py use the same artwork records.");
        }
        else
        {
            y += Wrap(a.Name.ToUpperInvariant(), x, y, w, Ink, 1.2f) + 14;
            y += Wrap(entry.Id, x, y, w, Gold) + 12;
            y += Wrap(a.Description, x, y, w, Ink) + 16;
            Detail("Type / visual confidence", a.Type + " / " + a.Confidence);
            Detail("Suggested layers / composition", string.Join(", ", a.Layers) + "\n" + a.Composition.Kind);
            Detail("Colors", string.Join(", ", a.Appearance.ColorCoverage.Select(c => c.Key + " " + c.Value.ToString("P1", CultureInfo.InvariantCulture))));
            foreach (var color in a.Appearance.Palette)
            {
                int rgb = Convert.ToInt32(color.Hex[1..], 16);
                Fill(new((int)x, (int)y, 18, 18), new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)color.Alpha));
                y += Wrap(color.Hex + " " + color.Coverage.ToString("P1", CultureInfo.InvariantCulture) + " / A" + color.Alpha, x + 26, y, w - 26, Muted) + 10;
            }
            y += 12;
            Detail("Subjects / confidence / basis", string.Join("\n", a.Subjects.Select(s => s.Term + " / " + s.Confidence + " / " + s.Basis)));
            Detail("Source identity", entry.Basis + " / " + entry.Confidence);
            Detail("Original crop", entry.Source + "\n[" + string.Join(",", entry.Rect) + "]");
            Detail("Composition limits", "Animation " + a.Composition.Animation + "; directions " + a.Composition.Directions + "; sockets " + a.Composition.Sockets + "; collision " + a.Composition.Collision + ".");
            foreach (string key in new[] { "named_variant_group", "glyph", "input_prompt", "exact_rgba_duplicates", "character_matrix" })
                if (a.Relations.TryGetProperty(key, out var value) && value.ValueKind != System.Text.Json.JsonValueKind.Null && value.GetRawText() != "[]")
                    Detail(key.Replace('_', ' '), value.ToString());
            Detail("Review status", a.Evidence.ReviewStatus);
            foreach (var observation in a.Evidence.Observations) Detail(observation.Scope + " observation", observation.Description);
            if (a.Evidence.Uncertainties.Count > 0) Detail("Uncertainty", string.Join("\n", a.Evidence.Uncertainties));
            Detail("Evidence", string.Join("\n", a.Evidence.References));
            Detail("Recorded inspection", string.Join("\n", a.Evidence.InspectionRefs));
            if (_atlasQuery.IncludeGame)
            {
                Detail("Optional current casting", entry.IthicaStatus + ": " + entry.LunarRole);
                Detail("Lunar limits and proposals", entry.LunarLimits + "\n" + string.Join("\n", entry.LunarIdeas));
                Detail("Colony / preview bindings", string.Join(", ", entry.Usage.GetValueOrDefault("colony") ?? []) + "\n" + string.Join(", ", entry.Usage.GetValueOrDefault("rpg") ?? []));
                Detail("Retained preview interpretation", entry.Role + "\n" + entry.Adaptation);
                Detail("Prior generation notes and proposals", entry.GenerationNotes + "\n" + string.Join("\n", entry.Ideas));
            }
        }
        int maxScroll = Math.Max(0, (int)(y + _detailScroll - clip.Bottom) + 16);
        _detailScroll = Math.Min(_detailScroll, maxScroll);
        _batch.End();
        GraphicsDevice.ScissorRectangle = new(0, 0, width, height);
        _batch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp);
    }
    private void CheckArtworkAtlas(Action<bool, string> check)
    {
        check(_entries.Count == 7115 && _entries.All(e => e.Asset.Name.Length > 0 && e.Asset.Type.Length > 0
            && e.Asset.Evidence.InspectionRefs.Count > 0 && e.Asset.Subjects.Count > 0
            && e.Asset.Appearance.Palette.Sum(p => p.Pixels) == e.Asset.Appearance.ForegroundPixels),
            "all native artwork metadata, palettes and inspection references load");
        ResetAtlas();
        _search = "subject:ghost confidence:high"; FilterAtlas();
        check(_filtered.Count == 1 && _filtered[0].Id == "urizen-onebit-r42-c110", "native subject confidence query finds original ghost");
        _search = "Interference"; FilterAtlas();
        check(_filtered.Count == 0, "native neutral search excludes game casting");
        _atlasShowGame = true; FilterAtlas();
        check(_filtered.Count > 0, "native optional game search finds casting");
        ResetAtlas();
        _familyFilter = Array.IndexOf(_atlasTypes, "actor");
        _colorFilter = Array.IndexOf(AtlasColors, "green"); FilterAtlas();
        check(_filtered.Count == 513, "native type and measured-color controls agree with canonical query");
        _sortFilter = Array.IndexOf(ArtworkQuery.Sorts, "name"); FilterAtlas();
        check(_filtered.Select(e => e.Asset.Name.ToLowerInvariant()).SequenceEqual(_filtered.Select(e => e.Asset.Name.ToLowerInvariant()).Order(StringComparer.Ordinal)),
            "native sort control orders neutral names");
        ResetAtlas();
        _search = "coverage:2"; FilterAtlas();
        check(_filtered.Count == 0 && _atlasQuery.Error.Length > 0, "invalid artwork query is explicit and has no matches");
        ResetAtlas();
    }

}
