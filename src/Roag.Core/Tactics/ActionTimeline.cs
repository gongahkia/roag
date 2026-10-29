namespace Roag.Core.Tactics;

public enum ScheduledKind { ActorReady, Impact, Effect, StagePressure, StageDeadline, EmberPulse }

public sealed record ScheduledAction(long Sequence, long Due, ScheduledKind Kind, long ActorId,
    bool PlayerControlled, long ReferenceId);

public sealed record TimelineSnapshot(long Clock, long NextSequence, ScheduledAction[] Pending);

/// <summary>
/// One ordered simulation timeline. Only committed simulation work calls TakeNext;
/// inspection and rendering use Peek/Snapshot. No floor-local parking or rebasing.
/// </summary>
public sealed class ActionTimeline
{
    private sealed class Ordering : IComparer<ScheduledAction>
    {
        public int Compare(ScheduledAction? x, ScheduledAction? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            int order = x.Due.CompareTo(y.Due);
            if (order != 0) return order;
            bool actorX = x.Kind == ScheduledKind.ActorReady, actorY = y.Kind == ScheduledKind.ActorReady;
            order = actorX.CompareTo(actorY); // impacts/effects/deadlines before readiness
            if (order != 0) return order;
            if (actorX)
            {
                order = y.PlayerControlled.CompareTo(x.PlayerControlled);
                if (order != 0) return order;
            }
            return x.Sequence.CompareTo(y.Sequence);
        }
    }

    private readonly SortedSet<ScheduledAction> _pending = new(new Ordering());
    private readonly Dictionary<long, ScheduledAction> _bySequence = [];
    public long Clock { get; private set; }
    public long NextSequence { get; private set; } = 1;
    public int Count => _pending.Count;
    public ScheduledAction? Peek => _pending.Min;

    public ActionTimeline(long clock = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clock);
        Clock = clock;
    }

    public static ActionTimeline Restore(TimelineSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Clock < 0 || snapshot.NextSequence < 1 || snapshot.NextSequence == long.MaxValue || snapshot.Pending is null)
            throw new InvalidDataException("Invalid tactical timeline header.");
        var timeline = new ActionTimeline(snapshot.Clock) { NextSequence = snapshot.NextSequence };
        foreach (var entry in snapshot.Pending)
        {
            if (entry is null || entry.Sequence < 1 || entry.Sequence >= snapshot.NextSequence
                || entry.Due < snapshot.Clock || !Enum.IsDefined(entry.Kind) || entry.ActorId < 0
                || entry.ReferenceId < 0 || entry.Kind == ScheduledKind.ActorReady && entry.ActorId == 0
                || entry.Kind != ScheduledKind.ActorReady && entry.PlayerControlled
                || !timeline._bySequence.TryAdd(entry.Sequence, entry))
                throw new InvalidDataException("Invalid tactical timeline entry.");
            timeline._pending.Add(entry);
        }
        return timeline;
    }

    public ScheduledAction Schedule(ScheduledKind kind, long due, long actorId = 0,
        bool playerControlled = false, long referenceId = 0)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (due < Clock) throw new ArgumentOutOfRangeException(nameof(due), "Cannot schedule in the past.");
        if (actorId < 0 || kind == ScheduledKind.ActorReady && actorId == 0) throw new ArgumentOutOfRangeException(nameof(actorId));
        if (referenceId < 0) throw new ArgumentOutOfRangeException(nameof(referenceId));
        if (kind != ScheduledKind.ActorReady && playerControlled) throw new ArgumentException("Only readiness has player priority.", nameof(playerControlled));
        if (NextSequence >= long.MaxValue - 1) throw new InvalidOperationException("Timeline sequence exhausted.");
        var entry = new ScheduledAction(NextSequence++, due, kind, actorId, playerControlled, referenceId);
        _pending.Add(entry);
        _bySequence.Add(entry.Sequence, entry);
        return entry;
    }

    public ScheduledAction? TakeNext()
    {
        if (_pending.Min is not { } next) return null;
        _pending.Remove(next);
        _bySequence.Remove(next.Sequence);
        Clock = next.Due;
        return next;
    }

    public bool Cancel(long sequence)
    {
        if (!_bySequence.Remove(sequence, out var entry)) return false;
        _pending.Remove(entry);
        return true;
    }

    /// <summary>Extend committed work while preserving its original tie-breaking sequence.</summary>
    public bool Delay(long sequence, long delay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delay);
        if (!_bySequence.TryGetValue(sequence, out var entry)) return false;
        var delayed = entry with { Due = checked(entry.Due + delay) };
        _pending.Remove(entry); _pending.Add(delayed); _bySequence[sequence] = delayed;
        return true;
    }

    public int CancelActor(long actorId, ScheduledKind? kind = null)
    {
        var removed = _pending.Where(e => e.ActorId == actorId && (kind is null || e.Kind == kind)).ToArray();
        foreach (var entry in removed) Cancel(entry.Sequence);
        return removed.Length;
    }

    public TimelineSnapshot Snapshot() => new(Clock, NextSequence, _pending.ToArray());
}
