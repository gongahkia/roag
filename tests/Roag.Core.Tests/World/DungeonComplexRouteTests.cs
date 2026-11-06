using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class DungeonComplexRouteTests
{
    [Fact]
    public void APhysicalBranchUsesItsOwnEliteAndOrderedOffersAndOpensOnlyExistingChosenSeals()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var blueprint = Blueprint(world); var complex = world.InstallDungeonComplex(blueprint); var hero = world.Party.Single();
        Assert.Empty(world.AvailableDungeonRoutes(hero.Id));
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.DungeonRoute, Choice: 1)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        // Explicit deep-branch positioning tests paid ownership, not natural travel.
        var source = complex.Run.Floors.First(f => f.Stage == 2 && f.Boss > 0);
        Place(world, hero, Around(world, source.Exit, hero.Id));
        Assert.Empty(world.AvailableDungeonRoutes(hero.Id));
        Defeat(world, source.Boss);
        var expected = DungeonRoutes.ForStage(2, [DungeonRoutes.RouteId(source.Biome)]);
        Assert.Equal(expected, world.AvailableDungeonRoutes(hero.Id));
        var passage = blueprint.Passages.Single(p => p.Branch == source.Branch);
        const int choice = 2; var selected = passage.Routes[choice - 1];
        var edits = Edits(world); var position = hero.Position; long clock = world.DungeonCombat.Clock;
        var layout = JsonSerializer.Serialize(complex.Run.Floors);
        var preview = world.DungeonCombat.Preview(new(TacticalAction.DungeonRoute, Choice: choice));
        Assert.True(preview.Valid);
        Assert.True(world.SubmitDungeon(new(TacticalAction.DungeonRoute, Choice: choice)).Success);
        Assert.Equal(clock + preview.Recovery, world.DungeonCombat.Clock); Assert.Equal(position, hero.Position);
        Assert.Equal(layout, JsonSerializer.Serialize(complex.Run.Floors));
        Assert.Equal(1, complex.Run.Stage); Assert.Empty(complex.Run.RouteHistory);
        Assert.Equal(expected[choice - 1].Id, complex.Routes!.Choices[source.Branch]);
        Assert.Empty(world.AvailableDungeonRoutes(hero.Id));
        Assert.Equal(Material.Floor, world.Chunks.At(passage.Barrier));
        Assert.Equal(Material.Floor, world.Chunks.At(selected.Barrier));
        foreach (var other in passage.Routes.Where(r => r != selected)) Assert.Equal(Material.Wall, world.Chunks.At(other.Barrier));
        var after = Edits(world);
        Assert.Equal(new[] { passage.Barrier, selected.Barrier }.OrderBy(DungeonRunState.CellKey),
            after.Where(p => !edits.TryGetValue(p.Key, out var value) || value != p.Value).Select(p => p.Key).OrderBy(DungeonRunState.CellKey));
        string paid = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.DungeonRoute, Choice: 1)).Success);
        Assert.Equal(paid, JsonSerializer.Serialize(world.State));
        Defeat(world, complex.Run.Floors.Single(f => f.Stage == 4).Boss);
        Assert.True(complex.Run.Victory); Assert.False(world.State.Endgame.Victory);
        Assert.Single(world.State.Tactics!.Events, e => e.Kind == "ComplexCleared");
        TacticalValidation.Validate(world.Plan, world.State);
        ExactContinuation(world);
    }

    [Fact]
    public void SplitMemberChoosesItsPhysicalRouteWithoutUsingTheSelectedOutdoorsMembersVisionOrRecovery()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var complex = world.InstallDungeonComplex(Blueprint(world));
        var founder = world.Party.Single(); var other = world.ActorById(world.NativeAdventurers[0].Actor);
        Place(world, founder, Around(world, other.Position, founder.Id));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Recruit, Target: other.Id)).Success);
        Assert.True(world.SplitDungeonMember(other.Id).Success);
        var source = complex.Run.Floors.First(f => f.Stage == 3 && f.Boss > 0);
        other.Position = Around(world, source.Exit, other.Id); Defeat(world, source.Boss);
        for (int i = 0; i < 200 && world.State.Tactics!.ActiveActor != other.Id; i++)
        {
            Assert.True(world.SelectDungeonMember(founder.Id).Success);
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        }
        Assert.Equal(other.Id, world.State.Tactics!.ActiveActor);
        Assert.True(world.SelectDungeonMember(founder.Id).Success);
        Assert.DoesNotContain(source.Exit, world.Visible);
        Assert.Single(world.AvailableDungeonRoutes(other.Id));
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.DungeonRoute, Choice: 1)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        var outside = founder.Position;
        var pending = world.State.Tactics.Queue.Snapshot().Pending.Single(e => e.ActorId == founder.Id && e.Kind == ScheduledKind.ActorReady);
        Assert.True(world.SelectDungeonMember(other.Id).Success);
        Assert.True(world.DungeonCombat.Commit(new(TacticalAction.DungeonRoute, Choice: 1)).Success);
        Assert.Equal(outside, founder.Position);
        Assert.Equal(pending, world.State.Tactics.Queue.Snapshot().Pending.Single(e => e.ActorId == founder.Id && e.Kind == ScheduledKind.ActorReady));
        Assert.Equal("core:route/gateway", complex.Routes!.Choices[source.Branch]);
        world.DungeonCombat.AdvanceToPartyDecision();
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void PaidExcavationAllowsAnUnchosenBranchAndLaterChoicePreservesARebuiltSourceWall()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var blueprint = Blueprint(world); var complex = world.InstallDungeonComplex(blueprint); var hero = world.Party.Single();
        var source = complex.Run.Floors.First(f => f.Stage == 1 && f.Boss > 0);
        var passage = blueprint.Passages.Single(p => p.Branch == source.Branch);
        Defeat(world, source.Boss);
        Place(world, hero, passage.Barrier.Offset(1, 0));
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Dig, passage.Barrier))).Success);
        FinishWork(world, hero);
        Assert.Equal(Material.Floor, world.Chunks.At(passage.Barrier));
        Assert.DoesNotContain(DungeonRunState.CellKey(passage.Barrier), complex.Routes!.UnbrokenSeals);
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Build, passage.Barrier, Material: Material.Wall))).Success);
        FinishWork(world, hero);
        Assert.Equal(Material.Wall, world.Chunks.At(passage.Barrier));
        Assert.Contains(world.State.Structures, s => s.Position == passage.Barrier);
        Place(world, hero, source.Exit);
        Assert.True(world.SubmitDungeon(new(TacticalAction.DungeonRoute, Choice: 1)).Success);
        Assert.Equal(Material.Wall, world.Chunks.At(passage.Barrier));
        Assert.Contains(world.State.Structures, s => s.Position == passage.Barrier);
        var unchosen = passage.Routes[1];
        Place(world, hero, unchosen.Barrier.Offset(-1, 0));
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Dig, unchosen.Barrier))).Success);
        FinishWork(world, hero);
        Assert.Equal(Material.Floor, world.Chunks.At(unchosen.Barrier));
        Assert.DoesNotContain(DungeonRunState.CellKey(unchosen.Barrier), complex.Routes.UnbrokenSeals);
        Assert.Equal(passage.Routes[0].Route, complex.Routes.Choices[source.Branch]);
        Assert.Contains(DungeonRunState.CellKey(passage.Routes[2].Barrier), complex.Routes.UnbrokenSeals);
        TacticalValidation.Validate(world.Plan, world.State);
        ExactContinuation(world);
    }

    [Theory]
    [InlineData("foreign-seal")] [InlineData("changed-seal")] [InlineData("choice-without-elite")]
    public void PhysicalRouteSavesRejectInventedOrUnretiredSealReceipts(string kind)
    {
        using var world = WorldSession.CreateNativeCampaign(42); var blueprint = Blueprint(world);
        var complex = world.InstallDungeonComplexTerrain(blueprint); var passage = blueprint.Passages[0];
        switch (kind)
        {
            case "foreign-seal": complex.Routes!.UnbrokenSeals.Add(DungeonRunState.CellKey(world.DungeonMember.Position)); break;
            case "changed-seal": world.Chunks.Edit(passage.Barrier, Material.Floor); break;
            case "choice-without-elite": complex.Routes!.Choices[passage.Branch] = passage.Routes[0].Route; break;
        }
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
    }

    [Fact]
    public void OlderPhysicalTerrainWithoutRouteMetadataRemainsUnchangedOnOrdinaryLoad()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var complex = world.InstallDungeonComplexTerrain(Blueprint(world));
        complex.Routes = null;
        Assert.DoesNotContain("\"Routes\"", JsonSerializer.Serialize(complex));
        ExactContinuation(world);
        Assert.Null(complex.Routes);
    }

    private static DungeonComplexBlueprint Blueprint(WorldSession world)
        => DungeonComplexPlanner.Generate(world.Plan, DungeonParcelPlanner.Reserve(world.Plan)[0]);
    private static void Place(WorldSession world, Actor hero, WorldPosition position)
    {
        Assert.True(world.CanEnter(position, hero.Id)); hero.Position = world.State.Position = position;
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
    }
    private static WorldPosition Around(WorldSession world, WorldPosition center, long hero)
        => new[] { center.Offset(0, -1), center.Offset(1, 0), center.Offset(0, 1), center.Offset(-1, 0) }
            .First(p => world.CanEnter(p, hero));
    private static void Defeat(WorldSession world, long boss)
    {
        var combat = world.DungeonCombat; var victim = world.ActorById(boss);
        combat.ResolveAttack(new(combat.State.NextAction++, world.DungeonMember.Id, world.DungeonMember.Position, [victim.Position],
            "test:physical-route-elite", AttackDelivery.Area, DamageElement.Physical, 100000, combat.Clock, IgnoresTerrain: true));
        Assert.Equal(LifeState.Dead, victim.Life);
    }
    private static Dictionary<WorldPosition, Material> Edits(WorldSession world)
        => world.State.Chunks.SelectMany(c => c.Edits.Select(e => new KeyValuePair<WorldPosition, Material>(
            new(c.Key.X * 64 + e.Key % 64, c.Key.Y * 64 + e.Key / 64, c.Key.Floor), e.Value))).ToDictionary();
    private static void FinishWork(WorldSession world, Actor hero)
    {
        for (int i = 0; i < 100 && hero.Tactics!.Activity != null; i++) world.ContinueDungeonActivities(32);
        Assert.Null(hero.Tactics!.Activity); Assert.Empty(world.State.Work); Assert.Equal(LifeState.Alive, hero.Life);
    }
    private static void ExactContinuation(WorldSession world)
    {
        string root = Path.Combine(Path.GetTempPath(), "roag-physical-route-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
