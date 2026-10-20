using Ithica.Core.Expeditions;

namespace Ithica.Core.Generation;

public static class LunarGenerator
{
    public const int CurrentVersion = 3;
    public static Zone Create(string id, int seed, bool structure, bool rescue, int generatorVersion = CurrentVersion)
    {
        if (generatorVersion == 2) return CreateLegacy(id, seed, structure, rescue);
        if (generatorVersion != CurrentVersion) throw new ArgumentOutOfRangeException(nameof(generatorVersion));
        var zone = structure ? LunarAreas.Compartments(id, seed) : LunarAreas.Surface(id, seed, rescue);
        var validity = Validate(zone);
        if (!validity.Valid) throw new InvalidOperationException($"Lunar seed {seed} ({(structure ? "compartments" : "surface")}): " + string.Join("; ", validity.Errors));
        var eligible = Enumerable.Range(0, zone.Tiles.Length).Where(i => !zone.Safe[i] && zone.Walkable(new(i % zone.Width, i / zone.Width))).ToArray();
        if (eligible.Length > 0)
        {
            int i = eligible[GenerationSeeds.Stream(seed, 331).Next(eligible.Length)];
            zone.Environment.Cells[i] = zone.Environment.Sources[i] = HazardKind.Corruption;
            zone.Environment.Age[i] = 1;
        }
        return zone;
    }

    private static Zone CreateLegacy(string id, int seed, bool structure, bool rescue)
    {
        // Keep the tested graph/CA algorithms. Recast collision and artwork together;
        // no forest, pond, flame or fantasy quest object survives into the lunar site.
        var source = structure ? WorldBuilder.Abbey(seed, new()) : WorldBuilder.Forest(seed);
        var zone = new Zone(id, structure ? "Below the surface" : "Surface", "A signal, and the marks around it", source.Width, source.Height, Ground.Void);
        zone.Generation = source.Generation;
        zone.Generation.Algorithm = structure ? "Lunar compartments / WFC sockets and cellular damage" : "Lunar rubble / cellular terrain and protected approaches";
        zone.Generation.Rooms = source.Generation.Rooms.Select(r => r with { Variant = r.Variant switch { "cloister" => "intact", "cistern" => "service", _ => "damaged" } }).ToList();
        var dressing = GenerationSeeds.Stream(seed, 93);
        for (int i = 0; i < zone.Tiles.Length; i++)
        {
            zone.Tiles[i] = source.Tiles[i] switch
            {
                Ground.Tree => Ground.Wall,
                Ground.Water => Ground.Wall,
                Ground.Grass or Ground.Wood => Ground.Stone,
                Ground.Bridge => Ground.Path,
                var ground => ground
            };
            zone.Safe[i] = source.Safe[i];
            zone.GroundSprites[i] = zone.Tiles[i] == Ground.Wall
                ? structure ? "cracked-wall" : "boulder"
                : structure ? "flagstone" : dressing.Next(3) == 0 ? "rock-floor" : "grit";
        }
        Cell Entry() => source.Objects.Single(o => o.Id == (structure ? "to-forest" : "to-town")).Position;
        Cell At(string objectId) => source.Objects.Single(o => o.Id == objectId).Position;
        zone.Objects.Add(new("return", "Return to shelter", ObjectKind.Return, Entry(), "stairs-up", Detail: "Travel takes 8 ticks."));
        if (rescue)
            zone.Objects.Add(new("survivor", "A person", ObjectKind.Rescue, At("wilds-hounds"), "resident", Detail: "Share supplies and escort them home."));
        else
        {
            string encounter = structure ? "presence-heavy" : "presence";
            zone.Objects.Add(new(encounter, "Movement", ObjectKind.Encounter, At(structure ? "abbey-abbot" : "wilds-hounds"), "bandit"));
            zone.Objects.Add(new("component", "A usable component", ObjectKind.Salvage, At(structure ? "relic" : "iron-cache"), "crystal", Detail: encounter));
            if (structure)
            {
                zone.Objects.Add(new("terminal", "Damaged pump", ObjectKind.Terminal, At("abbey-chest"), "well", Detail: "Repair with 2 carried water. Securing it supplies home with water."));
                var gate = At("iron-gate");
                zone.Objects.Add(new("seal", "Sealed passage", ObjectKind.Gate, gate, "gate", true, "Restore the damaged pump to cycle the seal."));
            }
        }
        // Keep objective approaches clear even when the source object's former use differed.
        foreach (var obj in zone.Objects)
            foreach (var p in new[] { obj.Position })
                if (zone.Inside(p)) { zone.Set(p, Ground.Stone); zone.Safe[zone.Index(p)] = true; }
        var validity = Validate(zone);
        if (!validity.Valid) throw new InvalidOperationException($"Lunar seed {seed} ({(structure ? "compartments" : "surface")}): " + string.Join("; ", validity.Errors));
        // Radiation/interference occupies unprotected ground. There is no outdoor fire.
        var eligible = Enumerable.Range(0, zone.Tiles.Length).Where(i => !zone.Safe[i] && zone.Walkable(new(i % zone.Width, i / zone.Width))).ToArray();
        if (eligible.Length > 0)
        {
            int i = eligible[dressing.Next(eligible.Length)];
            zone.Environment.Cells[i] = zone.Environment.Sources[i] = HazardKind.Corruption;
            zone.Environment.Age[i] = 1;
        }
        return zone;
    }

    public static QuestValidation Validate(Zone zone)
    {
        var errors = new List<string>();
        var returns = zone.Objects.Where(o => o.Kind == ObjectKind.Return).ToArray();
        if (returns.Length != 1) return new(false, ["Expected one return approach."]);
        var entry = returns[0];
        if (entry is null) return new(false, ["Missing return approach."]);
        if (zone.Objects.Select(o => o.Id).Distinct().Count() != zone.Objects.Count) return new(false, ["Duplicate object."]);
        if (new[] { ObjectKind.Gate, ObjectKind.Terminal, ObjectKind.Salvage, ObjectKind.Encounter, ObjectKind.Rescue }.Any(kind => zone.Objects.Count(o => o.Kind == kind) > 1))
            return new(false, ["Duplicate objective kind."]);
        foreach (var obj in zone.Objects)
            if (!zone.Walkable(obj.Position) || obj.Position != entry.Position && zone.FindPath(entry.Position, [obj.Position]).Count == 0)
                errors.Add("Unreachable " + obj.Id);
        if (zone.Objects.SingleOrDefault(o => o.Id == "seal") is { } gate)
        {
            var terminal = zone.Objects.SingleOrDefault(o => o.Kind == ObjectKind.Terminal);
            var salvage = zone.Objects.SingleOrDefault(o => o.Kind == ObjectKind.Salvage);
            if (terminal is null || zone.FindPath(entry.Position, [terminal.Position], p => p != gate.Position).Count == 0)
                errors.Add("Seal prerequisite is inaccessible.");
            if (salvage is not null && zone.FindPath(entry.Position, [salvage.Position], p => p != gate.Position).Count > 0)
                errors.Add("Seal can be bypassed.");
        }
        if (zone.Generation.Version >= 3)
        {
            var connected = LunarPaths.Reachable(zone, entry.Position);
            var protectedRoutes = LunarPaths.Reachable(zone, entry.Position, safeOnly: true);
            if (zone.Objects.Any(o => !protectedRoutes.Contains(o.Position))) errors.Add("Unprotected objective route.");
            int floors = zone.Tiles.Count(g => !Zone.Solid(g));
            if (connected.Count != floors) errors.Add("Disconnected traversable terrain.");
            if (zone.Objects.Select(o => o.Position).Distinct().Count() != zone.Objects.Count) errors.Add("Overlapping objectives.");
            if (zone.Objects.Any(o => !zone.Inside(o.Position) || !zone.Safe[zone.Index(o.Position)])) errors.Add("Unprotected objective.");
        }
        if (zone.Tiles.Any(g => g is Ground.Grass or Ground.Tree or Ground.Water or Ground.Wood)) errors.Add("Non-lunar terrain.");
        if (zone.Environment.Cells.Any(h => h is HazardKind.Fire or HazardKind.Spores) || zone.Environment.Sources.Any(h => h is HazardKind.Fire or HazardKind.Spores)) errors.Add("Ineligible outdoor hazard.");
        return new(errors.Count == 0, errors.ToArray());
    }
}
