using System.Numerics;

namespace Ithica.Core.Expeditions;

// Socket order and bit order: north, east, south, west. These are authored game
// connections, unrelated to numbered cells or inferred bitmasks in the artwork.
public sealed record WfcTile(string Id, int North, int East, int South, int West, int Weight = 1)
{
    public int Socket(int direction) => direction switch { 0 => North, 1 => East, 2 => South, _ => West };
}
public sealed record WfcResult(int[] Tiles, int Attempts, int Observations, int Propagations);

public sealed class WaveFunctionCollapse
{
    private readonly WfcTile[] _tiles;
    private readonly ulong[,] _compatible;
    private static readonly Cell[] Directions = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)];
    public WaveFunctionCollapse(IEnumerable<WfcTile> tiles)
    {
        _tiles = tiles.ToArray();
        if (_tiles.Length is < 1 or > 63 || _tiles.Any(t => t.Weight <= 0))
            throw new ArgumentException("WFC needs 1..63 tiles with positive weights.");
        _compatible = new ulong[_tiles.Length, 4];
        for (int t = 0; t < _tiles.Length; t++)
            for (int d = 0; d < 4; d++)
                for (int n = 0; n < _tiles.Length; n++)
                    if (_tiles[t].Socket(d) == _tiles[n].Socket((d + 2) % 4)) _compatible[t, d] |= 1UL << n;
    }
    public WfcResult? Solve(int width, int height, Func<int, WfcTile, bool> allowed, uint seed, int maxAttempts = 8)
    {
        if (width <= 0 || height <= 0 || width * (long)height > 100_000 || maxAttempts < 0)
            throw new ArgumentException("Invalid WFC bounds.");
        var initial = new ulong[width * height];
        for (int i = 0; i < initial.Length; i++)
            for (int t = 0; t < _tiles.Length; t++)
                if (allowed(i, _tiles[t])) initial[i] |= 1UL << t;
        if (initial.Contains(0UL)) return null;
        int observations = 0, propagations = 0;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var random = new RandomStream(GenerationSeeds.Mix(seed, (uint)attempt + 1));
            var domains = (ulong[])initial.Clone();
            var queue = new Queue<int>(Enumerable.Range(0, domains.Length));
            bool Propagate()
            {
                while (queue.TryDequeue(out int i))
                {
                    propagations++;
                    for (int d = 0; d < 4; d++)
                    {
                        int x = i % width + Directions[d].X, y = i / width + Directions[d].Y;
                        if (x < 0 || y < 0 || x >= width || y >= height) continue;
                        ulong support = 0, bits = domains[i];
                        while (bits != 0)
                        {
                            int t = BitOperations.TrailingZeroCount(bits);
                            bits &= bits - 1;
                            support |= _compatible[t, d];
                        }
                        int n = y * width + x;
                        ulong reduced = domains[n] & support;
                        if (reduced == domains[n]) continue;
                        if (reduced == 0) return false;
                        domains[n] = reduced;
                        queue.Enqueue(n);
                    }
                }
                return true;
            }
            bool valid = Propagate();
            while (valid)
            {
                int chosen = -1, ties = 0;
                double lowest = double.PositiveInfinity;
                for (int i = 0; i < domains.Length; i++)
                {
                    if (BitOperations.PopCount(domains[i]) <= 1) continue;
                    int total = 0;
                    double logs = 0;
                    for (int t = 0; t < _tiles.Length; t++)
                        if ((domains[i] & (1UL << t)) != 0)
                        {
                            int w = _tiles[t].Weight;
                            total += w;
                            logs += w * Math.Log(w);
                        }
                    double entropy = Math.Log(total) - logs / total;
                    if (entropy < lowest - 1e-9) { lowest = entropy; chosen = i; ties = 1; }
                    else if (Math.Abs(entropy - lowest) < 1e-9 && random.Next(++ties) == 0) chosen = i;
                }
                if (chosen < 0) return new(domains.Select(b => BitOperations.TrailingZeroCount(b)).ToArray(), attempt + 1, observations, propagations);
                int sum = 0;
                for (int t = 0; t < _tiles.Length; t++) if ((domains[chosen] & (1UL << t)) != 0) sum += _tiles[t].Weight;
                int pick = random.Next(sum);
                for (int t = 0; t < _tiles.Length; t++)
                    if ((domains[chosen] & (1UL << t)) != 0 && (pick -= _tiles[t].Weight) < 0)
                    {
                        domains[chosen] = 1UL << t;
                        break;
                    }
                observations++;
                queue.Enqueue(chosen);
                valid = Propagate();
            }
        }
        return null;
    }
}

public static class GenerationSeeds
{
    public static uint Mix(uint seed, uint salt)
    {
        uint x = unchecked(seed ^ salt * 0x9e3779b9U);
        x ^= x >> 16;
        x = unchecked(x * 0x7feb352dU);
        x ^= x >> 15;
        x = unchecked(x * 0x846ca68bU);
        uint result = x ^ (x >> 16);
        return result == 0 ? 1U : result;
    }
    public static RandomStream Stream(int seed, uint salt) => new(Mix(unchecked((uint)seed), salt));
}
