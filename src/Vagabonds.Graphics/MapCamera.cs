using Microsoft.Xna.Framework;

namespace Vagabonds.Graphics;

public sealed class MapCamera
{
    public Vector2 Center
    {
        get; set;
    }
    public float CellSize { get; private set; } = 24;
    private float _minimum = 6, _maximum = 72;
    public void ConstrainZoom(float minimum, float maximum)
    {
        if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum <= 0 || maximum < minimum) throw new ArgumentOutOfRangeException(nameof(minimum));
        _minimum = minimum; _maximum = maximum; CellSize = Math.Clamp(CellSize, minimum, maximum);
    }
    public Rectangle Viewport
    {
        get; set;
    }
    public Vector2 ScreenToWorld(Vector2 point) => Center + (point - new Vector2(Viewport.Center.X, Viewport.Center.Y)) / CellSize;
    public Vector2 WorldToScreen(Vector2 point) => new Vector2(Viewport.Center.X, Viewport.Center.Y) + (point - Center) * CellSize;
    public void ZoomAt(Vector2 screenPoint, int direction)
    {
        var anchor = ScreenToWorld(screenPoint);
        CellSize = Math.Clamp(CellSize * MathF.Pow(1.25f, direction), _minimum, _maximum);
        Center += anchor - ScreenToWorld(screenPoint);
    }
    public void Reset(float cellSize = 24) => CellSize = Math.Clamp(cellSize, _minimum, _maximum);
    public void Pan(Vector2 screenDelta) => Center -= screenDelta / CellSize;
}
