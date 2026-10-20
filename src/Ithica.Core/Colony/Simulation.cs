using System.Runtime.InteropServices;

namespace Ithica.Core.Colony;

public sealed partial class Simulation
{
    public const int TicksPerCycle = 480;
    public static readonly Stock RescueCost = new(0, 2, 2, 0);
    public int Seed
    {
        get; private set;
    }
    public long Tick
    {
        get; private set;
    }
    public World World
    {
        get; private set;
    }
    public RandomStream Random
    {
        get; private set;
    }
    public GridRouter Router
    {
        get; private set;
    }
    public Stock Resources { get; set; } = new(150, 90, 100, 16);
    public List<Colonist> Crew { get; } = [];
    public List<Survivor> Survivors { get; } = [];
    public List<Expedition> Expeditions { get; } = [];
    public List<Notice> Log { get; } = [];
    private readonly List<Machine> _machines = [];
    private readonly List<WorkOrder> _jobs = [];
    private readonly Dictionary<int, Machine> _machinesByCell = [];
    private readonly Dictionary<int, WorkOrder> _jobsByCell = [], _jobsById = [];
    private readonly Dictionary<int, List<WorkOrder>> _workChunks = [];
    private readonly WorkOrder?[] _candidates = new WorkOrder?[8];
    private readonly List<int> _safe = [], _beds = [];
    private readonly List<PowerGroup> _groups = [];
    private int[] _network, _networkQueue;
    private long _safeTick = -1, _nextSignal = 180;
    private int _nextJob = 1, _nextMachine, _networkVersion = -1, _machineVersion, _groupMachineVersion = -1, _pathBudget, _unassigned;
    private bool _insideTick;
    public IReadOnlyList<Machine> Machines => _machines;
    public IReadOnlyList<WorkOrder> Jobs => _jobs;
    public long StormUntil
    {
        get; set;
    }
    public bool Victory
    {
        get; private set;
    }
    public bool GameOver
    {
        get; private set;
    }
    public int Cycle => (int)(Tick / TicksPerCycle) + 1;
    public float Daylight => Math.Max(0, (float)Math.Sin((Tick / (double)TicksPerCycle + .18) * 2 * Math.PI));
    public bool Storm => Tick < StormUntil;
    public float Generated
    {
        get; private set;
    }
    public float Demand
    {
        get; private set;
    }
    public float Charge
    {
        get; private set;
    }
    public float Capacity
    {
        get; private set;
    }
    public int LivingCount
    {
        get
        {
            int n = 0;
            foreach (var c in Crew)
            {
                if (c.Alive)
                {
                    n++;
                }
            }

            return n;
        }
    }
    public int RescuedCount
    {
        get
        {
            int n = 0;
            foreach (var s in Survivors)
            {
                if (s.State == ContactState.Rescued)
                {
                    n++;
                }
            }

            return n;
        }
    }
    public float Pressure
    {
        get
        {
            float sum = 0;
            int n = 0, cx = World.Width / 2, cy = World.Height / 2;
            for (int y = cy - 4; y <= cy + 4; y++)
            {
                for (int x = cx - 8; x <= cx + 8; x++)
                {
                    int i = World.Index(x, y);
                    if (World.Tiles[i] == Terrain.Floor)
                    {
                        sum += World.Oxygen[i];
                        n++;
                    }
                }
            }

            return sum / Math.Max(1, n);
        }
    }
    public float Morale
    {
        get
        {
            float sum = 0;
            int n = 0;
            foreach (var c in Crew)
            {
                if (c.Alive)
                {
                    sum += c.Morale;
                    n++;
                }
            }

            return sum / Math.Max(1, n);
        }
    }
    public static string TerrainName(Terrain t) => t switch
    {
        Terrain.Vacuum => "Lunar surface",
        Terrain.Rock => "Basalt",
        Terrain.Ice => "Water ice",
        Terrain.Ore => "Iron deposit",
        Terrain.Crystal => "Silicate crystal",
        Terrain.Floor => "Habitat deck",
        Terrain.Wall => "Pressure hull",
        _ => "Cycling airlock"
    };

    public Simulation(int seed = 42, int width = 96, int height = 64, bool setup = true, bool lunarTerrain = false)
    {
        if (width < 48 || height < 32)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Colony worlds must be at least 48 x 32 cells.");
        }

        Seed = seed;
        Random = new RandomStream(unchecked((ulong)seed));
        World = new World(width, height);
        Router = new GridRouter(World);
        _network = new int[World.Count];
        _networkQueue = new int[World.Count];
        Array.Fill(_network, -1);
        if (!setup)
        {
            return;
        }

        if (lunarTerrain) World.GenerateLunar(seed);
        else World.Generate(Random);
        int cx = width / 2, cy = height / 2;
        foreach (var (kind, x, y) in new[] { (ModuleKind.Rtg, -7, -3), (ModuleKind.Oxygen, -5, -3), (ModuleKind.Farm, 3, -3), (ModuleKind.Battery, -7, 3), (ModuleKind.Farm, 5, -3) })
        {
            AddMachine(kind, World.Index(cx + x, cy + y), kind == ModuleKind.Battery ? 220 : 0);
        }

        foreach (int dx in new[] { -4, -2, 0, 2, 4, 6 })
        {
            AddMachine(ModuleKind.Bunk, World.Index(cx + dx, cy + 3));
        }

        AddColonist("Ada", "Engineer", "Meticulous", World.Index(cx - 5, cy));
        Event("SHELTER / Ada made it. The other sleeping pods are empty.", NoticeLevel.Good);
        Event("A voice in the wasteland. Click a distress contact to send help.");
        Event("Charter: rescue 3 people, reach cycle 3 with 4 crew and 20 food, and power an uplink.");
        UpdatePower();
        SpawnSurvivor();
    }

    public void Event(string text, NoticeLevel level = NoticeLevel.Info)
    {
        if (Log.Count == 160)
        {
            Log.RemoveAt(0);
        }

        Log.Add(new(Tick, text, level));
    }
    public Machine? MachineAt(int cell) => _machinesByCell.GetValueOrDefault(cell);
    public WorkOrder? JobAt(int cell) => _jobsByCell.GetValueOrDefault(cell);
    public Survivor? SurvivorAt(int cell)
    {
        foreach (var s in Survivors)
        {
            if (s.Active && s.Cell == cell)
            {
                return s;
            }
        }

        return null;
    }
    public Colonist? CrewAt(int cell)
    {
        foreach (var c in Crew)
        {
            if (c.Alive && c.Location == ResidentLocation.Home && c.Cell == cell)
            {
                return c;
            }
        }

        return null;
    }
    public Expedition? ExpeditionFor(int survivorId)
    {
        for (int i = Expeditions.Count - 1; i >= 0; i--)
        {
            if (Expeditions[i].SurvivorId == survivorId && Expeditions[i].Active)
            {
                return Expeditions[i];
            }
        }

        return null;
    }
    public Colonist AddColonist(string name, string role, string trait, int cell)
    {
        if (!World.Valid(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell));
        }

        var c = new Colonist { Id = Crew.Count, Name = name, Role = role, Trait = trait, Cell = cell, FieldRole = role switch { "Miner" => 1, "Botanist" => 2, "Medic" => 3, _ => 0 } };
        Crew.Add(c);
        if (StoryEnabled) InitializeDisposition(c);
        return c;
    }
    public Machine AddMachine(ModuleKind kind, int cell, float energy = 0)
    {
        if (!World.Valid(cell) || _machinesByCell.ContainsKey(cell))
        {
            throw new ArgumentException("Machine cell is invalid or occupied.");
        }

        var machine = new Machine { Id = _nextMachine++, Kind = kind, Cell = cell, Energy = energy };
        _machines.Add(machine);
        _machinesByCell.Add(cell, machine);
        _machineVersion++;
        return machine;
    }
    private void RemoveMachine(Machine machine)
    {
        _machines.Remove(machine);
        _machinesByCell.Remove(machine.Cell);
        _machineVersion++;
    }
    private int Chunk(int cell) => (World.Y(cell) / 16) * ((World.Width + 15) / 16) + World.X(cell) / 16;
    private void IndexJob(WorkOrder job)
    {
        _jobs.Add(job);
        _jobsById.Add(job.Id, job);
        _jobsByCell.Add(job.Cell, job);
        int chunk = Chunk(job.Cell);
        if (!_workChunks.TryGetValue(chunk, out var list))
        {
            _workChunks[chunk] = list = [];
        }

        list.Add(job);
        if (job.Assigned is null)
        {
            _unassigned++;
        }
    }
    private void RemoveJob(WorkOrder job)
    {
        if (job.Assigned is null)
        {
            _unassigned--;
        }

        _jobs.Remove(job);
        _jobsById.Remove(job.Id);
        _jobsByCell.Remove(job.Cell);
        _workChunks[Chunk(job.Cell)].Remove(job);
    }
    public Result Designate(int x, int y, Tool tool, ModuleKind blueprint = ModuleKind.Wall)
    {
        if (!World.Inside(x, y))
        {
            return new(false, "Choose a cell inside the map.");
        }

        int i = World.Index(x, y);
        if (World.Discovered[i] == 0)
        {
            return new(false, "Explore closer before placing a work order.");
        }

        var existing = JobAt(i);
        if (tool == Tool.Cancel)
        {
            if (existing is null)
            {
                return new(false, "No work order here.");
            }

            Cancel(existing);
            return new(true, "Order cancelled. Reserved materials returned.");
        }
        if (existing is not null)
        {
            return new(false, "A work order is already queued here.");
        }

        var tile = World.Tiles[i];
        var machine = MachineAt(i);
        var job = new WorkOrder { Id = _nextJob, Cell = i, Required = 1 };
        if (tool == Tool.Survey)
        {
            if (!World.Walkable(i))
            {
                return new(false, "Survey open ground or habitat deck.");
            }

            job.Kind = WorkKind.Survey;
        }
        else if (tool == Tool.Dig)
        {
            if (!World.Solid(tile) && tile != Terrain.Airlock && machine is null)
            {
                return new(false, "Choose rock, a resource seam, hull, or a module.");
            }

            job.Kind = machine is not null || tile is Terrain.Wall or Terrain.Airlock ? WorkKind.Salvage : WorkKind.Dig;
            job.Required = tile == Terrain.Rock ? 5 : 7;
        }
        else if (tool == Tool.Build)
        {
            if (blueprint == ModuleKind.Rtg)
            {
                return new(false, "The starter RTG cannot be manufactured here.");
            }

            if (machine is not null)
            {
                return new(false, "Salvage the existing module first.");
            }

            bool allowed = blueprint switch
            {
                ModuleKind.Airlock => tile is Terrain.Vacuum or Terrain.Floor or Terrain.Wall,
                ModuleKind.Wall => tile is Terrain.Vacuum or Terrain.Floor,
                ModuleKind.Floor => tile == Terrain.Vacuum,
                _ => tile == Terrain.Floor
            };
            if (!allowed)
            {
                return new(false, "Modules need deck. Mine rock before extending the habitat.");
            }

            if (blueprint == ModuleKind.Wall && CrewAt(i) is not null)
            {
                return new(false, "A colonist is standing here.");
            }

            var bp = Blueprint.Get(blueprint);
            if (!Resources.Covers(bp.Cost))
            {
                return new(false, "Insufficient materials for " + bp.Name.ToLowerInvariant() + ".");
            }

            Resources -= bp.Cost;
            job.Kind = WorkKind.Build;
            job.Blueprint = blueprint;
            job.Required = bp.Work;
            job.Reserved = true;
        }
        else
        {
            return new(false, "Select a work tool first.");
        }

        IndexJob(job);
        _nextJob++;
        return new(true, "Work queued. A colonist will find a route.");
    }
    public void Cancel(WorkOrder job)
    {
        if (!_jobsById.ContainsKey(job.Id))
        {
            return;
        }

        if (job.Reserved)
        {
            Resources += job.Kind == WorkKind.Maintain ? job.MaintenanceCost : Blueprint.Get(job.Blueprint).Cost;
        }

        if (job.Assigned is int id)
        {
            Release(Crew.Single(c => c.Id == id));
        }

        RemoveJob(job);
    }
    private void Release(Colonist c)
    {
        if (c.Job is int id && _jobsById.TryGetValue(id, out var job) && job.Assigned is not null)
        {
            job.Assigned = null;
            _unassigned++;
        }
        c.Job = null;
        c.Path.Clear();
        c.PathCursor = 0;
    }
    private bool Route(Colonist c, ReadOnlySpan<int> targets)
    {
        if (_insideTick && _pathBudget <= 0)
        {
            return false;
        }

        if (_insideTick)
        {
            _pathBudget--;
        }

        c.PathCursor = 0;
        return Router.Find(c.Cell, targets, c.Path);
    }
    private int JobTargets(WorkOrder job, Span<int> targets)
    {
        int count = World.Neighbors(job.Cell, targets), result = 0;
        for (int n = 0; n < count; n++)
        {
            if (World.Walkable(targets[n]))
            {
                targets[result++] = targets[n];
            }
        }

        return result;
    }
    private bool Move(Colonist c)
    {
        if (c.PathCursor >= c.Path.Count)
        {
            return false;
        }

        int next = c.Path[c.PathCursor];
        if (!World.Walkable(next) || World.Fire[next] > .5f || World.Distance(c.Cell, next) != 1)
        {
            c.Path.Clear();
            c.PathCursor = 0;
            return false;
        }
        c.Cell = next;
        c.PathCursor++;
        World.Reveal(next);
        return true;
    }
    private void RefreshSafety()
    {
        if (_safeTick == Tick)
        {
            return;
        }

        _safeTick = Tick;
        _safe.Clear();
        _beds.Clear();
        foreach (int i in World.Floors)
        {
            if (World.Breathable(i, .5f))
            {
                _safe.Add(i);
            }
        }

        foreach (var m in _machines)
        {
            if (m.Kind == ModuleKind.Bunk && World.Breathable(m.Cell, .35f))
            {
                _beds.Add(m.Cell);
            }
        }
    }
    private bool RouteToSafety(Colonist c, bool bed = false)
    {
        RefreshSafety();
        return Route(c, CollectionsMarshal.AsSpan(bed && _beds.Count > 0 ? _beds : _safe));
    }

    private sealed class PowerGroup
    {
        public readonly List<Machine> Members = [], Consumers = [], Banks = [];
    }
    public int NetworkAt(int cell)
    {
        UpdatePower();
        return _network[cell];
    }
    public int CachedNetworkAt(int cell) => _network[cell];
    public void UpdatePower(bool advance = false)
    {
        bool rebuild = _networkVersion != World.TopologyVersion;
        if (rebuild)
        {
            Array.Fill(_network, -1);
            int group = 0;
            Span<int> neighbors = stackalloc int[4];
            for (int root = 0; root < World.Count; root++)
            {
                if (_network[root] >= 0 || World.Tiles[root] is not (Terrain.Floor or Terrain.Airlock))
                {
                    continue;
                }

                int head = 0, tail = 0;
                _networkQueue[tail++] = root;
                _network[root] = group;
                while (head < tail)
                {
                    int cell = _networkQueue[head++], count = World.Neighbors(cell, neighbors);
                    for (int n = 0; n < count; n++)
                    {
                        int next = neighbors[n];
                        if (_network[next] < 0 && World.Tiles[next] is Terrain.Floor or Terrain.Airlock)
                        {
                            _network[next] = group;
                            _networkQueue[tail++] = next;
                        }
                    }
                }
                group++;
            }
            _networkVersion = World.TopologyVersion;
        }
        if (rebuild || _groupMachineVersion != _machineVersion)
        {
            _groups.Clear();
            var byId = new Dictionary<int, PowerGroup>();
            foreach (var m in _machines)
            {
                m.Powered = false;
                int group = _network[m.Cell];
                if (group < 0)
                {
                    continue;
                }

                if (!byId.TryGetValue(group, out var power))
                {
                    power = new();
                    byId.Add(group, power);
                    _groups.Add(power);
                }
                power.Members.Add(m);
                if (Blueprint.Get(m.Kind).Power > 0)
                {
                    power.Consumers.Add(m);
                }

                if (m.Kind == ModuleKind.Battery)
                {
                    power.Banks.Add(m);
                }
            }
            foreach (var group in _groups)
            {
                group.Consumers.Sort((a, b) => { int priority = Priority(a.Kind).CompareTo(Priority(b.Kind)); return priority != 0 ? priority : a.Id.CompareTo(b.Id); });
            }

            _groupMachineVersion = _machineVersion;
        }
        Generated = Demand = Capacity = Charge = 0;
        foreach (var group in _groups)
        {
            float supply = 0, stored = 0, used = 0;
            int banks = 0;
            foreach (var m in group.Members)
            {
                m.Powered = Operational(m) && m.Kind is (ModuleKind.Rtg or ModuleKind.Solar or ModuleKind.Battery or ModuleKind.Bunk);
                if (!Operational(m)) continue;
                if (m.Kind == ModuleKind.Rtg)
                {
                    supply += 12 * Efficiency(m);
                }
                else if (m.Kind == ModuleKind.Solar)
                {
                    supply += 12 * Efficiency(m) * Daylight * (Storm ? .15f : 1);
                }
                else if (m.Kind == ModuleKind.Battery)
                {
                    stored += m.Energy;
                    banks++;
                }
            }
            float available = supply + Math.Min(16, stored / .05f);
            foreach (var m in group.Consumers)
            {
                if (!Operational(m)) { m.Powered = false; continue; }
                float draw = Blueprint.Get(m.Kind).Power;
                Demand += draw;
                m.Powered = used + draw <= available + .00001f;
                if (m.Powered)
                {
                    used += draw;
                }
            }
            if (advance && banks > 0)
            {
                float energy = Math.Clamp(stored + Math.Min(16, supply - used) * .05f, 0, 240 * banks);
                foreach (var m in group.Banks)
                {
                    if (Operational(m)) m.Energy = energy / banks;
                }
            }
            Generated += supply;
        }
        foreach (var m in _machines)
        {
            if (m.Kind == ModuleKind.Battery && Operational(m))
            {
                Capacity += 240;
                Charge += m.Energy;
            }
        }
    }
    private static int Priority(ModuleKind kind) => kind switch { ModuleKind.Oxygen => 0, ModuleKind.Farm => 1, ModuleKind.Medbay => 2, _ => 3 };
    private void Production()
    {
        for (int n = _machines.Count - 1; n >= 0; n--)
        {
            var m = _machines[n];
            int i = m.Cell;
            if (World.Fire[i] > .2f)
            {
                m.Health -= World.Fire[i] * .4f;
            }

            if (m.Health <= 0)
            {
                RemoveMachine(m);
                Event(Blueprint.Get(m.Kind).Name + " destroyed by fire.", NoticeLevel.Danger);
                continue;
            }
            if (!m.Powered)
            {
                continue;
            }

            if (m.Kind == ModuleKind.Rtg)
            {
                World.Heat[i] += .5f;
            }
            else if (m.Kind == ModuleKind.Oxygen && Resources.Water >= .014f)
            {
                Resources -= new Stock(0, .014f, 0, 0);
                World.Oxygen[i] = Math.Min(1, World.Oxygen[i] + .24f * Efficiency(m));
                World.Heat[i] += Math.Max(0, (26 - World.Heat[i]) * .12f);
            }
            else if (m.Kind == ModuleKind.Farm && World.Oxygen[i] >= .38f && World.Heat[i] is >= 0 and < 46 && Resources.Water >= .008f)
            {
                bool tended = false;
                foreach (var c in Crew)
                {
                    if (c.Alive && c.Location == ResidentLocation.Home && c.Role == "Botanist" && World.Distance(c.Cell, i) <= 2)
                    {
                        tended = true;
                        break;
                    }
                }

                m.Progress += (tended ? 1.8f : 1) * Efficiency(m);
                Resources -= new Stock(0, .008f, 0, 0);
                if (m.Progress >= 160)
                {
                    m.Progress -= 160;
                    Resources += new Stock(0, 0, 3, 0);
                    Event("Hydroponics harvest: +3 food.", NoticeLevel.Good);
                }
            }
        }
    }
    public void Step(int count = 1)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        try
        {
            _insideTick = true;
            for (int step = 0; step < count && !GameOver; step++)
            {
                Tick++;
                _pathBudget = 64;
                UpdatePower(true);
                Production();
                SurvivorTick();
                int crewCount = Crew.Count;
                int start = crewCount > 0 ? (int)((Tick * 64) % crewCount) : 0;
                for (int offset = 0; offset < crewCount; offset++)
                {
                    int i = (start + offset) % crewCount;
                    if (Crew[i].Alive)
                    {
                        CrewTick(Crew[i]);
                    }
                }
                UpdatePower();
                World.Environment(Random, Daylight);
                StoryTick();
                if (StoryEnabled) UpdatePower();
                Events();
            }
        }
        finally { _insideTick = false; }
    }
    public void DemoOrders()
    {
        foreach (var s in Survivors)
        {
            if (s.State == ContactState.Stranded && ExpeditionFor(s.Id) is null)
            {
                Dispatch(s.Id);
                break;
            }
        }

        int cx = World.Width / 2, cy = World.Height / 2;
        foreach (var (ox, oy) in new[] { (-7, -8), (5, 8), (12, -5) })
        {
            for (int dy = 0; dy < 3; dy++)
            {
                for (int dx = 0; dx < 3; dx++)
                {
                    Designate(cx + ox + dx, cy + oy + dy, Tool.Dig);
                }
            }
        }

        Designate(cx + 6, cy + 1, Tool.Build, ModuleKind.Solar);
        Designate(cx + 7, cy + 1, Tool.Build, ModuleKind.Solar);
    }
}
