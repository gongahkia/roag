using Microsoft.Xna.Framework;
using Vagabonds.Core.World;

namespace Vagabonds.Client;

/// <summary>Interpolates committed grid steps without changing authoritative coordinates or time.</summary>
internal sealed class GridMotion
{
    private readonly Queue<(Vector2 Target, double Speed)> _steps = [];
    private WorldPosition? _logical;
    public Vector2 Position { get; private set; }
    public bool FacingLeft { get; private set; }
    public bool Moving => _steps.Count > 0;

    public void Reset(WorldPosition cell, bool resetFacing = false)
    {
        _steps.Clear(); _logical = cell; Position = Center(cell);
        if (resetFacing) FacingLeft = false;
    }

    public void Observe(WorldPosition cell, double secondsPerStep)
    {
        if (secondsPerStep <= 0 || !double.IsFinite(secondsPerStep)) throw new ArgumentOutOfRangeException(nameof(secondsPerStep));
        if (_logical is not { } previous || cell.Floor != previous.Floor || cell.Distance(previous) > 8 || _steps.Count >= 8)
        { Reset(cell); return; }
        if (cell == previous) return;
        _steps.Enqueue((Center(cell), Vector2.Distance(Center(previous), Center(cell)) / secondsPerStep));
        _logical = cell;
    }

    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        double remaining = elapsed.TotalSeconds;
        while (remaining > 0 && _steps.TryPeek(out var step))
        {
            var offset = step.Target - Position; double distance = offset.Length();
            if (offset.X < -.0001f) FacingLeft = true;
            else if (offset.X > .0001f) FacingLeft = false;
            double duration = distance / step.Speed;
            if (remaining >= duration - 1e-7)
            { Position = step.Target; _steps.Dequeue(); remaining = Math.Max(0, remaining - duration); }
            else { Position += offset * (float)(remaining / duration); remaining = 0; }
        }
    }

    private static Vector2 Center(WorldPosition cell) => new(cell.X + .5f, cell.Y + .5f);
}
