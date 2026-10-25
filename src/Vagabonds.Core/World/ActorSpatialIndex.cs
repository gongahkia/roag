using System.Collections.ObjectModel;

namespace Vagabonds.Core.World;

/// <summary>List semantics plus mutation notifications; JSON remains an ordinary actor array.</summary>
public sealed class ActorCollection : Collection<Actor>
{
    internal event Action? Changed;
    private bool _batch;
    protected override void InsertItem(int index, Actor item) { base.InsertItem(index, item); Notify(); }
    protected override void SetItem(int index, Actor item) { base.SetItem(index, item); Notify(); }
    protected override void RemoveItem(int index) { base.RemoveItem(index); Notify(); }
    protected override void ClearItems() { base.ClearItems(); Notify(); }
    private void Notify() { if (!_batch) Changed?.Invoke(); }
    public void AddRange(IEnumerable<Actor> actors)
    {
        var copy = actors.ToArray();
        _batch = true;
        try { foreach (var actor in copy) Add(actor); }
        finally { _batch = false; Changed?.Invoke(); }
    }
}

/// <summary>Ephemeral ordered identity, tile and region indexes. Never serialized or consulted for visibility.</summary>
internal sealed class ActorSpatialIndex : IDisposable
{
    public const int RegionSize = 16;
    public ActorCollection Source { get; }
    private readonly Dictionary<WorldPosition, List<Actor>> _tiles = [];
    private readonly Dictionary<(int X, int Y, int Floor), List<Actor>> _regions = [];
    private readonly Dictionary<long, List<Actor>> _identities = [];
    private readonly Dictionary<Actor, (int Order, long Id)> _actors = [];
    private bool _dirty;
    public ActorSpatialIndex(ActorCollection source) { Source = source; source.Changed += Invalidate; Rebuild(); }
    private void Invalidate() => _dirty = true;
    private void Ensure() { if (_dirty) Rebuild(); }
    private static int Region(int value) => (int)Math.Floor(value / (double)RegionSize);
    private static (int, int, int) Key(WorldPosition p) => (Region(p.X), Region(p.Y), p.Floor);
    private void Rebuild()
    {
        _dirty = false;
        foreach (var actor in _actors.Keys) actor.IndexChanged -= Move;
        _tiles.Clear(); _regions.Clear(); _identities.Clear(); _actors.Clear();
        for (int i = 0; i < Source.Count; i++)
        {
            var actor = Source[i];
            if (_actors.TryAdd(actor, (i, actor.Id))) actor.IndexChanged += Move;
            Add(_tiles, actor.Position, actor); Add(_regions, Key(actor.Position), actor); Add(_identities, actor.Id, actor);
        }
    }
    private static void Add<T>(Dictionary<T, List<Actor>> map, T key, Actor actor) where T : notnull
    {
        if (!map.TryGetValue(key, out var list)) map[key] = list = [];
        list.Add(actor);
    }
    private static void Remove<T>(Dictionary<T, List<Actor>> map, T key, Actor actor) where T : notnull
    {
        if (!map.TryGetValue(key, out var list)) return;
        list.Remove(actor); if (list.Count == 0) map.Remove(key);
    }
    private void Move(Actor actor, WorldPosition before)
    {
        if (_dirty) return;
        if (!_actors.TryGetValue(actor, out var entry) || actor.Id != entry.Id) { Invalidate(); return; }
        Remove(_tiles, before, actor); Remove(_regions, Key(before), actor);
        Add(_tiles, actor.Position, actor); Add(_regions, Key(actor.Position), actor);
        _tiles[actor.Position].Sort(Compare); _regions[Key(actor.Position)].Sort(Compare);
    }
    private int Compare(Actor left, Actor right) => _actors[left].Order.CompareTo(_actors[right].Order);
    public Actor Single(long id)
    {
        Ensure();
        return _identities.TryGetValue(id, out var actors) && actors.Count == 1
            ? actors[0] : throw new InvalidOperationException("Actor identity must have exactly one owner.");
    }
    public Actor? Occupant(WorldPosition cell, long except)
    {
        Ensure();
        if (_tiles.TryGetValue(cell, out var actors))
            foreach (var actor in actors) if (actor.Id != except && actor.Life != LifeState.Dead) return actor;
        return null;
    }
    public IReadOnlyList<Actor> Near(WorldPosition center, int radius)
    {
        Ensure();
        if (radius < 0 || radius > WorldPlan.Size * 2) throw new ArgumentOutOfRangeException(nameof(radius));
        var result = new List<Actor>();
        for (int y = Region(center.Y - radius); y <= Region(center.Y + radius); y++)
            for (int x = Region(center.X - radius); x <= Region(center.X + radius); x++)
                if (_regions.TryGetValue((x, y, center.Floor), out var actors))
                    foreach (var actor in actors) if (actor.Life != LifeState.Dead && actor.Position.Distance(center) <= radius) result.Add(actor);
        result.Sort(Compare); return result;
    }
    public IReadOnlyList<Actor> Near(IEnumerable<WorldPosition> centers, int radius)
    {
        var result = new HashSet<Actor>();
        foreach (var center in centers) foreach (var actor in Near(center, radius)) result.Add(actor);
        var ordered = result.ToList(); ordered.Sort(Compare); return ordered;
    }
    public void Dispose()
    {
        Source.Changed -= Invalidate;
        foreach (var actor in _actors.Keys) actor.IndexChanged -= Move;
    }
}

public sealed partial class WorldSession
{
    private ActorSpatialIndex? _actorIndex;
    private ActorSpatialIndex ActorIndex
    {
        get
        {
            if (_actorIndex == null || !ReferenceEquals(_actorIndex.Source, State.Actors))
            { _actorIndex?.Dispose(); _actorIndex = new(State.Actors); }
            return _actorIndex;
        }
    }
    public Actor ActorById(long id) => ActorIndex.Single(id);
    public IReadOnlyList<Actor> ActorsNear(WorldPosition center, int radius) => ActorIndex.Near(center, radius);
}
