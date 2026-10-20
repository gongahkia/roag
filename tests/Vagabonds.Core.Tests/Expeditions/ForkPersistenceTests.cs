using Vagabonds.Core.Expeditions;
using Vagabonds.Core.Generation;
using Xunit;

namespace Vagabonds.Tests.Expeditions;

public sealed class ForkPersistenceTests
{
    [Fact]
    public void LegacyImportLeavesOriginalUntouchedAndPreservesContinuation()
    {
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string source = Path.Combine(directory, "lantern.json"), target = Path.Combine(directory, "vagabonds.json");
            var original = new Campaign(1337);
            original.TravelTo("to-wilds"); original.TravelTo("iron-cache");
            original.Save(source);
            byte[] before = File.ReadAllBytes(source);
            var imported = Campaign.ImportLegacy(source, target);
            Assert.Equal(before, File.ReadAllBytes(source));
            Assert.Equal(original.StateJson(), imported.StateJson());
            Assert.Equal(original.StateJson(), Campaign.Load(target).StateJson());
            Assert.True(original.Wait().Success); Assert.True(imported.Wait().Success);
            Assert.Equal(original.StateJson(), imported.StateJson());
            imported.Save(target);
            Assert.Equal(before, File.ReadAllBytes(source));
            Assert.Equal(imported.StateJson(), Campaign.Load(target).StateJson());
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void ImportCannotOverwriteItsOriginalSource()
    {
        string source = Path.Combine(Path.GetTempPath(), "vagabonds-source-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            new Campaign().Save(source);
            byte[] before = File.ReadAllBytes(source);
            Assert.Throws<ArgumentException>(() => Campaign.ImportLegacy(source, source));
            Assert.Equal(before, File.ReadAllBytes(source));
        }
        finally { File.Delete(source); }
    }

    [Theory]
    [InlineData("{\"format\":\"ithica\",\"version\":1}")]
    [InlineData("{\"format\":\"regolith-csharp\",\"version\":1}")]
    public void ImportRejectsColonySavesWithoutCreatingAnOutput(string json)
    {
        string source = Path.Combine(Path.GetTempPath(), "vagabonds-invalid-" + Guid.NewGuid().ToString("N") + ".json");
        string target = source + ".imported";
        try
        {
            File.WriteAllText(source, json);
            Assert.Throws<InvalidDataException>(() => Campaign.ImportLegacy(source, target));
            Assert.Equal(json, File.ReadAllText(source));
            Assert.False(File.Exists(target));
        }
        finally { File.Delete(source); File.Delete(target); }
    }

    [Fact]
    public void ImportRejectsAnOtherwiseValidLunarFieldSession()
    {
        string source = Path.Combine(Path.GetTempPath(), "vagabonds-field-" + Guid.NewGuid().ToString("N") + ".json");
        string target = source + ".imported";
        try
        {
            Campaign.CreateField(42, LunarGenerator.Create("salvage", 42, false, false)).Save(source);
            byte[] before = File.ReadAllBytes(source);
            Assert.Throws<InvalidDataException>(() => Campaign.ImportLegacy(source, target));
            Assert.Equal(before, File.ReadAllBytes(source));
            Assert.False(File.Exists(target));
        }
        finally { File.Delete(source); File.Delete(target); }
    }
}
