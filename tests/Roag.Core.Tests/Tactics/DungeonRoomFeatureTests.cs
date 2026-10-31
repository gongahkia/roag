using System.Text.Json;
using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void RoomFeatureCommandsMatchOriginalOreWardFocusAndTimedMist()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-room-features.json")));
        int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            using var world = Floor(input.TryGetProperty("class", out var classId) ? ClassCatalog.Get("core:actor/" + classId.GetString()).Kind : ClassKind.Adventurer);
            var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
            var kind = Enum.Parse<DungeonRoomFeatureKind>(input.GetProperty("kind").GetString()!, true);
            var feature = combat.SpawnRoomFeature(kind, "test:original-feature", origin.Offset(input.TryGetProperty("offset", out var offset) ? offset.GetInt32() : 1, 0));
            world.State.Inventory["gold"] = 0; hero.Tactics!.Supplies.FocusActions = input.TryGetProperty("focus", out var focus) ? focus.GetInt32() : 0;
            if (kind == DungeonRoomFeatureKind.Brazier) Overlay(world, origin.Offset(2, 0), 'm');
            if (input.TryGetProperty("enemy", out _))
            {
                var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard"); enemy.Position = origin.Offset(1, 1);
                combat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 250, enemy.Id);
            }
            if (input.TryGetProperty("sentry", out _))
            {
                var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard"); enemy.Position = origin.Offset(4, 0);
                combat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 100000, enemy.Id);
            }
            combat.State.RandomState = 37; world.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                var action = command.GetString() switch { "move" => TacticalAction.Move, "wait" => TacticalAction.Wait,
                    "sweeping_slash" => TacticalAction.Special, _ => TacticalAction.BasicAttack };
                long first = combat.State.NextEvent; var expected = sample.GetProperty("expected")[index++];
                var intent = new TacticalIntent(action, Facing.East); string before = JsonSerializer.Serialize(world.State);
                Assert.True(combat.Preview(intent).Valid); Assert.Equal(before, JsonSerializer.Serialize(world.State));
                Assert.True(world.SubmitDungeon(intent).Success);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), 0), hero.Position);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), world.Count("gold"));
                Assert.Equal(expected.GetProperty("hp").GetInt32(), hero.Health);
                Assert.Equal(expected.GetProperty("focus").GetInt32(), hero.Tactics.Supplies.FocusActions);
                Assert.Equal(expected.GetProperty("ward").GetInt32(), hero.Tactics.Supplies.WardActions);
                var current = combat.State.RoomFeatures.Nodes.Single(n => n.Id == feature.Id);
                Assert.Equal(expected.GetProperty("used").GetBoolean(), current.Used);
                Assert.Equal(expected.GetProperty("litUntil").GetInt64(), current.LitUntil);
                Assert.Equal(expected.GetProperty("mist").GetBoolean(), world.DungeonMistBlocks(origin.Offset(2, 0)));
                foreach (var (key, name) in new[] { ("broken", "OreBroken"), ("collected", "WardFlaskCollected"), ("lit", "BrazierLit"), ("blocked", "SupplyWardAbsorbed") })
                    Assert.Equal(expected.GetProperty(key).GetInt32(), combat.State.Events.Count(e => e.Sequence >= first && e.Kind == name));
                TacticalValidation.Validate(world.Plan, world.State); count++;
            }
        }
        Assert.Equal(20, count);
    }

    [Fact]
    public void LitBrazierClearsActualPrivateSightAndSupportWithoutChangingUnderlyingMist()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
        var fire = combat.SpawnRoomFeature(DungeonRoomFeatureKind.Brazier, "test:fire", origin.Offset(1, 0));
        var ally = ReplaceRegular(world, "core:enemy/bodyguard_brute"); var leader = ReplaceRegular(world, "core:enemy/ward_weaver");
        ally.Tactics!.InnateStats = ally.Tactics.InnateStats! with { Defense = 0 };
        ally.Position = origin.Offset(1, -1); leader.Position = origin.Offset(3, -1);
        var mist = origin.Offset(2, -1); Overlay(world, mist, 'm');
        for (int y = -4; y <= 4; y++) Overlay(world, origin.Offset(2, y), 'm');
        Assert.DoesNotContain(leader.Position, world.VisionFrom(origin));
        var squad = new EnemySquad("test:brazier-ward", EnemySquadKind.WardedBodyguards, leader.Id, leader.Position);
        ally.Tactics!.Enemy!.Squad = leader.Tactics!.Enemy!.Squad = squad; leader.Tactics.Enemy.WardReady = true;
        foreach (var enemy in new[] { ally, leader })
        { combat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 10000, enemy.Id); }
        void Hit() => combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [ally.Position], "test:mist-support",
            AttackDelivery.Projectile, DamageElement.Physical, 1, combat.Clock));
        int hp = ally.Health; Hit(); Assert.Equal(hp - 1, ally.Health);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal('m', world.DungeonTerrainAt(mist)); Assert.False(world.DungeonMistBlocks(mist));
        Assert.Contains(leader.Position, world.VisionFrom(origin));
        Hit(); Assert.Equal(hp - 1, ally.Health);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.True(world.DungeonMistBlocks(mist));
        Hit(); Assert.Equal(hp - 2, ally.Health); TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData(DungeonRoomFeatureKind.Ore)]
    [InlineData(DungeonRoomFeatureKind.Brazier)]
    [InlineData(DungeonRoomFeatureKind.Ward)]
    public void RoomFeatureReceiptsAndLitDurationContinueThroughActualSaves(DungeonRoomFeatureKind kind)
    {
        using var world = Floor(); var combat = world.DungeonCombat; var hero = world.Party.Single();
        var node = combat.SpawnRoomFeature(kind, "test:saved-feature", hero.Position.Offset(1, 0));
        var action = kind == DungeonRoomFeatureKind.Ore ? TacticalAction.BasicAttack : TacticalAction.Move;
        Assert.True(world.SubmitDungeon(new(action, Facing.East)).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-room-feature-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            for (int i = 0; i < 3; i++)
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            }
            Assert.Equal(node.Id, loaded.DungeonCombat.State.RoomFeatures.Nodes.Single(n => n.Marker == node.Marker).Id);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void PillarHasNoActorOrHealthAndCannotSealAReachablePassage()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
        int actors = world.State.Actors.Count;
        var pillar = combat.SpawnRoomFeature(DungeonRoomFeatureKind.Pillar, "test:pillar", origin.Offset(1, 0)); world.Reveal();
        Assert.False(world.CanEnter(pillar.Position)); Assert.True(Materials.Opaque(world.Chunks.At(pillar.Position)));
        Assert.Equal(actors, world.State.Actors.Count); Assert.Null(world.OccupantAt(pillar.Position));
        Assert.True(world.SubmitDungeon(new(TacticalAction.BasicAttack, Facing.East)).Success);
        Assert.Equal(Material.Pillar, world.Chunks.At(pillar.Position)); Assert.False(combat.State.RoomFeatures.Nodes.Single(n => n.Id == pillar.Id).Used);
        Assert.Contains("no health", WorldInspection.Inspect(world, pillar.Position).Detail);
        var choke = origin.Offset(0, 2); world.Chunks.Edit(choke.Offset(-1, 0), Material.Wall); world.Chunks.Edit(choke.Offset(1, 0), Material.Wall);
        world.Chunks.Edit(choke.Offset(0, 2), Material.Wall); world.Chunks.Edit(choke.Offset(-1, 1), Material.Wall); world.Chunks.Edit(choke.Offset(1, 1), Material.Wall);
        Assert.False(combat.CanPlaceRoomFeature(DungeonRoomFeatureKind.Pillar, "test:sealed", choke));
        world.Chunks.Edit(pillar.Position, Material.Floor);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
    }

    [Fact]
    public void LastWardActionProtectsItsCommittedRecoveryAndSurvivesSavingBeforeTheImpact()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        hero.Tactics!.Supplies.WardActions = 1;
        var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard"); enemy.Position = hero.Position.Offset(1, 0);
        combat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 0, enemy.Id);
        Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
        Assert.Equal(0, hero.Tactics.Supplies.WardActions); Assert.True(hero.Tactics.Supplies.WardThroughRecovery);
        Assert.Same(enemy, combat.AdvanceToDecision());
        combat.CommitWindup("core:ability/basic_attack", [hero.Position], AttackDelivery.Melee, DamageElement.Physical, 3, 50, 100, intendedTarget: hero.Id);
        string root = Path.Combine(Path.GetTempPath(), "roag-ward-recovery-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.True(loaded.Party.Single().Tactics!.Supplies.WardThroughRecovery);
            int hp = hero.Health; combat.AdvanceToPartyDecision(); loaded.DungeonCombat.AdvanceToPartyDecision();
            Assert.Equal(hp, hero.Health); Assert.False(hero.Tactics.Supplies.WardThroughRecovery);
            Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            Assert.Single(combat.State.Events, e => e.Kind == "SupplyWardAbsorbed");
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
