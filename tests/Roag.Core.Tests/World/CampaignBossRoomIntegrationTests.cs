using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class CampaignBossRoomIntegrationTests
{
    [Fact]
    public void ArenaRoomTakesPrecedenceWithinAComplexColumnAndContinuesExactly()
    {
        string directory = Path.Combine(Path.GetTempPath(), "roag-boss-room-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var world = WorldSession.CreateNativeCampaign(42);
            var parcel = DungeonParcelPlanner.Reserve(world.Plan, 1).Single();
            world.InstallDungeonComplexTerrain(DungeonComplexPlanner.Generate(world.Plan, parcel));
            var center = new WorldPosition(parcel.X + 7, parcel.Y + 7, 1);
            var hero = world.Party.Single(); var position = hero.Position;
            long clock = world.DungeonCombat.Clock;
            var arena = world.CreateCampaignBossArena(CampaignBossKind.Bellkeeper, center, 73, false);
            Assert.Equal(position, hero.Position); Assert.Equal(clock, world.DungeonCombat.Clock);
            Assert.False(world.Chunks.Seen(center));
            var room = world.CampaignBossRoomAt(center);
            Assert.NotNull(room); Assert.Equal(1000 + arena.Id, room.Value.Place);
            Assert.Equal(room, world.RoomAt(center));
            Assert.Equal(room, WorldSession.SnapshotAuthoredRoomAt(world.State, center));
            Assert.Equal(room, WorldSession.SnapshotRoomAt(world.State, world.Chunks.Generator, center));
            string slot = Path.Combine(directory, "campaign.json"); world.Save(slot);
            using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(room, loaded.RoomAt(center));
            Assert.Equal(room, WorldSession.SnapshotRoomAt(loaded.State, loaded.Chunks.Generator, center));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
