using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record EnemyEmber(long Action, long Source, WorldPosition Position, int Remaining,
    long NextDue, long Expires, long Schedule);

internal static class EnemyEmbers
{
    public const string Definition = "core:enemy/ember_moth", Dive = "core:ability/moth_dive", Pulse = "core:ability/moth_ember";
    public const int Interval = 100, Ticks = 2, Cap = 8;

    public static bool ValidAttack(ActorTactics? tactics, string ability, WorldPosition origin,
        IReadOnlyList<WorldPosition>? cells, AttackDelivery delivery, WorldPosition? landing,
        EnemySupport? support, EnemyAdvance? advance, EnemyShove? shove, Facing? sentinel, bool committing)
    {
        if (ability != Dive && landing == null) return true;
        return tactics is { DefinitionId: Definition, Enemy: { } memory }
            && (committing ? memory.EmberDives < 2 : memory.EmberDives > 0)
            && ability == Dive && delivery == AttackDelivery.Area && landing is { } cell
            && cell.Floor == origin.Floor && cell.Distance(origin) is >= 2 and <= 3
            && cells?.Count == 1 && cells[0] == cell
            && support == null && advance == null && shove == null && sentinel == null;
    }

    public static void Validate(WorldPlan plan, WorldState world, IReadOnlyDictionary<long, Actor> actors, ContentCatalog? content)
    {
        var state = world.Tactics!; var queue = state.Queue.Snapshot();
        if (state.Embers == null || state.Embers.Count > Cap || state.Embers.Any(e => e == null)
            || state.Embers.Select(e => e.Action).Distinct().Count() != state.Embers.Count
            || state.Embers.Select(e => e.Position).Distinct().Count() != state.Embers.Count
            || state.Embers.Select(e => e.Schedule).Distinct().Count() != state.Embers.Count)
            throw new InvalidDataException("Invalid ember store.");
        content ??= ContentCatalog.Default;
        using var terrain = state.Embers.Count > 0 ? new ChunkCache(plan, world, content) : null;
        var counts = new Dictionary<long, int>();
        foreach (var ember in state.Embers)
        {
            if (ember.Action <= 0 || ember.Action >= state.NextAction || !actors.TryGetValue(ember.Source, out var owner)
                || owner.Tactics is not { DefinitionId: Definition, Enemy: { } memory }
                || ember.Remaining is < 1 or > Ticks || ember.NextDue < queue.Clock || ember.NextDue - queue.Clock > Interval
                || ember.Expires - ember.NextDue != (ember.Remaining - 1) * Interval
                || !plan.Contains(ember.Position) || !EnemySilt.Plain(terrain!.At(ember.Position))
                || !WorldSession.SnapshotProjectileOpen(plan, world, content, terrain, ember.Position)
                || counts.GetValueOrDefault(ember.Source) >= memory.EmberDives
                || queue.Pending.Count(w => Matches(w, ember)) != 1)
                throw new InvalidDataException("Invalid owned ember terrain, budget or scheduled lifetime.");
            counts[ember.Source] = counts.GetValueOrDefault(ember.Source) + 1;
        }
    }

    public static bool Matches(ScheduledAction work, EnemyEmber ember) => work.Kind == ScheduledKind.EmberPulse
        && work.ActorId == ember.Source && work.ReferenceId == ember.Action && work.Sequence == ember.Schedule && work.Due == ember.NextDue;
}

public sealed partial class TacticalCombat
{
    private EnemyEmber ScheduleEmber(EnemyEmber ember)
    {
        var work = State.Queue.Schedule(ScheduledKind.EmberPulse, ember.NextDue, ember.Source, referenceId: ember.Action);
        return ember with { Schedule = work.Sequence };
    }

    private void ResolveMothLanding(Actor owner, LockedAttack attack)
    {
        var landing = attack.EmberLanding!.Value;
        if (!SiltGround(landing)) return;
        if (_world.OccupantAt(landing, owner.Id) == null)
        {
            var from = owner.Position; _world.MoveTacticalActor(owner, landing);
            Emit("Displaced", owner.Id, owner.Id, attack.Id, landing, forced: true, from: from);
            Emit("TileEntered", owner.Id, owner.Id, attack.Id, landing, forced: true, from: from);
        }
        else Emit("MothLandingBlocked", owner.Id, 0, attack.Id, landing);
        if (State.Embers.Count >= EnemyEmbers.Cap || State.Embers.Any(e => e.Position == landing)) return;
        State.Embers.Add(ScheduleEmber(new(attack.Id, owner.Id, landing, EnemyEmbers.Ticks,
            checked(Clock + EnemyEmbers.Interval), checked(Clock + EnemyEmbers.Interval * EnemyEmbers.Ticks), 0)));
        Emit("EmberCreated", owner.Id, 0, attack.Id, landing);
    }

    private void PruneEmbers()
    {
        for (int i = State.Embers.Count - 1; i >= 0; i--)
        {
            var ember = State.Embers[i];
            if (ember.Expires >= Clock && SiltGround(ember.Position)) continue;
            State.Queue.Cancel(ember.Schedule); State.Embers.RemoveAt(i);
        }
    }

    private void ResolveEmberPulse(ScheduledAction work)
    {
        var ember = State.Embers.SingleOrDefault(e => EnemyEmbers.Matches(work, e));
        if (ember == null) return; // Terrain invalidation may just have removed this popped pulse.
        ResolveAtomically(RootCause(ember.Action), () =>
        {
            var target = _world.OccupantAt(ember.Position);
            Emit("EmberPulsed", ember.Source, target?.Id ?? 0, ember.Action, ember.Position, ember.Remaining - 1);
            if (target is { Life: not LifeState.Dead, Tactics: not null } && target.Id != ember.Source)
                ResolveDamageBatch(new(ember.Action, ember.Source, ember.Position, [ember.Position], EnemyEmbers.Pulse,
                    AttackDelivery.Melee, DamageElement.Fire, 1, Clock, DamageCause.GroundHazard,
                    PersistsWithoutOwner: true));
            State.Embers.Remove(ember);
            if (ember.Remaining > 1) State.Embers.Add(ScheduleEmber(ember with
                { Remaining = ember.Remaining - 1, NextDue = checked(Clock + EnemyEmbers.Interval) }));
        });
    }

    private IEnumerable<LockedAttack> EmberWarnings() => State.Embers.Select(e => new LockedAttack(e.Action, e.Source,
        e.Position, [e.Position], EnemyEmbers.Pulse, AttackDelivery.Area, DamageElement.Fire, 1, e.NextDue,
        DamageCause.GroundHazard, PersistsWithoutOwner: true));
}
