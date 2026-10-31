using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record WildlifeHabitat(string Character, WorldPosition Home, WorldPosition Position);

public sealed partial class WorldSession
{
    public EcologyRules? Ecology(Actor actor) => Content.Entries.GetValueOrDefault(actor.Archetype) is CharacterDefinition c ? c.Ecology : null;

    /// <summary>Coordinate-only habitat sampling; inspection consumes no random stream or saved state.</summary>
    public WildlifeHabitat? WildlifeHabitatAt(ChunkKey key)
    {
        if (key.X is < 0 or >= 64 || key.Y is < 0 or >= 64 || key.Floor is < 0 or >= WorldPlan.Floors) return null;
        var chunk = Chunks.Get(key);
        var random = WorldRandom.Stream(Seed, key.X, key.Y, 11300 + key.Floor);
        var species = Content.Game.Wildlife.OrderBy(id => id, StringComparer.Ordinal).ToList();
        for (int i = species.Count - 1; i > 0; i--) { int j = random.Next(i + 1); (species[i], species[j]) = (species[j], species[i]); }
        foreach (string id in species)
        {
            var rules = Content.Get<CharacterDefinition>(id).Ecology!;
            if (rules.Underground != (key.Floor > 0)) continue;
            int start = random.Next(4096), step = random.Next(2048) * 2 + 1;
            for (int n = 0; n < 4096; n++)
            {
                int index = (start + n * step) % 4096;
                if (!rules.Biomes.Contains(chunk.Biomes[index])) continue;
                var p = new WorldPosition(key.X * 64 + index % 64, key.Y * 64 + index / 64, key.Floor);
                // Bound ecology to the natural surface and dungeon footprints, even after extensive tunnelling.
                if (Chunks.IsEdited(p) || key.Floor == 0 && Plan.Towns.Any(t => t.Center.Distance(p) < 42)
                    || key.Floor > 0 && !Plan.Sites.Any(s => Math.Abs(s.Center.X - p.X) < 30 && Math.Abs(s.Center.Y - p.Y) < 30)) continue;
                if (rules.Resource is { } resource)
                {
                    if (ResourceAt(p)?.Definition.Id != resource) continue;
                }
                else if (Chunks.At(p) != (rules.Underground ? Material.Floor : Material.Grass) || !GroundOpen(p) || ResourceAt(p) != null) continue;
                // Nest/tree itself stays free for gathering; the defender has its own adjacent cell.
                foreach (var (dx, dy) in Directions)
                {
                    var spawn = p.Offset(dx, dy);
                    if (GroundOpen(spawn)) return new(id, p, spawn);
                }
            }
        }
        return null;
    }

    private void ObserveWildlife()
    {
        if (State.Dungeon != null) return;
        if (State.Party.Count == 0 || State.Endgame == null || State.Discoveries.Count == 0 || State.Tactics == null && IsInCombat) return;
        foreach (var key in Visible.Select(p => p.Chunk).Distinct().OrderBy(k => k.Floor).ThenBy(k => k.Y).ThenBy(k => k.X))
        {
            if (!State.EcologyChunks.Add(key)) continue;
            if (State.WildlifePopulations.Count >= WildlifePopulationLimit || !WildlifeActorRoom()) continue;
            if (State.Actors.Any(a => a.Life != LifeState.Dead && (a.Habitat?.Chunk == key || a.Migration?.Target?.Chunk == key))) continue;
            if (WildlifeHabitatAt(key) is not { } habitat) continue;
            var occupied = State.Actors.Where(a => a.Life != LifeState.Dead).Select(a => a.Position).ToHashSet();
            if (!TryFreeCell(habitat.Position, occupied, out var spawn, radius: 3)) continue;
            var town = Plan.Towns.OrderBy(t => Math.Abs(t.Center.X - habitat.Home.X) + Math.Abs(t.Center.Y - habitat.Home.Y)).First();
            var animal = ActorFactory.Create(Content, habitat.Character, Seed, State.NextActorId++, spawn, town.Id, town.Culture, town.FactionId);
            animal.Habitat = habitat.Home; animal.Job = "wildlife";
            State.Actors.Add(animal);
            RegisterWildlifePopulation(animal);
        }
    }

    public bool IsHostile(Actor actor)
    {
        if (actor.Life != LifeState.Alive || actor.Kind is not (ActorKind.Beast or ActorKind.Raider)) return false;
        if (SituationForActor(actor) is { } discovery) return discovery.Engaged && discovery.Outcome is "" or "tunnel";
        if (Ecology(actor) is not { } rules) return true;
        if (rules.Behavior == "flee" || actor.Habitat is not { } home) return false;
        int distance = actor.ProvokedUntil > State.Tick ? rules.Leash : rules.Notice;
        foreach (var member in Party) if (member.Life != LifeState.Dead && member.Position.Distance(home) <= distance) return true;
        return false;
    }

    public string WildlifeDescription(Actor actor)
    {
        if (Ecology(actor) is not { } rules) return "";
        string movement = actor.Migration is not { } migration ? "" : migration.Target == null ? "Seeking a viable habitat after " + MigrationCause(migration.Reason) + ". "
            : "Migrating to a viable habitat after " + MigrationCause(migration.Reason) + (actor.Journey is { Steps.Count: 0 } ? "; route blocked, waiting for access. " : ". ");
        if (rules.Behavior == "flee") return movement + "Timid; flees nearby travellers. Will fight only if hunted.";
        string place = rules.Resource is { } id ? Content.Get<ResourceDefinition>(id).Name.ToLowerInvariant() : "cave roost";
        return movement + (IsHostile(actor) ? "Defending its " : "Guards its ") + place +
            $". Keep {rules.Notice + 1} tiles away; pursuit stops beyond {rules.Leash} tiles.";
    }

    public string GatheringDanger(WorldPosition p) => State.Actors.Any(a => a.Life == LifeState.Alive && a.Habitat == p
        && Visible.Contains(a.Position) && Ecology(a)?.Behavior == "defend") ? " A visible defender guards this resource; gathering or digging here provokes it." : "";

    private void DisturbWildlife(WorldPosition p)
    {
        foreach (var animal in State.Actors.Where(a => a.Life == LifeState.Alive && a.Habitat == p && Ecology(a)?.Behavior == "defend"))
            animal.ProvokedUntil = State.Tick + 75;
    }

    private void MoveWildlife(Actor actor, EcologyRules rules)
    {
        if (actor.Habitat is not { } home) return;
        int range = actor.ProvokedUntil > State.Tick ? rules.Leash : rules.Notice;
        var target = Party.Where(p => p.Life == LifeState.Alive && p.Position.Floor == actor.Position.Floor
                && (rules.Behavior == "flee" || p.Position.Distance(home) <= range))
            .OrderBy(p => p.Position.Distance(actor.Position)).ThenBy(p => p.Id).FirstOrDefault();
        var random = WorldRandom.Stream(Seed, (int)actor.Id, unchecked((int)(State.Tick / 5)), 11350);
        var neighbours = Directions.Select(d => actor.Position.Offset(d.X, d.Y)).Where(p => CanEnter(p, actor.Id)).ToArray();
        if (rules.Behavior == "flee" && target != null && target.Position.Distance(actor.Position) <= rules.Notice)
        {
            var escape = neighbours.Where(p => p.Distance(target.Position) > actor.Position.Distance(target.Position))
                .OrderByDescending(p => p.Distance(target.Position)).ThenBy(p => p.Distance(home)).ToArray();
            if (escape.Length > 0) actor.Position = escape[0];
            return;
        }
        bool defending = target != null && IsHostile(actor);
        if (defending && target!.Position.Distance(home) <= rules.Notice) actor.ProvokedUntil = State.Tick + 75;
        WorldPosition destination = defending ? target!.Position : home;
        if (defending || actor.Position.Distance(home) > 2)
        {
            var next = LocalStep(actor.Position, destination, actor.Id);
            if (next != actor.Position && (rules.Behavior == "flee" || next.Distance(home) <= rules.Leash || next.Distance(home) < actor.Position.Distance(home)) && CanEnter(next, actor.Id)) actor.Position = next;
        }
        else
        {
            var wander = neighbours.Where(p => p.Distance(home) <= 2).ToArray();
            if (wander.Length > 0 && random.Next(3) == 0) actor.Position = wander[random.Next(wander.Length)];
        }
    }

    private void LeaveWildlifeRemains(Actor actor)
    {
        if (Ecology(actor) is not { } rules || State.Loot.Any(c => c.SourceActor == actor.Id)) return;
        actor.Migration = null; actor.Journey = null; actor.WildlifeAfter = 0;
        NoteWildlifeDeath(actor);
        PressureHabitat(actor.Habitat!.Value, 120);
        State.Loot.Add(new() { Id = "wildlife-" + actor.Id, SourceActor = actor.Id, Position = actor.Position,
            Contents = rules.Drops.ToDictionary(p => Content.Get<ItemDefinition>(p.Key).Stack, p => p.Value) });
    }
    public string LootName(LootCache cache) => IsWorkSupplyCache(cache) ? "Returned supply bundle" : cache.SourceActor > 0 ? State.Actors.First(a => a.Id == cache.SourceActor).Name + " remains" : "Unopened cache";
    public static string LootSprite(LootCache cache) => cache.SourceActor > 0 ? "animal-bones" : "chest";
    private static string MigrationCause(MigrationReason reason) => reason switch
    { MigrationReason.Food => "local food depletion", MigrationReason.Pressure => "habitat pressure", _ => "the old home was cleared or depleted" };
}
