using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void NativeEnemyRecoveryMatchesOriginalSpeedChangesStunsAndDisplacement()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Tactics/Fixtures/original-enemy-recovery.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
            EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = a.Hero.Position.Offset(1, 0);
            a.Friend.Position = new(110, 110);
            if (input.TryGetProperty("block_push", out var block) && block.GetBoolean())
                a.World.Chunks.Edit(a.Hero.Position.Offset(2, 0), Material.Wall);
            if (input.TryGetProperty("boon", out var boon))
                a.Hero.Tactics!.Boons.Inventory = a.Hero.Tactics.Boons.Inventory.Grant("core:boon/" + boon.GetString(),
                    BoonRarity.Common, ClassKind.Adventurer, input.TryGetProperty("stacks", out var stacks) ? stacks.GetInt32() : 1);
            if (input.TryGetProperty("enemy_speed_boon", out var buff) && buff.GetBoolean())
            {
                Grant(a.Enemy, "core:boon/road_flask");
                a.Enemy.Tactics!.Boons.Memory["core:boon/road_flask"] = new(Speed: 1000, ExpiresAt: 1000);
            }
            a.Combat.State.RandomState = 37; a.World.Reveal();
            int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                if (index == 1 && input.TryGetProperty("speed", out var speed))
                    a.Enemy.Tactics!.InnateStats = a.Enemy.Tactics.InnateStats! with { Speed = speed.GetInt32() };
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() == "basic_attack" ? TacticalAction.BasicAttack : TacticalAction.Wait);
                for (int i = 0; ; i++)
                {
                    Assert.True(i < 50, name + " failed to return control");
                    var actor = a.Combat.AdvanceToDecision(); Assert.NotNull(actor);
                    if (actor.Id == a.Hero.Id) break;
                    a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
                }
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" => $"windup:{e.Time}",
                    "DamageTaken" => $"damage:{e.Time}:{(e.Source == a.Hero.Id ? "player" : "enemy")}:{e.Amount}",
                    "WindupCancelled" => $"cancel:{e.Time}:{e.Ability}",
                    _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace),
                    name + " / " + index + " trace: " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                long? Due(ScheduledKind kind) => a.Combat.State.Queue.Snapshot().Pending
                    .SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == kind)?.Due;
                long? ExpectedDue(string key) => expected.GetProperty(key).ValueKind == JsonValueKind.Null ? null : expected.GetProperty(key).GetInt64();
                Assert.Equal(ExpectedDue("ready"), Due(ScheduledKind.ActorReady));
                Assert.Equal(ExpectedDue("impact"), Due(ScheduledKind.Impact));
                Assert.Equal(expected.GetProperty("hero_hp").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(expected.GetProperty("enemy_dx").GetInt32(), a.Enemy.Position.X - a.Hero.Position.X);
                Assert.Equal(expected.GetProperty("enemy_dy").GetInt32(), a.Enemy.Position.Y - a.Hero.Position.Y);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(30, decisions);
    }

    [Fact]
    public void EnemyTimelineProjectsChangingRecoveryWithoutCommittingOrLeakingIt()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = a.Hero.Position.Offset(1, 0);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); a.Next(); a.World.Reveal();
        a.Enemy.Tactics!.InnateStats = a.Enemy.Tactics.InnateStats! with { Speed = 140 };
        string before = JsonSerializer.Serialize(a.Combat.State);
        var projected = Assert.Single(a.Combat.VisibleTimeline(), e => e.ActorId == a.Enemy.Id && e.Projected);
        Assert.Equal(200, projected.Due); Assert.Null(projected.Sequence);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
        a.World.Visible.Remove(a.Enemy.Position);
        Assert.DoesNotContain(a.Combat.VisibleTimeline(), e => e.ActorId == a.Enemy.Id);
        a.World.Reveal(); a.Act(TacticalAction.Wait); a.Next();
        Assert.DoesNotContain(a.Combat.VisibleTimeline(), e => e.Projected);
        a.Enemy.Tactics.InnateStats = a.Enemy.Tactics.InnateStats with { Speed = 35 };
        Assert.Equal(200, Assert.Single(a.Combat.VisibleTimeline(), e => e.ActorId == a.Enemy.Id).Due);
    }

    [Fact]
    public void SavingDuringEnemyWindupPreservesImpactTimeRecoveryWithChangedSpeed()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = a.Hero.Position.Offset(1, 0);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); a.Next();
        a.Enemy.Tactics!.InnateStats = a.Enemy.Tactics.InnateStats! with { Speed = 95 };
        string directory = Path.Combine(Path.GetTempPath(), "roag-enemy-recovery-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.DoesNotContain(resumed.State.Queue.Snapshot().Pending,
                w => w.ActorId == a.Enemy.Id && w.Kind == ScheduledKind.ActorReady);
            a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            a.Next(); Assert.Equal(a.Hero.Id, resumed.AdvanceToDecision()!.Id);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.Equal(a.Hero.Health, loaded.State.Actors.Single(x => x.Id == a.Hero.Id).Health);
            Assert.Equal(240, Assert.Single(resumed.State.Queue.Snapshot().Pending,
                w => w.ActorId == a.Enemy.Id && w.Kind == ScheduledKind.ActorReady).Due);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(null)]
    public void NativeWindupRejectsMissingOrInvalidRecoveryContracts(int? cost)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = a.Hero.Position.Offset(1, 0);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        a.Combat.State.Attacks[0] = a.Combat.State.Attacks[0] with { RecoveryCost = cost };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }
}
