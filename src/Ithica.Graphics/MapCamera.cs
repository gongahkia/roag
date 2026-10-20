using Microsoft.Xna.Framework;

namespace Ithica.Graphics;

public sealed class MapCamera
{
    public Vector2 Center
    {
        get; set;
    }
    public float CellSize { get; private set; } = 24;
    public Rectangle Viewport
    {
        get; set;
    }
    public Vector2 ScreenToWorld(Vector2 point) => Center + (point - new Vector2(Viewport.Center.X, Viewport.Center.Y)) / CellSize;
    public Vector2 WorldToScreen(Vector2 point) => new Vector2(Viewport.Center.X, Viewport.Center.Y) + (point - Center) * CellSize;
    public void ZoomAt(Vector2 screenPoint, int direction)
    {
        var anchor = ScreenToWorld(screenPoint);
        CellSize = Math.Clamp(CellSize * MathF.Pow(1.25f, direction), 6, 72);
        Center += anchor - ScreenToWorld(screenPoint);
    }
    public void Reset(float cellSize = 24) => CellSize = Math.Clamp(cellSize, 6, 72);
    public void Pan(Vector2 screenDelta) => Center -= screenDelta / CellSize;
}
