using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void PrepareWeaver(Arena a)
    {
        EnemyProfile(a, "web_weaver"); a.Enemy.Position = new(103, 100); a.Friend.Position = new(110, 110);
        a.Hero.Tactics!.Build = CharacterBuild.Create(a.Hero.Tactics.Build!.Class, 37);
    }

    private static void Weave(Arena a)
    {
        for (int i = 0; i < 3; i++) { a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); }
        Assert.Equal(300, a.Combat.Clock); Assert.Single(a.Combat.State.Webs);
    }

    [Fact]
    public void WebWeaverPublicCommandsMatchOriginalCrossingDashArrowsExpiryAndRammerShove()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-web-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(input.GetProperty("class_name").GetString() == "ranger" ? ClassKind.Ranger : ClassKind.Adventurer, enemyDue: 50);
            PrepareWeaver(a); a.Combat.State.RandomState = 37; a.World.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                if (index == 3 && name == "blocked_arrow") RevivalTestEnemy(a, "mossbound_guard", new(101, 100)).Health = 2;
                if (index == 3 && name == "rammer") RevivalTestEnemy(a, "heavy_rammer", new(99, 100), 300);
                long first = a.Combat.State.NextEvent;
                var action = command.GetString() switch { "basic_attack" => TacticalAction.BasicAttack, "move" => TacticalAction.Move,
                    "dash" => TacticalAction.Dash, _ => TacticalAction.Wait };
                a.Act(action, name == "rammer" && action == TacticalAction.Move ? Facing.North : Facing.East);
                RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "SupportResolved" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "WebCreated" or "WebBlocked" or "WebCrossed" or "WebAnchorCut" => $"{e.Kind}:{e.Time}:{e.Position.X - 100}:{e.Position.Y - 100}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}", _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(new WorldPosition(100 + expected.GetProperty("x").GetInt32(), 100 + expected.GetProperty("y").GetInt32()), a.Hero.Position);
                Assert.Equal(expected.GetProperty("xp").GetInt32(), a.Hero.Tactics!.Build!.Experience);
                if (a.Hero.Tactics.Build.Experience > 0)
                {
                    Assert.Equal(expected.GetProperty("random").GetInt32(), a.Hero.Tactics.Build.RandomState);
                    Assert.Equal(37, a.Combat.State.RandomState);
                }
                else Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(expected.GetProperty("casts").GetInt32(), a.Enemy.Tactics!.Enemy!.WebCasts);
                Assert.Equal(expected.GetProperty("webs").EnumerateArray().Select(w =>
                    (w.GetProperty("ax").GetInt32(), w.GetProperty("ay").GetInt32(), w.GetProperty("bx").GetInt32(), w.GetProperty("by").GetInt32(), w.GetProperty("expires").GetInt64())),
                    a.Combat.State.Webs.Select(w => (w.Span.A.X - 100, w.Span.A.Y - 100, w.Span.B.X - 100, w.Span.B.Y - 100, w.Expires)));
                foreach (var (key, kind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                    Assert.Equal(expected.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : expected.GetProperty(key).GetInt64(),
                        a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == kind)?.Due);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(37, decisions);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("active")]
    [InlineData("crossed")]
    public void WebWorldSaveContinuesActualActorsBudgetGeometryAndCrossing(string phase)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareWeaver(a);
        if (phase == "pending") { a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); }
        else Weave(a);
        if (phase == "crossed") { a.Act(TacticalAction.Move); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); }
        string directory = Path.Combine(Path.GetTempPath(), "roag-web-" + Guid.NewGuid().ToString("N"));
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
    public void WebOffscreenPlacementIsPrivateAndExpiresAtTheExactSharedClockBoundary()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 50);
        EnemyProfile(a, "web_weaver"); a.Enemy.Position = new(103, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.World.Visible.Clear(); string before = JsonSerializer.Serialize(a.World.State);
        var preview = a.Combat.PreviewEnemyTurn(a.Friend.Id, (_, _) => null);
        Assert.Equal(EnemySupportKind.Web, preview.Support!.Kind); Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next(); Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        for (int i = 0; i < 2; i++) { a.Act(TacticalAction.Wait); a.Next(); }
        var web = Assert.Single(a.Combat.State.Webs); Assert.Equal(1, web.Span.A.Floor); Assert.Equal(606, web.Expires);
        Assert.Equal(300, a.Combat.Clock);
        a.Act(TacticalAction.Wait);
        var ready = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id && w.Kind == ScheduledKind.ActorReady);
        Assert.True(a.Combat.State.Queue.Delay(ready.Sequence, web.Expires - ready.Due));
        a.Next(); Assert.Equal(web.Expires, a.Combat.Clock); Assert.Empty(a.Combat.State.Webs);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("death")]
    [InlineData("wall")]
    [InlineData("stairs")]
    public void WebOwnerDeathAndInvalidatedTerrainRemoveWholeLines(string invalidation)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareWeaver(a); Weave(a);
        var web = Assert.Single(a.Combat.State.Webs);
        if (invalidation == "death") a.Hit(a.Enemy, 1000, source: a.Hero);
        else
        {
            a.World.Chunks.Edit(web.Span.B, invalidation == "wall" ? Material.Wall : Material.StairsDown);
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
            a.Act(TacticalAction.Wait); a.Next();
        }
        Assert.Empty(a.Combat.State.Webs); TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void WebInterruptedAttemptsRemainSpentAndOnlyTwoMayBeCommitted()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareWeaver(a);
        a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
        Assert.Equal(1, a.Enemy.Tactics!.Enemy!.WebCasts);
        Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        Assert.Empty(a.Combat.State.Attacks); Assert.Empty(a.Combat.State.Webs);
        a.Next(a.Enemy.Id);
        var again = a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(EnemySupportKind.Web, again.Support!.Kind); Assert.Equal(2, a.Enemy.Tactics.Enemy.WebCasts);
        Apply(a, Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy), BoonEffectKind.StunTarget);
        a.Next(a.Enemy.Id);
        var spent = a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Null(spent.Support); Assert.Equal(2, a.Enemy.Tactics.Enemy.WebCasts);
        Assert.Empty(a.Combat.State.Webs); TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void WebCrossingByAnEnemyDelaysExistingReadinessWithoutAdditionalDamageOrRepeatedDelay()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 50); PrepareWeaver(a); Weave(a);
        var victim = RevivalTestEnemy(a, "mossbound_guard", new(101, 99), 800);
        var before = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == victim.Id);
        int hp = victim.Health, random = a.Combat.State.RandomState;
        // A real basic hit activates Anchor Maul and enters the web at (101,100).
        a.Hero.Position = new(101, 98);
        Grant(a.Hero, "core:boon/anchor_maul");
        int attackDamage = Math.Max(0, a.Hero.Power - victim.Armor);
        a.Act(TacticalAction.BasicAttack, Facing.South);
        Assert.Equal(new WorldPosition(101, 100), victim.Position);
        Assert.Empty(a.Combat.State.Webs);
        var after = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == victim.Id);
        Assert.Equal(before.Sequence, after.Sequence); Assert.Equal(850, after.Due);
        Assert.Equal(hp - attackDamage, victim.Health); Assert.Equal(random, a.Combat.State.RandomState);
        Assert.Equal(0, victim.Tactics!.WebRecoveryDelay);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "WebCrossed" && e.Target == victim.Id);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void WebPlanningAvoidsProtectedAnchorsAndUsesVerticalFallbackWithoutMutation()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0); PrepareWeaver(a);
        a.World.Chunks.Edit(new(98, 100), Material.StairsDown);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        string before = JsonSerializer.Serialize(a.World.State);
        var vertical = a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(new WebSpan(new(100, 98), new(100, 102)), vertical.Support!.Web);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.World.Chunks.Edit(new(100, 98), Material.Wall);
        Assert.Null(a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null).Support);
        Assert.Equal(0, a.Enemy.Tactics!.Enemy!.WebCasts);
    }

    [Fact]
    public void WebHookPullDuringEnemyWindupPersistsDeferredDelayAndChargesItOnlyOnce()
    {
        using var a = new Arena(ClassKind.Hookrunner, enemyDue: 50); PrepareWeaver(a);
        a.World.Chunks.Edit(new(98, 100), Material.StairsDown);
        for (int i = 0; i < 3; i++) { a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id); }
        Assert.Equal(261, a.Combat.Clock);
        Assert.Equal(new WebSpan(new(100, 98), new(100, 102)), Assert.Single(a.Combat.State.Webs).Span);
        a.Hero.Position = new(99, 101);
        var victim = RevivalTestEnemy(a, "thornspitter", new(101, 101), a.Combat.Clock);
        a.Act(TacticalAction.Wait); RunRevivalEnemies(a.World, a.Combat, a.Hero.Id);
        Assert.Equal(348, a.Combat.Clock);
        var attack = Assert.Single(a.Combat.State.Attacks, p => p.Source == victim.Id); Assert.Equal(361, attack.Due);
        a.Act(TacticalAction.Special, Facing.East);
        Assert.Equal(new WorldPosition(100, 101), victim.Position); Assert.Empty(a.Combat.State.Webs);
        Assert.Equal(50, victim.Tactics!.WebRecoveryDelay);
        Assert.Contains(attack, a.Combat.State.Attacks);
        Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, w => w.Kind == ScheduledKind.ActorReady && w.ActorId == victim.Id);
        string directory = Path.Combine(Path.GetTempPath(), "roag-web-delay-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            a.Next();
            while (resumed.AdvanceToDecision() is { } actor && actor.Id != a.Hero.Id)
                Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            Assert.Equal(474, a.Combat.Clock);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
            Assert.Equal(50, victim.Tactics.WebRecoveryDelay);
            a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            Assert.Equal(victim.Id, a.Combat.AdvanceToDecision()!.Id);
            Assert.Equal(victim.Id, resumed.AdvanceToDecision()!.Id);
            Assert.Equal(555, a.Combat.Clock); Assert.Equal(0, victim.Tactics.WebRecoveryDelay);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void WebCapIsRecheckedAtImpactAndOverlappingSpansCannotBeSaved()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0); PrepareWeaver(a);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        var owners = Enumerable.Range(0, 4).Select(i => RevivalTestEnemy(a, "web_weaver", new(106 + i, 109))).ToArray();
        foreach (var owner in owners) owner.Tactics!.Enemy!.WebCasts = 2;
        for (int i = 0; i < 7; i++) a.Combat.State.Webs.Add(new(a.Combat.State.NextAction++, owners[i / 2].Id,
            new(new(95, 95 + i), new(99, 95 + i)), 400));
        var intent = a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(EnemySupportKind.Web, intent.Support!.Kind);
        a.Combat.State.Webs.Add(new(a.Combat.State.NextAction++, owners[3].Id, new(new(95, 102), new(99, 102)), 400));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        a.Next(); a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(8, a.Combat.State.Webs.Count);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "WebBlocked" && e.Source == a.Enemy.Id);
        Assert.Equal(1, a.Enemy.Tactics!.Enemy!.WebCasts);
        var last = a.Combat.State.Webs[^1];
        a.Combat.State.Webs[^1] = last with { Span = a.Combat.State.Webs[0].Span };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Combat.State.Webs[^1] = last;
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void WebValidationRejectsMalformedSpansUnspentBudgetsAndExpiredOrUnownedLines()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0); PrepareWeaver(a);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        var span = new WebSpan(new(98, 100), new(102, 100));
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<ArgumentException>(() => Commit("core:ability/weaver_web", span.Cells(), null));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/weaver_bite", span.Cells(), new(EnemySupportKind.Web, Web: span)));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/weaver_web", span.Cells().Reverse().ToArray(), new(EnemySupportKind.Web, Web: span)));
        Assert.Throws<InvalidDataException>(() => Commit("core:ability/weaver_web", span.Cells(), new(EnemySupportKind.Web, Web: new(new(98, 100), new(103, 100)))));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); a.Next();
        a.Enemy.Tactics!.Enemy!.WebCasts = 0;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Enemy.Tactics.Enemy.WebCasts = 1;
        a.Act(TacticalAction.Wait); a.Next();
        var web = Assert.Single(a.Combat.State.Webs);
        foreach (var bad in new[] { web with { Source = a.Hero.Id }, web with { Expires = a.Combat.Clock },
            web with { Span = new(new(98, 100), new(102, 101)) } })
        {
            a.Combat.State.Webs[0] = bad;
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        }
        a.Combat.State.Webs[0] = web;
        a.Enemy.Tactics.Enemy.WebCasts = 0;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Enemy.Tactics.Enemy.WebCasts = 1; TacticalValidation.Validate(a.World.Plan, a.World.State);
        LockedAttack Commit(string ability, WorldPosition[] cells, EnemySupport? support)
            => a.Combat.CommitWindup(ability, cells, AttackDelivery.Melee, DamageElement.Physical, 1, 140, 140, support: support);
    }
}
