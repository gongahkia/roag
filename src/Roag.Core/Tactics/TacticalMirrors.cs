using System.Collections.Immutable;
using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

internal static class EnemyMirrors
{
    public const string Definition = "core:enemy/mirror_hexer", Cast = "core:ability/hexer_mirror";
    public const int Duration = 400, Cap = 8;

    public static bool PlacementGeometry(WorldPosition origin, IReadOnlyList<WorldPosition>? cells, Facing? incoming)
    {
        if (incoming is not { } facing || !Enum.IsDefined(facing) || !AttackGeometry.Cardinal(facing) || cells?.Count != 1) return false;
        var (dx, dy) = AttackGeometry.Offset(facing);
        return origin == cells[0].Offset(dx, dy);
    }

    public static bool ReflectionValid(WorldPlan plan, WorldPosition origin, IReadOnlyList<WorldPosition> cells,
        AttackDelivery delivery, MirrorReflection? reflection, long nextAction, long nextActor)
    {
        if (reflection is not { } r) return true;
        if (r.Mirror <= 0 || r.Mirror >= nextAction || r.Owner <= 0 || r.Owner >= nextActor
            || delivery is not (AttackDelivery.Projectile or AttackDelivery.PiercingLine)
            || !Enum.IsDefined(r.Incoming) || !AttackGeometry.Cardinal(r.Incoming)
            || r.Step < 1 || r.Step >= r.Range || r.Step > cells.Count || cells.Count > r.Range) return false;
        var (dx, dy) = AttackGeometry.Offset(r.Incoming);
        WorldPosition At(int step) => step <= r.Step ? origin.Offset(dx * step, dy * step)
            : r.Position.Offset(-dy * (step - r.Step), dx * (step - r.Step));
        if (r.Position != At(r.Step)) return false;
        for (int i = 0; i < cells.Count; i++) if (cells[i] != At(i + 1) || !plan.Contains(cells[i])) return false;
        // A shot may leave the finite world's boundary on its turned leg.
        return cells.Count == r.Range || !plan.Contains(At(cells.Count + 1));
    }

    public static void Validate(WorldPlan plan, WorldState world, IReadOnlyDictionary<long, Actor> actors, ContentCatalog? content)
    {
        var state = world.Tactics!; long clock = state.Queue.Clock;
        if (state.Mirrors == null || state.Mirrors.Count > Cap || state.Mirrors.Any(m => m == null)
            || state.Mirrors.Select(m => m.Id).Distinct().Count() != state.Mirrors.Count
            || state.Mirrors.Select(m => m.Source).Distinct().Count() != state.Mirrors.Count
            || state.Mirrors.Select(m => m.Position).Distinct().Count() != state.Mirrors.Count)
            throw new InvalidDataException("Invalid mirror store.");
        content ??= ContentCatalog.Default;
        using var terrain = state.Mirrors.Count > 0 ? new ChunkCache(plan, world, content) : null;
        foreach (var mirror in state.Mirrors)
            if (mirror.Id <= 0 || mirror.Id >= state.NextAction || mirror.Expires <= clock || mirror.Expires - clock > Duration
                || !actors.TryGetValue(mirror.Source, out var owner)
                || owner is not { Life: LifeState.Alive, Health: > 0, Tactics: { DefinitionId: Definition, Enemy.MirrorCasts: > 0 } }
                || owner.Position.Floor != mirror.Position.Floor || !plan.Contains(mirror.Origin) || !plan.Contains(mirror.Position)
                || !PlacementGeometry(mirror.Origin, [mirror.Position], mirror.Incoming)
                || !EnemySilt.Plain(terrain!.At(mirror.Position)) || !WorldSession.SnapshotProjectileOpen(plan, world, content, terrain, mirror.Position))
                throw new InvalidDataException("Invalid active mirror ownership, geometry, terrain or lifetime.");
        foreach (var attack in state.Attacks)
            if (attack.Support?.Kind == EnemySupportKind.Mirror
                && (actors.GetValueOrDefault(attack.Source)?.Tactics?.Enemy?.MirrorCasts is not > 0
                    || state.Mirrors.Any(m => m.Id == attack.Id)))
                throw new InvalidDataException("Invalid committed mirror budget.");
    }
}

public sealed partial class TacticalCombat
{
    private void ResolveMirror(Actor owner, LockedAttack attack)
    {
        var cell = attack.Cells[0];
        bool fits = owner.Position.Floor == cell.Floor && owner.Position.Distance(cell) == 1 && SiltGround(cell)
            && _world.CanEnter(cell, owner.Id) && State.Mirrors.Count < EnemyMirrors.Cap
            && !State.Mirrors.Any(m => m.Source == owner.Id || m.Position == cell);
        if (fits) State.Mirrors.Add(new(attack.Id, owner.Id, cell, attack.Origin, attack.Support!.MirrorIncoming!.Value,
            checked(Clock + EnemyMirrors.Duration)));
        Emit(fits ? "MirrorCreated" : "MirrorBlocked", owner.Id, 0, attack.Id, cell);
    }

    private void PruneMirrors()
    {
        State.Mirrors.RemoveAll(m => m.Expires <= Clock || Actor(m.Source) is not { Life: LifeState.Alive, Health: > 0 } owner
            || owner.Position.Floor != m.Position.Floor || !SiltGround(m.Position));
    }

    private ReflectedPath RouteMirrors(Actor shooter, WorldPosition origin, IReadOnlyList<WorldPosition> cells,
        AttackDelivery delivery, bool knownOnly = false, long? ignoredBody = null)
    {
        var sight = knownOnly ? _world.VisionFrom(shooter.Position) : null;
        var route = MirrorRoutes.Route(origin, cells, delivery, State.Mirrors, Clock, ProjectileOpen,
            p => _world.State.Actors.Any(a => a.Id != (ignoredBody ?? shooter.Id) && a.Life == LifeState.Alive && a.Health > 0 && a.Position == p),
            id => Actor(id) is { Life: LifeState.Alive, Health: > 0 }, sight == null ? null : sight.Contains);
        return route.Reflection == null ? route : route with { Cells = route.Cells.TakeWhile(_world.Plan.Contains).ToArray() };
    }

    private void ConsumeMirror(Actor shooter, long action, MirrorReflection? reflection, IReadOnlyList<WorldPosition> cells)
    {
        if (reflection is not { } r) return;
        int removed = State.Mirrors.RemoveAll(m => m.Id == r.Mirror && m.Source == r.Owner);
        if (removed != 1) throw new InvalidOperationException("Committed reflector disappeared before activation.");
        // Original sentry commitments emit this owner event before their generated
        // damage. Mirror Dividend therefore applies to the reflection itself.
        Emit("MirrorReflected", shooter.Id, 0, action, r.Position,
            snapshot: CaptureBoonEvent(BoonTrigger.MirrorReflected, shooter.Id, 0, action, r.Position)
                with { TargetCells = cells.ToImmutableArray() }, reflection: r);
    }
}
