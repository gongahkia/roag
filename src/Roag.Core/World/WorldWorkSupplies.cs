using Roag.Core.Content;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private IEnumerable<(WorldPosition Position, Dictionary<string, int> Contents)> WorkStorage()
    {
        var rally = State.WorkRally ?? State.Position;
        int left = Math.Max(0, rally.X - WorkRallyRadius) / 64, right = Math.Min(WorldPlan.Size - 1, rally.X + WorkRallyRadius) / 64;
        int top = Math.Max(0, rally.Y - WorkRallyRadius) / 64, bottom = Math.Min(WorldPlan.Size - 1, rally.Y + WorkRallyRadius) / 64;
        for (int y = top; y <= bottom; y++) for (int x = left; x <= right; x++)
            if (Chunks.ExistingDelta(new(x, y, rally.Floor)) is { } delta)
                foreach (var (index, contents) in delta.Storage)
                {
                    var p = new WorldPosition(x * 64 + index % 64, y * 64 + index / 64, rally.Floor);
                    if (InWorkRally(p) && Chunks.Seen(p) && Chunks.At(p) == Material.Storage) yield return (p, contents);
                }
    }
    private bool PlanWorkSupplies(Dictionary<string, int> cost, WorldPosition target, out List<WorkSupply> supplies)
    {
        supplies = []; (WorldPosition Position, Dictionary<string, int> Contents)[]? storage = null;
        foreach (var (stack, count) in cost.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            int carried = Math.Min(count, Count(stack)), remaining = count - carried;
            if (carried > 0) supplies.Add(new() { Stack = stack, Count = carried });
            if (remaining == 0) continue;
            storage ??= WorkStorage().OrderBy(s => s.Position.Distance(target)).ThenBy(s => s.Position.Y).ThenBy(s => s.Position.X).ToArray();
            foreach (var source in storage)
            {
                int amount = Math.Min(remaining, source.Contents.GetValueOrDefault(stack));
                if (amount <= 0) continue;
                supplies.Add(new() { Stack = stack, Count = amount, Storage = source.Position }); remaining -= amount;
                if (remaining == 0) break;
            }
            if (remaining > 0 || supplies.Count > 64) return false;
        }
        return supplies.Count <= 64;
    }
    private string WorkSuppliesReason(Dictionary<string, int> cost, WorldPosition target)
    {
        bool packEnough = true;
        foreach (var (stack, count) in cost) if (Count(stack) < count) { packEnough = false; break; }
        if (packEnough && cost.Count <= 64 || PlanWorkSupplies(cost, target, out _)) return "";
        return "Needs " + string.Join(", ", cost.Select(p => p.Value + " " + p.Key)) + " in the pack or observed rally storage (at most 64 supply stacks).";
    }
    private void ReserveWorkSupplies(WorkOrder order, List<WorkSupply> supplies)
    {
        order.SupplyVersion = 1; order.Supplies = supplies;
        foreach (var supply in supplies)
            if (supply.Storage is { } source) Chunks.ExistingDelta(source.Chunk)!.Storage[source.LocalIndex][supply.Stack] -= supply.Count;
            else Add(supply.Stack, -supply.Count);
    }
    private bool HoldsWorkSupplies(WorkOrder order, long actor)
    {
        if (actor > 0 && order.Output?.Carrier == actor) return true;
        if (actor <= 0 || order.Supplies == null) return false;
        foreach (var supply in order.Supplies) if (supply.Phase == SupplyPhase.Carrying && supply.Carrier == actor) return true;
        return false;
    }
    private long ReservedCargo(long actor, string? stack = null)
    {
        long count = 0;
        if (ActorById(actor).Occupation is { } occupation)
            count += stack == null ? occupation.Carried.Values.Sum(v => (long)v) : occupation.Carried.GetValueOrDefault(stack);
        foreach (var order in State.Work)
        {
            if (order.Supplies != null) foreach (var supply in order.Supplies)
                if (supply.Phase == SupplyPhase.Carrying && supply.Carrier == actor && (stack == null || supply.Stack == stack)) count += supply.Count;
            if (order.Output is { } output && output.Carrier == actor)
                count += stack == null ? output.Remaining.Values.Sum(v => (long)v) : output.Remaining.GetValueOrDefault(stack);
        }
        return count;
    }
    private long FreeCargo(Actor actor, string stack)
    { int amount = actor.Cargo.GetValueOrDefault(stack); return amount == 0 ? 0 : amount - ReservedCargo(actor.Id, stack); }
    public string WorkSupplyDescription(WorkOrder order)
    {
        if (order.Output != null) return WorkOutputDescription(order);
        if (order.Reserved.Count == 0) return "No supplies needed.";
        if (order.Supplies == null) return "Reserved supplies are at the site.";
        int waiting = 0, carrying = 0, delivered = 0;
        foreach (var supply in order.Supplies)
            switch (supply.Phase) { case SupplyPhase.Waiting: waiting += supply.Count; break; case SupplyPhase.Carrying: carrying += supply.Count; break; default: delivered += supply.Count; break; }
        return string.Join(", ", order.Reserved.Select(p => p.Value + " " + p.Key)) + $". Supplies: {waiting} awaiting pickup / {carrying} carried / {delivered} at site.";
    }
    private WorkSupply? NextSupply(WorkOrder order, long worker)
    {
        if (order.Supplies == null) return null;
        WorkSupply? waiting = null;
        foreach (var supply in order.Supplies)
        {
            if (supply.Phase == SupplyPhase.Carrying && supply.Carrier != worker) return supply;
            if (waiting == null && supply.Phase == SupplyPhase.Waiting) waiting = supply;
        }
        return waiting;
    }
    private WorldPosition WorkStop(WorkOrder order, long worker)
    {
        if (order.Output is { } output) return output.Carrier != worker ? ActorById(output.Carrier).Position : order.DeliveryStorage ?? State.Position;
        var supply = NextSupply(order, worker);
        return supply == null ? order.Target : supply.Phase == SupplyPhase.Carrying ? ActorById(supply.Carrier).Position : supply.Storage ?? State.Position;
    }
    private bool SupplySourceChanged(WorkOrder order)
    {
        if (order.Supplies == null) return false;
        foreach (var supply in order.Supplies) if (supply.Phase == SupplyPhase.Waiting && supply.Storage is { } p && Chunks.At(p) != Material.Storage) return true;
        return false;
    }
    private bool StorageReservedForWork(WorldPosition p) => State.Work.Any(o => o.Supplies?.Any(s => s.Phase == SupplyPhase.Waiting && s.Storage == p) == true);
    private bool AdvanceWorkSupplies(WorkOrder order, Actor worker)
    {
        if (order.Supplies == null || order.Supplies.Count == 0) return true;
        var supply = NextSupply(order, worker.Id);
        while (supply != null)
        {
            if (supply.Phase == SupplyPhase.Waiting && supply.Storage == null && State.Position.Distance(order.Target) > 100)
            { order.Status = "Shared pack is away; waiting for supplies to return to the work area"; return false; }
            var stop = WorkStop(order, worker.Id);
            if (!ApproachWorkStop(order, worker, stop, "Collecting supplies")) return false;
            if (supply.Phase == SupplyPhase.Carrying)
            {
                var previous = ActorById(supply.Carrier);
                previous.Cargo[supply.Stack] -= supply.Count;
            }
            worker.Cargo[supply.Stack] = worker.Cargo.GetValueOrDefault(supply.Stack) + supply.Count;
            supply.Phase = SupplyPhase.Carrying; supply.Carrier = worker.Id;
            order.Status = "Collected " + supply.Count + " " + supply.Stack + ": " + worker.Name;
            Diagnose("work-supply-picked-up", new { order.Id, supply.Stack, supply.Count, worker = worker.Id, source = stop });
            supply = NextSupply(order, worker.Id);
        }
        if (!order.Supplies.Any(s => s.Phase == SupplyPhase.Carrying)) return true;
        if (!ApproachWorkStop(order, worker, order.Target, "Hauling supplies")) return false;
        foreach (var carried in order.Supplies.Where(s => s.Phase == SupplyPhase.Carrying))
        {
            worker.Cargo[carried.Stack] -= carried.Count; carried.Carrier = 0; carried.Phase = SupplyPhase.Delivered;
        }
        order.Status = "Supplies delivered: " + worker.Name;
        Diagnose("work-supplies-delivered", new { order.Id, worker = worker.Id, order.Target });
        return true;
    }
    private bool ApproachWorkStop(WorkOrder order, Actor worker, WorldPosition target, string activity)
    {
        if (worker.Position.Distance(target) <= 1) { MarkResidentBlocked(worker, false); return true; }
        order.Status = activity + ": " + worker.Name;
        if (worker.Recruited)
        {
            var path = WorkPath(worker, target);
            if (path == null)
            {
                order.Status = "No free observed approach for " + activity.ToLowerInvariant() + "; open access or move blockers";
                if (!order.PinnedWorker && !HoldsWorkSupplies(order, worker.Id)) order.Worker = 0;
            }
            else if (path.Count > 0) { MoveWorker(worker, path[0]); Contact(); }
        }
        else if (State.Tick % 5 == 0 && worker.Position.Floor == target.Floor)
        {
            var next = ResidentStep(worker, target); MarkResidentBlocked(worker, next == worker.Position);
            if (next == worker.Position) order.Status = "No free approach for " + activity.ToLowerInvariant() + "; waiting for access";
            else CommitResidentStep(worker, next);
        }
        return false;
    }
    private void ReturnWorkSupplies(WorkOrder order)
    {
        if (order.Supplies == null) { foreach (var (stack, count) in order.Reserved) Add(stack, count); return; }
        var dropped = new Dictionary<WorldPosition, Dictionary<string, int>>();
        foreach (var supply in order.Supplies)
        {
            WorldPosition location = supply.Phase switch
            {
                SupplyPhase.Waiting => supply.Storage ?? State.Position,
                SupplyPhase.Carrying => ActorById(supply.Carrier).Position,
                _ => order.Target
            };
            if (supply.Phase == SupplyPhase.Delivered && State.Discoveries.Any(d => d.Position == location))
                location = State.Actors.FirstOrDefault(a => a.Id == order.Worker && a.Position.Distance(order.Target) <= 1)?.Position
                    ?? Directions.Select(d => order.Target.Offset(d.X, d.Y)).FirstOrDefault(GroundOpen, order.Target);
            if (supply.Phase == SupplyPhase.Carrying) ActorById(supply.Carrier).Cargo[supply.Stack] -= supply.Count;
            int returned = 0;
            if (supply.Storage is { } source && location.Distance(source) <= 1 && Chunks.At(source) == Material.Storage)
            {
                var contents = Chunks.ExistingDelta(source.Chunk)!.Storage[source.LocalIndex];
                returned = Math.Min(supply.Count, Math.Max(0, 9999 - contents.GetValueOrDefault(supply.Stack)));
                contents[supply.Stack] = contents.GetValueOrDefault(supply.Stack) + returned;
            }
            else if (supply.Storage == null && location.Distance(State.Position) <= 1)
            { returned = Math.Min(supply.Count, Math.Max(0, 9999 - Count(supply.Stack))); Add(supply.Stack, returned); }
            if (returned < supply.Count)
            {
                if (!dropped.TryGetValue(location, out var contents)) dropped[location] = contents = [];
                contents[supply.Stack] = contents.GetValueOrDefault(supply.Stack) + supply.Count - returned;
            }
        }
        int index = 0;
        foreach (var (position, contents) in dropped)
        {
            DropWorkBundle(order.Id, index++, position, contents);
        }
    }
    private void DropWorkBundle(long order, int index, WorldPosition position, Dictionary<string, int> contents)
    {
        var existing = State.Loot.FirstOrDefault(c => !c.Taken && c.Position == position && IsWorkSupplyCache(c));
        if (existing == null) State.Loot.Add(new() { Id = $"work-return-{order}-{index}", Position = position, Contents = contents });
        else foreach (var (stack, count) in contents) existing.Contents[stack] = existing.Contents.GetValueOrDefault(stack) + count;
    }
    // Explicit ownership view for construction audits. Reserved cargo is counted
    // once, and a built structure retains the cost recoverable by dismantling.
    public long AccountedMaterial(string stack) => Count(stack)
        + State.Towns.Sum(t => (long)t.Stocks.GetValueOrDefault(stack))
        + State.Actors.Sum(a => (long)a.Cargo.GetValueOrDefault(stack))
        + State.Chunks.Sum(c => c.Storage.Values.Sum(v => (long)v.GetValueOrDefault(stack)))
        + State.Work.Sum(o => o.Output != null ? 0 : o.Supplies == null ? (long)o.Reserved.GetValueOrDefault(stack)
            : o.Supplies.Where(s => s.Stack == stack && s.Phase != SupplyPhase.Carrying).Sum(s => (long)s.Count))
        + State.Structures.Sum(s => (long)s.Cost.GetValueOrDefault(stack))
        + State.Loot.Where(c => !c.Taken).Sum(c => (long)c.Contents.GetValueOrDefault(stack));

    private static void ValidateWorkSupplies(WorldPlan plan, WorldState state, ContentCatalog catalog)
    {
        var stacks = catalog.All<ItemDefinition>().Select(i => i.Stack).ToHashSet(StringComparer.Ordinal);
        var actors = state.Actors.ToDictionary(a => a.Id);
        var claims = new Dictionary<(long Actor, string Stack), long>();
        foreach (var order in state.Work)
        {
            if (order.SupplyVersion == 0 && order.Supplies == null) continue;
            if (order.SupplyVersion != 1 || order.Supplies == null || order.Supplies.Count > 64 || order.Worker > 0 && actors[order.Worker].Kind != ActorKind.Person
                || order.Supplies.Any(s => s == null || s.Stack == null || !stacks.Contains(s.Stack) || s.Count is < 1 or > 9999 || !Enum.IsDefined(s.Phase)
                    || s.Storage is { } p && (!plan.Contains(p) || !state.Chunks.Any(c => c.Key == p.Chunk && c.Discovered.Contains(p.LocalIndex)))
                    || (s.Phase == SupplyPhase.Carrying ? !actors.ContainsKey(s.Carrier) || actors[s.Carrier].Kind != ActorKind.Person : s.Carrier != 0)))
                throw new InvalidDataException("Invalid work supply ownership.");
            var totals = order.Supplies.GroupBy(s => s.Stack).ToDictionary(g => g.Key, g => g.Sum(s => (long)s.Count));
            if (totals.Count != order.Reserved.Count || order.Reserved.Any(p => totals.GetValueOrDefault(p.Key) != p.Value)
                || order.Progress > 0 && order.Supplies.Any(s => s.Phase != SupplyPhase.Delivered)) throw new InvalidDataException("Work supplies do not match their reserved cost or progress.");
            if (order.Kind is WorkKind.Build or WorkKind.Craft && catalog.Entries.GetValueOrDefault(order.Definition) is RecipeDefinition recipe)
            {
                var cost = recipe.Cost.ToDictionary(p => catalog.Get<ItemDefinition>(p.Key).Stack, p => p.Value);
                if (cost.Count != order.Reserved.Count || cost.Any(p => order.Reserved.GetValueOrDefault(p.Key) != p.Value)) throw new InvalidDataException("Reserved supplies differ from the work recipe.");
            }
            foreach (var supply in order.Supplies.Where(s => s.Phase == SupplyPhase.Carrying))
            { var key = (supply.Carrier, supply.Stack); claims[key] = claims.GetValueOrDefault(key) + supply.Count; }
        }
        foreach (var order in state.Work)
            if (order.Output is { } output) foreach (var (stack, count) in output.Remaining)
            { var key = (output.Carrier, stack); claims[key] = claims.GetValueOrDefault(key) + count; }
        foreach (var actor in state.Actors)
            if (actor.Occupation is { } occupation) foreach (var (stack, count) in occupation.Carried)
            { var key = (actor.Id, stack); claims[key] = claims.GetValueOrDefault(key) + count; }
        foreach (var (key, count) in claims)
            if (actors[key.Actor].Cargo.GetValueOrDefault(key.Stack) < count) throw new InvalidDataException("Reserved work cargo is missing or claimed twice.");
    }
    public static bool IsWorkSupplyCache(LootCache cache) => cache.SourceActor == 0 && cache.Id.StartsWith("work-return-", StringComparison.Ordinal);
}
