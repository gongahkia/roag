using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void ToadPublicCommandsMatchOriginalInterceptionCarryingDeathAndCompetition()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-toad-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 50);
            var origin = a.Hero.Position;
            EnemyProfile(a, "gulp_toad"); a.Enemy.Position = origin.Offset(2, 0); a.Enemy.Health = 3;
            a.Hero.Health -= input.GetProperty("missing").GetInt32();
            a.Friend.Position = new(110, 110);
            if (name == "competing")
            {
                a.Friend.Position = origin.Offset(1, 1);
                a.Combat.RegisterEnemy(a.Friend, "core:enemy/gulp_toad", 50);
            }
            bool currency = input.GetProperty("kind").GetString() == "currency";
            a.Combat.SpawnLoot(currency ? GroundLootKind.Currency : GroundLootKind.Health, currency ? 7 : 10,
                origin.Offset(input.GetProperty("drop_x").GetInt32(), 0));
            a.World.State.Inventory["gold"] = 0; a.Combat.State.RandomState = 37; a.World.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() switch { "move" => TacticalAction.Move, "basic_attack" => TacticalAction.BasicAttack, _ => TacticalAction.Wait });
                for (int i = 0; ; i++)
                {
                    Assert.True(i < 30, name + " did not return control");
                    var actor = a.Combat.AdvanceToDecision(); Assert.NotNull(actor);
                    if (actor.Id == a.Hero.Id) break;
                    a.Combat.CommitEnemyTurn(a.Hero.Id, (from, goals) => EnemyPlanningTests.FirstStep(from, goals,
                        p => p.X is >= 95 and <= 110 && p.Y is >= 95 and <= 110 && a.World.CanEnter(p, actor.Id)));
                }
                var expected = sample.GetProperty("expected")[index++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first).Select(e => e.Kind switch
                {
                    "WindupStarted" or "SupportResolved" or "AttackPerformed" => $"{e.Kind}:{e.Time}:{e.Ability}",
                    "DropSwallowed" or "DropRegurgitated" or "CurrencyCollected" or "CurrencyDropped" or "SupplyCollected"
                        => $"{e.Kind}:{e.Time}:{e.Loot!.Drop}:{e.Amount}",
                    "GulpBlocked" => $"{e.Kind}:{e.Time}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}",
                    _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), a.World.Count("gold"));
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), expected.GetProperty("y").GetInt32()), a.Hero.Position);
                Assert.Equal(origin.Offset(expected.GetProperty("enemy_x").GetInt32(), expected.GetProperty("enemy_y").GetInt32()), a.Enemy.Position);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                Assert.Equal(expected.GetProperty("drops").EnumerateArray().Select(d => (d.GetProperty("id").GetInt64(),
                        d.GetProperty("kind").GetString()!, d.GetProperty("amount").GetInt32(), d.GetProperty("x").GetInt32(),
                        d.GetProperty("y").GetInt32(), d.GetProperty("carrier").GetInt32())),
                    a.Combat.State.Loot.Drops.Select(d => (d.Id, d.Kind == GroundLootKind.Currency ? "currency" : "health", d.Amount,
                        d.Position.X - origin.X, d.Position.Y - origin.Y, d.CarriedBy == a.Enemy.Id ? 1 : d.CarriedBy == a.Friend.Id ? 2 : 0)));
                foreach (var (key, kind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                    Assert.Equal(expected.GetProperty(key).ValueKind == JsonValueKind.Null ? (long?)null : expected.GetProperty(key).GetInt64(),
                        a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == kind)?.Due);
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(29, decisions);
    }

    [Fact]
    public void ToadChoosesOffscreenLooseLootByDistanceThenIdentityWithoutMutatingState()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "gulp_toad"); a.Enemy.Position = new(102, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Combat.SpawnLoot(GroundLootKind.Currency, 1, new(102, 100)); // other floor, zero horizontal distance
        var first = a.Combat.SpawnLoot(GroundLootKind.Currency, 7, new(103, 100, 1));
        a.Combat.SpawnLoot(GroundLootKind.Health, 10, new(101, 100, 1));
        a.Act(TacticalAction.Wait); Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.World.Visible.Clear(); string before = JsonSerializer.Serialize(a.World.State);
        var intent = a.Combat.PreviewEnemyTurn(a.Friend.Id, (_, _) => null);
        Assert.Equal(first.Id, intent.Support!.Drop);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next();
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(a.Enemy.Id, a.Combat.State.Loot.Drops.Single(d => d.Id == first.Id).CarriedBy);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("wall")]
    [InlineData("corner")]
    [InlineData("mist")]
    [InlineData("guarded")]
    [InlineData("distant")]
    public void ToadCannotTargetPhysicallyUnreachableOrGuardedLoot(string obstruction)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "gulp_toad"); a.Enemy.Position = new(102, 100);
        var drop = a.Combat.SpawnLoot(GroundLootKind.Currency, 7,
            obstruction == "corner" ? new(104, 102) : obstruction == "distant" ? new(107, 100) : new(104, 100));
        if (obstruction == "wall") a.World.Chunks.Edit(new(103, 100), Material.Wall);
        if (obstruction == "corner") a.World.Chunks.Edit(new(103, 100), Material.Wall);
        if (obstruction == "guarded") a.Friend.Position = drop.Position;
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        IReadOnlySet<WorldPosition>? goals = null;
        var intent = a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, g) => { goals = g; return null; },
            p => obstruction == "mist" && p == new WorldPosition(103, 100));
        Assert.Null(intent.Support); Assert.NotNull(goals); Assert.DoesNotContain(drop.Position, goals);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("carrying")]
    [InlineData("intercepted")]
    public void ToadActualWorldSaveResumesPendingCargoAndAlreadyCollectedTargets(string phase)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(102, 100);
        var drop = a.Combat.SpawnLoot(GroundLootKind.Currency, 7, new(101, 100));
        var attack = BeginExpansion(a, "gulp_toad"); a.Next();
        if (phase == "carrying") { a.Act(TacticalAction.Wait); a.Next(); }
        if (phase == "intercepted")
        {
            a.Act(TacticalAction.Move);
            Assert.Empty(a.Combat.State.Loot.Drops);
            Assert.Contains(a.Combat.State.Attacks, x => x.Support?.Drop == drop.Id);
        }
        string directory = Path.Combine(Path.GetTempPath(), "roag-toad-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            if (a.Combat.Active == null)
            { a.Next(); Assert.Equal(a.Hero.Id, resumed.AdvanceToDecision()!.Id); }
            a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            a.Next(); Assert.Equal(a.Hero.Id, resumed.AdvanceToDecision()!.Id);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.Equal(a.World.Count("gold"), loaded.Count("gold"));
            Assert.Equal(phase == "intercepted" ? 0 : 1, resumed.State.Loot.Drops.Count);
            if (phase != "intercepted") Assert.Equal(a.Enemy.Id, resumed.State.Loot.Drops.Single().CarriedBy);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void ToadCargoFollowsItsCarrierAcrossFloorsAndReleasesOnceAtItsDeathPosition()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(102, 100);
        var drop = a.Combat.SpawnLoot(GroundLootKind.Currency, 7, new(101, 100));
        EnemyProfile(a, "gulp_toad"); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision(); a.Act(TacticalAction.Wait);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision()); a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        a.Next(); a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(a.Enemy.Id, a.Combat.State.Loot.Drops.Single().CarriedBy);
        a.World.MoveTacticalActor(a.Enemy, new(103, 100, 1)); a.Friend.Position = new(102, 100, 1);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        int gold = a.World.Count("gold"); long next = a.Combat.State.Loot.NextDrop;
        a.Hit(a.Enemy, 1000, source: a.Friend);
        var released = a.Combat.State.Loot.Drops.Single(d => d.Id == drop.Id);
        Assert.Equal(drop with { Position = a.Enemy.Position }, released);
        Assert.Equal(next + 1, a.Combat.State.Loot.NextDrop); // ordinary kill reward is a distinct drop
        Assert.Equal(gold, a.World.Count("gold"));
        a.Hit(a.Enemy, 1000, source: a.Friend);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "DropRegurgitated");
        a.Next(a.Friend.Id); a.Act(TacticalAction.Move);
        Assert.Equal(gold + 7 + a.Enemy.Tactics!.CurrencyDrop, a.World.Count("gold"));
        Assert.Empty(a.Combat.State.Loot.Drops);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ToadCannotSwallowAnotherDropWhileCarryingOrRetargetAMovedDropDuringWindup()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(101, 100);
        var first = a.Combat.SpawnLoot(GroundLootKind.Currency, 7, a.Enemy.Position);
        var attack = BeginExpansion(a, "gulp_toad");
        a.Combat.State.Loot.Drops[0] = first with { Position = new(103, 100) };
        var second = a.Combat.SpawnLoot(GroundLootKind.Currency, 9, a.Enemy.Position);
        FinishExpansion(a, attack);
        Assert.All(a.Combat.State.Loot.Drops, d => Assert.Equal(0, d.CarriedBy));
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "GulpBlocked");
        a.Combat.State.Loot.Drops[1] = second with { CarriedBy = a.Enemy.Id };
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        var intent = a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal("core:ability/toad_bite", intent.Ability); Assert.Null(intent.Support);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ToadDeathDuringGulpLeavesOriginalDropAndCancelsTheChannel()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(102, 100);
        var drop = a.Combat.SpawnLoot(GroundLootKind.Currency, 7, new(101, 100));
        var attack = BeginExpansion(a, "gulp_toad"); a.Hit(a.Enemy, 1000, source: a.Hero); FinishExpansion(a, attack);
        Assert.Equal(drop, a.Combat.State.Loot.Drops.Single(d => d.Id == drop.Id));
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind is "DropSwallowed" or "DropRegurgitated" or "SupportResolved");
    }

    [Fact]
    public void ToadRejectsForgedGulpIdentityGeometryAndUnallocatedDropsWithoutCommitting()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "gulp_toad"); var drop = a.Combat.SpawnLoot(GroundLootKind.Currency, 7, a.Enemy.Position);
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        string before = JsonSerializer.Serialize(a.Combat.State);
        Assert.Throws<ArgumentException>(() => Commit("core:ability/toad_gulp", a.Enemy.Position, null));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/toad_bite", a.Enemy.Position, new(EnemySupportKind.Gulp, Drop: drop.Id)));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/toad_gulp", a.Enemy.Position.Offset(1, 1), new(EnemySupportKind.Gulp, Drop: drop.Id)));
        Assert.Throws<ArgumentException>(() => Commit("core:ability/toad_gulp", a.Enemy.Position, new(EnemySupportKind.Gulp, Drop: a.Combat.State.Loot.NextDrop)));
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
        var attack = Commit("core:ability/toad_gulp", a.Enemy.Position, new(EnemySupportKind.Gulp, Drop: drop.Id));
        foreach (var invalid in new[] { attack with { Support = null }, attack with { Ability = "core:ability/toad_bite" },
            attack with { Cells = [a.Enemy.Position.Offset(1, 1)] },
            attack with { Support = attack.Support! with { Drop = 0 } },
            attack with { Support = attack.Support! with { Drop = a.Combat.State.Loot.NextDrop } },
            attack with { Support = attack.Support! with { Target = a.Hero.Id } } })
        {
            a.Combat.State.Attacks[0] = invalid;
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        }
        a.Combat.State.Attacks[0] = attack;
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        LockedAttack Commit(string ability, WorldPosition cell, EnemySupport? support)
            => a.Combat.CommitWindup(ability, [cell], AttackDelivery.Melee, DamageElement.Physical, 2, 100, 140, support: support);
    }

    [Fact]
    public void LootSaveRejectsInvalidCarriersAndMultipleDropsInsideOneToad()
    {
        using var a = new Arena(ClassKind.Adventurer);
        EnemyProfile(a, "gulp_toad");
        var drop = a.Combat.SpawnLoot(GroundLootKind.Currency, 7, a.Enemy.Position);
        a.Combat.State.Loot.Drops[0] = drop with { CarriedBy = a.Hero.Id };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Combat.State.Loot.Drops[0] = drop with { CarriedBy = a.Enemy.Id };
        a.Enemy.Health = 0;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Enemy.Health = 3;
        var second = a.Combat.SpawnLoot(GroundLootKind.Health, 10, a.Enemy.Position);
        a.Combat.State.Loot.Drops[1] = second with { CarriedBy = a.Enemy.Id };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Combat.State.Loot.Drops[1] = second;
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }
}
