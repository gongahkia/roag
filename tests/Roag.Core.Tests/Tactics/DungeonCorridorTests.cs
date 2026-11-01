using System.Text.Json;
using Roag.Client;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    private static DungeonBranchKind BranchKind(string name) => name switch
    {
        "spur" => DungeonBranchKind.Spur, "braid" => DungeonBranchKind.Braid, "switchback" => DungeonBranchKind.Switchback,
        "hub" => DungeonBranchKind.Hub, "false_end" => DungeonBranchKind.FalseEnd, _ => throw new ArgumentException(name)
    };

    [Fact]
    public void CorridorBranchGeometryMatchesAllFortyPinnedOriginalOrientations()
    {
        using var fixture = CorridorFixture(); int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("geometry").EnumerateArray())
        {
            var branch = DungeonLayoutGenerator.DesignBranch(BranchKind(sample.GetProperty("kind").GetString()!), new(20, 20),
                sample.GetProperty("dx").GetInt32(), sample.GetProperty("dy").GetInt32(), sample.GetProperty("side").GetInt32());
            Assert.Equal(sample.GetProperty("cells").EnumerateArray().Select(p => new RoomPoint(p[0].GetInt32(), p[1].GetInt32())), branch.Cells);
            Assert.Equal(sample.GetProperty("nodes").EnumerateArray().Select(n => (n.GetProperty("kind").GetString()!, n.GetProperty("x").GetInt32(), n.GetProperty("y").GetInt32())),
                branch.Nodes.Select(n => (n.Kind.ToString().ToLowerInvariant(), n.Position.X, n.Position.Y))); count++;
        }
        Assert.Equal(40, count);
    }

    private static JsonDocument CorridorFixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-corridors.json")));

    [Fact]
    public void CorridorPathsMatchNinetyEightOriginalBiomeAndSeedCases()
    {
        using var fixture = CorridorFixture(); int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("paths").EnumerateArray())
        {
            bool horizontal = sample.GetProperty("horizontal").GetBoolean(); var random = new RoagRandom(sample.GetProperty("seed").GetInt32());
            var cells = DungeonLayoutGenerator.CorridorPath(new(10, 12), horizontal ? new(40, 20) : new(19, 44), horizontal, sample.GetProperty("strategy").GetString()!, random);
            Assert.Equal(sample.GetProperty("cells").EnumerateArray().Select(p => new RoomPoint(p[0].GetInt32(), p[1].GetInt32())), cells);
            Assert.Equal(sample.GetProperty("random").GetInt32(), random.State); count++;
        }
        Assert.Equal(98, count);
    }

    private static DungeonCorridorQuest TestQuest(WorldSession world, DungeonBranchKind kind)
    {
        var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var branch = DungeonLayoutGenerator.DesignBranch(kind, new(-3, -1), 1, 0, 1);
        long target = 0;
        if (kind == DungeonBranchKind.FalseEnd)
        {
            var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard");
            enemy.Position = hero.Position.Offset(2, 2); enemy.Health = 1;
            combat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 100000, enemy.Id); target = enemy.Id;
        }
        return combat.SpawnCorridorQuest("test:quest", branch, hero.Position, target);
    }

    [Fact]
    public void CorridorQuestsMatchPinnedOriginalPaidCommandsUnlockOrderAndOneTimeGold()
    {
        using var fixture = CorridorFixture(); int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); var kind = BranchKind(input.GetProperty("kind").GetString()!);
            using var world = Floor(input.TryGetProperty("class", out var c) ? ClassCatalog.Get("core:actor/" + c.GetString()).Kind : ClassKind.Adventurer);
            var combat = world.DungeonCombat; var hero = world.Party.Single(); var origin = hero.Position;
            var quest = TestQuest(world, kind); combat.State.RandomState = 37;
            hero.Tactics!.Build = CharacterBuild.Create(hero.Tactics.Build!.Class, 37); world.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                AttackGeometry.TryFacing(command[1].GetInt32(), command[2].GetInt32(), out var facing);
                var intent = new TacticalIntent(command[0].GetString() == "move" ? TacticalAction.Move : TacticalAction.BasicAttack, facing);
                var expected = sample.GetProperty("expected")[index++]; long first = combat.State.NextEvent;
                string before = JsonSerializer.Serialize(world.State); var preview = combat.Preview(intent);
                Assert.Equal(before, JsonSerializer.Serialize(world.State));
                Assert.Equal(expected.GetProperty("valid").GetBoolean(), preview.Valid);
                Assert.Equal(expected.GetProperty("recovery").GetInt64(), preview.Valid ? preview.Recovery : 0);
                Assert.Equal(preview.Valid, world.SubmitDungeon(intent).Success);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), expected.GetProperty("y").GetInt32()), hero.Position);
                quest = combat.State.Quests.Nodes.Single(q => q.Id == quest.Id);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), world.State.Inventory["gold"]);
                Assert.Equal(expected.GetProperty("ready").GetBoolean(), quest.Ready);
                Assert.Equal(expected.GetProperty("collected").GetBoolean(), quest.Completed);
                Assert.Equal(expected.GetProperty("gate").GetBoolean(), quest.Nodes.Any(n => n.Kind == DungeonBranchNodeKind.Gate) && quest.Opened);
                Assert.Equal(expected.GetProperty("active").EnumerateArray().Select(p => p.GetBoolean()), quest.Nodes.Where(n => n.Kind is not (DungeonBranchNodeKind.Gate or DungeonBranchNodeKind.Enemy)).Select(n => n.Activated));
                Assert.Equal(expected.GetProperty("trace").EnumerateArray().Select(p => p.GetString()),
                    combat.State.Events.Where(e => e.Sequence >= first && (e.Kind.StartsWith("Sidequest", StringComparison.Ordinal) || e.Kind == "CurrencyCollected")).Select(e => e.Kind));
                // Original growth drew the shared stream. Native members own their
                // progression stream; the false-end kill's one growth draw belongs there.
                Assert.Equal(37, combat.State.RandomState);
                Assert.Equal(expected.GetProperty("random").GetInt32(), hero.Tactics.Build!.RandomState);
                TacticalValidation.Validate(world.Plan, world.State); count++;
            }
        }
        Assert.Equal(78, count);
    }

    [Theory]
    [InlineData(DungeonBranchKind.Spur)]
    [InlineData(DungeonBranchKind.Braid)]
    [InlineData(DungeonBranchKind.Switchback)]
    [InlineData(DungeonBranchKind.Hub)]
    [InlineData(DungeonBranchKind.FalseEnd)]
    public void CorridorProgressAndPhysicalGatesContinueThroughActualSaves(DungeonBranchKind kind)
    {
        using var world = Floor(); var quest = TestQuest(world, kind); var hero = world.Party.Single();
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.South)).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-quest-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(world.State.Tactics), JsonSerializer.Serialize(loaded.State.Tactics));
            foreach (var facing in new[] { Facing.South, Facing.West, Facing.North, Facing.East })
            {
                var intent = new TacticalIntent(TacticalAction.Move, facing);
                Assert.Equal(world.SubmitDungeon(intent).Success, loaded.SubmitDungeon(intent).Success);
                Assert.Equal(JsonSerializer.Serialize(world.State.Tactics), JsonSerializer.Serialize(loaded.State.Tactics));
            }
            foreach (var node in quest.Nodes) Assert.Equal(world.Chunks.At(node.Position), loaded.Chunks.At(node.Position));
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void CorridorSwitchIgnoresEnemiesAndRememberedInspectionHidesCurrentProgress()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var quest = TestQuest(world, DungeonBranchKind.Switchback); var plate = quest.Nodes.Single(n => n.Kind == DungeonBranchNodeKind.Plate);
        var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard"); enemy.Position = plate.Position.Offset(0, -1);
        combat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 0, enemy.Id);
        Assert.True(combat.Commit(new(TacticalAction.Wait)).Success); Assert.Same(enemy, combat.AdvanceToDecision());
        Assert.True(combat.Commit(new(TacticalAction.Move, Facing.South)).Success); combat.AdvanceToPartyDecision();
        Assert.False(combat.State.Quests.Nodes.Single(q => q.Id == quest.Id).Opened);
        var gate = quest.Nodes.Single(n => n.Kind == DungeonBranchNodeKind.Gate); Assert.Equal(Material.ClosedDoor, world.Chunks.At(gate.Position));
        world.Reveal(); Assert.Contains("Closed", WorldInspection.Inspect(world, gate.Position).Detail);
        world.Visible.Remove(gate.Position); Assert.Contains("remembered", WorldInspection.Inspect(world, gate.Position).Title);
        Assert.DoesNotContain("Closed", WorldInspection.Inspect(world, gate.Position).Detail);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void OffFloorNonPartyKillUnlocksFalseEndWithoutAutomaticallyPayingAnOccupiedReward()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
        var quest = TestQuest(world, DungeonBranchKind.FalseEnd);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.South)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.South)).Success); // clue
        var target = world.ActorById(quest.Target);
        var enemy = world.State.Actors.First(a => a.Id != quest.Target && a.Tactics?.Enemy is { Squad: null, OathPartner: 0 }
            && EnemyCatalog.Default.Get(a.Tactics.DefinitionId).Ai is EnemyBrainKind.Guard or EnemyBrainKind.Skitter or EnemyBrainKind.ExpansionRegular);
        enemy.Position = origin.Offset(3, 2);
        hero.Position = world.State.Position = world.State.Dungeon!.Floors[1].Entry; world.Reveal();
        combat.ResolveAttack(new(combat.State.NextAction++, enemy.Id, enemy.Position, [target.Position], "test:hostile-kill",
            AttackDelivery.Melee, DamageElement.Physical, 1000, combat.Clock));
        quest = combat.State.Quests.Nodes.Single(q => q.Id == quest.Id);
        Assert.True(quest.EnemyDefeated); Assert.True(quest.Ready); Assert.False(quest.Completed);
        Assert.Equal(0, world.State.Inventory["gold"]); Assert.DoesNotContain(target.Position, world.Visible);
        var reward = quest.Nodes.Single(n => n.Kind == DungeonBranchNodeKind.Reward).Position;
        hero.Position = world.State.Position = reward; world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.Equal(0, world.State.Inventory["gold"]);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.North)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.South)).Success); Assert.Equal(5, world.State.Inventory["gold"]);
        Assert.Single(combat.State.Events, e => e.Kind == "SidequestCompleted"); TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void CorridorValidationRejectsForgedProgressAndFixturesCannotReplaceTheBranch()
    {
        using var world = Floor(); var combat = world.DungeonCombat; var quest = TestQuest(world, DungeonBranchKind.FalseEnd);
        int index = combat.State.Quests.Nodes.FindIndex(q => q.Id == quest.Id);
        combat.State.Quests.Nodes[index] = quest with { EnemyDefeated = true };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        combat.State.Quests.Nodes[index] = quest;
        var gate = quest.Nodes.Single(n => n.Kind == DungeonBranchNodeKind.Gate).Position; world.Chunks.Edit(gate, Material.Door);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State)); world.Chunks.Edit(gate, Material.ClosedDoor);
        var plain = quest.Cells.First(p => world.CanEnter(p) && world.ObjectAt(p) == null);
        Assert.False(combat.CanPlaceRoomFeature(DungeonRoomFeatureKind.Pillar, "test:replace", plain));
        Assert.False(combat.CanPlaceOptionalTerrain(OptionalTerrainKind.FragileCrossing, "test:replace", plain));
        Assert.Throws<ArgumentException>(() => combat.SpawnBoonChest("core:chest/standard", "test:replace", plain));
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
