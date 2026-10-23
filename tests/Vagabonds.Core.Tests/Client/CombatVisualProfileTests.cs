using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Vagabonds.Client;
using Vagabonds.Core.Content;
using Xunit;

namespace Vagabonds.Tests.Client;

public sealed class CombatVisualProfileTests
{
    private static string Document()
    {
        using var resource = typeof(WorldGame).Assembly.GetManifestResourceStream("Vagabonds.CombatPresentation.json")!;
        using var reader = new StreamReader(resource); return reader.ReadToEnd();
    }

    [Fact]
    public void ShippedProfilesCoverAbilitiesAndResolveNewAbilitiesWithoutNamesOrGameState()
    {
        var profiles = CombatVisualProfiles.Default;
        Assert.Equal(ContentCatalog.Default.All<AbilityDefinition>().Select(a => a.Id).Order(), profiles.AbilityIds.Order());
        Assert.Equal("projectile", profiles.Resolve("ability.arrow", "damage").Family);
        Assert.Equal("cooperative", profiles.Resolve("ability.twin-strike", "damage").Family);
        Assert.Equal("heal", profiles.Resolve("ability.shared-aid", "cleanse", true).Family);
        Assert.Equal("combat.ember", profiles.Resolve("ability.future-skill", "burn").Id);
        Assert.Equal("cooperative", profiles.Resolve(null, "damage", true).Family);
        Assert.Equal("heal", profiles.Resolve(null, "cleanse", true).Family);
        Assert.Equal("melee", profiles.Resolve(null, "future-effect").Family);
        Assert.All(profiles.Profiles, p => Assert.InRange(p.Duration, .16, 1.2));
    }

    [Theory]
    [InlineData("family", "tactical-movement")]
    [InlineData("sprite", "missing-sprite")]
    [InlineData("sound", "missing-cue")]
    [InlineData("color", "#XX00FF")]
    [InlineData("color", "red")]
    [InlineData("id", "ability.strike")]
    public void InvalidProfilesFailWithAnAuthoringError(string field, string value)
    {
        var document = JsonNode.Parse(Document())!; document["profiles"]![0]![field] = value;
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(document.ToJsonString()));
    }

    [Theory]
    [InlineData(-.1)] [InlineData(0)] [InlineData(.039)] [InlineData(.61)]
    public void InvalidDurationsCannotHangOrSkipRequiredBeats(double duration)
    {
        var document = JsonNode.Parse(Document())!; document["profiles"]![0]!["travel"] = duration;
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(document.ToJsonString()));
    }

    [Fact]
    public void IndividualValidDurationsCannotMakeAnUnboundedSequence()
    {
        var document = JsonNode.Parse(Document())!;
        foreach (string beat in new[] { "anticipation", "travel", "impact", "recovery" }) document["profiles"]![0]![beat] = .4;
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(document.ToJsonString()));
    }

    [Fact]
    public void BrokenReferencesMissingFallbacksDuplicatesAndUnknownFieldsAreRejected()
    {
        var document = JsonNode.Parse(Document())!;
        document["abilities"]!["ability.arrow"] = "combat.missing";
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(document.ToJsonString()));
        document = JsonNode.Parse(Document())!; document["abilities"]!["ability.missing"] = "combat.melee";
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(document.ToJsonString()));
        document = JsonNode.Parse(Document())!; document["effects"]!.AsObject().Remove("burn");
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(document.ToJsonString()));
        document = JsonNode.Parse(Document())!; document["profiles"]!.AsArray().Add(document["profiles"]![0]!.DeepClone());
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(document.ToJsonString()));
        document = JsonNode.Parse(Document())!; document["profiles"]![0]!["damange"] = 999;
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(document.ToJsonString()));
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(Document().Replace("\"version\": 1", "\"version\": 1, \"version\": 1")));
        document = JsonNode.Parse(Document())!; document["profiles"] = null;
        Assert.Throws<InvalidDataException>(() => CombatVisualProfiles.Load(document.ToJsonString()));
    }

    [Fact]
    public void ProfileEditsHaveTheirOwnHashAndNeverChangeGameplayContent()
    {
        string gameplayHash = ContentCatalog.Default.Hash;
        var document = JsonNode.Parse(Document())!; document["profiles"]![0]!["color"] = "#22FF66";
        var edited = CombatVisualProfiles.Load(document.ToJsonString());
        Assert.NotEqual(CombatVisualProfiles.Default.Hash, edited.Hash);
        Assert.Equal(34, edited.Resolve("ability.strike").Tint.R);
        Assert.Equal(gameplayHash, ContentCatalog.Default.Hash);
    }

    [Fact]
    public void AllOriginalAudioCuesAreDistinctDeterministicBoundedAndFaded()
    {
        var hashes = new HashSet<string>();
        foreach (string cue in CombatSoundSynthesis.Cues)
        {
            byte[] pcm = CombatSoundSynthesis.Cue(cue);
            Assert.Equal(pcm, CombatSoundSynthesis.Cue(cue));
            Assert.Equal(0, BitConverter.ToInt16(pcm, 0)); Assert.Equal(0, BitConverter.ToInt16(pcm, pcm.Length - 2));
            Assert.InRange(pcm.Length, 8000, 14000); Assert.Equal(0, pcm.Length % 2);
            var samples = Enumerable.Range(0, pcm.Length / 2).Select(i => (int)BitConverter.ToInt16(pcm, i * 2)).ToArray();
            Assert.InRange(samples.Max(Math.Abs), 1000, 22000);
            hashes.Add(Convert.ToHexString(SHA256.HashData(pcm)));
        }
        Assert.Equal(CombatSoundSynthesis.Cues.Count, hashes.Count);
        Assert.Throws<ArgumentException>(() => CombatSoundSynthesis.Cue("missing"));
    }

    [Fact]
    public void MutingAndDisposalNeverOpenAnAudioDeviceOrPlayACue()
    {
        using var audio = new CombatAudio();
        audio.Play("slash", 0); audio.Play("slash", float.NaN); audio.Stop(); audio.Dispose(); audio.Play("slash", 1);
        Assert.Equal(0, audio.PlayedCues); Assert.False(audio.Unavailable);
    }
}
