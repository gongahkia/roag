using Roag.Core.Expeditions;

namespace Roag.Core.World;

public sealed record WorkDesignationResult(int Queued, int Skipped, string Message);

public sealed partial class WorldSession
{
    public WorkDesignationResult DesignateWork(WorldCommandKind kind, IEnumerable<WorldPosition> cells, Material material = Material.Wall)
    {
        WorkDesignationResult Result(int queued, int skipped, string message, WorldPosition[]? targets = null)
        {
            var result = new WorkDesignationResult(queued, skipped, message);
            if (DiagnosticSink != null) Diagnose("work-designation", new { kind = kind.ToString(), material = material.ToString(), targets, result.Queued, result.Skipped, result.Message });
            return result;
        }
        if (IsInCombat || DeathPending || State.AwaitingContinuation || State.Endgame.Victory)
            return Result(0, 0, "Finish the encounter or expedition first.");
        if (kind is not (WorldCommandKind.Build or WorldCommandKind.Dig or WorldCommandKind.Repair or WorldCommandKind.Harvest)) return Result(0, 0, "Choose construction, excavation or repair.");
        var targets = cells.Distinct().Take(257).ToArray();
        if (targets.Length > 256) return Result(0, targets.Length, "Select at most 256 tiles at a time.", targets);
        int queued = 0, skipped = 0; string reason = "";
        foreach (var p in targets.OrderBy(p => p.Distance(State.Position)).ThenBy(p => p.Y).ThenBy(p => p.X))
        {
            string blocked = TargetBlockReason(kind, p, material, requireAdjacent: false);
            if (blocked.Length > 0) { skipped++; reason = blocked; continue; }
            ActionResult result = kind switch
            {
                WorldCommandKind.Build => QueueBuild(new(kind, p, Material: material), designated: true),
                WorldCommandKind.Dig => QueueDig(p, designated: true),
                WorldCommandKind.Harvest => QueueHarvest(p, designated: true), _ => QueueRepair(p, designated: true)
            };
            if (result.Success) queued++; else { skipped++; reason = result.Message; }
        }
        return Result(queued, skipped, queued == 0 ? (reason.Length > 0 ? reason : "Select suitable observed tiles.")
            : $"Queued {queued} order(s); supplies reserved. The party will approach and work." + (skipped > 0 ? $" Skipped {skipped}: {reason}" : ""), targets);
    }

    public void StopPartyWorkApproach()
    {
        foreach (var order in State.Work.Where(o => o.Worker == 0 || Party.Any(a => a.Id == o.Worker)))
        { if (!order.PinnedWorker) order.Worker = 0; order.Suspended = true; order.PartyApproach = false; order.Status = "Suspended"; }
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
