using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class DungeonParcelTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(42)] [InlineData(-2147483648)] [InlineData(2147483647)]
    public void FullSizedComplexReservationsPreserveEveryExistingPlaceAndWorldGeography(int firstSeed)
    {
        for (int n = 0; n < 8; n++)
        {
            var plan = WorldPlan.Generate(unchecked(firstSeed + n), false, floorCount: 8);
            string before = JsonSerializer.Serialize(plan);
            var parcels = DungeonParcelPlanner.Reserve(plan);
            Assert.Equal(4, parcels.Count);
            Assert.Equal(parcels, DungeonParcelPlanner.Reserve(plan));
            Assert.Equal(before, JsonSerializer.Serialize(plan));
            Assert.All(parcels, parcel =>
            {
                Assert.True(plan.Contains(new(parcel.X, parcel.Y, 7)));
                Assert.True(plan.Contains(new(parcel.X + parcel.Width - 1, parcel.Y + parcel.Height - 1, 7)));
                Assert.All(parcels.Where(p => p.Id != parcel.Id), other => Assert.False(parcel.Overlaps(other, 16)));
                foreach (var center in plan.Towns.Select(t => t.Center).Concat(plan.Sites.Select(s => s.Center)))
                {
                    // Independent inclusive endpoint check for the original
                    // 64-square footprint and the sixteen-tile earth buffer.
                    Assert.True(parcel.X + parcel.Width <= center.X - 48 || parcel.X >= center.X + 48
                        || parcel.Y + parcel.Height <= center.Y - 48 || parcel.Y >= center.Y + 48);
                }
            });
        }
    }

    [Fact]
    public void EveryOriginalRouteAlternativeFitsWithItsActualPairedStairOffset()
    {
        var first = DungeonRoutes.ForStage(1, []);
        var second = first.SelectMany(r => DungeonRoutes.ForStage(2, [r.Id])).Select(r => r.Biome).Distinct().ToArray();
        Assert.InRange(first.Count, 1, DungeonParcelPlanner.Columns * DungeonParcelPlanner.Rows);
        Assert.InRange(second.Length, 1, DungeonParcelPlanner.Columns * DungeonParcelPlanner.Rows);
        var plan = WorldPlan.Generate(42, false, floorCount: 8);
        var parcel = DungeonParcelPlanner.Reserve(plan)[0];
        foreach (var biome in DungeonCatalog.Default.Biomes)
            for (int seed = 0; seed < 8; seed++)
            {
                var upper = DungeonLayoutGenerator.Generate(seed, biome.Id);
                var lower = DungeonLayoutGenerator.Generate(unchecked(seed + 1), biome.Id);
                int dx = upper.Elite.X - 4 - lower.Start.X, dy = upper.Elite.Y - lower.Start.Y;
                // Fit each possible physical pair into each reserved slot,
                // retaining the four-tile horizontal stair relation exactly.
                for (int slot = 0; slot < 6; slot++)
                {
                    var origin = new WorldPosition(parcel.X + 16 + slot % 3 * DungeonParcelPlanner.SlotWidth - Math.Min(0, dx),
                        parcel.Y + 16 + slot / 3 * DungeonParcelPlanner.SlotHeight - Math.Min(0, dy), 1);
                    var lowerOrigin = origin.Offset(dx, dy) with { Floor = 2 };
                    Assert.True(parcel.Contains(origin)); Assert.True(parcel.Contains(origin.Offset(upper.Width - 1, upper.Height - 1)));
                    Assert.True(parcel.Contains(lowerOrigin)); Assert.True(parcel.Contains(lowerOrigin.Offset(lower.Width - 1, lower.Height - 1)));
                    var exit = origin.Offset(upper.Elite.X, upper.Elite.Y);
                    Assert.Equal(lowerOrigin.Offset(lower.Start.X, lower.Start.Y), WorldStairs.Destination(exit, Material.StairsDown));
                }
            }
    }
}
