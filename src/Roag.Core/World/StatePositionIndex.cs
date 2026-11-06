namespace Roag.Core.World;

/// <summary>
/// Ordered positions for immutable registry entries. Only membership is cached;
/// callers still evaluate the current entry, terrain and clock on every query.
/// Replacing an entry or its collection rebuilds this unsaved index.
/// </summary>
internal sealed class StatePositionIndex<T>(Func<T, WorldPosition> position, Func<T, WorldPosition?>? otherPosition = null)
{
    private StateList<T>? _source;
    private long _revision = -1;
    private readonly Dictionary<WorldPosition, List<T>> _cells = [];

    public List<T>? At(StateList<T> source, WorldPosition cell)
    {
        if (!ReferenceEquals(_source, source) || _revision != source.Revision)
        {
            _cells.Clear();
            foreach (var entry in source)
            {
                var first = position(entry); Add(first, entry);
                if (otherPosition?.Invoke(entry) is { } second && second != first) Add(second, entry);
            }
            _source = source; _revision = source.Revision;
        }
        return _cells.GetValueOrDefault(cell);
    }

    private void Add(WorldPosition cell, T entry)
    {
        if (!_cells.TryGetValue(cell, out var entries)) _cells[cell] = entries = [];
        entries.Add(entry);
    }
}
