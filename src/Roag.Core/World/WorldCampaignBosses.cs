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
        var combat = State.Tactics == null ? CreateTacticalSimulation() : new TacticalCombat(this, AdvanceTacticalWorld);
        var store = combat.State.CampaignBosses;
        var arena = new CampaignBossArena { Id = store.NextArena, Kind = kind, Center = center, Seed = seed, Variation = variation, FinalRole = finalRole };
        if (store.Arenas.Count >= 32 || store.Arenas.Any(a => a.Center.Floor == center.Floor
                && Math.Abs(a.Center.X - center.X) <= 14 && Math.Abs(a.Center.Y - center.Y) <= 14)
            || State.Actors.Any(a => a.Life != LifeState.Dead && arena.Contains(a.Position))
            || Enumerable.Range(-7, 15).SelectMany(y => Enumerable.Range(-7, 15).Select(x => center.Offset(x, y)))
                .Any(p => Chunks.Seen(p) || Chunks.IsEdited(p) || ObjectAt(p) is { Kind: not "decoration" }))
            throw new InvalidOperationException("Place new boss arenas only on unobserved, unedited, unoccupied ground away from registered fixtures.");
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

    internal static WorldPosition Rotate(WorldPosition center, int x, int y, int rotation) => rotation switch
    { 0 => center.Offset(x, y), 1 => center.Offset(-y, x), 2 => center.Offset(-x, -y), _ => throw new ArgumentOutOfRangeException(nameof(rotation)) };
    internal static Facing RotatedFacing(Facing facing, int rotation) => (Facing)(((int)facing + rotation * 2) % 8);
    public WorldRoomId? CampaignBossRoomAt(WorldPosition p) => SnapshotCampaignBossRoomAt(State, p);
    public static WorldRoomId? SnapshotCampaignBossRoomAt(WorldState world, WorldPosition p)
    {
        var arena = world.Tactics?.CampaignBosses.Arenas.FirstOrDefault(a => a.Contains(p));
        return arena == null ? null : new(RoomArea.Dungeon, checked(1000 + (int)arena.Id), p.Floor, 1);
    }
    public CampaignBossFixture? CampaignBossFixtureAt(WorldPosition p)
    {
        if (State.Tactics?.CampaignBosses is not { Arenas.Count: > 0 } store) return null;
        return store.Arenas.FirstOrDefault(a => a.Contains(p))?.Fixtures.FirstOrDefault(f => f.Health > 0 && f.Position == p);
    }
    internal WorldObject? CampaignBossObjectAt(WorldPosition p)
    {
        var fixture = CampaignBossFixtureAt(p);
        return fixture == null ? null : new(TacticalCombat.CampaignBossFixtureName(fixture.Kind), CampaignBossFixtureSprite(fixture.Kind), "campaign-boss-fixture", fixture.Solid);
    }
    internal static string CampaignBossFixtureSprite(CampaignBossFixtureKind kind) => kind switch
    {
        CampaignBossFixtureKind.Resonator => "roag-boss-resonator", CampaignBossFixtureKind.Valve => "roag-boss-valve",
        CampaignBossFixtureKind.WebAnchor or CampaignBossFixtureKind.WebStrand or CampaignBossFixtureKind.ReelLine => "roag-boss-web",
        _ => "bones"
    };
}
