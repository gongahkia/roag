using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    private ActionResult QueueBuild(WorldCommand command, bool designated = false)
    {
        var p = command.Target;
        if (!Plan.Contains(p) || p.Distance(State.Position) > (designated ? 100 : 1) || !Chunks.Seen(p)) return new(false, "Choose observed adjacent ground.");
        var before = Chunks.At(p);
        var recipe = Content.Game.Recipes.Select(Content.Get<RecipeDefinition>).FirstOrDefault(r => r.Output == null && r.Material == command.Material);
        if (recipe == null || before is Material.StairsDown or Material.StairsUp || before == command.Material || State.Structures.Any(s => s.Position == p)
            || !GroundOpen(p) && before != Material.Water || before == Material.Water && command.Material != Material.Bridge)
            return new(false, "Choose clear ground, or bridge water; preserve stairs. Dismantle owned structures before replacing them.");
        if ((!Materials.Walkable(command.Material) || command.Material == Material.Camp) && State.Actors.Any(a => a.Life != LifeState.Dead && a.Position == p)) return new(false, "Someone is standing there.");
        return QueueWork(new() { Kind = WorkKind.Build, PartyApproach = designated, Definition = recipe.Id, Target = p, Before = before, After = command.Material, Required = recipe.Work,
            Reserved = RecipeCost(recipe) });
    }
    private Dictionary<string, int> RecipeCost(RecipeDefinition recipe) => recipe.Cost.ToDictionary(p => Content.Get<ItemDefinition>(p.Key).Stack, p => p.Value);
    private ActionResult QueueDig(WorldPosition p, bool designated = false)
    {
        if (!Plan.Contains(p) || State.Position.Distance(p) > (designated ? 100 : 1) || !Chunks.Seen(p)) return new(false, "Choose observed adjacent terrain.");
        var before = Chunks.At(p); var owned = State.Structures.FirstOrDefault(s => s.Position == p);
        bool prop = ObjectAt(p) is { Removable: true };
        if (!prop && owned == null && before is not (Material.Rock or Material.Wall or Material.Tree or Material.Door or Material.Storage or Material.Camp)) return new(false, "Choose rock, walls, trees or an owned structure.");
        if (before == Material.Storage && State.Chunks.FirstOrDefault(c => c.Key == p.Chunk)?.Storage.GetValueOrDefault(p.LocalIndex)?.Values.Any(v => v > 0) == true) return new(false, "Empty the storage first.");
        int hardness = prop ? 1 : Materials.Hardness(before);
        return QueueWork(new() { Kind = WorkKind.Dig, PartyApproach = designated, Target = p, Before = before, After = prop ? before : owned?.Foundation ?? (p.Floor == 0 ? Material.Earth : Material.Floor),
            Required = hardness * 5, Reserved = owned != null ? [] : new() { ["food"] = hardness },
            Yield = owned != null ? new(owned.Cost) : new() { [prop ? "wood" : Materials.Yield(before)] = prop ? 1 : hardness + 1 } });
    }
    private ActionResult QueueCraft(string id)
    {
        var recipe = Content.Game.Recipes.Select(Content.Get<RecipeDefinition>).FirstOrDefault(r => r.Id == id && r.Output != null);
        if (recipe == null || !NearbyCamp(State.Position)) return new(false, "Choose a crafting recipe beside a serviceable camp.");
        var station = new[] { State.Position }.Concat(Directions.Select(d => State.Position.Offset(d.X, d.Y))).First(p => Chunks.At(p) == Material.Camp);
        return QueueWork(new() { Kind = WorkKind.Craft, Definition = recipe.Id, Target = station, Before = Material.Camp, After = Material.Camp, Required = recipe.Work,
            Reserved = RecipeCost(recipe), Yield = new() { [Content.Get<ItemDefinition>(recipe.Output!).Stack] = recipe.Amount } });
    }
    private ActionResult QueueRepair(WorldPosition p, bool designated = false)
    {
        var structure = State.Structures.FirstOrDefault(s => s.Position == p);
        if (structure == null || structure.Condition >= 95 || State.Position.Distance(p) > (designated ? 100 : 1)) return new(false, "Choose an adjacent worn structure.");
        return QueueWork(new() { Kind = WorkKind.Repair, PartyApproach = designated, Target = p, Before = structure.Material, After = structure.Material, Required = 12,
            Reserved = new() { [structure.Material is Material.Wall or Material.Floor ? "stone" : "wood"] = 1 } });
    }
    private ActionResult QueueForage()
    {
        if (Party.First(a => a.Life == LifeState.Alive).Body.ForageAfter > State.Tick || !Materials.Walkable(Chunks.At(State.Position))) return new(false, "Let the nearby gathering grounds recover for an hour.");
        return QueueWork(new() { Kind = WorkKind.Forage, Target = State.Position, Before = Chunks.At(State.Position), After = Chunks.At(State.Position), Required = Simulation.ForageWork, Yield = new() { ["food"] = 2 } });
    }
    private ActionResult QueueWork(WorkOrder order)
    {
        if (State.Work.Count >= 64 || State.Work.Any(w => w.Target == order.Target)) return new(false, "A work order already occupies this cell, or the work list is full.");
        if (order.Reserved.Any(p => Count(p.Key) < p.Value)) return new(false, "Required supplies: " + string.Join(", ", order.Reserved.Select(p => p.Value + " " + p.Key)) + ".");
        foreach (var (item, amount) in order.Reserved) Add(item, -amount);
        order.Id = State.NextWorkId++; order.PartyApproach = true;
        State.WorkRally ??= State.Position;
        order.Worker = Party.FirstOrDefault(a => a.Life == LifeState.Alive && !State.Work.Any(o => o.Worker == a.Id) && a.Position.Distance(order.Target) <= 1)?.Id ?? 0;
        State.Work.Add(order);
        return new(true, $"{order.Kind} queued; supplies reserved. Play or Wait advances work; Work assigns workers or cancels orders.");
    }
    private ActionResult AssignWork(long workId, long actorId)
    {
        var order = State.Work.FirstOrDefault(w => w.Id == workId); var actor = State.Actors.FirstOrDefault(a => a.Id == actorId);
        if (order == null || actor == null || actor.Life != LifeState.Alive || actor.Kind != ActorKind.Person || State.Work.Any(w => w.Id != workId && w.Worker == actorId)
            || actor.Position.Distance(order.Target) > 100) return new(false, "Choose a living nearby person without another work order.");
        order.Worker = actor.Id; order.PinnedWorker = true; order.PartyApproach = actor.Recruited; order.Suspended = false; order.Status = "Assigned to " + actor.Name; return new(true, order.Status + ". Their usual job pauses until this is finished.");
    }
    private ActionResult CancelWork(long id)
    {
        var order = State.Work.FirstOrDefault(w => w.Id == id);
        if (order == null) return new(false, "Choose a pending order.");
        foreach (var (item, amount) in order.Reserved) Add(item, amount);
        State.Work.Remove(order); return new(true, "Work cancelled; reserved supplies returned once.");
    }
    private void AdvanceWork()
    {
        DispatchPartyWork();
        foreach (var order in State.Work.OrderByDescending(o => o.Priority).ThenBy(o => o.Id).ToArray())
        {
            if (Chunks.At(order.Target) != order.Before || order.Kind == WorkKind.Repair && !State.Structures.Any(s => s.Position == order.Target))
            { CancelWork(order.Id); Log("A changed work target released its reserved supplies."); continue; }
            if (order.Suspended) { order.Status = "Suspended"; continue; }
            var worker = State.Actors.FirstOrDefault(a => a.Id == order.Worker && a.Life == LifeState.Alive);
            if (worker == null) { order.Worker = 0; continue; }
            if (worker.Recruited && (PartyTravelling || !order.PartyApproach && worker.Position.Distance(order.Target) > 1 || !InWorkRally(order.Target))) { order.Status = PartyTravelling ? "Travelling with the party" : "Outside rally area or work stopped"; continue; }
            if (State.Battle?.ActorIds.Contains(worker.Id) == true) { order.Status = "Worker in combat"; continue; }
            if (order.Kind == WorkKind.Forage && worker.Body.ForageAfter > State.Tick) { order.Status = "Gathering grounds recovering"; continue; }
            if (worker.Body.RestUntil > State.Tick || worker.Body.Fatigue >= 75 || worker.Body.Hunger >= Simulation.StarveAt || worker.Health < worker.MaxHealth / 4)
            { order.Status = "Worker needs food, rest or treatment"; continue; }
            if (worker.Position.Distance(order.Target) > 1)
            {
                order.Status = "Approaching: " + worker.Name;
                if (worker.Recruited)
                {
                    var path = WorkPath(worker, order.Target);
                    if (path == null) { order.Status = "No free observed approach; open access or move blockers"; if (!order.PinnedWorker) order.Worker = 0; }
                    else if (path.Count > 0) { MoveWorker(worker, path[0]); Contact(); }
                }
                else if (State.Tick % 5 == 0 && worker.Position.Floor == order.Target.Floor) worker.Position = LocalStep(worker.Position, order.Target, worker.Id);
                continue;
            }
            if (order.Kind is WorkKind.Build or WorkKind.Dig && (!Materials.Walkable(order.After) || order.After == Material.Camp) && State.Actors.Any(a => a.Life != LifeState.Dead && a.Position == order.Target))
            { order.Status = "Clear the target cell"; continue; }
            order.Status = "Working: " + worker.Name; double amount = WorkRate(worker);
            order.Progress += amount; worker.Body.WorkDone += amount;
            if (order.Progress < order.Required) continue;
            switch (order.Kind)
            {
                case WorkKind.Build:
                    Chunks.Edit(order.Target, order.After);
                    State.Structures.Add(new() { Position = order.Target, Material = order.After, Foundation = order.Before, Cost = new(order.Reserved) }); break;
                case WorkKind.Dig:
                    Chunks.Edit(order.Target, order.After); State.Structures.RemoveAll(s => s.Position == order.Target); break;
                case WorkKind.Repair:
                    State.Structures.Single(s => s.Position == order.Target).Condition = 100; break;
                case WorkKind.Forage: worker.Body.ForageAfter = State.Tick + TicksPerHour; break;
            }
            foreach (var (item, count) in order.Yield) Add(item, count);
            Remember(worker, "finished-work", "My work made a difference.", Disposition(worker)?.Value == "craft" ? 8 : 4);
            var leader = Party.FirstOrDefault(a => a.Life == LifeState.Alive);
            if (leader != null && leader.Id != worker.Id && Remember(worker, "helped-party", "I helped " + leader.Name + "'s expedition.", 3, leader.Id)) ChangeTrust(worker, leader, 3);
            State.Work.Remove(order); Log(worker.Name + " completed " + order.Kind.ToString().ToLowerInvariant() + "."); Reveal();
        }
        ReturnIdleParty();
        foreach (var structure in State.Structures) structure.Condition = Math.Max(0, structure.Condition - Simulation.StructureWearPerTick);
    }
    private ActionResult Wait(int ticks)
    {
        if (ticks is < 1 or > TicksPerHour) return new(false, "Wait between one minute and one hour.");
        long before = State.Tick;
        for (int i = 0; i < ticks && !IsInCombat && !State.AwaitingContinuation && !State.Endgame.Victory && !DeathPending; i++) AdvanceWorldTick();
        return new(true, $"Waited {(State.Tick - before) / 5.0:0.#} minutes. Nearby danger interrupts waiting.");
    }
}
