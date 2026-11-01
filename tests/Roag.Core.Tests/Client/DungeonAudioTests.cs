using System.Security.Cryptography;
using System.Text.Json;
using Roag.Client;
using Xunit;

namespace Roag.Core.Tests.Client;

public sealed class DungeonAudioTests
{
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
