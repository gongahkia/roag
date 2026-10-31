using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonRewardTests
{
    [Fact]
    public void BlockedEliteRewardSurvivesSaveAndAppearsOnceAfterPaidMovementClearsGround()
    {
        using var world = WorldSession.CreateDungeonRun(42); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var run = world.State.Dungeon!; var stair = run.Floors[0].Exit;
        var boss = world.State.Actors.Single(a => a.Id == run.Floors[1].Boss);
        for (int y = -5; y <= 5; y++) for (int x = -5; x <= 5; x++)
            if (Math.Abs(x) + Math.Abs(y) <= 5) world.Chunks.Edit(stair.Offset(x, y), Material.Wall);
        world.Chunks.Edit(stair, Material.StairsDown); var vacated = stair.Offset(-1, 0); world.Chunks.Edit(vacated, Material.Floor);
        hero.Position = world.State.Position = vacated; boss.Position = stair; world.Reveal();
        combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [stair], "test:blocked-elite",
            AttackDelivery.Area, DamageElement.Physical, 10000, combat.Clock, IgnoresTerrain: true));
        Assert.Contains(boss.Id, run.PendingEliteRewards);
        Assert.DoesNotContain(combat.State.Chests.Chests, c => c.Marker == "elite:stage:1");
        string root = Path.Combine(Path.GetTempPath(), "roag-pending-reward-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Contains(boss.Id, loaded.State.Dungeon!.PendingEliteRewards);
            Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
            Assert.Equal(2, hero.Position.Floor); Assert.Empty(run.PendingEliteRewards);
            Assert.Equal(vacated, Assert.Single(combat.State.Chests.Chests, c => c.Marker == "elite:stage:1").Position);
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(combat.State), System.Text.Json.JsonSerializer.Serialize(loaded.DungeonCombat.State));
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Single(combat.State.Chests.Chests, c => c.Marker == "elite:stage:1");
            Assert.Single(combat.State.Events, e => e.Kind == "EliteRewardPlaced");
            TacticalValidation.Validate(world.Plan, world.State); TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void ChestsCannotOverwriteServicesOptionalFixturesOrAnOwnedSentry()
    {
        using var world = WorldSession.CreateDungeonRun(42, ClassKind.Tinkerer); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var service = hero.Position.Offset(0, 1); var fixture = hero.Position.Offset(-1, 0); var sentry = hero.Position.Offset(1, 0);
        foreach (var p in new[] { service, fixture, sentry })
        { world.Chunks.Edit(p, Material.Floor); world.State.Dungeon!.Terrain.Remove(Roag.Core.Dungeons.DungeonRunState.CellKey(p)); }
        combat.SpawnDungeonService(DungeonServiceKind.ReturnToken, "test:protected-service", service);
        combat.PlaceOptionalTerrain(OptionalTerrainKind.PushFloor, "test:protected-push", fixture);
        world.Reveal(); Assert.True(world.SubmitDungeon(new(TacticalAction.Special, Facing.East)).Success);
        Assert.Equal(sentry, hero.Tactics!.Resources.Sentry!.Position);
        foreach (var p in new[] { service, fixture, sentry })
            Assert.Throws<ArgumentException>(() => combat.SpawnBoonChest("core:chest/free_elite", "test:overlap:" + p.X, p));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ForgedPendingRewardsAndZeroBossReceiptsRejectAsInvalidData()
    {
        using var world = WorldSession.CreateDungeonRun(42); var run = world.State.Dungeon!;
        run.BossExitReceipts.Add(0);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        run.BossExitReceipts.Clear(); run.PendingEliteRewards.Add(world.Party.Single().Id);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        run.PendingEliteRewards.Clear(); TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void PursuedEliteDropsItsRewardOnTheActualFloorWhileItsRouteStaysAtTheArena()
    {
        using var world = WorldSession.CreateDungeonRun(42); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var floor = world.State.Dungeon!.Floors[1]; var boss = world.State.Actors.Single(a => a.Id == floor.Boss);
        var corpse = hero.Position.Offset(2, 0);
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++) world.Chunks.Edit(corpse.Offset(x, y), Material.Floor);
        boss.Position = corpse;
        var hit = new LockedAttack(combat.State.NextAction++, hero.Id, hero.Position, [corpse], "test:pursued-boss",
            AttackDelivery.Area, DamageElement.Physical, 10000, combat.Clock, IgnoresTerrain: true);
        combat.ResolveAttack(hit);
        var reward = Assert.Single(combat.State.Chests.Chests, c => c.Marker == "elite:stage:1");
        Assert.Equal(corpse.Floor, reward.Position.Floor); Assert.Equal(1, corpse.Distance(reward.Position));
        Assert.Equal(corpse, Assert.Single(combat.State.Events, e => e.Kind == "EliteDefeated").Position);
        Assert.Equal(corpse, Assert.Single(combat.State.Loot.Drops, d => d.Kind == GroundLootKind.Health).Position);
        Assert.Empty(world.AvailableDungeonRoutes(hero.Id));
        hero.Position = world.State.Position = floor.Exit; world.Reveal();
        Assert.Equal(3, world.AvailableDungeonRoutes(hero.Id).Count);
        combat.ResolveAttack(hit);
        Assert.Single(combat.State.Chests.Chests, c => c.Marker == "elite:stage:1");
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void DungeonSuppliesUseIndependentSavedRollsAndOneDeathReceiptWithoutDeferredFood()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        var combat = world.DungeonCombat; var hero = world.Party.Single();
        int random = combat.State.RandomState, growth = hero.Tactics!.Build!.RandomState;
        int seed = (int)(WorldRandom.Hash(world.Seed, 0, salt: 9401) % (RoagRandom.Modulus - 1)) + 1;
        var enemies = world.State.Actors.Where(a => a.Tactics?.Enemy != null).ToArray();
        foreach (var enemy in enemies)
        {
            var definition = EnemyCatalog.Default.Get(enemy.Tactics!.DefinitionId);
            bool expected = definition.Ai is EnemyBrainKind.Warden or EnemyBrainKind.BiomeElite
                || definition.Ai is not (EnemyBrainKind.HollowRegent or EnemyBrainKind.Pursuer)
                    && RoagRandom.Derive(seed, "supply:" + enemy.Id) % 100 < 5;
            Assert.Equal(expected, enemy.Tactics.HealthDrop);
        }
        Assert.Equal(random, combat.State.RandomState); Assert.Equal(growth, hero.Tactics.Build.RandomState);
        Assert.DoesNotContain(combat.State.Chests.AvailableBoons, id => id is "core:boon/road_flask" or "core:boon/ration_reserve");
        var boss = enemies.Single(a => a.Tactics!.DefinitionId == "core:enemy/thorn_warden");
        var hit = new LockedAttack(combat.State.NextAction++, hero.Id, hero.Position, [boss.Position], "test:reward",
            AttackDelivery.Area, DamageElement.Physical, 10000, combat.Clock, IgnoresTerrain: true);
        combat.ResolveAttack(hit);
        var supply = Assert.Single(combat.State.Loot.Drops, d => d.Kind == GroundLootKind.Health);
        Assert.Equal(10, supply.Amount); Assert.Equal(boss.Id, supply.DefeatedActor);
        int drops = combat.State.Loot.Drops.Count;
        combat.ResolveAttack(hit);
        Assert.Equal(drops, combat.State.Loot.Drops.Count);
        Assert.DoesNotContain(combat.State.Loot.Drops, d => d.Kind == GroundLootKind.Food);
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
