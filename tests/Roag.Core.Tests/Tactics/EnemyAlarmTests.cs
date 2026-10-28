using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void PrepareAlarm(Arena a)
    {
        EnemyProfile(a, "alarm_keeper"); a.Enemy.Position = new(103, 100); a.Friend.Position = new(110, 110);
    }

    private static void AlarmWait(Arena a)
    {
        a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
    }

    [Fact]
    public void AlarmPublicCommandsMatchOriginalCallTimingPhysicalInterceptionAndFiniteBudget()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-alarm-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = sample.GetProperty("input").GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareAlarm(a);
            a.Combat.State.RandomState = 37; a.World.State.Inventory["gold"] = 0; a.World.Reveal(); int index = 0;
            foreach (var expected in sample.GetProperty("expected").EnumerateArray())
            {
                if (index == 2)
                {
                    if (name is "wall" or "stairs") a.World.Chunks.Edit(new(103, 99), name == "wall" ? Material.Wall : Material.StairsDown);
                    if (name == "occupied") RevivalTestEnemy(a, "mossbound_guard", new(103, 99));
                }
                long first = a.Combat.State.NextEvent; AlarmWait(a); index++;
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "SupportResolved" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "AlarmReinforcement" or "AlarmBlocked" => $"{e.Kind}:{e.Time}:{e.Position.X - 100}:{e.Position.Y - 100}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}", _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("xp").GetInt32(), a.Hero.Tactics!.Build!.Experience);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), a.World.Count("gold"));
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                Assert.Equal(expected.GetProperty("spent").GetBoolean(), a.Enemy.Tactics!.Enemy!.AlarmSpent);
                Assert.Equal(expected.GetProperty("called").EnumerateArray().Select(e =>
                    (e.GetProperty("x").GetInt32(), e.GetProperty("y").GetInt32(), e.GetProperty("hp").GetInt32())),
                    a.World.State.Actors.Where(x => x.Life == LifeState.Alive && x.Tactics?.Enemy?.CalledBy > 0)
                        .Select(x => (x.Position.X - 100, x.Position.Y - 100, x.Health)));
                foreach (var (key, kind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                    Assert.Equal(expected.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : expected.GetProperty(key).GetInt64(),
                        a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == kind)?.Due);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(28, decisions);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("called")]
    [InlineData("cancelled")]
    public void AlarmWorldSaveRetainsSpentAttemptPendingCellsAndCalledActors(string phase)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareAlarm(a); AlarmWait(a);
        if (phase == "called") for (int i = 0; i < 3; i++) AlarmWait(a);
        if (phase == "cancelled") Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        string directory = Path.Combine(Path.GetTempPath(), "roag-alarm-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            for (int i = 0; i < 4; i++)
            {
                a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); RunRevivalEnemies(loaded, resumed, a.Hero.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
                Assert.Equal(a.Hero.Health, loaded.Party.Single().Health);
            }
            Assert.True(loaded.State.Actors.Single(x => x.Id == a.Enemy.Id).Tactics!.Enemy!.AlarmSpent);
            Assert.Equal(phase == "cancelled" ? 0 : 2, loaded.State.Actors.Count(x => x.Tactics?.Enemy?.CalledBy == a.Enemy.Id));
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AlarmCancellationNeverRefundsAttemptOrCreatesReinforcements(bool killCaller)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareAlarm(a); AlarmWait(a);
        if (killCaller) a.Hit(a.Enemy, 1000, source: a.Hero);
        else Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        for (int i = 0; i < 6; i++) AlarmWait(a);
        Assert.True(a.Enemy.Tactics!.Enemy!.AlarmSpent);
        Assert.DoesNotContain(a.Combat.State.Attacks, p => p.Support?.Kind == EnemySupportKind.Alarm);
        Assert.DoesNotContain(a.World.State.Actors, x => x.Tactics?.Enemy?.CalledBy > 0);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void AlarmOffscreenCallUsesPhysicalLandingsAndKeepsPrivateIntent()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 50);
        EnemyProfile(a, "alarm_keeper"); a.Enemy.Position = new(103, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.World.Visible.Clear(); string before = JsonSerializer.Serialize(a.World.State);
        var intent = a.Combat.PreviewEnemyTurn(a.Friend.Id, (_, _) => null);
        Assert.Equal(new[] { new WorldPosition(103, 99, 1), new(104, 100, 1) }, intent.Cells);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next(); Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        for (int i = 0; i < 3; i++) { a.Act(TacticalAction.Wait); a.Next(); }
        var called = a.World.State.Actors.Where(x => x.Tactics?.Enemy?.CalledBy == a.Enemy.Id).ToArray();
        Assert.Equal(2, called.Length);
        Assert.All(called, x => { Assert.Equal(1, x.Position.Floor); Assert.Equal("spider", x.SpriteId); Assert.True(x.Tactics!.RewardExempt); });
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void AlarmWaitsWithoutSpendingIfAllLandingsBlockedAndCanSelectOneLegalCell()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0); PrepareAlarm(a);
        a.World.Chunks.Edit(new(103, 99), Material.Wall); a.World.Chunks.Edit(new(104, 100), Material.StairsDown);
        a.World.Chunks.Edit(new(103, 101), Material.Wall); a.World.Chunks.Edit(new(102, 100), Material.Wall);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        var noLanding = a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(EnemyIntentKind.Wait, noLanding.Kind); Assert.False(a.Enemy.Tactics!.Enemy!.AlarmSpent);
        a.World.Chunks.Edit(new(102, 100), Material.Floor);
        var one = a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(new WorldPosition(102, 100), Assert.Single(one.Cells)); Assert.False(a.Enemy.Tactics.Enemy.AlarmSpent);
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); Assert.True(a.Enemy.Tactics.Enemy.AlarmSpent);
    }

    [Fact]
    public void AlarmReinforcementsCannotGrantRewardsOrRevivalCorpsesAndRetainCallerHistory()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareAlarm(a);
        RevivalTestEnemy(a, "grave_cantor", new(109, 109));
        for (int i = 0; i < 4; i++) AlarmWait(a);
        var called = a.World.State.Actors.Where(x => x.Tactics?.Enemy?.CalledBy == a.Enemy.Id).ToArray(); Assert.Equal(2, called.Length);
        long xp = a.Hero.Tactics!.Build!.Experience; int loot = a.Combat.State.Loot.Drops.Count;
        foreach (var actor in called) a.Hit(actor, 1000, source: a.Hero);
        Assert.Equal(xp, a.Hero.Tactics.Build.Experience); Assert.Equal(loot, a.Combat.State.Loot.Drops.Count); Assert.Empty(a.Combat.State.Corpses);
        a.Hit(a.Enemy, 1000, source: a.Hero);
        a.Combat.State.Events.Clear(); a.Combat.State.Loot.Drops.Clear();
        Assert.True(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void AlarmRejectsInvalidCommitsAndForgedPendingBudgetOrCalledAncestry()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0); PrepareAlarm(a);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<ArgumentException>(() => Commit("core:ability/keeper_alarm", [new(103, 99)], null));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/keeper_touch", [new(103, 99)], new(EnemySupportKind.Alarm)));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/keeper_alarm", [new(103, 98)], new(EnemySupportKind.Alarm)));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/keeper_alarm", [new(103, 99), new(104, 100), new(103, 101)], new(EnemySupportKind.Alarm)));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Enemy.Tactics!.Enemy!.AlarmSpent = true;
        Assert.Throws<ArgumentException>(() => Commit("core:ability/keeper_alarm", [new(103, 99)], new(EnemySupportKind.Alarm)));
        a.Enemy.Tactics.Enemy.AlarmSpent = false;
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); a.Next();
        a.Enemy.Tactics.Enemy.AlarmSpent = false;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Enemy.Tactics.Enemy.AlarmSpent = true;
        for (int i = 0; i < 3; i++) AlarmWait(a);
        var called = a.World.State.Actors.First(x => x.Tactics?.Enemy?.CalledBy > 0);
        called.Tactics!.RewardExempt = false;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        called.Tactics.RewardExempt = true; called.Tactics.Enemy!.CalledBy = a.Hero.Id;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        called.Tactics.Enemy.CalledBy = a.Enemy.Id;
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        var extra = RevivalTestEnemy(a, "ruin_skitter", new(108, 108)); extra.Tactics!.RewardExempt = true; extra.Tactics.Enemy!.CalledBy = a.Enemy.Id;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        LockedAttack Commit(string ability, WorldPosition[] cells, EnemySupport? support)
            => a.Combat.CommitWindup(ability, cells, AttackDelivery.Melee, DamageElement.Physical, 1, 220, 150, support: support);
    }
}
