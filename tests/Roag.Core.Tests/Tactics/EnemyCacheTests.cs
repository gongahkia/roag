using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void HauntedCachePublicCommandsMatchOriginalDormancyActivationHealingAndEvasion()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-cache-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
            EnemyProfile(a, "haunted_cache"); var origin = a.Hero.Position;
            a.Enemy.Position = origin.Offset(input.GetProperty("x").GetInt32(), 0); a.Friend.Position = new(110, 110);
            if (name == "injured") a.Enemy.Health--;
            if (name == "alerted") a.Enemy.Tactics!.Enemy!.Alerted = true;
            a.Combat.State.RandomState = 37; a.World.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                if (index == 1 && name == "injured") a.Enemy.Health = a.Enemy.MaxHealth;
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() == "move" ? TacticalAction.Move : TacticalAction.Wait, Facing.West);
                for (int i = 0; ; i++)
                {
                    Assert.True(i < 30); var actor = a.Combat.AdvanceToDecision(); Assert.NotNull(actor);
                    if (actor.Id == a.Hero.Id) break;
                    a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals));
                }
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}",
                    _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), expected.GetProperty("y").GetInt32()), a.Enemy.Position);
                Assert.Equal(expected.GetProperty("alerted").GetBoolean(), a.Enemy.Tactics!.Enemy!.Alerted);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(25, decisions);
    }

    [Fact]
    public void BlockedInjuredCacheBecomesAlertOnlyWhenItsDecisionCommitsAndStaysAlertWhenHealed()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "haunted_cache"); a.Enemy.Health--;
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        string before = JsonSerializer.Serialize(a.World.State);
        var preview = a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(EnemyIntentKind.Wait, preview.Kind); Assert.True(preview.Alert);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.True(a.Enemy.Tactics!.Enemy!.Alerted); a.Enemy.Health = a.Enemy.MaxHealth;
        a.Next(a.Enemy.Id);
        Assert.Equal(EnemyIntentKind.Move, a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyRoute(a, from, goals)).Kind);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void CacheCanWakeAndAttackAnOffscreenPartyMemberWithoutRevealingItsIntent()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "haunted_cache"); a.Enemy.Position = new(101, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.World.Visible.Clear(); a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next();
        Assert.True(a.Enemy.Tactics!.Enemy!.Alerted);
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(a.Friend.MaxHealth - 2, a.Friend.Health); Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualCacheWorldSaveRetainsDormancyOrAlertAfterHealthIsRestored(bool alerted)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "haunted_cache"); if (alerted) a.Enemy.Health--;
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); a.Enemy.Health = a.Enemy.MaxHealth; a.Next();
        string directory = Path.Combine(Path.GetTempPath(), "roag-cache-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            var enemy = loaded.State.Actors.Single(x => x.Id == a.Enemy.Id);
            Assert.Equal(alerted, enemy.Tactics!.Enemy!.Alerted);
            a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            Assert.Same(a.Enemy, a.Combat.AdvanceToDecision()); Assert.Equal(enemy.Id, resumed.AdvanceToDecision()!.Id);
            a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => Facing.West); resumed.CommitEnemyTurn(a.Hero.Id, (_, _) => Facing.West);
            Assert.Equal(a.Enemy.Position, enemy.Position);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
