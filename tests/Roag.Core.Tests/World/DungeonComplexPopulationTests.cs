using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class DungeonComplexPopulationTests
{
    [Fact]
    public void AllFixedBranchesReceiveOriginalContentsWithoutSpendingTimeOrRevealingRooms()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var blueprint = Blueprint(world, 1);
        long clock = world.DungeonCombat.Clock; var position = world.DungeonMember.Position;
        int seen = world.State.Chunks.Sum(c => c.Discovered.Count), recruits = world.NativeAdventurers.Count;
        var complex = world.InstallDungeonComplex(blueprint); var state = world.State.Tactics!;
        Assert.True(complex.Populated); Assert.Equal(clock, state.Queue.Clock); Assert.Equal(1, state.NextAction);
        Assert.Equal(position, world.DungeonMember.Position); Assert.Equal(seen, world.State.Chunks.Sum(c => c.Discovered.Count));
        Assert.Equal(recruits + 20, world.NativeAdventurers.Count);
        Assert.Equal(45, state.Chests.Chests.Count);
        Assert.All(state.Chests.Chests, c => Assert.StartsWith("complex:1/branch:", c.Marker));
        Assert.Equal(9, complex.Run.Floors.Count(f => f.Boss > 0 && f.Stage < 4));
        var regent = world.ActorById(complex.Run.Floors.Single(f => f.Stage == 4).Boss);
        Assert.Equal("core:enemy/hollow_regent", regent.Tactics!.DefinitionId);
        foreach (var floor in complex.Run.Floors.Where(f => f.Stage < 4))
        {
            Assert.Equal(floor.Origin.Floor % 2 == 1 ? 2 : 3, state.Chests.Chests.Count(c => floor.Contains(c.Position)));
            Assert.Single(state.Secrets.Nodes, s => floor.Contains(s.Position));
            Assert.Contains(state.Services.Nodes, s => floor.Contains(s.Position));
            foreach (var kind in new[] { DungeonRoomFeatureKind.Ore, DungeonRoomFeatureKind.Brazier, DungeonRoomFeatureKind.Ward, DungeonRoomFeatureKind.Pillar })
                Assert.Contains(state.RoomFeatures.Nodes, n => floor.Contains(n.Position) && n.Kind == kind);
            Assert.InRange(state.Vaults.Nodes.Count(v => floor.Contains(v.Position)), 0, 1);
            Assert.All(world.State.Actors.Where(a => floor.Contains(a.Position) && a.Tactics?.Enemy != null),
                a => Assert.Equal(NativeAppearance.ForEnemy(a.Tactics!.DefinitionId), a.SpriteId));
            if (floor.Boss > 0)
            {
                var boss = world.ActorById(floor.Boss);
                Assert.True(boss.Tactics!.Enemy!.Dormant);
                Assert.DoesNotContain(state.Queue.Snapshot().Pending, w => w.ActorId == boss.Id);
            }
        }
        Assert.True(world.ChooseDungeonClass(ClassKind.Ranger).Success);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void IndependentSameDepthComplexContentsAndVaultsSaveAndContinueWithoutRepopulation()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var first = world.InstallDungeonComplex(Blueprint(world, 1));
        var second = world.InstallDungeonComplex(Blueprint(world, 2));
        var state = world.State.Tactics!;
        Assert.Equal(90, state.Chests.Chests.Count);
        Assert.Equal(90, state.Chests.Chests.Select(c => c.Marker).Distinct().Count());
        Assert.Contains(state.Services.Nodes, n => n.Marker.StartsWith("complex:1/", StringComparison.Ordinal));
        Assert.Contains(state.Services.Nodes, n => n.Marker.StartsWith("complex:2/", StringComparison.Ordinal));
        Assert.Contains(state.Vaults.Nodes.GroupBy(v => v.Position.Floor), g => g.Count() > 1);
        foreach (var sign in state.Services.Nodes.Where(n => n.Sign != null))
            Assert.Equal(WorldSession.SnapshotComplexAt(world.State, sign.Position)!.Parcel.Id, sign.Sign!.Target.Place);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Throws<InvalidOperationException>(() => world.PopulateDungeonComplex(Blueprint(world, 1), first));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        TacticalValidation.Validate(world.Plan, world.State);
        string directory = Path.Combine(Path.GetTempPath(), "roag-populated-complex-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(directory, "campaign.json"); world.Save(path);
            using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.All(first.Run.Floors.Concat(second.Run.Floors).Where(f => f.Boss > 0), f => Assert.True(TacticalReferences.RetainsActor(world.State, f.Boss)));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void EliteActivationAndDeathKeepTheirActualComplexOwnerAndUniqueRewardReceipt()
    {
        using var world = WorldSession.CreateNativeCampaign(42); var complex = world.InstallDungeonComplex(Blueprint(world, 1));
        var floor = complex.Run.Floors.First(f => f.Boss > 0 && f.Stage == 2); var boss = world.ActorById(floor.Boss);
        var hero = world.Party.Single();
        // Explicit placement isolates proximity wake/death/receipt integration;
        // it is not evidence of a natural expedition to this branch.
        hero.Position = world.State.Position = new[] { boss.Position.Offset(0, -1), boss.Position.Offset(1, 0),
            boss.Position.Offset(0, 1), boss.Position.Offset(-1, 0) }.First(p => world.CanEnter(p, hero.Id));
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        var combat = world.DungeonCombat;
        Assert.Same(hero, combat.AdvanceToDecision());
        Assert.False(boss.Tactics!.Enemy!.Dormant);
        combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [boss.Position], "test:owned-elite",
            AttackDelivery.Area, DamageElement.Physical, 100000, combat.Clock));
        Assert.Equal(LifeState.Dead, boss.Life); Assert.Contains(boss.Id, complex.Run.BossExitReceipts);
        string marker = $"elite:complex:1/branch:{floor.Branch}";
        Assert.Single(combat.State.Chests.Chests, c => c.Marker == marker);
        Assert.Empty(complex.Run.PendingEliteRewards);
        combat.ResolveAttack(new(combat.State.NextAction++, hero.Id, hero.Position, [boss.Position], "test:owned-elite-again",
            AttackDelivery.Area, DamageElement.Physical, 100000, combat.Clock));
        Assert.Single(combat.State.Chests.Chests, c => c.Marker == marker);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData("flag")] [InlineData("boss")]
    public void CompletedPopulationCannotLoseItsFlagOrAnOwnedBossInASavedWorld(string invalid)
    {
        using var world = WorldSession.CreateNativeCampaign(42); var complex = world.InstallDungeonComplex(Blueprint(world, 1));
        if (invalid == "flag") complex.Populated = false;
        else
        {
            int index = complex.Run.Floors.FindIndex(f => f.Boss > 0);
            complex.Run.Floors[index] = complex.Run.Floors[index] with { Boss = 0 };
        }
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
    }

    private static DungeonComplexBlueprint Blueprint(WorldSession world, int id)
        => DungeonComplexPlanner.Generate(world.Plan, DungeonParcelPlanner.Reserve(world.Plan)[id - 1]);
}
