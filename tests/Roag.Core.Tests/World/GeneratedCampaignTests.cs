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

    [Fact]
    public void PresentFinaleCannotSilentlyDefaultAnyRequiredGenerationReceiptField()
    {
        foreach (string field in new[] { "Version", "Role", "FinalActor", "FinalSite", "RegentComplex", "BossSites", "Guardians" })
        {
            var json = JsonSerializer.SerializeToNode(new GeneratedCampaignFinale())!.AsObject(); json.Remove(field);
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<GeneratedCampaignFinale>(json.ToJsonString()));
        }
    }

    [Theory]
    [InlineData(3, CampaignFinalRole.HollowRegent)]
    [InlineData(42, CampaignFinalRole.OssuaryCaptain)]
    [InlineData(1, CampaignFinalRole.Bellkeeper)]
    [InlineData(10, CampaignFinalRole.Threadmother)]
    [InlineData(13, CampaignFinalRole.SluiceMaw)]
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
            if (role == CampaignFinalRole.HollowRegent) AssertAllDownDoesNotRecordVictory(world, Path.Combine(root, "all-down.json"));
            world.EnablePermanentProfile(path, Path.Combine(root, "profile"));
            world.Save(path); string original = File.ReadAllText(path);
            using (var loaded = WorldSession.Load(path, profileDirectory: Path.Combine(root, "profile")))
            {
                Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
                loaded.Save(path); Assert.Equal(original, File.ReadAllText(path));
                Assert.Equal(world.FinalCampaignRole, loaded.FinalCampaignRole);
            }
            AssertInvalidBinding(world);
            AssertCohostedRewards(world);
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
            Roag.Core.Tests.Client.GeneratedCampaignInputTests.AssertContinuedPaidInput(victorious);
            if (role == CampaignFinalRole.OssuaryCaptain) AssertContinuedCrafting(victorious);
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

    [Fact]
    public void StrongerRegentRetainsItsOriginalPhaseAndExactLockedTimingWithoutAnyEarlierMajorDeath()
    {
        using var world = WorldSession.CreateOpenCampaign(3);
        var boss = world.FinalBoss; var hero = world.DungeonMember;
        Assert.Equal(CampaignFinalRole.HollowRegent, world.FinalCampaignRole);
        hero.Position = world.State.Position = Enumerable.Range(1, 5).Select(d => boss.Position.Offset(-d, 0))
            .First(p => world.CanEnter(p, hero.Id));
        hero.Tactics!.AutoEat = false; world.Reveal();
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, hero.Id, hero.Position, [boss.Position],
            "test:final-regent-phase", AttackDelivery.Area, DamageElement.Physical, 64, world.DungeonCombat.Clock));
        Assert.True(boss.Tactics!.Enemy!.PhaseTwo); Assert.Equal(125, boss.Speed);
        Assert.All(world.State.NativeCampaign!.Complexes!, c => Assert.Empty(c.Run.BossExitReceipts));
        Assert.True(world.DungeonCombat.Commit(new(TacticalAction.Wait)).Success);
        bool committed = false;
        for (int i = 0; i < 10000; i++)
        {
            var active = world.DungeonCombat.AdvanceToDecision(); Assert.NotNull(active);
            if (active.Id == boss.Id) { world.DungeonCombat.CommitWorldEnemyTurn(); committed = true; break; }
            if (active.Tactics!.Enemy != null) world.DungeonCombat.CommitWorldEnemyTurn();
            else Assert.True(world.DungeonCombat.Commit(new(TacticalAction.Wait)).Success);
        }
        Assert.True(committed);
        var locked = Assert.Single(world.State.Tactics.Attacks, a => a.Source == boss.Id);
        Assert.StartsWith("core:ability/regent_", locked.Ability);
        Assert.Equal(ActionTiming.Recovery(90, boss.Speed), locked.Due - world.DungeonCombat.Clock);
        Assert.Equal(115, locked.RecoveryCost);
        string root = Path.Combine(Path.GetTempPath(), "roag-final-regent-lock-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); string path = Path.Combine(root, "world.json");
        try
        {
            world.Save(path); using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            var copy = loaded.State.Tactics!.Attacks.Single(a => a.Source == boss.Id);
            Assert.Equal(JsonSerializer.Serialize(locked), JsonSerializer.Serialize(copy));
            world.DungeonCombat.AdvanceToPartyDecision(); loaded.DungeonCombat.AdvanceToPartyDecision();
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            for (int i = 0; i < 8 && world.State.Tactics.Attacks.Any(a => a.Id == locked.Id); i++)
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            }
            Assert.DoesNotContain(world.State.Tactics.Attacks, a => a.Id == locked.Id);
            Assert.Contains(world.State.Tactics.Events, e => e.Kind == "AttackPerformed" && e.Action == locked.Id);
            Assert.False(world.State.Endgame.Victory);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(42)]
    public void GlobalFinalDestinationNeverTracksAnUnseenBossPaidMoveOrItsSavedLivePosition(int seed)
    {
        using var world = WorldSession.CreateOpenCampaign(seed);
        var boss = world.FinalBoss; string objective = world.FinalObjective;
        var destination = world.FinalCampaignPosition; var original = boss.Position;
        Assert.DoesNotContain(boss.Position, world.Visible);
        Assert.True(world.DungeonCombat.Commit(new(TacticalAction.Wait)).Success);
        bool moved = false;
        for (int i = 0; i < 10000; i++)
        {
            var active = world.DungeonCombat.AdvanceToDecision(); Assert.NotNull(active);
            if (active.Id == boss.Id)
            {
                var direction = new[] { Facing.North, Facing.East, Facing.South, Facing.West }.First(f =>
                {
                    var (x, y) = AttackGeometry.Offset(f); return world.CanEnter(boss.Position.Offset(x, y), boss.Id);
                });
                Assert.True(world.DungeonCombat.Commit(new(TacticalAction.Move, direction)).Success);
                Assert.True(world.State.Tactics!.Queue.Snapshot().Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == boss.Id).Due > world.DungeonCombat.Clock);
                moved = true; break;
            }
            if (active.Tactics!.Enemy != null) world.DungeonCombat.CommitWorldEnemyTurn();
            else Assert.True(world.DungeonCombat.Commit(new(TacticalAction.Wait)).Success);
        }
        Assert.True(moved); Assert.NotEqual(original, boss.Position);
        Assert.DoesNotContain(boss.Position, world.Visible);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Equal(destination, world.FinalCampaignPosition); Assert.Equal(objective, world.FinalObjective);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        string root = Path.Combine(Path.GetTempPath(), "roag-final-private-home-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); string path = Path.Combine(root, "world.json");
        try
        {
            world.Save(path); using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(boss.Position, loaded.FinalBoss.Position);
            Assert.Equal(destination, loaded.FinalCampaignPosition); Assert.Equal(objective, loaded.FinalObjective);
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

    private static void AssertCohostedRewards(WorldSession world)
    {
        var firstHome = world.CampaignFinale!.Guardians.First(g => world.Content.Get<BossDefinition>(world.ActorById(g.Actor).Archetype).Minions.Length > 0);
        var homes = world.RetainedGuardiansAt(firstHome.SiteId); Assert.Equal(2, homes.Count);
        Assert.NotEqual(homes[0].Spawn.Floor, homes[1].Spawn.Floor);
        var bosses = homes.Select(h => world.ActorById(h.Actor)).ToArray();
        int secondHp = bosses[1].Health;
        foreach (var boss in bosses)
        {
            var definition = world.Content.Get<BossDefinition>(boss.Archetype);
            var reward = world.Content.Get<RewardDefinition>(definition.Reward);
            var attendants = world.State.Actors.Where(a => a.Id != boss.Id && a.EncounterLeader == boss.Id).ToArray();
            long xp = ExperienceUnits(world.DungeonMember);
            Strike(world, boss);
            Assert.Contains(boss.Id, world.State.RewardedEncounterIds);
            var cache = Assert.Single(world.State.Loot, c => c.Id == "native-encounter-" + boss.Id);
            Assert.Equal(boss.Position, cache.Position); Assert.Equal(reward.Gold, cache.Contents.GetValueOrDefault("gold"));
            Assert.Equal(reward.Food, cache.Contents.GetValueOrDefault("food"));
            Assert.All(attendants, a => Assert.Equal(LifeState.Alive, a.Life));
            Assert.Equal(xp + reward.Experience * ContributionRewards.UnitsPerExperience, ExperienceUnits(world.DungeonMember));
            xp = ExperienceUnits(world.DungeonMember); Strike(world, boss);
            Assert.Equal(xp, ExperienceUnits(world.DungeonMember));
            Assert.Single(world.State.Loot, c => c.Id == cache.Id);
            if (boss.Id == bosses[0].Id) Assert.Equal(secondHp, bosses[1].Health);
        }
        Assert.False(world.State.Endgame.Victory);
    }

    private static long ExperienceUnits(Actor actor)
        => actor.Tactics!.Build!.Experience * ContributionRewards.UnitsPerExperience + actor.Tactics.ExperienceFraction;

    private static void AssertContinuedCrafting(WorldSession world)
    {
        var worker = world.DungeonMember; var victory = world.CampaignFinale!.VictoryAt;
        var station = WorldSession.Directions.Select(d => worker.Position.Offset(d.X, d.Y)).First(p => world.CanEnter(p, worker.Id));
        world.Chunks.Edit(station, Material.Camp); world.Reveal(); // Isolated physical-camp fixture.
        var recipe = world.Content.Get<RecipeDefinition>("recipe.field-medicine");
        var stock = new Dictionary<string, int>(world.State.Inventory); int output = world.Count("medicine");
        var intent = new TacticalIntent(TacticalAction.StartWork, Work: new(WorldCommandKind.Craft, Item: recipe.Id));
        string before = JsonSerializer.Serialize(world.State);
        Assert.True(world.DungeonCombat.Preview(intent).Valid); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.SubmitDungeon(intent).Success);
        foreach (var cost in recipe.Cost)
        {
            string stack = world.Content.Get<ItemDefinition>(cost.Key).Stack;
            Assert.Equal(stock.GetValueOrDefault(stack) - cost.Value, world.Count(stack));
        }
        long started = world.DungeonCombat.Clock;
        for (int i = 0; i < 100 && worker.Tactics!.Activity != null; i++) world.ContinueDungeonActivities(8);
        Assert.Null(worker.Tactics!.Activity); Assert.Empty(world.State.Work);
        Assert.Equal(output + recipe.Amount, world.Count("medicine")); Assert.True(world.DungeonCombat.Clock > started);
        Assert.Equal(victory, world.CampaignFinale.VictoryAt); Assert.True(world.State.Endgame.Victory);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    private static void AssertAllDownDoesNotRecordVictory(WorldSession source, string path)
    {
        source.Save(path); using var world = WorldSession.Load(path);
        var boss = world.FinalBoss; var hero = world.DungeonMember;
        hero.Position = world.State.Position = Enumerable.Range(1, 5).Select(d => boss.Position.Offset(-d, 0))
            .First(p => world.CanEnter(p, hero.Id)); world.Reveal();
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, 0, boss.Position, [hero.Position, boss.Position],
            "test:simultaneous-final-and-last-standing", AttackDelivery.Area, DamageElement.Physical, 100000, world.DungeonCombat.Clock));
        Assert.Equal(LifeState.Downed, hero.Life); Assert.Equal(LifeState.Dead, boss.Life);
        Assert.False(world.State.AwaitingContinuation); Assert.False(world.State.Endgame.Victory);
        Assert.Null(world.CampaignFinale!.VictoryAt);
        Assert.DoesNotContain(world.State.Tactics.Events, e => e.Kind == "CampaignVictory");
        world.Save(path); using var loaded = WorldSession.Load(path);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        Assert.False(loaded.State.Endgame.Victory); Assert.Equal(5, loaded.DungeonMember.Tactics!.BleedTurns);
    }

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
