using System.Text.Json;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class SaveSnapshotTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-snapshot-audit-" + Guid.NewGuid().ToString("N"));
    private string Slot => Path.Combine(_directory, "world.json");
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static string State(WorldSession world) => JsonSerializer.Serialize(world.State);
    private static string[] Referenced(string path)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(path));
        return manifest.RootElement.GetProperty("chunks").EnumerateArray().Select(c => c.GetProperty("hash").GetString()! + ".json").Distinct().Order().ToArray();
    }
    [Fact]
    public void RepeatedSavesRetainCurrentChunksOtherFilesAndIndependentSlots()
    {
        using var world = WorldSession.Create(42); world.Save(Slot);
        string other = Path.Combine(_directory, "other.json"); world.Save(other); string original = State(world);
        string note = Path.Combine(Slot + ".chunks", "notes.json"); File.WriteAllText(note, "Unrelated file");
        for (int i = 0; i < 12; i++)
        {
            world.Chunks.Edit(world.State.Position.Offset(3 + i % 4, 3 + i / 4), Material.Floor); world.Save(Slot);
            using var loaded = WorldSession.Load(Slot); Assert.Equal(State(world), State(loaded));
            var snapshots = Directory.GetFiles(Slot + ".chunks", "*.json").Select(Path.GetFileName).Where(n => n != "notes.json").Order().ToArray();
            Assert.Equal(Referenced(Slot), snapshots);
        }
        Assert.Equal("Unrelated file", File.ReadAllText(note)); using var independent = WorldSession.Load(other); Assert.Equal(original, State(independent));
    }
    [Fact]
    public void FailedSerializationPreservesThePreviousManifestAndEveryReferencedChunk()
    {
        using var world = WorldSession.Create(42); world.Save(Slot); string original = State(world); byte[] manifest = File.ReadAllBytes(Slot);
        string[] referenced = Referenced(Slot); world.Chunks.Edit(world.State.Position.Offset(4, 4), Material.Floor);
        double hunger = world.Party.Single().Body.Hunger; world.Party.Single().Body.Hunger = double.NaN;
        Assert.Throws<ArgumentException>(() => world.Save(Slot)); Assert.Equal(manifest, File.ReadAllBytes(Slot));
        Assert.All(referenced, file => Assert.True(File.Exists(Path.Combine(Slot + ".chunks", file))));
        using (var loaded = WorldSession.Load(Slot)) Assert.Equal(original, State(loaded));
        world.Party.Single().Body.Hunger = hunger; world.Save(Slot);
        Assert.Equal(Referenced(Slot), Directory.GetFiles(Slot + ".chunks", "*.json").Select(Path.GetFileName).Order().ToArray());
        using var recovered = WorldSession.Load(Slot); Assert.Equal(State(world), State(recovered));
    }
}
