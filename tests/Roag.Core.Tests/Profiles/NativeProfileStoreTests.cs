using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Profiles;
using Roag.Core.Tactics;
using Xunit;

namespace Roag.Core.Tests.Profiles;

public sealed class NativeProfileStoreTests
{
    private const string Guardian = AchievementCatalog.Prefix + "first_guardian";
    private sealed class MemoryStorage : IProfileStorage
    {
        public readonly Dictionary<string, byte[]> Files = [];
        public int Writes;
        public int FailAt = int.MaxValue;
        public bool Truncate;
        private sealed class Held : IDisposable { public void Dispose() { } }
        public IDisposable Lock() => new Held();
        public byte[]? Read(string name) => Files.GetValueOrDefault(name);
        public void Write(string name, byte[] bytes)
        {
            if (++Writes == FailAt) throw new IOException("Injected storage failure.");
            Files[name] = Truncate ? bytes[..(bytes.Length / 2)] : bytes;
        }
    }

    [Fact]
    public void CatalogPreservesEveryPinnedOriginalIdentityNameAndReward()
    {
        using var original = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-achievements.json")));
        Assert.Equal("d498fe0889050d2a23f2ff86d80db6084ed6467a", original.RootElement.GetProperty("sourceRevision").GetString());
        var expected = original.RootElement.GetProperty("definitions").EnumerateArray().ToArray();
        Assert.Equal(22, expected.Length); Assert.Equal(expected.Length, AchievementCatalog.All.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].GetProperty("id").GetString(), AchievementCatalog.All[i].Id);
            Assert.Equal(expected[i].GetProperty("name").GetString(), AchievementCatalog.All[i].Name);
            Assert.Equal(expected[i].TryGetProperty("reward", out var reward) ? reward.GetString() : null, AchievementCatalog.All[i].Reward);
        }
        Assert.Equal(4, AchievementCatalog.All.Count(a => a.Reward != null));
        Assert.All(AchievementCatalog.All.Where(a => a.Reward != null), a => Assert.True(BoonCatalog.Default.Contains(a.Reward!)));
    }

    [Fact]
    public void ReservationsAreReplicatedAndNeverReuseAnIssuedSerialAfterCorruptionOrPartialFailure()
    {
        var disk = new MemoryStorage(); var store = new NativeProfileStore(disk);
        Assert.Equal(ProfileStatus.Absent, store.Inspect().Status); Assert.Empty(disk.Files);
        var first = store.AllocateRun(); Assert.Equal(1, first.Run); Assert.Equal(2, disk.Files.Count);
        disk.Files[NativeProfileStore.Slots[0]] = [0];
        Assert.Equal(ProfileStatus.Recovered, store.Inspect().Status);
        var second = store.AllocateRun(); Assert.Equal(2, second.Run); Assert.Equal(first.Profile, second.Profile);
        disk.FailAt = disk.Writes + 2; Assert.Throws<IOException>(() => store.AllocateRun());
        disk.FailAt = int.MaxValue;
        var fourth = store.AllocateRun(); Assert.Equal(4, fourth.Run);
    }

    [Fact]
    public void AwardsRemainMonotonicAndExistInBothRecordsBeforeTerminalAcknowledgement()
    {
        var disk = new MemoryStorage(); var store = new NativeProfileStore(disk); var run = store.AllocateRun();
        run.Earned.Add(Guardian); run.Pending.Add(Guardian);
        store.CommitProgress(run, terminal: true); Assert.Empty(run.Pending);
        Assert.Contains(Guardian, store.Inspect().Data!.Earned);
        Assert.Throws<IOException>(() => store.ValidateContinuation(run, terminal: false));
        disk.Files[NativeProfileStore.Slots[1]] = [0];
        Assert.Contains(Guardian, store.Inspect().Data!.Earned);
        Assert.Throws<IOException>(() => store.ValidateContinuation(run, terminal: false));
        store.ValidateContinuation(run, terminal: true);
        var next = store.AllocateRun(); Assert.Contains(Guardian, next.StartingAchievements);
        store.ValidateContinuation(run, terminal: true); // Construction alone leaves the terminal save viewable.
        store.CommitCheckpoint(next, terminal: false);
        Assert.Throws<IOException>(() => store.ValidateContinuation(run, terminal: true));
        next.Earned.Add(AchievementCatalog.Prefix + "first_blood"); store.CommitProgress(next, terminal: false);
        Assert.Contains(Guardian, store.Inspect().Data!.Earned);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedOrTruncatedAwardWriteRetainsPendingAndPreviousProgress(bool truncate)
    {
        var disk = new MemoryStorage(); var store = new NativeProfileStore(disk); var run = store.AllocateRun();
        run.Earned.Add(Guardian); run.Pending.Add(Guardian);
        disk.Truncate = truncate; if (!truncate) disk.FailAt = disk.Writes + 1;
        Assert.Throws<IOException>(() => store.CommitProgress(run, terminal: false));
        Assert.Contains(Guardian, run.Pending); Assert.DoesNotContain(Guardian, store.Inspect().Data!.Earned);
        disk.Truncate = false; disk.FailAt = int.MaxValue; store.CommitProgress(run, terminal: false);
        Assert.Empty(run.Pending); Assert.Contains(Guardian, store.Inspect().Data!.Earned);
    }

    [Fact]
    public void SecondAwardWriteFailureRequiresRetryAndThenSurvivesEitherSingleRecordLoss()
    {
        var disk = new MemoryStorage(); var store = new NativeProfileStore(disk); var run = store.AllocateRun();
        run.Earned.Add(Guardian); run.Pending.Add(Guardian); disk.FailAt = disk.Writes + 2;
        Assert.Throws<IOException>(() => store.CommitProgress(run, terminal: false)); Assert.Contains(Guardian, run.Pending);
        disk.FailAt = int.MaxValue; store.CommitProgress(run, terminal: false);
        var snapshots = disk.Files.ToDictionary(p => p.Key, p => p.Value);
        foreach (string slot in NativeProfileStore.Slots)
        {
            disk.Files.Clear(); foreach (var pair in snapshots) disk.Files.Add(pair.Key, pair.Value);
            disk.Files[slot] = [0]; Assert.Contains(Guardian, store.Inspect().Data!.Earned);
        }
    }

    [Theory]
    [InlineData("corrupt")]
    [InlineData("oversize")]
    [InlineData("incompatible")]
    public void UnreadableOrNewerIncompatibleProfileNeverOverwritesPreviousRecords(string kind)
    {
        var disk = new MemoryStorage(); var store = new NativeProfileStore(disk); store.AllocateRun();
        if (kind == "incompatible")
        {
            string slot = disk.Files.OrderByDescending(p => JsonNode.Parse(p.Value)!["sequence"]!.GetValue<long>()).First().Key;
            var record = JsonNode.Parse(disk.Files[slot])!; record["format"] = "roag-native-profile/99";
            disk.Files[slot] = JsonSerializer.SerializeToUtf8Bytes(record);
        }
        else foreach (string slot in NativeProfileStore.Slots) disk.Files[slot] = kind == "oversize" ? new byte[NativeProfileStore.MaximumBytes + 1] : [0];
        Assert.Equal(kind == "incompatible" ? ProfileStatus.Incompatible : ProfileStatus.Corrupt, store.Inspect().Status);
        int writes = disk.Writes; Assert.Throws<IOException>(() => store.AllocateRun()); Assert.Equal(writes, disk.Writes);
    }

    [Fact]
    public void UnknownAwardsAndForgedStartingUnlocksCannotBeCommitted()
    {
        var disk = new MemoryStorage(); var store = new NativeProfileStore(disk); var run = store.AllocateRun();
        int writes = disk.Writes; run.Earned.Add("unknown");
        Assert.Throws<InvalidDataException>(() => store.CommitProgress(run, terminal: false)); Assert.Equal(writes, disk.Writes);
        run.Earned.Clear(); run.StartingAchievements.Add(Guardian);
        Assert.Throws<IOException>(() => store.ValidateContinuation(run, terminal: false)); Assert.Equal(writes, disk.Writes);
    }
}
