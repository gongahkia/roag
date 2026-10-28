using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record WebSpan(WorldPosition A, WorldPosition B)
{
    [JsonIgnore] public bool Valid => A.Floor == B.Floor && (A.X == B.X || A.Y == B.Y)
        && Math.Abs((long)A.X - B.X) + Math.Abs((long)A.Y - B.Y) == 4;

    public WorldPosition[] Cells()
    {
        if (!Valid) throw new InvalidDataException("Invalid web anchors.");
        int dx = Math.Sign((long)B.X - A.X), dy = Math.Sign((long)B.Y - A.Y);
        return Enumerable.Range(0, 5).Select(i => A.Offset(dx * i, dy * i)).ToArray();
    }

    public bool Contains(WorldPosition p, bool anchors = true) => p.Floor == A.Floor
        && (A.X == B.X && p.X == A.X && p.Y > Math.Min(A.Y, B.Y) && p.Y < Math.Max(A.Y, B.Y)
            || A.Y == B.Y && p.Y == A.Y && p.X > Math.Min(A.X, B.X) && p.X < Math.Max(A.X, B.X)
            || anchors && (p == A || p == B));
}

public sealed record EnemyWeb(long Id, long Source, WebSpan Span, long Expires);

internal static class EnemyWebRules
{
    public const int Duration = 400;
    public const int Delay = 50;
    public const int Cap = 8;

    public static bool Fits(WebSpan span, WorldPosition source, Func<WorldPosition, bool> ground,
        Func<WorldPosition, bool> free, Func<WorldPosition, bool> reserved, IReadOnlyList<EnemyWeb> webs)
        => span.Valid && span.A.Floor == source.Floor && span.A != source && span.B != source
            && span.Cells().All(p => ground(p) && !reserved(p) && !webs.Any(w => w.Span.Contains(p)))
            && free(span.A) && free(span.B);

    public static void Validate(WorldPlan plan, WorldState world, IReadOnlyDictionary<long, Actor> actors, ContentCatalog? content)
    {
        var state = world.Tactics!; long clock = state.Queue.Clock;
        if (state.Webs == null || state.Webs.Count > Cap || state.Webs.Any(w => w == null)
            || state.Webs.Select(w => w.Id).Distinct().Count() != state.Webs.Count)
            throw new InvalidDataException("Invalid web store.");
        using var terrain = state.Webs.Count > 0 ? new ChunkCache(plan, world, content) : null;
        var counts = new Dictionary<long, int>(); var occupied = new HashSet<WorldPosition>();
        foreach (var web in state.Webs)
        {
            if (web.Id <= 0 || web.Id >= state.NextAction || web.Span is not { Valid: true }
                || !actors.TryGetValue(web.Source, out var source)
                || source is not { Life: LifeState.Alive, Health: > 0, Tactics: { DefinitionId: "core:enemy/web_weaver", Enemy: { } memory } }
                || source.Position.Floor != web.Span.A.Floor || web.Expires <= clock || web.Expires - clock > Duration
                || counts.GetValueOrDefault(source.Id) >= memory.WebCasts)
                throw new InvalidDataException("Invalid active web identity, budget or lifetime.");
            counts[source.Id] = counts.GetValueOrDefault(source.Id) + 1;
            foreach (var cell in web.Span.Cells())
                if (!plan.Contains(cell) || !occupied.Add(cell) || !Materials.Walkable(terrain!.At(cell))
                    || terrain.At(cell) is Material.StairsDown or Material.StairsUp or Material.Camp
                    || actors.Values.Any(a => a.Tactics?.Resources.Bulwark?.Position == cell))
                    throw new InvalidDataException("Invalid saved web terrain or overlap.");
        }
        foreach (var attack in state.Attacks)
            if (attack.Support?.Kind == EnemySupportKind.Web
                && (!actors.TryGetValue(attack.Source, out var source)
                    || source.Tactics is not { DefinitionId: "core:enemy/web_weaver", Enemy.WebCasts: > 0 }
                    || counts.GetValueOrDefault(source.Id) >= source.Tactics.Enemy.WebCasts
                    || state.Webs.Any(w => w.Id == attack.Id)))
                throw new InvalidDataException("Invalid committed web budget.");
    }
}

public sealed partial class TacticalCombat
{
    private void ResolveWeb(Actor owner, LockedAttack attack)
    {
        var span = attack.Support!.Web!;
        bool fits = State.Webs.Count < EnemyWebRules.Cap && EnemyWebRules.Fits(span, owner.Position,
            _world.GroundOpen, p => _world.CanEnter(p, owner.Id), ProtectedForcedLanding, State.Webs);
        if (fits) State.Webs.Add(new(attack.Id, owner.Id, span, checked(Clock + EnemyWebRules.Duration)));
        Emit(fits ? "WebCreated" : "WebBlocked", owner.Id, 0, attack.Id, span.Cells()[2]);
    }

    private void PruneWebs()
    {
        if (State.Webs.Count == 0) return;
        State.Webs.RemoveAll(w => w.Expires <= Clock || Actor(w.Source) is not { Life: LifeState.Alive, Health: > 0 } owner
            || owner.Position.Floor != w.Span.A.Floor || w.Span.Cells().Any(p => !_world.GroundOpen(p) || ProtectedForcedLanding(p)));
    }

    /// <summary>Capture reach before primary deaths; queued listeners cannot see through a just-killed blocker.</summary>
    private ImmutableArray<WorldPosition> CaptureWebCuts(long source, IReadOnlyList<WorldPosition> cells, AttackDelivery delivery, int damage)
    {
        if (damage <= 0 || State.Webs.Count == 0) return [];
        var result = ImmutableArray.CreateBuilder<WorldPosition>();
        foreach (var cell in cells)
        {
            bool pass = delivery == AttackDelivery.Melee || ProjectileOpen(cell);
            if (!pass && delivery != AttackDelivery.Area) break;
            if (pass) result.Add(cell);
            if (delivery == AttackDelivery.Projectile && _world.OccupantAt(cell, source) != null) break;
        }
        return result.ToImmutable();
    }

    private void ResolveWebEvent(BoonEvent e, ImmutableArray<WorldPosition> cuts)
    {
        if (State.Webs.Count == 0) return;
        if (e.SourceId is not { } source || source == 0) return;
        for (int i = State.Webs.Count - 1; i >= 0; i--)
        {
            var web = State.Webs[i];
            if (web.Expires <= Clock) continue;
            if (e.Kind == BoonTrigger.TileEntered && source != web.Source && e.Position is { } cell
                && web.Span.Contains(cell, anchors: false) && Actor(source) is { Life: LifeState.Alive, Health: > 0, Tactics: { } tactics } victim)
            {
                State.Webs.RemoveAt(i);
                var ready = State.Queue.Snapshot().Pending.FirstOrDefault(w => w.ActorId == victim.Id && w.Kind == ScheduledKind.ActorReady);
                if (ready != null) State.Queue.Delay(ready.Sequence, EnemyWebRules.Delay);
                else tactics.WebRecoveryDelay = checked(tactics.WebRecoveryDelay + EnemyWebRules.Delay);
                Emit("WebCrossed", web.Source, source, e.RootActionId ?? 0, cell, EnemyWebRules.Delay);
            }
            else if (e.Kind == BoonTrigger.AttackPerformed && !cuts.IsDefaultOrEmpty)
                foreach (var cut in cuts)
                    if (cut == web.Span.A || cut == web.Span.B)
                    {
                        State.Webs.RemoveAt(i);
                        Emit("WebAnchorCut", source, 0, e.RootActionId ?? 0, cut);
                        break;
                    }
        }
    }
}
