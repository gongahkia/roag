using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void RammerPublicCommandsMatchOriginalShovesBlockedLandingsTrapsLootAndRecovery()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-rammer-turns.json")));
        int decisions = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = sample.GetProperty("input").GetProperty("name").GetString()!;
            using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
            var origin = a.Hero.Position; var landing = origin.Offset(1, 0);
            EnemyProfile(a, "heavy_rammer"); a.Enemy.Position = origin.Offset(-1, 0); a.Friend.Position = new(110, 110);
            if (name == "wall") a.World.Chunks.Edit(landing, Material.Wall);
            if (name == "stairs") a.World.Chunks.Edit(landing, Material.StairsUp);
            if (name == "occupied")
            { a.Friend.Position = landing; a.Combat.RegisterEnemy(a.Friend, "core:enemy/mossbound_guard", 10000); }
            if (name == "snare")
            {
                a.Friend.Position = origin.Offset(4, 3); a.Combat.RegisterEnemy(a.Friend, "core:enemy/snare_scribe", 10000);
                a.Combat.State.EnemySnares.Add(new(a.Friend.Id, landing));
            }
            a.World.State.Inventory["gold"] = 0;
            if (name == "currency") a.Combat.SpawnLoot(GroundLootKind.Currency, 9, landing);
            a.Combat.State.RandomState = 37; a.World.Reveal(); int index = 0;
            foreach (var command in sample.GetProperty("input").GetProperty("commands").EnumerateArray())
            {
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() == "move" ? TacticalAction.Move : TacticalAction.Wait, Facing.North);
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
                    "WindupStarted" => $"windup:{e.Time}:{e.Ability}",
                    "DamageTaken" => $"damage:{e.Time}:{e.Amount}",
                    "Displaced" => $"shove:{e.Time}:{e.Position.X - origin.X}:{e.Position.Y - origin.Y}",
                    "ShoveBlocked" => $"blocked:{e.Time}:{e.Ability}",
                    "EnemySnareTriggered" => $"snare:{e.Time}",
                    _ => null
                }).Where(e => e != null).ToArray();
                Assert.True(expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()).SequenceEqual(trace), name + " / " + index + ": " + string.Join(", ", trace));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), a.Hero.Health);
                Assert.Equal(expected.GetProperty("enemy_hp").GetInt32(), a.Enemy.Health);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), expected.GetProperty("y").GetInt32()), a.Hero.Position);
                Assert.Equal(origin.Offset(expected.GetProperty("enemy_x").GetInt32(), expected.GetProperty("enemy_y").GetInt32()), a.Enemy.Position);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                Assert.Equal(expected.GetProperty("traps").GetInt32(), a.Combat.State.EnemySnares.Count);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), a.World.State.Inventory["gold"]);
                foreach (var (key, kind) in new[] { ("ready", ScheduledKind.ActorReady), ("impact", ScheduledKind.Impact) })
                {
                    long? due = expected.GetProperty(key).ValueKind == JsonValueKind.Null ? null : expected.GetProperty(key).GetInt64();
                    Assert.Equal(due, a.Combat.State.Queue.Snapshot().Pending.SingleOrDefault(w => w.ActorId == a.Enemy.Id && w.Kind == kind)?.Due);
                }
                TacticalValidation.Validate(a.World.Plan, a.World.State); decisions++;
            }
        }
        Assert.Equal(21, decisions);
    }

    [Fact]
    public void RammerShovesTheCurrentOccupantAndCancelsItsPendingEnemyAttack()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(99, 100);
        var attack = BeginExpansion(a, "heavy_rammer"); a.Next();
        a.Hero.Position = new(100, 99); a.Friend.Position = attack.Cells[0];
        a.Combat.RegisterEnemy(a.Friend, "core:enemy/mossbound_guard", a.Combat.Clock);
        a.Act(TacticalAction.Wait); Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Combat.CommitWindup("test:interrupted", [a.Hero.Position], AttackDelivery.Melee, DamageElement.Physical, 1, 500, 100);
        a.Next();
        Assert.Equal(new WorldPosition(101, 100), a.Friend.Position);
        Assert.DoesNotContain(a.Combat.State.Attacks, x => x.Source == a.Friend.Id);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "WindupCancelled" && e.Target == a.Friend.Id && e.Ability == "displacement");
        Assert.Equal(attack.Due + ActionTiming.Recovery(100, a.Friend.Speed),
            a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Friend.Id && w.Kind == ScheduledKind.ActorReady).Due);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void RammerLockedShoveAndForcedLandingResumeThroughActualWorldSave()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(99, 100); var attack = BeginExpansion(a, "heavy_rammer"); a.Next();
        string directory = Path.Combine(Path.GetTempPath(), "roag-rammer-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(attack.Shove, Assert.Single(resumed.State.Attacks).Shove);
            a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            a.Next(); Assert.Equal(a.Hero.Id, resumed.AdvanceToDecision()!.Id);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            Assert.Equal(a.Hero.Position, loaded.State.Actors.Single(x => x.Id == a.Hero.Id).Position);
            Assert.Equal(a.Hero.Health, loaded.State.Actors.Single(x => x.Id == a.Hero.Id).Health);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(Facing.NorthEast, false)]
    [InlineData(Facing.East, true)]
    public void InvalidShoveStateIsRejectedWithoutACommandMutation(Facing facing, bool outside)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "heavy_rammer"); a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        string before = JsonSerializer.Serialize(a.Combat.State);
        var shove = new EnemyShove(outside ? a.Hero.Position.Offset(1, 0) : a.Hero.Position, facing);
        void Commit() => a.Combat.CommitWindup("test:shove", [a.Hero.Position], AttackDelivery.Melee,
            DamageElement.Physical, 2, 100, 100, shove: shove);
        if (outside) Assert.Throws<ArgumentException>(Commit);
        else Assert.Throws<InvalidDataException>(Commit);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
    }

    [Fact]
    public void RammerCanResolveOnAnotherFloorWithoutRevealingItsIntentToTheSelectedMember()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "heavy_rammer"); a.Enemy.Position = new(99, 100, 1); a.Friend.Position = new(100, 100, 1);
        a.Act(TacticalAction.Wait); Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitEnemyTurn(a.Friend.Id, (_, _) => null); a.Next();
        Assert.Same(a.Hero, a.Combat.Active);
        Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        Assert.DoesNotContain(a.Combat.VisibleTimeline(), e => e.ActorId == a.Enemy.Id);
        int hp = a.Friend.Health;
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(new WorldPosition(101, 100, 1), a.Friend.Position); Assert.Equal(hp - 2, a.Friend.Health);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void BoonShovesRespectProtectedStairsAndKeepTheBlockedTargetsWindup()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        EnemyProfile(a, "mossbound_guard"); a.Enemy.Position = new(101, 100);
        a.World.Chunks.Edit(new(102, 100), Material.StairsDown); Grant(a.Hero, "core:boon/anchor_maul");
        a.Act(TacticalAction.Wait); a.Combat.AdvanceToDecision();
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); a.Next();
        long pending = Assert.Single(a.Combat.State.Attacks).Id;
        a.Act(TacticalAction.BasicAttack);
        Assert.Equal(new WorldPosition(101, 100), a.Enemy.Position);
        Assert.Contains(a.Combat.State.Attacks, x => x.Id == pending);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "ShoveBlocked" && e.Ability == "stairs");
    }

    [Theory]
    [InlineData("outside")]
    [InlineData("direction")]
    [InlineData("identity")]
    public void SavedRammerShoveRejectsForgedFootprintsDirectionsAndIdentity(string corruption)
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(99, 100); var attack = BeginExpansion(a, "heavy_rammer");
        a.Combat.State.Attacks[0] = corruption switch
        {
            "outside" => attack with { Shove = attack.Shove! with { Cell = attack.Shove!.Cell.Offset(1, 0) } },
            "direction" => attack with { Shove = attack.Shove! with { Facing = Facing.West } },
            _ => attack with { Ability = "core:ability/wraith_touch" }
        };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }
}
