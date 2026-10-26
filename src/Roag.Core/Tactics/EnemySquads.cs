using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum EnemySquadKind { WardedBodyguards, HealerRushwings, SnaresAndSiege, ShieldAndArcher, CaptainAndFollowers }

/// <summary>Persistent group identity and home, independent of local combat participation.</summary>
public sealed record EnemySquad(string Id, EnemySquadKind Kind, long Leader, WorldPosition Home)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || !Enum.IsDefined(Kind) || Leader <= 0)
            throw new InvalidDataException("Invalid enemy squad identity.");
    }
}

public sealed record EnemySnare(long Source, WorldPosition Position);
public sealed record EnemyProtection(string Reason, long Source);

public static class EnemySquads
{
    public const int WardRadius = 4, GuardLeash = 6, RallyRadius = 5, SnareCap = 2, SnareDelay = 40;

    public static (string Leader, string Follower) Roster(EnemySquadKind kind) => kind switch
    {
        EnemySquadKind.WardedBodyguards => ("core:enemy/ward_weaver", "core:enemy/bodyguard_brute"),
        EnemySquadKind.HealerRushwings => ("core:enemy/mend_acolyte", "core:enemy/rushwing"),
        EnemySquadKind.SnaresAndSiege => ("core:enemy/snare_scribe", "core:enemy/siege_wisp"),
        EnemySquadKind.ShieldAndArcher => ("core:enemy/shield_bearer", "core:enemy/pin_archer"),
        EnemySquadKind.CaptainAndFollowers => ("core:enemy/banner_captain", "core:enemy/banner_follower"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static bool Clear(WorldPosition from, WorldPosition to, Func<WorldPosition, bool> open)
    {
        if (from.Floor != to.Floor) return false;
        int dx = Math.Abs(to.X - from.X), dy = Math.Abs(to.Y - from.Y);
        int sx = to.X > from.X ? 1 : -1, sy = to.Y > from.Y ? 1 : -1, error = dx - dy;
        while (from != to)
        {
            var old = from; int twice = error * 2;
            if (twice > -dy) { error -= dy; from = from.Offset(sx, 0); }
            if (twice < dx) { error += dx; from = from.Offset(0, sy); }
            if (from.X != old.X && from.Y != old.Y && (!open(new(from.X, old.Y, from.Floor)) || !open(new(old.X, from.Y, from.Floor)))) return false;
            if (!open(from)) return false;
        }
        return true;
    }
}
