namespace Ithica.Core.Colony;

public enum MachineFault { None, Offline, Leak, Overheating }

public sealed partial class Simulation
{
    public static bool Serviceable(Machine m) => m.Kind != ModuleKind.Bunk;
    private bool Operational(Machine m) => m.Enabled && (!StoryEnabled || m.Fault != MachineFault.Offline && m.Condition > 15);
    private float Efficiency(Machine m) => !StoryEnabled ? 1 : m.Condition < 50 ? .6f : 1;
    public static Stock MaintenancePrice(Machine m) => m.Fault != MachineFault.None || m.Condition < 50 || m.Health < 60
        ? new(6, 0, 0, 1) : new(2, 0, 0, 0);

    public Result OrderMaintenance(int cell)
    {
        if (!StoryEnabled || GameOver || !World.Valid(cell) || World.Discovered[cell] == 0 || MachineAt(cell) is not { } m || !Serviceable(m))
            return new(false, "Select a discovered serviceable module.");
        if (JobAt(cell) is { Kind: not WorkKind.Extract }) return new(false, "A work order already occupies this module.");
        if (m.Condition >= 99 && m.Health >= 99 && m.Fault == MachineFault.None) return new(false, "This module does not need service.");
        var cost = MaintenancePrice(m);
        if (!Resources.Covers(cost)) return new(false, "Service needs 2 metal; major repairs need 6 metal and 1 crystal.");
        if (JobAt(cell) is { } extraction) Cancel(extraction);
        Resources -= cost;
        IndexJob(new() { Id = _nextJob++, Kind = WorkKind.Maintain, Cell = cell, Blueprint = m.Kind,
            Required = cost.Crystal > 0 ? 18 : 9, Reserved = true, MaintenanceCost = cost });
        Event("Service queued for " + Blueprint.Get(m.Kind).Name.ToLowerInvariant() + ". Materials reserved.");
        return new(true, "A resident must reach the module and finish the repair. Cancel returns the reserved parts.");
    }

    private void MaintenanceTick()
    {
        foreach (var m in _machines)
        {
            if (!Serviceable(m)) continue;
            float wear = m.Powered ? .008f : .002f;
            if (Storm || World.Oxygen[m.Cell] < .25f || World.Heat[m.Cell] > 45) wear *= 2;
            if (m.Reinforced) wear *= .5f;
            m.Condition = Math.Max(0, m.Condition - wear);
            if (m.Fault == MachineFault.Leak)
            {
                float loss = Math.Min(Resources.Water, .035f);
                Resources -= new Stock(0, loss, 0, 0);
                World.Oxygen[m.Cell] *= .94f;
            }
            if (m.Fault == MachineFault.Overheating)
            {
                World.Heat[m.Cell] += .8f;
                m.Condition = Math.Max(0, m.Condition - .15f);
                if (m.Condition < 20) World.Fire[m.Cell] = Math.Max(World.Fire[m.Cell], .25f);
            }
            if (m.Condition <= 15 && m.Fault != MachineFault.Offline)
            {
                m.Fault = MachineFault.Offline;
                Story!.RecoveryUntil = Math.Max(Story.RecoveryUntil, Tick + 180);
                Event(Blueprint.Get(m.Kind).Name + " stopped after deferred maintenance. Queue service.", NoticeLevel.Danger);
            }
        }
        // A fire/impact can destroy a reserved job's target. Refund once and release its worker.
        foreach (var job in _jobs.Where(j => j.Kind == WorkKind.Maintain && MachineAt(j.Cell) is null).ToArray()) Cancel(job);
    }
}
