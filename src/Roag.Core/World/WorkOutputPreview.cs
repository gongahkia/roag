using Roag.Core.Content;

namespace Roag.Core.World;

/// <summary>A natural harvest followed by real actor cargo and storage delivery.</summary>
public static class WorkOutputPreview
{
    public static (WorldPosition Resource, WorldPosition Storage) Prepare(WorldSession world)
    {
        var target = ResourcePreview.Prepare(world, "resource.apple-tree"); var origin = world.State.Position;
        for (int y = -12; y <= 12; y++) for (int x = -12; x <= 12; x++)
        {
            var p = origin.Offset(x, y);
            if (p != target && world.Plan.Contains(p)) world.Chunks.Edit(p, Material.Floor);
        }
        var storage = origin.Offset(7, 0); world.Chunks.Edit(storage, Material.Storage);
        world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex] = [];
        world.RevealSurface(origin.X - 12, origin.Y - 12, origin.X + 13, origin.Y + 13);
        world.Paused = true; return (target, storage);
    }
    public static object Run(ContentCatalog content, int seed)
    {
        using var world = WorldSession.Create(seed, content); var (target, storage) = Prepare(world);
        int initialStock = world.ResourceAt(target)!.Available, initialPack = world.Count("apple"); long balance = world.AccountedMaterial("apple");
        var queued = world.DesignateWork(WorldCommandKind.Harvest, [target]);
        if (queued.Queued != 1) throw new InvalidDataException(queued.Message);
        var order = world.State.Work.Single();
        if (!world.Submit(new(WorldCommandKind.ConfigureWork, storage, WorkId: order.Id, Item: "deliver-storage")).Success)
            throw new InvalidDataException("The output preview could not select its delivery storage.");
        var stages = new List<object>(); bool extracted = false; world.Paused = false;
        for (int i = 0; i < 160 && world.State.Work.Count > 0; i++)
        {
            if (world.AdvanceTicks(1) != 1) throw new InvalidDataException("The output preview stopped advancing.");
            if (!extracted && order.Output is { } output)
            {
                extracted = true; var actor = world.ActorById(output.Carrier);
                stages.Add(new { world.State.Tick, phase = "Carrying", carrier = actor.Id, actor.Position, cargo = new Dictionary<string, int>(output.Remaining), order.Progress, order.Status });
            }
            if (world.AccountedMaterial("apple") != balance + (extracted ? 2 : 0))
                throw new InvalidDataException("The output preview lost or duplicated material.");
        }
        var contents = world.Chunks.Delta(storage.Chunk).Storage[storage.LocalIndex];
        bool passed = extracted && world.State.Work.Count == 0 && contents.GetValueOrDefault("apple") == 2
            && world.Count("apple") == initialPack && world.ResourceAt(target)!.Available == initialStock - 1;
        stages.Add(new { world.State.Tick, phase = "Delivered", storage, contents = new Dictionary<string, int>(contents) });
        return new { passed, seed, target, storage, stages, initialStock, finalStock = world.ResourceAt(target)!.Available,
            initialMaterial = balance, finalMaterial = world.AccountedMaterial("apple"), world.State.Tick,
            scope = "A naturally generated apple tree, one extraction, actual worker cargo and delivery to observed storage. No player saves, rendering or model calls." };
    }
}
