namespace Ithica.Core.Colony;

/// <summary>Reusable BFS workspace. No per-visited-cell objects or front-removal from routes.</summary>
public sealed class GridRouter(World world)
{
    private readonly int[] _visited = new int[world.Count], _targets = new int[world.Count], _parent = new int[world.Count], _queue = new int[world.Count];
    private int _stamp;
    public long Searches
    {
        get; private set;
    }
    public long VisitedCells
    {
        get; private set;
    }
    public bool Find(int start, ReadOnlySpan<int> targets, List<int> route)
    {
        route.Clear();
        if (targets.Length == 0)
        {
            return false;
        }

        Searches++;
        if (++_stamp == int.MaxValue)
        {
            Array.Clear(_visited);
            Array.Clear(_targets);
            _stamp = 1;
        }
        foreach (int i in targets)
        {
            if (world.Valid(i))
            {
                _targets[i] = _stamp;
            }
        }

        if (_targets[start] == _stamp)
        {
            return true;
        }

        int head = 0, tail = 0, found = -1;
        _queue[tail++] = start;
        _visited[start] = _stamp;
        Span<int> neighbors = stackalloc int[4];
        while (head < tail && found < 0)
        {
            int cell = _queue[head++], count = world.Neighbors(cell, neighbors);
            VisitedCells++;
            for (int n = 0; n < count; n++)
            {
                int next = neighbors[n];
                if (_visited[next] == _stamp || !world.Walkable(next) || world.Fire[next] > .5f)
                {
                    continue;
                }

                _visited[next] = _stamp;
                _parent[next] = cell;
                if (_targets[next] == _stamp)
                {
                    found = next;
                    break;
                }
                _queue[tail++] = next;
            }
        }
        if (found < 0)
        {
            return false;
        }

        while (found != start)
        {
            route.Add(found);
            found = _parent[found];
        }
        route.Reverse();
        return true;
    }
}
