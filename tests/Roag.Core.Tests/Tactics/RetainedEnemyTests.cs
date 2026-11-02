using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class RetainedEnemyTests
{
    private sealed class Field : IDisposable
    {
        public WorldSession World { get; } = WorldSession.Create(42);
        public TacticalCombat Combat { get; }
        public Actor Hero { get; }
        public Actor Enemy { get; }
        public Field(string source, string variant = "", ClassKind kind = ClassKind.Sentinel)
        {
            Hero = World.Party.Single(); Hero.Position = World.State.Position = new(100, 100);
            for (int floor = 0; floor < 2; floor++) for (int y = 90; y < 112; y++) for (int x = 90; x < 112; x++)
                World.Chunks.Edit(new(x, y, floor), Material.Floor);
            long id = World.State.NextActorId++;
            Enemy = source.StartsWith("boss.", StringComparison.Ordinal)
                ? ActorFactory.CreateBoss(World.Content, source, variant, id, new(101, 100), 0)
                : ActorFactory.Create(World.Content, source, 42, id, new(101, 100), 0);
            World.State.Actors.Add(Enemy);
            Combat = new(World, _ => { });
            Combat.Register(Hero, new() { Build = CharacterBuild.Create(kind, 42) });
            Combat.RegisterRetainedEnemy(Enemy);
            Assert.Same(Hero, Combat.AdvanceToDecision());
        }
        public Actor Add(string source, WorldPosition position)
        {
            var actor = ActorFactory.Create(World.Content, source, 42, World.State.NextActorId++, position, 0);
            actor.EncounterLeader = Enemy.EncounterLeader;
            World.State.Actors.Add(actor); Combat.RegisterRetainedEnemy(actor, 10000); return actor;
        }
        public void EnemyReady()
        {
            Assert.True(Combat.Commit(new(TacticalAction.Wait)).Success);
            Assert.Same(Enemy, Combat.AdvanceToDecision());
        }
        public EnemyIntent Choose(string ability)
        {
            for (int seed = 1; seed < 1000; seed++)
            {
                Enemy.Tactics!.Enemy!.Retained!.RandomState = seed * 1009;
                var intent = Combat.PreviewWorldEnemyTurn();
                if (intent.Ability == ability) return intent;
            }
            throw new InvalidOperationException("Source never offered " + ability);
        }
        public void Release()
        {
            for (int i = 0; i < 50 && Combat.State.Attacks.Count > 0; i++)
            {
                var ready = Combat.AdvanceToDecision(); Assert.NotNull(ready);
                if (Combat.State.Attacks.Count > 0) Assert.True(Combat.Commit(new(TacticalAction.Wait)).Success);
            }
            Assert.Empty(Combat.State.Attacks);
        }
        public void Dispose() => World.Dispose();
    }

    public static IEnumerable<object[]> Bosses() => ContentCatalog.Default.All<BossDefinition>()
        .Select(b => new object[] { b.Id, b.Variants[0] });

    public static IEnumerable<object[]> HostileAbilities()
    {
        var covered = new HashSet<string>();
        foreach (var character in ContentCatalog.Default.All<CharacterDefinition>().Where(c => c.ActorKind is ActorKind.Beast or ActorKind.Raider
                     && c.Ecology?.Behavior != "flee"))
            foreach (string ability in character.Behaviors.Select(r => r.Ability))
                if (covered.Add(ability)) yield return [character.Id, "", false, ability];
        foreach (var boss in ContentCatalog.Default.All<BossDefinition>())
            foreach (bool enraged in new[] { false, true })
                foreach (string ability in (enraged ? boss.Enraged : boss.Behaviors).Select(r => r.Ability))
                    if (covered.Add(ability)) yield return [boss.Id, boss.Variants[0], enraged, ability];
    }

    [Theory]
    [MemberData(nameof(HostileAbilities))]
    public void EveryHostileAuthoredAbilityActuallyReleasesThroughNativeCombat(string source, string variant, bool enraged, string ability)
    {
        using var f = new Field(source, variant);
        f.Enemy.Habitat = f.Enemy.Position;
        f.Enemy.ProvokedUntil = 75;
        f.Enemy.Health = source.StartsWith("boss.", StringComparison.Ordinal) && !enraged ? f.Enemy.MaxHealth : Math.Max(1, f.Enemy.MaxHealth / 3);
        f.Enemy.Tactics!.Enemy!.Retained!.Actions = 5;
        f.EnemyReady(); f.Choose(ability);
        f.Combat.CommitWorldEnemyTurn(); f.Release();
        Assert.Contains(f.Combat.State.Events, e => e.Kind == "RetainedSkillUsed" && e.Source == f.Enemy.Id && e.Ability == ability);
        Assert.Equal(ability, f.Enemy.Tactics.Enemy.Retained.LastAbility);
        TacticalValidation.Validate(f.World.Plan, f.World.State);
        Assert.Equal(19, HostileAbilities().Count());
    }

    [Theory]
    [MemberData(nameof(Bosses))]
    public void EveryImportedBossUsesItsAuthoredNormalAndEnragedRulesWithoutChangingTheOriginalRoster(string source, string variant)
    {
        using var f = new Field(source, variant); f.EnemyReady();
        var boss = f.World.Content.Get<BossDefinition>(source);
        foreach (bool enraged in new[] { false, true })
        {
            f.Enemy.Health = enraged ? Math.Max(1, f.Enemy.MaxHealth * boss.PhasePercent / 100) : f.Enemy.MaxHealth;
            var allowed = (enraged ? boss.Enraged : boss.Behaviors).Concat(f.World.Content.Get<VariantDefinition>(variant).Behaviors)
                .Select(r => r.Ability).Append(f.World.Content.Game.EnemyFallback);
            string before = JsonSerializer.Serialize(f.World.State);
            var preview = f.Combat.PreviewWorldEnemyTurn();
            Assert.Equal(EnemyIntentKind.Windup, preview.Kind);
            Assert.Contains(preview.Ability, allowed); Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
            Assert.Contains("enraged", f.Combat.InspectEnemy(f.Hero.Id, f.Enemy.Id)!.Tendency);
            Assert.False(f.Enemy.Tactics!.Enemy!.PhaseTwo);
            TacticalValidation.Validate(f.World.Plan, f.World.State);
        }
        Assert.Equal(50, EnemyCatalog.Default.Definitions.Length);
        Assert.Equal(48, ContentCatalog.Default.All<BossDefinition>().Count());
    }

    [Fact]
    public void ImportedStatsPreserveVariantAndHealthRatioAndNeverScaleToPartyLevel()
    {
        using var f = new Field("character.raider");
        Assert.Equal(10, f.Enemy.MaxHealth); Assert.Equal(3, f.Enemy.Power - f.Enemy.Equipment.Values.Sum(e => e.Power));
        Assert.Equal(100, f.Enemy.Speed); Assert.Equal("bandit", f.Enemy.SpriteId);
        int hp = f.Enemy.MaxHealth, power = f.Enemy.Power;
        f.Hero.Tactics!.Build = f.Hero.Tactics.Build!.GainExperience(10000).Build;
        Assert.Equal(hp, f.Enemy.MaxHealth); Assert.Equal(power, f.Enemy.Power);
        Assert.True(RetainedEnemyCatalog.Contains(f.Enemy.Tactics!.DefinitionId));
        Assert.False(EnemyCatalog.Default.Contains(f.Enemy.Tactics.DefinitionId));
    }

    [Fact]
    public void RetainedBossRolePreservesTheNativeClassBossPullRestriction()
    {
        using var f = new Field("boss.red-huntsman", "variant.emberborne", ClassKind.Hookrunner);
        f.Enemy.Position = new(103, 100);
        Assert.True(f.Enemy.Tactics!.ImmuneToPull); Assert.False(f.Enemy.Tactics.RegularEnemy);
        var position = f.Enemy.Position;
        Assert.True(f.Combat.Commit(new(TacticalAction.Special, Facing.East, f.Enemy.Id)).Success);
        Assert.Equal(position, f.Enemy.Position);
        Assert.DoesNotContain(f.Combat.State.Events, e => e.Kind == "Displaced" && e.Target == f.Enemy.Id);
        Assert.Contains(f.Combat.State.Timeline.Pending, w => w.ActorId == f.Hero.Id && w.Due > 0);
    }

    [Theory]
    [InlineData("character.hollow-bleeder", "ability.rend", NativeConditionKind.Bleed)]
    [InlineData("character.road-bowman", "ability.hunters-mark", NativeConditionKind.Mark)]
    public void ActualAuthoredCastAppliesAttributedConditionsOnlyAfterRelease(string source, string ability, NativeConditionKind kind)
    {
        using var f = new Field(source); f.EnemyReady(); f.Choose(ability);
        var rng = f.Enemy.Tactics!.Enemy!.Retained!.RandomState;
        Assert.Equal(ability, f.Combat.CommitWorldEnemyTurn().Ability);
        Assert.Empty(f.Hero.Tactics!.Conditions); Assert.Equal("", f.Enemy.Tactics.Enemy.Retained.LastAbility);
        Assert.NotEqual(rng, f.Enemy.Tactics.Enemy.Retained.RandomState);
        Assert.Equal(1, f.Enemy.Tactics.Enemy.Retained.Actions);
        var attack = Assert.Single(f.Combat.State.Attacks); Assert.True(attack.RetainedAbility);
        TacticalValidation.Validate(f.World.Plan, f.World.State);
        f.Release();
        var condition = Assert.Single(f.Hero.Tactics.Conditions, c => c.Kind == kind);
        Assert.Equal(f.Enemy.Id, condition.Source); Assert.Equal(attack.Id, condition.Action);
        Assert.Equal(ability, f.Enemy.Tactics.Enemy.Retained.LastAbility);
        TacticalValidation.Validate(f.World.Plan, f.World.State);
    }

    [Fact]
    public void AuthoredDreadTargetsHighestStressAndActuallyUsesTeaResistance()
    {
        using var f = new Field("character.ash-whisper");
        var friend = f.World.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        friend.Position = new(100, 102); friend.Recruited = true; f.World.State.Party.Add(friend.Id);
        f.Combat.Register(friend, new() { Build = CharacterBuild.Create(ClassKind.Ranger, 43) }, 10000);
        f.Hero.Stress = 10; friend.Stress = 40;
        friend.Preparations.Add(new("camp.honey-tea", "focus", 3, 10000));
        f.EnemyReady(); f.Choose("ability.dread");
        Assert.Equal(friend.Id, f.Combat.SelectEnemyTarget(f.Enemy.Id));
        Assert.Contains("highest stress", f.Combat.InspectEnemy(f.Hero.Id, f.Enemy.Id)!.Tendency);
        f.Combat.CommitWorldEnemyTurn(); f.Release();
        Assert.Equal(40, friend.Stress); Assert.Equal(10, f.Hero.Stress);
        Assert.Contains(f.Combat.State.Events, e => e.Kind == "StressChanged" && e.Target == friend.Id && e.Amount == 0);
    }

    [Fact]
    public void PreparedAreaFireHasLongerLockedWindupAndOneSharedBurnSchedule()
    {
        using var f = new Field("character.cinder-channeler"); f.EnemyReady();
        var intent = f.Combat.CommitWorldEnemyTurn(); Assert.Equal("ability.cinder-surge", intent.Ability);
        var attack = Assert.Single(f.Combat.State.Attacks);
        Assert.Equal(ActionTiming.Recovery(200, f.Enemy.Speed), attack.Due - f.Combat.Clock);
        Assert.Equal(140, attack.RecoveryCost); Assert.Null(f.Hero.Tactics!.Burning);
        f.Release(); var burn = f.Hero.Tactics.Burning!;
        Assert.NotNull(burn); Assert.Null(burn.Activation); Assert.Equal(f.Enemy.Id, burn.Native!.Actor);
        Assert.Single(f.Combat.State.Timeline.Pending, w => w.Kind == ScheduledKind.Effect && w.ActorId == f.Hero.Id);
        TacticalValidation.Validate(f.World.Plan, f.World.State);
    }

    [Fact]
    public void MovingOffLockedTargetAndBuildingAProjectileWallDenyHitsAndConditions()
    {
        using var f = new Field("character.road-bowman"); f.EnemyReady(); f.Choose("ability.hunters-mark");
        f.Combat.CommitWorldEnemyTurn(); int hp = f.Hero.Health;
        f.Hero.Position = f.World.State.Position = new(100, 101);
        f.Release(); Assert.Equal(hp, f.Hero.Health); Assert.Empty(f.Hero.Tactics!.Conditions);
        using var wall = new Field("character.road-bowman"); wall.Enemy.Position = new(104, 100); wall.EnemyReady(); wall.Choose("ability.hunters-mark");
        wall.Combat.CommitWorldEnemyTurn(); hp = wall.Hero.Health;
        wall.World.Chunks.Edit(new(102, 100), Material.Wall);
        wall.Release(); Assert.Equal(hp, wall.Hero.Health); Assert.Empty(wall.Hero.Tactics!.Conditions);
    }

    [Fact]
    public void UnreleasedInterruptedCastDoesNotBecomeLastAbilityAndDoesNotRefundAttempt()
    {
        using var f = new Field("character.cinder-channeler"); f.EnemyReady(); f.Combat.CommitWorldEnemyTurn();
        f.Combat.StunNativeActor(f.Hero, f.Enemy, f.Combat.State.NextAction++, "ability.shield-bash", 100);
        Assert.Empty(f.Combat.State.Attacks); Assert.Equal("", f.Enemy.Tactics!.Enemy!.Retained!.LastAbility);
        Assert.Equal(1, f.Enemy.Tactics.Enemy.Retained.Actions); Assert.Null(f.Hero.Tactics!.Burning);
        TacticalValidation.Validate(f.World.Plan, f.World.State);
    }

    [Theory]
    [InlineData("character.road-bowman", "", false)]
    [InlineData("boss.red-huntsman", "variant.emberborne", true)]
    public void AuthoredWeakestAndStrongestUseAbsoluteHPInsteadOfHealthPercentage(string source, string variant, bool strongest)
    {
        using var f = new Field(source, variant);
        var friend = f.World.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        friend.Position = new(101, 102); friend.Recruited = true; f.World.State.Party.Add(friend.Id);
        var build = CharacterBuild.Create(ClassKind.Ranger, 43).GainExperience(10000).Build;
        f.Combat.Register(friend, new() { Build = build }, 10000);
        f.Hero.Health = 12; friend.Health = 15;
        Assert.True((double)friend.Health / friend.MaxHealth < (double)f.Hero.Health / f.Hero.MaxHealth);
        f.EnemyReady();
        var selected = f.Combat.SelectEnemyTarget(f.Enemy.Id);
        Assert.Equal(strongest ? friend.Id : f.Hero.Id, selected);
        Assert.Contains(strongest ? "highest current HP" : "lowest current HP", f.Combat.InspectEnemy(f.Hero.Id, f.Enemy.Id)!.Tendency);
    }

    [Fact]
    public void TimidAnimalRemainsNonhostileAndPrivateObservationCannotBorrowPartySight()
    {
        using var timid = new Field("character.rabbit"); timid.Enemy.Habitat = timid.Enemy.Position;
        timid.EnemyReady(); Assert.False(timid.World.IsHostile(timid.Enemy));
        Assert.Null(timid.Combat.SelectEnemyTarget(timid.Enemy.Id));
        Assert.Equal(EnemyIntentKind.Wait, timid.Combat.PreviewWorldEnemyTurn().Kind);
        using var f = new Field("character.road-bowman"); f.EnemyReady();
        f.World.Chunks.Edit(new(100, 100, 0), Material.Floor);
        f.Hero.Position = f.World.State.Position = new(99, 100);
        f.World.Chunks.Edit(new(100, 100), Material.Wall);
        Assert.Null(f.Combat.SelectEnemyTarget(f.Enemy.Id));
        Assert.Equal(EnemyIntentKind.Wait, f.Combat.PreviewWorldEnemyTurn().Kind);
        Assert.Equal(new WorldPosition(100, 100), f.Enemy.Tactics!.Enemy!.Awareness!.LastKnown);
        Assert.NotEqual(f.Hero.Position, f.Enemy.Tactics.Enemy.Awareness.LastKnown);
    }

    [Fact]
    public void ImportedHealerSelectsActualWoundedAllyAndCannotFollowItAfterCommit()
    {
        using var f = new Field("character.vow-mender"); var ally = f.Add("character.road-bruiser", new(102, 101));
        ally.Health -= 8; int hp = ally.Health; f.EnemyReady(); f.Choose("ability.mend");
        var preview = f.Combat.PreviewWorldEnemyTurn(); Assert.Equal(ally.Id, preview.Retained!.Target);
        f.Combat.CommitWorldEnemyTurn(); ally.Position = new(103, 101); f.Release(); Assert.Equal(hp, ally.Health);
    }

    [Fact]
    public void GuardFacingIsCommittedBeforePreyMovesRatherThanRetargetingAtImpact()
    {
        using var f = new Field("character.raider"); f.Enemy.Tactics!.Enemy!.Retained!.Actions = 2;
        f.EnemyReady(); f.Choose("ability.guard"); f.Combat.CommitWorldEnemyTurn();
        Assert.Equal(Facing.West, f.Enemy.Tactics.Facing); Assert.False(f.Enemy.Tactics.Guarding);
        f.Hero.Position = f.World.State.Position = new(101, 101); f.Release();
        Assert.True(f.Enemy.Tactics.Guarding); Assert.Equal(Facing.West, f.Enemy.Tactics.Facing);
    }

    [Fact]
    public void RetainedShotConsumesARealMirrorAndHitsTheBodyOnItsLockedTurnedPath()
    {
        using var f = new Field("character.road-bowman"); f.Enemy.Position = new(104, 100);
        var owner = ActorFactory.Create(f.World.Content, "character.raider", 42, f.World.State.NextActorId++, new(101, 100), 0);
        f.World.State.Actors.Add(owner); f.Combat.RegisterEnemy(owner, "core:enemy/mirror_hexer", 10000);
        owner.Tactics!.Enemy!.MirrorCasts = 1;
        f.Combat.State.Mirrors.Add(new(f.Combat.State.NextAction++, owner.Id, new(102, 100), owner.Position, Facing.West, 400));
        var victim = f.Add("character.raider", new(102, 99)); int hp = victim.Health, heroHp = f.Hero.Health;
        f.EnemyReady(); f.Choose("ability.hunters-mark"); f.Combat.CommitWorldEnemyTurn();
        Assert.Empty(f.Combat.State.Mirrors); var attack = Assert.Single(f.Combat.State.Attacks);
        Assert.NotNull(attack.Reflection); Assert.Contains(victim.Position, attack.Cells); Assert.DoesNotContain(f.Hero.Position, attack.Cells);
        TacticalValidation.Validate(f.World.Plan, f.World.State);
        f.Release(); Assert.True(victim.Health < hp); Assert.Equal(heroHp, f.Hero.Health);
        Assert.Contains(victim.Tactics!.Conditions, c => c.Kind == NativeConditionKind.Mark && c.Source == f.Enemy.Id);
        TacticalValidation.Validate(f.World.Plan, f.World.State);
    }

    [Fact]
    public void PendingImportedCastAndNativeBurnRoundTripAndContinueExactly()
    {
        using var f = new Field("character.cinder-channeler"); f.EnemyReady(); f.Combat.CommitWorldEnemyTurn();
        string root = Path.Combine(Path.GetTempPath(), "roag-retained-enemy-" + Guid.NewGuid().ToString("N"));
        try
        {
            f.World.Save(root); using var loaded = WorldSession.Load(root); var other = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(loaded.State));
            bool savedBurn = false;
            for (int i = 0; i < 12; i++)
            {
                var a = f.Combat.AdvanceToDecision(); var b = other.AdvanceToDecision(); Assert.Equal(a!.Id, b!.Id);
                Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(loaded.State));
                if (!savedBurn && f.Hero.Tactics!.Burning?.Native != null)
                {
                    f.World.Save(root); using var burning = WorldSession.Load(root);
                    Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(burning.State));
                    TacticalValidation.Validate(burning.Plan, burning.State); savedBurn = true;
                }
                if (a.Id == f.Enemy.Id) { Assert.Equal(JsonSerializer.Serialize(f.Combat.CommitWorldEnemyTurn()), JsonSerializer.Serialize(other.CommitWorldEnemyTurn())); }
                else { Assert.True(f.Combat.Commit(new(TacticalAction.Wait)).Success); Assert.True(other.Commit(new(TacticalAction.Wait)).Success); }
                Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(loaded.State));
                if (f.World.State.AwaitingContinuation) break;
            }
            Assert.True(savedBurn);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
