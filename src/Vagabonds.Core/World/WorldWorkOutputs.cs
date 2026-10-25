using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public IReadOnlyList<WorldPosition> WorkDeliveryStorage()
    {
        var rally = State.WorkRally ?? State.Position; var result = new HashSet<WorldPosition>();
        int left = Math.Max(0, rally.X - WorkRallyRadius) / 64, right = Math.Min(WorldPlan.Size - 1, rally.X + WorkRallyRadius) / 64;
        int top = Math.Max(0, rally.Y - WorkRallyRadius) / 64, bottom = Math.Min(WorldPlan.Size - 1, rally.Y + WorkRallyRadius) / 64;
        for (int y = top; y <= bottom; y++) for (int x = left; x <= right; x++)
            if (Chunks.ExistingDelta(new(x, y, rally.Floor)) is { } delta)
                foreach (int index in delta.Edits.Where(e => e.Value == Material.Storage).Select(e => e.Key).Concat(delta.Storage.Keys))
                {
                    var p = new WorldPosition(x * 64 + index % 64, y * 64 + index / 64, rally.Floor);
                    if (InWorkRally(p) && Chunks.Seen(p) && Chunks.At(p) == Material.Storage) result.Add(p);
                }
        return result.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray();
    }
    public string WorkDeliveryReason(WorkOrder order, WorldPosition? storage)
    {
        if (!order.Yield.Values.Any(v => v > 0)) return "This work does not produce material to deliver.";
        if (storage == null) return "";
        var p = storage.Value;
        if (!Plan.Contains(p) || !Chunks.Seen(p) || !InWorkRally(p) || Chunks.At(p) != Material.Storage) return "Choose observed storage in the work rally.";
        if (order.Kind == WorkKind.Dig && order.Target == p) return "Choose storage that will remain after this work.";
        return "";
    }
    public string WorkOutputDescription(WorkOrder order)
    {
        if (order.OutputVersion == 0) return "";
        string destination = order.DeliveryStorage is { } p ? $"storage at {p.X}, {p.Y}" : "the shared pack";
        return order.Output is { } output
            ? "Delivering " + string.Join(", ", output.Remaining.Select(p => p.Value + " " + p.Key)) + " to " + destination + "."
            : "Output goes to " + destination + ".";
    }
    private void AdvanceWorkOutput(WorkOrder order, Actor worker)
    {
        var output = order.Output!;
        if (output.Carrier != worker.Id)
        {
            var previous = ActorById(output.Carrier);
            if (!ApproachWorkStop(order, worker, previous.Position, "Recovering work output")) return;
            foreach (var (stack, count) in output.Remaining)
            {
                previous.Cargo[stack] -= count; worker.Cargo[stack] = worker.Cargo.GetValueOrDefault(stack) + count;
            }
            output.Carrier = worker.Id;
            Diagnose("work-output-handoff", new { order.Id, previous = previous.Id, worker = worker.Id });
        }
        Dictionary<string, int>? storageContents = null; var destination = State.Position;
        if (order.DeliveryStorage is { } storage)
        {
            if (Chunks.At(storage) != Material.Storage) { order.Status = "Delivery storage unavailable; choose another destination"; return; }
            destination = storage;
        }
        else if (worker.Position.Distance(destination) > 100)
        { order.Status = "Shared pack is away; waiting to deliver output"; return; }
        if (!ApproachWorkStop(order, worker, destination, "Delivering output")) return;
        if (order.DeliveryStorage is { } target)
        {
            var delta = Chunks.Delta(target.Chunk);
            if (!delta.Storage.TryGetValue(target.LocalIndex, out storageContents)) delta.Storage[target.LocalIndex] = storageContents = [];
        }
        foreach (var (stack, count) in output.Remaining.ToArray())
        {
            int current = storageContents?.GetValueOrDefault(stack) ?? Count(stack);
            int amount = Math.Min(count, Math.Max(0, 9999 - current));
            if (amount <= 0) continue;
            if (storageContents != null) storageContents[stack] = current + amount; else Add(stack, amount);
            worker.Cargo[stack] -= amount; output.Delivered[stack] = output.Delivered.GetValueOrDefault(stack) + amount;
            if (amount == count) output.Remaining.Remove(stack); else output.Remaining[stack] -= amount;
        }
        if (output.Remaining.Count > 0) { order.Status = "Delivery destination full; remaining output stays with " + worker.Name; return; }
        Diagnose("work-output-delivered", new { order.Id, worker = worker.Id, order.DeliveryStorage, delivered = new Dictionary<string, int>(output.Delivered) });
        FinishWork(order, worker);
    }
    private void ReturnWorkOutput(WorkOrder order)
    {
        var output = order.Output!; var carrier = ActorById(output.Carrier); var remaining = new Dictionary<string, int>();
        Dictionary<string, int>? storage = null;
        bool nearbyPack = order.DeliveryStorage == null && carrier.Position.Distance(State.Position) <= 1;
        if (order.DeliveryStorage is { } p && carrier.Position.Distance(p) <= 1 && Chunks.At(p) == Material.Storage)
        {
            var delta = Chunks.Delta(p.Chunk);
            if (!delta.Storage.TryGetValue(p.LocalIndex, out storage)) delta.Storage[p.LocalIndex] = storage = [];
        }
        foreach (var (stack, count) in output.Remaining)
        {
            carrier.Cargo[stack] -= count;
            int amount = storage != null || nearbyPack ? Math.Min(count, Math.Max(0, 9999 - (storage?.GetValueOrDefault(stack) ?? Count(stack)))) : 0;
            if (storage != null) storage[stack] = storage.GetValueOrDefault(stack) + amount; else if (nearbyPack) Add(stack, amount);
            if (amount < count) remaining[stack] = count - amount;
        }
        if (remaining.Count > 0) DropWorkBundle(order.Id, 0, carrier.Position, remaining);
    }
    private static void ValidateWorkOutputs(WorldPlan plan, WorldState state, ContentCatalog catalog)
    {
        var stacks = catalog.All<ItemDefinition>().Select(i => i.Stack).ToHashSet(StringComparer.Ordinal);
        var actors = state.Actors.ToDictionary(a => a.Id);
        foreach (var order in state.Work)
        {
            if (order.OutputVersion == 0 && order.Output == null && order.DeliveryStorage == null) continue;
            if (order.OutputVersion != 1 || !order.Yield.Values.Any(v => v > 0) || order.Kind is not (WorkKind.Dig or WorkKind.Harvest or WorkKind.Forage or WorkKind.Craft)
                || order.DeliveryStorage is { } p && (!plan.Contains(p) || !state.Chunks.Any(c => c.Key == p.Chunk && c.Discovered.Contains(p.LocalIndex))))
                throw new InvalidDataException("Invalid work output destination or version.");
            if (order.Output is not { } output) continue;
            if (output.ProducedTick < 1 || output.ProducedTick > state.Tick || !actors.TryGetValue(output.Carrier, out var carrier) || carrier.Kind != ActorKind.Person
                || order.Progress != order.Required || output.Remaining == null || output.Delivered == null || output.Remaining.Count == 0
                || output.Remaining.Any(p => !stacks.Contains(p.Key) || p.Value is < 1 or > 9999 || !order.Yield.ContainsKey(p.Key))
                || output.Delivered.Any(p => !stacks.Contains(p.Key) || p.Value is < 1 or > 9999 || !order.Yield.ContainsKey(p.Key))
                || order.Yield.Any(p => (long)output.Remaining.GetValueOrDefault(p.Key) + output.Delivered.GetValueOrDefault(p.Key) != p.Value))
                throw new InvalidDataException("Invalid extracted work output ownership.");
        }
    }
}
