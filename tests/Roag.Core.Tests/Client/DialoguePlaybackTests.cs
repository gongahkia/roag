using Roag.Client;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Client;

public sealed class DialoguePlaybackTests
{
    [Fact]
    public void ClickRevealsThenAdvancesAndFinishedSpeechDoesNotReplaySounds()
    {
        var speech = new DialoguePlayback(); speech.Start([new(1, "A", "hero", "Hello, traveller."), new(2, "B", "hero", "Hello!")], 60, 4);
        int sounds = 0; speech.Update(.05, (_, _) => sounds++);
        Assert.True(speech.Speaking); Assert.True(sounds > 0); Assert.False(speech.Finished);
        speech.Advance(); Assert.False(speech.Speaking); Assert.Equal("Hello, traveller.", speech.Text);
        int old = sounds; speech.Update(10, (_, _) => sounds++); Assert.Equal(old, sounds);
        speech.Advance(); Assert.Equal(2, speech.Line!.Speaker); Assert.Equal("", speech.Text);
        speech.Advance(); speech.Advance(); Assert.True(speech.Finished);
        speech.Advance(); Assert.True(speech.Finished); Assert.Equal(2, speech.Line.Speaker);
    }
    [Fact]
    public void LongDialoguePaginatesWithoutLosingTextAndPunctuationCreatesSilence()
    {
        string text = string.Join(' ', Enumerable.Repeat("The traveller waits beside the old gate.", 20));
        var pages = DialoguePlayback.Paginate(text, 40, 3).ToArray(); Assert.True(pages.Length > 1);
        Assert.All(pages, p => { Assert.InRange(p.Split('\n').Length, 1, 3); Assert.All(p.Split('\n'), l => Assert.True(l.Length <= 40)); });
        Assert.Equal(text, string.Join(' ', pages).Replace('\n', ' '));
        var speech = new DialoguePlayback(); speech.Start([new(1, "A", "hero", "!hello")], 60, 4);
        int sounds = 0; speech.Update(.01, (_, _) => sounds++); Assert.Equal("!", speech.Text);
        speech.Update(.1, (_, _) => sounds++); Assert.Equal("!", speech.Text); Assert.Equal(0, sounds);
    }
    [Fact]
    public void OriginalVoiceBuffersAreDeterministicDistinctAndFadeAtTheirEdges()
    {
        byte[] a = SpeechSynthesis.Syllable(10001, 'a'), b = SpeechSynthesis.Syllable(10002, 'a');
        Assert.Equal(a, SpeechSynthesis.Syllable(10001, 'a')); Assert.NotEqual(a, b); Assert.NotEqual(a, SpeechSynthesis.Syllable(10001, 'i'));
        Assert.InRange(a.Length, 2000, 4000); Assert.Equal(0, BitConverter.ToInt16(a, 0)); Assert.Equal(0, BitConverter.ToInt16(a, a.Length - 2));
        Assert.Contains(a, sample => sample != 0);
    }
    [Theory]
    [InlineData("person", true)] [InlineData("work", false)] [InlineData("inventory", false)] [InlineData("game", true)] [InlineData("", false)]
    public void ManagementPanelsRespectTheAgreedClockPolicy(string panel, bool paused) => Assert.Equal(paused, WorldGame.PanelPausesTime(panel));
}
