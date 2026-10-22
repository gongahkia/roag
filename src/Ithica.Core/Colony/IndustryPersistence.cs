namespace Ithica.Core.Colony;

public sealed partial class Simulation
{
    private static void ValidateIndustry(ExtractionState? state, List<Colonist> crew, List<Machine> machines, List<WorkOrder> jobs, int size)
    {
        if (state is not null && (state.Version != 1 || state.Deposits is null || state.Deposits.Count != 3
            || state.Deposits.Any(d => d is null || (uint)d.Cell >= size || d.Depth is < 1 or > 3 || d.Remaining < 0 || d.Remaining > 24 * d.Depth
                || d.Extracted < 0 || d.Extracted > 144 || d.Remaining + d.Extracted != 12 * d.Depth * (d.Depth + 1)
                || !d.Surveyed && (d.Depth != 1 || d.Extracted != 0))
            || state.Deposits.Select(d => d.Cell).Distinct().Count() != state.Deposits.Count))
            throw new InvalidDataException("Invalid saved extraction deposits.");
        foreach (var c in crew)
        {
            if (c.Work is null || c.Work.Priorities is null || c.Work.Priorities.Length != 6 || c.Work.Priorities.Any(p => p is < 0 or > 3) || !Enum.IsDefined(c.Work.Shift)
                || c.Incapacitated && (c.Location != ResidentLocation.Home || c.Job is not null || c.Expedition is not null || c.CarryingResidentId is not null)
                || c.CarryingResidentId is int id && (id < 0 || id >= crew.Count || id == c.Id || !c.Alive || c.Location != ResidentLocation.Home || c.Job is not null
                    || !crew[id].Alive || !crew[id].Incapacitated || crew[id].Cell != c.Cell))
                throw new InvalidDataException("Invalid saved crew work or recovery state.");
            if (c.Job is int jobId && !jobs.Any(j => j.Id == jobId && j.Assigned == c.Id))
                throw new InvalidDataException("Work assignment disagrees with resident.");
        }
        var carried = crew.Where(c => c.CarryingResidentId is not null).Select(c => c.CarryingResidentId).ToArray();
        if (carried.Distinct().Count() != carried.Length) throw new InvalidDataException("Resident has multiple rescuers.");
        foreach (var m in machines)
            if (m.Kind == ModuleKind.Drill && state?.Deposits.SingleOrDefault(d => d.Cell == m.Cell) is not { Surveyed: true }
                || m.Kind == ModuleKind.Fabricator && state is null || m.Reinforced && !Serviceable(m))
                throw new InvalidDataException("Invalid industrial module or reinforcement.");
        foreach (var j in jobs)
        {
            if (j.Assigned is int id && (crew[id].Job != j.Id || !crew[id].Alive || crew[id].Location != ResidentLocation.Home || crew[id].Incapacitated))
                throw new InvalidDataException("Work order disagrees with assigned resident.");
            if (j.Kind is not (WorkKind.Extract or WorkKind.Fabricate or WorkKind.Deepen or WorkKind.Reinforce or WorkKind.Rescue))
            {
                if (j.TargetResidentId is not null) throw new InvalidDataException("Unexpected recovery target.");
                continue;
            }
            var m = machines.SingleOrDefault(m => m.Cell == j.Cell);
            var d = state?.Deposits.SingleOrDefault(d => d.Cell == j.Cell);
            Stock cost; float required;
            switch (j.Kind)
            {
                case WorkKind.Extract:
                    if (m is not { Kind: ModuleKind.Drill } || d is not { Surveyed: true, Remaining: > 0 }) throw new InvalidDataException("Invalid extraction order.");
                    cost = default; required = 36 + 12 * (d.Depth - 1); break;
                case WorkKind.Fabricate:
                    if (m is not { Kind: ModuleKind.Fabricator }) throw new InvalidDataException("Invalid fabrication order.");
                    cost = ComponentRecipe; required = 24; break;
                case WorkKind.Deepen:
                    if (m is not { Kind: ModuleKind.Drill } || d is not { Surveyed: true, Remaining: 0, Depth: < 3 }) throw new InvalidDataException("Invalid shaft order.");
                    cost = DeepeningPrice(d); required = 24; break;
                case WorkKind.Reinforce:
                    if (m is null || m.Reinforced || !Serviceable(m)) throw new InvalidDataException("Invalid reinforcement order.");
                    cost = ReinforcementPrice; required = 18; break;
                default:
                    if (j.TargetResidentId is not int target || target < 0 || target >= crew.Count || !crew[target].Alive || !crew[target].Incapacitated
                        || crew[target].Cell != j.Cell || carried.Contains(target)) throw new InvalidDataException("Invalid resident recovery order.");
                    cost = RescueParts; required = 9; break;
            }
            if (j.MaintenanceCost != cost || j.Reserved != (cost != default) || j.Required != required
                || j.Kind != WorkKind.Rescue && (j.TargetResidentId is not null || j.Blueprint != m!.Kind))
                throw new InvalidDataException("Invalid reserved industrial parts.");
        }
    }
}
