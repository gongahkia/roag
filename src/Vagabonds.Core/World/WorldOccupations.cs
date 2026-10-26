using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public const int OccupationRadius = 64;
    private readonly Dictionary<int, List<WorldPosition>[]> _occupationSources = [];
    private static bool ProductiveOccupation(Actor actor) => actor.Job is "farmer" or "woodcutter" or "quarrier" or "healer";
    private WorldPosition OccupationHome(Actor actor) => Plan.Towns[actor.HomeTown].Center;
    private WorldPosition OccupationStop(Actor actor, OccupationJob job) => job.Phase is OccupationPhase.Collecting or OccupationPhase.Delivering or OccupationPhase.Returning
        ? OccupationHome(actor) : job.Target;
    private bool OccupationActive(Actor actor) => !actor.Recruited && actor.Life == LifeState.Alive && actor.Occupation != null
        && actor.Routine?.Kind == RoutineKind.Occupation && actor.Position.Floor == 0 && !HasWork(actor);
    private void ActivateOccupation(Actor actor)
    { if (actor.OccupationVersion == 0 && ProductiveOccupation(actor)) actor.OccupationVersion = 1; }
    private List<WorldPosition>[] OccupationSources(int town)
    {
        if (_occupationSources.TryGetValue(town, out var existing)) return existing;
        List<WorldPosition>[] result = [[], [], []]; var center = Plan.Towns[town].Center;
        // The world has 24 towns and at most 8,321 source coordinates per town.
        // Candidate terrain is derived once; edits, stock and claims stay live.
        for (int radius = 0; radius <= OccupationRadius; radius++)
            for (int y = -radius; y <= radius; y++)
                for (int side = 0; side < (Math.Abs(y) == radius ? 1 : 2); side++)
                {
                    int x = (radius - Math.Abs(y)) * (side == 0 ? 1 : -1); var p = center.Offset(x, y);
                    if (!Plan.Contains(p) || Chunks.IsEdited(p)) continue;
                    var material = Chunks.At(p);
                    if (material is not (Material.Grass or Material.Tree or Material.Rock)) continue;
                    if (ResourceAt(p) is { } resource)
                    {
                        if (resource.Definition.Yield.Count == 1 && resource.Definition.Yield.Any(v => Content.Get<ItemDefinition>(v.Key).Stack == "food")) result[0].Add(p);
                    }
                    else if (ObjectAt(p) is not { Kind: "discovery" or "loot" })
                    { if (material == Material.Tree) result[1].Add(p); else if (material == Material.Rock) result[2].Add(p); }
                }
        _occupationSources[town] = result; return result;
    }
    private bool OccupationSourceClaimed(WorldPosition p, long actor) => State.Work.Any(w => w.Target == p)
        || State.Actors.Any(a => a.Id != actor && a.Occupation is { Phase: not (OccupationPhase.Delivering or OccupationPhase.Returning) } job && job.Target == p);
    private bool SelectOccupationRoutine(Actor actor)
    {
        if (actor.OccupationVersion != 1 || !ProductiveOccupation(actor) || actor.Position.Floor != 0) return false;
        var home = OccupationHome(actor);
        if (actor.Occupation == null && State.Tick >= actor.OccupationAfter && actor.Position.Distance(home) <= OccupationRadius)
            actor.Occupation = PlanOccupation(actor);
        if (actor.Occupation is not { } job)
        { SetResidentRoutine(actor, RoutineKind.Occupation, home, actor.Position.Distance(home) > OccupationRadius ? "Returning to the home work area" : "Waiting for local work supplies or sources", ground: false); return true; }
        SetResidentRoutine(actor, RoutineKind.Occupation, OccupationStop(actor, job), OccupationDescription(actor), ground: false); return true;
    }
    public string OccupationDescription(Actor actor)
    {
        if (actor.Occupation is not { } job) return "";
        return job.Phase switch
        {
            OccupationPhase.Collecting => "Collecting ingredients from home storage",
            OccupationPhase.Delivering => "Carrying " + string.Join(", ", job.Carried.Select(p => p.Value + " " + p.Key)) + " to home storage",
            OccupationPhase.Returning => "Returning unused ingredients to home storage",
            _ => job.Kind switch { OccupationKind.Gather => "Gathering grain", OccupationKind.Fell => "Felling a tree", OccupationKind.Quarry => "Quarrying stone", OccupationKind.BuildWorkplace => "Building the town workplace", OccupationKind.RepairWorkplace => "Repairing the town workplace", _ => "Preparing medicine at the workplace" }
        };
    }
    private OccupationJob? PlanOccupation(Actor actor)
    {
        OccupationJob? job = null; var home = OccupationHome(actor); actor.OccupationAfter = State.Tick + TicksPerHour;
        if (actor.Job == "healer")
        {
            if (actor.OccupationWorkplace is not { } workplace || Chunks.At(workplace) != Material.Camp)
            {
                WorldPosition? camp = null;
                for (int r = 0; r <= 16 && camp == null; r++)
                    for (int y = -r; y <= r && camp == null; y++) for (int x = -r; x <= r; x++)
                        if (Math.Abs(x) + Math.Abs(y) == r && Plan.Contains(home.Offset(x, y)) && Chunks.At(home.Offset(x, y)) == Material.Camp)
                        { camp = home.Offset(x, y); break; }
                actor.OccupationWorkplace = camp ?? ResidentGround(home.Offset(-6, 0), actor.Position);
            }
            var target = actor.OccupationWorkplace.Value;
            bool building = Chunks.At(target) != Material.Camp;
            if (!building && State.Structures.FirstOrDefault(s => s.Position == target) is { Condition: <= 40 })
            {
                if (State.Towns[actor.HomeTown].Stocks["wood"] < 1) return null;
                job = new() { Kind = OccupationKind.RepairWorkplace, Phase = OccupationPhase.Collecting, Target = target,
                    Before = Material.Camp, Required = 12, Inputs = new() { ["wood"] = 1 } };
                job.Id = State.NextWorkId++; job.LastPosition = actor.Position; job.LastProgressTick = State.Tick; return job;
            }
            var recipe = Content.Game.Recipes.Select(Content.Get<RecipeDefinition>).OrderBy(r => r.Id, StringComparer.Ordinal)
                .FirstOrDefault(r => building ? r.Output == null && r.Material == Material.Camp : r.Output != null && Content.Get<ItemDefinition>(r.Output).Stack == "medicine");
            if (recipe == null || building && (!GroundOpen(target) || OccupationSourceClaimed(target, actor.Id))) return null;
            var cost = RecipeCost(recipe);
            if (cost.Any(p => State.Towns[actor.HomeTown].Stocks.GetValueOrDefault(p.Key) < p.Value)) return null;
            job = new() { Kind = building ? OccupationKind.BuildWorkplace : OccupationKind.Craft, Phase = OccupationPhase.Collecting,
                Target = target, Before = Chunks.At(target), Definition = recipe.Id, Required = recipe.Work, Inputs = cost,
                Yield = building ? [] : new() { [Content.Get<ItemDefinition>(recipe.Output!).Stack] = recipe.Amount } };
        }
        else
        {
            int index = actor.Job == "farmer" ? 0 : actor.Job == "woodcutter" ? 1 : 2;
            WorldPosition? target = null; int distance = int.MaxValue;
            foreach (var p in OccupationSources(actor.HomeTown)[index])
            {
                int next = actor.Position.Distance(p);
                if (next >= distance || actor.OccupationAvoid == p && State.Tick < actor.OccupationAvoidUntil || Chunks.IsEdited(p) || OccupationSourceClaimed(p, actor.Id)) continue;
                if (index == 0 ? ResourceAt(p) is not { Available: > 0 } : Chunks.At(p) != (index == 1 ? Material.Tree : Material.Rock)) continue;
                target = p; distance = next;
            }
            if (target is not { } source) return null;
            var node = index == 0 ? ResourceAt(source) : null; var before = Chunks.At(source);
            job = new() { Kind = (OccupationKind)index, Phase = OccupationPhase.Working, Target = source, Before = before,
                Definition = node?.Definition.Id ?? "", Required = node?.Definition.Work ?? Materials.Hardness(before) * 5,
                Yield = node?.Definition.Yield.ToDictionary(p => Content.Get<ItemDefinition>(p.Key).Stack, p => p.Value)
                    ?? new() { [Materials.Yield(before)] = Materials.Hardness(before) + 1 } };
        }
        job.Id = State.NextWorkId++; job.LastPosition = actor.Position; job.LastProgressTick = State.Tick; return job;
    }
    private bool OccupationSourceValid(Actor actor, OccupationJob job)
    {
        if (OccupationSourceClaimed(job.Target, actor.Id)) return false;
        if (job.Kind == OccupationKind.RepairWorkplace) return Chunks.At(job.Target) == Material.Camp && State.Structures.Any(s => s.Position == job.Target && s.Condition < 95);
        if (job.Kind is OccupationKind.Craft or OccupationKind.BuildWorkplace) return Chunks.At(job.Target) == job.Before && (job.Kind != OccupationKind.Craft || ServiceableCamp(job.Target));
        if (Chunks.IsEdited(job.Target)) return false;
        if (job.Kind == OccupationKind.Gather) return ResourceAt(job.Target) is { Available: > 0 } resource && resource.Definition.Id == job.Definition;
        return Chunks.At(job.Target) == job.Before && ResourceAt(job.Target) == null && ObjectAt(job.Target) is not { Kind: "discovery" or "loot" };
    }
    private void AdvanceOccupations()
    {
        foreach (var actor in State.Actors)
        {
            if (actor.Occupation is not { } job || !OccupationActive(actor) || actor.Body.RestUntil > State.Tick || actor.Body.Fatigue >= 75
                || actor.Body.Hunger >= Simulation.StarveAt || actor.Health < actor.MaxHealth / 4 || State.Battle?.ActorIds.Contains(actor.Id) == true) continue;
            if (actor.Position != job.LastPosition) { job.LastPosition = actor.Position; job.LastProgressTick = State.Tick; }
            if (job.Phase is OccupationPhase.Delivering or OccupationPhase.Returning)
            {
                if (job.Carried.Count == 0) { FinishOccupation(actor, job); continue; }
                if (actor.Position.Distance(OccupationHome(actor)) > 1) continue;
                var storage = State.Towns[actor.HomeTown].Stocks;
                foreach (var (stack, count) in job.Carried.ToArray())
                {
                    int accepted = Math.Min(count, Math.Max(0, 9999 - storage.GetValueOrDefault(stack)));
                    if (accepted == 0) continue;
                    storage[stack] = storage.GetValueOrDefault(stack) + accepted; actor.Cargo[stack] -= accepted;
                    job.Delivered[stack] = job.Delivered.GetValueOrDefault(stack) + accepted;
                    if (accepted == count) job.Carried.Remove(stack); else job.Carried[stack] -= accepted;
                }
                if (job.Carried.Count == 0) FinishOccupation(actor, job);
                continue;
            }
            if (job.Phase == OccupationPhase.Collecting)
            {
                if (actor.Position.Distance(OccupationHome(actor)) > 1) continue;
                var storage = State.Towns[actor.HomeTown].Stocks;
                if (job.Inputs.Any(p => storage.GetValueOrDefault(p.Key) < p.Value)) continue;
                foreach (var (stack, count) in job.Inputs) { storage[stack] -= count; actor.Cargo[stack] = actor.Cargo.GetValueOrDefault(stack) + count; job.Carried[stack] = count; }
                job.Phase = OccupationPhase.Working; job.LastProgressTick = State.Tick; continue;
            }
            if (actor.Position.Distance(job.Target) > 1)
            {
                if (State.Tick - job.LastProgressTick < 100) continue;
                actor.OccupationAvoid = job.Target; actor.OccupationAvoidUntil = State.Tick + TicksPerHour;
                ReturnOccupationInputs(actor, job); continue;
            }
            if (!OccupationSourceValid(actor, job)) { ReturnOccupationInputs(actor, job); continue; }
            if (job.Kind == OccupationKind.BuildWorkplace && OccupantAt(job.Target) != null) continue;
            double amount = WorkRate(actor) * WorkActivityMultiplier(actor, job.Kind == OccupationKind.Gather
                ? Content.Get<ResourceDefinition>(job.Definition).Activity : job.Kind is OccupationKind.Fell or OccupationKind.Quarry ? "dig" : "craft")
                * (job.Kind is OccupationKind.Gather or OccupationKind.Fell or OccupationKind.Quarry ? ClimateGathering(job.Target) : 1);
            job.Progress += amount; actor.Body.WorkDone += amount; job.LastProgressTick = State.Tick;
            if (job.Progress < job.Required) continue;
            foreach (var (stack, count) in job.Inputs) { actor.Cargo[stack] -= count; State.ResourcesConsumed += count; }
            job.Carried.Clear(); job.Progress = job.Required;
            if (job.Kind == OccupationKind.Gather) HarvestResource(job.Target);
            else if (job.Kind is OccupationKind.Fell or OccupationKind.Quarry)
            { RecordHabitatClearing(job.Target, job.Before, Material.Earth); Chunks.Edit(job.Target, Material.Earth); }
            else if (job.Kind == OccupationKind.BuildWorkplace)
            {
                RecordHabitatClearing(job.Target, job.Before, Material.Camp);
                Chunks.Edit(job.Target, Material.Camp); State.Structures.Add(new() { Position = job.Target, Material = Material.Camp, Foundation = job.Before, Cost = new(job.Inputs) });
                actor.Occupation = null; actor.OccupationAfter = State.Tick + 25; continue;
            }
            else if (job.Kind == OccupationKind.RepairWorkplace)
            { State.Structures.Single(s => s.Position == job.Target).Condition = 100; actor.Occupation = null; actor.OccupationAfter = State.Tick + 25; continue; }
            foreach (var (stack, count) in job.Yield)
            { actor.Cargo[stack] = actor.Cargo.GetValueOrDefault(stack) + count; job.Carried[stack] = count; State.ResourcesProduced += count; State.NamedResourcesProduced += count; }
            job.Phase = OccupationPhase.Delivering;
            Diagnose("occupation-produced", new { actor = actor.Id, job.Id, job.Kind, job.Target, output = new Dictionary<string, int>(job.Yield) });
        }
    }
    private void ReturnOccupationInputs(Actor actor, OccupationJob job)
    {
        if (job.Carried.Count > 0) { job.Phase = OccupationPhase.Returning; job.Progress = 0; }
        else { actor.Occupation = null; actor.OccupationAfter = State.Tick + 25; }
    }
    private void FinishOccupation(Actor actor, OccupationJob job)
    {
        Diagnose("occupation-delivered", new { actor = actor.Id, job.Id, job.Kind, returned = job.Phase == OccupationPhase.Returning, delivered = new Dictionary<string, int>(job.Delivered) });
        actor.Occupation = null; actor.OccupationAfter = State.Tick + (actor.Job == "healer" ? TicksPerHour : 25);
    }
    private bool EatOccupationFood(Actor actor)
    {
        if (actor.Occupation is not { Phase: OccupationPhase.Delivering } job || job.Carried.GetValueOrDefault("food") < 1) return false;
        actor.Cargo["food"]--; if (--job.Carried["food"] == 0) job.Carried.Remove("food");
        job.Consumed["food"] = job.Consumed.GetValueOrDefault("food") + 1; State.ResourcesConsumed++; return true;
    }
    private void RecruitOccupation(Actor actor)
    {
        if (actor.Occupation is not { } job) return;
        var overflow = new Dictionary<string, int>();
        foreach (var (stack, count) in job.Carried)
        {
            int amount = Math.Min(count, Math.Max(0, 9999 - Count(stack))); Add(stack, amount); actor.Cargo[stack] -= count;
            if (amount < count) overflow[stack] = count - amount;
            State.ResourcesExported += count;
        }
        if (overflow.Count > 0) DropWorkBundle(job.Id, 0, actor.Position, overflow);
        actor.Occupation = null; actor.OccupationAfter = State.Tick + TicksPerHour;
    }
}
