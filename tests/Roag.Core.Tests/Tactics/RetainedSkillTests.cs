using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static string UnlockRetained(Actor member, string ability)
    {
        string id = RetainedBoons.Id(ability);
        member.Tactics!.Boons.Inventory = member.Tactics.Boons.Inventory.Grant(id, BoonRarity.Common, member.Tactics.Build!.Class);
        return id;
    }

    [Fact]
    public void RetainedMendPaysOwnClassRecoveryHealsPhysicallyAndCreditsOnlyActualHealingWithoutMana()
    {
        using var a = new Arena(ClassKind.Spiritbinder, ClassKind.Ranger);
        string boon = UnlockRetained(a.Hero, "ability.mend");
        a.Hit(a.Friend, 10); int health = a.Friend.Health, mana = a.Hero.Mana;
        a.Combat.ApplyNativeCondition(a.Friend, NativeConditionKind.Bleed, a.Enemy, a.Combat.State.NextAction++, "ability.rend", 3, 3);
        var intent = new TacticalIntent(TacticalAction.RetainedSkill, Item: boon, Target: a.Friend.Id);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.True(a.Combat.Preview(intent).Valid); Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        Assert.True(a.Combat.Commit(intent).Success);
        Assert.Equal(health + 6, a.Friend.Health); Assert.Empty(a.Friend.Tactics!.Conditions);
        Assert.Equal(mana, a.Hero.Mana); Assert.Empty(a.Hero.Talents);
        Assert.Equal(6, a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id].Healing);
        Assert.Equal(ActionTiming.Recovery(ClassCatalog.Get(ClassKind.Spiritbinder).Costs.Special + 30, a.Hero.Speed),
            a.Combat.State.Timeline.Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Hero.Id).Due);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void RetainedSkillsCannotReachAcrossWallsFloorsOrUnownedBuildsAndInvalidAttemptsAreFree()
    {
        using var a = new Arena(ClassKind.Spiritbinder, ClassKind.Ranger);
        string boon = UnlockRetained(a.Hero, "ability.mend"); a.Friend.Health -= 5;
        foreach (var blocked in new[] { "wall", "floor", "unowned" })
        {
            a.World.Chunks.Edit(new(100, 101), blocked == "wall" ? Material.Wall : Material.Floor);
            a.Friend.Position = new(100, 102, blocked == "floor" ? 1 : 0);
            string before = JsonSerializer.Serialize(a.World.State);
            var result = a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: blocked == "unowned" ? RetainedBoons.Id("ability.volley") : boon, Target: a.Friend.Id));
            Assert.False(result.Success); Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        }
    }

    [Fact]
    public void RetainedWatchRallyGivesBoundedAttributedWardWithoutFreeGuardOrRecoveryChanges()
    {
        using var a = new Arena(ClassKind.Sentinel, ClassKind.Ranger);
        string boon = UnlockRetained(a.Hero, "ability.stand-fast"); a.Friend.Stress = 20;
        var ready = a.Combat.State.Timeline.Pending.Single(w => w.ActorId == a.Friend.Id);
        Assert.True(a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: boon)).Success);
        Assert.Equal(12, a.Friend.Stress); Assert.False(a.Friend.Tactics!.Guarding);
        Assert.Contains(a.Combat.State.Timeline.Pending, w => w == ready);
        int hp = a.Friend.Health; a.Hit(a.Friend, 5);
        Assert.Equal(hp - 3, a.Friend.Health);
        Assert.Equal(2, a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id].Protection);
        Assert.Empty(a.Friend.Tactics.Conditions);
        a.Hit(a.Friend, 5); Assert.Equal(hp - 8, a.Friend.Health);
    }

    [Fact]
    public void RetainedEmberSharesBurnRefreshScheduleAndKeepsItsOwnedSkillProvenance()
    {
        using var a = new Arena(ClassKind.Arcanist);
        string boon = UnlockRetained(a.Hero, "ability.ember");
        Assert.True(a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: boon, Target: a.Enemy.Id)).Success);
        var burn = a.Enemy.Tactics!.Burning!; Assert.NotNull(burn);
        Assert.Equal(boon, burn.Activation!.BoonId); Assert.Equal("ability.ember", burn.Activation.Event.AbilityId);
        Assert.Equal(a.Hero.Id, burn.Activation.OwnerId); Assert.Equal(3, burn.Power);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void RetainedHuntersMarkCreditsAnotherMembersActualBonusDamageWithoutExtraXP()
    {
        using var a = new Arena(ClassKind.Ranger, ClassKind.Adventurer);
        string boon = UnlockRetained(a.Hero, "ability.hunters-mark");
        a.Enemy.Position = new(102, 102); a.Friend.Position = new(102, 103);
        Assert.True(a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: boon, Target: a.Enemy.Id)).Success);
        Assert.Contains(a.Enemy.Tactics!.Conditions, c => c.Kind == NativeConditionKind.Mark && c.Power == 4 && c.Source == a.Hero.Id);
        a.Next(a.Friend.Id);
        int hp = a.Enemy.Health; long previous = a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id].Damage;
        a.Act(TacticalAction.BasicAttack, Facing.North);
        int loss = hp - a.Enemy.Health;
        Assert.Equal(a.Friend.Power + 4, loss);
        var ledger = a.Combat.State.Rewards.Enemies[a.Enemy.Id];
        Assert.Equal(4, ledger.Members[a.Hero.Id].Damage - previous);
        Assert.Equal(a.Friend.Power, ledger.Members[a.Friend.Id].Damage);
    }

    [Fact]
    public void RetainedTalentsAreOwnedBoonsAndModifyRealDamageAndFutureRecovery()
    {
        using var a = new Arena(ClassKind.Adventurer);
        UnlockRetained(a.Hero, "talent.finisher"); UnlockRetained(a.Hero, "talent.quick-footed");
        a.Enemy.Health = a.Enemy.MaxHealth / 2; a.Enemy.Position = new(101, 100);
        int hp = a.Enemy.Health;
        Assert.Equal(ActionTiming.Recovery(100, a.Hero.Speed + 2), a.Combat.Preview(new(TacticalAction.Wait)).Recovery);
        a.Act(TacticalAction.BasicAttack, Facing.East); Assert.Equal(hp - a.Hero.Power - 2, a.Enemy.Health);
        Assert.Empty(a.Hero.Talents); Assert.Empty(a.Hero.CombatTrait);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void RetainedInitiatorDiscountDoesNotDiscountTheRecoveringHelpersCost()
    {
        using var a = new Arena(ClassKind.Arcanist, ClassKind.Arcanist);
        a.Friend.Position = new(100, 101);
        UnlockRetained(a.Hero, "talent.arcanist-reservoir"); UnlockRetained(a.Friend, "talent.arcanist-reservoir");
        var choice = a.Combat.CooperativeOptions(a.Friend.Id).Single(o => o.Choice == 2);
        Assert.Equal(ActionTiming.Recovery(ClassCatalog.Get(ClassKind.Arcanist).Costs.Special + 30, a.Hero.Speed), choice.Recovery);
        Assert.Equal(ActionTiming.Recovery(ClassCatalog.Get(ClassKind.Arcanist).Costs.BasicAttack + 30, a.Friend.Speed), choice.HelperRecovery);
    }

    [Fact]
    public void RetainedShieldBashCancelsARealCommittedWindupWithoutRefundingEitherActorsRecovery()
    {
        using var a = new Arena(ClassKind.Sentinel, enemyDue: 0);
        string boon = UnlockRetained(a.Hero, "ability.shield-bash"); a.Enemy.Position = new(101, 100);
        a.Act(TacticalAction.Wait); a.Next(a.Enemy.Id);
        var attack = a.Combat.CommitWindup("test:retained-bash", [a.Hero.Position], AttackDelivery.Melee, DamageElement.Physical, 5, 300, 150);
        a.Next(); Assert.Contains(a.Combat.State.Attacks, w => w.Id == attack.Id);
        var clock = a.Combat.Clock;
        Assert.True(a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: boon, Target: a.Enemy.Id)).Success);
        Assert.DoesNotContain(a.Combat.State.Attacks, w => w.Id == attack.Id);
        Assert.DoesNotContain(a.Combat.State.Timeline.Pending, w => w.Kind == ScheduledKind.Impact && w.ReferenceId == attack.Id);
        Assert.True(a.Enemy.Tactics!.StunnedUntil >= clock + 100);
        Assert.Contains(a.Combat.State.Timeline.Pending, w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Hero.Id && w.Due > clock);
        Assert.True(a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id].Control >= 100);
    }

    [Fact]
    public void RetainedProjectileSkillsRequireActualClearBodyLines()
    {
        using var a = new Arena(ClassKind.Arcanist, ClassKind.Ranger);
        string boon = UnlockRetained(a.Hero, "ability.ember");
        a.Friend.Position = new(101, 100); a.Enemy.Position = new(103, 100);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: boon, Target: a.Enemy.Id)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void RetainedSkillsAndTheirPendingOwnedConditionsContinueExactlyThroughOrdinarySaves()
    {
        using var a = new Arena(ClassKind.Arcanist, ClassKind.Ranger);
        string ember = UnlockRetained(a.Hero, "ability.ember"), mark = UnlockRetained(a.Friend, "ability.hunters-mark");
        a.Enemy.Position = new(103, 101);
        Assert.True(a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: ember, Target: a.Enemy.Id)).Success);
        a.Next(a.Friend.Id);
        Assert.True(a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: mark, Target: a.Enemy.Id)).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-retained-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            a.World.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(a.World.State), JsonSerializer.Serialize(loaded.State));
            var continuation = new TacticalCombat(loaded, _ => { });
            for (int i = 0; i < 8; i++)
            {
                var first = a.Combat.AdvanceToDecision(); var second = continuation.AdvanceToDecision();
                Assert.Equal(first!.Id, second!.Id);
                Assert.True(a.Combat.Commit(new(TacticalAction.Wait)).Success);
                Assert.True(continuation.Commit(new(TacticalAction.Wait)).Success);
                Assert.Equal(JsonSerializer.Serialize(a.World.State), JsonSerializer.Serialize(loaded.State));
            }
            Assert.Null(a.Enemy.Tactics!.Burning);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void NativeBleedingUsesOwnTurnsAndEggPreparationWithoutChangingTheDownedCounter()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Hero.Preparations.Add(new("camp.cherry-eggs", "bleed-resist", 2, 1800));
        a.Combat.ApplyNativeCondition(a.Hero, NativeConditionKind.Bleed, a.Enemy, a.Combat.State.NextAction++, "ability.rend", 3, 3);
        int hp = a.Hero.Health;
        for (int i = 1; i <= 3; i++)
        {
            a.Act(TacticalAction.Wait); a.Next();
            Assert.Equal(hp - i, a.Hero.Health); Assert.Equal(0, a.Hero.Tactics!.BleedTurns);
        }
        Assert.Empty(a.Hero.Tactics!.Conditions);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void TeaAndResolveEquipmentReduceActualDreadStressWithoutASeparateManaOrNeedAuthority()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Hero.Preparations.Add(new("camp.honey-tea", "focus", 3, 1800));
        a.Combat.ApplyNativeDread(a.Enemy, a.Hero, a.Combat.State.NextAction++, "ability.dread", 12);
        Assert.Equal(3, a.Hero.Stress);
        a.World.State.Tick = 1800;
        a.Combat.ApplyNativeDread(a.Enemy, a.Hero, a.Combat.State.NextAction++, "ability.dread", 12);
        Assert.Equal(15, a.Hero.Stress);
    }

    [Theory]
    [InlineData(ClassKind.Ranger, "talent.ranger-headhunter", "wounded", 4)]
    [InlineData(ClassKind.Ranger, "talent.keen-eye", "marked", 2)]
    [InlineData(ClassKind.Ranger, "talent.ranger-stalker", "marked", 4)]
    [InlineData(ClassKind.Sentinel, "talent.warden-breaker", "stunned", 4)]
    [InlineData(ClassKind.Adventurer, "talent.opportunist", "stunned", 2)]
    public void RetainedConditionalTalentsChangeActualDirectHits(ClassKind kind, string talent, string condition, int bonus)
    {
        using var a = new Arena(kind, ClassKind.Adventurer);
        UnlockRetained(a.Hero, talent); a.Enemy.Position = new(101, 100);
        if (condition == "wounded") a.Enemy.Health = a.Enemy.MaxHealth / 2;
        if (condition == "marked") a.Combat.ApplyNativeCondition(a.Enemy, NativeConditionKind.Mark, a.Friend,
            a.Combat.State.NextAction++, "ability.hunters-mark", 4, 3);
        if (condition == "stunned") a.Combat.StunNativeActor(a.Friend, a.Enemy, a.Combat.State.NextAction++, "ability.shield-bash", 200);
        int health = a.Enemy.Health; a.Act(TacticalAction.BasicAttack, Facing.East);
        Assert.Equal(health - a.Hero.Power - bonus - (condition == "marked" ? 4 : 0), a.Enemy.Health);
    }

    [Theory]
    [InlineData("talent.arcanist-cinder-adept", "ability.ember", 4)]
    [InlineData("talent.arcanist-stormcaller", "ability.chain-arc", 3)]
    public void RetainedFireAndAreaTalentsAffectNativeSkillsButNotTheirOngoingDamage(string talent, string ability, int bonus)
    {
        using var a = new Arena(ClassKind.Arcanist);
        UnlockRetained(a.Hero, talent); string skill = UnlockRetained(a.Hero, ability);
        var intent = new TacticalIntent(TacticalAction.RetainedSkill, Item: skill, Target: a.Enemy.Id);
        int power = a.Combat.Preview(intent).Damage!.Value, health = a.Enemy.Health;
        Assert.True(a.Combat.Commit(intent).Success); Assert.Equal(health - power - bonus, a.Enemy.Health);
        if (ability == "ability.ember") Assert.Equal(3, a.Enemy.Tactics!.Burning!.Power);
    }

    [Fact]
    public void RetainedHealingCalmAndCoverTalentsUseRealSupportEffectsAndBoundedCredit()
    {
        using var a = new Arena(ClassKind.Spiritbinder, ClassKind.Ranger);
        UnlockRetained(a.Hero, "talent.vestal-lifebinder"); UnlockRetained(a.Hero, "talent.vestal-resolve-keeper");
        string mend = UnlockRetained(a.Hero, "ability.mend"), absolution = UnlockRetained(a.Hero, "ability.absolution");
        a.Hit(a.Friend, 12); int health = a.Friend.Health;
        Assert.True(a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: mend, Target: a.Friend.Id)).Success);
        Assert.Equal(health + 11, a.Friend.Health);
        Assert.Equal(11, a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id].Healing);
        a.Next(); a.Friend.Stress = 50;
        Assert.True(a.Combat.Commit(new(TacticalAction.RetainedSkill, Item: absolution)).Success);
        Assert.Equal(22, a.Friend.Stress);
        a.Friend.Health = a.Friend.MaxHealth / 2; health = a.Friend.Health;
        a.Hit(a.Friend, 5);
        Assert.Equal(health - 3, a.Friend.Health);
        Assert.Equal(2, a.Combat.State.Rewards.Enemies[a.Enemy.Id].Members[a.Hero.Id].Protection);
    }
}

public sealed class RetainedBoonCatalogTests
{
    [Fact]
    public void ProductionChestsNaturallyOfferAllEightActiveSkillsToEligibleClasses()
    {
        var remaining = RetainedBoons.All.Where(e => e.Active).Select(e => e.Id).ToHashSet();
        var examples = new List<object>();
        for (int seed = 1; seed <= 32 && remaining.Count > 0; seed++)
        {
            using var world = WorldSession.CreateDungeonRun(seed, survival: true);
            foreach (var chest in world.State.Tactics!.Chests.Chests)
                foreach (var kind in new[] { ClassKind.Ranger, ClassKind.Arcanist, ClassKind.Sentinel, ClassKind.Spiritbinder })
                    foreach (var offer in world.State.Tactics.Chests.Offers(chest, kind))
                        if (remaining.Remove(offer.Boon)) examples.Add(new { seed, kind, chest.Id, chest.Position, offer });
        }
        Assert.Empty(remaining);
        if (Environment.GetEnvironmentVariable("ROAG_RETAINED_AUDIT") is { Length: > 0 } path)
            File.WriteAllText(path, JsonSerializer.Serialize(examples, new JsonSerializerOptions { WriteIndented = true }));
    }

    [Fact]
    public void EveryImportedTalentAndAllEightAdditionalPlayerSkillsHaveExplicitNativeMappings()
    {
        var talents = ContentCatalog.Default.All<TalentDefinition>().Select(t => t.Id).Order().ToArray();
        Assert.Equal(22, talents.Length);
        Assert.Equal(talents, RetainedBoons.All.Where(e => !e.Active).Select(e => e.Source).Order());
        Assert.Equal(8, RetainedBoons.All.Count(e => e.Active));
        Assert.Equal(30, RetainedBoons.All.Select(e => e.Id).Distinct().Count());
        Assert.All(RetainedBoons.All, e => Assert.Contains(e.Id, BoonCatalog.Default.NormalPool));
        Assert.Equal(103, BoonCatalog.Original.Definitions.Length);
        Assert.Equal(133, BoonCatalog.Default.Definitions.Length);
    }
}
