using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class CrowdedWorkAuditTests
{
    [Theory]
    [InlineData(42)] [InlineData(1337)] [InlineData(-42)]
    public void MaximumPartyCompletesAllSixtyFourObstructedJobsWithExactPhaseReloads(int seed)
    {
        var report = CrowdedWorkAudit.Run(ContentCatalog.Default, seed);
        Assert.True(report.Passed); Assert.Equal(64, report.Queued); Assert.Equal(4, report.PartySize); Assert.Equal(12, report.Blockers);
        Assert.Equal(320, report.ExtractedStone); Assert.Equal(report.Ticks, report.Timing.Samples); Assert.Equal(49, report.BlockedTicks);
        Assert.Equal(new[] { "blocked", "walking", "suspended-work", "hauling", "completed" }, report.SavedPhases);
        Assert.Equal(0, report.Checkpoints[^1].Pending);
    }
}
