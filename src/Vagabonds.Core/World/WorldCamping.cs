using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public bool ServiceableCamp(WorldPosition camp) => Plan.Contains(camp) && Chunks.At(camp) == Material.Camp
        && (State.Structures.FirstOrDefault(s => s.Position == camp)?.Condition ?? 100) > 20;
    public int PreparationPower(Actor actor, string effect) => actor.Preparations.Where(p => p.Effect == effect && p.Until > State.Tick).Select(p => p.Power).DefaultIfEmpty().Max();
    private Actor CombatActor(CombatUnit unit) => State.Actors.Single(a => a.Id == State.Battle!.ActorIds[unit.Id]);
    public string PreparationDescription(Actor actor) => string.Join("; ", actor.Preparations.Where(p => p.Until > State.Tick)
        .Select(p => Content.Get<CampDefinition>(p.Definition).Name + $" ({(p.Until - State.Tick + 4) / 5} min)"));
    public string CampActivityReason(WorldPosition camp, string id, long workerId = 0)
    {
        if (!Content.Game.CampActivities.Contains(id)) return "Choose a camp activity.";
        var definition = Content.Get<CampDefinition>(id);
        if (!Chunks.Seen(camp) || !ServiceableCamp(camp)) return "Choose an observed, serviceable camp.";
        if (camp.Distance(State.Position) > 4) return "Bring the party within four tiles of the camp.";
        if (State.Work.Count >= 64) return "The work list is full.";
        if (State.Work.Any(w => w.Station == camp && w.Kind == WorkKind.Camp && Content.Get<CampDefinition>(w.Definition).Role == definition.Role)) return "This duty already has an order at this camp.";
        if (workerId != 0)
        {
            var worker = State.Actors.FirstOrDefault(a => a.Id == workerId);
            if (worker == null || worker.Kind != ActorKind.Person || worker.Life != LifeState.Alive || worker.Position.Distance(camp) > 100) return "Choose a conscious nearby person.";
            if (State.Work.Any(w => w.Worker == worker.Id)) return worker.Name + " already has an order; reassign or cancel it in Work.";
            if (definition.Role == "stories" && worker.Memories.Any(m => m.Key == "camp-story" && m.Until > State.Tick)) return "This companion has already shared a story; let two game hours pass.";
        }
        if (workerId == 0 && definition.Role == "stories" && !Party.Any(a => a.Life == LifeState.Alive && !a.Memories.Any(m => m.Key == "camp-story" && m.Until > State.Tick))) return "The companions have already shared their stories; let two game hours pass.";
        if (definition.Role == "treat" && !Party.Any(a => a.Life == LifeState.Alive && a.Position.Distance(camp) <= 6 && (a.Health < a.MaxHealth || a.Body.Wounds > 0))) return "No nearby conscious companion needs treatment.";
        if (definition.Cost.Count > 0 && MissingSupplies(definition.Cost) is { Length: > 0 } missing) return missing;
        return CampWorkCell(camp, workerId) == null ? "Clear observed ground beside the camp for another duty." : "";
    }
    private WorldPosition? CampWorkCell(WorldPosition camp, long workerId)
    {
        var origin = State.Actors.FirstOrDefault(a => a.Id == workerId)?.Position ?? State.Position;
        return (from y in Enumerable.Range(-2, 5) from x in Enumerable.Range(-2, 5)
            let p = camp.Offset(x, y)
            where Math.Abs(x) + Math.Abs(y) is >= 1 and <= 2 && Chunks.Seen(p) && GroundOpen(p)
                && !State.Work.Any(w => w.Target == p) && OccupantAt(p) is not { Recruited: false }
            orderby p.Distance(origin), p.Y, p.X select (WorldPosition?)p).FirstOrDefault();
    }
    private ActionResult QueueCampActivity(WorldPosition camp, string id, long workerId)
    {
        string reason = CampActivityReason(camp, id, workerId); if (reason.Length > 0) return new(false, reason);
        var definition = Content.Get<CampDefinition>(id); var p = CampWorkCell(camp, workerId)!.Value;
        var order = new WorkOrder { Kind = WorkKind.Camp, Definition = id, Station = camp, Target = p, Before = Chunks.At(p), After = Chunks.At(p), Required = definition.Work,
            Reserved = definition.Cost.ToDictionary(y => Content.Get<ItemDefinition>(y.Key).Stack, y => y.Value) };
        var result = QueueWork(order);
        if (result.Success && workerId != 0) { order.Worker = workerId; order.PinnedWorker = true; }
        return result.Success ? new(true, definition.Name + " assigned; supplies reserved. Other companions can take different camp duties.") : result;
    }
    private bool CampWorkerEligible(WorkOrder order, Actor actor) => order.Kind != WorkKind.Camp
        || Content.Get<CampDefinition>(order.Definition).Role != "stories" || !actor.Memories.Any(m => m.Key == "camp-story" && m.Until > State.Tick);
    private void CompleteCampActivity(WorkOrder order, Actor worker)
    {
        var definition = Content.Get<CampDefinition>(order.Definition); var camp = order.Station!.Value;
        var companions = Party.Where(a => a.Life == LifeState.Alive && a.Position.Distance(camp) <= 6).ToArray();
        if (definition.Role == "stories") { ShareCampStory(worker, camp, definition); return; }
        foreach (var actor in companions)
        {
            if (definition.Role == "treat")
            { actor.Health = Math.Min(actor.MaxHealth, actor.Health + definition.Power * 6); actor.Body.Wounds = Math.Max(0, actor.Body.Wounds - definition.Power); }
            else
            {
                actor.Preparations.RemoveAll(p => p.Effect == definition.Effect || p.Until <= State.Tick);
                actor.Preparations.Add(new(definition.Id, definition.Effect, definition.Power, State.Tick + definition.Duration));
                if (definition.Role == "cook") actor.Body.Hunger = Math.Max(0, actor.Body.Hunger - Simulation.MealReduction);
            }
            Remember(actor, "camp-" + definition.Role, worker.Name + " prepared " + definition.Name + " for us.", 3, actor.Id == worker.Id ? 0 : worker.Id);
        }
        Log(worker.Name + ": " + definition.Name + " ready for " + companions.Length + " nearby companion(s). " + definition.Description);
    }
    private void ShareCampStory(Actor speaker, WorldPosition camp, CampDefinition definition)
    {
        if (speaker.Memories.Any(m => m.Key == "camp-story" && m.Until > State.Tick)) return;
        var memory = speaker.Memories.Where(m => m.Key is "fallen-friend" or "finished-work" or "hungry" or "helped-party").OrderByDescending(m => m.Tick).FirstOrDefault();
        string text = memory?.Key switch
        {
            "fallen-friend" => "I keep thinking about those we lost. " + memory.Text,
            "finished-work" or "helped-party" => "It feels good to have made something together.",
            "hungry" => "Let us prepare enough food before we leave this fire.",
            _ => "I hope this road brings me closer to what I want: " + speaker.Motivation + "."
        };
        var site = Plan.Sites.Where(s => !State.KnownSites.Contains(s.Id)).OrderBy(s => s.Center.Distance(camp with { Floor = 0 })).ThenBy(s => s.Id).FirstOrDefault();
        if (site != null)
        { State.KnownSites.Add(site.Id); text += $" I heard of {site.Name}, at {site.Center.X}, {site.Center.Y}. I marked it on our map."; }
        Remember(speaker, "camp-story", text, 4);
        var recorded = speaker.Memories.FindIndex(m => m.Key == "camp-story");
        if (recorded >= 0) speaker.Memories[recorded] = speaker.Memories[recorded] with { Until = State.Tick + definition.Duration };
        foreach (var other in Party.Where(a => a.Id != speaker.Id && a.Life == LifeState.Alive && a.Position.Distance(camp) <= 6)) ChangeTrust(speaker, other, 2);
        State.LastCampStory = new(speaker.Id, text, State.Tick, camp); Log(speaker.Name + ": " + text);
    }
}
