using System.Text.Json.Serialization;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public enum WildlifeRecovery { Alive, Recovering, Food, Pressure, Habitat, Occupied, Remains, Capacity }
public sealed class WildlifePopulation
{
    [JsonRequired] public WorldPosition Origin { get; set; }
    [JsonRequired] public WorldPosition Home { get; set; }
    [JsonRequired] public string Definition { get; set; } = "";
    [JsonRequired] public long Actor { get; set; }
    [JsonRequired] public int Generation { get; set; }
    [JsonRequired] public int Deaths { get; set; }
    [JsonRequired] public long LastDeath { get; set; }
    [JsonRequired] public long RecoverAt { get; set; }
    [JsonRequired] public WildlifeRecovery Recovery { get; set; }
    [JsonRequired] public List<long> History { get; set; } = [];
}

public sealed partial class WorldSession
{
    public const int WildlifeRecoveryTicks = TicksPerHour * 24 * 3, WildlifeBirthFood = 4;
    public const int WildlifePopulationLimit = 2048, WildlifeActorLimit = 4096, WildlifeHistoryLimit = 4, WildlifeRecoveryBudget = 4;
    private long _wildlifeRecoveryAttempts;
    private PriorityQueue<WildlifePopulation, (long Deadline, long Id)>? _wildlifeRecoveryDue;
    public long WildlifeRecoveryAttempts => _wildlifeRecoveryAttempts;
    private bool WildlifeActorRoom() => State.Actors.Count < 10000 && State.Actors.Count(a => a.Habitat != null) < WildlifeActorLimit;
    private WildlifePopulation? RegisterWildlifePopulation(Actor actor)
    {
        if (actor.WildlifePopulation != 0) return State.WildlifePopulations.GetValueOrDefault(actor.WildlifePopulation);
        if (actor.Habitat is not { } home || Ecology(actor) == null || State.WildlifePopulations.Count >= WildlifePopulationLimit) return null;
        actor.WildlifePopulation = actor.Id;
        var population = new WildlifePopulation { Actor = actor.Id, Definition = actor.Archetype, Origin = actor.NatalHabitat ?? home, Home = home,
            Deaths = actor.Life == LifeState.Dead ? 1 : 0, LastDeath = actor.Life == LifeState.Dead ? State.Tick : 0,
            Recovery = actor.Life == LifeState.Dead ? WildlifeRecovery.Recovering : WildlifeRecovery.Alive,
            RecoverAt = actor.Life == LifeState.Dead ? State.Tick + WildlifeRecoveryTicks : 0 };
        State.WildlifePopulations.Add(actor.Id, population); return population;
    }
    private void NoteWildlifeDeath(Actor actor)
    {
        var population = RegisterWildlifePopulation(actor); if (population == null) return;
        population.Home = actor.Habitat!.Value; population.Deaths = population.Generation + 1; population.LastDeath = State.Tick;
        population.RecoverAt = State.Tick + WildlifeRecoveryTicks; population.Recovery = WildlifeRecovery.Recovering;
    }
    private void NoteWildlifeHome(Actor actor)
    {
        if (actor.WildlifePopulation != 0 && State.WildlifePopulations.TryGetValue(actor.WildlifePopulation, out var population)) population.Home = actor.Habitat!.Value;
    }
    private bool WildlifeHistoryReferenced(long id)
    {
        if (State.Party.Contains(id) || State.Battle?.ActorIds.Contains(id) == true || State.LastEncounterReward?.Sources.Contains(id) == true
            || State.CombatEvents.Any(e => e.Source == id || e.Target == id) || State.LastCampStory?.Speaker == id || State.Endgame.ActorId == id
            || State.Discoveries.Any(d => d.WitnessId == id || d.Enemies.Contains(id))
            || State.Work.Any(w => w.Worker == id || w.PinnedWorker && w.Worker == id || w.Output?.Carrier == id || w.Supplies?.Any(s => s.Carrier == id) == true)) return true;
        return State.Actors.Any(a => a.Id != id && (a.FriendId == id || a.EncounterLeader == id || a.Routine?.Other == id
            || a.Trust.ContainsKey(id) || a.Memories.Any(m => m.Other == id)));
    }
    private bool ReclaimWildlifeHistory(WildlifePopulation population)
    {
        while (population.History.Count >= WildlifeHistoryLimit)
        {
            bool reclaimed = false;
            foreach (long id in population.History)
            {
                var actor = ActorById(id); var cache = State.Loot.FirstOrDefault(c => c.SourceActor == id);
                if (actor.Life != LifeState.Dead || cache is not { Taken: true } || WildlifeHistoryReferenced(id)) continue;
                // Already collected drops remain in their actual inventories.
                // Any residual personal cargo decays with the old body and is accounted.
                State.ResourcesLost += actor.Cargo.Values.Sum(v => (long)v);
                State.Loot.Remove(cache); State.RewardedEncounterIds.Remove(id); State.Actors.Remove(actor); population.History.Remove(id);
                reclaimed = true; break;
            }
            if (!reclaimed) return false;
        }
        return true;
    }
    private void AdvanceWildlifePopulations()
    {
        if (State.Tick % 50 != 0 || IsInCombat) return;
        int registered = 0;
        foreach (var actor in State.Actors)
            if (actor.Habitat != null && actor.WildlifePopulation == 0 && registered < 8 && RegisterWildlifePopulation(actor) != null) registered++;
        var due = _wildlifeRecoveryDue ??= new(); due.Clear();
        foreach (var (id, population) in State.WildlifePopulations)
            if (population.RecoverAt > 0 && population.RecoverAt <= State.Tick) due.Enqueue(population, (population.RecoverAt, id));
        int budget = WildlifeRecoveryBudget;
        while (budget-- > 0 && due.TryDequeue(out var population, out _))
        {
            var previous = ActorById(population.Actor);
            if (previous.Life != LifeState.Dead || population.RecoverAt > State.Tick) continue;
            _wildlifeRecoveryAttempts++;
            population.RecoverAt = State.Tick + TicksPerHour;
            var habitat = HabitatAt(population.Home);
            if (habitat.Pressure >= 200) { population.Recovery = WildlifeRecovery.Pressure; continue; }
            if (habitat.Food < WildlifeBirthFood) { population.Recovery = WildlifeRecovery.Food; continue; }
            if (WildlifeDestinationClaimed(previous, population.Home.Chunk)) { population.Recovery = WildlifeRecovery.Occupied; continue; }
            var rules = Content.Get<CharacterDefinition>(population.Definition).Ecology!;
            if (!WildlifeHomeSuitable(population.Home, rules)) { population.Recovery = WildlifeRecovery.Habitat; continue; }
            WorldPosition? spawn = null;
            foreach (var (x, y) in Directions)
            { var candidate = population.Home.Offset(x, y); if (CanEnter(candidate)) { spawn = candidate; break; } }
            if (spawn == null) { population.Recovery = WildlifeRecovery.Occupied; continue; }
            if (!ReclaimWildlifeHistory(population)) { population.Recovery = WildlifeRecovery.Remains; continue; }
            if (!WildlifeActorRoom()) { population.Recovery = WildlifeRecovery.Capacity; continue; }
            var animal = ActorFactory.Create(Content, population.Definition, Seed, State.NextActorId++, spawn.Value,
                previous.HomeTown, previous.Culture, previous.FactionId);
            animal.Habitat = animal.NatalHabitat = population.Home; animal.Job = "wildlife";
            animal.WildlifePopulation = previous.WildlifePopulation; animal.WildlifeAfter = State.Tick + TicksPerHour;
            if (!TakeWildFood(population.Home, WildlifeBirthFood)) throw new InvalidOperationException("Wildlife recovery food changed during sequential birth.");
            population.History.Add(previous.Id); population.Actor = animal.Id; population.Generation++; population.Recovery = WildlifeRecovery.Alive; population.RecoverAt = 0;
            State.Actors.Add(animal);
            Diagnose("wildlife-recovered", new { population = animal.WildlifePopulation, actor = animal.Id, population.Generation, population.Home });
        }
    }
    public string WildlifePopulationDescription(long actorId)
    {
        var actor = ActorById(actorId);
        if (actor.WildlifePopulation == 0 || !State.WildlifePopulations.TryGetValue(actor.WildlifePopulation, out var population)) return "";
        if (population.Actor != actorId || population.Recovery == WildlifeRecovery.Alive) return "A later generation can return after hunting if the habitat stays viable.";
        return population.Recovery switch
        {
            WildlifeRecovery.Food => "Wildlife recovery waits for enough local food.",
            WildlifeRecovery.Pressure => "Hunting and habitat pressure still delay wildlife recovery.",
            WildlifeRecovery.Habitat => "The lost nest or cleared habitat prevents local wildlife recovery.",
            WildlifeRecovery.Occupied => "Another animal or an obstruction occupies the recovering habitat.",
            WildlifeRecovery.Remains => "Repeated hunting still limits recovery here; recover old remains and allow recent events to fade.",
            WildlifeRecovery.Capacity => "Wildlife recovery is limited across the explored region.",
            _ => $"Wildlife needs a quiet recovery period: {Math.Max(1, (population.RecoverAt - State.Tick + TicksPerHour - 1) / TicksPerHour)} game hours remain."
        };
    }
    private static void ValidateWildlifePopulations(WorldPlan plan, WorldState state, ContentCatalog catalog)
    {
        if (state.WildlifePopulations == null || state.WildlifePopulations.Count > WildlifePopulationLimit) throw new InvalidDataException("Invalid wildlife population registry.");
        var actors = state.Actors.ToDictionary(a => a.Id); var members = new HashSet<long>();
        foreach (var (id, population) in state.WildlifePopulations)
        {
            if (id < 1 || id >= state.NextActorId || population == null || !catalog.Game.Wildlife.Contains(population.Definition)
                || !plan.Contains(population.Origin) || !plan.Contains(population.Home) || population.Origin.Floor != population.Home.Floor
                || !state.EcologyChunks.Contains(population.Origin.Chunk) || !state.EcologyChunks.Contains(population.Home.Chunk)
                || !actors.TryGetValue(population.Actor, out var current) || current.WildlifePopulation != id || current.Archetype != population.Definition || current.Habitat != population.Home
                || !Enum.IsDefined(population.Recovery) || population.Generation is < 0 or > 1000000000
                || population.Deaths != population.Generation + (current.Life == LifeState.Dead ? 1 : 0)
                || population.LastDeath < 0 || population.LastDeath > state.Tick || population.Generation == 0 && population.Actor != id
                || population.Generation > 0 && population.Actor <= id || population.History == null || population.History.Count > WildlifeHistoryLimit || population.History.Count > population.Generation
                || population.History.Distinct().Count() != population.History.Count
                || population.History.Any(h => h >= population.Actor || !actors.TryGetValue(h, out var old) || old.Life != LifeState.Dead || old.WildlifePopulation != id || old.Archetype != population.Definition)
                || (current.Life == LifeState.Dead ? population.Recovery == WildlifeRecovery.Alive || population.RecoverAt < population.LastDeath + WildlifeRecoveryTicks || population.RecoverAt > state.Tick + WildlifeRecoveryTicks
                    : population.Recovery != WildlifeRecovery.Alive || population.RecoverAt != 0)
                || !members.Add(population.Actor) || population.History.Any(h => !members.Add(h)))
                throw new InvalidDataException("Invalid wildlife population identity, history or recovery.");
        }
        if (state.Actors.Any(a => a.WildlifePopulation != 0 && (!members.Contains(a.Id) || !state.WildlifePopulations.ContainsKey(a.WildlifePopulation))))
            throw new InvalidDataException("Wildlife population member has no owning record.");
    }
}
