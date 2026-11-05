using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

/// <summary>World party policy; actors, terrain and the single timeline own live simulation state.</summary>
public sealed class NativeCampaignState
{
    public long StartedAt { get; set; }
    public Dictionary<long, long> Following { get; set; } = [];
    public List<DungeonRecruit> Recruits { get; set; } = [];
    public HashSet<int> AdventurerTowns { get; set; } = [];
    public NativeWildlifeBreeding Breeding { get; set; } = new();
    public NativeEncounterRewards EncounterRewards { get; set; } = new();

    internal void Validate(WorldPlan plan, WorldState world, IReadOnlyDictionary<long, Actor> actors)
    {
        if (StartedAt < 0 || StartedAt > world.Tick || world.Dungeon != null || world.Tactics is not { DrivesWorldClock: true }
            || world.RoagCombat || world.RoagCombatState.Length != 0 || plan.RoagNative
            || Following == null || Following.Any(p => p.Key == p.Value || !world.Party.Contains(p.Key) || !world.Party.Contains(p.Value)
                || Following.ContainsKey(p.Value) || actors[p.Key].Life == LifeState.Dead || actors[p.Value].Life == LifeState.Dead)
            || AdventurerTowns == null || AdventurerTowns.Any(id => id < 0 || id >= plan.Towns.Count)
            || Recruits == null || Recruits.Count > 512 || Recruits.Any(r => r == null)
            || Recruits.Select(r => r.Actor).Distinct().Count() != Recruits.Count
            || Recruits.Any(r => !Enum.IsDefined(r.Class) || !actors.TryGetValue(r.Actor, out var actor) || actor.Kind != ActorKind.Person
                || actor.Recruited != world.Party.Contains(actor.Id)
                || (actor.Recruited ? actor.Tactics?.Build == null || actor.Tactics.Resident != null : actor.Tactics != null && actor.Tactics.Resident == null)
                || actor.Tactics?.Build is { } build && build.Class != r.Class)
            || world.Party.Any(id => !actors.TryGetValue(id, out var actor) || actor.Tactics?.Build == null || !world.Tactics.Participants.Contains(id)))
            throw new InvalidDataException("Invalid native world party policy or fixed adventurer.");
        if (Breeding == null) throw new InvalidDataException("Missing native breeding state.");
        Breeding.Validate(world, actors);
        if (EncounterRewards == null) throw new InvalidDataException("Missing native encounter rewards.");
        EncounterRewards.Validate(world, actors);
    }
}

public sealed partial class WorldSession
{
    public long NativeElapsedTime => State.Tactics is { } tactics
        ? tactics.Queue.Clock - (State.NativeCampaign?.StartedAt ?? 0) * tactics.Tuning.UnitsPerWorldTick : 0;
    public bool HasNativeParty => State.Dungeon != null || State.NativeCampaign != null;
    internal Dictionary<long, long>? PartyFollowing => State.NativeCampaign?.Following ?? State.Dungeon?.Following;
    public IReadOnlyDictionary<long, long> DungeonFollowing => PartyFollowing ?? [];
    public IReadOnlyList<DungeonRecruit> NativeAdventurers => State.NativeCampaign?.Recruits ?? State.Dungeon?.Recruits ?? [];

    /// <summary>Persistent living world with native party actions; bounded runs remain independently loadable.</summary>
    public static WorldSession CreateNativeCampaign(int seed, ClassKind kind = ClassKind.Adventurer)
    {
        // Initial exploration must use the native founder's actual calendar/weather
        // sight after its timeline exists, without inheriting prototype sight.
        var world = CreateWorld(seed, null, null, roagNative: false, floorCount: 8, populateWorld: true, observe: false);
        try
        {
            // A fresh expedition begins at dawn, with the same absolute action
            // clock driving calendar, routines and recovery from that instant.
            world.State.Tick = 6 * TicksPerHour;
            world.State.NativeCampaign = new() { StartedAt = world.State.Tick };
            var combat = world.CreateTacticalSimulation(new(EnableSurvival: true));
            combat.State.Chests.AvailableBoons = BoonCatalog.Default.NormalPool.ToArray();
            var hero = world.Party.Single();
            combat.Register(hero, new() { Build = CharacterBuild.Create(kind, world.DungeonSeed) });
            hero.SpriteId = NativeAppearance.ForClass(kind);
            world.EnrollCampaignCreatures(combat);
            world.Reveal(); combat.AdvanceToPartyDecision();
            return world;
        }
        catch { world.Dispose(); throw; }
    }

    private void EnrollCampaignCreatures(TacticalCombat? combat = null)
    {
        if (State.NativeCampaign == null) return;
        combat ??= DungeonCombat;
        foreach (var actor in State.Actors.Where(a => a is { Life: LifeState.Alive, Tactics: null, Kind: ActorKind.Beast or ActorKind.Raider }).OrderBy(a => a.Id))
            combat.RegisterRetainedEnemy(actor);
    }
}
