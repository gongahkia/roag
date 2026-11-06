using System.Text.Json.Serialization;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum CampaignBossKind { Bellkeeper, SluiceMaw, Threadmother, OssuaryCaptain }
public enum CampaignBossMove { Soundseek, FuneralChime, ChannelSurge, ReservoirFeast, PartitionWeb, ReelIn, ShieldAdvance, BoneRampart }
public enum CampaignBossFixtureKind { Resonator, Valve, WebAnchor, WebStrand, ReelLine, BoneRampart }
public enum CampaignBossPulseKind { Chime, BurningWeb }

/// <summary>Additional authored roles. The original fifty-entry catalog and pools remain unchanged.</summary>
public static class CampaignBossCatalog
{
    public static string Id(CampaignBossKind kind) => "core:enemy/" + (kind switch
    {
        CampaignBossKind.Bellkeeper => "bellkeeper", CampaignBossKind.SluiceMaw => "sluice_maw",
        CampaignBossKind.Threadmother => "threadmother", CampaignBossKind.OssuaryCaptain => "ossuary_captain",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    });
    public static bool Contains(string id) => Enum.GetValues<CampaignBossKind>().Any(k => Id(k) == id);
    public static CampaignBossKind Kind(string id) => Enum.GetValues<CampaignBossKind>().First(k => Id(k) == id);
    public static string Name(CampaignBossKind kind) => kind switch
    {
        CampaignBossKind.Bellkeeper => "The Bellkeeper", CampaignBossKind.SluiceMaw => "Sluice-Maw",
        CampaignBossKind.Threadmother => "The Threadmother", CampaignBossKind.OssuaryCaptain => "The Ossuary Captain",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public static EnemyDefinition Definition(CampaignBossKind kind, bool finalRole, int variation)
    {
        if (variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(variation));
        int hp = kind switch { CampaignBossKind.Bellkeeper => 52, CampaignBossKind.SluiceMaw => 66, CampaignBossKind.Threadmother => 56, _ => 72 };
        return new(Id(kind), Name(kind) + (finalRole ? " / Final Guardian" : ""), EnemyBrainKind.Guard,
            finalRole ? hp * 2 : hp, finalRole ? 9 : 5, kind == CampaignBossKind.OssuaryCaptain ? 1 : 0,
            finalRole ? 100 : 85, finalRole ? 120 : 160, finalRole ? 100 : 140, 100, 11,
            finalRole ? 100 : 40, finalRole ? 30 : 15,
            kind == CampaignBossKind.Threadmother ? EnemyTargetPriority.VulnerablePrey : EnemyTargetPriority.NearestThreat,
            SpecialEncounter: true);
    }
    public static string MoveName(CampaignBossMove move) => move switch
    {
        CampaignBossMove.Soundseek => "Soundseek", CampaignBossMove.FuneralChime => "Funeral Chime",
        CampaignBossMove.ChannelSurge => "Channel Surge", CampaignBossMove.ReservoirFeast => "Reservoir Feast",
        CampaignBossMove.PartitionWeb => "Partition Web", CampaignBossMove.ReelIn => "Reel In",
        CampaignBossMove.ShieldAdvance => "Shield Advance", CampaignBossMove.BoneRampart => "Bone Rampart",
        _ => throw new ArgumentOutOfRangeException(nameof(move))
    };
    public static string Sprite(CampaignBossKind kind) => "roag-enemy-" + Id(kind)["core:enemy/".Length..].Replace('_', '-');
}

public sealed class CampaignBossMemory
{
    public long Arena { get; set; }
    public long Moves { get; set; }
    public long ExposedUntil { get; set; }
    public Facing ShieldFacing { get; set; } = Facing.South;
    public WorldPosition? LastNoise { get; set; }
    public long NoiseAt { get; set; }
    public long NoiseAction { get; set; }
    internal void Validate()
    {
        if (Arena <= 0 || Moves < 0 || ExposedUntil < 0 || !Enum.IsDefined(ShieldFacing) || !AttackGeometry.Cardinal(ShieldFacing)
            || NoiseAt < 0 || NoiseAction < 0 || LastNoise == null && (NoiseAt != 0 || NoiseAction != 0))
            throw new InvalidDataException("Invalid campaign boss memory.");
    }
}

public sealed record CampaignBossCast(CampaignBossMove Move, int Feature = -1, Facing Facing = Facing.South);
public sealed class CampaignBossFixture
{
    public long Id { get; set; }
    public CampaignBossFixtureKind Kind { get; set; }
    public WorldPosition Position { get; set; }
    public int Health { get; set; }
    public long Group { get; set; }
    public int Channel { get; set; } = -1;
    [JsonIgnore] public bool Solid => Health > 0 && Kind is CampaignBossFixtureKind.Resonator or CampaignBossFixtureKind.WebAnchor
        or CampaignBossFixtureKind.WebStrand or CampaignBossFixtureKind.BoneRampart;
}
public sealed class CampaignBossChannel
{
    public WorldPosition[] Cells { get; set; } = [];
    public WorldPosition Pool { get; set; }
    public Facing Flow { get; set; }
    public bool Drained { get; set; }
}
public sealed class CampaignBossArena
{
    public const int Radius = 7;
    public long Id { get; set; }
    public long Boss { get; set; }
    public CampaignBossKind Kind { get; set; }
    public WorldPosition Center { get; set; }
    public int Seed { get; set; }
    public int Variation { get; set; }
    public bool FinalRole { get; set; }
    public long NextFixture { get; set; } = 1;
    public List<CampaignBossFixture> Fixtures { get; set; } = [];
    public List<CampaignBossChannel> Channels { get; set; } = [];
    public bool Contains(WorldPosition p) => p.Floor == Center.Floor && Math.Abs((long)p.X - Center.X) <= Radius && Math.Abs((long)p.Y - Center.Y) <= Radius;
    public CampaignBossFixture AddFixture(CampaignBossFixtureKind kind, WorldPosition p, int health, long group = 0, int channel = -1)
    {
        var fixture = new CampaignBossFixture { Id = NextFixture++, Kind = kind, Position = p, Health = health, Group = group, Channel = channel };
        Fixtures.Add(fixture); return fixture;
    }
}
public sealed record CampaignBossPulse(long Id, long Arena, long Source, CampaignBossPulseKind Kind,
    WorldPosition Origin, WorldPosition[] Cells, long Due, long Schedule, int Damage, long Fixture = 0,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BoonCause? Causality = null);
public sealed class CampaignBossWorldState
{
    public long NextArena { get; set; } = 1;
    public List<CampaignBossArena> Arenas { get; set; } = [];
    public List<CampaignBossPulse> Pulses { get; set; } = [];
}
