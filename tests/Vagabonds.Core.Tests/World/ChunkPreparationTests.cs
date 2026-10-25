using System.Text.Json;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class ChunkPreparationTests
{
    [Theory]
    [InlineData(42, 0)] [InlineData(1337, 1)] [InlineData(-42, 2)] [InlineData(int.MinValue, 3)]
    public async Task PreparedTerrainMatchesDemandGenerationWithoutMutatingSavedState(int seed, int floor)
    {
        var plan = WorldPlan.Generate(seed); var state = new WorldState();
        using var cache = new ChunkCache(plan, state); var expected = new ChunkGenerator(plan);
        var center = plan.Sites[0].Center with { Floor = floor };
        string before = JsonSerializer.Serialize(state);
        cache.PrepareView(center, center.X - 16, center.Y - 12, center.X + 16, center.Y + 12, 1, 0);
        await cache.PreparationCompletion.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.InRange(cache.PreparationStatus.Ready, 1, ChunkCache.PreparationCapacity);
        Assert.Equal(0, cache.ResidentCount); Assert.Equal(before, JsonSerializer.Serialize(state));
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
        {
            var key = center.Chunk with { X = center.Chunk.X + x, Y = center.Chunk.Y + y };
            if (key.X is < 0 or >= 64 || key.Y is < 0 or >= 64) continue;
            var actual = cache.Get(key); var synchronous = expected.Generate(key);
            Assert.Equal(synchronous.BaseHash, actual.BaseHash); Assert.Equal(synchronous.Tiles, actual.Tiles);
            Assert.Equal(synchronous.Biomes, actual.Biomes); Assert.Equal(synchronous.Cultures, actual.Cultures);
        }
        Assert.True(cache.PreparationStatus.Hits > 0); Assert.Equal(before, JsonSerializer.Serialize(state));
        cache.Edit(center, Material.Camp);
        for (int x = 0; x < 32; x++) cache.Get(new(x, 8, floor));
        Assert.Equal(Material.Camp, cache.At(center)); Assert.InRange(cache.ResidentCount, 1, ChunkCache.Capacity);
    }

    [Fact]
    public async Task ReversalAndDisposalDiscardOldWorkWithoutBlockingOrPublishing()
    {
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        int calls = 0;
        using var queue = new ChunkPreparationQueue(key =>
        {
            Interlocked.Increment(ref calls); entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            return Empty(key);
        });
        try
        {
            queue.Request(Enumerable.Range(0, 20).Select(x => new ChunkKey(x, 1)));
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(ChunkCache.PreparationCapacity, queue.Status.Requested); Assert.Equal(1, queue.Status.InFlight);
            // A demand miss never waits behind speculative work.
            Assert.Null(queue.TakeReady(new(0, 1)));
            queue.Request(Enumerable.Range(30, 20).Select(x => new ChunkKey(x, 1)));
            Assert.Equal(ChunkCache.PreparationCapacity, queue.Status.Requested);
            var completion = queue.Completion; queue.Dispose();
            Assert.Equal(0, queue.Status.Requested); release.Set();
            await completion.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(1, calls); Assert.Equal(0, queue.Status.Ready); Assert.Equal(0, queue.Status.InFlight);
            Assert.True(queue.Status.Discarded > 0);
            Assert.Throws<ObjectDisposedException>(() => queue.Request([]));
        }
        finally { release.Set(); }
    }

    [Fact]
    public async Task FailedSpeculationIsObservedAndDemandCanFallBack()
    {
        using var queue = new ChunkPreparationQueue(_ => throw new InvalidDataException("fixture failure"));
        queue.Request([new(4, 5)]); await queue.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, queue.Status.Failed); Assert.Equal(0, queue.Status.Ready);
        Assert.Null(queue.TakeReady(new(4, 5)));
    }

    [Fact]
    public async Task BackgroundWorkNeverRevealsStairs()
    {
        var plan = WorldPlan.Generate(42); var state = new WorldState(); using var cache = new ChunkCache(plan, state);
        var center = plan.Sites[0].Center.Offset(2, 0);
        cache.PrepareView(center, center.X - 8, center.Y - 8, center.X + 8, center.Y + 8);
        await cache.PreparationCompletion.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.False(cache.Seen(center)); Assert.Empty(state.Chunks);
        var downstairs = center with { Floor = 1 };
        var actual = cache.Get(downstairs.Chunk);
        Assert.Equal(new ChunkGenerator(plan).Generate(downstairs.Chunk).BaseHash, actual.BaseHash);
        Assert.Equal(1, cache.PreparationStatus.Hits); Assert.Empty(state.Chunks);
        cache.Dispose(); Assert.Throws<ObjectDisposedException>(() => cache.Get(center.Chunk));
    }

    [Fact]
    public async Task SpeculativeRequestsAndUnfinishedWorkPreserveExactSaveContinuation()
    {
        using var world = WorldCollisionTests.Arena(); var center = world.State.Position.Offset(96, 0);
        string before = JsonSerializer.Serialize(world.State);
        world.Chunks.PrepareView(center, center.X - 20, center.Y - 12, center.X + 20, center.Y + 12, 1);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-preparation-" + Guid.NewGuid().ToString("N"));
        try
        {
            var file = Path.Combine(directory, "world.json"); world.Save(file);
            using var loaded = WorldSession.Load(file);
            world.Paused = loaded.Paused = false;
            for (int i = 0; i < 20; i++)
            {
                world.AdvanceTicks(1); loaded.AdvanceTicks(1);
                Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            }
            await world.Chunks.PreparationCompletion.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static GeneratedChunk Empty(ChunkKey key) => new(key, [], [], [], "fixture");

    [Fact]
    public void ReproducibleJourneyMeasuresReadyHitsAndChecksDemandOrderAndState()
    {
        var report = JsonSerializer.SerializeToElement(StreamingBenchmarks.Run(Vagabonds.Core.Content.ContentCatalog.Default, 42, 3));
        Assert.True(report.GetProperty("generationMatches").GetBoolean());
        Assert.True(report.GetProperty("demandOrderMatches").GetBoolean());
        Assert.True(report.GetProperty("savedStateMatches").GetBoolean());
        Assert.Equal(3, report.GetProperty("preparation").GetProperty("Hits").GetInt64());
    }
}
