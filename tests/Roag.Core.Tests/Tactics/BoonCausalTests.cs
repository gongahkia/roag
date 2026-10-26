using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void PublicCommandProcChainsMatchOriginalGameIncludingDelayedWorkAndRandomConsumption()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Tactics/Fixtures/original-boon-chains.json")));
        int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            string name = input.GetProperty("name").GetString()!;
            using var a = new Arena(ClassCatalog.Get("core:actor/" + input.GetProperty("class").GetString()).Kind);
            a.Hero.Health -= 5;
            a.Friend.Position = new(110, 110); // Remove the unrelated resident from the reference arena.
            var actors = new List<Actor> { a.Hero };
            foreach (var enemy in input.GetProperty("enemies").EnumerateArray())
            {
                var actor = actors.Count == 1 ? a.Enemy : new Actor
                {
                    Id = a.World.State.NextActorId++, Kind = ActorKind.Raider, Archetype = "bandit", Health = 1
                };
                actor.Position = a.Hero.Position.Offset(enemy.GetProperty("dx").GetInt32(), enemy.GetProperty("dy").GetInt32());
                var tactics = new ActorTactics { InnateStats = new(1000, 6, 6,
                    enemy.TryGetProperty("armor", out var armor) ? armor.GetInt32() : 0, 100, 0, 150),
                    DefinitionId = "core:enemy/mossbound_guard", RegularEnemy = true };
                if (actors.Count == 1) actor.Tactics = tactics;
                else { a.World.State.Actors.Add(actor); a.Combat.Register(actor, tactics, 100000); }
                actor.Health = 1000; actors.Add(actor);
            }
            foreach (var grant in input.GetProperty("grants").EnumerateArray())
            {
                var actor = actors[grant.GetProperty("owner").GetInt32() - 1];
                actor.Tactics!.Boons.Inventory = actor.Tactics.Boons.Inventory.Grant("core:boon/" + grant.GetProperty("boon").GetString(),
                    BoonRarity.Common, actor.Tactics.Build?.Class, grant.GetProperty("count").GetInt32());
            }
            a.Combat.State.RandomState = input.GetProperty("seed").GetInt32();
            a.World.Reveal();
            int step = 0;
            int Index(long id) => actors.FindIndex(actor => actor.Id == id) + 1;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                long first = a.Combat.State.NextEvent;
                a.Act(command.GetString() switch
                {
                    "basic_attack" => TacticalAction.BasicAttack, "sweeping_slash" => TacticalAction.Special,
                    "move" => TacticalAction.Move, "dash" => TacticalAction.Dash, _ => TacticalAction.Wait
                });
                a.Next();
                var expected = sample.GetProperty("expected")[step++];
                var trace = a.Combat.State.Events.Where(e => e.Sequence >= first && e.Kind is
                    "BoonActivated" or "DamageTaken" or "DamageBlocked" or "HitConfirmed" or "Healed")
                    .Select(e => e.Kind == "BoonActivated" ? $"{e.Kind}:{Index(e.Source)}:{e.Ability}"
                        : $"{e.Kind}:{Index(e.Source)}:{Index(e.Target)}:{e.Amount}").ToArray();
                var original = expected.GetProperty("trace").EnumerateArray().Select(e => e.GetString()!).ToArray();
                Assert.True(original.SequenceEqual(trace), $"{name}, decision {step}\nOriginal: {string.Join(", ", original)}\nNative: {string.Join(", ", trace)}");
                Assert.Equal(expected.GetProperty("health").EnumerateArray().Select(e => e.GetInt32()), actors.Select(actor => actor.Health));
                Assert.Equal(expected.GetProperty("clock").GetInt64(), a.Combat.Clock);
                Assert.Equal(expected.GetProperty("random").GetInt32(), a.Combat.State.RandomState);
                TacticalValidation.Validate(a.World.Plan, a.World.State);
                count++;
            }
        }
        Assert.Equal(75, count);
    }

    [Fact]
    public void WindupReactionsUseEachOwnersSightWithoutChangingSelectedVision()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        a.Friend.Position = new(105, 100); a.Enemy.Position = new(106, 100);
        for (int y = 95; y <= 110; y++) a.World.Chunks.Edit(new(103, y), Material.Wall);
        Grant(a.Hero, "core:boon/windup_hunter"); Grant(a.Friend, "core:boon/windup_hunter");
        a.Act(TacticalAction.Wait); a.Next(a.Friend.Id); a.Act(TacticalAction.Wait);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        var viewpoint = a.World.State.Position; var vision = a.World.Visible.ToHashSet();
        Assert.Contains(a.Enemy.Position, vision);
        int hp = a.Enemy.Health;
        a.Combat.CommitWindup("test:windup", [a.Hero.Position, a.Friend.Position], AttackDelivery.Area, DamageElement.Fire, 2, 50, 100);
        var activation = Assert.Single(a.Combat.State.Events, e => e.Kind == "BoonActivated");
        Assert.Equal(a.Friend.Id, activation.Source);
        Assert.True(a.Enemy.Health < hp);
        Assert.Equal(viewpoint, a.World.State.Position); Assert.True(vision.SetEquals(a.World.Visible));
        Assert.Single(a.Combat.State.BoonEffects.Dispatch.Chains);
        a.Next();
        Assert.Empty(a.Combat.State.BoonEffects.Dispatch.Chains);
    }

    [Fact]
    public void RealCommandBurnEchoAndAncestryResumeExactlyFromWorldSave()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100);
        Grant(a.Hero, "core:boon/emberbrand"); Grant(a.Hero, "core:boon/temporal_echo");
        Grant(a.Hero, "core:boon/resonant_wounds"); Grant(a.Friend, "core:boon/temporal_echo");
        a.Act(TacticalAction.BasicAttack);
        var echo = Assert.Single(a.Combat.State.BoonEffects.Echoes).Value;
        Assert.Equal(a.Hero.Id, echo.Activation.OwnerId);
        Assert.Contains(new BoonActivationKey(a.Hero.Id, "core:boon/temporal_echo"), echo.Activation.Ancestry);
        Assert.NotNull(a.Enemy.Tactics!.Burning); Assert.Single(a.Combat.State.BoonEffects.Dispatch.Chains);
        string directory = Path.Combine(Path.GetTempPath(), "roag-causal-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json"); a.World.Save(slot);
            using var restored = WorldSession.Load(slot); var combat = new TacticalCombat(restored, _ => { });
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(a.Combat.AdvanceToDecision()!.Id, combat.AdvanceToDecision()!.Id);
                Assert.True(a.Combat.Commit(new(TacticalAction.Wait)).Success);
                Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
            }
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(combat.State));
            Assert.Equal(a.Enemy.Health, restored.State.Actors.Single(actor => actor.Id == a.Enemy.Id).Health);
            Assert.Empty(combat.State.BoonEffects.Dispatch.Chains);
            TacticalValidation.Validate(restored.Plan, restored.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void LethalBatchTriggersKillBoonOnceAndDeadEnemyCannotReflectFromItsCorpse()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100); a.Enemy.Health = 1; a.Hero.Health -= 5;
        Grant(a.Hero, "core:boon/grave_dividend"); Grant(a.Enemy, "core:boon/thorn_mirror");
        a.Act(TacticalAction.BasicAttack);
        Assert.Equal(LifeState.Dead, a.Enemy.Life); Assert.Equal(a.Hero.MaxHealth - 4, a.Hero.Health);
        var activation = Assert.Single(a.Combat.State.Events, e => e.Kind == "BoonActivated");
        Assert.Equal("core:boon/grave_dividend", activation.Ability);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "EntityKilled");
        a.Next(); a.Act(TacticalAction.BasicAttack);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "EntityKilled");
    }

    [Fact]
    public void ReactiveDowningPreservesTheActorsAlreadyPaidRecovery()
    {
        using var a = new Arena(ClassKind.Berserker, ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100); a.Hero.Health = 1;
        a.Enemy.Tactics!.Boons.Inventory = a.Enemy.Tactics.Boons.Inventory.Grant("core:boon/thorn_mirror", BoonRarity.Common, null, 20);
        long due = a.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Recovery;
        a.Act(TacticalAction.BasicAttack);
        Assert.Equal(LifeState.Downed, a.Hero.Life); Assert.Equal(5, a.Hero.Tactics!.BleedTurns);
        Assert.Equal(due, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id).Due);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void DodgingLockedTilesPrimesReversalForOneLaterHitOnThatEnemy()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Position = new(101, 99); Grant(a.Hero, "core:boon/ring_of_reversal");
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitWindup("test:windup", [a.Hero.Position], AttackDelivery.Melee, DamageElement.Physical, 5, 200, 500);
        a.Next(); a.Act(TacticalAction.Move, Facing.North);
        Assert.Equal(a.Enemy.Id, a.Hero.Tactics!.Boons.ReversalTarget);
        a.Next(); int hp = a.Enemy.Health;
        a.Act(TacticalAction.BasicAttack);
        Assert.Equal(hp - a.Hero.Power - 2, a.Enemy.Health); Assert.Null(a.Hero.Tactics.Boons.ReversalTarget);
        a.Next(); hp = a.Enemy.Health; a.Act(TacticalAction.BasicAttack);
        Assert.Equal(hp - a.Hero.Power, a.Enemy.Health);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "RingReversalSpent");
    }

    [Fact]
    public void DashCrossingASnareTriggersAtTheEnteredTileAndCarriesOrdinaryHazardProcs()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Tactics!.Build = CharacterBuild.Create(ClassKind.Trapper, 44); a.Enemy.Tactics.InnateStats = null;
        a.Enemy.Health = a.Enemy.MaxHealth;
        a.Enemy.Tactics.Resources.Snare = new(101, 100);
        Grant(a.Hero, "core:boon/thorn_mirror");
        a.Act(TacticalAction.Dash);
        Assert.Equal(new WorldPosition(102, 100), a.Hero.Position);
        Assert.Equal(a.Hero.MaxHealth - 3, a.Hero.Health); Assert.Equal(a.Enemy.MaxHealth - 1, a.Enemy.Health);
        Assert.Null(a.Enemy.Tactics.Resources.Snare); Assert.Equal(a.Enemy.Id, a.Hero.Tactics!.SnaredBy);
        Assert.True(a.Combat.State.Events.FindIndex(e => e.Kind == "Moved") < a.Combat.State.Events.FindIndex(e => e.Kind == "TileEntered"));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void WorldValidationRejectsDanglingHistoryAndFutureCommittedCausality()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        Grant(a.Hero, "core:boon/harriers_response");
        a.Act(TacticalAction.Wait); Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitWindup("test:windup", [a.Hero.Position], AttackDelivery.Melee, DamageElement.Physical, 5, 200, 300);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        var attack = Assert.Single(a.Combat.State.Attacks);
        a.Combat.State.Attacks[0] = attack with { Causality = attack.Causality! with { ChainId = a.Combat.State.NextAction } };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        a.Combat.State.Attacks[0] = attack;
        var chain = Assert.Single(a.Combat.State.BoonEffects.Dispatch.Chains).Value;
        chain.Damages.Add(new(long.MaxValue, "enemy", a.Enemy.Position));
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }

    [Theory]
    [InlineData(ClassKind.Stormcaller)]
    [InlineData(ClassKind.Tinkerer)]
    public void AutonomousClassDamageDoesNotTriggerEitherOwnersOrdinaryAttackBoons(ClassKind kind)
    {
        using var a = new Arena(kind);
        Grant(a.Hero, "core:boon/temporal_echo"); Grant(a.Hero, "core:boon/emberbrand");
        a.Hero.Tactics!.Boons.Inventory = a.Hero.Tactics.Boons.Inventory.Grant("core:boon/storm_conductor", BoonRarity.Common, kind, 50);
        Grant(a.Enemy, "core:boon/thorn_mirror");
        if (kind == ClassKind.Stormcaller) a.Hero.Tactics.Resources.StormCharge = 1;
        int hp = a.Enemy.Health;
        a.Act(TacticalAction.Special);
        for (int i = 0; i < 5; i++) { a.Next(); a.Act(TacticalAction.Wait); }
        Assert.True(a.Enemy.Health < hp);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "BoonActivated");
        Assert.Null(a.Enemy.Tactics!.Burning); Assert.Empty(a.Combat.State.BoonEffects.Echoes);
        Assert.Empty(a.Combat.State.BoonEffects.Dispatch.Chains);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }
}
