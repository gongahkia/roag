using System.Text.Json;
using Roag.Core.Content;
using Roag.Client;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class CampaignBossTests
{
    private sealed class Encounter : IDisposable
    {
        public WorldSession World { get; }
        public TacticalCombat Combat { get; }
        public CampaignBossArena Arena { get; }
        public Actor Hero { get; }
        public Actor Boss { get; }
        public Encounter(CampaignBossKind kind, bool final = false, int variation = 0, int members = 1, ClassKind heroClass = ClassKind.Adventurer)
        {
            World = WorldSession.Create(42);
            Combat = new(World, _ => { }, new(EnableSurvival: false));
            Hero = World.Party.Single();
            Combat.Register(Hero, new() { Build = CharacterBuild.Create(heroClass, 42), AutoEat = false });
            Arena = World.CreateCampaignBossArena(kind, new(512, 512, 1), 73, final, variation, readyAt: 50);
            Boss = World.ActorById(Arena.Boss);
            Hero.Position = World.State.Position = Arena.Center.Offset(0, 6);
            for (int i = 1; i < members; i++)
            {
                var ally = ActorFactory.Create(World.Content, World.Content.Game.Traveller, 42, World.State.NextActorId++, Arena.Center.Offset(i, 6), 0);
                ally.Recruited = true; World.State.Actors.Add(ally); World.State.Party.Add(ally.Id);
                Combat.Register(ally, new() { Build = CharacterBuild.Create(i % 2 == 0 ? ClassKind.Ranger : ClassKind.Adventurer, 42 + i), AutoEat = false });
            }
            World.Reveal(); Assert.Same(Hero, Combat.AdvanceToDecision());
        }
        public void Act(TacticalIntent intent)
        { var result = Combat.Commit(intent); Assert.True(result.Success, result.Message); }
        public Actor NextHero()
        {
            for (int i = 0; i < 100; i++)
            {
                var actor = Combat.AdvanceToDecision(); Assert.NotNull(actor);
                if (actor.Id == Hero.Id) return actor;
                if (actor.Tactics!.Enemy != null) Combat.CommitWorldEnemyTurn();
                else Act(new(TacticalAction.Wait));
            }
            throw new InvalidOperationException("Founder did not become ready.");
        }
        public LockedAttack Cast(long index)
        {
            Boss.Tactics!.Enemy!.CampaignBoss!.Moves = index;
            Act(new(TacticalAction.Wait));
            for (int i = 0; i < 10; i++)
            {
                var actor = Combat.AdvanceToDecision(); Assert.NotNull(actor);
                if (actor.Id == Boss.Id)
                {
                    var preview = Combat.PreviewEnemyTurn(Hero.Id, (_, _) => null);
                    Assert.Equal(EnemyIntentKind.Windup, preview.Kind);
                    Combat.CommitEnemyTurn(Hero.Id, (_, _) => null);
                    var attack = Assert.Single(Combat.State.Attacks); NextHero(); return attack;
                }
                Act(new(TacticalAction.Wait));
            }
            throw new InvalidOperationException("Boss did not become ready.");
        }
        public void Resolve(LockedAttack attack)
        {
            for (int i = 0; i < 20 && Combat.State.Attacks.Any(a => a.Id == attack.Id); i++)
            { Act(new(TacticalAction.Wait)); NextHero(); }
            Assert.DoesNotContain(Combat.State.Attacks, a => a.Id == attack.Id);
        }
        public void Hit(WorldPosition p, int damage = 10, DamageElement element = DamageElement.Physical, AttackDelivery delivery = AttackDelivery.Melee)
            => Combat.ResolveAttack(new(Combat.State.NextAction++, Hero.Id, Hero.Position, [p], "test:boss-counterplay", delivery, element, damage, Combat.Clock));
        public void Dispose() => World.Dispose();
    }

    [Theory]
    [InlineData(CampaignBossKind.Bellkeeper)]
    [InlineData(CampaignBossKind.SluiceMaw)]
    [InlineData(CampaignBossKind.Threadmother)]
    [InlineData(CampaignBossKind.OssuaryCaptain)]
    public void AdditionalRolesPreserveOriginalFiftyAndUseFixedStrongerFinalDifficulty(CampaignBossKind kind)
    {
        Assert.Equal(50, EnemyCatalog.Default.Definitions.Length);
        Assert.False(EnemyCatalog.Default.Contains(CampaignBossCatalog.Id(kind)));
        for (int variation = 0; variation < 3; variation++)
        {
            using var normal = new Encounter(kind, variation: variation);
            using var final = new Encounter(kind, final: true, variation: variation, members: 4);
            Assert.Equal(normal.Boss.MaxHealth * 2, final.Boss.MaxHealth);
            Assert.True(final.Boss.Power > normal.Boss.Power);
            Assert.True(final.Boss.Speed > normal.Boss.Speed);
            Assert.False(normal.Boss.Tactics!.RegularEnemy); Assert.True(final.Boss.Tactics!.ImmuneToPull);
            Assert.Equal(CampaignBossCatalog.Sprite(kind), normal.Boss.SpriteId);
            Assert.Equal(new WorldRoomId(RoomArea.Dungeon, 1001, 1, 1), normal.World.CampaignBossRoomAt(normal.Hero.Position));
            foreach (var direction in new[] { (0, -7), (7, 0), (0, 7), (-7, 0) })
                Assert.Equal(Material.Floor, normal.World.Chunks.At(normal.Arena.Center.Offset(direction.Item1, direction.Item2)));
            TacticalValidation.Validate(normal.World.Plan, normal.World.State);
            TacticalValidation.Validate(final.World.Plan, final.World.State);
        }
    }

    [Fact]
    public void InitialMaterializationRejectsObservedEditedOverlappingAndOccupiedGroundWithoutMutation()
    {
        using var a = new Encounter(CampaignBossKind.Bellkeeper);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<InvalidOperationException>(() => a.World.CreateCampaignBossArena(CampaignBossKind.Threadmother, a.Arena.Center, 74, false));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        var edited = new WorldPosition(600, 600, 1); a.World.Chunks.Edit(edited, Material.Floor);
        Assert.Throws<InvalidOperationException>(() => a.World.CreateCampaignBossArena(CampaignBossKind.Threadmother, edited, 74, false));
        var observed = new WorldPosition(700, 700, 1); a.Hero.Position = a.World.State.Position = observed; a.World.Reveal();
        Assert.Throws<InvalidOperationException>(() => a.World.CreateCampaignBossArena(CampaignBossKind.Threadmother, observed.Offset(7, 7), 74, false));
    }

    [Theory]
    [InlineData("structure")]
    [InlineData("work")]
    [InlineData("loot")]
    [InlineData("corpse")]
    [InlineData("decoy")]
    public void PlacementRejectsRegisteredPhysicalStateBeforeCreatingATimelineOrPainting(string kind)
    {
        using var world = WorldSession.Create(42); var p = new WorldPosition(512, 512, 1);
        switch (kind)
        {
            case "structure": world.State.Structures.Add(new() { Position = p, Material = Material.Floor, Foundation = Material.Rock }); break;
            case "work": world.State.Work.Add(new() { Id = 1, Target = p, Definition = "test:reserved-work", Required = 100 }); break;
            case "loot": world.State.Loot.Add(new() { Id = "test:physical-cache", Position = p, Contents = new() { ["gold"] = 2 } }); break;
            case "corpse":
                var corpse = ActorFactory.Create(world.Content, world.Content.Game.Raider, 42, world.State.NextActorId++, p, 0);
                corpse.Life = LifeState.Dead; corpse.Health = 0; world.State.Actors.Add(corpse); break;
            case "decoy":
                var combat = new TacticalCombat(world, _ => { }); var hero = world.Party.Single();
                combat.Register(hero, new() { Build = CharacterBuild.Create(ClassKind.Shade, 42) }); hero.Tactics!.Resources.Decoy = p; break;
        }
        bool hadTimeline = world.State.Tactics != null;
        string before = JsonSerializer.Serialize(world.State);
        Assert.Throws<InvalidOperationException>(() => world.CreateCampaignBossArena(CampaignBossKind.Bellkeeper, p, 73, false));
        Assert.Equal(hadTimeline, world.State.Tactics != null);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void SoundseekBaitsToCommittedNoiseAndKeepsLockedCellsAfterTargetMoves()
    {
        using var a = new Encounter(CampaignBossKind.Bellkeeper, members: 4);
        a.Hero.Position = a.World.State.Position = a.Arena.Center.Offset(-3, 2); a.World.Reveal();
        a.Act(new(TacticalAction.BasicAttack, Facing.North));
        var noisy = a.Hero.Position;
        while (a.Combat.AdvanceToDecision() is { } actor && actor.Id != a.Boss.Id) a.Act(new(TacticalAction.Wait));
        string before = JsonSerializer.Serialize(a.Combat.State);
        var plan = a.Combat.PreviewEnemyTurn(a.Hero.Id, (_, _) => null);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
        Assert.Equal(new[] { noisy }, plan.Cells);
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null); var locked = Assert.Single(a.Combat.State.Attacks);
        Assert.Equal(0, locked.IntendedTarget);
        Assert.Null(a.Combat.InspectEnemy(a.Hero.Id, a.Boss.Id)!.TargetId);
        a.NextHero(); a.Act(new(TacticalAction.Move, Facing.South)); a.NextHero();
        Assert.Equal(new[] { noisy }, locked.Cells); a.Resolve(locked);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.Equal(noisy, a.Boss.Tactics!.Enemy!.CampaignBoss!.LastNoise);
    }

    [Fact]
    public void ResonatorBreakingCancelsCommittedChimeAndLeavesLongExposedRecovery()
    {
        using var a = new Encounter(CampaignBossKind.Bellkeeper);
        var attack = a.Cast(1); Assert.Equal(CampaignBossMove.FuneralChime, attack.Boss!.Move);
        var resonator = a.Arena.Fixtures.First(); Assert.True(a.World.ObjectAt(resonator.Position)!.Solid);
        long due = attack.Due;
        a.Hit(resonator.Position);
        Assert.DoesNotContain(a.Combat.State.Attacks, x => x.Id == attack.Id);
        Assert.False(a.World.ObjectAt(resonator.Position)?.Solid == true);
        Assert.True(a.Boss.Tactics!.Enemy!.CampaignBoss!.ExposedUntil > due);
        Assert.True(a.Combat.State.Queue.Snapshot().Pending.Single(w => w.Kind == ScheduledKind.ActorReady && w.ActorId == a.Boss.Id).Due >= a.Combat.Clock + 400);
        int health = a.Boss.Health; a.Hit(a.Boss.Position, 4); Assert.Equal(health - 6, a.Boss.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ExpandingChimeWarningsHaveTilePrivateVisionAndBreakingOneCancelsOnlyItsRings()
    {
        using var a = new Encounter(CampaignBossKind.Bellkeeper, final: true);
        var attack = a.Cast(1); a.Resolve(attack);
        Assert.NotEmpty(a.Combat.State.CampaignBosses!.Pulses);
        var pulse = a.Combat.State.CampaignBosses!.Pulses.First();
        a.World.Visible.Clear(); a.World.Visible.Add(pulse.Cells[0]);
        string before = JsonSerializer.Serialize(a.Combat.State);
        Assert.NotEmpty(a.Combat.VisibleAttacks(a.World.Visible));
        a.World.Visible.Clear(); Assert.Empty(a.Combat.VisibleAttacks(a.World.Visible));
        Assert.DoesNotContain(a.Combat.VisibleTimeline(), e => e.Kind == ScheduledKind.CampaignBossPulse);
        Assert.Equal(before, JsonSerializer.Serialize(a.Combat.State));
        a.Hit(pulse.Origin);
        Assert.DoesNotContain(a.Combat.State.CampaignBosses!.Pulses, p => p.Fixture == pulse.Fixture);
        Assert.NotEmpty(a.Combat.State.CampaignBosses!.Pulses);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void PaidValveDrainsRealChannelDeniesAnnouncedFeastAndCannotBeRepeated()
    {
        using var a = new Encounter(CampaignBossKind.SluiceMaw);
        a.Boss.Health -= 20;
        var attack = a.Cast(1); Assert.Equal(CampaignBossMove.ReservoirFeast, attack.Boss!.Move);
        var valve = a.Arena.Fixtures.Single(f => f.Channel == attack.Boss.Feature);
        a.Hero.Position = a.World.State.Position = valve.Position.Offset(0, 1); a.World.Reveal();
        var action = new TacticalIntent(TacticalAction.CampaignBossInteract, Target: a.Arena.Id, Choice: (int)valve.Id);
        var preview = a.Combat.Preview(action); Assert.True(preview.Valid); Assert.True(preview.Recovery > 0);
        long clock = a.Combat.Clock; int hp = a.Boss.Health;
        a.Act(action); a.NextHero(); Assert.True(a.Combat.Clock > clock);
        Assert.True(a.Arena.Channels[attack.Boss.Feature].Drained);
        a.Resolve(attack); Assert.Equal(hp, a.Boss.Health);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "BossFeastDenied");
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(action).Success); Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        Assert.Contains(a.Arena.Channels[attack.Boss.Feature].Cells, p => a.World.Chunks.At(p) == Material.Floor);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ConnectedWetPoolFeastMovesPersistentBossAndHealsOnlyAtImpact()
    {
        using var a = new Encounter(CampaignBossKind.SluiceMaw);
        a.Boss.Health -= 20; int hp = a.Boss.Health;
        var attack = a.Cast(1); Assert.Equal(hp, a.Boss.Health);
        a.Resolve(attack);
        Assert.Equal(a.Arena.Channels[attack.Boss!.Feature].Pool, a.Boss.Position);
        Assert.Equal(hp + 14, a.Boss.Health);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "Healed" && e.Source == a.Boss.Id && e.Target == a.Boss.Id && e.Amount == 14);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ChannelSurgeKeepsLaneAndPushesActualOccupantsOnlyWhenPhysicallyOpen()
    {
        using var a = new Encounter(CampaignBossKind.SluiceMaw, final: true, members: 4);
        var channel = a.Arena.Channels[0];
        var lane = channel.Cells.First(p => p.X == a.Arena.Center.X - 2 && p.Y == a.Arena.Center.Y - 3);
        a.Hero.Position = a.World.State.Position = lane;
        // Bridges allow occupying a wet channel; a wall stops the actual wave push.
        a.World.Chunks.Edit(lane.Offset(1, 0), Material.Wall); a.World.Reveal();
        var attack = a.Cast(0); Assert.Equal(channel.Cells, attack.Cells);
        int hp = a.Hero.Health; a.Resolve(attack);
        Assert.True(a.Hero.Health < hp); Assert.Equal(lane, a.Hero.Position);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "ShoveBlocked" && e.Target == a.Hero.Id);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void PartitionHasPhysicalCuttableAnchorsAndPermanentWalkingGapForSolo()
    {
        using var a = new Encounter(CampaignBossKind.Threadmother);
        var attack = a.Cast(0); Assert.Empty(a.Arena.Fixtures); a.Resolve(attack);
        var anchor = a.Arena.Fixtures.First(f => f.Kind == CampaignBossFixtureKind.WebAnchor);
        Assert.True(a.World.ObjectAt(anchor.Position)!.Solid);
        Assert.True(a.World.GroundOpen(a.Arena.Center.Offset(2, 0)));
        a.Hero.Position = a.World.State.Position = anchor.Position.Offset(-1, 0); a.World.Reveal();
        a.Act(new(TacticalAction.BasicAttack, Facing.East)); a.NextHero();
        Assert.DoesNotContain(a.Arena.Fixtures, f => f.Group == attack.Id);
        Assert.True(a.World.GroundOpen(anchor.Position));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ReelLineCanBeCutOrLockedLandingEvadedAndWallsConstrainPull()
    {
        using var a = new Encounter(CampaignBossKind.Threadmother);
        var attack = a.Cast(1); var line = a.Arena.Fixtures.First(f => f.Kind == CampaignBossFixtureKind.ReelLine);
        Assert.False(a.World.ObjectAt(line.Position)!.Solid);
        a.Hit(line.Position, 1); var position = a.Hero.Position;
        a.Resolve(attack); Assert.Equal(position, a.Hero.Position); Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.DoesNotContain(a.Arena.Fixtures, f => f.Kind == CampaignBossFixtureKind.ReelLine);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void FireSchedulesConnectedNetworkWithFriendlyFireAndActualClearedPaths()
    {
        using var a = new Encounter(CampaignBossKind.Threadmother, members: 4, heroClass: ClassKind.Arcanist);
        var partition = a.Cast(0); a.Resolve(partition);
        var strand = a.Arena.Fixtures.First(f => f.Kind == CampaignBossFixtureKind.WebStrand);
        a.Hero.Position = a.World.State.Position = strand.Position.Offset(-1, 0);
        var ally = a.World.Party.First(m => m.Id != a.Hero.Id); ally.Position = strand.Position.Offset(1, 0);
        int hp = ally.Health;
        a.World.Reveal(); long clock = a.Combat.Clock;
        var command = new TacticalIntent(TacticalAction.Special, Facing.East);
        Assert.Contains(strand.Position, a.Combat.Preview(command).Cells);
        a.Act(command); a.NextHero(); Assert.True(a.Combat.Clock > clock);
        Assert.True(a.Combat.State.CampaignBosses!.Pulses.Count >= 3);
        Assert.All(a.Combat.State.CampaignBosses.Pulses, p => Assert.Equal(a.Hero.Id, p.Source));
        var due = a.Combat.State.CampaignBosses!.Pulses.Min(p => p.Due);
        while (a.Combat.Clock < due) { a.Act(new(TacticalAction.Wait)); a.NextHero(); }
        Assert.True(ally.Health < hp); Assert.True(a.World.GroundOpen(strand.Position));
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "BossWebBurned");
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "DamageTaken" && e.Source == a.Hero.Id && e.Target == ally.Id && e.Ability == "core:ability/boss_burning_network");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void FirstChimeImpactCannotAddUnmarkedCellsWhenAWallOpensAfterCommitment()
    {
        using var a = new Encounter(CampaignBossKind.Bellkeeper);
        var resonator = a.Arena.Fixtures.First(f => f.Kind == CampaignBossFixtureKind.Resonator);
        var opened = resonator.Position.Offset(0, 1);
        a.World.Chunks.Edit(opened, Material.Wall);
        var attack = a.Cast(1); Assert.DoesNotContain(opened, attack.Cells);
        Assert.Equal(0, attack.IntendedTarget);
        a.World.Chunks.Edit(opened, Material.Floor);
        a.Hero.Position = a.World.State.Position = opened; a.World.Reveal();
        int hp = a.Hero.Health; a.Resolve(attack);
        Assert.Equal(hp, a.Hero.Health);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "BossChimeRing");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ResonatorOnlyControlEarnsOwnedOneTimeExperienceWithoutBossDamage()
    {
        using var a = new Encounter(CampaignBossKind.Bellkeeper, members: 4);
        _ = a.Cast(1); var resonator = a.Arena.Fixtures.First();
        a.Hit(resonator.Position);
        var support = a.Combat.State.Rewards.Enemies[a.Boss.Id].Members[a.Hero.Id];
        Assert.Equal(0, support.Damage); Assert.True(support.Control > 0);
        long credit = support.Control; a.Hit(resonator.Position); Assert.Equal(credit, support.Control);
        var finisher = a.World.Party.First(m => m.Id != a.Hero.Id);
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, finisher.Id, finisher.Position,
            [a.Boss.Position], "test:boss-finisher", AttackDelivery.Melee, DamageElement.Physical, 1000, a.Combat.Clock));
        Assert.Equal(LifeState.Dead, a.Boss.Life); Assert.True(a.Boss.Tactics!.RewardsClaimed);
        Assert.True(ExperienceUnits(a.Hero) > 0); Assert.True(ExperienceUnits(finisher) > 0);
        Assert.Equal(40L * ContributionRewards.UnitsPerExperience, a.World.Party.Sum(ExperienceUnits));
        Assert.All(a.World.Party.Where(m => m.Id != a.Hero.Id && m.Id != finisher.Id), m => Assert.Equal(0, ExperienceUnits(m)));
        string rewards = JsonSerializer.Serialize(a.World.Party.Select(m => new { m.Id, m.Tactics!.Build, m.Tactics.ExperienceFraction }));
        a.Hit(a.Boss.Position, 1000);
        Assert.Equal(rewards, JsonSerializer.Serialize(a.World.Party.Select(m => new { m.Id, m.Tactics!.Build, m.Tactics.ExperienceFraction })));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    private static long ExperienceUnits(Actor member) => member.Tactics!.Build!.Experience * ContributionRewards.UnitsPerExperience + member.Tactics.ExperienceFraction;

    [Fact]
    public void CuttingSeveralReelSegmentsCreditsControlOnlyOnce()
    {
        using var a = new Encounter(CampaignBossKind.Threadmother);
        _ = a.Cast(1); var line = a.Arena.Fixtures.Where(f => f.Kind == CampaignBossFixtureKind.ReelLine).ToArray();
        Assert.True(line.Length > 1);
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Hero.Id, a.Hero.Position,
            line.Select(f => f.Position).ToArray(), "test:line-cut", AttackDelivery.Area, DamageElement.Physical, 1, a.Combat.Clock));
        var contribution = a.Combat.State.Rewards.Enemies[a.Boss.Id].Members[a.Hero.Id];
        Assert.Equal(ActionTiming.Recovery(a.Boss.Tactics!.MoveCost, a.Boss.Speed), contribution.Control);
        Assert.Equal(0, contribution.Damage);
        a.Hit(line[0].Position); Assert.Equal(ActionTiming.Recovery(a.Boss.Tactics.MoveCost, a.Boss.Speed), contribution.Control);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void CanonicalTriggeredFireIgnitesTheNetworkAndRetainsItsOwningMember()
    {
        using var a = new Encounter(CampaignBossKind.Threadmother, heroClass: ClassKind.Arcanist);
        a.Resolve(a.Cast(0));
        var strand = a.Arena.Fixtures.First(f => f.Kind == CampaignBossFixtureKind.WebStrand);
        const string boon = "core:boon/flashpoint";
        a.Hero.Tactics!.Boons.Inventory = a.Hero.Tactics.Boons.Inventory.Grant(boon, BoonRarity.Common, ClassKind.Arcanist);
        long action = a.Combat.State.NextAction++;
        var e = new BoonEvent { Id = a.Combat.State.NextEvent++, ChainId = action, RootActionId = action,
            Kind = BoonTrigger.HitConfirmed, SourceId = a.Hero.Id, SourceFaction = "player",
            SourcePosition = a.Hero.Position, Position = strand.Position };
        var activation = new BoonActivation(a.Combat.State.BoonEffects.Dispatch.NextActivationId++, boon, a.Hero.Id,
            "player", a.Hero.Position, action, action, e.Id, [new(a.Hero.Id, boon)], 1, e);
        a.Combat.ExecuteBoonEffect(activation, BoonCatalog.Default.Get(boon).Effects.Single());
        Assert.True(a.Combat.State.CampaignBosses!.Pulses.Count >= 3);
        Assert.All(a.Combat.State.CampaignBosses.Pulses, p => Assert.Equal(a.Hero.Id, p.Source));
        Assert.Contains(a.Combat.State.Events, ev => ev.Kind == "BossWebIgnited" && ev.Source == a.Hero.Id);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void EnvironmentalFireKeepsSourceZeroThroughDelayedFriendlyFire()
    {
        using var a = new Encounter(CampaignBossKind.Threadmother, members: 2);
        a.Resolve(a.Cast(0));
        var strand = a.Arena.Fixtures.First(f => f.Kind == CampaignBossFixtureKind.WebStrand);
        var friend = a.World.Party.First(m => m.Id != a.Hero.Id); friend.Position = strand.Position.Offset(1, 0);
        int hp = friend.Health;
        long action = a.Combat.State.NextAction++;
        a.Combat.ResolveAttack(new(action, 0, strand.Position, [strand.Position], "core:terrain/heat_vent",
            AttackDelivery.Area, DamageElement.Fire, 1, a.Combat.Clock, Cause: DamageCause.GroundHazard));
        Assert.NotEmpty(a.Combat.State.CampaignBosses!.Pulses);
        Assert.All(a.Combat.State.CampaignBosses.Pulses, p => Assert.Equal(0, p.Source));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        var due = a.Combat.State.CampaignBosses.Pulses.Min(p => p.Due);
        while (a.Combat.Clock < due) { a.Act(new(TacticalAction.Wait)); a.NextHero(); }
        Assert.True(friend.Health < hp);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "DamageTaken" && e.Source == 0 && e.Target == friend.Id
            && e.Ability == "core:ability/boss_burning_network");
        Assert.DoesNotContain(a.Combat.State.Rewards.Enemies.Values.SelectMany(e => e.Members.Keys), id => id == 0);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DelayedNetworkRetainsProcAncestryAndGeneratedExclusionThroughWorldSave(bool generated)
    {
        using var a = new Encounter(CampaignBossKind.Threadmother);
        a.Resolve(a.Cast(0));
        var strand = a.Arena.Fixtures.First(f => f.Kind == CampaignBossFixtureKind.WebStrand);
        const string dividend = "core:boon/grave_dividend", ancestor = "core:boon/flashpoint";
        a.Hero.Tactics!.Boons.Inventory = a.Hero.Tactics.Boons.Inventory.Grant(dividend, BoonRarity.Common, ClassKind.Adventurer);
        long action = a.Combat.State.NextAction++;
        var cause = new BoonCause(action, action, [new(a.Hero.Id, ancestor)], .25, generated);
        a.Combat.ResolveAttack(new(action, a.Hero.Id, a.Hero.Position, [strand.Position], "test:generated-web-fire",
            AttackDelivery.Area, DamageElement.Fire, 1, a.Combat.Clock, Cause: DamageCause.TriggeredEffect,
            ClassGenerated: generated, Causality: cause));
        Assert.All(a.Combat.State.CampaignBosses!.Pulses, p => Assert.Equal(cause, p.Causality));
        a.Boss.Position = strand.Position.Offset(1, 0); a.Boss.Health = 1;
        a.Hero.Health -= 5; int hp = a.Hero.Health, maximum = a.Hero.MaxHealth;
        string directory = Path.Combine(Path.GetTempPath(), "roag-network-cause-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            long due = a.Combat.State.CampaignBosses.Pulses.Min(p => p.Due);
            while (a.Combat.Clock < due) { a.Act(new(TacticalAction.Wait)); a.NextHero(); }
            while (resumed.Clock < due)
            {
                Assert.True(resumed.Commit(new(TacticalAction.Wait)).Success);
                while (resumed.AdvanceToDecision() is { } actor && actor.Id != a.Hero.Id) resumed.CommitWorldEnemyTurn();
            }
            Assert.Equal(LifeState.Dead, a.Boss.Life);
            Assert.Equal(hp + a.Hero.MaxHealth - maximum + (generated ? 0 : 1), a.Hero.Health);
            Assert.Equal(!generated, a.Combat.State.Events.Any(e => e.Kind == "BoonActivated" && e.Ability == dividend));
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            TacticalValidation.Validate(loaded.Plan, loaded.State);
            var pending = a.Combat.State.CampaignBosses.Pulses.First();
            a.Combat.State.CampaignBosses.Pulses[0] = pending with { Causality = cause with { ProcCoefficient = double.NaN } };
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(CampaignBossKind.Bellkeeper, false)]
    [InlineData(CampaignBossKind.SluiceMaw, false)]
    [InlineData(CampaignBossKind.Threadmother, true)]
    [InlineData(CampaignBossKind.OssuaryCaptain, false)]
    public void EveryAdditionalRoleHasFreePrivateInspectionOfItsActualTargetingPolicy(CampaignBossKind kind, bool vulnerable)
    {
        using var a = new Encounter(kind, members: 2);
        var friend = a.World.Party.First(m => m.Id != a.Hero.Id);
        a.Hero.Position = a.World.State.Position = a.Arena.Center.Offset(0, 2);
        friend.Position = a.Arena.Center.Offset(3, 2); friend.Health = 1; a.World.Reveal();
        long target = vulnerable ? friend.Id : a.Hero.Id;
        Assert.Equal(target, a.Combat.SelectEnemyTarget(a.Boss.Id));
        string before = JsonSerializer.Serialize(a.World.State);
        var inspection = a.Combat.InspectEnemy(a.Hero.Id, a.Boss.Id)!;
        Assert.Equal(target, inspection.TargetId);
        Assert.Contains(vulnerable ? "lowest HP percentage" : "standing threats before downed prey", inspection.Tendency);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Boss.Id, a.Boss.Position, [friend.Position],
            "test:down-prey", AttackDelivery.Area, DamageElement.Physical, 1000, a.Combat.Clock));
        Assert.Equal(LifeState.Downed, friend.Life);
        Assert.Equal(target, a.Combat.SelectEnemyTarget(a.Boss.Id));
        a.Hero.Position = a.World.State.Position = a.Hero.Position with { Floor = 0 };
        Assert.Null(a.Combat.InspectEnemy(a.Hero.Id, a.Boss.Id));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(CampaignBossKind.Bellkeeper)]
    [InlineData(CampaignBossKind.SluiceMaw)]
    [InlineData(CampaignBossKind.Threadmother)]
    [InlineData(CampaignBossKind.OssuaryCaptain)]
    public void ArenaGuardsChooseInsidePreyEvenWhenAnOutsideMemberIsNearer(CampaignBossKind kind)
    {
        using var a = new Encounter(kind, members: 2);
        a.Boss.Position = a.Arena.Center.Offset(0, -6);
        a.Hero.Position = a.World.State.Position = a.Arena.Center.Offset(0, -8);
        a.World.Chunks.Edit(a.Hero.Position, Material.Floor);
        var friend = a.World.Party.First(m => m.Id != a.Hero.Id); friend.Position = a.Arena.Center.Offset(5, -5);
        a.World.Reveal();
        Assert.True(a.Boss.Position.Distance(a.Hero.Position) < a.Boss.Position.Distance(friend.Position));
        Assert.Contains(a.Hero.Position, a.World.VisionFrom(a.Boss.Position));
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Equal(friend.Id, a.Combat.SelectEnemyTarget(a.Boss.Id));
        Assert.Contains("Guards this arena", a.Combat.CampaignBossDescription(a.Boss.Id));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(CampaignBossKind.Bellkeeper)]
    [InlineData(CampaignBossKind.SluiceMaw)]
    [InlineData(CampaignBossKind.Threadmother)]
    [InlineData(CampaignBossKind.OssuaryCaptain)]
    public void RetreatSaveAndReentryRetainTheActualBossAndPendingWorldConsequences(CampaignBossKind kind)
    {
        using var a = new Encounter(kind, members: 2);
        _ = a.Cast(1); a.Boss.Health -= 7;
        a.Hero.Position = a.World.State.Position = a.Arena.Center.Offset(0, 8);
        a.World.Chunks.Edit(a.Hero.Position, Material.Floor);
        var friend = a.World.Party.First(m => m.Id != a.Hero.Id);
        friend.Position = a.Hero.Position.Offset(1, 0); a.World.Chunks.Edit(friend.Position, Material.Floor); a.World.Reveal();
        Assert.Null(a.Combat.SelectEnemyTarget(a.Boss.Id));
        string directory = Path.Combine(Path.GetTempPath(), "roag-boss-retreat-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            for (int i = 0; i < 6; i++)
            {
                a.Act(new(TacticalAction.Wait)); a.NextHero();
                Assert.True(resumed.Commit(new(TacticalAction.Wait)).Success);
                for (int turns = 0; turns < 100; turns++)
                {
                    var actor = resumed.AdvanceToDecision(); Assert.NotNull(actor);
                    if (actor.Id == a.Hero.Id) break;
                    if (actor.Tactics!.Enemy != null) resumed.CommitWorldEnemyTurn();
                    else Assert.True(resumed.Commit(new(TacticalAction.Wait)).Success);
                }
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
                Assert.Equal(a.Boss.Health, loaded.ActorById(a.Boss.Id).Health);
                Assert.True(a.Arena.Contains(a.Boss.Position));
            }
            long moves = a.Boss.Tactics!.Enemy!.CampaignBoss!.Moves;
            int hp = a.Boss.Health; var position = a.Boss.Position;
            string fixtures = JsonSerializer.Serialize(a.Arena.Fixtures);
            a.Hero.Position = a.World.State.Position = a.Arena.Center.Offset(0, 6); a.World.Reveal();
            Assert.Equal(a.Boss.Id, a.Arena.Boss); Assert.Equal(hp, a.Boss.Health); Assert.Equal(position, a.Boss.Position);
            Assert.Equal(fixtures, JsonSerializer.Serialize(a.Arena.Fixtures));
            a.Act(new(TacticalAction.Wait)); a.NextHero();
            Assert.True(a.Boss.Tactics.Enemy.CampaignBoss.Moves > moves);
            TacticalValidation.Validate(a.World.Plan, a.World.State); TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(CampaignBossKind.Bellkeeper, false)]
    [InlineData(CampaignBossKind.Bellkeeper, true)]
    [InlineData(CampaignBossKind.SluiceMaw, false)]
    [InlineData(CampaignBossKind.SluiceMaw, true)]
    [InlineData(CampaignBossKind.Threadmother, false)]
    [InlineData(CampaignBossKind.Threadmother, true)]
    [InlineData(CampaignBossKind.OssuaryCaptain, false)]
    [InlineData(CampaignBossKind.OssuaryCaptain, true)]
    public void CanonicalShoveThroughAnOpenExitSavesOutsideAndReturnsOnlyByPaidPhysicalSteps(CampaignBossKind kind, bool blocked)
    {
        using var a = new Encounter(kind);
        var exit = a.Arena.Center.Offset(0, -7); var outside = exit.Offset(0, -1);
        a.World.Chunks.Edit(outside, Material.Floor);
        foreach (var p in new[] { outside.Offset(-1, 0), outside.Offset(1, 0), outside.Offset(0, -1) })
            a.World.Chunks.Edit(p, Material.Wall);
        a.Hero.Position = a.World.State.Position = exit;
        var rammer = ActorFactory.Create(a.World.Content, a.World.Content.Game.Raider, 42,
            a.World.State.NextActorId++, exit.Offset(0, 1), 0);
        a.World.State.Actors.Add(rammer); a.Combat.RegisterEnemy(rammer, "core:enemy/heavy_rammer", 0);
        a.World.Reveal(); a.Act(new(TacticalAction.Wait)); Assert.Same(rammer, a.Combat.AdvanceToDecision());
        a.Combat.CommitEnemyTurn(a.Hero.Id, (_, _) => null);
        var ram = Assert.Single(a.Combat.State.Attacks); Assert.NotNull(ram.Shove);
        a.NextHero();
        a.Hero.Position = a.World.State.Position = exit.Offset(1, 1); a.Boss.Position = exit; a.World.Reveal();
        a.Act(new(TacticalAction.Wait)); a.NextHero();
        Assert.Equal(outside, a.Boss.Position);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "Displaced" && e.Source == rammer.Id && e.Target == a.Boss.Id && e.Position == outside);
        if (blocked) a.World.Chunks.Edit(exit, Material.Wall);
        string directory = Path.Combine(Path.GetTempPath(), "roag-boss-outside-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(outside, loaded.ActorById(a.Boss.Id).Position);
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            bool sawBlocked = false, returned = false;
            for (int turn = 0; turn < 8 && !returned; turn++)
            {
                a.Act(new(TacticalAction.Wait)); Assert.True(resumed.Commit(new(TacticalAction.Wait)).Success);
                for (int work = 0; work < 20; work++)
                {
                    var actor = a.Combat.AdvanceToDecision(); var restored = resumed.AdvanceToDecision();
                    Assert.NotNull(actor); Assert.NotNull(restored); Assert.Equal(actor.Id, restored.Id);
                    if (actor.Id == a.Hero.Id) break;
                    bool returnStep = actor.Id == a.Boss.Id && !a.Arena.Contains(actor.Position);
                    int hp = a.Boss.Health; long moves = a.Boss.Tactics!.Enemy!.CampaignBoss!.Moves;
                    string fixtures = JsonSerializer.Serialize(a.Arena.Fixtures);
                    if (returnStep)
                    {
                        string before = JsonSerializer.Serialize(a.World.State);
                        Assert.Equal(blocked && !sawBlocked ? EnemyIntentKind.Wait : EnemyIntentKind.Move, a.Combat.PreviewWorldEnemyTurn().Kind);
                        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
                    }
                    a.Combat.CommitWorldEnemyTurn(); resumed.CommitWorldEnemyTurn();
                    if (returnStep)
                    {
                        Assert.Equal(hp, a.Boss.Health); Assert.Equal(moves, a.Boss.Tactics.Enemy.CampaignBoss.Moves);
                        Assert.Equal(fixtures, JsonSerializer.Serialize(a.Arena.Fixtures));
                        Assert.DoesNotContain(a.Combat.State.Attacks, w => w.Source == a.Boss.Id);
                        Assert.Equal(a.Combat.Clock + ActionTiming.Recovery(a.Boss.Tactics.MoveCost, a.Boss.Speed),
                            a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Boss.Id && w.Kind == ScheduledKind.ActorReady).Due);
                        if (blocked && !sawBlocked)
                        {
                            Assert.Equal(outside, a.Boss.Position); sawBlocked = true;
                            a.World.Chunks.Edit(exit, Material.Floor); loaded.Chunks.Edit(exit, Material.Floor);
                        }
                        else
                        { Assert.Equal(exit, a.Boss.Position); Assert.Equal(1, outside.Distance(a.Boss.Position)); returned = true; }
                    }
                }
                Assert.Equal(a.Boss.Position, loaded.ActorById(a.Boss.Id).Position);
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            }
            Assert.True(returned); Assert.Equal(blocked, sawBlocked);
            TacticalValidation.Validate(a.World.Plan, a.World.State); TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FinalThreadmotherPartitionsHaveIndependentAnchorsAndFireNetworks(int variation)
    {
        using var cut = new Encounter(CampaignBossKind.Threadmother, final: true, variation: variation);
        cut.Resolve(cut.Cast(0));
        var groups = cut.Arena.Fixtures.GroupBy(f => f.Group).ToArray(); Assert.Equal(2, groups.Length);
        var anchor = groups[0].First(f => f.Kind == CampaignBossFixtureKind.WebAnchor);
        var remaining = groups[1].Select(f => f.Id).ToArray();
        cut.Hit(anchor.Position);
        Assert.Equal(remaining, cut.Arena.Fixtures.Select(f => f.Id));
        Assert.All(cut.Arena.Fixtures, f => Assert.False(cut.World.GroundOpen(f.Position)));
        TacticalValidation.Validate(cut.World.Plan, cut.World.State);
        using var burn = new Encounter(CampaignBossKind.Threadmother, final: true, variation: variation);
        burn.Resolve(burn.Cast(0));
        var strand = burn.Arena.Fixtures.First(f => f.Kind == CampaignBossFixtureKind.WebStrand);
        var connected = burn.Arena.Fixtures.Where(f => f.Group == strand.Group).Select(f => f.Id).ToHashSet();
        burn.Hit(strand.Position, 1, DamageElement.Fire);
        Assert.NotEmpty(burn.Combat.State.CampaignBosses!.Pulses);
        Assert.All(burn.Combat.State.CampaignBosses.Pulses, p => Assert.Contains(p.Fixture, connected));
        Assert.All(burn.Arena.Fixtures.Where(f => f.Group != strand.Group), f => Assert.Equal(5, f.Health));
        TacticalValidation.Validate(burn.World.Plan, burn.World.State);
    }

    [Fact]
    public void CaptainProtectsFrontAndDiagonalsWhileFlanksAndChargeRecoveryExposeIt()
    {
        using var a = new Encounter(CampaignBossKind.OssuaryCaptain);
        a.Hero.Position = a.World.State.Position = a.Boss.Position.Offset(0, 2);
        int hp = a.Boss.Health; a.Hit(a.Boss.Position, 10); Assert.InRange(hp - a.Boss.Health, 0, 2);
        a.Hero.Position = a.Boss.Position.Offset(2, 0); hp = a.Boss.Health; a.Hit(a.Boss.Position, 10); Assert.Equal(hp - 9, a.Boss.Health);
        a.Hero.Position = a.World.State.Position = a.Arena.Center.Offset(0, 6); a.World.Reveal();
        var attack = a.Cast(0); a.World.Chunks.Edit(attack.Cells[1], Material.Wall);
        a.Resolve(attack); Assert.Equal(attack.Cells[0], a.Boss.Position);
        Assert.True(a.Boss.Tactics!.Enemy!.CampaignBoss!.ExposedUntil > a.Combat.Clock);
        hp = a.Boss.Health; a.Hit(a.Boss.Position, 4); Assert.Equal(hp - 5, a.Boss.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void BoneRampartsLeaveCentralGapAndBreakUnderActualPaidAttack()
    {
        using var a = new Encounter(CampaignBossKind.OssuaryCaptain);
        var attack = a.Cast(1); a.Resolve(attack);
        Assert.True(a.World.GroundOpen(a.Arena.Center.Offset(0, 2)));
        var bone = a.Arena.Fixtures.First(); Assert.False(a.World.GroundOpen(bone.Position));
        a.Hero.Position = a.World.State.Position = bone.Position.Offset(0, 1); a.World.Reveal();
        a.Act(new(TacticalAction.BasicAttack, Facing.North)); a.NextHero();
        Assert.True(a.World.GroundOpen(bone.Position));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(CampaignBossKind.Bellkeeper, 0, false)]
    [InlineData(CampaignBossKind.Bellkeeper, 1, true)]
    [InlineData(CampaignBossKind.SluiceMaw, 0, false)]
    [InlineData(CampaignBossKind.SluiceMaw, 1, true)]
    [InlineData(CampaignBossKind.Threadmother, 0, false)]
    [InlineData(CampaignBossKind.Threadmother, 1, true)]
    [InlineData(CampaignBossKind.OssuaryCaptain, 0, false)]
    [InlineData(CampaignBossKind.OssuaryCaptain, 1, true)]
    public void OrdinaryWorldSaveExactlyContinuesEveryCommittedMoveAndSharedPartyTimeline(CampaignBossKind kind, int move, bool final)
    {
        using var a = new Encounter(kind, final, variation: 1, members: 4);
        _ = a.Cast(move);
        string directory = Path.Combine(Path.GetTempPath(), "roag-campaign-boss-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "slot"); a.World.Save(slot);
            using var loaded = WorldSession.Load(slot); var combat = new TacticalCombat(loaded, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(combat.State));
            for (int i = 0; i < 5; i++)
            {
                a.Act(new(TacticalAction.Wait)); a.NextHero();
                Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
                for (int turns = 0; turns < 100; turns++)
                {
                    var actor = combat.AdvanceToDecision(); Assert.NotNull(actor);
                    if (actor.Id == a.Hero.Id) break;
                    if (actor.Tactics!.Enemy != null) combat.CommitWorldEnemyTurn(); else Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
                }
                Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(combat.State));
                Assert.Equal(JsonSerializer.Serialize(a.World.State.Actors.Select(x => new { x.Id, x.Position, x.Health, x.Life, x.Tactics })),
                    JsonSerializer.Serialize(loaded.State.Actors.Select(x => new { x.Id, x.Position, x.Health, x.Life, x.Tactics })));
                TacticalValidation.Validate(loaded.Plan, loaded.State);
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void SurgeActuallyPushesAcrossWetTilesWithoutGrantingOrdinarySwimming()
    {
        using var a = new Encounter(CampaignBossKind.SluiceMaw, final: true);
        var start = a.Arena.Center.Offset(-2, -3);
        a.Hero.Position = a.World.State.Position = start; a.World.Reveal();
        Assert.Equal(Material.Water, a.World.Chunks.At(start.Offset(1, 0)));
        Assert.False(a.World.GroundOpen(start.Offset(1, 0)));
        var attack = a.Cast(0); a.Resolve(attack);
        Assert.Equal(start.Offset(2, 0), a.Hero.Position);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "Displaced" && e.Target == a.Hero.Id);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void NewWallStopsChargeDamageAndMovementBeforeBodiesBeyondTheObstruction()
    {
        using var a = new Encounter(CampaignBossKind.OssuaryCaptain, members: 4);
        var ally = a.World.Party.First(m => m.Id != a.Hero.Id); ally.Position = a.Arena.Center.Offset(0, 3);
        int hp = ally.Health;
        var attack = a.Cast(0); Assert.Contains(ally.Position, attack.Cells);
        a.World.Chunks.Edit(a.Arena.Center.Offset(0, 2), Material.Wall);
        a.Resolve(attack);
        Assert.Equal(hp, ally.Health); Assert.Equal(a.Arena.Center.Offset(0, 3), ally.Position);
        Assert.Equal(a.Arena.Center.Offset(0, 1), a.Boss.Position);
        Assert.Contains(a.Combat.State.Events, e => e.Kind == "BossExposed");
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReelPullsActualOccupantButMovingOffItsLockedTileEvadesIt(bool evade)
    {
        using var a = new Encounter(CampaignBossKind.Threadmother);
        var attack = a.Cast(1); var start = a.Hero.Position;
        if (evade) { a.Act(new(TacticalAction.Move, Facing.East)); a.NextHero(); }
        a.Resolve(attack);
        Assert.Equal(evade ? start.Offset(1, 0) : start.Offset(0, -2), a.Hero.Position);
        Assert.Equal(a.Hero.MaxHealth - (evade ? 0 : a.Boss.Power), a.Hero.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void ForeignAndMalformedBossWindupsAreRejectedBeforeChangingAnyState()
    {
        using var a = new Encounter(CampaignBossKind.Bellkeeper);
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<ArgumentException>(() => a.Combat.CommitWindup("core:ability/boss_soundseek", [a.Boss.Position],
            AttackDelivery.Area, DamageElement.Physical, 5, 160, 140, boss: new(CampaignBossMove.Soundseek)));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        a.Act(new(TacticalAction.Wait)); Assert.Same(a.Boss, a.Combat.AdvanceToDecision());
        before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<ArgumentException>(() => a.Combat.CommitWindup("core:ability/boss_channelsurge", [a.Hero.Position],
            AttackDelivery.Area, DamageElement.Physical, 5, 160, 140, boss: new(CampaignBossMove.ChannelSurge, 0)));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void EmptyChargeAndMixedControllerEffectsAreRejectedWithoutSpendingReadiness()
    {
        using var a = new Encounter(CampaignBossKind.OssuaryCaptain);
        a.Act(new(TacticalAction.Wait)); Assert.Same(a.Boss, a.Combat.AdvanceToDecision());
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.Throws<ArgumentException>(() => a.Combat.CommitWindup("core:ability/boss_shieldadvance", [],
            AttackDelivery.Area, DamageElement.Physical, 5, 160, 240, boss: new(CampaignBossMove.ShieldAdvance)));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        Assert.Throws<ArgumentException>(() => a.Combat.CommitWindup("core:ability/boss_shieldadvance", [a.Boss.Position.Offset(0, 1)],
            AttackDelivery.Area, DamageElement.Physical, 5, 160, 240,
            advance: new(Facing.South, 1), boss: new(CampaignBossMove.ShieldAdvance)));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State)); Assert.Same(a.Boss, a.Combat.Active);
    }

    [Fact]
    public void AbsentNewStateInAnOlderNativeSaveRestoresAnEmptyRegistryWithoutChangingItsTimeline()
    {
        using var world = WorldSession.Create(42); var combat = new TacticalCombat(world, _ => { });
        combat.Register(world.Party.Single(), new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42) });
        var old = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(combat.State))!;
        old.AsObject().Remove("CampaignBosses");
        var restored = JsonSerializer.Deserialize<TacticalWorldState>(old.ToJsonString())!;
        Assert.Null(restored.CampaignBosses);
        Assert.Equal(old.ToJsonString(), JsonSerializer.Serialize(restored));
        Assert.Equal(JsonSerializer.Serialize(combat.State.Timeline), JsonSerializer.Serialize(restored.Timeline));
        world.State.Tactics = restored; TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void PreBossNativeWorldManifestWithOriginalWindupLoadsAndResavesByteForByte()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var combat = world.DungeonCombat; var hero = world.DungeonMember;
        var position = new[] { Facing.North, Facing.East, Facing.South, Facing.West }
            .Select(f => { var (x,y) = AttackGeometry.Offset(f); return hero.Position.Offset(x,y); }).First(p => world.CanEnter(p));
        var enemy = ActorFactory.Create(world.Content, world.Content.Game.Raider, world.Seed, world.State.NextActorId++, position, 0);
        enemy.CombatTrait = ""; world.State.Actors.Add(enemy);
        combat.RegisterEnemy(enemy, "core:enemy/mossbound_guard", combat.Clock);
        Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
        // Existing world creatures already own earlier same-time readiness slots.
        // Let those real actions settle; the newly registered guard never skips them.
        Actor? ready = null;
        for (int i = 0; i <= combat.State.Participants.Count; i++)
        {
            ready = combat.AdvanceToDecision(); Assert.NotNull(ready);
            if (ready.Id == enemy.Id) break;
            Assert.NotNull(ready.Tactics!.Enemy); combat.CommitWorldEnemyTurn();
        }
        Assert.Same(enemy, ready);
        combat.CommitEnemyTurn(hero.Id, (_, _) => null); world.Reveal();
        Assert.Contains(combat.State.Attacks, a => a.Source == enemy.Id);
        Assert.Null(combat.State.CampaignBosses);
        _ = combat.VisibleAttacks(world.Visible); _ = combat.VisibleTimeline();
        _ = combat.InspectEnemy(hero.Id, enemy.Id); _ = WorldInspection.Inspect(world, enemy.Position);
        Assert.Null(world.CampaignBossRoomAt(hero.Position)); Assert.Null(combat.State.CampaignBosses);
        string directory = Path.Combine(Path.GetTempPath(), "roag-old-boss-compatibility-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "old-slot"); world.Save(slot);
            byte[] original = File.ReadAllBytes(slot);
            using (var manifest = JsonDocument.Parse(original))
            {
                var tactics = manifest.RootElement.GetProperty("state").GetProperty("tactics");
                Assert.False(tactics.TryGetProperty("campaignBosses", out _));
                foreach (var attack in tactics.GetProperty("attacks").EnumerateArray()) Assert.False(attack.TryGetProperty("boss", out _));
                foreach (var actor in manifest.RootElement.GetProperty("state").GetProperty("actors").EnumerateArray())
                    if (actor.GetProperty("tactics").ValueKind == JsonValueKind.Object
                        && actor.GetProperty("tactics").GetProperty("enemy").ValueKind == JsonValueKind.Object)
                        Assert.False(actor.GetProperty("tactics").GetProperty("enemy").TryGetProperty("campaignBoss", out _));
            }
            using var loaded = WorldSession.Load(slot);
            Assert.Null(loaded.State.Tactics!.CampaignBosses);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
            loaded.Save(slot); Assert.Equal(original, File.ReadAllBytes(slot));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("role")]
    [InlineData("stats")]
    [InlineData("arena")]
    [InlineData("cast")]
    [InlineData("fixture")]
    [InlineData("pulse")]
    public void SaveValidationRejectsForeignRolesMutatedDifficultyAndOrphanedWork(string corruption)
    {
        using var a = new Encounter(CampaignBossKind.Bellkeeper);
        var attack = a.Cast(1);
        switch (corruption)
        {
            case "role": a.Boss.Tactics!.DefinitionId = "core:enemy/sluice_maw"; break;
            case "stats": a.Boss.Tactics!.InnateStats = a.Boss.Tactics.Stats with { MaxHealth = 1000 }; break;
            case "arena": a.Combat.State.CampaignBosses!.Arenas.Clear(); break;
            case "cast": a.Combat.State.Attacks[0] = attack with { Boss = new(CampaignBossMove.ChannelSurge, 0) }; break;
            case "fixture": a.Arena.Fixtures[0].Channel = 99; break;
            case "pulse": a.Combat.State.Queue.Schedule(ScheduledKind.CampaignBossPulse, a.Combat.Clock + 100, a.Boss.Id, referenceId: 1); break;
        }
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }
}
