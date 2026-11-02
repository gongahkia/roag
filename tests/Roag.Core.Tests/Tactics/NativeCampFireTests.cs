using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeCampFireTests
{
    [Fact]
    public void FireConsumesOneWoodAndPaidRecoveryThenExpiresWithoutReplacingTerrain()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        int wood = world.Count("wood"); var before = world.Chunks.At(hero.Position);
        Assert.True(world.SubmitDungeon(new(TacticalAction.LightCampFire)).Success);
        Assert.Equal(wood - 1, world.Count("wood")); Assert.Equal(100, world.DungeonCombat.Clock);
        Assert.Equal(before, world.Chunks.At(hero.Position)); Assert.Equal("campfire", world.ObjectAt(hero.Position)!.Kind);
        Assert.True(world.WarmedByCampFire(hero.Position));
        Assert.False(world.WarmedByCampFire(hero.Position with { Floor = 2 }));
        string snapshot = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.LightCampFire)).Success);
        Assert.Equal(snapshot, JsonSerializer.Serialize(world.State));
        Assert.True(world.SubmitDungeon(new(TacticalAction.Watch, Choice: 10)).Success);
        for (int i = 0; i < 50 && hero.Tactics!.Activity != null; i++) world.ContinueDungeonActivities(32);
        Assert.Empty(world.State.Tactics!.CampFires); Assert.False(world.WarmedByCampFire(hero.Position));
        Assert.Equal(before, world.Chunks.At(hero.Position)); Assert.Equal(wood - 1, world.Count("wood"));
    }

    [Fact]
    public void ExtinguishingIsPaidDoesNotRefundFuelAndExactBurningSavesContinue()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        Assert.True(world.SubmitDungeon(new(TacticalAction.LightCampFire)).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-fire-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            int wood = world.Count("wood"); long before = world.DungeonCombat.Clock;
            Assert.True(world.SubmitDungeon(new(TacticalAction.ExtinguishCampFire)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.ExtinguishCampFire)).Success);
            Assert.Equal(before + 100, world.DungeonCombat.Clock); Assert.Empty(world.State.Tactics!.CampFires);
            Assert.Equal(wood, world.Count("wood")); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void WarmthRequiresAnActualClearNearbySameFloorFire()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        Assert.True(world.SubmitDungeon(new(TacticalAction.LightCampFire)).Success);
        var close = hero.Position.Offset(2, 0); world.Chunks.Edit(close, Material.Floor); world.Chunks.Edit(hero.Position.Offset(1, 0), Material.Floor);
        Assert.True(world.WarmedByCampFire(close));
        world.Chunks.Edit(hero.Position.Offset(1, 0), Material.Wall);
        Assert.False(world.WarmedByCampFire(close)); Assert.False(world.WarmedByCampFire(hero.Position.Offset(4, 0)));
    }

    [Theory]
    [InlineData("core:enemy/ruin_skitter", true)]
    [InlineData("core:enemy/ember_moth", false)]
    public void SpeciesFireDecisionsArePhysicalPaidAndVisibleInInspection(string definition, bool avoids)
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        Assert.True(world.SubmitDungeon(new(TacticalAction.LightCampFire)).Success);
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy is { Dormant: false } && a.Position.Floor == 1);
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 6; x++) world.Chunks.Edit(hero.Position.Offset(x, y), Material.Floor);
        enemy.Position = hero.Position.Offset(avoids ? 2 : 5, 0);
        var combat = world.DungeonCombat; enemy.Tactics = EnemyCatalog.Default.Get(definition).CreateTactics(); enemy.Health = enemy.MaxHealth;
        var inspection = combat.InspectEnemy(hero.Id, enemy.Id)!;
        Assert.Contains(avoids ? "avoids" : "Approaches", inspection.Tendency);
        // The moth sees the fire after the party has physically left its floor.
        if (!avoids)
        {
            hero.Position = world.State.Dungeon!.Floors.Single(f => f.Origin.Floor == 2).Entry;
        }
        combat.State.Queue.CancelActor(enemy.Id, ScheduledKind.ActorReady);
        combat.State.Queue.Schedule(ScheduledKind.ActorReady, combat.Clock + 100, hero.Id, true);
        combat.State.ActiveActor = enemy.Id;
        string before = JsonSerializer.Serialize(world.State); var plan = combat.PreviewWorldEnemyTurn();
        Assert.Equal(before, JsonSerializer.Serialize(world.State)); Assert.Equal(EnemyIntentKind.Move, plan.Kind);
        int prior = enemy.Position.Distance(combat.State.CampFires.Single().Position);
        combat.CommitWorldEnemyTurn();
        Assert.Contains(combat.State.Timeline.Pending, w => w.ActorId == enemy.Id && w.Kind == ScheduledKind.ActorReady && w.Due > combat.Clock);
        int after = enemy.Position.Distance(combat.State.CampFires.Single().Position);
        if (!avoids)
            for (int i = 0; i < 8 && after >= prior; i++)
            {
                // A real occupant can require an initial sideways step. Continue
                // the moth's paid decisions rather than assuming a straight line.
                combat.AdvanceToPartyDecision();
                Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
                combat.AdvanceToPartyDecision();
                after = enemy.Position.Distance(combat.State.CampFires.Single().Position);
            }
        Assert.True(avoids ? after > prior : after < prior, $"Fire response {definition}: {prior} -> {after}");
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("deadline")]
    [InlineData("position")]
    [InlineData("duplicate")]
    public void InvalidSavedFireIdentityLifetimeAndFootprintAreRejected(string invalid)
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        Assert.True(world.SubmitDungeon(new(TacticalAction.LightCampFire)).Success);
        var fires = world.State.Tactics!.CampFires; var fire = Assert.Single(fires);
        fires[0] = invalid switch
        {
            "owner" => fire with { Owner = 0 },
            "deadline" => fire with { Until = long.MaxValue },
            "position" => fire with { Position = new(0, 0, 0) },
            _ => fire
        };
        if (invalid == "duplicate") fires.Add(fire);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
    }

    [Fact]
    public void NoFuelRejectsFreelyAndEveryOriginalSpeciesHasAnInspectableFirePolicy()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); world.State.Inventory["wood"] = 0;
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.LightCampFire)).Success); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.All(EnemyCatalog.Default.Definitions, e => Assert.NotEmpty(CampFireResponses.Description(e.Id)));
    }
}
