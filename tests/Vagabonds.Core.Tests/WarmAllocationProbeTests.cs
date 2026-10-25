using Xunit;

namespace Vagabonds.Core.Tests;

public sealed class WarmAllocationProbeTests
{
    [Fact]
    public void CompleteBatchesStillExposeAnAllocationOnEveryCall()
    {
        long empty = WarmAllocationProbe.Measure(static () => { });
        long allocating = WarmAllocationProbe.Measure(static () => GC.KeepAlive(new object()));
        Assert.InRange(empty, 0, 1024); Assert.True(allocating >= WarmAllocationProbe.Operations * 16L);
    }
}
