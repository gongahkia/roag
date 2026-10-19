using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Regolith.Client;

public sealed class SpriteLibrary : IDisposable
{
    private sealed class Manifest
    {
        public int SchemaVersion
        {
            get; set;
        }
        public Dictionary<string, string> Bindings { get; set; } = [];
        public Dictionary<string, string> Font { get; set; } = [];
        public Dictionary<string, SpriteData> Sprites { get; set; } = [];
    }
    private sealed class SpriteData
    {
        public string Source { get; set; } = "";
        public int[] Rect { get; set; } = [];
    }
    private readonly record struct TexturePair(Texture2D Original, Texture2D Mask);
    private readonly record struct Frame(TexturePair Texture, Rectangle Source);
    private readonly Dictionary<string, TexturePair> _textures = [];
    private readonly Dictionary<string, Frame> _bindings = [];
    private readonly Dictionary<char, Frame> _font = [];
    public int BindingCount => _bindings.Count;

    public SpriteLibrary(GraphicsDevice device, string assetRoot)
    {
        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(assetRoot, "runtime-sprites.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                     ?? throw new InvalidDataException("Empty runtime sprite manifest.");
        if (manifest.SchemaVersion != 1)
        {
            throw new InvalidDataException("Unsupported runtime sprite manifest.");
        }

        foreach (var data in manifest.Sprites.Values)
        {
            if (_textures.ContainsKey(data.Source))
            {
                continue;
            }

            using var file = File.OpenRead(Path.Combine(assetRoot, data.Source));
            using var decoded = Texture2D.FromStream(device, file);
            var pixels = new Color[decoded.Width * decoded.Height];
            var mask = new Color[pixels.Length];
            decoded.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                if (Math.Max(c.R, Math.Max(c.G, c.B)) <= 8 || c.A == 0)
                {
                    pixels[i] = mask[i] = Color.Transparent;
                    continue;
                }
                pixels[i] = Color.FromNonPremultiplied(c.R, c.G, c.B, c.A);
                mask[i] = new Color(c.A, c.A, c.A, c.A);
            }
            var original = new Texture2D(device, decoded.Width, decoded.Height);
            var monochrome = new Texture2D(device, decoded.Width, decoded.Height);
            original.SetData(pixels);
            monochrome.SetData(mask);
            _textures.Add(data.Source, new(original, monochrome));
        }
        Frame Resolve(string id)
        {
            var s = manifest.Sprites[id];
            if (s.Rect.Length != 4)
            {
                throw new InvalidDataException("Invalid sprite rectangle: " + id);
            }

            var frame = new Frame(_textures[s.Source], new(s.Rect[0], s.Rect[1], s.Rect[2], s.Rect[3]));
            if (frame.Source.Right > frame.Texture.Original.Width || frame.Source.Bottom > frame.Texture.Original.Height)
            {
                throw new InvalidDataException("Sprite outside source image: " + id);
            }

            return frame;
        }
        foreach (var (name, id) in manifest.Bindings)
        {
            _bindings[name] = Resolve(id);
        }

        foreach (var (character, id) in manifest.Font)
        {
            if (character.Length == 1)
            {
                _font[character[0]] = Resolve(id);
            }
        }
    }
    public void Draw(SpriteBatch batch, string binding, Rectangle target, Color color, bool original = false)
    {
        if (!_bindings.TryGetValue(binding, out var frame))
        {
            throw new KeyNotFoundException("Missing sprite binding: " + binding);
        }

        batch.Draw(original ? frame.Texture.Original : frame.Texture.Mask, target, frame.Source, color);
    }
    public void Fit(SpriteBatch batch, string binding, Rectangle bounds, Color color, bool original = false)
    {
        var frame = _bindings[binding];
        float scale = Math.Min(bounds.Width / (float)frame.Source.Width, bounds.Height / (float)frame.Source.Height);
        int w = (int)(frame.Source.Width * scale), h = (int)(frame.Source.Height * scale);
        Draw(batch, binding, new(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h), color, original);
    }
    public static float Measure(string text, float scale = 1) => text.Length * 8 * Math.Max(1, scale);
    public void Text(SpriteBatch batch, string text, Vector2 position, Color color, float scale = 1)
    {
        // Downsampling a one-pixel bitmap stroke makes letters disappear.
        scale = Math.Max(1, scale);
        float x = position.X, y = position.Y;
        foreach (char character in text)
        {
            if (character == '\n')
            {
                x = position.X;
                y += 16 * scale;
                continue;
            }
            if (character != ' ')
            {
                char c = character switch
                {
                    '—' or '–' => '-',
                    '’' => '\'',
                    '×' => 'x',
                    _ => character
                };
                if (!_font.TryGetValue(c, out var frame))
                {
                    frame = _font['?'];
                }

                batch.Draw(frame.Texture.Mask, new Vector2(MathF.Round(x - 3 * scale), MathF.Round(y)), frame.Source, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
            x += 8 * scale;
        }
    }
    public float Wrapped(SpriteBatch batch, string text, Vector2 position, float width, Color color, float scale = 1, float lineSpacing = 3)
    {
        scale = Math.Max(1, scale);
        int columns = Math.Max(1, (int)(width / (8 * scale)));
        float y = position.Y;
        foreach (string paragraph in text.Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > columns)
                {
                    Text(batch, line, new(position.X, y), color, scale);
                    y += 16 * scale + lineSpacing;
                    line = "";
                }
                if (word.Length > columns)
                {
                    if (line.Length > 0)
                    {
                        Text(batch, line, new(position.X, y), color, scale);
                        y += 16 * scale + lineSpacing;
                        line = "";
                    }
                    for (int i = 0; i < word.Length; i += columns)
                    {
                        Text(batch, word.Substring(i, Math.Min(columns, word.Length - i)), new(position.X, y), color, scale);
                        y += 16 * scale + lineSpacing;
                    }
                }
                else
                {
                    line += (line.Length > 0 ? " " : "") + word;
                }
            }
            if (line.Length > 0)
            {
                Text(batch, line, new(position.X, y), color, scale);
            }

            y += 16 * scale + lineSpacing;
        }
        return y - position.Y;
    }
    public void Dispose()
    {
        foreach (var pair in _textures.Values)
        {
            pair.Original.Dispose();
            pair.Mask.Dispose();
        }
    }
}
