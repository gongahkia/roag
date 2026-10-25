namespace Vagabonds.Core.Tests;

internal static class WarmAllocationProbe
{
    public const int Operations = 10_000;
    public static long Measure(Action operation)
    {
        // Exercise cache hits as well as population before measuring. Complete
        // batches retain per-call allocations; the minimum avoids attributing
        // one-off runtime/test-host bookkeeping to the settled cache path.
        for (int i = 0; i < Operations; i++) operation();
        long minimum = long.MaxValue;
        for (int batch = 0; batch < 3; batch++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Operations; i++) operation();
            minimum = Math.Min(minimum, GC.GetAllocatedBytesForCurrentThread() - before);
        }
        return minimum;
    }
}
