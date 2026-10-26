using Roag.Core.Content;

namespace Roag.Core.World;

public static class WorkSupplyPreview
{
    public static (WorldPosition Storage, WorldPosition Target) Prepare(WorldSession world)
    {
        var p = world.State.Position; int parked = 0;
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited && a.Position.Distance(p) <= 100))
        { actor.Position = p.Offset(110 + parked++ * 2, 20); world.Chunks.Edit(actor.Position, Material.Floor); }
        for (int y = -12; y <= 12; y++) for (int x = -12; x <= 12; x++) world.Chunks.Edit(p.Offset(x, y), Material.Floor);
        var storage = p.Offset(-5, 0); var target = p.Offset(5, 0);
        world.Chunks.Edit(storage, Material.Storage); world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex] = new() { ["stone"] = 12 };
        world.State.Inventory["stone"] = 0; world.Reveal(); world.Paused = true; return (storage, target);
    }
    public static object Run(ContentCatalog content, int seed)
    {
        using var world = WorldSession.Create(seed, content); var (storage, target) = Prepare(world);
        long initial = world.AccountedMaterial("stone"); var queued = world.DesignateWork(WorldCommandKind.Build, [target]);
        if (queued.Queued != 1) throw new InvalidDataException(queued.Message);
        var order = world.State.Work.Single(); var stages = new List<object>(); SupplyPhase? last = null;
        void Observe()
        {
            var supply = order.Supplies!.Single();
            if (supply.Phase == last) return; last = supply.Phase;
            stages.Add(new { world.State.Tick, phase = supply.Phase.ToString(), supply.Count, supply.Stack, supply.Carrier,
                position = world.State.Position, order.Progress, order.Status, balance = world.AccountedMaterial("stone") });
        }
        Observe(); world.Paused = false;
        for (int i = 0; i < 120 && world.State.Work.Count > 0; i++)
        {
            if (world.AdvanceTicks(1) != 1) throw new InvalidDataException("The supply preview stopped advancing.");
            if (world.AccountedMaterial("stone") != initial) throw new InvalidDataException("The supply preview lost or duplicated material.");
            Observe();
        }
        bool passed = stages.Count == 3 && world.State.Work.Count == 0 && world.Chunks.At(target) == Material.Wall && world.AccountedMaterial("stone") == initial;
        return new { passed, seed, storage, target, stages, initialMaterial = initial, finalMaterial = world.AccountedMaterial("stone"), world.State.Tick,
            scope = "Actual storage reservation, pickup, actor cargo, delivery and construction on an isolated plaza. No player saves, rendering or model calls." };
    }
}
