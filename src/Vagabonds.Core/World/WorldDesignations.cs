using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed record WorkDesignationResult(int Queued, int Skipped, string Message);

public sealed partial class WorldSession
{
    public WorkDesignationResult DesignateWork(WorldCommandKind kind, IEnumerable<WorldPosition> cells, Material material = Material.Wall)
    {
        if (IsInCombat || DeathPending || State.AwaitingContinuation || State.Endgame.Victory)
            return new(0, 0, "Finish the encounter or expedition first.");
        if (kind is not (WorldCommandKind.Build or WorldCommandKind.Dig or WorldCommandKind.Repair)) return new(0, 0, "Choose construction, excavation or repair.");
        var targets = cells.Distinct().Take(257).ToArray();
        if (targets.Length > 256) return new(0, targets.Length, "Select at most 256 tiles at a time.");
        int queued = 0, skipped = 0; string reason = "";
        foreach (var p in targets.OrderBy(p => p.Distance(State.Position)).ThenBy(p => p.Y).ThenBy(p => p.X))
        {
            string blocked = TargetBlockReason(kind, p, material, requireAdjacent: false);
            if (blocked.Length > 0) { skipped++; reason = blocked; continue; }
            ActionResult result = kind switch
            {
                WorldCommandKind.Build => QueueBuild(new(kind, p, Material: material), designated: true),
                WorldCommandKind.Dig => QueueDig(p, designated: true), _ => QueueRepair(p, designated: true)
            };
            if (result.Success) queued++; else { skipped++; reason = result.Message; }
        }
        return new(queued, skipped, queued == 0 ? (reason.Length > 0 ? reason : "Select suitable observed tiles.")
            : $"Queued {queued} order(s); supplies reserved. The party will approach and work." + (skipped > 0 ? $" Skipped {skipped}: {reason}" : ""));
    }

    public void StopPartyWorkApproach()
    {
        foreach (var order in State.Work) order.PartyApproach = false;
    }

    /// <summary>Advance the saved work-travel intent by one ordinary player step. Time is driven by the client.</summary>
    public ActionResult ApproachDesignatedWork()
    {
        if (Frozen) return new(false, "Work travel is paused.");
        var orders = State.Work.Where(w => w.PartyApproach).OrderBy(w => w.Id).ToArray();
        if (orders.Length == 0) return new(false, "");
        foreach (var order in orders)
        {
            var worker = State.Actors.FirstOrDefault(a => a.Id == order.Worker && a.Life == LifeState.Alive);
            if (worker is { Recruited: false }) { order.PartyApproach = false; continue; }
            var leader = Party.First();
            if (leader.Life != LifeState.Alive) { order.Status = "Choose a conscious party leader"; return new(false, order.Status); }
            if (worker != leader)
            {
                if (State.Work.Any(w => w.Id != order.Id && w.Worker == leader.Id)) continue;
                order.Worker = leader.Id;
            }
            if (leader.Position.Distance(order.Target) <= 1) return new(false, "");
            var route = RouteToAdjacent(order.Target);
            if (route.Count > 0) return Submit(new(WorldCommandKind.Move, route[0]));
            // A later order may open access to this one (for example a selected rock seam).
            order.Worker = 0; order.Status = "No free observed approach; open access or move a blocking character";
        }
        return new(false, "Waiting for a free approach to the selected work.");
    }

    public IReadOnlyList<WorldPosition> RouteToAdjacent(WorldPosition target, int range = 1)
    {
        if (State.Position.Distance(target) <= range) return [];
        var candidates = new List<WorldPosition>();
        for (int y = -range; y <= range; y++) for (int x = -range; x <= range; x++)
        {
            var p = target.Offset(x, y);
            if (Math.Abs(x) + Math.Abs(y) <= range && Chunks.Seen(p) && CanEnter(p, State.Party[0], observedOnly: true)) candidates.Add(p);
        }
        foreach (var p in candidates.OrderBy(p => p.Distance(State.Position)).ThenBy(p => p.Y).ThenBy(p => p.X))
        { var route = Route(p); if (route.Count > 0) return route; }
        return [];
    }
}
