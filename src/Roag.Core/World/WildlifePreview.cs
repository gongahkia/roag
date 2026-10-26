using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record WildlifeObservation(int Seed, string Id, WorldPosition Habitat, WorldPosition Before, WorldPosition After,
    string Outside, bool Threatened, int Elapsed, bool InCombat, bool Fled, string Description);
public sealed record WildlifeAudit(bool Passed, int Seeds, int Encounters, int Escapes, Dictionary<string, int> DistinctHabitats, WildlifeObservation[] Observations, string Interpretation);

public static class WildlifePreview
{
    public static WildlifeHabitat Find(WorldSession world, string id)
    {
        var ecology = world.Content.Get<CharacterDefinition>(id).Ecology ?? throw new ArgumentException("Choose a wildlife character.");
        IEnumerable<WorldPosition> centers = ecology.Underground ? world.Plan.Sites.Select(s => s.Center with { Floor = 1 })
            : world.Plan.BiomeRegions.Where(r => ecology.Biomes.Contains(r.Biome)).Select(r => new WorldPosition(r.X, r.Y));
        var sampled = new HashSet<ChunkKey>();
        foreach (var center in centers)
            for (int radius = 0; radius <= 3; radius++)
                for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++)
                {
                    var key = new ChunkKey(center.Chunk.X + x, center.Chunk.Y + y, center.Floor);
                    if (sampled.Add(key) && world.WildlifeHabitatAt(key) is { } habitat && habitat.Character == id) return habitat;
                }
        throw new InvalidDataException("No seeded wildlife habitat found in the bounded sample: " + id);
    }

    /// <summary>Observe a real seeded habitat, then clear only its approach in an isolated demonstration world.</summary>
    public static Actor Prepare(WorldSession world, string id)
    {
        var habitat = Find(world, id); var hero = world.Party.First();
        world.State.Position = hero.Position = habitat.Home; world.Reveal();
        var animal = world.State.Actors.Single(a => a.Habitat == habitat.Home);
        var origin = habitat.Home.Offset(6, 0);
        for (int dx = -2; dx <= 8; dx++) for (int dy = -2; dy <= 2; dy++)
        {
            var p = habitat.Home.Offset(dx, dy);
            if (p != habitat.Home && world.Plan.Contains(p) && world.Chunks.At(p) is not (Material.StairsUp or Material.StairsDown)) world.Chunks.Edit(p, Material.Floor);
        }
        world.State.Position = hero.Position = origin; world.State.WorkRally = null; world.Reveal();
        foreach (var other in world.State.Actors.Where(a => !a.Recruited && a.Id != animal.Id && a.Position.Distance(origin) < 100))
            other.Position = world.Plan.Towns[(other.HomeTown + 12) % 24].Center with { Floor = other.Position.Floor };
        world.Paused = true; return animal;
    }

    public static WildlifeObservation Run(ContentCatalog catalog, string id, int seed)
    {
        var world = WorldSession.Create(seed, catalog); var animal = Prepare(world, id); var rules = world.Ecology(animal)!;
        var before = animal.Position; string outside = world.WildlifeDescription(animal);
        var approach = animal.Habitat!.Value.Offset(2, 0);
        world.State.Position = world.Party.First().Position = approach; world.Reveal(); world.Paused = false;
        bool threatened = world.IsHostile(animal);
        int elapsed = world.AdvanceTicks(30);
        return new(seed, id, animal.Habitat.Value, before, animal.Position, outside, threatened, elapsed, world.IsInCombat,
            rules.Behavior == "flee" && animal.Position.Distance(approach) > before.Distance(approach), world.WildlifeDescription(animal));
    }

    public static WildlifeAudit Audit(ContentCatalog catalog, int seed, int samples)
    {
        if (samples is < 1 or > 32) throw new ArgumentException("Audit 1..32 worlds.");
        var observations = new List<WildlifeObservation>();
        for (int n = 0; n < samples; n++) foreach (var id in catalog.Game.Wildlife) observations.Add(Run(catalog, id, unchecked(seed + n)));
        var distinct = observations.GroupBy(o => o.Id).ToDictionary(g => g.Key, g => g.Select(o => o.Habitat).Distinct().Count());
        bool passed = observations.Count > 0 && distinct.Values.All(count => count >= Math.Min(2, samples))
            && observations.All(o => catalog.Get<CharacterDefinition>(o.Id).Ecology!.Behavior == "flee" ? o.Fled && !o.InCombat && !o.Threatened : o.InCombat && o.Threatened);
        return new(passed, samples, observations.Count(o => o.InCombat), observations.Count(o => o.Fled), distinct, observations.ToArray(),
            "Bounded seeded habitat search and real movement/contact handlers, with cleared approaches in isolated fixtures. Verifies behaviour and habitat variety, not population balance or long-term ecology.");
    }
}
