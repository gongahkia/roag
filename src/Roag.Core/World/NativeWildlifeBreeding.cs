using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record NativeWildlifeBirth(long Parent, long Tick);

/// <summary>Real living-parent brood deadlines and individual birth provenance.</summary>
public sealed class NativeWildlifeBreeding
{
    public SortedDictionary<long, long> After { get; set; } = [];
    public SortedDictionary<long, NativeWildlifeBirth> Births { get; set; } = [];

    internal void Validate(WorldState world, IReadOnlyDictionary<long, Actor> actors)
    {
        if (After == null || Births == null || After.Count > WorldSession.WildlifeActorLimit || Births.Count > WorldSession.WildlifeActorLimit
            || After.Any(p => !actors.TryGetValue(p.Key, out var a) || a.Life != LifeState.Alive || a.WildlifePopulation == 0 || a.Habitat == null
                || p.Value < 0 || p.Value > world.Tick + WorldSession.NativeBroodTicks
                || Births.TryGetValue(p.Key, out var born) && (born == null || p.Value < born.Tick + WorldSession.NativeBroodTicks))
            || Births.Any(p => !actors.TryGetValue(p.Key, out var child) || child.WildlifePopulation == 0 || child.Habitat == null || p.Value == null
                || p.Value.Parent < 1 || p.Value.Parent >= p.Key || p.Value.Parent >= world.NextActorId
                || p.Value.Tick < 0 || p.Value.Tick > world.Tick
                || actors.TryGetValue(p.Value.Parent, out var parent) && (parent.Archetype != child.Archetype || parent.Habitat == null)
                || Births.TryGetValue(p.Value.Parent, out var older) && (older == null || p.Value.Tick < older.Tick + WorldSession.NativeBroodTicks)))
            throw new InvalidDataException("Invalid native living-population breeding or birth provenance.");
    }
}

public sealed partial class WorldSession
{
    public const int NativeBroodTicks = WildlifeRecoveryTicks, NativeBroodLocalLimit = 2;

    private void RegisterNativeBrood(Actor actor)
    {
        if (State.NativeCampaign is { } campaign && actor is { Life: LifeState.Alive, WildlifePopulation: > 0, Habitat: not null }
            && actor.Tactics is not { RewardExempt: true }
            && !campaign.Breeding.After.ContainsKey(actor.Id))
            campaign.Breeding.After.Add(actor.Id, checked(State.Tick + NativeBroodTicks));
    }

    private bool NativeBroodParentReady(Actor actor)
    {
        if (actor is not { Life: LifeState.Alive, Habitat: { } home, Migration: null }
            || !NativeCreatureNeeds(actor) || actor.Tactics is { RewardExempt: true } || Ecology(actor) is not { } rules
            || actor.Position.Distance(home) > 2 || (long)actor.Health * 4 < (long)actor.MaxHealth * 3
            || actor.Body.Hunger >= Simulation.MealAt || actor.Body.Fatigue >= 75 || actor.Body.RestUntil > State.Tick
            || State.NativeCampaign!.Breeding.Births.TryGetValue(actor.Id, out var born) && State.Tick < born.Tick + NativeBroodTicks
            || IsActorInCombat(actor.Id) || NativeFleeThreat(actor) != null) return false;
        var habitat = HabitatAt(home);
        return habitat.Food >= WildlifeBirthFood && habitat.Pressure < 200 && habitat.Capacity >= 2
            && State.Actors.Count(a => a.Life == LifeState.Alive && a.Habitat?.Chunk == home.Chunk) < NativeBroodLocalLimit
            && WildlifeHomeSuitable(home, rules);
    }

    private void AdvanceNativeBroods()
    {
        if (State.NativeCampaign is not { } campaign || State.Tick % 50 != 0) return;
        foreach (var actor in State.Actors.Where(a => a.Life == LifeState.Alive && a.WildlifePopulation > 0).OrderBy(a => a.Id))
            RegisterNativeBrood(actor);
        var due = campaign.Breeding.After.Where(p => p.Value <= State.Tick).OrderBy(p => p.Value).ThenBy(p => p.Key)
            .Take(WildlifeRecoveryBudget).Select(p => p.Key).ToArray();
        foreach (long id in due)
        {
            var parent = ActorById(id);
            campaign.Breeding.After[id] = checked(State.Tick + TicksPerHour);
            if (!NativeBroodParentReady(parent)) continue;
            var home = parent.Habitat!.Value;
            WorldPosition? spawn = null;
            foreach (var (x, y) in Directions)
            {
                var candidate = home.Offset(x, y);
                if (candidate.Chunk == home.Chunk && CanEnter(candidate)
                    && Chunks.At(candidate) is not (Material.StairsUp or Material.StairsDown)) { spawn = candidate; break; }
            }
            if (spawn == null) continue;
            // A vacant population ledger can hold the new inhabitant's history;
            // its dead individual is never restored, and a living parent is required.
            var vacant = State.WildlifePopulations.Values.Where(p => p.Definition == parent.Archetype && p.Home.Chunk == home.Chunk
                    && ActorById(p.Actor).Life == LifeState.Dead && p.Generation < 1_000_000_000)
                .OrderBy(p => p.LastDeath).ThenBy(p => p.Actor).FirstOrDefault();
            if (vacant != null && !ReclaimWildlifeHistory(vacant)) continue;
            if (!WildlifeActorRoom() || vacant == null && State.WildlifePopulations.Count >= WildlifePopulationLimit) continue;
            var child = ActorFactory.Create(Content, parent.Archetype, Seed, State.NextActorId, spawn.Value,
                parent.HomeTown, parent.Culture, parent.FactionId);
            child.Habitat = child.NatalHabitat = home; child.Job = "wildlife"; child.WildlifeAfter = State.Tick + TicksPerHour;
            if (!TakeWildFood(home, WildlifeBirthFood)) throw new InvalidOperationException("Brood food changed during sequential birth.");
            State.NextActorId++;
            if (vacant != null)
            {
                vacant.History.Add(vacant.Actor); vacant.Actor = child.Id; vacant.Home = home; vacant.Generation++;
                vacant.Recovery = WildlifeRecovery.Alive; vacant.RecoverAt = 0;
                child.WildlifePopulation = State.WildlifePopulations.First(p => ReferenceEquals(p.Value, vacant)).Key;
            }
            State.Actors.Add(child);
            if (vacant == null) RegisterWildlifePopulation(child);
            RegisterNativeBrood(child);
            campaign.Breeding.After[parent.Id] = checked(State.Tick + NativeBroodTicks);
            campaign.Breeding.Births.Add(child.Id, new(parent.Id, State.Tick));
            Diagnose("wildlife-born", new { actor = child.Id, parent = parent.Id, population = child.WildlifePopulation, home });
        }
    }
}
