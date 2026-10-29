using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void PrepareHexer(Arena a)
    {
        EnemyProfile(a, "mirror_hexer"); a.Enemy.Position = new(103, 100); a.Friend.Position = new(110, 110);
        a.Hero.Tactics!.Build = CharacterBuild.Create(a.Hero.Tactics.Build!.Class, 37);
    }

    [Fact]
    public void MirrorPublicCommandsMatchOriginalShotsWeaponFormsDividendEnemyWindupsAndSentries()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-mirror-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            var kind = input.GetProperty("class_name").GetString() switch
            { "alchemist" => ClassKind.Alchemist, "hookrunner" => ClassKind.Hookrunner, "tinkerer" => ClassKind.Tinkerer, _ => ClassKind.Ranger };
            using var a = new Arena(kind, enemyDue: 50); PrepareHexer(a);
            if (input.GetProperty("boon").GetString() is { } boon) Grant(a.Hero, "core:boon/" + boon);
            if (name is "dividend" or "sentry") a.Hero.Health -= 4;
            Actor? guard = null, blocker = null, shooter = null; a.Combat.State.RandomState = 37; int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                if (index == 3 && name is not ("expiry" or "occupied" or "hook"))
                {
                    guard = RevivalTestEnemy(a, "mossbound_guard", new(102, 101));
                    if (name is "blocker" or "needle")
                    { blocker = RevivalTestEnemy(a, "mossbound_guard", new(101, 100)); if (name == "blocker") blocker.Health = 2; }
                    if (name == "enemy") { a.Hero.Position = new(104, 100); shooter = RevivalTestEnemy(a, "thornspitter", new(100, 100), a.Combat.Clock + 1); }
                }
                a.World.Reveal(); long first = a.Combat.State.NextEvent;
                var action = command.GetString() switch { "basic_attack" => TacticalAction.BasicAttack, "move" => TacticalAction.Move,
                    "sweeping_slash" => TacticalAction.Special, _ => TacticalAction.Wait };
                var preview = a.Combat.Preview(new(action, Facing.East));
                a.Act(action, Facing.East); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "SupportResolved" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "MirrorCreated" or "MirrorBlocked" or "MirrorReflected" => $"{e.Kind}:{e.Time}:{e.Position.X - 100}:{e.Position.Y - 100}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}", _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("x").GetInt32(), 100 + expected.GetProperty("y").GetInt32()), a.Hero.Position);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("enemy_x").GetInt32(), 100 + expected.GetProperty("enemy_y").GetInt32()), a.Enemy.Position);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(expected.GetProperty("casts").GetInt32(), a.Enemy.Tactics!.Enemy!.MirrorCasts);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Hero.Tactics!.Build!.Level > 1 ? a.Hero.Tactics.Build.RandomState : a.Combat.State.RandomState);
                Assert.Equal(expected.GetProperty("preview_step").ValueKind == JsonValueKind.Null ? (int?)null : expected.GetProperty("preview_step").GetInt32(), preview.Reflection?.Step);
                Assert.Equal(expected.GetProperty("mirrors").EnumerateArray().Select(m =>
                    (m.GetProperty("x").GetInt32(), m.GetProperty("y").GetInt32(), m.GetProperty("dx").GetInt32(), m.GetProperty("dy").GetInt32(), m.GetProperty("expires").GetInt64())),
                    a.Combat.State.Mirrors.Select(m => (m.Position.X - 100, m.Position.Y - 100, AttackGeometry.Offset(m.Incoming).X, AttackGeometry.Offset(m.Incoming).Y, m.Expires)));
                foreach (var (key, scheduleKind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                    Assert.Equal(expected.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : expected.GetProperty(key).GetInt64(),
                        a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == scheduleKind)?.Due);
                if (guard != null) Assert.Equal(expected.GetProperty("guard_hp").GetInt32(), guard.Health);
                if (blocker != null) Assert.Equal(expected.GetProperty("blocker_hp").GetInt32(), blocker.Health);
                var pending = a.Combat.State.Attacks.SingleOrDefault(x => x.Source == shooter?.Id);
                Assert.Equal(expected.GetProperty("shooter_cells").EnumerateArray().Select(c => new WorldPosition(100 + c.GetProperty("x").GetInt32(), 100 + c.GetProperty("y").GetInt32())), pending?.Cells ?? []);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(55, decisions);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("active")]
    [InlineData("reflected")]
    [InlineData("dead_owner")]
    [InlineData("sentry")]
    public void MirrorWorldSavesPreservePlacementAndConsumedEnemyOrSentryPaths(string phase)
    {
        using var a = new Arena(phase == "sentry" ? ClassKind.Tinkerer : ClassKind.Ranger, enemyDue: 50); PrepareHexer(a);
        SentinelWait(a, phase == "pending" ? 1 : 3);
        if (phase is "reflected" or "dead_owner")
        {
            a.Hero.Position = new(104, 100);
            var shooter = RevivalTestEnemy(a, "thornspitter", new(100, 100), a.Combat.Clock + 1);
            RevivalTestEnemy(a, "mossbound_guard", new(102, 101));
            SentinelWait(a, 1);
            Assert.NotNull(Assert.Single(a.Combat.State.Attacks, x => x.Source == shooter.Id).Reflection);
            Assert.Empty(a.Combat.State.Mirrors);
            if (phase == "dead_owner") a.Hit(a.Enemy, 1000, source: a.Hero);
        }
        if (phase == "sentry")
        {
            RevivalTestEnemy(a, "mossbound_guard", new(102, 101));
            a.Act(TacticalAction.Special); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
            Assert.NotNull(a.Hero.Tactics!.Resources.Sentry!.Reflection); Assert.Empty(a.Combat.State.Mirrors);
        }
        string directory = Path.Combine(Path.GetTempPath(), "roag-mirror-" + Guid.NewGuid().ToString("N"));
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

    [Theory]
    [InlineData("death")]
    [InlineData("wall")]
    [InlineData("stairs")]
    [InlineData("floor")]
    public void MirrorOwnerDeathFloorDepartureAndInvalidatedTerrainRemoveTheMarker(string reason)
    {
        using var a = new Arena(ClassKind.Ranger, enemyDue: 50); PrepareHexer(a); SentinelWait(a, 3);
        var mirror = Assert.Single(a.Combat.State.Mirrors);
        if (reason == "death") a.Hit(a.Enemy, 1000, source: a.Hero);
        else
        {
            if (reason == "floor") a.Enemy.Position = a.Enemy.Position with { Floor = 1 };
            else a.World.Chunks.Edit(mirror.Position, reason == "wall" ? Material.Wall : Material.StairsDown);
            a.Act(TacticalAction.Wait);
        }
        Assert.Empty(a.Combat.State.Mirrors); TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void MirrorOffscreenPlacementExpiresAtTheSharedClockAndPrivatePreviewsStayReadOnly()
    {
        using var a = new Arena(ClassKind.Ranger, ClassKind.Ranger, enemyDue: 50);
        EnemyProfile(a, "mirror_hexer"); a.Enemy.Position = new(103, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.World.Visible.Clear(); string before = JsonSerializer.Serialize(a.World.State);
        for (int i = 0; i < 20; i++) Assert.Equal(EnemySupportKind.Mirror, a.Combat.PreviewEnemyTurn(a.Friend.Id, (_, _) => null).Support!.Kind);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next();
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        for (int i = 0; i < 2; i++) { a.Act(TacticalAction.Wait); a.Next(); }
        var mirror = Assert.Single(a.Combat.State.Mirrors); Assert.Equal(626, mirror.Expires); Assert.Equal(1, mirror.Position.Floor);
        before = JsonSerializer.Serialize(a.World.State);
        Assert.Null(a.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Reflection);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Act(TacticalAction.Wait);
        var ready = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id && w.Kind == ScheduledKind.ActorReady);
        Assert.True(a.Combat.State.Queue.Delay(ready.Sequence, mirror.Expires - ready.Due));
        a.Next(); Assert.Equal(626, a.Combat.Clock); Assert.Empty(a.Combat.State.Mirrors);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void MirrorInterruptedAttemptsStaySpentAndTwoCastsExhaustTheLifetimeBudget()
    {
        using var a = new Arena(ClassKind.Ranger, enemyDue: 50); PrepareHexer(a); SentinelWait(a, 1);
        Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        Assert.Equal(1, a.Enemy.Tactics!.Enemy!.MirrorCasts); Assert.Empty(a.Combat.State.Attacks);
        a.Next(a.Enemy.Id); a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        Assert.Equal(2, a.Enemy.Tactics.Enemy.MirrorCasts); a.Next(a.Enemy.Id);
        Assert.Equal("core:ability/hexer_bolt", a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null).Ability);
        Assert.Empty(a.Combat.State.Mirrors); TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("direction")]
    [InlineData("origin")]
    [InlineData("expiry")]
    [InlineData("owner")]
    [InlineData("overlap")]
    [InlineData("budget")]
    public void MirrorSavesRejectForgedDirectionOriginsExpiryOwnershipAndBudgets(string corruption)
    {
        using var a = new Arena(ClassKind.Ranger, enemyDue: 50); PrepareHexer(a); SentinelWait(a, 3);
        var mirror = Assert.Single(a.Combat.State.Mirrors);
        switch (corruption)
        {
            case "direction": a.Combat.State.Mirrors[0] = mirror with { Incoming = Facing.NorthEast }; break;
            case "origin": a.Combat.State.Mirrors[0] = mirror with { Origin = mirror.Origin.Offset(1, 0) }; break;
            case "expiry": a.Combat.State.Mirrors[0] = mirror with { Expires = a.Combat.Clock }; break;
            case "owner": a.Combat.State.Mirrors[0] = mirror with { Source = a.Hero.Id }; break;
            case "overlap": a.Combat.State.Mirrors.Add(mirror); break;
            case "budget": a.Enemy.Tactics!.Enemy!.MirrorCasts = 0; break;
        }
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MirrorPlacementRechecksGlobalCapacityAndOtherOwnersAtImpact(bool full)
    {
        using var a = new Arena(ClassKind.Ranger, enemyDue: 50); PrepareHexer(a); SentinelWait(a, 1);
        for (int i = 0; i < (full ? 8 : 1); i++)
        {
            var owner = RevivalTestEnemy(a, "mirror_hexer", new(96 + i, 108));
            owner.Tactics!.Enemy!.MirrorCasts = 1;
            var cell = full ? new WorldPosition(96 + i, 107) : new(102, 100);
            a.Combat.State.Mirrors.Add(new(a.Combat.State.NextAction++, owner.Id, cell, cell.Offset(0, 1), Facing.South, a.Combat.Clock + 400));
        }
        SentinelWait(a, 2);
        Assert.Equal(full ? 8 : 1, a.Combat.State.Mirrors.Count);
        Assert.DoesNotContain(a.Combat.State.Mirrors, m => m.Source == a.Enemy.Id);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "MirrorBlocked" && e.Source == a.Enemy.Id);
        Assert.Equal(1, a.Enemy.Tactics!.Enemy!.MirrorCasts);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("ability")]
    [InlineData("direction")]
    [InlineData("budget")]
    [InlineData("cell")]
    public void MirrorPendingPlacementRejectsForgedIdentityGeometryAndBudget(string corruption)
    {
        using var a = new Arena(ClassKind.Ranger, enemyDue: 50); PrepareHexer(a); SentinelWait(a, 1);
        var attack = Assert.Single(a.Combat.State.Attacks);
        a.Combat.State.Attacks[0] = corruption switch
        {
            "ability" => attack with { Ability = "core:ability/hexer_bolt" },
            "direction" => attack with { Support = attack.Support! with { MirrorIncoming = Facing.West } },
            "cell" => attack with { Cells = [attack.Cells[0].Offset(0, 1)] },
            _ => attack
        };
        if (corruption == "budget") a.Enemy.Tactics!.Enemy!.MirrorCasts = 0;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    private static LockedAttack ReflectedEnemyWindup(Arena a)
    {
        PrepareHexer(a); SentinelWait(a, 3); a.Hero.Position = new(104, 100);
        var shooter = RevivalTestEnemy(a, "thornspitter", new(100, 100), a.Combat.Clock + 1);
        SentinelWait(a, 1);
        var attack = Assert.Single(a.Combat.State.Attacks, x => x.Source == shooter.Id);
        Assert.NotNull(attack.Reflection); return attack;
    }

    [Theory]
    [InlineData("direction")]
    [InlineData("step")]
    [InlineData("range")]
    [InlineData("mirror")]
    [InlineData("owner")]
    [InlineData("cell")]
    [InlineData("truncated")]
    [InlineData("delivery")]
    public void MirrorLockedEnemyPathsRejectRetargetingAndForgedReflectionMetadata(string corruption)
    {
        using var a = new Arena(ClassKind.Ranger, enemyDue: 50);
        var attack = ReflectedEnemyWindup(a); var reflection = attack.Reflection!;
        int index = a.Combat.State.Attacks.FindIndex(x => x.Id == attack.Id);
        a.Combat.State.Attacks[index] = corruption switch
        {
            "direction" => attack with { Reflection = reflection with { Incoming = Facing.West } },
            "step" => attack with { Reflection = reflection with { Step = 0 } },
            "range" => attack with { Reflection = reflection with { Range = reflection.Range + 1 } },
            "mirror" => attack with { Reflection = reflection with { Mirror = a.Combat.State.NextAction } },
            "owner" => attack with { Reflection = reflection with { Owner = a.World.State.NextActorId } },
            "cell" => attack with { Cells = attack.Cells.Select((c, i) => i == 2 ? c.Offset(1, 0) : c).ToArray() },
            "truncated" => attack with { Cells = attack.Cells[..^1] },
            "delivery" => attack with { Delivery = AttackDelivery.Area },
            _ => throw new ArgumentException(corruption)
        };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void MirrorSentrySaveRejectsRetargetingOfAnAlreadyConsumedReflection()
    {
        using var a = new Arena(ClassKind.Tinkerer, enemyDue: 50); PrepareHexer(a); SentinelWait(a, 3);
        a.Act(TacticalAction.Special); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
        var sentry = a.Hero.Tactics!.Resources.Sentry!; Assert.NotNull(sentry.Reflection);
        sentry.Cells[^1] = sentry.Cells[^1].Offset(1, 0);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Fact]
    public void MirrorIsConsumedAndDividendHealsEvenWhenTheReflectedManualAttackRollsAMiss()
    {
        using var a = new Arena(ClassKind.Ranger, enemyDue: 50); PrepareHexer(a); SentinelWait(a, 3);
        Grant(a.Hero, "core:boon/fateful_dice"); Grant(a.Hero, "core:boon/mirror_dividend");
        a.Hero.Health -= 4;
        var target = RevivalTestEnemy(a, "mossbound_guard", new(102, 101)); int hp = target.Health;
        int seed = Enumerable.Range(1, 1000).First(s => new RoagRandom(s).NextOneBased(8) == 8);
        var random = new RoagRandom(seed); random.NextOneBased(8); a.Combat.State.RandomState = seed;
        string before = JsonSerializer.Serialize(a.World.State);
        for (int i = 0; i < 10; i++) Assert.NotNull(a.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Reflection);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Act(TacticalAction.BasicAttack);
        Assert.Empty(a.Combat.State.Mirrors); Assert.Equal(hp, target.Health);
        Assert.Equal(a.Hero.MaxHealth - 2, a.Hero.Health); Assert.Equal(random.State, a.Combat.State.RandomState);
        Assert.Equal(8, a.Hero.Tactics!.Boons.Memory["core:boon/fateful_dice"].Face);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "MirrorReflected");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void MirrorWorldBoundaryClipsOnlyTheOutgoingLegAndPreservesTheLockedPathOnDisk()
    {
        using var a = new Arena(ClassKind.Ranger, enemyDue: 50); PrepareHexer(a);
        int y = WorldPlan.Size - 1;
        for (int x = 98; x <= 106; x++) for (int dy = 0; dy <= 2; dy++) a.World.Chunks.Edit(new(x, y - dy), Material.Floor);
        a.Hero.Position = a.World.State.Position = new(100, y); a.Enemy.Position = new(103, y);
        SentinelWait(a, 3); Assert.Single(a.Combat.State.Mirrors);
        a.Hero.Position = new(104, y);
        var shooter = RevivalTestEnemy(a, "thornspitter", new(100, y), a.Combat.Clock + 1);
        SentinelWait(a, 1);
        var attack = Assert.Single(a.Combat.State.Attacks, x => x.Source == shooter.Id);
        Assert.Equal(4, attack.Reflection!.Range); Assert.Equal(2, attack.Reflection.Step);
        Assert.Equal(new[] { new WorldPosition(101, y), new(102, y) }, attack.Cells);
        string directory = Path.Combine(Path.GetTempPath(), "roag-mirror-edge-" + Guid.NewGuid().ToString("N"));
        try
        {
            a.World.Save(directory);
            using var loaded = WorldSession.Load(directory); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
