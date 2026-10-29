using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void PrepareSentinel(Arena a)
    {
        EnemyProfile(a, "rotating_sentinel"); a.Enemy.Position = new(101, 100); a.Friend.Position = new(110, 110);
        a.Hero.Tactics!.Build = CharacterBuild.Create(ClassKind.Adventurer, 37);
    }

    private static void SentinelWait(Arena a, int count)
    {
        for (int i = 0; i < count; i++) { a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); }
    }

    [Fact]
    public void SentinelPublicCommandsMatchOriginalQuartersFriendlyFireDamageLockedArcsAndDeath()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-sentinel-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSentinel(a);
            var guards = new List<Actor>();
            if (name == "friendly") foreach (int y in new[] { 99, 101 }) guards.Add(RevivalTestEnemy(a, "mossbound_guard", new(100, y)));
            if (name == "wall") a.World.Chunks.Edit(new(100, 99), Material.Wall);
            a.Combat.State.RandomState = 37; a.World.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                if (index == 1 && name == "death") a.Enemy.Health = 3;
                long first = a.Combat.State.NextEvent;
                var action = command.GetString() switch { "basic_attack" => TacticalAction.BasicAttack, "move" => TacticalAction.Move, _ => TacticalAction.Wait };
                a.Act(action, action == TacticalAction.Move ? Facing.West : Facing.East);
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "DamageTaken" => $"damage:{e.Time}:{(e.Target == a.Hero.Id ? "hero" : e.Target == a.Enemy.Id ? "sentinel" : "guard")}:{e.Amount}", _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("x").GetInt32(), 100 + expected.GetProperty("y").GetInt32()), a.Hero.Position);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(expected.GetProperty("strikes").GetInt32(), a.Enemy.Tactics!.Enemy!.SentinelStrikes);
                Assert.Equal(expected.GetProperty("finished").GetBoolean(), a.Enemy.Tactics.Enemy.SentinelFinished);
                Assert.Equal(expected.GetProperty("facing").ValueKind == JsonValueKind.Null ? (Facing?)null
                    : (Facing)((expected.GetProperty("facing").GetInt32() - 1) * 2), a.Enemy.Tactics.Enemy.SentinelFacing);
                var attack = a.Combat.State.Attacks.SingleOrDefault(x => x.Source == a.Enemy.Id);
                Assert.Equal(expected.GetProperty("cells").EnumerateArray().Select(c => new WorldPosition(100 + c.GetProperty("x").GetInt32(), 100 + c.GetProperty("y").GetInt32())), attack?.Cells ?? []);
                Assert.Equal(expected.GetProperty("guards").EnumerateArray().Select(h => h.GetInt32()), guards.Select(g => g.Health));
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Hero.Tactics!.Build!.Level > 1 ? a.Hero.Tactics.Build.RandomState : a.Combat.State.RandomState);
                foreach (var (key, kind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                    Assert.Equal(expected.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : expected.GetProperty(key).GetInt64(),
                        a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == kind)?.Due);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(43, decisions);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(10, false)]
    [InlineData(1, true)]
    public void SentinelWorldSavesContinueLockedAndFinishedSequencesDespiteLaterTerrainChanges(int waits, bool changedTerrain)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSentinel(a); SentinelWait(a, waits);
        if (changedTerrain) a.World.Chunks.Edit(new(100, 99), Material.Wall);
        string directory = Path.Combine(Path.GetTempPath(), "roag-sentinel-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            for (int i = 0; i < 5; i++)
            {
                a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); RunRevivalEnemies(loaded, resumed, a.Hero.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
            }
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void SentinelPreviewIsPureAndOffscreenQuartersDoNotLeakToTheSelectedMember()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 50);
        EnemyProfile(a, "rotating_sentinel"); a.Enemy.Position = new(101, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.World.Visible.Clear(); string before = JsonSerializer.Serialize(a.World.State);
        for (int i = 0; i < 30; i++)
        {
            var preview = a.Combat.PreviewEnemyTurn(a.Friend.Id, (_, _) => null);
            Assert.Equal(Facing.West, preview.SentinelFacing); Assert.Equal(3, preview.Cells.Length);
        }
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next();
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible)); Assert.DoesNotContain(a.Combat.VisibleTimeline(), t => t.ActorId == a.Enemy.Id);
        int hp = a.Friend.Health;
        for (int i = 0; i < 2; i++) { a.Act(TacticalAction.Wait); a.Next(); }
        Assert.Equal(hp - 2, a.Friend.Health); Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("count")]
    [InlineData("facing")]
    [InlineData("finished")]
    [InlineData("footprint")]
    [InlineData("order")]
    [InlineData("identity")]
    [InlineData("owner")]
    public void SentinelSavesRejectForgedSequenceFacingGeometryAndRoles(string corruption)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSentinel(a); SentinelWait(a, 1);
        var attack = Assert.Single(a.Combat.State.Attacks); var memory = a.Enemy.Tactics!.Enemy!;
        switch (corruption)
        {
            case "count": memory.SentinelStrikes = 5; break;
            case "facing": memory.SentinelFacing = Facing.NorthEast; break;
            case "finished": memory.SentinelFinished = true; break;
            case "footprint": a.Combat.State.Attacks[0] = attack with { Cells = [a.Enemy.Position] }; break;
            case "order": a.Combat.State.Attacks[0] = attack with { Cells = attack.Cells.Reverse().ToArray() }; break;
            case "identity": a.Combat.State.Attacks[0] = attack with { Ability = "core:ability/sentinel_touch" }; break;
            case "owner": a.Enemy.Tactics.DefinitionId = "core:enemy/mossbound_guard"; break;
        }
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    public void SentinelShovesEndStartedSequencesIncludingRecoveryButBlockedShovesAndUnstartedSequencesStayIntact(int waits, bool blocked)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSentinel(a); SentinelWait(a, waits);
        var rammer = RevivalTestEnemy(a, "heavy_rammer", new(101, 99));
        var position = a.Enemy.Position; var landing = position.Offset(0, 1);
        if (blocked) a.World.Chunks.Edit(landing, Material.Wall);
        var pending = a.Combat.State.Attacks.SingleOrDefault(x => x.Source == a.Enemy.Id);
        long? ready = a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == ScheduledKind.ActorReady)?.Due;
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, rammer.Id, rammer.Position, [position], "core:ability/rammer_shove",
            AttackDelivery.Melee, DamageElement.Physical, 2, a.Combat.Clock, Shove: new(position, Facing.South)));
        Assert.Equal(blocked ? position : landing, a.Enemy.Position);
        Assert.Equal(!blocked && waits > 0, a.Enemy.Tactics!.Enemy!.SentinelFinished);
        if (blocked) Assert.Equal(pending, a.Combat.State.Attacks.SingleOrDefault(x => x.Source == a.Enemy.Id));
        else Assert.DoesNotContain(a.Combat.State.Attacks, x => x.Source == a.Enemy.Id);
        Assert.Equal(!blocked && pending != null ? a.Combat.Clock + 100 : ready,
            a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == ScheduledKind.ActorReady)?.Due);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        long nextEvent = a.Combat.State.NextEvent;
        SentinelWait(a, 4);
        if (!blocked && waits > 0) Assert.DoesNotContain(a.Combat.State.Events,
            e => e.Sequence >= nextEvent && e.Source == a.Enemy.Id && e.Kind == "WindupStarted" && e.Ability == EnemySentinel.Quarter);
    }

    [Fact]
    public void SentinelRetainsChampionBoonImmunityAndHookrunnerPreservesAnAlreadyLockedQuarter()
    {
        using var a = new Arena(ClassKind.Hookrunner, enemyDue: 50);
        EnemyProfile(a, "rotating_sentinel"); a.Enemy.Position = new(103, 100); a.Friend.Position = new(110, 110);
        SentinelWait(a, 1); var pending = Assert.Single(a.Combat.State.Attacks);
        Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        Apply(a, Activation(a, a.Hero, "core:boon/anchor_maul", 100, a.Enemy), BoonEffectKind.DisplaceTarget);
        Assert.Equal(pending, Assert.Single(a.Combat.State.Attacks)); Assert.Equal(0, a.Enemy.Tactics!.StunnedUntil);
        Assert.Equal(new WorldPosition(103, 100), a.Enemy.Position);
        a.Act(TacticalAction.Special);
        Assert.Equal(new WorldPosition(101, 100), a.Enemy.Position);
        Assert.Equal(pending, Assert.Single(a.Combat.State.Attacks)); Assert.False(a.Enemy.Tactics.Enemy!.SentinelFinished);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("diagonal")]
    [InlineData("wrong_turn")]
    [InlineData("spent")]
    public void SentinelInvalidCommitmentsFailBeforeSpendingTimeOrTheNextQuarter(string invalid)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareSentinel(a);
        if (invalid is "wrong_turn" or "spent") SentinelWait(a, invalid == "spent" ? 12 : 3);
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        string before = JsonSerializer.Serialize(a.World.State);
        Facing? facing = invalid switch { "missing" => null, "diagonal" => Facing.NorthEast, _ => Facing.West };
        Assert.Throws<ArgumentException>(() => a.Combat.CommitWindup(EnemySentinel.Quarter, [a.Hero.Position], AttackDelivery.Area,
            DamageElement.Physical, 2, 160, 80, sentinelFacing: facing));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }
}
