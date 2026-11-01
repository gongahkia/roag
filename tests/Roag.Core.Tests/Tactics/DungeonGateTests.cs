using System.Text.Json;
using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void TimedGateCommandsMatchOriginalPlateRefreshOccupiedClosureAndFreeRefusal()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-gates.json")));
        int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            using var world = Floor(input.TryGetProperty("class", out var kind) ? ClassCatalog.Get("core:actor/" + kind.GetString()).Kind : ClassKind.Adventurer);
            var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
            bool blocked = input.TryGetProperty("blocked", out _);
            var gate = combat.SpawnTimedGate("test:original-gate", origin.Offset(blocked ? 1 : 2, 0), origin.Offset(1, blocked ? 1 : 0));
            combat.State.RandomState = 37; world.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                var action = command[0].GetString() switch { "move" => TacticalAction.Move, "dash" => TacticalAction.Dash,
                    "wait" => TacticalAction.Wait, _ => TacticalAction.BasicAttack };
                var intent = new TacticalIntent(action, command[1].GetInt32() > 0 ? Facing.East : Facing.West);
                var expected = sample.GetProperty("expected")[index++]; long first = combat.State.NextEvent;
                string before = JsonSerializer.Serialize(world.State); var preview = combat.Preview(intent);
                Assert.Equal(before, JsonSerializer.Serialize(world.State)); Assert.Equal(expected.GetProperty("valid").GetBoolean(), preview.Valid);
                Assert.Equal(expected.GetProperty("recovery").GetInt64(), preview.Valid ? preview.Recovery : 0);
                Assert.Equal(preview.Valid, world.SubmitDungeon(intent).Success);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), 0), hero.Position);
                Assert.Equal(expected.GetProperty("open").GetBoolean(), world.Chunks.At(gate.Position) == Material.Door);
                Assert.Equal(expected.GetProperty("due").GetInt64(), combat.State.Gates.Nodes.Single(g => g.Id == gate.Id).CloseAt);
                foreach (var (key, name) in new[] { ("opened", "TimedGateOpened"), ("closed", "TimedGateClosed") })
                    Assert.Equal(expected.GetProperty(key).GetInt32(), combat.State.Events.Count(e => e.Sequence >= first && e.Kind == name));
                TacticalValidation.Validate(world.Plan, world.State); count++;
            }
        }
        Assert.Equal(19, count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PendingAndOccupiedGateClosuresContinueThroughActualSaves(bool occupied)
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var gate = combat.SpawnTimedGate("test:saved-gate", hero.Position.Offset(2, 0), hero.Position.Offset(1, 0)); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        if (occupied)
        {
            Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(0, combat.State.Gates.Nodes.Single(g => g.Id == gate.Id).Schedule);
            Assert.DoesNotContain(combat.State.Queue.Snapshot().Pending, w => w.Kind == ScheduledKind.GateClosure && w.ReferenceId == gate.Id);
        }
        string root = Path.Combine(Path.GetTempPath(), "roag-gate-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            for (int i = 0; i < 3; i++)
            {
                var intent = new TacticalIntent(occupied && i == 0 ? TacticalAction.Move : TacticalAction.Wait, Facing.East);
                Assert.True(world.SubmitDungeon(intent).Success); Assert.True(loaded.SubmitDungeon(intent).Success);
                Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            }
            Assert.Equal(Material.ClosedDoor, loaded.Chunks.At(gate.Position));
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void AnEnemyCanTriggerAPlateAndGateInspectionDoesNotRevealRememberedCurrentState()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
        var gate = combat.SpawnTimedGate("test:enemy-plate", origin.Offset(2, 0), origin.Offset(1, 0));
        var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard"); enemy.Position = origin.Offset(1, -1);
        combat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 0, enemy.Id);
        Assert.True(combat.Commit(new(TacticalAction.Wait)).Success); Assert.Same(enemy, combat.AdvanceToDecision());
        Assert.True(combat.Commit(new(TacticalAction.Move, Facing.South)).Success); combat.AdvanceToPartyDecision();
        Assert.Equal(Material.Door, world.Chunks.At(gate.Position));
        Assert.Contains(combat.State.Events, e => e.Kind == "TimedGateOpened" && e.Source == enemy.Id);
        world.Reveal(); string before = JsonSerializer.Serialize(world.State);
        Assert.Contains("Closes in", WorldInspection.Inspect(world, gate.Position).Detail);
        world.Visible.Remove(gate.Position);
        Assert.Contains("remembered", WorldInspection.Inspect(world, gate.Position).Title);
        Assert.DoesNotContain("Closes in", WorldInspection.Inspect(world, gate.Position).Detail);
        Assert.Equal(before, JsonSerializer.Serialize(world.State)); TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void GateClosureContinuesAfterPaidStairsAndHidesItsOffFloorTimeline()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var gate = combat.SpawnTimedGate("test:off-floor-gate", hero.Position.Offset(2, 0), hero.Position.Offset(1, 0));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        hero.Position = world.State.Position = world.State.Dungeon!.Floors[0].Exit; world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseStairs)).Success); Assert.Equal(2, hero.Position.Floor);
        Assert.DoesNotContain(combat.VisibleTimeline(), w => w.Kind == ScheduledKind.GateClosure);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(Material.ClosedDoor, world.Chunks.At(gate.Position));
        Assert.DoesNotContain(gate.Position, world.Visible); TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void GateValidationRejectsDanglingPlateAndOrphanedClosureWithoutAllowingRouteSealing()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
        var gate = combat.SpawnTimedGate("test:invalid-gate", origin.Offset(2, 0), origin.Offset(1, 0));
        var plate = combat.State.RoomFeatures.Nodes.Single(n => n.Gate == gate.Id);
        combat.State.RoomFeatures.Nodes.Remove(plate);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        combat.State.RoomFeatures.Nodes.Add(plate); combat.State.RoomFeatures.Nodes.Sort((a, b) => a.Id.CompareTo(b.Id));
        long orphan = combat.State.Queue.Schedule(ScheduledKind.GateClosure, 100, referenceId: gate.Id).Sequence;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        combat.State.Queue.Cancel(orphan); TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void AnOwnedBulwarkHoldsTheDueGateOpenUntilItsActualExpiry()
    {
        using var world = Floor(ClassKind.Geomancer); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var gate = combat.SpawnTimedGate("test:blocked-gate", hero.Position.Offset(2, 0), hero.Position.Offset(1, 0)); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Special, Facing.East)).Success);
        Assert.Equal(gate.Position, hero.Tactics!.Resources.Bulwark!.Position);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(Material.Door, world.Chunks.At(gate.Position));
        Assert.Equal(0, combat.State.Gates.Nodes.Single(g => g.Id == gate.Id).Schedule);
        TacticalValidation.Validate(world.Plan, world.State);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Null(hero.Tactics.Resources.Bulwark); Assert.Equal(Material.ClosedDoor, world.Chunks.At(gate.Position));
        Assert.Single(combat.State.Events, e => e.Kind == "TimedGateClosed" && e.Position == gate.Position);
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
