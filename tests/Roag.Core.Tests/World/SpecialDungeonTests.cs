using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class SpecialDungeonTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(42)]
    public void FreshWorldSelectsOnlyASpecialSubsetAndNeverDoomsTheSelectedFinalGuardian(int seed)
    {
        using var world = WorldSession.CreateOpenCampaign(seed);
        var complexes = world.State.NativeCampaign!.Complexes!;
        var special = complexes.Where(c => c.Special != null).ToArray();
        Assert.Equal(2, special.Length); Assert.Equal(2, complexes.Count(c => c.Special == null));
        Assert.Equal(Enum.GetValues<SpecialDungeonAftermath>().Order(), special.Select(c => c.Special!.Aftermath).Order());
        Assert.All(special, c =>
        {
            Assert.DoesNotContain(c.Run.Floors, f => f.Boss == world.FinalBoss.Id);
            Assert.Null(c.Special!.ActivatedAt); Assert.Equal(0, c.Special.DeadlineSchedule);
        });
        Assert.Empty(world.KnownSpecialDungeons());
        Assert.DoesNotContain(world.State.Tactics!.Queue.Snapshot().Pending,
            w => w.Kind is ScheduledKind.StageDeadline or ScheduledKind.StagePressure or ScheduledKind.SpecialPursuerArrival);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Throws<InvalidOperationException>(() => world.InitializeSpecialDungeons(world.FinalBoss.Id));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    private sealed class Visit : IDisposable
    {
        public WorldSession World { get; } = WorldSession.CreateNativeCampaign(42);
        public DungeonComplexState Complex { get; }
        public Actor Hero => World.Party.First();
        public SpecialDungeonState Special => Complex.Special!;
        public WorldPosition Entry => WorldSession.SpecialDungeonActivation(Complex);
        public Visit(SpecialDungeonAftermath aftermath, SpecialDungeonTuning? tuning = null)
        {
            var parcel = DungeonParcelPlanner.Reserve(World.Plan, 1).Single();
            Complex = World.InstallDungeonComplex(DungeonComplexPlanner.Generate(World.Plan, parcel));
            Complex.KnownEntrance = true;
            Complex.Special = new() { Aftermath = aftermath, Tuning = tuning ?? new(DurationSeconds: 3, FirstPressure: 10000, PursuerDelay: 10000),
                RandomState = RoagRandom.Derive(Complex.Seed, "special-pressure") };
            // Explicit physical unit placement, never a natural/rendered travel claim.
            Hero.Position = World.State.Position = Entry;
            Assert.True(World.SelectDungeonMember(Hero.Id).Success);
        }
        public void Activate()
        {
            var result = World.SubmitRoag(new("activate_special", Target: Complex.Parcel.Id, Choice: 1));
            Assert.True(result.Success, result.Message);
        }
        public void Dispose() => World.Dispose();
    }

    [Fact]
    public void EntryAndFreeInspectionDoNotActivateAndUnconfirmedOrDistantActionsAreRejectedFreely()
    {
        using var f = new Visit(SpecialDungeonAftermath.PartialCollapse);
        string before = JsonSerializer.Serialize(f.World.State); long clock = f.World.DungeonCombat.Clock;
        Assert.Single(f.World.KnownSpecialDungeons());
        Assert.False(f.World.SubmitRoag(new("activate_special", Target: f.Complex.Parcel.Id)).Success);
        Assert.Null(f.Special.ActivatedAt);
        Assert.DoesNotContain(f.World.State.Tactics!.Queue.Snapshot().Pending,
            w => w.Kind is ScheduledKind.StageDeadline or ScheduledKind.StagePressure or ScheduledKind.SpecialPursuerArrival);
        Assert.Equal(clock, f.World.DungeonCombat.Clock); Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
        Assert.True(f.World.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
        Assert.Equal(0, f.Hero.Position.Floor);
        before = JsonSerializer.Serialize(f.World.State);
        Assert.False(f.World.SubmitRoag(new("activate_special", Target: f.Complex.Parcel.Id, Choice: 1)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
        RoundTrip(f.World, loaded => Assert.Null(loaded.State.NativeCampaign!.Complexes![0].Special!.ActivatedAt));
    }

    [Fact]
    public void NonlethalDeadlineContinuesOutsideAcrossExactReloadAndReentryNeverResetsRewardsOrTerrain()
    {
        using var f = new Visit(SpecialDungeonAftermath.PartialCollapse);
        string chests = JsonSerializer.Serialize(f.World.State.Tactics!.Chests);
        f.Activate(); long deadline = f.Special.ExpiresAt;
        Assert.True(f.World.DungeonCombat.Clock < deadline);
        RoundTrip(f.World, loaded =>
        {
            foreach (var world in new[] { f.World, loaded })
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
                Assert.Equal(0, world.DungeonMember.Position.Floor);
                while (world.DungeonCombat.Clock < deadline) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
                var special = world.State.NativeCampaign!.Complexes![0].Special!;
                Assert.Equal(deadline, special.ExpiredAt); Assert.Equal(deadline, special.ExpiresAt);
                Assert.Equal(LifeState.Alive, world.DungeonMember.Life);
                Assert.Empty(special.Casualties); Assert.NotEmpty(special.Rubble);
                Assert.All(special.Rubble, p => Assert.Equal(Material.Wall, world.Chunks.At(p)));
                Assert.Equal(chests, JsonSerializer.Serialize(world.State.Tactics!.Chests));
                Assert.True(world.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
                Assert.Equal(1, world.DungeonMember.Position.Floor);
                string before = JsonSerializer.Serialize(world.State);
                Assert.False(world.SubmitRoag(new("activate_special", Target: f.Complex.Parcel.Id, Choice: 1)).Success);
                Assert.Equal(before, JsonSerializer.Serialize(world.State));
                TacticalValidation.Validate(world.Plan, world.State);
            }
            Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(loaded.State));
        });
        RoundTrip(f.World, loaded => Assert.Equal(f.Special.Rubble, loaded.State.NativeCampaign!.Complexes![0].Special!.Rubble));
    }

    [Fact]
    public void RealPursuerArrivalIsSavedOnceAndCannotFollowThroughPhysicalReturnStairs()
    {
        using var f = new Visit(SpecialDungeonAftermath.PartialCollapse,
            new(DurationSeconds: 20, FirstPressure: 50, PursuerDelay: 100, FirstPeriod: 80, SecondPeriod: 80, ThirdPeriod: 80));
        // Physical unit fixture: an already explored, open-door expedition.
        // No door identity is replaced and this setup is not natural play evidence.
        foreach (var door in f.World.State.Tactics!.Doors.Nodes.Where(d => f.Complex.Contains(d.Position)))
            f.World.Chunks.Edit(door.Position, Material.Door);
        f.World.Reveal();
        int gameplayRandom = f.World.State.Tactics!.RandomState;
        f.Activate();
        Assert.True(f.Special.Pursuer > 0); var pursuer = f.World.ActorById(f.Special.Pursuer);
        Assert.Equal("core:enemy/extraction_pursuer", pursuer.Tactics!.DefinitionId);
        Assert.Equal(SpecialDungeonRules.Boundary(f.Complex), pursuer.Tactics.Enemy!.PursuitBoundary);
        Assert.Equal(gameplayRandom, f.World.State.Tactics.RandomState); // Pressure owns its own content stream.
        Assert.DoesNotContain(f.World.DungeonCombat.VisibleTimeline(), e => e.ActorId == pursuer.Id);
        Assert.DoesNotContain(pursuer.Position, f.World.Visible);
        Assert.True(f.World.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
        long originalDeadline = f.Special.ExpiresAt, pursuerId = f.Special.Pursuer;
        // Isolate the actual owning-boundary stair decision on the spawned actor.
        pursuer.Position = f.Entry;
        for (int i = 0; i < 5; i++) Assert.True(f.World.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(1, pursuer.Position.Floor);
        Assert.True(SpecialDungeonRules.Boundary(f.Complex).Contains(pursuer.Position));
        Assert.Equal(pursuerId, f.Special.Pursuer); Assert.Equal(originalDeadline, f.Special.ExpiresAt);
        Assert.True(TacticalReferences.RetainsActor(f.World.State, pursuerId));
        RoundTrip(f.World, loaded =>
        {
            Assert.Equal(pursuerId, loaded.State.NativeCampaign!.Complexes![0].Special!.Pursuer);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(f.World.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(loaded.State));
        });
    }

    [Fact]
    public void PressureUsesReachableAuthoredAnchorsAndEveryMembersVisionInsteadOfTheViewedCamera()
    {
        using var f = new Visit(SpecialDungeonAftermath.PartialCollapse);
        var floor = f.Complex.Run.Floors[0];
        foreach (var door in f.World.State.Tactics!.Doors.Nodes.Where(d => f.Complex.Contains(d.Position)))
            f.World.Chunks.Edit(door.Position, Material.Door);
        f.World.Reveal();
        string before = JsonSerializer.Serialize(f.World.State);
        var candidates = f.World.DungeonCombat.SpecialSpawnCandidates(f.Complex, floor, 8);
        Assert.NotEmpty(candidates); Assert.All(candidates, p =>
        {
            Assert.True(floor.Contains(p)); Assert.True(f.World.CanEnter(p));
            Assert.True(p.Distance(f.Hero.Position) >= 8); Assert.DoesNotContain(p, f.World.VisionFrom(f.Hero.Position));
        });
        f.World.Visible.Clear();
        Assert.Equal(candidates, f.World.DungeonCombat.SpecialSpawnCandidates(f.Complex, floor, 8));
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
        foreach (var (dx, dy) in WorldSession.Directions)
        {
            var next = f.Hero.Position.Offset(dx, dy);
            if (f.World.Chunks.At(next) == Material.Floor) f.World.Chunks.Edit(next, Material.Wall);
        }
        before = JsonSerializer.Serialize(f.World.State);
        Assert.Empty(f.World.DungeonCombat.SpecialSpawnCandidates(f.Complex, floor, 8, pursuerFallback: true));
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
    }

    [Fact]
    public void LethalExpiryKillsOnlyInsideTheComplexAndKeepsOutsideCompanionsAndCorpseGear()
    {
        using var f = new Visit(SpecialDungeonAftermath.LethalCollapse);
        var recruit = f.World.ActorById(f.World.NativeAdventurers.First(r => f.World.ActorById(r.Actor).Position.Floor == 0).Actor);
        var hero = f.Hero;
        hero.Position = recruit.Position.Offset(0, -1); f.World.Chunks.Edit(hero.Position, Material.Floor);
        Assert.True(f.World.SelectDungeonMember(hero.Id).Success);
        Assert.True(f.World.SubmitDungeon(new(TacticalAction.Recruit, Target: recruit.Id)).Success);
        Assert.True(f.World.SplitDungeonMember(recruit.Id).Success);
        hero.Position = f.Entry; recruit.Position = f.Entry.Offset(4, 0) with { Floor = 0 };
        f.World.State.Position = hero.Position;
        while (f.World.State.Tactics!.ActiveActor != hero.Id)
        {
            Assert.True(f.World.SelectDungeonMember(f.World.State.Tactics.ActiveActor).Success);
            Assert.True(f.World.SubmitDungeon(new(TacticalAction.Wait)).Success);
        }
        Assert.True(f.World.SelectDungeonMember(hero.Id).Success);
        string equipment = JsonSerializer.Serialize(hero.Equipment); string outsideEquipment = JsonSerializer.Serialize(recruit.Equipment);
        f.Activate(); long deadline = f.Special.ExpiresAt;
        while (f.World.DungeonCombat.Clock < deadline)
        {
            Assert.True(f.World.SelectDungeonMember(f.World.State.Tactics.ActiveActor).Success);
            Assert.True(f.World.SubmitDungeon(new(TacticalAction.Wait)).Success);
        }
        Assert.Equal(LifeState.Dead, hero.Life); Assert.Equal(0, hero.Health);
        Assert.Equal(LifeState.Alive, recruit.Life); Assert.False(f.World.State.AwaitingContinuation);
        Assert.Equal(equipment, JsonSerializer.Serialize(hero.Equipment)); Assert.Equal(outsideEquipment, JsonSerializer.Serialize(recruit.Equipment));
        Assert.Equal(f.Entry, hero.Position); Assert.Contains(hero.Id, f.Special.Casualties); Assert.DoesNotContain(recruit.Id, f.Special.Casualties);
        Assert.True(TacticalReferences.RetainsActor(f.World.State, hero.Id));
        TacticalValidation.Validate(f.World.Plan, f.World.State);
        RoundTrip(f.World, loaded => Assert.Equal(LifeState.Dead, loaded.ActorById(hero.Id).Life));
    }

    [Theory]
    [InlineData("deadline")]
    [InlineData("schedule")]
    [InlineData("pursuer")]
    public void MalformedDeadlineOrOwningReferencesRejectBeforeLoading(string fault)
    {
        using var f = new Visit(SpecialDungeonAftermath.PartialCollapse);
        f.Activate();
        if (fault == "deadline") f.Special.ExpiresAt++;
        if (fault == "schedule") f.Special.DeadlineSchedule++;
        if (fault == "pursuer") f.Special.Pursuer = f.Hero.Id;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(f.World.Plan, f.World.State));
    }

    private static void RoundTrip(WorldSession world, Action<WorldSession> inspect)
    {
        string directory = Path.Combine(Path.GetTempPath(), "roag-special-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(directory, "world.json"); world.Save(path);
            using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State)); inspect(loaded);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
