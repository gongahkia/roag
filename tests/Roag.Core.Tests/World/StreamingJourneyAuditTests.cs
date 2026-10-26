using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class StreamingJourneyAuditTests
{
    [Theory]
    [InlineData(42)] [InlineData(1337)] [InlineData(-42)]
    public void ActualPartyJourneysCrossChunksAndEveryFloorWithExactPreparedTerrainAndReloads(int seed)
    {
        var report = StreamingJourneyAudit.Run(ContentCatalog.Default, seed);
        Assert.True(report.Passed); Assert.Equal(4, report.PartySize); Assert.Equal(6, report.StairTransfers);
        Assert.True(report.SurfaceCrossings >= 4); Assert.True(report.DeepCrossings >= 2); Assert.Equal(25, report.PeakResidentChunks);
        Assert.True(report.DemandMisses > 0); Assert.Equal(report.Steps, report.Timing.Samples);
        Assert.Contains(report.Checkpoints, p => p.Phase == "surface-walking");
        Assert.Equal("returned", report.Checkpoints[^1].Phase);
    }
}
