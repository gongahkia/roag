using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonCatalogTests
{
    [Fact]
    public void NativeDungeonCatalogPreservesEveryOriginalRoomBiomeAndFinalChamber()
    {
        var catalog = DungeonCatalog.Default;
        Assert.Equal(147, catalog.Rooms.Count); Assert.Equal(7, catalog.Biomes.Count); Assert.Equal(21, catalog.RoomOrder.Count);
        foreach (var biome in catalog.Biomes)
        {
            Assert.NotEmpty(catalog.ForRole(biome.Id, "start"));
            Assert.NotEmpty(catalog.ForRole(biome.Id, "combat"));
            Assert.NotEmpty(catalog.ForRole(biome.Id, "elite"));
            Assert.All(biome.EnemyIds.Append(biome.EliteId), id => Assert.True(EnemyCatalog.Default.Contains(id)));
        }
        Assert.Equal(140, catalog.Generation.Width); Assert.Equal(90, catalog.Generation.Height);
        Assert.Equal(23, catalog.Finale.Width); Assert.Equal(17, catalog.Finale.Height);
        Assert.Equal('.', catalog.Finale.Rows[catalog.Finale.StartY - 1][catalog.Finale.StartX - 1]);
        Assert.Equal('.', catalog.Finale.Rows[catalog.Finale.BossY - 1][catalog.Finale.BossX - 1]);
    }

    [Fact]
    public void NativeRotationsAndMirrorsMatchAllOriginalRoomTransforms()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-room-transforms.json")));
        var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; int count = 0;
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var original = DungeonCatalog.Default.Rooms[sample.GetProperty("id").GetString()!];
            var transformed = original.Transform(sample.GetProperty("turns").GetInt32(), sample.GetProperty("mirror").GetBoolean());
            Assert.Equal(sample.GetProperty("rows").EnumerateArray().Select(e => e.GetString()), transformed.Rows);
            Assert.Equal(sample.GetProperty("sockets").Deserialize<RoomSocket[]>(json), transformed.Sockets);
            Assert.Equal(sample.GetProperty("editable").Deserialize<RoomPoint[]>(json), transformed.Editable);
            Assert.Equal(sample.GetProperty("structures").Deserialize<RoomStructure[]>(json), transformed.Structures);
            foreach (var marker in sample.GetProperty("markers").EnumerateObject())
                Assert.Equal(marker.Value.Deserialize<RoomPoint[]>(json), transformed.Markers[marker.Name]);
            Assert.Equal(sample.GetProperty("markers").EnumerateObject().Count(), transformed.Markers.Count);
            count++;
        }
        Assert.Equal(1176, count);
    }
}
