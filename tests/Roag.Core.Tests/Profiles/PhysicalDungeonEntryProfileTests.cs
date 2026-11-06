using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Profiles;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Profiles;

public sealed class PhysicalDungeonEntryProfileTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void PaidEntryUsesThePhysicalBranchAndCannotBeGrantedFromOutside(int stage)
    {
        string directory = Path.Combine(Path.GetTempPath(), "roag-physical-entry-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var world = WorldSession.CreateNativeCampaign(42, ClassKind.Ranger);
            var parcel = DungeonParcelPlanner.Reserve(world.Plan, 1).Single();
            var complex = world.InstallDungeonComplexTerrain(DungeonComplexPlanner.Generate(world.Plan, parcel));
            string slot = Path.Combine(directory, "campaign.json");
            world.EnablePermanentProfile(slot);
            var hero = world.Party.Single();
            var floor = complex.Run.Floors.First(f => f.Stage == stage);
            var start = floor.Origin.Offset(-1, 1);
            // Explicitly excavated boundary setup; the paid command below,
            // rather than camera selection or a synthetic entry event, earns it.
            for (int i = 0; i <= 3; i++) world.Chunks.Edit(start.Offset(i, 0), Material.Floor);
            hero.Position = world.State.Position = start;
            Assert.True(world.SelectDungeonMember(hero.Id).Success);
            Assert.Null(world.DungeonFloorAt(hero.Position));
            string before = JsonSerializer.Serialize(world.State);
            world.RecordDungeonEntry(hero.Id, stage, stage == 4);
            Assert.Equal(before, JsonSerializer.Serialize(world.State));
            Assert.Empty(world.State.Tactics!.Profile!.Earned);
            Assert.Equal(1, complex.Run.Stage);

            var command = new TacticalIntent(TacticalAction.Dash, Facing.East);
            var preview = world.DungeonCombat.Preview(command);
            Assert.True(preview.Valid, preview.Reason);
            Assert.Equal(3, preview.Cells.Count);
            long clock = world.DungeonCombat.Clock;
            Assert.True(world.SubmitDungeon(command).Success);
            Assert.Equal(clock + preview.Recovery, world.DungeonCombat.Clock);
            Assert.Contains(AchievementCatalog.Prefix + "deeper_road", world.PermanentAchievements);
            Assert.Equal(stage == 4, world.PermanentAchievements.Contains(AchievementCatalog.Prefix + "sanctum_reached"));
            Assert.Equal(1, complex.Run.Stage);
            world.Save(slot);
            using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(world.PermanentAchievements.Order(), loaded.PermanentAchievements.Order());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
