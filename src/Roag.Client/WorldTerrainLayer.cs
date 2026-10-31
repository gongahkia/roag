using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private readonly record struct TerrainTile(Rectangle Rect, string? Sprite, Color Tint, string? Prop, float Light, bool Door, Color Background);
    private List<TerrainTile> _terrain = [], _nextTerrain = [];
    private List<(Rectangle Rect, string Sprite, float Light)> _resourceIcons = [], _nextResourceIcons = [];
    private RenderTarget2D? _terrainLayer;
    private Rectangle _terrainMap;
    private WorldSession? _terrainSession;
    private int _terrainFloor = -1;
    private long _terrainCacheHits, _terrainCacheRebuilds, _terrainCacheBypasses;
    private int _terrainSettledFrames;
    private bool _terrainLayerDirty = true;

    private void BuildTerrainSnapshot()
    {
        _nextTerrain.Clear(); _nextResourceIcons.Clear();
        var left = _camera.ScreenToWorld(new(_map.Left, _map.Top)); var right = _camera.ScreenToWorld(new(_map.Right, _map.Bottom));
        for (int y = (int)Math.Floor(left.Y); y <= (int)right.Y; y++) for (int x = (int)Math.Floor(left.X); x <= (int)right.X; x++)
        {
            var p = new WorldPosition(x, y, _session.State.Position.Floor);
            if (!_session.Chunks.Seen(p)) continue;
            var rect = TileRect(p); if (!_map.Intersects(rect)) continue;
            var chunk = _session.Chunks.Get(p.Chunk); var biome = chunk.Biomes[p.LocalIndex]; int culture = chunk.Cultures[p.LocalIndex];
            var material = _session.Chunks.At(p);
            Color tint = material switch { Material.Water => new(72, 132, 158), Material.Tree => new(104, 141, 104), Material.StairsDown or Material.StairsUp => Gold, Material.Camp => new(241, 159, 88), _ => biome switch { Biome.Forest => new(127, 150, 112), Biome.Marsh => new(127, 144, 110), Biome.Badland => new(187, 144, 103), Biome.Snow => new(199, 211, 217), Biome.Highland => new(160, 163, 159), _ => new(156, 170, 123) } };
            if (material is Material.Wall or Material.Floor or Material.Door) tint = new(191, 184, 163);
            bool bright = _session.Visible.Contains(p);
            float light = bright ? 1 : .35f;
            bool ground = material is Material.Grass or Material.Earth or Material.Floor;
            var background = material switch { Material.Grass => new Color(29, 42, 33), Material.Floor => new Color(51, 45, 36), Material.Earth => new Color(49, 43, 33), Material.Water => new Color(19, 38, 47), _ => new Color(32, 36, 34) };
            string? sprite = material != Material.Grass || WorldRandom.Hash(_session.Seed, x, y) % 3 == 0
                ? Materials.Sprite(material, biome, culture < 0 ? null : SampleLibrary.Default.Cultures[culture]) : null;
            if (_session.DungeonTerrainAt(p) is { } semantic)
            {
                sprite = semantic switch { 'b' => "shrub", 'g' => "reed", 'i' => "water", 'm' => "wisp", 'c' => "crystal", _ => sprite };
                tint = semantic switch { 'b' => new(171, 192, 104), 'g' => new(111, 150, 124), 'i' => new(145, 214, 237),
                    'm' => new(163, 160, 195), 'c' => new(151, 224, 227), _ => tint };
                ground = false;
            }
            var resource = _session.ResourceAt(p);
            if (resource != null)
            {
                if (material is Material.Tree or Material.Rock or Material.Water)
                {
                    if (resource.Available > 0) _nextResourceIcons.Add((new(rect.Right - rect.Width / 2, rect.Top, rect.Width / 2, rect.Height / 2), resource.Sprite, light));
                    else { sprite = resource.Definition.DepletedSprite; tint = Muted; }
                }
                else { sprite = resource.Sprite; tint = resource.Available > 0 ? Gold : Muted; ground = false; }
            }
            string? prop = resource == null && _session.ObjectAt(p) is { Kind: "decoration" } decoration ? decoration.Sprite : null;
            _nextTerrain.Add(new(rect, sprite, tint * light * (ground ? .32f : 1), prop, light, material == Material.Door, background * light));
        }
    }

    private void PrepareTerrainLayer()
    {
        BuildTerrainSnapshot();
        int width = _frame!.Width, height = _frame.Height;
        bool changed = _terrainMap != _map || !ReferenceEquals(_terrainSession, _session)
            || _terrainFloor != _session.State.Position.Floor
            || !CollectionsMarshal.AsSpan(_terrain).SequenceEqual(CollectionsMarshal.AsSpan(_nextTerrain))
            || !CollectionsMarshal.AsSpan(_resourceIcons).SequenceEqual(CollectionsMarshal.AsSpan(_nextResourceIcons));
        if (changed)
        {
            (_terrain, _nextTerrain) = (_nextTerrain, _terrain);
            (_resourceIcons, _nextResourceIcons) = (_nextResourceIcons, _resourceIcons);
            _terrainMap = _map; _terrainSession = _session; _terrainFloor = _session.State.Position.Floor;
            _terrainLayerDirty = true; _terrainSettledFrames = 0;
        }
        else _terrainSettledFrames = Math.Min(2, _terrainSettledFrames + 1);
        if (_terrainLayer != null && (_terrainLayer.Width != width || _terrainLayer.Height != height || _terrainLayer.IsDisposed))
        { _terrainLayer.Dispose(); _terrainLayer = null; _terrainLayerDirty = true; }
        // Reuse is worthwhile once the actual visual inputs settle. During
        // camera motion or edits, draw directly without an extra target pass.
        if (!_terrainLayerDirty || _terrainSettledFrames == 0) return;
        _terrainLayer ??= new(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
        GraphicsDevice.SetRenderTarget(_terrainLayer); GraphicsDevice.Clear(Color.Transparent);
        GraphicsDevice.ScissorRectangle = _map;
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: _mapRasterizer);
        DrawTerrainSnapshot();
        _batch.End(); GraphicsDevice.SetRenderTarget(null);
        _terrainLayerDirty = false; _terrainCacheRebuilds++;
    }

    private void DrawTerrainSnapshot()
    {
        Fill(_map, new(9, 16, 21));
        foreach (var tile in _terrain) Fill(tile.Rect, tile.Background);
        _batch.End(); _batch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: _mapRasterizer);
        foreach (var tile in _terrain)
            if (tile.Sprite is { } sprite) _sprites.Draw(_batch, sprite, tile.Rect, tile.Tint);
        _batch.End(); _batch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: _mapRasterizer);
        foreach (var tile in _terrain)
        {
            if (tile.Prop is { } prop) _sprites.Draw(_batch, prop, tile.Rect, new Color(154, 141, 105) * tile.Light);
            if (tile.Door) Fill(new(tile.Rect.Center.X - 2, tile.Rect.Top + tile.Rect.Height / 3, 4, tile.Rect.Height / 2), Gold * .7f);
        }
        _batch.End(); _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: _mapRasterizer);
        foreach (var resource in _resourceIcons) _sprites.Draw(_batch, resource.Sprite, resource.Rect, Gold * resource.Light);
    }

    private void DrawCachedTerrain()
    {
        if (_terrainLayerDirty) { DrawTerrainSnapshot(); _terrainCacheBypasses++; }
        else { _batch.Draw(_terrainLayer!, new Rectangle(0, 0, _frame!.Width, _frame.Height), Color.White); _terrainCacheHits++; }
    }
}
