using Roag.Core.Dungeons;
using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void DungeonTerrainCommandsMatchPinnedOriginalMovementCostsContactAndBarricades()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-terrain.json")));
        int commands = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
            var origin = hero.Position; var input = sample.GetProperty("input");
            foreach (var tile in input.GetProperty("tiles").EnumerateArray())
            {
                var p = origin.Offset(tile[0].GetInt32(), tile[1].GetInt32()); char glyph = tile[2].GetString()![0];
                if (glyph is '#' or 'B') world.Chunks.Edit(p, glyph == '#' ? Material.Wall : Material.Barricade);
                else Overlay(world, p, glyph);
            }
            int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                var action = command.GetString() switch { "move" => TacticalAction.Move, "dash" => TacticalAction.Dash,
                    "basic_attack" => TacticalAction.BasicAttack, _ => TacticalAction.Wait };
                var intent = new TacticalIntent(action, Facing.East); long first = combat.State.NextEvent;
                var expected = sample.GetProperty("expected")[index++]; var preview = combat.Preview(intent);
                Assert.True(preview.Valid, preview.Reason); Assert.Equal(expected.GetProperty("recovery").GetInt64(), preview.Recovery);
                Assert.True(world.SubmitDungeon(intent).Success);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), hero.Health);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), expected.GetProperty("y").GetInt32()), hero.Position);
                var events = combat.State.Events.Where(e => e.Sequence >= first).ToArray();
                foreach (var (key, kind) in new[] { ("triggered", "TerrainTriggered"), ("broken", "TerrainBroken"), ("hits", "DamageTaken") })
                    Assert.Equal(expected.GetProperty(key).GetInt32(), events.Count(e => e.Kind == kind));
                commands++;
            }
            TacticalValidation.Validate(world.Plan, world.State);
        }
        Assert.Equal(11, commands);
    }

    private static WorldSession Floor(ClassKind kind = ClassKind.Adventurer)
    {
        var world = WorldSession.CreateDungeonRun(42, kind);
        var start = world.Party.Single().Position;
        // This fixture replaces a local authored area with an empty arena.
        world.DungeonCombat.State.RoomFeatures.Nodes.RemoveAll(n => n.Position.Floor == start.Floor
            && Math.Abs(n.Position.X - start.X) <= 4 && Math.Abs(n.Position.Y - start.Y) <= 4);
        for (int y = -4; y <= 4; y++) for (int x = -4; x <= 4; x++)
        {
            var p = start.Offset(x, y);
            world.Chunks.Edit(p, Material.Floor);
            world.State.Dungeon!.Terrain.Remove(DungeonRunState.CellKey(p));
        }
        world.Reveal(); return world;
    }
    private static void Overlay(WorldSession world, WorldPosition p, char glyph)
        => world.State.Dungeon!.Terrain[DungeonRunState.CellKey(p)] = glyph;

    private static Actor ReplaceRegular(WorldSession world, string definition)
    {
        var actor = world.State.Actors.First(a => a.Tactics?.Enemy is { Squad: null, OathPartner: 0 }
            && EnemyCatalog.Default.Get(a.Tactics.DefinitionId).Ai is EnemyBrainKind.Guard or EnemyBrainKind.Skitter or EnemyBrainKind.ExpansionRegular);
        world.DungeonCombat.ReplaceUnactedEnemy(actor, definition);
        return actor;
    }

    [Fact]
    public void MistStopsAnActiveWardWithoutStoppingTheAttackProjectile()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var leader = ReplaceRegular(world, "core:enemy/ward_weaver");
        var bodyguard = ReplaceRegular(world, "core:enemy/bodyguard_brute");
        leader.Position = hero.Position.Offset(3, 0); bodyguard.Position = hero.Position.Offset(1, 0);
        var squad = new EnemySquad("test:mist-ward", EnemySquadKind.WardedBodyguards, leader.Id, leader.Position);
        leader.Tactics!.Enemy!.Squad = bodyguard.Tactics!.Enemy!.Squad = squad;
        leader.Tactics.Enemy.WardReady = true;
        void Shoot() => combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [bodyguard.Position],
            "test:projectile", AttackDelivery.Projectile, DamageElement.Physical, 1, combat.Clock));
        int hp = bodyguard.Health;
        Shoot(); Assert.Equal(hp, bodyguard.Health);
        Overlay(world, hero.Position.Offset(2, 0), 'm');
        Shoot(); Assert.Equal(hp - 1, bodyguard.Health);
        world.State.Dungeon!.Terrain.Remove(DungeonRunState.CellKey(hero.Position.Offset(2, 0)));
        Shoot(); Assert.Equal(hp - 1, bodyguard.Health);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void MistRejectsOathCreationAndPermanentlyBreaksAnExistingBond()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var a = ReplaceRegular(world, EnemyOaths.A); var b = ReplaceRegular(world, EnemyOaths.B);
        a.Position = hero.Position.Offset(1, 2); b.Position = hero.Position.Offset(3, 2);
        var mist = hero.Position.Offset(2, 2); Overlay(world, mist, 'm');
        Assert.Throws<ArgumentException>(() => combat.LinkOathTwins(a.Id, b.Id));
        world.State.Dungeon!.Terrain.Remove(DungeonRunState.CellKey(mist));
        combat.LinkOathTwins(a.Id, b.Id);
        TacticalValidation.Validate(world.Plan, world.State);
        Overlay(world, mist, 'm');
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        int hp = b.Health;
        combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [a.Position], "test:mist-oath",
            AttackDelivery.Area, DamageElement.Physical, 4, combat.Clock));
        Assert.Equal(hp, b.Health); Assert.Equal(a.MaxHealth - 4, a.Health);
        Assert.True(a.Tactics!.Enemy!.OathBroken); Assert.True(b.Tactics!.Enemy!.OathBroken);
        world.State.Dungeon.Terrain.Remove(DungeonRunState.CellKey(mist));
        Assert.Throws<ArgumentException>(() => combat.LinkOathTwins(a.Id, b.Id));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData('b')]
    [InlineData('g')]
    [InlineData('i')]
    [InlineData('m')]
    [InlineData('c')]
    public void MothDiveDamagesButCannotLeavePlainGroundEmbersOnSemanticTerrain(char glyph)
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var moth = ReplaceRegular(world, EnemyEmbers.Definition); moth.Position = hero.Position.Offset(3, 0);
        moth.Tactics!.Enemy!.EmberDives = 1;
        Overlay(world, hero.Position, glyph); int hp = hero.Health;
        combat.ResolveAttack(new(combat.State.NextAction++, moth.Id, moth.Position, [hero.Position], EnemyEmbers.Dive,
            AttackDelivery.Area, DamageElement.Fire, 1, combat.Clock, EmberLanding: hero.Position));
        Assert.Equal(hp - 1, hero.Health); Assert.Empty(combat.State.Embers);
        Assert.Equal(hero.Position.Offset(3, 0), moth.Position);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData("silt")]
    [InlineData("ember")]
    [InlineData("mirror")]
    public void SavedPlainGroundEffectsRejectSemanticTerrain(string kind)
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var owner = ReplaceRegular(world, kind switch { "silt" => EnemySilt.Definition, "ember" => EnemyEmbers.Definition, _ => EnemyMirrors.Definition });
        var cell = hero.Position.Offset(1, 0); owner.Position = cell.Offset(1, 0);
        var memory = owner.Tactics!.Enemy!; long action = combat.State.NextAction++;
        if (kind == "silt") { memory.SiltCharges = 1; combat.State.Silt.Add(new(action, owner.Id, cell, combat.Clock + 300)); }
        else if (kind == "ember")
        {
            memory.EmberDives = 1;
            var work = combat.State.Queue.Schedule(ScheduledKind.EmberPulse, combat.Clock + 100, owner.Id, referenceId: action);
            combat.State.Embers.Add(new(action, owner.Id, cell, 2, combat.Clock + 100, combat.Clock + 200, work.Sequence));
        }
        else { memory.MirrorCasts = 1; combat.State.Mirrors.Add(new(action, owner.Id, cell, owner.Position, Facing.East, combat.Clock + 400)); }
        TacticalValidation.Validate(world.Plan, world.State);
        foreach (char glyph in "bgimc")
        {
            Overlay(world, cell, glyph);
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        }
        world.State.Dungeon!.Terrain.Remove(DungeonRunState.CellKey(cell));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void SemanticTerrainAndBrokenBarricadesSurviveAnActualContinuingSave()
    {
        using var world = Floor(); var hero = world.Party.Single();
        var bramble = hero.Position.Offset(0, 1); var barrier = hero.Position.Offset(1, 0);
        Overlay(world, bramble, 'b'); world.Chunks.Edit(barrier, Material.Barricade);
        Assert.True(world.SubmitDungeon(new(TacticalAction.BasicAttack, Facing.East)).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-terrain-" + Guid.NewGuid().ToString("N"));
        string slot = Path.Combine(root, "campaign.json");
        try
        {
            world.Save(slot);
            using var loaded = WorldSession.Load(slot);
            Assert.Equal(Material.Floor, loaded.Chunks.At(barrier));
            Assert.Equal('b', loaded.DungeonTerrainAt(bramble));
            Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.South)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Move, Facing.South)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State.Tactics), JsonSerializer.Serialize(loaded.State.Tactics));
            Assert.Equal(hero.Health, loaded.Party.Single().Health);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void DashAcrossBrambleBedsTakesOneSourceLessHitAndArmorOrWardCanBlockIt()
    {
        foreach (int defense in new[] { 0, 1, 2 })
        {
            using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
            int hp = hero.Health;
            Overlay(world, hero.Position.Offset(1, 0), 'b'); Overlay(world, hero.Position.Offset(2, 0), 'b');
            if (defense == 1) hero.Tactics!.Resources.StormWard = true;
            if (defense == 2)
            {
                var build = hero.Tactics!.Build!;
                hero.Tactics.Build = build with { Level = 2, Experience = 5, Stats = build.Stats with { Defense = 1 } };
            }
            Assert.True(combat.Commit(new(TacticalAction.Dash, Facing.East)).Success);
            Assert.Equal(defense == 0 ? hp - 1 : hp, hero.Health);
            Assert.Single(combat.State.Events, e => e.Kind == "TerrainTriggered");
            var hit = Assert.Single(combat.State.Events, e => e.Kind is "DamageTaken" or "DamageBlocked");
            Assert.Equal(0, hit.Source); Assert.Equal("core:terrain/bramble_bed", hit.Ability);
            if (defense == 1) Assert.False(hero.Tactics!.Resources.StormWard);
            TacticalValidation.Validate(world.Plan, world.State);
        }
    }

    [Fact]
    public void BogCostsAndIceReachAreVisibleBeforeCommitAndUseEveryCrossedTile()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        Overlay(world, hero.Position.Offset(1, 0), 'i'); Overlay(world, hero.Position.Offset(2, 0), 'g');
        Overlay(world, hero.Position.Offset(3, 0), 'g');
        var dash = combat.Preview(new(TacticalAction.Dash, Facing.East));
        Assert.Equal(3, dash.Cells.Count);
        Assert.Equal(ActionTiming.Recovery(ClassCatalog.Get(ClassKind.Adventurer).Costs.Dash + 40, hero.Speed), dash.Recovery);
        Assert.True(combat.Commit(new(TacticalAction.Dash, Facing.East)).Success);
        Assert.Equal(dash.Cells[^1], hero.Position);
        combat.AdvanceToPartyDecision();
        var walk = combat.Preview(new(TacticalAction.Move, Facing.West));
        Assert.Equal(ActionTiming.Recovery(ClassCatalog.Get(ClassKind.Adventurer).Costs.Move + 40, hero.Speed), walk.Recovery);
    }

    [Fact]
    public void MistBlocksPrivateSightButDoesNotStopProjectilesOrMovement()
    {
        using var world = Floor(ClassKind.Ranger); var hero = world.Party.Single();
        for (int y = -4; y <= 4; y++) Overlay(world, hero.Position.Offset(1, y), 'm');
        world.Reveal();
        Assert.Contains(hero.Position.Offset(1, 0), world.Visible);
        Assert.DoesNotContain(hero.Position.Offset(2, 0), world.Visible);
        Assert.True(world.CanEnter(hero.Position.Offset(1, 0), hero.Id));
        Assert.Contains(hero.Position.Offset(2, 0), world.DungeonCombat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Cells);
    }

    [Fact]
    public void LightningUsesFirstCrystalAndNearestOpposingLivingRecipientWithoutRecursiveArcs()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var enemies = world.State.Actors.Where(a => a.Tactics?.Enemy != null).Take(3).ToArray();
        for (int i = 0; i < 3; i++)
        {
            enemies[i].Position = hero.Position.Offset(2 + i % 2, i / 2);
            var tactics = enemies[i].Tactics!;
            tactics.InnateStats = tactics.InnateStats! with { Defense = 0 };
        }
        Overlay(world, hero.Position.Offset(2, -1), 'c'); Overlay(world, hero.Position.Offset(3, 0), 'c');
        int[] hp = enemies.Select(a => a.Health).ToArray();
        combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [enemies[0].Position], "test:lightning",
            AttackDelivery.Area, DamageElement.Lightning, 1, combat.Clock));
        Assert.Equal(hp[0] - 1, enemies[0].Health); Assert.Equal(hp[1] - 1, enemies[1].Health);
        Assert.Equal(hp[2], enemies[2].Health);
        var arc = Assert.Single(combat.State.Events, e => e.Kind == "TerrainTriggered");
        Assert.Equal(hero.Position.Offset(2, -1), arc.Position); Assert.Equal(enemies[1].Id, arc.Target);
        Assert.Single(combat.State.Events, e => e.Ability == "core:terrain/crystal_arc" && e.Kind == "DamageTaken");
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void SourceLessHazardsCanDownAndKillWithoutInventingAnActorOrDuplicatingRewards()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        int actors = world.State.Actors.Count;
        var hit = new LockedAttack(combat.State.NextAction++, 0, hero.Position, [hero.Position], "test:environment",
            AttackDelivery.Area, DamageElement.Bramble, 1000, combat.Clock, DamageCause.GroundHazard);
        combat.ResolveAttack(hit); Assert.Equal(LifeState.Downed, hero.Life);
        for (int i = 0; i < 5; i++) combat.ResolveAttack(hit with { Damage = 1 });
        Assert.Equal(LifeState.Dead, hero.Life); Assert.True(world.State.AwaitingContinuation);
        Assert.Equal(actors, world.State.Actors.Count); Assert.Empty(combat.State.Loot.Drops);
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
