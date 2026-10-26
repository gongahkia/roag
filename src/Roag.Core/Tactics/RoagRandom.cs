using System.Text;

namespace Roag.Core.Tactics;

/// <summary>Original Park–Miller combat/content stream. World generation keeps its own streams.</summary>
public sealed class RoagRandom
{
    public const int Modulus = 2147483647;
    public int State { get; private set; }

    public RoagRandom(int state)
    {
        if (state <= 0 || state >= Modulus) throw new ArgumentOutOfRangeException(nameof(state));
        State = state;
    }

    public int NextRaw() => State = (int)((long)State * 48271 % Modulus);

    public int NextOneBased(int maximum)
    {
        if (maximum <= 0 || maximum >= Modulus) throw new ArgumentOutOfRangeException(nameof(maximum));
        int span = Modulus - 1, limit = span - span % maximum, value;
        do { value = NextRaw() - 1; } while (value >= limit);
        return value % maximum + 1;
    }

    public static int Derive(int seed, string label)
    {
        if (seed <= 0 || seed >= Modulus) throw new ArgumentOutOfRangeException(nameof(seed));
        ArgumentException.ThrowIfNullOrEmpty(label);
        long value = seed;
        // Lua indexes UTF-8 bytes, not UTF-16 characters. Labels must reproduce the same stream.
        foreach (byte part in Encoding.UTF8.GetBytes(label)) value = (value * 131 + part) % (Modulus - 1);
        return (int)value + 1;
    }
}
