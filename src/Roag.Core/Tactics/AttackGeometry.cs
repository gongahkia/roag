using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum Facing { North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }
public enum AttackDelivery { Melee, Projectile, PiercingLine, Area, Placement, Parry, CompanionCommand, Delayed }
public enum DamageElement { Physical, Lightning, Fire, Acid, Explosion, Pulse, Spirit, Arcane, Echo, Ice, Mist, Resonance, Shard, Shock, Stone, Thorns, Oath }
public sealed record AttackFootprint(string AbilityId, AttackDelivery Delivery, DamageElement Element,
    IReadOnlyList<WorldPosition> Cells);

/// <summary>
/// Pure geometry in actual world coordinates. Obstruction and body queries are supplied by
/// the world, so previews and commits share terrain without copying a battle-sized map.
/// Geometry includes a blocking projectile cell; resolution decides whether it can be hit.
/// </summary>
public static class AttackGeometry
{
    private static readonly (int X, int Y)[] Directions =
        [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)];
    private static readonly (int X, int Y)[] Cross = [(0, 0), (0, -1), (1, 0), (0, 1), (-1, 0)];

    public static bool TryFacing(int dx, int dy, out Facing facing)
    {
        for (int i = 0; i < Directions.Length; i++)
            if (Directions[i] == (dx, dy)) { facing = (Facing)i; return true; }
        facing = default;
        return false;
    }

    public static (int X, int Y) Offset(Facing facing)
    {
        if (!Enum.IsDefined(facing)) throw new ArgumentOutOfRangeException(nameof(facing));
        return Directions[(int)facing];
    }

    public static bool Cardinal(Facing facing)
    {
        _ = Offset(facing);
        return (int)facing % 2 == 0;
    }

    public static IReadOnlyList<WorldPosition> Arc(WorldPosition origin, Facing facing, int halfWidth = 1)
    {
        _ = Offset(facing);
        if (halfWidth is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(halfWidth));
        var cells = new List<WorldPosition>();
        for (int i = -halfWidth; i <= halfWidth; i++)
        {
            var (x, y) = Directions[((int)facing + i + 8) % 8];
            cells.Add(origin.Offset(x, y));
        }
        return cells;
    }

    public static IReadOnlyList<WorldPosition> Line(WorldPosition origin, Facing facing, int range,
        Func<WorldPosition, bool> projectileOpen)
    {
        ArgumentNullException.ThrowIfNull(projectileOpen);
        ArgumentOutOfRangeException.ThrowIfNegative(range);
        var (dx, dy) = Offset(facing);
        var cells = new List<WorldPosition>();
        for (int step = 1; step <= range; step++)
        {
            var cell = origin.Offset(dx * step, dy * step);
            if (dx != 0 && dy != 0)
            {
                var horizontal = cell.Offset(0, -dy);
                var vertical = cell.Offset(-dx, 0);
                if (!projectileOpen(horizontal)) { cells.Add(horizontal); break; }
                if (!projectileOpen(vertical)) { cells.Add(vertical); break; }
            }
            cells.Add(cell);
            if (!projectileOpen(cell)) break;
        }
        return cells;
    }

    public static AttackFootprint For(ClassKind kind, TacticalAction action, WorldPosition origin,
        Facing facing, Func<WorldPosition, bool> projectileOpen,
        Func<WorldPosition, bool>? hasBody = null, bool ownsSpirit = false, bool explicitAim = true)
    {
        var definition = ClassCatalog.Get(kind);
        var (dx, dy) = Offset(facing);
        ArgumentNullException.ThrowIfNull(projectileOpen);
        var element = DamageElement.Physical;
        var delivery = AttackDelivery.Melee;
        IReadOnlyList<WorldPosition> cells;
        if (action == TacticalAction.BasicAttack)
        {
            int range = kind switch
            {
                ClassKind.Ranger => 4,
                ClassKind.Arcanist or ClassKind.Alchemist or ClassKind.Geomancer
                    or ClassKind.Spiritbinder or ClassKind.Tinkerer or ClassKind.Stormcaller => 3,
                ClassKind.Shade or ClassKind.Wayfinder => 2,
                ClassKind.Duelist when explicitAim => 2,
                _ => 1
            };
            if (range > 1)
            {
                delivery = kind == ClassKind.Duelist ? AttackDelivery.PiercingLine : AttackDelivery.Projectile;
                cells = Line(origin, facing, range, projectileOpen);
            }
            else cells = [origin.Offset(dx, dy)];
            element = kind switch
            {
                ClassKind.Arcanist or ClassKind.Stormcaller => DamageElement.Lightning,
                ClassKind.Alchemist => DamageElement.Acid,
                ClassKind.Spiritbinder or ClassKind.Tinkerer => DamageElement.Spirit,
                _ => DamageElement.Physical
            };
            return new(definition.BasicAbilityId, delivery, element, cells);
        }
        if (action != TacticalAction.Special) throw new ArgumentOutOfRangeException(nameof(action));
        switch (kind)
        {
            case ClassKind.Ranger:
                cells = Line(origin, facing, 5, projectileOpen);
                delivery = AttackDelivery.PiercingLine;
                break;
            case ClassKind.Arcanist:
                cells = Burst(origin, facing, 2, square: true, fallback: true, projectileOpen);
                delivery = AttackDelivery.Area; element = DamageElement.Fire;
                break;
            case ClassKind.Alchemist:
                cells = Burst(origin, facing, 3, square: false, fallback: true, projectileOpen);
                delivery = AttackDelivery.Area; element = DamageElement.Explosion;
                break;
            case ClassKind.Berserker:
                cells = Arc(origin, facing, 2);
                break;
            case ClassKind.Sentinel:
                cells = Directions.Select(d => origin.Offset(d.X, d.Y)).ToArray();
                element = DamageElement.Pulse;
                break;
            case ClassKind.Hookrunner:
                cells = Line(origin, facing, 3, projectileOpen);
                delivery = AttackDelivery.Projectile;
                break;
            case ClassKind.Duelist:
                cells = Arc(origin, facing);
                delivery = AttackDelivery.Parry;
                break;
            case ClassKind.Stormcaller:
                cells = Burst(origin, facing, 2, square: false, fallback: false, projectileOpen);
                delivery = AttackDelivery.Delayed; element = DamageElement.Lightning;
                break;
            case ClassKind.Spiritbinder:
                cells = SpiritCommand(origin, facing, projectileOpen, hasBody ?? (_ => false), ownsSpirit);
                delivery = AttackDelivery.CompanionCommand;
                break;
            case ClassKind.Shade or ClassKind.Trapper or ClassKind.Geomancer or ClassKind.Tinkerer or ClassKind.Wayfinder:
                cells = [origin.Offset(dx, dy)];
                delivery = AttackDelivery.Placement;
                break;
            default:
                cells = Arc(origin, facing);
                break;
        }
        return new(definition.SpecialAbilityId, delivery, element, cells);
    }

    private static IReadOnlyList<WorldPosition> Burst(WorldPosition origin, Facing facing, int distance,
        bool square, bool fallback, Func<WorldPosition, bool> projectileOpen)
    {
        var (dx, dy) = Offset(facing);
        var travel = Line(origin, facing, distance, projectileOpen);
        var center = origin.Offset(dx * distance, dy * distance);
        if (travel.Count == 0 || travel[^1] != center || !projectileOpen(center)) return fallback ? travel : [];
        var cells = new List<WorldPosition>();
        if (square)
        {
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    if (projectileOpen(center.Offset(x, y))) cells.Add(center.Offset(x, y));
        }
        else
            foreach (var (x, y) in Cross)
                if (projectileOpen(center.Offset(x, y))) cells.Add(center.Offset(x, y));
        return cells;
    }

    private static IReadOnlyList<WorldPosition> SpiritCommand(WorldPosition origin, Facing facing,
        Func<WorldPosition, bool> projectileOpen, Func<WorldPosition, bool> hasBody, bool ownsSpirit)
    {
        var (dx, dy) = Offset(facing);
        WorldPosition? last = null;
        for (int step = 1; step <= 3; step++)
        {
            var cell = origin.Offset(dx * step, dy * step);
            if (dx != 0 && dy != 0 && (!projectileOpen(cell.Offset(-dx, 0)) || !projectileOpen(cell.Offset(0, -dy)))) break;
            if (!projectileOpen(cell)) break;
            if (hasBody(cell)) return ownsSpirit ? [cell] : last is { } previous ? [previous] : [];
            if (step <= 2) last = cell;
        }
        return last is { } destination ? [destination] : [];
    }
}
