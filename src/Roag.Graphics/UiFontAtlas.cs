using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Roag.Graphics;

/// <summary>Bundled UI text faces. This is separate from the game sprite catalog.</summary>
public sealed class UiFontAtlas : IDisposable
{
    private sealed record Glyph(int Code, int[] Rect, int Left, int Top, int Advance);
    private sealed record Face(string Style, string Size, string Source, string Sha256, int Ascent, int Advance, Glyph[] Glyphs);
    private sealed record Document(int Version, string Texture, Face[] Faces);
    private readonly Texture2D _texture;
    private readonly Dictionary<(string Style, string Size), Face> _faces;
    private readonly Dictionary<(string Style, string Size), Dictionary<char, Glyph>> _glyphs;
    private readonly TextLayoutCache _layouts = new();
    private Face _face;
    private Dictionary<char, Glyph> _characters;
    public int Advance => _face.Advance;
    public int LineHeight => Math.Max(16, _face.Ascent + 5);

    public UiFontAtlas(GraphicsDevice device, string assetRoot)
    {
        string directory = Path.Combine(assetRoot, "fonts");
        var data = JsonSerializer.Deserialize<Document>(File.ReadAllText(Path.Combine(directory, "ui-fonts.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Missing UI fonts.");
        if (data.Version != 1 || data.Faces.Length != 6) throw new InvalidDataException("Invalid UI font atlas.");
        using var stream = File.OpenRead(Path.Combine(directory, data.Texture)); _texture = Texture2D.FromStream(device, stream);
        var pixels = new Color[_texture.Width * _texture.Height]; _texture.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.FromNonPremultiplied(pixels[i].R, pixels[i].G, pixels[i].B, pixels[i].A);
        _texture.SetData(pixels);
        _faces = data.Faces.ToDictionary(f => (f.Style, f.Size));
        _glyphs = data.Faces.ToDictionary(f => (f.Style, f.Size), f => f.Glyphs.ToDictionary(g => (char)g.Code));
        _face = _faces[("Pixel", "Normal")]; _characters = _glyphs[("Pixel", "Normal")];
    }

    public void Select(string style, string size) { _face = _faces[(style, size)]; _characters = _glyphs[(style, size)]; }
    public float Measure(string text, float scale = 1) => text.Length * Advance * scale;

    public void Draw(SpriteBatch batch, string text, Vector2 position, Color color, float scale = 1)
    {
        float x = position.X, y = position.Y;
        foreach (char character in text)
        {
            if (character == '\n') { x = position.X; y += LineHeight * scale; continue; }
            char key = character switch { '—' or '–' => '-', '’' => '\'', '×' => 'x', '·' => '/', _ => character };
            var glyph = _characters.GetValueOrDefault(key) ?? _characters['?'];
            if (glyph.Rect[2] > 0 && glyph.Rect[3] > 0)
                batch.Draw(_texture, new Vector2(MathF.Round(x + glyph.Left * scale), MathF.Round(y + (_face.Ascent - glyph.Top) * scale)),
                    new Rectangle(glyph.Rect[0], glyph.Rect[1], glyph.Rect[2], glyph.Rect[3]), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            x += Advance * scale;
        }
    }

    public float Wrapped(SpriteBatch batch, string text, Vector2 position, float width, Color color)
    {
        var lines = _layouts.Get(text, Math.Max(1, (int)(width / Advance)));
        for (int i = 0; i < lines.Count; i++) Draw(batch, lines[i], position + new Vector2(0, i * (LineHeight + 3)), color);
        return lines.Count * (LineHeight + 3);
    }

    public void Dispose() => _texture.Dispose();
}
