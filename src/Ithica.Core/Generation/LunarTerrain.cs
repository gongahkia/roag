using Ithica.Core.Expeditions;
using Ithica.Core.Colony;

namespace Ithica.Core.Generation;

/// <summary>Shared lunar geology: warped multi-octave gradient fields, crater bowls/rims and separate deposit fields.</summary>
public sealed class LunarTerrain
{
    private readonly GradientNoise _relief, _warpX, _warpY, _ore, _ice, _crystal;
    public LunarProfile Profile { get; }
    public IReadOnlyList<CraterPlacement> Craters { get; }
    public LunarTerrain(int seed, int width, int height)
    {
        Profile = LunarRules.Default.Profile(seed);
        GradientNoise Noise(uint salt) => new(unchecked((int)GenerationSeeds.Mix(unchecked((uint)seed), salt)));
        _relief = Noise(302); _warpX = Noise(303); _warpY = Noise(304); _ore = Noise(305); _ice = Noise(306); _crystal = Noise(307);
        var random = new LunarRandom(seed, 308);
        var craters = new List<CraterPlacement>();
        int scale = Math.Max(1, width * height / (54 * 36));
        for (int n = 0; n < Math.Min(80, Profile.Craters * scale); n++)
            craters.Add(new(random.Next(3, width - 3), random.Next(3, height - 3), random.Next(3, Math.Max(5, Math.Min(width, height) / 4))));
        Craters = craters;
    }
    public (double Height, double Rim) At(int x, int y)
    {
        var rules = LunarRules.Default;
        double wx = x + _warpX.Sample(x / 23.0, y / 23.0) * Profile.Warp;
        double wy = y + _warpY.Sample(x / 23.0, y / 23.0) * Profile.Warp;
        double elevation = _relief.Fractal(wx / rules.NoiseScale, wy / rules.NoiseScale, rules.NoiseOctaves), rim = 0;
        foreach (var crater in Craters)
        {
            double dx = x - crater.X, dy = y - crater.Y, distance = Math.Sqrt(dx * dx + dy * dy) / crater.Radius;
            if (distance < .8) elevation -= .45 * (1 - distance / .8);
            double ring = Math.Max(0, 1 - Math.Abs(distance - .94) / .20);
            elevation += ring * .42; rim = Math.Max(rim, ring);
        }
        return (elevation, rim);
    }
    public Terrain Deposit(int x, int y, double elevation, bool rock)
    {
        // Elongated, independently seeded fields create connected seams instead of white-noise scatter.
        double ore = _ore.Fractal(x / 17.0, y / 5.0, 3);
        double ice = _ice.Fractal(x / 10.0, y / 13.0, 3);
        double crystal = 1 - Math.Abs(_crystal.Fractal(x / 8.0, y / 5.0, 3));
        if (ice > (Profile.Id == "ice-basin" ? .11 : .26) && elevation < .14) return Terrain.Ice;
        if (rock && ore > .17) return Terrain.Ore;
        if (rock && crystal > .965 && elevation > .15) return Terrain.Crystal;
        return rock ? Terrain.Rock : Terrain.Vacuum;
    }
    public void Fill(World world)
    {
        for (int y = 0; y < world.Height; y++)
            for (int x = 0; x < world.Width; x++)
            {
                double elevation = At(x, y).Height;
                world.Tiles[world.Index(x, y)] = Deposit(x, y, elevation, elevation > Profile.RockThreshold);
            }
        world.GenerationVersion = LunarRules.Default.Version;
        world.GeologyProfile = Profile.Name;
    }
}
