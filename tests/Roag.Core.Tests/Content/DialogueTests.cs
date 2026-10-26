using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Content;

public sealed class DialogueTests
{
    [Fact]
    public void EveryConversationHasRuntimePreviewAndUsesRealNamesWithoutSpendingTime()
    {
        foreach (var dialogue in ContentCatalog.Default.All<DialogueDefinition>())
        {
            var result = JsonSerializer.SerializeToNode(DialoguePreview.Run(ContentCatalog.Default, dialogue.Id, 42), ContentCatalog.Json)!;
            Assert.True(result["unchangedStateDuringSpeech"]!.GetValue<bool>());
            Assert.NotEmpty(result["lines"]!.AsArray());
            foreach (var line in result["lines"]!.AsArray()) Assert.DoesNotContain("{", line!["text"]!.GetValue<string>());
            if (dialogue.Interaction != null) Assert.True(result["result"]!["success"]!.GetValue<bool>(), dialogue.Id);
        }
        Assert.Equal(ContentCatalog.Default.All<InteractionDefinition>().Count() + 1, ContentCatalog.Default.All<DialogueDefinition>().Count());
    }
    [Fact]
    public void SpeechChangesDoNotInvalidateSavesButMechanicsChangesStillDo()
    {
        var original = ContentCatalog.Default;
        var without = ContentCatalog.Load(ContentTests.Documents().Where(d => !d.File.Contains("dialogue", StringComparison.Ordinal)));
        Assert.Equal(original.Hash, without.Hash);
        var edited = ContentTests.Edited("dialogue.stories", n => n["variants"]![0]!["player"] = "Tell me about your work.");
        Assert.Empty(edited.Issues); Assert.Equal(original.Hash, edited.Hash); Assert.NotEqual(original.PresentationHash, edited.PresentationHash);
        Assert.NotEqual(original.Hash, ContentTests.Edited("interaction.healing", n => n["amount"] = 1).Hash);
    }
    [Theory]
    [InlineData("condition")] [InlineData("token")] [InlineData("reference")] [InlineData("null")] [InlineData("fallback")]
    public void InvalidSpeechIsReportedBeforeRuntime(string kind)
    {
        var edited = ContentTests.Edited("dialogue.trade", n =>
        {
            if (kind == "condition") n["variants"]![0]!["when"] = "invented";
            if (kind == "token") n["variants"]![0]!["player"] = "Hello {invented}.";
            if (kind == "reference") n["interaction"] = "interaction.missing";
            if (kind == "null") n["variants"] = null;
            if (kind == "fallback") n["variants"]![0]!["when"] = "hungry";
        });
        Assert.NotEmpty(edited.Issues);
    }
}
