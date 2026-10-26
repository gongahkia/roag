namespace Roag.Core.Generation;

/// <summary>Stateless seeded 2D gradient noise. No simulation RNG or trigonometric gradients.</summary>
public sealed class GradientNoise(int seed)
{
    private readonly uint _seed = unchecked((uint)seed);
    private static uint Hash(uint value)
    {
        value ^= value >> 16; value *= 0x7feb352dU;
        value ^= value >> 15; value *= 0x846ca68bU;
        return value ^ value >> 16;
    }
    private double Gradient(int x, int y, double dx, double dy)
    {
        uint hash = Hash(unchecked((uint)x * 0x9e3779b9U ^ (uint)y * 0x85ebca6bU ^ _seed));
        return (hash & 7) switch { 0 => dx, 1 => -dx, 2 => dy, 3 => -dy,
            4 => (dx + dy) * .7071067811865476, 5 => (dx - dy) * .7071067811865476,
            6 => (-dx + dy) * .7071067811865476, _ => (-dx - dy) * .7071067811865476 };
    }
    private static double Fade(double x) => x * x * x * (x * (x * 6 - 15) + 10);
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    public double Sample(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || x is < -1000000000 or > 1000000000 || y is < -1000000000 or > 1000000000)
            throw new ArgumentOutOfRangeException(nameof(x), "Noise coordinates must be finite and bounded.");
        int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
        double dx = x - ix, dy = y - iy, u = Fade(dx), v = Fade(dy);
        return Math.Clamp(Lerp(Lerp(Gradient(ix, iy, dx, dy), Gradient(ix + 1, iy, dx - 1, dy), u),
            Lerp(Gradient(ix, iy + 1, dx, dy - 1), Gradient(ix + 1, iy + 1, dx - 1, dy - 1), u), v) * 1.42, -1, 1);
    }
    public double Fractal(double x, double y, int octaves = 4)
    {
        if (octaves is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(octaves));
        double sum = 0, amplitude = 1, weight = 0;
        for (int octave = 0; octave < octaves; octave++)
        {
            sum += Sample(x, y) * amplitude; weight += amplitude;
            x = x * 2.03 + 17.17; y = y * 2.03 - 9.23; amplitude *= .5;
        }
        return sum / weight;
    }
}
