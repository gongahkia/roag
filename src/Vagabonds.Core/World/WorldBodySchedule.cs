namespace Vagabonds.Core.World;

public sealed record BodySimulationStatus(long Processed, long Scheduled, int BoundActors, int ActiveActors, int QueuedDeadlines);

public sealed partial class WorldSession
{
    private sealed class BodySlot(Actor actor, BodyState body, int order)
    {
        public Actor Actor { get; } = actor;
        public BodyState Body { get; set; } = body;
        public int Order { get; set; } = order;
        public long Due { get; set; }
        public bool Queued { get; set; }
    }
    private readonly record struct BodyDeadline(long Tick, int Order, Actor Actor);
    private readonly Dictionary<Actor, BodySlot> _bodySlots = [];
    private readonly SortedSet<BodyDeadline> _bodyDeadlines = new(Comparer<BodyDeadline>.Create(static (a, b) =>
    { int tick = a.Tick.CompareTo(b.Tick); return tick != 0 ? tick : a.Order.CompareTo(b.Order); }));
    private readonly HashSet<Actor> _bodyDirty = [], _bodyActive = [], _bodyNearLast = [], _bodyNear = [], _bodyCandidates = [];
    private readonly HashSet<long> _bodyWorkers = [];
    private readonly List<BodySlot> _bodyPass = [];
    private ActorCollection? _bodySource;
    private bool _bodyRosterDirty = true;
    private Func<long>? _bodyClock;
    private long _bodyClockTick, _bodyProcessed, _bodyScheduled;
    // Reference mode for deterministic audit comparisons; never saved or enabled by gameplay.
    internal bool DetailedBodiesEverywhere { get; set; }
    public BodySimulationStatus BodySimulation => new(_bodyProcessed, _bodyScheduled, _bodySlots.Count, _bodyActive.Count, _bodyDeadlines.Count);
    private long ReadBodyClock() => _bodyClockTick;
    private void BodyRosterChanged() => _bodyRosterDirty = true;
    private void BodyChanged(Actor actor) => _bodyDirty.Add(actor);

    private void PrepareBodyClock()
    {
        if (_bodyClock == null) { _bodyClockTick = State.Tick; _bodyClock = ReadBodyClock; }
        // Explicit fixture/time changes retain the original no-offline-catch-up
        // semantics. Ordinary advancement always moves the two clocks together.
        if (_bodyClockTick != State.Tick)
        {
            foreach (var slot in _bodySlots.Values) slot.Body.Reanchor(_bodyClockTick, State.Tick);
            _bodyClockTick = State.Tick; _bodyRosterDirty = true;
        }
        if (!ReferenceEquals(_bodySource, State.Actors))
        {
            if (_bodySource != null) _bodySource.Changed -= BodyRosterChanged;
            _bodySource = State.Actors; _bodySource.Changed += BodyRosterChanged; _bodyRosterDirty = true;
        }
        if (!_bodyRosterDirty) return;
        var current = State.Actors.ToHashSet();
        foreach (var actor in _bodySlots.Keys.Where(a => !current.Contains(a)).ToArray())
        {
            var slot = _bodySlots[actor]; CancelBodyDeadline(slot); slot.Body.Unbind(); actor.NeedsChanged -= BodyChanged;
            _bodySlots.Remove(actor); _bodyDirty.Remove(actor); _bodyActive.Remove(actor); _bodyNearLast.Remove(actor);
        }
        for (int i = 0; i < State.Actors.Count; i++)
        {
            var actor = State.Actors[i];
            if (!_bodySlots.TryGetValue(actor, out var slot))
            {
                slot = new(actor, actor.Body, i); _bodySlots.Add(actor, slot);
                slot.Body.Bind(_bodyClock, _bodyClockTick); actor.NeedsChanged += BodyChanged;
            }
            else { CancelBodyDeadline(slot); slot.Order = i; RebindBody(slot); }
            QueueBody(slot, State.Tick + 1);
        }
        _bodyRosterDirty = false;
    }
    private void RebindBody(BodySlot slot)
    {
        if (ReferenceEquals(slot.Body, slot.Actor.Body)) return;
        slot.Body.Unbind(); slot.Body = slot.Actor.Body; slot.Body.Bind(_bodyClock!, _bodyClockTick);
    }
    private void CancelBodyDeadline(BodySlot slot)
    {
        if (!slot.Queued) return;
        _bodyDeadlines.Remove(new(slot.Due, slot.Order, slot.Actor)); slot.Queued = false;
    }
    private void QueueBody(BodySlot slot, long tick)
    {
        CancelBodyDeadline(slot); slot.Due = tick; slot.Queued = true;
        _bodyDeadlines.Add(new(tick, slot.Order, slot.Actor));
    }
    private void GatherBodies()
    {
        _bodyWorkers.Clear();
        foreach (var work in State.Work) if (work.Worker > 0 && !work.Suspended) _bodyWorkers.Add(work.Worker);
        _bodyNear.Clear();
        ActorIndex.AddNearTo(State.Position, 100, _bodyNear);
        _bodyCandidates.Clear();
        foreach (var actor in _bodyNear) _bodyCandidates.Add(actor);
        foreach (var actor in _bodyNearLast) _bodyCandidates.Add(actor);
        foreach (var actor in _bodyActive) _bodyCandidates.Add(actor);
        foreach (var actor in _bodyDirty) _bodyCandidates.Add(actor);
        foreach (var id in State.Party) _bodyCandidates.Add(ActorById(id));
        foreach (var id in _bodyWorkers) _bodyCandidates.Add(ActorById(id));
        if (DetailedBodiesEverywhere) _bodyCandidates.UnionWith(State.Actors);
        while (_bodyDeadlines.Count > 0 && _bodyDeadlines.Min.Tick <= State.Tick)
        {
            var first = _bodyDeadlines.Min; var slot = _bodySlots[first.Actor];
            _bodyDeadlines.Remove(first); slot.Queued = false; _bodyCandidates.Add(first.Actor); _bodyScheduled++;
        }
        _bodyPass.Clear();
        foreach (var actor in _bodyCandidates) if (_bodySlots.TryGetValue(actor, out var slot)) _bodyPass.Add(slot);
        _bodyPass.Sort(CompareBodySlots);
        // Configure every participant before exposing the current body clock.
        // Contact/battle setup above this pass still observes the previous tick.
        foreach (var slot in _bodyPass)
        {
            RebindBody(slot); ConfigureBodyRates(slot.Actor, _bodyClockTick, State.Tick);
        }
        _bodyClockTick = State.Tick;
    }
    private static int CompareBodySlots(BodySlot a, BodySlot b) => a.Order.CompareTo(b.Order);
    private bool BodyFighting(Actor actor) => State.Battle?.ActorIds.Contains(actor.Id) == true;
    private void ConfigureBodyRates(Actor actor, long at, long next)
    {
        var body = actor.Body;
        if (actor.Life != LifeState.Alive) { body.RatesAt(at, 0, 0); return; }
        bool fighting = BodyFighting(actor);
        bool automaticRest = !actor.Recruited && !fighting && body.FatigueAt(at) >= 75 && body.RestUntil <= next;
        bool resting = !fighting && (body.RestUntil > next || automaticRest);
        double fatigue = Simulation.FatiguePerTick * (Disposition(actor)?.FatiguePercent ?? 100) / 100.0 / Math.Min(2, 1 + (actor.Level - 1) * .02);
        body.RatesAt(at, Simulation.HungerPerTick, resting ? -Simulation.RestPerTick : fatigue * (_bodyWorkers.Contains(actor.Id) || body.ForageProgress > 0 ? 2 : 1));
    }
    private void ScheduleBody(BodySlot slot)
    {
        var actor = slot.Actor; var body = actor.Body;
        CancelBodyDeadline(slot); _bodyDirty.Remove(actor);
        if (actor.Life != LifeState.Alive) { _bodyActive.Remove(actor); return; }
        ConfigureBodyRates(actor, State.Tick, State.Tick + 1);
        if (actor.Recruited || BodyFighting(actor) || _bodyWorkers.Contains(actor.Id) || body.Hunger >= Simulation.MealAt)
        { _bodyActive.Add(actor); return; }
        _bodyActive.Remove(actor);
        if (_bodyNear.Contains(actor) || DetailedBodiesEverywhere) return;
        long due = State.Tick + (50 - State.Tick % 50);
        if (body.RestUntil > State.Tick) due = Math.Min(due, body.RestUntil);
        if (body.Fatigue >= 75 && body.RestUntil <= State.Tick + 1) due = State.Tick + 1;
        for (int i = 0; i < actor.Memories.Count; i++) if (actor.Memories[i].Until > State.Tick) due = Math.Min(due, actor.Memories[i].Until);
        for (int i = 0; i < actor.Preparations.Count; i++) if (actor.Preparations[i].Until > State.Tick) due = Math.Min(due, actor.Preparations[i].Until);
        due = State.Tick + body.ThresholdIn((int)(due - State.Tick), Simulation.MealAt);
        QueueBody(slot, due);
    }
    private void FinishBodyPass()
    {
        _bodyNearLast.Clear(); foreach (var actor in _bodyNear) _bodyNearLast.Add(actor);
    }
    private void DisposeBodySchedule()
    {
        if (_bodySource != null) _bodySource.Changed -= BodyRosterChanged;
        foreach (var slot in _bodySlots.Values) { slot.Actor.NeedsChanged -= BodyChanged; slot.Body.Unbind(); }
        _bodySlots.Clear(); _bodyDeadlines.Clear(); _bodyDirty.Clear(); _bodyActive.Clear();
    }
}
