using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void Grant(Actor actor, string boon, BoonRarity rarity = BoonRarity.Common)
    {
        var tactics = actor.Tactics!;
        tactics.Boons.Inventory = tactics.Boons.Inventory.Grant(boon, rarity, tactics.Build?.Class);
    }

    [Fact]
    public void TimedBoonSpeedChangesOnlyTheOwningMembersFutureCommittedRecovery()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        Grant(a.Hero, "core:boon/bloodrush");
        a.Hero.Tactics!.Boons.ApplyTimed("core:boon/bloodrush", BoonEffectKind.SpeedBoost, 100, 100, a.Combat.Clock);
        Assert.Equal(50, a.Combat.Preview(new(TacticalAction.Wait)).Recovery);
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        Assert.Equal(100, a.Combat.Preview(new(TacticalAction.Wait)).Recovery);
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Hero, a.Combat.AdvanceToDecision()); Assert.Equal(50, a.Combat.Clock);
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision()); a.Act(TacticalAction.Wait);
        Assert.Same(a.Hero, a.Combat.AdvanceToDecision()); Assert.Equal(100, a.Combat.Clock);
        Assert.Equal(50, a.Combat.Preview(new(TacticalAction.Wait)).Recovery); // inclusive expiry
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(150, a.Combat.Clock);
        Assert.Equal(100, a.Combat.Preview(new(TacticalAction.Wait)).Recovery);
        Assert.Equal(100, a.Hero.Speed); // permanent and gear/needs stats were not rewritten
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ACommittedRecoveryIsNotRewrittenWhenItsSpeedEffectExpiresOffscreen()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        Grant(a.Hero, "core:boon/bloodrush");
        a.Hero.Tactics!.Boons.ApplyTimed("core:boon/bloodrush", BoonEffectKind.SpeedBoost, 100, 1, 0);
        a.Act(TacticalAction.Special); // 150-cost slash is committed with recovery 75
        a.Hero.Position = new(100, 100, 1);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision()); a.Act(TacticalAction.Wait);
        Assert.Same(a.Hero, a.Combat.AdvanceToDecision());
        Assert.Equal(75, a.Combat.Clock);
        Assert.Equal(150, a.Combat.Preview(new(TacticalAction.Special)).Recovery);
    }

    [Fact]
    public void DirectionalGuardReducesTheHitBeforeOneHitBoonWardsAreSpent()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        Grant(a.Hero, "core:boon/betrayal_ward"); Grant(a.Hero, "core:boon/beggars_cap");
        // Insert in reverse catalog order; betrayal must still be spent first.
        a.Hero.Tactics!.Boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 7, 100, 0);
        a.Hero.Tactics.Boons.ApplyTimed("core:boon/betrayal_ward", BoonEffectKind.Ward, 2, 100, 0);
        a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Guard);
        a.Hit(a.Hero, 8); // Guard absorbs four, first ward two, second ward two (remaining five discarded).
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.Equal(0, a.Hero.Tactics.Boons.WardTotal(0));
        var hit = a.Combat.State.Events.Last(e => e.Kind == "HitConfirmed");
        Assert.Equal(8, hit.Prevented);
        a.Hit(a.Friend, 8);
        Assert.Equal(a.Friend.MaxHealth - 8, a.Friend.Health);
    }

    [Fact]
    public void ArmorOrClassProtectionDoesNotWasteBoonWardsAndHazardsCanConsumeThem()
    {
        using var a = new Arena(ClassKind.Duelist);
        Grant(a.Hero, "core:boon/beggars_cap");
        a.Hero.Tactics!.Boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 3, 200, 0);
        a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Special); a.Hit(a.Hero, 5);
        Assert.Equal(3, a.Hero.Tactics.Boons.WardTotal(0)); // paid parry already covered the hit
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Enemy.Id, a.Hero.Position, [a.Hero.Position], "test:ground",
            AttackDelivery.Area, DamageElement.Fire, 2, a.Combat.Clock, DamageCause.GroundHazard));
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.Equal(0, a.Hero.Tactics.Boons.WardTotal(0));
        a.Hero.Tactics.Boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 3, 200, 0);
        a.Hero.Tactics.Invulnerable = true; a.Hit(a.Hero, 50);
        Assert.Equal(3, a.Hero.Tactics.Boons.WardTotal(0));
    }

    [Fact]
    public void TimedMemoryRefreshPreservesStrongerUnexpiredValuesAndOnlyOnePrime()
    {
        var boons = new ActorBoons();
        boons.Inventory = boons.Inventory.Grant("core:boon/beggars_cap", BoonRarity.Common, ClassKind.Adventurer)
            .Grant("core:boon/bloodrush", BoonRarity.Common, ClassKind.Adventurer)
            .Grant("core:boon/stonewake", BoonRarity.Common, ClassKind.Adventurer);
        boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 8, 100, 0);
        boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 2, 100, 100);
        Assert.Equal(8, boons.WardTotal(200)); Assert.Equal(0, boons.WardTotal(201));
        boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 2, 100, 201);
        Assert.Equal(2, boons.WardTotal(201));
        boons.ApplyTimed("core:boon/bloodrush", BoonEffectKind.SpeedBoost, 20, 100, 0);
        boons.ApplyTimed("core:boon/bloodrush", BoonEffectKind.SpeedBoost, 10, 100, 99);
        Assert.Equal(20, boons.SpeedBonus(199));
        boons.ApplyTimed("core:boon/bloodrush", BoonEffectKind.SpeedBoost, 10, 100, 200);
        Assert.Equal(10, boons.SpeedBonus(200));
        boons.ApplyTimed("core:boon/stonewake", BoonEffectKind.Prime, 1, 100, 0);
        boons.ApplyTimed("core:boon/stonewake", BoonEffectKind.Prime, 1, 100, 0);
        Assert.True(boons.ConsumePrime("core:boon/stonewake", 100));
        Assert.False(boons.ConsumePrime("core:boon/stonewake", 100));
        boons.Validate(ClassKind.Adventurer);
    }

    [Fact]
    public void MemberBoonBuildsAndTimedMemoryResumeOnTheSameWorldTimeline()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer, enemyDue: 0);
        Grant(a.Hero, "core:boon/beggars_cap", BoonRarity.Rare); Grant(a.Hero, "core:boon/bloodrush");
        Grant(a.Friend, "core:boon/beggars_cap", BoonRarity.Legendary);
        a.Hero.Tactics!.Boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 4, 300, 0);
        a.Hero.Tactics.Boons.ApplyTimed("core:boon/bloodrush", BoonEffectKind.SpeedBoost, 20, 300, 0);
        a.Friend.Tactics!.Boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 6, 100, 0);
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision()); a.Act(TacticalAction.Wait);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitWindup("test:area", [a.Hero.Position, a.Friend.Position], AttackDelivery.Area, DamageElement.Fire, 5, 100, 100);
        string directory = Path.Combine(Path.GetTempPath(), "roag-boon-world-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot);
            var resumed = new TacticalCombat(loaded, _ => { });
            for (int i = 0; i < 12; i++)
            {
                Assert.Equal(a.Combat.AdvanceToDecision()!.Id, resumed.AdvanceToDecision()!.Id);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                foreach (var original in a.World.Party)
                {
                    var restored = loaded.ActorById(original.Id);
                    Assert.Equal(original.Health, restored.Health);
                    Assert.Equal(JsonSerializer.Serialize(original.Tactics!.Boons), JsonSerializer.Serialize(restored.Tactics!.Boons));
                }
                a.Act(TacticalAction.Wait); Assert.True(resumed.Commit(new(TacticalAction.Wait, Facing.East)).Success);
            }
            Assert.Equal(a.Hero.MaxHealth - 1, a.Hero.Health);
            Assert.Equal(a.Friend.MaxHealth, a.Friend.Health);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
            var forged = loaded.ActorById(a.Hero.Id).Tactics!.Boons;
            forged.Memory["core:boon/missing"] = new(ExpiresAt: 100, Ward: 1);
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(loaded.Plan, loaded.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
