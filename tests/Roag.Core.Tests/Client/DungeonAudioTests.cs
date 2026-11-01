using System.Security.Cryptography;
using System.Text.Json;
using Roag.Client;
using Xunit;

namespace Roag.Core.Tests.Client;

public sealed class DungeonAudioTests
{
    [Theory]
    [InlineData("attack")]
    [InlineData("chest")]
    [InlineData("chest-open")]
    public void AdditionalActionCuesAreDeterministicBoundedPcmWithoutAnAudioDevice(string kind)
    {
        var pcm = DungeonSoundSynthesis.Sample(kind);
        Assert.Equal(pcm, DungeonSoundSynthesis.Sample(kind)); Assert.Equal(0, pcm.Length % 2);
        var samples = Enumerable.Range(0, pcm.Length / 2).Select(i => (int)BitConverter.ToInt16(pcm, i * 2)).ToArray();
        Assert.InRange(samples.Length, DungeonSoundSynthesis.SampleRate / 10, DungeonSoundSynthesis.SampleRate);
        Assert.Contains(samples, s => s > 1000); Assert.Contains(samples, s => s < -1000);
        Assert.All(samples, s => Assert.InRange(s, -24000, 24000));
        Assert.InRange(Math.Abs(samples[0]), 0, 10); Assert.InRange(Math.Abs(samples[^1]), 0, 100);
    }

    [Fact]
    public void NativeBuiltInAudioMatchesAllEightOriginalPcmSamplesWithoutAnAudioDevice()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-audio.json")));
        foreach (var sample in fixture.RootElement.GetProperty("samples").EnumerateArray())
        {
            var pcm = DungeonSoundSynthesis.Sample(sample.GetProperty("kind").GetString()!);
            Assert.Equal(sample.GetProperty("bytes").GetInt32(), pcm.Length);
            Assert.Equal(sample.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(pcm)).ToLowerInvariant());
        }
        Assert.Throws<ArgumentException>(() => DungeonSoundSynthesis.Sample("missing"));
    }
}
