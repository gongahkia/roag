using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeAppearanceTests
{
    [Fact]
    public void EveryServiceUsesAnExistingStoredVagabondsBinding()
    {
        using var manifest = JsonDocument.Parse(typeof(NativeAppearance).Assembly.GetManifestResourceStream("Roag.ContentSprites.json")!);
        foreach (var kind in Enum.GetValues<DungeonServiceKind>())
        {
            string sprite = manifest.RootElement.GetProperty("bindings").GetProperty(NativeAppearance.ForService(kind)).GetString()!;
            Assert.StartsWith("urizen-onebit-", sprite); Assert.True(manifest.RootElement.GetProperty("sprites").TryGetProperty(sprite, out _));
        }
    }

    [Fact]
    public void AllNativeClassesAndEnemiesUseStoredUniversalAtlasIdentitiesAndUnchangedCrops()
    {
        using var manifest = JsonDocument.Parse(typeof(NativeAppearance).Assembly.GetManifestResourceStream("Roag.ContentSprites.json")!);
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/native-actor-bindings.json")));
        var records = inventory.RootElement.GetProperty("actors").EnumerateArray().ToDictionary(r => r.GetProperty("content_id").GetString()!);
        var required = ClassCatalog.All.Select(c => (c.Id, Binding: NativeAppearance.ForClass(c.Kind)))
            .Concat(EnemyCatalog.Default.Definitions.Select(e => (e.Id, Binding: NativeAppearance.ForEnemy(e.Id)))).ToArray();
        Assert.Equal(65, required.Length); Assert.Equal(65, records.Count);
        foreach (var (id, binding) in required)
        {
            var record = records[id]; Assert.Equal(binding, record.GetProperty("binding").GetString());
            string sprite = manifest.RootElement.GetProperty("bindings").GetProperty(binding).GetString()!;
            Assert.Equal(record.GetProperty("sprite").GetString(), sprite);
            Assert.StartsWith("urizen-onebit-", sprite);
            var crop = manifest.RootElement.GetProperty("sprites").GetProperty(sprite);
            Assert.Equal(record.GetProperty("source").GetString(), crop.GetProperty("source").GetString());
            Assert.Equal(record.GetProperty("rect").EnumerateArray().Select(v => v.GetInt32()), crop.GetProperty("rect").EnumerateArray().Select(v => v.GetInt32()));
        }
    }

    [Fact]
    public void ActualDungeonActorsAndFreeStartingClassSelectionUseTheirOwnAppearance()
    {
        using var world = WorldSession.CreateDungeonRun(42, ClassKind.Ranger);
        Assert.Equal(NativeAppearance.ForClass(ClassKind.Ranger), world.Party.Single().Sprite);
        Assert.True(world.ChooseDungeonClass(ClassKind.Geomancer).Success);
        Assert.Equal(NativeAppearance.ForClass(ClassKind.Geomancer), world.Party.Single().Sprite);
        Assert.All(world.State.Actors.Where(a => a.Tactics?.Enemy != null),
            actor => Assert.Equal(NativeAppearance.ForEnemy(actor.Tactics!.DefinitionId), actor.Sprite));
    }
}
