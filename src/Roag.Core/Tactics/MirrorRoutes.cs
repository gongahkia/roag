using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record EnemyMirror(long Id, long Source, WorldPosition Position, WorldPosition Origin, Facing Incoming, long Expires);
public sealed record MirrorReflection(long Mirror, long Owner, WorldPosition Position, Facing Incoming, int Step, int Range);
public sealed record ReflectedPath(IReadOnlyList<WorldPosition> Cells, MirrorReflection? Reflection = null);

/// <summary>Read-only, single-use path selection. The action owner consumes the chosen mirror at commitment.</summary>
internal static class MirrorRoutes
{
    public static ReflectedPath Route(WorldPosition origin, IReadOnlyList<WorldPosition> cells, AttackDelivery delivery,
        IReadOnlyList<EnemyMirror> mirrors, long clock, Func<WorldPosition, bool> projectileOpen,
        Func<WorldPosition, bool> blockingBody, Func<long, bool> livingOwner, Func<WorldPosition, bool>? known = null)
    {
        ReflectedPath Unchanged() => new(cells.ToArray());
        if (mirrors.Count == 0 || delivery is not (AttackDelivery.Projectile or AttackDelivery.PiercingLine) || cells.Count < 2)
            return Unchanged();
        int dx = cells[0].X - origin.X, dy = cells[0].Y - origin.Y;
        if (!AttackGeometry.TryFacing(dx, dy, out var incoming) || !AttackGeometry.Cardinal(incoming)) return Unchanged();
        for (int i = 0; i < cells.Count; i++) if (cells[i] != origin.Offset(dx * (i + 1), dy * (i + 1))) return Unchanged();
        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (!projectileOpen(cell) || delivery == AttackDelivery.Projectile && blockingBody(cell) && (known == null || known(cell)))
                return Unchanged();
            var mirror = mirrors.FirstOrDefault(m => i < cells.Count - 1 && m.Position == cell && m.Expires > clock
                && m.Incoming == incoming && livingOwner(m.Source) && (known == null || known(cell)));
            if (mirror == null) continue;
            var result = cells.Take(i + 1).ToList();
            for (int step = 1; step < cells.Count - i; step++) result.Add(cell.Offset(-dy * step, dx * step));
            return new(result, new(mirror.Id, mirror.Source, cell, incoming, i + 1, cells.Count));
        }
        return Unchanged();
    }
}
