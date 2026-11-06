using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Profiles;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class GeneratedCampaignTests
{
    [Fact]
    public void SeededSelectionIncludesAllFiveFixedRolesAndAcceptsTheWholeWorldSeedDomain()
    {
        var roles = Enumerable.Range(-100, 201).Select(GeneratedCampaignFinale.SelectRole).ToHashSet();
        Assert.Equal(Enum.GetValues<CampaignFinalRole>().Order(), roles.Order());
        foreach (int seed in new[] { int.MinValue, -1, 0, 1, int.MaxValue })
            Assert.Equal(GeneratedCampaignFinale.SelectRole(seed), GeneratedCampaignFinale.SelectRole(seed));
        var normal = EnemyCatalog.Default.Get("core:enemy/hollow_regent");
        var final = GeneratedCampaignFinale.FinalRegentDefinition;
        Assert.Equal(50, EnemyCatalog.Default.Definitions.Length); Assert.Equal(62, normal.Hp);
        Assert.Equal(normal.Id, final.Id); Assert.Equal(normal.Ai, final.Ai);
        Assert.True(final.Hp > normal.Hp && final.Damage > normal.Damage && final.Speed > normal.Speed);
        Assert.True(final.Windup < normal.Windup && final.Recovery < normal.Recovery);
    }

    [Theory]
    [InlineData(3, CampaignFinalRole.HollowRegent)]
    [InlineData(42, CampaignFinalRole.OssuaryCaptain)]
    public void ActualGeneratedWorldPreservesRolesPathsExactContinuationAndOneTimeOptionalOrderVictory(int seed, CampaignFinalRole role)
    {
        // Actual generation and canonical damage fixtures verify backend identity,
        // settlement and continuation; these are not rendered natural combat.
        using var world = WorldSession.CreateOpenCampaign(seed);
        Assert.Equal(role, world.FinalCampaignRole); Assert.Equal(world.FinalBoss.Id, world.CampaignFinale!.FinalActor);
        Assert.Equal(4, world.GeneratedBossSites.Count);
        Assert.All(world.State.NativeCampaign!.Complexes!, c => Assert.Empty(c.Run.BossExitReceipts));
        foreach (var site in world.GeneratedBossSites)
        {
            var arena = world.State.Tactics!.CampaignBosses!.Arenas.Single(a => a.Id == site.Arena);
            var landing = world.StairLanding(site.Entrance) ?? throw new InvalidOperationException("The generated boss site lacks paired stairs.");
            Assert.False(world.Chunks.Seen(site.Entrance));
            Assert.True(PhysicallyConnected(world, landing, arena.Center.Offset(0, 6), arena.Center));
            Assert.Equal(site.Kind == GeneratedCampaignFinale.BossKind(role), arena.FinalRole);
        }
        var root = Path.Combine(Path.GetTempPath(), "roag-generated-final-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); string path = Path.Combine(root, "world.json");
        try
        {
            world.EnablePermanentProfile(path, Path.Combine(root, "profile"));
            world.Save(path); string original = File.ReadAllText(path);
            using (var loaded = WorldSession.Load(path, profileDirectory: Path.Combine(root, "profile")))
            {
                Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
                loaded.Save(path); Assert.Equal(original, File.ReadAllText(path));
                Assert.Equal(world.FinalCampaignRole, loaded.FinalCampaignRole);
            }
            AssertInvalidBinding(world);
            var other = world.State.Tactics!.CampaignBosses!.Arenas.First(a => a.Boss != world.FinalBoss.Id);
            Strike(world, world.ActorById(other.Boss));
            Assert.False(world.State.Endgame.Victory); Assert.Null(world.CampaignFinale.VictoryAt);
            Assert.Single(world.State.Tactics.Events, e => e.Kind == "EntityKilled" && e.Target == other.Boss);
            // No other major boss is a prerequisite or is changed by this death.
            Assert.All(world.State.NativeCampaign.Complexes!, c => Assert.Empty(c.Run.BossExitReceipts));
            var final = world.FinalBoss; int hp = final.Health; var stats = final.Tactics!.InnateStats;
            Strike(world, world.ActorById(other.Boss));
            Assert.Equal(hp, final.Health); Assert.Equal(stats, final.Tactics.InnateStats);
            if (role == CampaignFinalRole.HollowRegent)
            {
                Assert.Equal(124, final.MaxHealth); Assert.Equal(8, final.Power);
                Assert.All(world.DungeonFloors.Where(f => f.Stage == 4 && f.Boss != final.Id), f => Assert.Equal(62, world.ActorById(f.Boss).MaxHealth));
            }
            Strike(world, final);
            Assert.Equal(LifeState.Dead, final.Life); Assert.True(world.State.Endgame.Victory);
            Assert.NotNull(world.CampaignFinale.VictoryAt); Assert.Equal(world.DungeonMember.Id, world.CampaignFinale.Victor);
            Assert.Single(world.State.Tactics.Events, e => e.Kind == "CampaignVictory" && e.Target == final.Id);
            Assert.False(world.ExpeditionEndedByVictory); Assert.Contains("exploration", world.FinalObjective);
            var receipt = world.CampaignFinale.VictoryAt;
            Strike(world, final); Assert.Equal(receipt, world.CampaignFinale.VictoryAt);
            Assert.Single(world.State.Tactics.Events, e => e.Kind == "CampaignVictory");
            world.Save(path);
            using var victorious = WorldSession.Load(path, profileDirectory: Path.Combine(root, "profile"));
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(victorious.State));
            long clock = world.DungeonCombat.Clock;
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(victorious.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(world.DungeonCombat.Clock > clock);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(victorious.State));
            Assert.Equal(receipt, victorious.CampaignFinale!.VictoryAt);
            Assert.Equal(role == CampaignFinalRole.HollowRegent, world.State.Tactics.Profile!.Earned.Contains(AchievementCatalog.Prefix + "regents_fall"));
            TacticalValidation.Validate(world.Plan, world.State);
            // Reentry/loading and late factory calls cannot repaint or reset.
            string before = JsonSerializer.Serialize(world.State);
            Assert.Throws<InvalidOperationException>(world.PopulateGeneratedCampaign);
            Assert.Equal(before, JsonSerializer.Serialize(world.State));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void EarlierAbsentFinaleStateLoadsAndResavesWithoutAddingGenerationOrChangingTheObjective()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        string root = Path.Combine(Path.GetTempPath(), "roag-pre-final-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); string path = Path.Combine(root, "world.json");
        try
        {
            Assert.Null(world.CampaignFinale); world.Save(path); string before = File.ReadAllText(path);
            Assert.DoesNotContain("\"finale\":", before);
            using var loaded = WorldSession.Load(path); Assert.Null(loaded.CampaignFinale);
            Assert.Equal(world.FinalObjective, loaded.FinalObjective);
            loaded.Save(path); Assert.Equal(before, File.ReadAllText(path));
        }
        finally { Directory.Delete(root, true); }
    }

    private static void AssertInvalidBinding(WorldSession world)
    {
        var finale = world.CampaignFinale!; long selected = finale.FinalActor;
        finale.FinalActor = world.DungeonMember.Id;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State)); finale.FinalActor = selected;
        var role = finale.Role; finale.Role = (CampaignFinalRole)(((int)role + 1) % 5);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State)); finale.Role = role;
        var home = finale.Guardians[0]; finale.Guardians[0] = home with { SiteId = (home.SiteId + 1) % 48 };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State)); finale.Guardians[0] = home;
        world.State.Endgame.Victory = true;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State)); world.State.Endgame.Victory = false;
        var cache = world.State.Loot.First(c => c.Id == "site-0-0"); world.State.Loot.Remove(cache);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State)); world.State.Loot.Add(cache);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    private static void Strike(WorldSession world, Actor target)
        => world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, world.DungeonMember.Id, world.DungeonMember.Position,
            [target.Position], "test:generated-campaign-death", AttackDelivery.Area, DamageElement.Physical, 100000, world.DungeonCombat.Clock));

    private static bool PhysicallyConnected(WorldSession world, WorldPosition start, WorldPosition target, WorldPosition center)
    {
        var queue = new Queue<WorldPosition>(); var seen = new HashSet<WorldPosition> { start }; queue.Enqueue(start);
        while (queue.TryDequeue(out var cell))
        {
            if (cell == target) return true;
            foreach (var (dx, dy) in WorldSession.Directions)
            {
                var next = cell.Offset(dx, dy);
                if (Math.Abs(next.X - center.X) > 48 || Math.Abs(next.Y - center.Y) > 48 || !seen.Add(next)
                    || world.Chunks.At(next) is Material.StairsDown || !world.GroundOpen(next)) continue;
                queue.Enqueue(next);
            }
        }
        return false;
    }
}
