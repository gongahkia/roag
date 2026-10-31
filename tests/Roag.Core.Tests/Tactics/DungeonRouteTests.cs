using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonRouteTests
{
    [Fact]
    public void EveryBiomeRouteBranchMatchesThePinnedOriginalOrderedOffers()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-routes.json")));
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var history = sample.GetProperty("history").EnumerateArray().Select(e => e.GetString()!).ToArray();
            var actual = DungeonRoutes.ForStage(sample.GetProperty("stage").GetInt32(), history);
            var expected = sample.GetProperty("routes").EnumerateArray().ToArray();
            Assert.Equal(expected.Select(r => r.GetProperty("id").GetString()), actual.Select(r => r.Id));
            Assert.Equal(expected.Select(r => r.GetProperty("name").GetString()), actual.Select(r => r.Name));
            Assert.Equal(expected.Select(r => r.GetProperty("description").GetString()), actual.Select(r => r.Description));
        }
    }

    [Fact]
    public void DungeonEliteRemainsDormantUntilARealPartyActionReachesItsSevenCellRadius()
    {
        using var world = WorldSession.CreateDungeonRun(42); var combat = world.DungeonCombat;
        var floor = world.State.Dungeon!.Floors[1]; var boss = world.State.Actors.Single(a => a.Id == floor.Boss);
        Assert.True(boss.Tactics!.Enemy!.Dormant);
        Assert.DoesNotContain(combat.State.Queue.Snapshot().Pending, w => w.ActorId == boss.Id);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.True(boss.Tactics.Enemy.Dormant);
        var hero = world.Party.Single(); hero.Position = world.State.Position = boss.Position.Offset(0, 7);
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.False(boss.Tactics.Enemy.Dormant);
        Assert.Single(combat.State.Events, e => e.Kind == "EliteEngaged");
        TacticalValidation.Validate(world.Plan, world.State);
    }

    private static void DefeatBoss(WorldSession world)
    {
        var combat = world.DungeonCombat; var boss = world.State.Actors.Single(a => a.Id == world.State.Dungeon!.Floors.Last().Boss);
        combat.ResolveAttack(new(combat.State.NextAction++, world.State.Party[0], world.Party.Single().Position, [boss.Position], "test:boss-receipt",
            AttackDelivery.Area, DamageElement.Physical, 10000, combat.Clock, IgnoresTerrain: true));
    }

    [Fact]
    public void ThreePaidBiomeRoutesConnectAllSevenPersistentFloorsAndTheAuthoredFinalSanctum()
    {
        using var world = WorldSession.CreateDungeonRun(42); var run = world.State.Dungeon!; var hero = world.Party.Single();
        var original = run.Floors[0];
        for (int stage = 1; stage <= 3; stage++)
        {
            Assert.Empty(world.AvailableDungeonRoutes(hero.Id));
            string before = JsonSerializer.Serialize(world.State);
            Assert.False(world.SubmitDungeon(new(TacticalAction.DungeonRoute, Choice: 1)).Success);
            Assert.Equal(before, JsonSerializer.Serialize(world.State));
            var last = run.Floors.Last(); DefeatBoss(world);
            var reward = Assert.Single(world.DungeonCombat.State.Chests.Chests, c => c.Marker == "elite:stage:" + stage);
            Assert.Equal("core:chest/free_elite", reward.Definition); Assert.NotEqual(last.Exit, reward.Position);
            hero.Position = world.State.Position = last.Exit; world.Reveal();
            var routes = world.AvailableDungeonRoutes(hero.Id);
            Assert.Equal(stage == 3 ? 1 : 3, routes.Count);
            int choice = stage == 2 ? 2 : 1;
            string id = routes[choice - 1].Id; long clock = world.DungeonCombat.Clock;
            long recovery = world.DungeonCombat.Preview(new(TacticalAction.DungeonRoute, Choice: choice)).Recovery;
            Assert.True(world.SubmitDungeon(new(TacticalAction.DungeonRoute, Choice: choice)).Success);
            Assert.Equal(clock + recovery, world.DungeonCombat.Clock); Assert.Equal(last.Exit, hero.Position);
            Assert.Equal(id, run.RouteHistory[^1]); Assert.Equal(stage + 1, run.Stage);
            Assert.Empty(world.AvailableDungeonRoutes(hero.Id));
            var entry = run.Floors.Single(f => f.Origin.Floor == last.Origin.Floor + 1).Entry;
            Assert.Equal(entry, world.StairLanding(last.Exit)); Assert.Equal(last.Exit, world.StairLanding(entry));
            Assert.True(world.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
            Assert.Equal(entry, hero.Position);
            Assert.Same(original, run.Floors[0]);
            TacticalValidation.Validate(world.Plan, world.State);
        }
        Assert.Equal(7, run.Floors.Count); Assert.Equal(8, world.Plan.FloorCount);
        var final = run.Floors[^1]; var map = DungeonCatalog.Default.Finale;
        Assert.Equal(23, final.Width); Assert.Equal(17, final.Height);
        for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
        {
            var p = final.Origin.Offset(x, y);
            Assert.Equal(p == final.Entry ? Material.StairsUp : map.Rows[y][x] == '#' ? Material.Wall : Material.Floor, world.Chunks.At(p));
        }
        DefeatBoss(world); Assert.True(run.Victory); Assert.False(world.State.AwaitingContinuation);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
        Assert.Equal(6, hero.Position.Floor); Assert.True(run.Victory);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void NewDeepFloorsRoutesEliteReceiptsAndOldFloorEditsContinueThroughAnActualSave()
    {
        using var world = WorldSession.CreateDungeonRun(-17); var hero = world.Party.Single();
        DefeatBoss(world); hero.Position = world.State.Position = world.State.Dungeon!.Floors[1].Exit; world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.DungeonRoute, Choice: 3)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
        var edited = world.State.Dungeon.Floors[0].Entry.Offset(0, 1); world.Chunks.Edit(edited, Material.Barricade);
        string root = Path.Combine(Path.GetTempPath(), "roag-deep-route-" + Guid.NewGuid().ToString("N"));
        string slot = Path.Combine(root, "campaign.json");
        try
        {
            world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(world.State.Dungeon), JsonSerializer.Serialize(loaded.State.Dungeon));
            Assert.Equal(8, loaded.Plan.FloorCount); Assert.Equal(3, loaded.Party.Single().Position.Floor);
            Assert.Equal(Material.Barricade, loaded.Chunks.At(edited));
            for (int i = 0; i < 3; i++)
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.Equal(JsonSerializer.Serialize(world.State.Tactics), JsonSerializer.Serialize(loaded.State.Tactics));
            }
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
