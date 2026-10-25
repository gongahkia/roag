using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public SimulationDefinition Simulation => Content.Get<SimulationDefinition>(Content.Game.Simulation);
    private TraitDefinition? Disposition(Actor actor) => actor.TraitId.Length > 0 ? Content.Get<TraitDefinition>(actor.TraitId) : null;
    private bool Remember(Actor actor, string key, string text, int mood, long other = 0)
    {
        if (actor.Life == LifeState.Dead || actor.Memories.Any(m => m.Key == key && m.Other == other && m.Until > State.Tick)) return false;
        if (actor.Memories.Count >= 12) actor.Memories.RemoveAt(0);
        actor.Memories.Add(new(key, text, State.Tick, State.Tick + Simulation.MemoryTicks, mood, other));
        actor.Stress = Math.Clamp(actor.Stress - mood, 0, 100);
        if (State.Battle is { } battle)
        {
            int index = battle.ActorIds.IndexOf(actor.Id);
            if (index >= 0) battle.Units[index].Stress = actor.Stress;
        }
        return true;
    }
    private static void ChangeTrust(Actor actor, Actor other, int amount) => actor.Trust[other.Id] = Math.Clamp(actor.Trust.GetValueOrDefault(other.Id) + amount, -100, 100);
    private bool HasWork(Actor actor)
    {
        foreach (var order in State.Work) if (order.Worker == actor.Id && !order.Suspended) return true;
        return false;
    }
    public double WorkRate(Actor actor) => (Disposition(actor)?.WorkPercent ?? 100) / 100.0 * Math.Min(2, 1 + (actor.Level - 1) * .02)
        * (actor.Body.Wounds > 0 ? .75 : 1) * (actor.Stress >= 60 ? .75 : 1);
    private void AdvanceBodies()
    {
        GatherBodies();
        foreach (var slot in _bodyPass)
        {
            if (slot.Actor.Life == LifeState.Alive) { AdvanceBody(slot.Actor); _bodyProcessed++; }
            ScheduleBody(slot);
        }
        FinishBodyPass();
        if (State.Tick % TicksPerHour == 0 && !IsInCombat) AdvanceCommunity();
    }
    private void AdvanceBody(Actor actor)
    {
        var settings = Simulation;
        var body = actor.Body; bool fighting = State.Battle?.ActorIds.Contains(actor.Id) == true;
        if (!actor.Recruited && !fighting && body.FatigueAt(State.Tick - 1) >= 75 && body.RestUntil <= State.Tick) body.RestUntil = State.Tick + 300;
        bool resting = !fighting && body.RestUntil > State.Tick;
        if (body.Hunger >= settings.MealAt && TryEat(actor)) body.Hunger = Math.Max(0, body.Hunger - settings.MealReduction);
        // Unmanaged people/creatures can spend time gathering food using the same forage work cost.
        if (!actor.Recruited && !fighting && !_bodyWorkers.Contains(actor.Id) && !resting && body.Hunger >= settings.MealAt && State.Tick >= body.ForageAfter)
        {
            double forage = WorkRate(actor); body.ForageProgress += forage; body.WorkDone += forage;
            if (body.ForageProgress >= settings.ForageWork)
            {
                body.ForageProgress = 0; body.ForageAfter = State.Tick + TicksPerHour;
                actor.Cargo["food"] = actor.Cargo.GetValueOrDefault("food") + 2; State.ResourcesProduced += 2;
            }
        }
        for (int i = actor.Memories.Count - 1; i >= 0; i--) if (actor.Memories[i].Until <= State.Tick) actor.Memories.RemoveAt(i);
        for (int i = actor.Preparations.Count - 1; i >= 0; i--) if (actor.Preparations[i].Until <= State.Tick) actor.Preparations.RemoveAt(i);
        if (State.Tick % 50 != 0 || fighting) return;
        if (body.Hunger >= settings.StarveAt)
        {
            actor.Health = Math.Max(1, actor.Health - 1);
            Remember(actor, "hungry", "There was not enough to eat.", -8);
        }
        if (body.Fatigue >= 80) Remember(actor, "exhausted", "I need a break before more work.", Disposition(actor)?.Aversion == "overwork" ? -10 : -6);
        if (resting && body.Hunger < settings.StarveAt)
        {
            bool camp = NearbyCamp(actor.Position);
            actor.Health = Math.Min(actor.MaxHealth, actor.Health + (camp ? 8 : 2));
            actor.Mana = Math.Min(actor.MaxMana, actor.Mana + (camp ? 4 : 1));
            if (State.Tick % TicksPerHour == 0) body.Wounds = Math.Max(0, body.Wounds - 1);
        }
        if (actor.Position.Floor > 0 && Disposition(actor)?.Aversion == "confinement") Remember(actor, "confined", "The deep walls press close.", -3);
    }
    private bool TryEat(Actor actor)
    {
        if (actor.Recruited)
        {
            if (Count("food") < 1) return false;
            Add("food", -1); return true;
        }
        if (actor.Cargo.GetValueOrDefault("food") > 0)
        { actor.Cargo["food"]--; State.ResourcesConsumed++; return true; }
        var town = State.Towns[actor.HomeTown];
        if (actor.Position.Distance(Plan.Towns[actor.HomeTown].Center) > 28 || town.Stocks["food"] < 1) return false;
        town.Stocks["food"]--; State.ResourcesConsumed++; return true;
    }
    private bool NearbyCamp(WorldPosition position)
    {
        if (State.Structures.Any(s => s.Material == Material.Camp && s.Condition > 20 && s.Position.Distance(position) <= 1)) return true;
        if (position.Distance(State.Position) > 100) return false;
        return new[] { position }.Concat(Directions.Select(d => position.Offset(d.X, d.Y))).Any(p => Chunks.At(p) == Material.Camp
            && (State.Structures.FirstOrDefault(s => s.Position == p)?.Condition ?? 100) > 20);
    }
    private void AdvanceCommunity()
    {
        var people = State.Actors.Where(a => a.Kind == ActorKind.Person && a.Life == LifeState.Alive && !HasWork(a) && a.Body.RestUntil <= State.Tick).ToArray();
        foreach (var actor in people)
        {
            var other = people.FirstOrDefault(b => b.Id > actor.Id && b.Recruited == actor.Recruited && b.Position.Distance(actor.Position) <= 4);
            if (other == null) continue;
            if (Remember(actor, "company", "A quiet conversation with " + other.Name + ".", 3, other.Id))
            { Remember(other, "company", "A quiet conversation with " + actor.Name + ".", 3, actor.Id); ChangeTrust(actor, other, 2); ChangeTrust(other, actor, 2); }
        }
    }
    private void AdvanceIncidents()
    {
        if (State.Tick % TicksPerHour != 0 || IsInCombat) return;
        var candidates = Content.Game.Incidents.Select(Content.Get<IncidentDefinition>).Where(i => State.IncidentAfter.GetValueOrDefault(i.Id) <= State.Tick)
            .SelectMany(i => State.Actors.Where(a => a.Life == LifeState.Alive && a.Kind == ActorKind.Person && a.Position.Distance(State.Position) <= 100 && IncidentEligible(i, a)).Select(a => (Definition: i, Actor: a))).ToArray();
        if (candidates.Length == 0) return;
        var rng = new RandomStream(State.IncidentRandom); var choice = candidates[rng.Next(candidates.Length)]; State.IncidentRandom = rng.State;
        var d = choice.Definition; var actor = choice.Actor;
        State.IncidentAfter[d.Id] = State.Tick + d.Cooldown;
        if (d.Effect == "rest") actor.Body.RestUntil = State.Tick + 300;
        if (d.Effect == "aid")
        {
            var town = State.Towns[actor.HomeTown];
            if (actor.Position.Distance(Plan.Towns[actor.HomeTown].Center) <= 28 && town.Stocks["medicine"] > 0)
            { town.Stocks["medicine"]--; State.ResourcesConsumed++; actor.Health = Math.Min(actor.MaxHealth, actor.Health + 12); actor.Body.Wounds = Math.Max(0, actor.Body.Wounds - 1); }
        }
        Remember(actor, d.Id, d.Description, d.Mood); Log(actor.Name + ": " + d.Name + ". " + d.Description);
    }
    private bool IncidentEligible(IncidentDefinition d, Actor a) => d.Condition switch
    {
        "hunger" => a.Body.Hunger >= 70,
        "exhaustion" => a.Body.Fatigue >= 75,
        "injury" => a.Body.Wounds > 0 && a.Position.Distance(Plan.Towns[a.HomeTown].Center) <= 28 && State.Towns[a.HomeTown].Stocks["medicine"] > 0,
        "craft" => a.Body.WorkDone >= 20,
        "trust" => a.Trust.Values.Any(t => t >= 10), _ => false
    };
}
