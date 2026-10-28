using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void WraithPublicCommandsMatchOriginalPhaseCounterplayAndTrapTiming()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-wraith-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(name == "trap" ? ClassKind.Trapper : ClassKind.Adventurer, enemyDue: 0);
            var origin = a.Hero.Position; var landing = origin.Offset(-1, 0); var wall = origin.Offset(-2, 0);
            EnemyProfile(a, "hollow_wraith"); a.Enemy.Position = origin.Offset(-3, 0); a.Friend.Position = new(110, 110);
            a.World.Chunks.Edit(wall, Material.Wall);
            if (name == "stairs") a.World.Chunks.Edit(landing, Material.StairsUp);
            if (name == "trap") a.Hero.Tactics!.Resources.Snare = landing;
            a.Combat.State.RandomState = 37; a.World.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                if (index == 1 && name == "removed-wall") a.World.Chunks.Edit(wall, Material.Floor);
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() == "move" ? TacticalAction.Move : TacticalAction.Wait, Facing.West);
                for (int i = 0; ; i++)
                {
                    Assert.True(i < 30, name + " did not return control");
                    var actor = a.Combat.AdvanceToDecision(); Assert.NotNull(actor);
                    if (actor.Id == a.Hero.Id) break;
                    a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
                }
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "SupportResolved" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "DamageTaken" => $"damage:{e.Time}:{(e.Target == a.Hero.Id ? "hero" : "enemy")}:{e.Amount}",
                    "Displaced" => $"phase:{e.Time}:{e.Position.X - origin.X}:{e.Position.Y - origin.Y}",
                    "PhaseBlocked" or "SnareTriggered" or "SnareHeld" => $"{e.Kind}:{e.Time}",
                    _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), expected.GetProperty("y").GetInt32()), a.Hero.Position);
                Assert.Equal(origin.Offset(expected.GetProperty("enemy_x").GetInt32(), expected.GetProperty("enemy_y").GetInt32()), a.Enemy.Position);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(20, decisions);
    }

    [Fact]
    public void WraithPhasesOnAnotherFloorWithoutConsultingSelectedVision()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "hollow_wraith"); a.Enemy.Position = new(97, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.World.Chunks.Edit(new(98, 100, 1), Material.Wall);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        string before = JsonSerializer.Serialize(a.Combat.State);
        a.World.Visible.Clear();
        Assert.Equal(EnemySupportKind.Phase, a.Combat.PreviewEnemyTurn(a.Friend.Id, (_, _) => null).Support!.Kind);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next();
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(new WorldPosition(99, 100, 1), a.Enemy.Position);
        Assert.Equal(a.Friend.MaxHealth, a.Friend.Health);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "AttackPerformed");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void WraithPhaseDestinationAndRecoverySurviveWorldSave()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(97, 100); a.World.Chunks.Edit(new(98, 100), Material.Wall);
        var attack = BeginExpansion(a, "hollow_wraith"); a.Next();
        string directory = Path.Combine(Path.GetTempPath(), "roag-wraith-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(EnemySupportKind.Phase, Assert.Single(resumed.State.Attacks).Support!.Kind);
            a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            a.Next(); Assert.Equal(a.Hero.Id, resumed.AdvanceToDecision()!.Id);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.Equal(a.Enemy.Position, loaded.State.Actors.Single(x => x.Id == a.Enemy.Id).Position);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void WraithRejectsForgedDiagonalPhaseBeforeCommittingAndWhenLoading()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "hollow_wraith"); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        string before = JsonSerializer.Serialize(a.Combat.State);
        var invalid = a.Enemy.Position.Offset(1, 1);
        Assert.Throws<ArgumentException>(() => a.Combat.CommitWindup("core:ability/wraith_phase", [invalid],
            AttackDelivery.Melee, DamageElement.Physical, 2, 100, 120, support: new(EnemySupportKind.Phase)));
        Assert.Throws<ArgumentException>(() => a.Combat.CommitWindup("core:ability/wraith_phase", [a.Enemy.Position.Offset(2, 0)],
            AttackDelivery.Melee, DamageElement.Physical, 2, 100, 120));
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
        var attack = a.Combat.CommitWindup("core:ability/wraith_phase", [a.Enemy.Position.Offset(2, 0)],
            AttackDelivery.Melee, DamageElement.Physical, 2, 100, 120, support: new(EnemySupportKind.Phase));
        a.Combat.State.Attacks[0] = attack with { Cells = [invalid] };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Theory]
    [InlineData(Material.Wall)]
    [InlineData(Material.StairsDown)]
    public void NewObstacleAtWraithLandingBlocksPhaseWithoutRefundingRecovery(Material obstacle)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(97, 100); a.World.Chunks.Edit(new(98, 100), Material.Wall);
        var attack = BeginExpansion(a, "hollow_wraith"); a.World.Chunks.Edit(attack.Cells[0], obstacle);
        FinishExpansion(a, attack);
        Assert.Equal(new WorldPosition(97, 100), a.Enemy.Position);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "PhaseBlocked");
        Assert.Equal(attack.Due + ActionTiming.Recovery(120, a.Enemy.Speed),
            a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Enemy.Id && w.Kind == ScheduledKind.ActorReady).Due);
    }

    [Fact]
    public void WraithCannotFinishPhasingAfterBeingKilledDuringItsChannel()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(97, 100); a.World.Chunks.Edit(new(98, 100), Material.Wall);
        var attack = BeginExpansion(a, "hollow_wraith"); a.Hit(a.Enemy, 1000, source: a.Hero); FinishExpansion(a, attack);
        Assert.Equal(new WorldPosition(97, 100), a.Enemy.Position);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "SupportResolved");
    }
}
