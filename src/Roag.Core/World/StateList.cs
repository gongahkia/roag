using System.Collections.ObjectModel;

namespace Roag.Core.World;

/// <summary>Ordinary JSON array with notifications for simulation deadlines.</summary>
public sealed class StateList<T> : Collection<T>
{
    internal event Action? Changed;
    protected override void InsertItem(int index, T item) { base.InsertItem(index, item); Changed?.Invoke(); }
    protected override void SetItem(int index, T item) { base.SetItem(index, item); Changed?.Invoke(); }
    protected override void RemoveItem(int index) { base.RemoveItem(index); Changed?.Invoke(); }
    protected override void ClearItems() { base.ClearItems(); Changed?.Invoke(); }
    public int FindIndex(Predicate<T> match)
    {
        for (int i = 0; i < Count; i++) if (match(this[i])) return i;
        return -1;
    }
    public int RemoveAll(Predicate<T> match)
    {
        int removed = 0;
        for (int i = Count - 1; i >= 0; i--) if (match(this[i])) { RemoveAt(i); removed++; }
        return removed;
    }
}
