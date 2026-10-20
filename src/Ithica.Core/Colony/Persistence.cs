using System.Text.Json;

namespace Ithica.Core.Colony;

public sealed partial class Simulation
{
    public static string DefaultSave => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".regolith", "colony-csharp.json");
    private static readonly JsonSerializerOptions SaveOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private sealed class SaveData
    {
        public string Format { get; set; } = "regolith-csharp";
        public int Version { get; set; } = 1;
        public int Seed
        {
            get; set;
        }
        public int Width
        {
            get; set;
        }
        public int Height
        {
            get; set;
        }
        public long Tick
        {
            get; set;
        }
        public ulong RandomState
        {
            get; set;
        }
        public Stock Resources
        {
            get; set;
        }
        public long StormUntil
        {
            get; set;
        }
        public long NextSignal
        {
            get; set;
        }
        public bool Victory
        {
            get; set;
        }
        public bool GameOver
        {
            get; set;
        }
        public bool IncidentsEnabled
        {
            get; set;
        }
        public Terrain[] Tiles { get; set; } = [];
        public float[] Oxygen { get; set; } = [];
        public float[] Heat { get; set; } = [];
        public float[] Fire { get; set; } = [];
        public byte[] Discovered { get; set; } = [];
        public List<Colonist> Crew { get; set; } = [];
        public List<Machine> Machines { get; set; } = [];
        public List<WorkOrder> Jobs { get; set; } = [];
        public List<Survivor> Survivors { get; set; } = [];
        public List<Expedition> Expeditions { get; set; } = [];
        public List<Notice> Log { get; set; } = [];
    }
    private SaveData Snapshot() => new()
    {
        Seed = Seed,
        Width = World.Width,
        Height = World.Height,
        Tick = Tick,
        RandomState = Random.State,
        Resources = Resources,
        StormUntil = StormUntil,
        NextSignal = _nextSignal,
        Victory = Victory,
        GameOver = GameOver,
        IncidentsEnabled = IncidentsEnabled,
        Tiles = World.Tiles,
        Oxygen = World.Oxygen,
        Heat = World.Heat,
        Fire = World.Fire,
        Discovered = World.Discovered,
        Crew = Crew,
        Machines = _machines,
        Jobs = _jobs,
        Survivors = Survivors,
        Expeditions = Expeditions,
        Log = Log
    };
    public string StateJson() => JsonSerializer.Serialize(Snapshot(), SaveOptions);
    public void Save(string? path = null)
    {
        path = Path.GetFullPath(path ?? DefaultSave);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, Snapshot(), SaveOptions);
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
    public static Simulation Load(string? path = null) => FromJson(File.ReadAllText(path ?? DefaultSave));
    public static Simulation FromJson(string json)
    {
        SaveData data = JsonSerializer.Deserialize<SaveData>(json, SaveOptions) ?? throw new InvalidDataException("Empty save file.");
        if (data.Format != "regolith-csharp" || data.Version != 1)
        {
            throw new InvalidDataException("Unsupported save format. The archived Python prototype uses a separate save file.");
        }

        if (data.Width < 48 || data.Height < 32 || data.Width > 2048 || data.Height > 2048 || data.Tick < 0 || !data.Resources.Valid)
        {
            throw new InvalidDataException("Invalid colony dimensions or resources.");
        }

        int size = checked(data.Width * data.Height);
        if (data.Tiles.Length != size || data.Oxygen.Length != size || data.Heat.Length != size || data.Fire.Length != size || data.Discovered.Length != size)
        {
            throw new InvalidDataException("Incomplete world arrays.");
        }

        for (int i = 0; i < size; i++)
        {
            if (!Enum.IsDefined(data.Tiles[i]) || !float.IsFinite(data.Oxygen[i]) || !float.IsFinite(data.Heat[i]) || !float.IsFinite(data.Fire[i])
                || data.Oxygen[i] is < 0 or > 1 || data.Fire[i] is < 0 or > 1)
            {
                throw new InvalidDataException("Invalid cell state.");
            }
        }

        static bool Unit(float value) => float.IsFinite(value) && value >= 0 && value <= 100;
        bool Cell(int i) => (uint)i < size;
        for (int i = 0; i < data.Crew.Count; i++)
        {
            var c = data.Crew[i];
            if (c.Id != i || !Cell(c.Cell) || !Unit(c.Health) || !Unit(c.Suit) || !Unit(c.Energy) || !Unit(c.Hunger) || !Unit(c.Morale)
                || !Enum.IsDefined(c.Location) || c.FieldRole is < 0 or > 3 || c.Charge is < 0 or > 40 || c.Level is < 1 or > 100 || c.Experience < 0
                || !float.IsFinite(c.SuitEfficiency) || c.SuitEfficiency is < .1f or > 1
                || c.Location == ResidentLocation.Field && (c.Job is not null || c.Expedition is not null || string.IsNullOrWhiteSpace(c.SiteId))
                || c.PathCursor < 0 || c.PathCursor > c.Path.Count || c.Path.Any(p => !Cell(p)))
            {
                throw new InvalidDataException("Invalid crew state.");
            }
        }
        for (int i = 0; i < data.Survivors.Count; i++)
        {
            var s = data.Survivors[i];
            if (s.Id != i || !Cell(s.Cell) || !Unit(s.Health) || !Unit(s.Suit) || !Enum.IsDefined(s.State)
                || s.RecruitedResidentId is int resident && ((uint)resident >= data.Crew.Count || s.State != ContactState.Rescued || data.Crew[resident].Name != s.Name))
            {
                throw new InvalidDataException("Invalid survivor state.");
            }
        }
        for (int i = 0; i < data.Expeditions.Count; i++)
        {
            var e = data.Expeditions[i];
            if (e.Id != i || (uint)e.CrewId >= data.Crew.Count || (uint)e.SurvivorId >= data.Survivors.Count || !Enum.IsDefined(e.Phase))
            {
                throw new InvalidDataException("Invalid rescue state.");
            }
        }
        var jobIds = new HashSet<int>();
        var jobCells = new HashSet<int>();
        var machineCells = new HashSet<int>();
        var machineIds = new HashSet<int>();
        foreach (var j in data.Jobs)
        {
            if (j.Id < 1 || !jobIds.Add(j.Id) || !jobCells.Add(j.Cell) || !Cell(j.Cell) || !Enum.IsDefined(j.Kind) || !Enum.IsDefined(j.Blueprint)
                || !float.IsFinite(j.Progress) || j.Progress < 0 || !float.IsFinite(j.Required) || j.Required <= 0 || (j.Assigned is int id && (uint)id >= data.Crew.Count))
            {
                throw new InvalidDataException("Invalid work orders.");
            }
        }

        foreach (var m in data.Machines)
        {
            if (m.Id < 0 || !machineIds.Add(m.Id) || !machineCells.Add(m.Cell) || !Cell(m.Cell) || !Enum.IsDefined(m.Kind) || !float.IsFinite(m.Health) || !float.IsFinite(m.Energy)
                || m.Energy < 0 || m.Energy > 240 || !float.IsFinite(m.Progress))
            {
                throw new InvalidDataException("Invalid machine state.");
            }
        }

        foreach (var c in data.Crew)
        {
            if (c.Job is int job && !jobIds.Contains(job))
            {
                throw new InvalidDataException("Missing crew work order.");
            }

            if (c.Expedition is int e && ((uint)e >= data.Expeditions.Count || data.Expeditions[e].CrewId != c.Id || !data.Expeditions[e].Active))
            {
                throw new InvalidDataException("Invalid crew rescue assignment.");
            }
        }
        var sim = new Simulation(data.Seed, data.Width, data.Height, false)
        {
            Tick = data.Tick,
            Resources = data.Resources,
            StormUntil = data.StormUntil,
            _nextSignal = data.NextSignal,
            Victory = data.Victory,
            GameOver = data.GameOver,
            IncidentsEnabled = data.IncidentsEnabled
        };
        data.Tiles.CopyTo(sim.World.Tiles, 0);
        data.Oxygen.CopyTo(sim.World.Oxygen, 0);
        data.Heat.CopyTo(sim.World.Heat, 0);
        data.Fire.CopyTo(sim.World.Fire, 0);
        data.Discovered.CopyTo(sim.World.Discovered, 0);
        sim.World.InvalidateTopology();
        sim.Random.State = data.RandomState;
        sim.Crew.AddRange(data.Crew);
        sim.Survivors.AddRange(data.Survivors);
        sim.Expeditions.AddRange(data.Expeditions);
        foreach (var m in data.Machines)
        {
            sim._machines.Add(m);
            sim._machinesByCell.Add(m.Cell, m);
            sim._nextMachine = Math.Max(sim._nextMachine, m.Id + 1);
        }
        foreach (var j in data.Jobs)
        {
            sim.IndexJob(j);
            sim._nextJob = Math.Max(sim._nextJob, j.Id + 1);
        }
        sim.Log.AddRange(data.Log.TakeLast(160));
        sim._machineVersion++;
        sim.UpdatePower();
        return sim;
    }
    public object Summary() => new
    {
        Seed,
        Tick,
        Cycle,
        Living = LivingCount,
        Pressure = Math.Round(Pressure, 3),
        Resources,
        PowerKw = Generated,
        DemandKw = Demand,
        Battery = Charge,
        Jobs = _jobs.Count,
        Rescued = RescuedCount,
        Distress = Survivors.Where(s => s.Active).Select(s => new { s.Id, s.Name, s.Role, X = World.X(s.Cell), Y = World.Y(s.Cell), s.State, s.Health, s.Suit, s.TimeLeft }),
        Charter = Victory,
        GameOver,
        Events = Log.TakeLast(6)
    };
}
