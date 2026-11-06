using Roag.Core.Content;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>Paint once at caller-selected world coordinates. Placement and final-role selection belong to campaign generation.</summary>
    public CampaignBossArena CreateCampaignBossArena(CampaignBossKind kind, WorldPosition center, int seed, bool finalRole,
        int variation = -1, long? readyAt = null)
    {
        if (!Enum.IsDefined(kind) || !Plan.Contains(center.Offset(-7, -7)) || !Plan.Contains(center.Offset(7, 7)))
            throw new ArgumentOutOfRangeException(nameof(center));
        if (variation == -1) variation = RoagRandom.Derive(seed, "campaign-boss:" + kind) % 3;
        if (variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(variation));
        long clock = State.Tactics?.Queue.Clock ?? checked(State.Tick * new TacticalTuning().UnitsPerWorldTick);
        if (readyAt is { } due && due < clock) throw new ArgumentOutOfRangeException(nameof(readyAt));
        if (State.Dungeon != null) throw new InvalidOperationException("Campaign boss arenas require world terrain outside the bounded dungeon run.");
        // Validate the entire footprint before creating a tactical state or mutating terrain.
        var store = State.Tactics?.CampaignBosses ?? new CampaignBossWorldState();
        var arena = new CampaignBossArena { Id = store.NextArena, Kind = kind, Center = center, Seed = seed, Variation = variation, FinalRole = finalRole };
        var reserved = CampaignBossPlacementReservations();
        if (store.NextArena is < 1 or >= 1000 || store.Arenas.Count >= 32 || store.Arenas.Any(a => a.Center.Floor == center.Floor
                && Math.Abs(a.Center.X - center.X) <= 14 && Math.Abs(a.Center.Y - center.Y) <= 14)
            || reserved.Any(arena.Contains)
            || Enumerable.Range(-7, 15).SelectMany(y => Enumerable.Range(-7, 15).Select(x => center.Offset(x, y)))
                .Any(p => Chunks.Seen(p) || Chunks.IsEdited(p) || ObjectAt(p) is { Kind: not "decoration" }))
            throw new InvalidOperationException("Place new boss arenas only on unobserved, unedited, unoccupied ground away from registered fixtures.");
        var combat = State.Tactics == null ? CreateTacticalSimulation() : new TacticalCombat(this, AdvanceTacticalWorld);
        combat.State.CampaignBosses = store;
        for (int y = -7; y <= 7; y++) for (int x = -7; x <= 7; x++)
            Chunks.Edit(center.Offset(x, y), Math.Max(Math.Abs(x), Math.Abs(y)) == 7 && x != 0 && y != 0 ? Material.Wall : Material.Floor);
        if (kind == CampaignBossKind.Bellkeeper)
        {
            foreach (var (x, y) in new[] { (-4, -3), (4, -3), (0, 4) }.Take(finalRole ? 3 : 2))
                arena.AddFixture(CampaignBossFixtureKind.Resonator, Rotate(center, x, y, variation), finalRole ? 8 : 5);
        }
        if (kind == CampaignBossKind.SluiceMaw)
        {
            for (int i = 0; i < (finalRole ? 3 : 2); i++)
            {
                int lane = (i - 1) * 3;
                var channel = new CampaignBossChannel { Cells = Enumerable.Range(-5, 11).Select(x => Rotate(center, x, lane, variation))
                        .Concat(Enumerable.Range(Math.Min(0, lane), Math.Abs(lane) + 1).Select(y => Rotate(center, 0, y, variation))).Distinct().ToArray(),
                    Pool = Rotate(center, i % 2 == 0 ? 5 : -5, lane, variation), Flow = RotatedFacing(Facing.East, variation) };
                arena.Channels.Add(channel);
                foreach (var p in channel.Cells) Chunks.Edit(p, p == center || Math.Abs(p.X - center.X) == 2 || Math.Abs(p.Y - center.Y) == 2 ? Material.Bridge : Material.Water);
                arena.AddFixture(CampaignBossFixtureKind.Valve, Rotate(center, -5 + i * 5, 5, variation), 1, channel: i);
            }
        }
        var actor = ActorFactory.Create(Content, Content.Game.Raider, Seed, State.NextActorId++, center, 0);
        actor.Name = CampaignBossCatalog.Definition(kind, finalRole, variation).Name;
        actor.CombatTrait = ""; actor.Talents.Clear();
        // Sluice-Maw has a physical central island; feast uses the explicit connected channels.
        Chunks.Edit(center, Material.Floor);
        State.Actors.Add(actor); arena.Boss = actor.Id;
        store.NextArena++; store.Arenas.Add(arena);
        combat.RegisterCampaignBoss(actor, arena, readyAt ?? checked(combat.Clock + 100));
        actor.Health = actor.MaxHealth;
        return arena;
    }

    private HashSet<WorldPosition> CampaignBossPlacementReservations()
    {
        var cells = State.Structures.Select(s => s.Position).Concat(State.Discoveries.Select(d => d.Position))
            .Concat(State.Loot.Where(l => !l.Taken).Select(l => l.Position)).ToHashSet();
        foreach (var work in State.Work)
        {
            cells.Add(work.Target);
            if (work.Station is { } station) cells.Add(station);
            if (work.DeliveryStorage is { } delivery) cells.Add(delivery);
            foreach (var supply in work.Supplies ?? []) if (supply.Storage is { } storage) cells.Add(storage);
        }
        foreach (var actor in State.Actors)
        {
            // Corpses and their recoverable equipment are physical history too.
            cells.Add(actor.Position);
            if (actor.Tactics?.Resources is not { } resources) continue;
            foreach (var p in new[] { resources.Decoy, resources.Spirit, resources.Snare, resources.Beacon?.Position,
                resources.Bulwark?.Position, resources.Sentry?.Position, resources.Parry?.Origin })
                if (p is { } position) cells.Add(position);
        }
        if (State.Tactics is not { } tactics) return cells;
        cells.UnionWith(tactics.Attacks.SelectMany(a => a.Cells));
        cells.UnionWith(tactics.CampFires.Select(f => f.Position));
        cells.UnionWith(tactics.Chests.Chests.Select(c => c.Position));
        cells.UnionWith(tactics.Services.Nodes.Select(n => n.Position));
        cells.UnionWith(tactics.Services.Nodes.Where(n => n.Fixture != null).SelectMany(n => n.Fixture!.Cells));
        cells.UnionWith(tactics.Doors.Nodes.Select(n => n.Position));
        cells.UnionWith(tactics.Gates.Nodes.Select(n => n.Position));
        cells.UnionWith(tactics.Quests.Nodes.SelectMany(q => q.Cells));
        cells.UnionWith(tactics.Vaults.Nodes.SelectMany(v => new[] { v.Position, v.KeyPosition }));
        foreach (var secret in tactics.Secrets.Nodes)
        {
            cells.Add(secret.Position); cells.Add(secret.Clue);
            if (secret.Cache is { } cache) cells.Add(cache);
        }
        cells.UnionWith(tactics.RoomFeatures.Nodes.Select(n => n.Position));
        cells.UnionWith(tactics.OptionalTerrain.Nodes.Select(n => n.Position));
        cells.UnionWith(tactics.Loot.Drops.Select(d => d.Position));
        cells.UnionWith(tactics.EnemySnares.Select(n => n.Position));
        cells.UnionWith(tactics.Webs.SelectMany(w => w.Span.Cells()));
        cells.UnionWith(tactics.Silt.Select(n => n.Position));
        cells.UnionWith(tactics.Embers.Select(n => n.Position));
        cells.UnionWith(tactics.Mirrors.Select(n => n.Position));
        return cells;
    }

    internal static WorldPosition Rotate(WorldPosition center, int x, int y, int rotation) => rotation switch
    { 0 => center.Offset(x, y), 1 => center.Offset(-y, x), 2 => center.Offset(-x, -y), _ => throw new ArgumentOutOfRangeException(nameof(rotation)) };
    internal static Facing RotatedFacing(Facing facing, int rotation) => (Facing)(((int)facing + rotation * 2) % 8);
    public WorldRoomId? CampaignBossRoomAt(WorldPosition p) => SnapshotCampaignBossRoomAt(State, p);
    public static WorldRoomId? SnapshotCampaignBossRoomAt(WorldState world, WorldPosition p)
    {
        if (world.Tactics?.CampaignBosses is not { Arenas.Count: > 0 } store) return null;
        for (int i = 0; i < store.Arenas.Count; i++)
            if (store.Arenas[i] is { } arena && arena.Contains(p))
                return new(RoomArea.Dungeon, checked(1000 + (int)arena.Id), p.Floor, 1);
        return null;
    }
    public CampaignBossFixture? CampaignBossFixtureAt(WorldPosition p)
    {
        if (State.Tactics?.CampaignBosses is not { Arenas.Count: > 0 } store) return null;
        // These queries run for every collision/pathfinding probe. Avoid captured
        // predicates even before the absent-state guard, and never allocate props.
        for (int i = 0; i < store.Arenas.Count; i++)
        {
            var arena = store.Arenas[i];
            if (!arena.Contains(p)) continue;
            for (int j = 0; j < arena.Fixtures.Count; j++)
            {
                var fixture = arena.Fixtures[j];
                if (fixture.Health > 0 && fixture.Position == p) return fixture;
            }
        }
        return null;
    }
    private static readonly WorldObject[] CampaignBossObjectTemplates = Enum.GetValues<CampaignBossFixtureKind>()
        .Select(static kind => new WorldObject(TacticalCombat.CampaignBossFixtureName(kind), CampaignBossFixtureSprite(kind),
            "campaign-boss-fixture", kind is not (CampaignBossFixtureKind.Valve or CampaignBossFixtureKind.ReelLine))).ToArray();
    internal WorldObject? CampaignBossObjectAt(WorldPosition p)
    {
        var fixture = CampaignBossFixtureAt(p);
        return fixture == null ? null : CampaignBossObjectTemplates[(int)fixture.Kind];
    }
    internal static string CampaignBossFixtureSprite(CampaignBossFixtureKind kind) => kind switch
    {
        CampaignBossFixtureKind.Resonator => "roag-boss-resonator", CampaignBossFixtureKind.Valve => "roag-boss-valve",
        CampaignBossFixtureKind.WebAnchor or CampaignBossFixtureKind.WebStrand or CampaignBossFixtureKind.ReelLine => "roag-boss-web",
        _ => "bones"
    };
}
