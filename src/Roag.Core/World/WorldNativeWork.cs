using Roag.Core.Tactics;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    internal string NativeWorkReason(Actor worker, WorldCommand? command)
    {
        if (command == null || command.ActorId != 0 && command.ActorId != worker.Id) return "Assign this member their own work.";
        if (worker.Body.Fatigue >= 75 || worker.Body.Hunger >= Simulation.StarveAt || worker.Health < worker.MaxHealth / 4)
            return "This member needs food, rest or treatment before working.";
        if (command.Kind == WorldCommandKind.AssignWork)
        {
            var existing = State.Work.FirstOrDefault(w => w.Id == command.WorkId);
            if (existing == null || existing.Worker != 0 && !State.Party.Contains(existing.Worker) || State.Work.Any(w => w.Id != existing.Id && w.Worker == worker.Id))
                return "Choose unfinished party work and a member without another order.";
            if (!CampWorkerEligible(existing, worker)) return "This member has already shared a story; let two game hours pass.";
            return "";
        }
        if (State.Work.Any(w => w.Worker == worker.Id)) return "Finish or cancel this member's existing order first.";
        if (command.Kind == WorldCommandKind.CampActivity)
        {
            if (!VisionFrom(worker.Position).Contains(command.Target)) return "Choose a camp in this member's current sight.";
            if (Content.Entries.GetValueOrDefault(command.Item) is Content.CampDefinition { Role: "cook" }
                && State.Tactics!.PreparedMeals.GetValueOrDefault(command.Item)
                    + State.Work.Count(w => w.Kind == WorkKind.Camp && w.Definition == command.Item) * 4 > 996)
                return "Eat some prepared servings before cooking more of this meal.";
            return CampActivityReason(command.Target, command.Item, worker.Id);
        }
        if (command.Kind is WorldCommandKind.Dig or WorldCommandKind.Build or WorldCommandKind.Repair or WorldCommandKind.Harvest)
        {
            if (!VisionFrom(worker.Position).Contains(command.Target)) return "Choose terrain in this member's current sight.";
            if (State.Dungeon != null && !State.Dungeon.Contains(command.Target)) return "The terrain is outside this complex.";
            if (command.Kind is WorldCommandKind.Dig or WorldCommandKind.Build && (State.Tactics!.Gates.Nodes.Any(g => g.Position == command.Target)
                || State.Tactics.Quests.Nodes.Any(q => q.Nodes.Any(n => n.Position == command.Target && n.Kind == Roag.Core.Dungeons.DungeonBranchNodeKind.Gate))))
                return "This reinforced mechanism resists alteration. Ordinary adjacent walls can still be excavated.";
            return TargetBlockReason(command.Kind, command.Target, command.Material, requireAdjacent: false);
        }
        if (command.Kind == WorldCommandKind.Forage)
        {
            if (worker.Body.ForageAfter > State.Tick) return "These gathering grounds need an hour to recover.";
            if (State.Work.Count >= 64 || State.Work.Any(w => w.Target == worker.Position)) return "The work site or list is occupied.";
            return WildFoodUnavailable(worker.Position);
        }
        return command.Kind == WorldCommandKind.Craft ? CommandBlockReason(command, nativeWork: true) : "Choose excavation, construction, repair, crafting or gathering.";
    }

    internal WorkOrder BeginNativeWork(Actor worker, WorldCommand command)
    {
        if (command.Kind == WorldCommandKind.AssignWork)
        {
            var existing = State.Work.Single(w => w.Id == command.WorkId);
            if (existing.Worker != 0 && existing.Worker != worker.Id)
            {
                if (existing.Progress > 0) NoteInterruptedWork(ActorById(existing.Worker), "work:" + existing.Id);
                DungeonCombat.CancelActivity(existing.Worker);
            }
            existing.Worker = worker.Id;
            existing.Suspended = false; existing.PinnedWorker = true;
            return existing;
        }
        long id = State.NextWorkId;
        var result = command.Kind switch
        {
            WorldCommandKind.Dig => QueueDig(command.Target, designated: true),
            WorldCommandKind.Build => QueueBuild(command, designated: true),
            WorldCommandKind.Repair => QueueRepair(command.Target, designated: true),
            WorldCommandKind.Harvest => QueueHarvest(command.Target, designated: true),
            WorldCommandKind.Craft => QueueCraft(command.Item),
            WorldCommandKind.CampActivity => QueueCampActivity(command.Target, command.Item, worker.Id),
            WorldCommandKind.Forage => QueueWork(new() { Kind = WorkKind.Forage, Target = worker.Position, Before = Chunks.At(worker.Position),
                After = Chunks.At(worker.Position), Required = Simulation.ForageWork, Yield = new() { ["food"] = 2 } }),
            _ => new ActionResult(false, "Unsupported native work.")
        };
        if (!result.Success) throw new InvalidOperationException("A previewed native work order failed: " + result.Message);
        var order = State.Work.Single(w => w.Id == id);
        order.Worker = worker.Id; order.RequestedBy = worker.Id; order.PinnedWorker = true; order.Suspended = false;
        return order;
    }

    internal void SuspendNativeWork(long id)
    {
        if (State.Work.FirstOrDefault(w => w.Id == id) is not { } order) return;
        order.Suspended = true; order.Status = "Interrupted; resume or cancel explicitly";
    }

    internal TacticalIntent? NativeWorkStep(Actor worker, long id)
    {
        var order = State.Work.FirstOrDefault(w => w.Id == id && w.Worker == worker.Id && !w.Suspended);
        if (order == null) return null;
        if (worker.Body.Fatigue >= 75 || worker.Body.Hunger >= Simulation.StarveAt || worker.Health < worker.MaxHealth / 4)
        { order.Status = "Worker needs food, rest or treatment"; return null; }
        var sight = VisionFrom(worker.Position);
        bool KnownByWorker(WorldPosition p) => !worker.Recruited && ResumingNativeResident(worker) ? Plan.Contains(p) : Chunks.Seen(p);
        if (order.Kind == WorkKind.Camp && (!CampWorkerEligible(order, worker)
            || order.Station is { } camp && sight.Contains(camp) && !ServiceableCamp(camp)))
        { CancelWork(id, deliberate: false); return null; }
        bool observedSupplyChange = order.Supplies?.Any(s => s.Phase == SupplyPhase.Waiting && s.Storage is { } p && sight.Contains(p) && Chunks.At(p) != Material.Storage) == true;
        if (order.Output == null && (observedSupplyChange || sight.Contains(order.Target) && (Chunks.At(order.Target) != order.Before
            || order.Kind == WorkKind.Harvest && (ResourceAt(order.Target) is not { Available: > 0 } node || node.Definition.Id != order.Definition)
            || order.Kind == WorkKind.Repair && !State.Structures.Any(s => s.Position == order.Target))))
        { CancelWork(id, deliberate: false); return null; }
        var target = WorkStop(order, worker.Id);
        if (worker.Position.Distance(target) <= 1)
        {
            if (worker.Position == order.Target && order.Kind is WorkKind.Build or WorkKind.Dig
                && (!Materials.Walkable(order.After) || order.After == Material.Camp))
            {
                foreach (var direction in new[] { Facing.North, Facing.East, Facing.South, Facing.West })
                {
                    var (dx, dy) = AttackGeometry.Offset(direction); var next = worker.Position.Offset(dx, dy);
                    if (CanEnter(next, worker.Id) && KnownByWorker(next) && Chunks.At(next) is not (Material.StairsDown or Material.StairsUp))
                        return new(TacticalAction.Move, direction);
                }
                order.Status = "No clear tile beside construction"; return null;
            }
            return new(TacticalAction.ContinueActivity);
        }
        var goals = new HashSet<WorldPosition>();
        foreach (var direction in new[] { Facing.North, Facing.East, Facing.South, Facing.West })
        {
            var (dx, dy) = AttackGeometry.Offset(direction); var p = target.Offset(dx, dy);
            if (CanEnter(p, worker.Id)) goals.Add(p);
        }
        var step = Navigate(worker, goals, p => KnownByWorker(p) && (State.Dungeon == null || State.Dungeon.Contains(p)));
        if (step == null) { order.Status = "No free observed approach; open access or move blockers"; return null; }
        return new(step.Action, step.Facing);
    }

    internal bool PerformNativeWork(Actor worker, long id, long recovery, TacticalCombat combat, long action)
    {
        var order = State.Work.Single(w => w.Id == id);
        if (order.Output != null) { AdvanceWorkOutput(order, worker); return State.Work.Contains(order); }
        if (!AdvanceWorkSupplies(order, worker) || worker.Position.Distance(order.Target) > 1) return true;
        if (order.Kind == WorkKind.Forage && (worker.Body.ForageAfter > State.Tick || WildFoodUnavailable(order.Target).Length > 0)) return false;
        if (order.Kind is WorkKind.Build or WorkKind.Dig && (!Materials.Walkable(order.After) || order.After == Material.Camp)
            && State.Actors.Any(a => a.Life != LifeState.Dead && a.Position == order.Target)) return false;
        PerformWork(order, worker, WorkRate(worker) * WorkMultiplier(worker, order) * recovery / State.Tactics!.Tuning.UnitsPerWorldTick, combat, action);
        return State.Work.Contains(order);
    }

    public ActionResult CancelDungeonWork(long id)
    {
        if (!HasNativeParty || State.Work.FirstOrDefault(w => w.Id == id && State.Party.Contains(w.Worker)) is not { } order)
            return new(false, "Choose an unfinished party order.");
        DungeonCombat.CancelActivity(order.Worker);
        return CancelWork(id);
    }
}
