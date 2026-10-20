using Ithica.Core.Expeditions;

namespace Ithica.Core.Generation;

internal sealed class LunarRandom(int seed, uint salt)
{
    private readonly RandomStream _stream = GenerationSeeds.Stream(seed, salt);
    public int Next(int maximum) => _stream.Next(maximum);
    public int Next(int minimum, int maximum)
    {
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum));
        return minimum + _stream.Next(maximum - minimum);
    }
    public double NextDouble() => _stream.Next(1 << 24) / (double)(1 << 24);
}
