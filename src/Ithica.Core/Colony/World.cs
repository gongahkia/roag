namespace Ithica.Core.Colony;

/// <summary>Contiguous cell fields; reusable diffusion buffers; only habitat cells need atmosphere updates.</summary>
public sealed class World
{
    public int Width
    {
        get;
    }
    public int Height
    {
        get;
    }
    public int GenerationVersion { get; internal set; } = 1;
    public string GeologyProfile { get; internal set; } = "Legacy geology";
    public int Count => Width * Height;
    public int Center => Index(Width / 2, Height / 2);
    public Terrain[] Tiles
    {
        get;
    }
    public float[] Oxygen
    {
        get;
    }
    public float[] Heat
    {
        get;
    }
    public float[] Fire
    {
        get;
    }
    public byte[] Discovered
    {
        get;
    }
    public int TopologyVersion
    {
        get; private set;
    }
    private readonly float[] _oxygenNext, _heatNext, _fireNext;
    private readonly List<int> _floors = [];
    private int _floorVersion = -1;
    public float Ambient { get; private set; } = -35;
    public IReadOnlyList<int> Floors
    {
        get
        {
            RefreshFloors();
            return _floors;
        }
    }

    public World(int width, int height)
    {
        if (width < 7 || height < 7 || width > 2048 || height > 2048)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        Width = width;
        Height = height;
        Tiles = new Terrain[Count];
        Oxygen = new float[Count];
        Heat = new float[Count];
        Fire = new float[Count];
        Discovered = new byte[Count];
        _oxygenNext = new float[Count];
        _heatNext = new float[Count];
        _fireNext = new float[Count];
        Array.Fill(Heat, -35);
    }
    public int Index(int x, int y) => y * Width + x;
    public int X(int cell) => cell % Width;
    public int Y(int cell) => cell / Width;
    public bool Inside(int x, int y) => (uint)x < Width && (uint)y < Height;
    public bool Valid(int cell) => (uint)cell < Count;
    public static bool Solid(Terrain tile) => tile is Terrain.Rock or Terrain.Ice or Terrain.Ore or Terrain.Crystal or Terrain.Wall;
    public bool Walkable(int cell) => Valid(cell) && !Solid(Tiles[cell]);
    public bool Breathable(int cell, float threshold = .32f) => Tiles[cell] == Terrain.Floor && Oxygen[cell] > threshold && Fire[cell] < .15f;
    public float Temperature(int cell) => Tiles[cell] == Terrain.Vacuum ? Ambient : Heat[cell];
    public int Distance(int a, int b) => Math.Abs(X(a) - X(b)) + Math.Abs(Y(a) - Y(b));
    public int Neighbors(int cell, Span<int> output)
    {
        int count = 0, x = X(cell), y = Y(cell);
        if (y > 0)
        {
            output[count++] = cell - Width;
        }

        if (x > 0)
        {
            output[count++] = cell - 1;
        }

        if (x < Width - 1)
        {
            output[count++] = cell + 1;
        }

        if (y < Height - 1)
        {
            output[count++] = cell + Width;
        }

        return count;
    }
    public void SetTerrain(int cell, Terrain tile)
    {
        if (!Valid(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell));
        }

        if (Tiles[cell] == tile)
        {
            return;
        }

        Tiles[cell] = tile;
        TopologyVersion++;
        if (tile != Terrain.Floor)
        {
            Oxygen[cell] = Fire[cell] = 0;
        }
    }
    public void InvalidateTopology() => TopologyVersion++;
    private void RefreshFloors()
    {
        if (_floorVersion == TopologyVersion)
        {
            return;
        }

        _floors.Clear();
        for (int i = 0; i < Count; i++)
        {
            if (Tiles[i] == Terrain.Floor)
            {
                _floors.Add(i);
            }
        }

        _floorVersion = TopologyVersion;
    }
    public void Reveal(int cell, int radius = 5)
    {
        int cx = X(cell), cy = Y(cell);
        for (int y = Math.Max(0, cy - radius); y <= Math.Min(Height - 1, cy + radius); y++)
        {
            for (int x = Math.Max(0, cx - radius); x <= Math.Min(Width - 1, cx + radius); x++)
            {
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                {
                    Discovered[Index(x, y)] = 1;
                }
            }
        }
    }

    public void Generate(RandomStream random)
    {
        var mask = new bool[Count];
        var next = new bool[Count];
        for (int i = 0; i < Count; i++)
        {
            mask[i] = random.NextDouble() < .44;
        }

        for (int pass = 0; pass < 5; pass++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int n = 0, index = Index(x, y);
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if ((dx != 0 || dy != 0) && (!Inside(x + dx, y + dy) || mask[Index(x + dx, y + dy)]))
                            {
                                n++;
                            }
                        }
                    }

                    next[index] = n >= (mask[index] ? 4 : 5);
                }
            }

            (mask, next) = (next, mask);
        }
        for (int i = 0; i < Count; i++)
        {
            Tiles[i] = mask[i] ? Terrain.Rock : Terrain.Vacuum;
        }

        int veins = Math.Max(65, Count / 72);
        for (int v = 0; v < veins; v++)
        {
            int x = random.Next(2, Width - 2), y = random.Next(2, Height - 2), type = random.Next(0, 11);
            Terrain resource = type < 4 ? Terrain.Ice : type < 9 ? Terrain.Ore : Terrain.Crystal;
            int length = random.Next(4, 14);
            for (int step = 0; step < length; step++)
            {
                Tiles[Index(x, y)] = resource;
                switch (random.Next(0, 4))
                {
                    case 0:
                        x++;
                        break;
                    case 1:
                        x--;
                        break;
                    case 2:
                        y++;
                        break;
                    default:
                        y--;
                        break;
                }
                x = Math.Clamp(x, 1, Width - 2);
                y = Math.Clamp(y, 1, Height - 2);
            }
        }
        InitializeShelter();
    }

    public void GenerateLunar(int seed)
    {
        new Generation.LunarTerrain(seed, Width, Height).Fill(this);
        InitializeShelter();
    }

    private void InitializeShelter()
    {
        int cx = Width / 2, cy = Height / 2;
        for (int y = cy - 9; y <= cy + 9; y++)
        {
            for (int x = cx - 13; x <= cx + 13; x++)
            {
                Tiles[Index(x, y)] = Terrain.Vacuum;
            }
        }

        for (int y = cy - 5; y <= cy + 5; y++)
        {
            for (int x = cx - 9; x <= cx + 9; x++)
            {
                int i = Index(x, y);
                bool edge = Math.Abs(x - cx) == 9 || Math.Abs(y - cy) == 5;
                Tiles[i] = edge ? Terrain.Wall : Terrain.Floor;
                Oxygen[i] = edge ? 0 : .92f;
                Heat[i] = edge ? -10 : 19;
            }
        }

        Tiles[Index(cx + 9, cy)] = Terrain.Airlock;
        foreach (var (ox, oy, type) in new[] { (-7, -8, Terrain.Ice), (5, 8, Terrain.Ore), (12, -5, Terrain.Crystal) })
        {
            for (int dy = 0; dy < 3; dy++)
            {
                for (int dx = 0; dx < 3; dx++)
                {
                    Tiles[Index(cx + ox + dx, cy + oy + dy)] = type;
                }
            }
        }

        InvalidateTopology();
        Reveal(Center, 19);
    }

    /// <summary>Double-buffered local diffusion. Exterior vacuum is a zero-pressure, ambient-temperature boundary.</summary>
    public void Environment(RandomStream random, float daylight)
    {
        RefreshFloors();
        Ambient = -55 + 85 * daylight;
        foreach (int i in _floors)
        {
            _oxygenNext[i] = Oxygen[i];
            _heatNext[i] = Heat[i];
            _fireNext[i] = Fire[i];
        }
        Span<int> neighbors = stackalloc int[4];
        foreach (int i in _floors)
        {
            int count = Neighbors(i, neighbors);
            for (int n = 0; n < count; n++)
            {
                int j = neighbors[n];
                if (Tiles[j] == Terrain.Floor && j > i)
                {
                    float air = (Oxygen[i] - Oxygen[j]) * .12f, heat = (Heat[i] - Heat[j]) * .05f;
                    _oxygenNext[i] -= air;
                    _oxygenNext[j] += air;
                    _heatNext[i] -= heat;
                    _heatNext[j] += heat;
                    if (Fire[i] > .25f && Oxygen[j] > .18f && random.NextDouble() < Fire[i] * .018)
                    {
                        _fireNext[j] = Math.Max(_fireNext[j], .35f);
                    }

                    if (Fire[j] > .25f && Oxygen[i] > .18f && random.NextDouble() < Fire[j] * .018)
                    {
                        _fireNext[i] = Math.Max(_fireNext[i], .35f);
                    }
                }
                else if (Tiles[j] == Terrain.Vacuum)
                {
                    _oxygenNext[i] -= Oxygen[i] * .12f;
                    _heatNext[i] += (Ambient - Heat[i]) * .05f;
                }
            }
            _heatNext[i] += (17 - Heat[i]) * .002f;
            if (Fire[i] > 0)
            {
                _oxygenNext[i] -= Fire[i] * .06f;
                _heatNext[i] += Fire[i] * 5;
                _fireNext[i] = Oxygen[i] > .14f ? Math.Min(1, Fire[i] + .035f) : Math.Max(0, Fire[i] - .16f);
            }
        }
        foreach (int i in _floors)
        {
            Oxygen[i] = Math.Clamp(_oxygenNext[i], 0, 1);
            Heat[i] = Math.Clamp(_heatNext[i], -100, 180);
            Fire[i] = Math.Clamp(_fireNext[i], 0, 1);
        }
    }
}
