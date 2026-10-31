using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void OptionalTerrainPublicCommandsMatchOriginalWarningTimingPushesAndFiniteShortcuts()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-optional-terrain.json")));
        int commands = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var a = new Arena(ClassKind.Adventurer); var origin = a.Hero.Position; a.Combat.State.RandomState = 37;
            var input = sample.GetProperty("input"); int serial = 0;
            foreach (var node in input.GetProperty("nodes").EnumerateArray())
            {
                var kind = node[0].GetString() switch { "heat_vent" => OptionalTerrainKind.HeatVent,
                    "push_floor" => OptionalTerrainKind.PushFloor, _ => OptionalTerrainKind.FragileCrossing };
                Facing facing = Facing.South;
                if (kind == OptionalTerrainKind.PushFloor) Assert.True(AttackGeometry.TryFacing(node[3].GetInt32(), node[4].GetInt32(), out facing));
                a.Combat.PlaceOptionalTerrain(kind, "fixture:" + serial++, origin.Offset(node[1].GetInt32(), node[2].GetInt32()), facing);
            }
            int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                long first = a.Combat.State.NextEvent; Assert.True(AttackGeometry.TryFacing(command[1].GetInt32(), command[2].GetInt32(), out var facing));
                a.Act(command[0].GetString() switch { "move" => TacticalAction.Move, "dash" => TacticalAction.Dash, _ => TacticalAction.Wait }, facing); a.Next();
                var expected = sample.GetProperty("expected")[index++];
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), expected.GetProperty("y").GetInt32()), a.Hero.Position);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first && e.Kind is "HeatVentWarned" or "HeatVentBurst" or "PushFloorTriggered" or "PushFloorBlocked" or "FragileArmed" or "FragileCollapsed").Select(e => e.Kind);
                Assert.Equal(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()), trace);
                Assert.Equal(expected.GetProperty("nodes").EnumerateArray().Select(n => n.GetProperty("phase").GetString()), a.Combat.State.OptionalTerrain.Nodes.Select(n => n.Phase.ToString().ToLowerInvariant()));
                Assert.Equal(expected.GetProperty("nodes").EnumerateArray().Select(n => n.GetProperty("due").GetInt64()), a.Combat.State.OptionalTerrain.Nodes.Select(n => n.Due));
                TacticalValidation.Validate(a.World.Plan, a.World.State); commands++;
            }
        }
        Assert.Equal(13, commands);
    }

    [Fact]
    public void OptionalVentWarnsOnThePaidActionAndBurstsBeforeReadinessWithoutGuardOrCameraGates()
    {
        using var a = new Arena(ClassKind.Adventurer);
        var vent = a.Combat.PlaceOptionalTerrain(OptionalTerrainKind.HeatVent, "test:vent", new(101, 100));
        Assert.Equal(0, vent.Schedule); Assert.Equal(0, a.Combat.Clock);
        Assert.Equal(1, a.Combat.State.NextAction);
        a.Hero.Tactics!.Guarding = true;
        a.Act(TacticalAction.Move); a.Next();
        var warning = Assert.Single(a.Combat.State.OptionalTerrain.Nodes);
        Assert.Equal(OptionalTerrainPhase.Warning, warning.Phase); Assert.Equal(200, warning.Due);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Contains(a.Combat.VisibleTimeline(), e => e.Kind == ScheduledKind.TerrainCycle);
        Assert.Contains(a.Combat.VisibleAttacks(a.World.Visible), attack => attack.Ability == "core:terrain/heat_vent");
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Act(TacticalAction.Guard); a.Next(); Assert.Equal(22, a.Hero.Health);
        var cooldown = Assert.Single(a.Combat.State.OptionalTerrain.Nodes);
        Assert.Equal(OptionalTerrainPhase.Cooldown, cooldown.Phase); Assert.Equal(500, cooldown.Due);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void OptionalVentContinuesItsPhysicalCycleAndSaveOnAnUnviewedFloor()
    {
        using var a = new Arena(ClassKind.Adventurer);
        var position = new WorldPosition(103, 100, 1); a.Enemy.Position = position;
        a.Combat.PlaceOptionalTerrain(OptionalTerrainKind.HeatVent, "test:offscreen-vent", position.Offset(0, 1));
        a.Act(TacticalAction.Wait); a.Next();
        a.Enemy.Position = position.Offset(0, 1);
        Assert.DoesNotContain(a.Combat.VisibleTimeline(), e => e.Kind == ScheduledKind.TerrainCycle);
        Assert.DoesNotContain(a.Combat.VisibleAttacks(a.World.Visible), attack => attack.Ability == "core:terrain/heat_vent");
        string root = Path.Combine(Path.GetTempPath(), "roag-vent-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var combat = new TacticalCombat(loaded, _ => { });
            for (int i = 0; i < 4; i++)
            {
                a.Act(TacticalAction.Wait); Assert.True(combat.Commit(new(TacticalAction.Wait, Facing.East)).Success);
                a.Next(); Assert.Equal(a.Hero.Id, combat.AdvanceToPartyDecision()!.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(combat.State));
            }
            Assert.Equal(198, a.Enemy.Health); Assert.Equal(24, a.Hero.Health);
            Assert.Equal(a.Enemy.Health, loaded.State.Actors.Single(actor => actor.Id == a.Enemy.Id).Health);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void PushFloorCyclesStopAtFourPhysicalShovesPerActorAndAction()
    {
        using var a = new Arena(ClassKind.Adventurer); a.Enemy.Position = new(110, 110);
        foreach (var (cell, facing) in new[] { (new WorldPosition(101, 100), Facing.East), (new(102, 100), Facing.South),
            (new(102, 101), Facing.West), (new(101, 101), Facing.North) })
            a.Combat.PlaceOptionalTerrain(OptionalTerrainKind.PushFloor, "test:push:" + cell, cell, facing);
        a.Act(TacticalAction.Move);
        Assert.Equal(new(101, 100), a.Hero.Position);
        Assert.Equal(4, a.Combat.State.Events.Count(e => e.Kind == "PushFloorTriggered"));
        Assert.Single(a.Combat.State.Events, e => e.Kind == "PushFloorBlocked" && e.Ability == "chain limit");
        Assert.Equal(100, Assert.Single(a.Combat.State.Queue.Snapshot().Pending, w => w.ActorId == a.Hero.Id).Due);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(Material.Wall)]
    [InlineData(Material.StairsDown)]
    public void PushFloorsCannotShoveOntoBlockedOrProtectedLandings(Material obstruction)
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Combat.PlaceOptionalTerrain(OptionalTerrainKind.PushFloor, "test:blocked-push", new(101, 100), Facing.East);
        a.World.Chunks.Edit(new(102, 100), obstruction);
        a.Act(TacticalAction.Move); Assert.Equal(new(101, 100), a.Hero.Position);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "PushFloorBlocked");
    }

    [Fact]
    public void FragileShortcutArmsOnEntryAndClosesOnlyAfterBodiesAndLootLeave()
    {
        using var a = new Arena(ClassKind.Adventurer); var position = new WorldPosition(101, 100);
        a.Combat.PlaceOptionalTerrain(OptionalTerrainKind.FragileCrossing, "test:shortcut", position);
        a.Act(TacticalAction.Move); a.Next();
        Assert.Equal(OptionalTerrainPhase.Armed, a.Combat.State.OptionalTerrain.Nodes[0].Phase);
        a.Combat.SpawnLoot(GroundLootKind.Currency, 2, position);
        a.Act(TacticalAction.Move, Facing.South); a.Next();
        Assert.Equal(Material.Floor, a.World.Chunks.At(position));
        a.Act(TacticalAction.Move, Facing.North); a.Next(); Assert.Empty(a.Combat.State.Loot.Drops);
        a.Act(TacticalAction.Move, Facing.South); a.Next();
        Assert.Equal(OptionalTerrainPhase.Closed, a.Combat.State.OptionalTerrain.Nodes[0].Phase);
        Assert.Equal(Material.Water, a.World.Chunks.At(position)); Assert.Equal(24, a.Hero.Health);
        Assert.False(a.Combat.Preview(new(TacticalAction.Move, Facing.North)).Valid);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "FragileCollapsed");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        a.World.Chunks.Edit(position, Material.Floor);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void OptionalTerrainRejectsOrphanedSchedulesAndUnsafeShortcutPlacement()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Combat.PlaceOptionalTerrain(OptionalTerrainKind.HeatVent, "test:validation", new(101, 100));
        a.Act(TacticalAction.Wait); a.Next();
        var node = a.Combat.State.OptionalTerrain.Nodes[0];
        a.Combat.State.OptionalTerrain.Nodes[0] = node with { Due = node.Due + 1 };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Combat.State.OptionalTerrain.Nodes[0] = node;
        a.Combat.State.Queue.Schedule(ScheduledKind.TerrainCycle, 150, referenceId: 999);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        for (int x = 100; x <= 104; x++) foreach (int y in new[] { 104, 106 }) a.World.Chunks.Edit(new(x, y), Material.Wall);
        a.World.Chunks.Edit(new(102, 105), Material.Floor);
        a.World.Chunks.Edit(new(99, 105), Material.Wall); a.World.Chunks.Edit(new(105, 105), Material.Wall);
        Assert.Throws<ArgumentException>(() => a.Combat.PlaceOptionalTerrain(OptionalTerrainKind.FragileCrossing, "test:cut-route", new(102, 105)));
    }
}
