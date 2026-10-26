using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record BoonPatternOrigin(WorldPosition Position, WorldPosition? Source = null, WorldPosition? From = null);
public sealed record BoonWeaponShape(IReadOnlyList<WorldPosition> Cells, AttackDelivery Delivery, BoonWeaponForm? Form);

/// <summary>Original boon geometry on world tiles. Pure queries: no RNG, actor mutation or clock advancement.</summary>
public static class BoonGeometry
{
    private static readonly (int X, int Y)[] Cardinal = [(0, -1), (1, 0), (0, 1), (-1, 0)];
    private static readonly (int X, int Y)[] Diagonal = [(-1, -1), (1, -1), (1, 1), (-1, 1)];
    private static readonly (int X, int Y)[] Knight = [(-1, -2), (1, -2), (2, -1), (2, 1), (1, 2), (-1, 2), (-2, 1), (-2, -1)];

    public static bool Axis(WorldPosition target, WorldPosition? origin, BoonAxis axis)
    {
        if (origin is not { } from || from.Floor != target.Floor) return false;
        int dx = target.X - from.X, dy = target.Y - from.Y;
        return axis switch
        {
            BoonAxis.Cardinal => (dx == 0) != (dy == 0),
            BoonAxis.Diagonal => dx != 0 && Math.Abs(dx) == Math.Abs(dy),
            _ => throw new ArgumentOutOfRangeException(nameof(axis))
        };
    }

    /// <summary>Short supercover segment with strict diagonal corner checks.</summary>
    public static bool ClearSegment(WorldPosition from, WorldPosition to, Func<WorldPosition, bool> open)
    {
        if (from.Floor != to.Floor) return false;
        int nx = Math.Abs(to.X - from.X), ny = Math.Abs(to.Y - from.Y);
        int sx = Math.Sign(to.X - from.X), sy = Math.Sign(to.Y - from.Y), ix = 0, iy = 0;
        var at = from;
        while (ix < nx || iy < ny)
        {
            long a = (1L + 2L * ix) * ny, b = (1L + 2L * iy) * nx;
            var next = a == b ? at.Offset(sx, sy) : a < b ? at.Offset(sx, 0) : at.Offset(0, sy);
            if (a <= b) ix++;
            if (a >= b) iy++;
            if (!open(next) || next.X != at.X && next.Y != at.Y
                && (!open(new(at.X, next.Y, at.Floor)) || !open(new(next.X, at.Y, at.Floor)))) return false;
            at = next;
        }
        return true;
    }

    public static IReadOnlyList<WorldPosition> Pattern(BoonPattern pattern, BoonPatternOrigin origin, Func<WorldPosition, bool> open)
    {
        if (!Enum.IsDefined(pattern)) throw new ArgumentOutOfRangeException(nameof(pattern));
        ArgumentNullException.ThrowIfNull(open);
        if (pattern == BoonPattern.Backtrail && origin.From == null) return [];
        var center = pattern == BoonPattern.Backtrail ? origin.From!.Value : origin.Position;
        if (center.Floor != origin.Position.Floor || origin.Source is { } source && source.Floor != center.Floor) return [];
        var cells = new List<WorldPosition>(); var seen = new HashSet<WorldPosition>();
        void Append(WorldPosition p) { if (open(p) && seen.Add(p)) cells.Add(p); }
        void Ray(int dx, int dy, int length)
        {
            var at = center;
            for (int i = 0; i < length; i++)
            {
                var next = at.Offset(dx, dy);
                if (!open(next) || dx != 0 && dy != 0 && (!open(at.Offset(dx, 0)) || !open(at.Offset(0, dy)))) break;
                Append(next); at = next;
            }
        }
        int dx = origin.Source is { } x ? Math.Sign(center.X - x.X) : 0;
        int dy = origin.Source is { } y ? Math.Sign(center.Y - y.Y) : 0;
        switch (pattern)
        {
            case BoonPattern.Lance:
                if (dx != 0 || dy != 0) Ray(dx, dy, 2);
                break;
            case BoonPattern.Rook:
                if (Axis(origin.Position, origin.Source, BoonAxis.Cardinal)) { Ray(-dy, dx, 2); Ray(dy, -dx, 2); }
                break;
            case BoonPattern.Bishop:
                if (Axis(origin.Position, origin.Source, BoonAxis.Diagonal)) foreach (var d in Diagonal) Ray(d.X, d.Y, 2);
                break;
            case BoonPattern.Knight:
                foreach (var d in Knight)
                {
                    bool Route(bool horizontalFirst)
                    {
                        var at = center;
                        for (int leg = 0; leg < 2; leg++)
                        {
                            bool horizontal = (leg == 0) == horizontalFirst;
                            int count = horizontal ? Math.Abs(d.X) : Math.Abs(d.Y);
                            for (int i = 0; i < count; i++)
                            {
                                at = at.Offset(horizontal ? Math.Sign(d.X) : 0, horizontal ? 0 : Math.Sign(d.Y));
                                if (!open(at)) return false;
                            }
                        }
                        return true;
                    }
                    if (Route(true) || Route(false)) Append(center.Offset(d.X, d.Y));
                }
                break;
            case BoonPattern.Crown:
                for (int oy = -2; oy <= 2; oy++) for (int ox = -2; ox <= 2; ox++)
                    if (Math.Max(Math.Abs(ox), Math.Abs(oy)) == 2 && ClearSegment(center, center.Offset(ox, oy), open)) Append(center.Offset(ox, oy));
                break;
            case BoonPattern.Crosswind:
                foreach (var d in Cardinal) Ray(d.X, d.Y, 2);
                break;
            case BoonPattern.Fan:
                if (dx != 0 || dy != 0) for (int side = -1; side <= 1; side++)
                {
                    var p = center.Offset(dx - dy * side, dy + dx * side);
                    if (ClearSegment(center, p, open)) Append(p);
                }
                break;
            case BoonPattern.Backtrail:
                Append(center); foreach (var d in Cardinal) Ray(d.X, d.Y, 1);
                break;
        }
        return cells;
    }

    public static BoonWeaponShape Weapon(BoonWeaponForm form, WorldPosition owner, Facing facing,
        IReadOnlyList<WorldPosition> current, IReadOnlyList<WorldPosition> baseCells, AttackDelivery delivery,
        Func<WorldPosition, bool> open, Func<WorldPosition, bool> livingBody, bool explicitAim = true,
        bool knownOnly = false, Func<WorldPosition, bool>? visible = null)
    {
        if (!Enum.IsDefined(form)) throw new ArgumentOutOfRangeException(nameof(form));
        ArgumentNullException.ThrowIfNull(open); ArgumentNullException.ThrowIfNull(livingBody);
        if (knownOnly && visible is null) throw new ArgumentNullException(nameof(visible));
        var (dx, dy) = AttackGeometry.Offset(facing);
        if (!explicitAim && form is BoonWeaponForm.PommelReturn or BoonWeaponForm.NeedleFlight or BoonWeaponForm.SplitThrust)
            return new(current.ToArray(), delivery, null);
        var cells = new List<WorldPosition>(); var seen = new HashSet<WorldPosition>();
        void Append(WorldPosition p, bool blocked = false)
        { if (p != owner && p.Floor == owner.Floor && (blocked || open(p)) && seen.Add(p)) cells.Add(p); }
        switch (form)
        {
            case BoonWeaponForm.PommelReturn:
            case BoonWeaponForm.Backswing:
                foreach (var p in current) Append(p, true);
                IReadOnlyList<WorldPosition> extra = form == BoonWeaponForm.PommelReturn ? [owner.Offset(-dx, -dy)]
                    : AttackGeometry.Arc(owner, (Facing)(((int)facing + 4) % 8));
                foreach (var p in extra) if (ClearSegment(owner, p, open)) Append(p);
                break;
            case BoonWeaponForm.NeedleFlight:
                WorldPosition? blocked = null;
                foreach (var p in baseCells)
                {
                    if (!open(p)) { blocked = p; break; }
                    Append(p);
                }
                foreach (var p in current) if (open(p)) Append(p);
                if (blocked is { } end) Append(end, true);
                delivery = AttackDelivery.PiercingLine;
                break;
            case BoonWeaponForm.HollowSigil:
                var center = owner.Offset(2 * dx, 2 * dy);
                if (!baseCells.Contains(center) || !open(center)) return new(current.ToArray(), delivery, null);
                foreach (var p in current) if (p != center) Append(p, true);
                break;
            case BoonWeaponForm.DistantBastion:
                for (int oy = -2; oy <= 2; oy++) for (int ox = -2; ox <= 2; ox++)
                    if (Math.Abs(ox) + Math.Abs(oy) == 2 && ClearSegment(owner, owner.Offset(ox, oy), open)) Append(owner.Offset(ox, oy));
                delivery = AttackDelivery.Area;
                break;
            case BoonWeaponForm.VolatileVial:
                int contact = -1;
                for (int i = 0; i < baseCells.Count; i++)
                {
                    var p = baseCells[i];
                    if (!open(p)) break;
                    if (p == owner || !livingBody(p)) continue;
                    if (knownOnly && !visible!(p)) return new(current.ToArray(), delivery, form);
                    contact = i; break;
                }
                if (contact < 0) return new(current.ToArray(), delivery, form);
                for (int i = 0; i <= contact; i++) Append(baseCells[i]);
                foreach (var d in Cardinal)
                {
                    var p = baseCells[contact].Offset(d.X, d.Y);
                    if (ClearSegment(baseCells[contact], p, open)) Append(p);
                }
                delivery = AttackDelivery.Area;
                break;
            case BoonWeaponForm.SplitThrust:
                if (baseCells.Count == 0) return new(current.ToArray(), delivery, null);
                var stem = baseCells[0]; Append(stem, true);
                if (open(stem)) foreach (int side in new[] { -1, 1 })
                {
                    var p = owner.Offset(dx * 2 - dy * side, dy * 2 + dx * side);
                    if (ClearSegment(stem, p, open)) Append(p);
                }
                delivery = AttackDelivery.PiercingLine;
                break;
        }
        return new(cells, delivery, form);
    }
}
