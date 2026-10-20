namespace Ithica.Core.Colony;

public sealed partial class Simulation
{
    private static void ValidateStory(StoryState? story, List<Colonist> crew, List<Machine> machines, List<WorkOrder> jobs, long tick, int size)
    {
        static bool Unit(float v) => float.IsFinite(v) && v is >= 0 and <= 100;
        foreach (var m in machines)
            if (!Unit(m.Condition) || !Enum.IsDefined(m.Fault)) throw new InvalidDataException("Invalid module service state.");
        foreach (var j in jobs)
            if (!j.MaintenanceCost.Valid || j.Kind == WorkKind.Maintain && (!j.Reserved || j.MaintenanceCost != new Stock(2, 0, 0, 0) && j.MaintenanceCost != new Stock(6, 0, 0, 1)))
                throw new InvalidDataException("Invalid reserved service parts.");
        foreach (var c in crew)
        {
            if (story is not null && c.Disposition is null || c.Disposition is { } disposition && (!Enum.IsDefined(disposition.Likes) || !Enum.IsDefined(disposition.Dislikes))
                || c.Memories is null || c.Memories.Count > 12 || c.Memories.Any(m => m is null || string.IsNullOrWhiteSpace(m.Key) || string.IsNullOrWhiteSpace(m.Text)
                    || m.Tick < 0 || m.Tick > tick || m.Until <= m.Tick || m.Mood is < -30 or > 30 || m.OtherId is int id && (id < 0 || id >= crew.Count || id == c.Id))
                || c.Bonds is null || c.Bonds.Count > crew.Count || c.Bonds.Any(b => b is null || b.OtherId < 0 || b.OtherId >= crew.Count || b.OtherId == c.Id || b.Trust is < -100 or > 100)
                || c.Bonds.Select(b => b.OtherId).Distinct().Count() != c.Bonds.Count || c.BreakUntil < 0 || c.WorkTicks is < 0 or > 1000000)
                throw new InvalidDataException("Invalid resident memories or relationships.");
        }
        if (story is null) return; // Historical colony saves remain readable.
        bool Definition(string? id) => IncidentLibrary.Any(d => d.Id == id);
        bool Resident(int? id) => id is null || id >= 0 && id < crew.Count;
        bool Cell(int? cell) => cell is null || cell >= 0 && cell < size;
        if (story.Version != 1 || story.NextBeat < 0 || story.RecoveryUntil < 0 || story.NextId <= 0
            || !float.IsFinite(story.Budget) || story.Budget is < 0 or > 3 || story.LastSeverity is < 0 or > 2 || story.HostileStreak is < 0 or > 2
            || story.Pending is null || story.Pending.Count > 3 || story.History is null || story.History.Count > 64
            || story.Followups is null || story.Followups.Count > 12 || story.Cooldowns is null
            || story.Cooldowns.Any(c => !Definition(c.Key) || c.Value < 0)
            || story.Pending.Any(i => i is null || i.Id <= 0 || i.Id >= story.NextId || !Definition(i.DefinitionId)
                || i.OpenedAt < 0 || i.OpenedAt > tick || i.DueAt <= i.OpenedAt || i.DueAt <= tick || !Cell(i.Cell) || !Resident(i.ResidentId) || !Resident(i.OtherId)
                || i.OtherId is not null && i.OtherId == i.ResidentId || i.MachineId is < 0)
            || story.Pending.Select(i => i.Id).Distinct().Count() != story.Pending.Count
            || story.History.Any(h => h is null || h.Id <= 0 || h.Id >= story.NextId || !Definition(h.DefinitionId) || h.Tick < 0 || h.Tick > tick || string.IsNullOrWhiteSpace(h.Outcome))
            || story.History.Select(h => h.Id).Distinct().Count() != story.History.Count
            || story.Pending.Any(i => story.History.Any(h => h.Id == i.Id))
            || story.Followups.Any(f => f is null || f.Kind is not ("pump" or "harvest" or "cache") || f.IncidentId <= 0 || f.IncidentId >= story.NextId || f.DueAt <= tick || !Cell(f.Cell) || f.MachineId is < 0
                || f.Kind != "cache" && f.MachineId is null))
            throw new InvalidDataException("Invalid incident director state.");
        foreach (var i in story.Pending)
        {
            bool module = i.DefinitionId is "pump" or "relay" or "seed";
            bool pair = i.DefinitionId is "workload" or "meal" or "solidarity";
            bool person = pair || i.DefinitionId == "fatigue";
            if (module != (i.MachineId is not null) || (module || i.DefinitionId is "debris" or "cache" or "signal") != (i.Cell is not null)
                || person != (i.ResidentId is not null) || pair != (i.OtherId is not null))
                throw new InvalidDataException("Invalid incident targets.");
            var target = machines.FirstOrDefault(m => m.Id == i.MachineId && m.Cell == i.Cell);
            if (target is not null && (i.DefinitionId == "pump" && target.Kind != ModuleKind.Oxygen
                || i.DefinitionId == "seed" && target.Kind != ModuleKind.Farm
                || i.DefinitionId == "relay" && (!Serviceable(target) || target.Kind == ModuleKind.Oxygen)))
                throw new InvalidDataException("Incident targets the wrong module kind.");
        }
    }
}
