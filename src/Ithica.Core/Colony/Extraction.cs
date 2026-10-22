namespace Ithica.Core.Colony;

public sealed class DeepDeposit
{
    public int Cell { get; set; }
    public bool Surveyed { get; set; }
    public int Depth { get; set; } = 1;
    public int Remaining { get; set; } = 24;
    public int Extracted { get; set; }
}

public sealed class ExtractionState
{
    public int Version { get; set; } = 1;
    public List<DeepDeposit> Deposits { get; set; } = [];
}

public sealed partial class Simulation
{
    public ExtractionState? Extraction { get; private set; }
    public static readonly Stock ComponentRecipe = new(4, 0, 0, 2, 0, 4);
    public static readonly Stock ReinforcementPrice = new(6, 0, 0, 1, 0, 4);
    public static readonly Stock RescueParts = new(2, 1, 0, 0);
    public DeepDeposit? DepositAt(int cell) => Extraction?.Deposits.SingleOrDefault(d => d.Cell == cell);
    public static Stock DeepeningPrice(DeepDeposit d) => new(8 * d.Depth, 0, 0, 2 * d.Depth);
    public static float BatteryCapacity(Machine m) => m.Reinforced ? 360 : 240;

    public void EnableExtraction()
    {
        if (Extraction is not null) return;
        Extraction = new();
        int cx = World.Width / 2, cy = World.Height / 2;
        // Readings under the original cleared apron: no terrain edits or shared RNG draws.
        // Persist the readings, including on upgraded historical campaigns.
        foreach (var (x, y) in new[] { (12, 6), (-12, -6), (4, 8) })
        {
            int cell = World.Index(cx + x, cy + y);
            Extraction.Deposits.Add(new() { Cell = cell });
            World.Reveal(cell, 1);
        }
        Event("The restored pump resolves three deep readings near the shelter. Survey a reading, extend power and pressure, then build an extraction rig.", NoticeLevel.Good);
    }

    private void SurveyDeposit(int cell)
    {
        var d = DepositAt(cell);
        if (d is null || d.Surveyed) return;
        d.Surveyed = true;
        Event($"DEPOSIT / {World.X(cell)},{World.Y(cell)}: {d.Remaining} deep ore in the first layer. Three finite layers; a rig needs breathable pressure, a nearby sleeping pod and 8 kW.", NoticeLevel.Good);
    }

    public string? IndustrialBlock(Machine m, bool requireOre = true)
    {
        if (!m.Enabled) return "Switched off";
        if (!Operational(m)) return "Equipment stopped; queue service";
        if (!m.Powered) return "Insufficient connected power";
        if (!World.Breathable(m.Cell, .45f) || World.Heat[m.Cell] is < 0 or >= 46 || World.Fire[m.Cell] > .1f)
            return "Needs breathable pressure and safe temperature";
        if (m.Kind == ModuleKind.Drill)
        {
            var d = DepositAt(m.Cell);
            if (d is not { Surveyed: true }) return "Needs a surveyed deep deposit";
            if (requireOre && d.Remaining == 0) return d.Depth < 3 ? "Layer exhausted; lower the rig" : "Deposit exhausted";
            if (!HasExtractionRefuge(m.Cell)) return "Needs a sleeping pod reachable through breathable deck within 6 steps";
        }
        return null;
    }

    private bool HasExtractionRefuge(int start)
    {
        // Radius six contains at most 85 cells. Bounded, local and independent of route/RNG budgets.
        Span<int> cells = stackalloc int[85], distances = stackalloc int[85], neighbors = stackalloc int[4];
        int head = 0, tail = 1; cells[0] = start; distances[0] = 0;
        while (head < tail)
        {
            int cell = cells[head], distance = distances[head++];
            if (MachineAt(cell) is { Kind: ModuleKind.Bunk } && World.Breathable(cell, .5f)) return true;
            if (distance == 6) continue;
            int count = World.Neighbors(cell, neighbors);
            for (int n = 0; n < count; n++)
            {
                int next = neighbors[n];
                if (!World.Breathable(next, .45f) || World.Heat[next] is < 0 or >= 46 || cells[..tail].Contains(next)) continue;
                cells[tail] = next; distances[tail++] = distance + 1;
            }
        }
        return false;
    }

    public float PowerDraw(Machine m) => !m.Enabled ? 0 : m.Kind == ModuleKind.Drill ? 8 + 4 * ((DepositAt(m.Cell)?.Depth ?? 1) - 1) : Blueprint.Get(m.Kind).Power;

    public Result SetIndustryEnabled(int cell, bool enabled)
    {
        if (GameOver || MachineAt(cell) is not { Kind: ModuleKind.Drill or ModuleKind.Fabricator } m || m.Enabled == enabled)
            return new(false, "Select an industrial module and change its power switch.");
        m.Enabled = enabled;
        if (!enabled && JobAt(cell) is { Assigned: int id }) Release(Crew.Single(c => c.Id == id));
        UpdatePower();
        return new(true, enabled ? "Industrial power enabled." : "Industrial power isolated; work and reservations are retained.");
    }

    public Result OrderExtraction(int cell) => QueueIndustrial(cell, ModuleKind.Drill, WorkKind.Extract, default, 36, "Extraction queued. A resident must operate the rig; cancel or service interrupts drilling.");
    public Result OrderComponent(int cell) => QueueIndustrial(cell, ModuleKind.Fabricator, WorkKind.Fabricate, ComponentRecipe, 24, "One component queued. Ingredients are reserved until completed or canceled.");

    private Result QueueIndustrial(int cell, ModuleKind kind, WorkKind work, Stock cost, float required, string message)
    {
        if (!StoryEnabled || GameOver || MachineAt(cell) is not { } m || m.Kind != kind || JobAt(cell) is not null || World.Discovered[cell] == 0)
            return new(false, "Select the right discovered module with no existing work order.");
        if (kind == ModuleKind.Drill && DepositAt(cell) is not { Surveyed: true, Remaining: > 0 }) return new(false, "Survey a deposit with ore remaining first.");
        if (!Resources.Covers(cost)) return new(false, "One component needs 4 deep ore, 4 metal and 2 crystal.");
        Resources -= cost;
        IndexJob(new() { Id = _nextJob++, Cell = cell, Kind = work, Blueprint = kind, Required = required + (work == WorkKind.Extract ? 12 * ((DepositAt(cell)?.Depth ?? 1) - 1) : 0),
            Reserved = cost != default, MaintenanceCost = cost });
        return new(true, message);
    }

    public Result OrderDeepening(int cell)
    {
        var d = DepositAt(cell);
        if (GameOver || MachineAt(cell) is not { Kind: ModuleKind.Drill } || d is not { Surveyed: true, Remaining: 0, Depth: < 3 } || JobAt(cell) is not null)
            return new(false, "Lower an idle rig only after exhausting its current layer. Maximum depth is 3.");
        var cost = DeepeningPrice(d);
        if (!Resources.Covers(cost)) return new(false, $"Lowering this rig needs {cost.Metal:0} metal and {cost.Crystal:0} crystal.");
        Resources -= cost;
        IndexJob(new() { Id = _nextJob++, Cell = cell, Kind = WorkKind.Deepen, Blueprint = ModuleKind.Drill, Required = 24, Reserved = true, MaintenanceCost = cost });
        return new(true, "Shaft work queued. The next layer needs 4 kW more and causes greater wear.");
    }

    public Result OrderReinforcement(int cell)
    {
        if (!StoryEnabled || GameOver || MachineAt(cell) is not { } m || !Serviceable(m) || m.Reinforced || JobAt(cell) is not null || World.Discovered[cell] == 0)
            return new(false, "Select an unreinforced serviceable module with no work order.");
        if (!Resources.Covers(ReinforcementPrice)) return new(false, "Reinforcement needs 4 deep ore, 6 metal and 1 crystal.");
        Resources -= ReinforcementPrice;
        IndexJob(new() { Id = _nextJob++, Cell = cell, Kind = WorkKind.Reinforce, Blueprint = m.Kind, Required = 18, Reserved = true, MaintenanceCost = ReinforcementPrice });
        return new(true, "Reinforcement queued: half wear; life support also saves 35% water, batteries hold 360, suit service equips 50% oxygen savings.");
    }

    public Result OrderResidentRescue(int residentId)
    {
        var c = Crew.SingleOrDefault(c => c.Id == residentId && c.Alive && c.Location == ResidentLocation.Home && c.Incapacitated);
        if (GameOver || c is null || Crew.Any(r => r.CarryingResidentId == c.Id) || JobAt(c.Cell) is not null)
            return new(false, "Select a stranded home resident without an existing rescue or work order on their cell.");
        if (!Resources.Covers(RescueParts)) return new(false, "Recovery equipment needs 2 metal and 1 water.");
        Resources -= RescueParts;
        IndexJob(new() { Id = _nextJob++, Cell = c.Cell, Kind = WorkKind.Rescue, Required = 9, Reserved = true, MaintenanceCost = RescueParts, TargetResidentId = c.Id });
        Event("Recovery queued for " + c.Name + ". Another resident must reach them and carry them to pressure.", NoticeLevel.Warning);
        return new(true, "Rescue equipment reserved. A rested worker with Rescue enabled is needed.");
    }

    // Return true when this job supplied its own progress/completion handling.
    private bool IndustrialWork(Colonist c, WorkOrder job, float progress)
    {
        if (job.Kind is not (WorkKind.Extract or WorkKind.Fabricate)) return false;
        var m = MachineAt(job.Cell);
        if (m is null) { Cancel(job); return true; }
        string? blocked = IndustrialBlock(m);
        if (blocked is not null || !World.Breathable(c.Cell, .38f) || job.Kind == WorkKind.Extract && m.Condition < 50 && !c.Work.AllowWornDrilling)
        {
            c.Activity = blocked ?? "Drilling stopped by resident safety policy";
            Release(c); job.RetryAfter = Tick + 8;
            return true;
        }
        c.Activity = job.Kind == WorkKind.Extract ? $"Drilling / depth {DepositAt(m.Cell)!.Depth}" : "Fabricating a component";
        job.Progress += progress;
        if (job.Kind == WorkKind.Fabricate)
        {
            if (job.Progress >= job.Required) Complete(c, job);
            return true;
        }
        var d = DepositAt(m.Cell)!;
        float before = m.Condition;
        m.Condition = Math.Max(0, m.Condition - (.13f + .04f * d.Depth) * (m.Reinforced ? .5f : 1));
        c.Energy = Math.Max(0, c.Energy - .08f * d.Depth);
        if (before >= 60 && m.Condition < 60)
            Event("Extraction rig at " + World.X(m.Cell) + "," + World.Y(m.Cell) + " is wearing out. Service before 50%; worn drilling risks its operator.", NoticeLevel.Warning);
        if (m.Condition <= 25 || m.Condition < 50 && c.Energy < 35)
        {
            m.Fault = MachineFault.Offline;
            c.Health = Math.Max(5, c.Health - 30); c.Incapacitated = true;
            Cancel(job);
            Remember(c, "drilling-injury", "I was injured operating a worn extraction rig. I need someone to bring me back.", -16, 960);
            foreach (var other in Crew.Where(o => o.Alive && o.Id != c.Id))
                Remember(other, "unsafe-drilling", c.Name + " was hurt after worn drilling was allowed.", other.Disposition?.Dislikes == CrewAversion.Danger ? -12 : -7, 960, c.Id);
            Event(c.Name + " is stranded beside a jammed rig after worn drilling. Queue a resident rescue and service the rig.", NoticeLevel.Danger);
            return true;
        }
        if (job.Progress < job.Required) return true;
        int amount = Math.Min(d.Remaining, 2 + 2 * d.Depth);
        d.Remaining -= amount; d.Extracted += amount;
        Resources += new Stock(amount, 0, 0, 0, 0, amount);
        job.Progress = 0; c.CompletedWork++;
        Remember(c, "extraction", "Our new workings supplied the station.", 6);
        Event($"EXTRACTION / +{amount} deep ore, +{amount} metal. Layer {d.Depth}: {d.Remaining} ore remains.", NoticeLevel.Good);
        if (d.Remaining == 0) { Cancel(job); Event(d.Depth < 3 ? "Layer exhausted. Service the rig, then lower it to reach the next layer." : "The third layer is exhausted. Survey another reading."); }
        return true;
    }

    private bool CompleteIndustrial(Colonist c, WorkOrder job)
    {
        if (job.Kind == WorkKind.Fabricate)
        {
            Resources += new Stock(0, 0, 0, 0, 1);
            Remember(c, "fabrication", "I made a usable component from our deep ore.", 8);
            Event(c.Name + " fabricated a component.", NoticeLevel.Good);
        }
        else if (job.Kind == WorkKind.Deepen)
        {
            var d = DepositAt(job.Cell)!;
            d.Depth++; d.Remaining = 24 * d.Depth;
            Event($"Rig lowered to depth {d.Depth}: {d.Remaining} ore, {8 + 4 * (d.Depth - 1)} kW required. Queue extraction when ready.", NoticeLevel.Good);
        }
        else if (job.Kind == WorkKind.Reinforce)
        {
            MachineAt(job.Cell)!.Reinforced = true;
            Remember(c, "reinforcement", "I made the station more resilient.", 8);
            Event(c.Name + " reinforced " + Blueprint.Get(job.Blueprint).Name.ToLowerInvariant() + ".", NoticeLevel.Good);
        }
        else if (job.Kind == WorkKind.Rescue)
        {
            c.CarryingResidentId = job.TargetResidentId;
            Crew.Single(r => r.Id == job.TargetResidentId).Cell = c.Cell;
            Event(c.Name + " reached the injured worker and is carrying them to shelter.", NoticeLevel.Good);
        }
        else return false;
        return true;
    }

    private void IndustrialTick()
    {
        foreach (var c in Crew.Where(c => c.CarryingResidentId is int id && (!c.Alive || !Crew.Single(t => t.Id == id).Alive)))
            c.CarryingResidentId = null;
        foreach (var job in _jobs.Where(j => j.Kind is WorkKind.Extract or WorkKind.Fabricate or WorkKind.Deepen or WorkKind.Reinforce or WorkKind.Rescue).ToArray())
        {
            if (job.Kind == WorkKind.Rescue ? !Crew.Any(c => c.Id == job.TargetResidentId && c.Alive && c.Incapacitated) : MachineAt(job.Cell) is null)
                Cancel(job);
        }
    }
}
