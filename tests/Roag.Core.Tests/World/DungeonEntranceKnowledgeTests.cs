using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class DungeonEntranceKnowledgeTests
{
    [Fact]
    public void PaidRumoursKeepOriginalDiscoveriesAndRememberEntrancesWithoutRevealingTerrain()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var complexes = DungeonParcelPlanner.Reserve(world.Plan, 2)
            .Select(p => world.InstallDungeonComplexTerrain(DungeonComplexPlanner.Generate(world.Plan, p))).ToArray();
        Assert.Empty(world.KnownDungeonEntrances());
        Assert.DoesNotContain("KnownEntrance", JsonSerializer.Serialize(world.State), StringComparison.Ordinal);
        var hero = world.DungeonMember; var person = world.ActorById(1000);
        // Physical unit fixture isolates the paid rumour transaction from unrelated travel.
        hero.Position = new(100, 100, 1); person.Position = new(101, 100, 1);
        for (int y = 97; y <= 103; y++) for (int x = 97; x <= 104; x++) world.Chunks.Edit(new(x, y, 1), Material.Floor);
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        var combat = world.DungeonCombat;
        var intent = new TacticalIntent(TacticalAction.SocialInteraction, Target: person.Id, Item: "interaction.rumours");
        var preview = combat.Preview(intent); Assert.True(preview.Valid);
        string chunks = JsonSerializer.Serialize(world.State.Chunks); int random = combat.State.RandomState;
        string before = JsonSerializer.Serialize(world.State);
        Assert.Empty(world.KnownDungeonEntrances()); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        long clock = combat.Clock;
        Assert.True(combat.Commit(intent).Success);
        Assert.Equal(clock + preview.Recovery, Assert.Single(combat.State.Timeline.Pending, e => e.ActorId == hero.Id).Due);
        Assert.Equal(random, combat.State.RandomState);
        Assert.Equal(chunks, JsonSerializer.Serialize(world.State.Chunks));
        Assert.Single(world.State.Discoveries, d => d.Known && d.TownId == person.HomeTown);
        var report = Assert.Single(world.KnownDungeonEntrances());
        Assert.Equal(0, report.Position.Floor); Assert.False(world.Chunks.Seen(report.Position));
        Assert.Equal(Material.StairsDown, world.Chunks.At(report.Position));
        Assert.True(complexes.Single(c => c.Parcel.Id == report.Id).KnownEntrance);
        Assert.Contains(world.State.Journal, text => text.Contains("Entrance report: " + report.Name, StringComparison.Ordinal));
        RoundTrip(world);
    }

    [Fact]
    public void OnlyActuallyObservedEntrancesBecomeKnownAndReportsSurviveLeavingSight()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var blueprints = DungeonParcelPlanner.Reserve(world.Plan, 2).Select(p => DungeonComplexPlanner.Generate(world.Plan, p)).ToArray();
        var complexes = blueprints.Select(world.InstallDungeonComplexTerrain).ToArray();
        var hero = world.DungeonMember;
        hero.Position = world.State.Position = blueprints[0].SurfaceEntrance.Offset(1, 0);
        long clock = world.DungeonCombat.Clock; int random = world.DungeonCombat.State.RandomState;
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.True(complexes[0].KnownEntrance); Assert.False(complexes[1].KnownEntrance);
        var report = Assert.Single(world.KnownDungeonEntrances()); Assert.Equal(blueprints[0].SurfaceEntrance, report.Position);
        Assert.Equal(clock, world.DungeonCombat.Clock); Assert.Equal(random, world.DungeonCombat.State.RandomState);
        Assert.DoesNotContain(blueprints[1].SurfaceEntrance, world.Visible);
        Assert.False(world.Chunks.Seen(blueprints[1].SurfaceEntrance));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.West)).Success);
        Assert.Equal(1, hero.Position.Floor); Assert.DoesNotContain(report.Position, world.Visible);
        Assert.Equal(report, Assert.Single(world.KnownDungeonEntrances()));
        RoundTrip(world);
    }

    private static void RoundTrip(WorldSession world)
    {
        string directory = Path.Combine(Path.GetTempPath(), "roag-entrance-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(directory, "campaign.json"); world.Save(path);
            using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(world.KnownDungeonEntrances(), loaded.KnownDungeonEntrances());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
