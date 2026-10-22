namespace Ithica.Core.Colony;

public enum WorkGroup { Construction, Mining, Maintenance, Survey, Industry, Rescue }
public enum CrewShift { Flexible, Day, Night, Extended, Rest }

public sealed class CrewWorkSettings
{
    // 0 disables a category; 1 is highest priority. Defaults preserve historical work behavior.
    public int[] Priorities { get; set; } = [2, 2, 2, 2, 2, 1];
    public CrewShift Shift { get; set; }
    public bool AllowWornDrilling { get; set; }
}

public sealed partial class Simulation
{
    public static WorkGroup GroupOf(WorkKind kind) => kind switch
    {
        WorkKind.Build or WorkKind.Reinforce => WorkGroup.Construction,
        WorkKind.Dig or WorkKind.Salvage or WorkKind.Extract or WorkKind.Deepen => WorkGroup.Mining,
        WorkKind.Maintain => WorkGroup.Maintenance,
        WorkKind.Survey => WorkGroup.Survey,
        WorkKind.Rescue => WorkGroup.Rescue,
        _ => WorkGroup.Industry
    };

    public bool OnShift(Colonist c) => c.Work.Shift switch
    {
        CrewShift.Day => Tick % TicksPerCycle < 240,
        CrewShift.Night => Tick % TicksPerCycle >= 240,
        CrewShift.Extended => Tick % TicksPerCycle < 400,
        CrewShift.Rest => false,
        _ => true
    };

    public Result SetWorkPriority(int residentId, WorkGroup group, int priority)
    {
        var c = Crew.SingleOrDefault(c => c.Id == residentId && c.Alive);
        if (c is null || !Enum.IsDefined(group) || priority is < 0 or > 3)
            return new(false, "Choose a living resident, work category and priority 0–3.");
        if (c.Work.Priorities[(int)group] == priority) return new(false, "That priority is already set.");
        c.Work.Priorities[(int)group] = priority;
        Release(c);
        return new(true, "Work priority changed. 1 is highest; 0 disables this work.");
    }

    public Result SetShift(int residentId, CrewShift shift)
    {
        var c = Crew.SingleOrDefault(c => c.Id == residentId && c.Alive);
        if (c is null || !Enum.IsDefined(shift) || c.Work.Shift == shift)
            return new(false, "Choose a different shift for a living resident.");
        c.Work.Shift = shift;
        Release(c);
        if (!OnShift(c)) c.Rest = true;
        if (StoryEnabled && shift == CrewShift.Rest) Remember(c, "reassigned-rest", "I was taken off the work roster to recover.", 8);
        return new(true, "Shift changed. Health, protected breaks and emergency safety still take precedence.");
    }

    public Result SetDrillingRisk(int residentId, bool allowWorn)
    {
        var c = Crew.SingleOrDefault(c => c.Id == residentId && c.Alive);
        if (c is null || c.Work.AllowWornDrilling == allowWorn) return new(false, "Choose a different drilling policy for a living resident.");
        c.Work.AllowWornDrilling = allowWorn;
        Release(c);
        return new(true, allowWorn ? "Worn drilling allowed. Below 50% condition, fatigue or a jam can injure and strand the operator." : "Drilling stops below 50% condition for this resident.");
    }

    public Result AssignRest(int residentId, int ticks = 120)
    {
        var c = Crew.SingleOrDefault(c => c.Id == residentId && c.Alive && c.Location == ResidentLocation.Home);
        if (c is null || c.Incapacitated || c.CarryingResidentId is not null || c.Expedition is not null || ticks is < 24 or > 480 || c.BreakUntil >= Tick + ticks)
            return new(false, "Assign 24–480 ticks of rest to an available home resident.");
        GrantBreak(c, ticks);
        return new(true, "Protected rest assigned. Work resumes after the break and recovery.");
    }

    private int WorkPriority(Colonist c, WorkOrder j) => c.Work.Priorities[(int)GroupOf(j.Kind)];
    private int WorkScore(Colonist c, WorkOrder j) => WorkPriority(c, j) * 100000 + World.Distance(c.Cell, j.Cell) - (Specialist(c, j) ? 10 : 0);
    private bool WorkEligible(Colonist c, WorkOrder j) => WorkPriority(c, j) > 0
        && (j.Kind != WorkKind.Extract || MachineAt(j.Cell) is { } m && IndustrialBlock(m) is null && (m.Condition >= 50 || c.Work.AllowWornDrilling))
        && (j.Kind != WorkKind.Deepen || MachineAt(j.Cell) is { Condition: >= 50 } shaft && IndustrialBlock(shaft, requireOre: false) is null)
        && (j.Kind != WorkKind.Fabricate || MachineAt(j.Cell) is { } f && IndustrialBlock(f) is null);

    private bool CarryTick(Colonist c)
    {
        if (c.CarryingResidentId is not int id) return false;
        var target = Crew.Single(t => t.Id == id);
        if (!target.Alive || !target.Incapacitated) { c.CarryingResidentId = null; return false; }
        if (World.Breathable(c.Cell, .5f) && World.Fire[c.Cell] < .1f)
        {
            target.Cell = c.Cell; target.Incapacitated = false; target.Rest = true; target.Health = Math.Max(20, target.Health);
            c.CarryingResidentId = null; c.Path.Clear(); c.PathCursor = 0;
            Remember(target, "rescued", c.Name + " carried me to breathable shelter.", 12, 960, c.Id);
            Remember(c, "rescuer", "I brought " + target.Name + " back from the failed rig.", 8, 960, target.Id);
            ChangeTrust(c, target, 12); ChangeTrust(target, c, 18);
            Event(c.Name + " brought " + target.Name + " to safety. They still need recovery.", NoticeLevel.Good);
            return true;
        }
        if (c.PathCursor >= c.Path.Count) RouteToSafety(c);
        Move(c); target.Cell = c.Cell;
        c.Energy = Math.Max(0, c.Energy - .12f);
        c.Activity = "Carrying " + target.Name + " to pressure";
        return true;
    }
}
