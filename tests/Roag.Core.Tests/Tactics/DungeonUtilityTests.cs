using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void UtilityAndSupplyPublicCommandsMatchThePinnedOriginal()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-utilities.json")));
        int commands = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
            var input = sample.GetProperty("input"); var origin = hero.Position;
            if (input.TryGetProperty("utility", out var kind)) hero.Tactics!.Supplies.Utility = new(kind.GetString() switch
            { "escape_scroll" => UtilityKind.EscapeScroll, _ => UtilityKind.ReturnToken });
            var supplies = hero.Tactics!.Supplies;
            supplies.FocusActions = input.TryGetProperty("focus", out var focus) ? focus.GetInt32() : 0;
            supplies.WardActions = input.TryGetProperty("ward", out var ward) ? ward.GetInt32() : 0;
            if (input.TryGetProperty("tiles", out var tiles)) foreach (var tile in tiles.EnumerateArray())
            {
                var p = origin.Offset(tile[0].GetInt32(), tile[1].GetInt32()); char glyph = tile[2].GetString()![0];
                if (glyph == '#') world.Chunks.Edit(p, Material.Wall); else Overlay(world, p, glyph);
            }
            world.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                var action = command[0].GetString() switch { "use_utility" => TacticalAction.UseUtility,
                    "move" => TacticalAction.Move, "basic_attack" => TacticalAction.BasicAttack, _ => TacticalAction.Wait };
                var intent = new TacticalIntent(action, Facing.East, Choice: command.GetArrayLength() == 4 ? command[3].GetInt32() : 0);
                var expected = sample.GetProperty("expected")[index++]; long first = combat.State.NextEvent;
                var preview = combat.Preview(intent);
                Assert.Equal(expected.GetProperty("valid").GetBoolean(), preview.Valid);
                Assert.Equal(expected.GetProperty("recovery").GetInt64(), preview.Valid ? preview.Recovery : 0);
                Assert.Equal(preview.Valid, world.SubmitDungeon(intent).Success);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock);
                Assert.Equal(expected.GetProperty("hp").GetInt32(), hero.Health);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), expected.GetProperty("y").GetInt32()), hero.Position);
                Assert.Equal(expected.GetProperty("focus").GetInt32(), supplies.FocusActions);
                Assert.Equal(expected.GetProperty("ward").GetInt32(), supplies.WardActions);
                Assert.Equal(expected.GetProperty("utility").GetBoolean(), supplies.Utility != null);
                Assert.Equal(expected.GetProperty("planted").GetBoolean(), supplies.Utility?.Marker != null);
                foreach (var (key, eventKind) in new[] { ("entered", "TileEntered"), ("moved", "Moved"), ("spent", "SupplyFocusSpent") })
                    Assert.Equal(expected.GetProperty(key).GetInt32(), combat.State.Events.Count(e => e.Sequence >= first && e.Kind == eventKind));
                TacticalValidation.Validate(world.Plan, world.State); commands++;
            }
        }
        Assert.Equal(15, commands);
    }

    [Fact]
    public void EscapeScrollRejectsFreelyAndPaysDashTerrainCostsWithoutUsingClassDashResources()
    {
        using var world = Floor(ClassKind.Stormcaller); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        hero.Tactics!.Supplies.Utility = new(UtilityKind.EscapeScroll); hero.Tactics.Resources.StormCharge = 2;
        var origin = hero.Position; var scroll = new TacticalIntent(TacticalAction.UseUtility, Facing.East, Choice: 3);
        world.Chunks.Edit(origin.Offset(2, 0), Material.Wall);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(combat.Commit(scroll).Success); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        world.Chunks.Edit(origin.Offset(2, 0), Material.Floor);
        Overlay(world, origin.Offset(1, 0), 'g'); Overlay(world, origin.Offset(2, 0), 'g');
        var preview = combat.Preview(scroll);
        Assert.True(preview.Valid, preview.Reason);
        Assert.Equal(ActionTiming.Recovery(140, hero.Speed), preview.Recovery);
        Assert.True(combat.Commit(scroll).Success);
        Assert.Equal(origin.Offset(3, 0), hero.Position); Assert.Null(hero.Tactics.Supplies.Utility);
        Assert.Equal(2, hero.Tactics.Resources.StormCharge); Assert.False(hero.Tactics.Resources.StormWard);
        var moved = Assert.Single(combat.State.Events, e => e.Kind == "Moved");
        Assert.Equal("core:ability/dash", moved.Ability);
        Assert.Equal(3, combat.State.Events.Count(e => e.Kind == "TileEntered" && e.Source == hero.Id));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ReturnTokenPlantsAndReturnsForTwoPaidActionsWithoutTouchingTheInterveningBramble()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var room = world.RoomAt(hero.Position); Assert.NotNull(room);
        var start = hero.Position;
        var path = new[] { Facing.East, Facing.South, Facing.West, Facing.North }.Select(f =>
        {
            var (dx, dy) = AttackGeometry.Offset(f); return (Facing: f, Middle: start.Offset(dx, dy), End: start.Offset(2 * dx, 2 * dy));
        }).First(p => world.RoomAt(p.Middle) == room && world.RoomAt(p.End) == room && world.CanEnter(p.End));
        hero.Tactics!.Supplies.Utility = new(UtilityKind.ReturnToken);
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseUtility)).Success);
        Assert.Equal(100, combat.Clock); Assert.Equal(start, hero.Tactics.Supplies.Utility!.Marker!.Position);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, path.Facing)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, path.Facing)).Success);
        Overlay(world, path.Middle, 'b'); int hp = hero.Health;
        long first = combat.State.NextEvent;
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseUtility)).Success);
        Assert.Equal(400, combat.Clock); Assert.Equal(start, hero.Position); Assert.Equal(hp, hero.Health);
        Assert.Null(hero.Tactics.Supplies.Utility);
        Assert.Single(combat.State.Events.Where(e => e.Sequence >= first), e => e.Kind == "TileEntered");
        Assert.DoesNotContain(combat.State.Events.Where(e => e.Sequence >= first), e => e.Kind == "Displaced");
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void PlantedTokenSurvivesSaveAndExpiresOnAnActualRoomBoundaryEntry()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        hero.Tactics!.Supplies.Utility = new(UtilityKind.ReturnToken);
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseUtility)).Success);
        var marker = hero.Tactics.Supplies.Utility!.Marker!;
        string root = Path.Combine(Path.GetTempPath(), "roag-utility-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot);
            using var loaded = WorldSession.Load(slot);
            Assert.Equal(marker, loaded.Party.Single().Tactics!.Supplies.Utility!.Marker);
            var site = world.State.Dungeon!.Floors.Single(f => f.Origin.Floor == marker.Position.Floor)
                .Rooms.Single(r => r.Index + 1 == marker.Room.Room);
            var outside = site.Origin.Offset(site.Width, site.Height / 2);
            Assert.NotEqual(marker.Room, world.RoomAt(outside));
            // Test an actual forced-entry event rather than changing only the selected view.
            var enemy = ReplaceRegular(world, "core:enemy/heavy_rammer");
            hero.Position = world.State.Position = outside.Offset(-1, 0);
            world.Chunks.Edit(hero.Position, Material.Floor); world.Chunks.Edit(outside, Material.Floor);
            combat.ResolveAttack(new(combat.State.NextAction++, enemy.Id, enemy.Position, [hero.Position], "test:token-expiry",
                AttackDelivery.Area, DamageElement.Physical, 0, combat.Clock, Shove: new(hero.Position, Facing.East)));
            Assert.Equal(outside, hero.Position); Assert.Null(hero.Tactics.Supplies.Utility);
            Assert.Single(combat.State.Events, e => e.Kind == "ReturnTokenExpired");
            TacticalValidation.Validate(world.Plan, world.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void FocusAddsTwoToOneWholeImmediateAttackAndCountsOnlyTheOwnersPaidActions()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var enemy = ReplaceRegular(world, "core:enemy/mossbound_guard"); enemy.Position = hero.Position.Offset(1, 0);
        enemy.Tactics!.InnateStats = enemy.Tactics.InnateStats! with { Defense = 0 };
        hero.Tactics!.Supplies.FocusActions = 3;
        int hp = enemy.Health; var attack = new TacticalIntent(TacticalAction.BasicAttack, Facing.East);
        var preview = combat.Preview(attack); Assert.Equal(hero.Power + 2, preview.Damage);
        Assert.Equal(3, hero.Tactics.Supplies.FocusActions);
        Assert.True(combat.Commit(attack).Success);
        Assert.Equal(hp - hero.Power - 2, enemy.Health); Assert.Equal(0, hero.Tactics.Supplies.FocusActions);
        Assert.Single(combat.State.Events, e => e.Kind == "SupplyFocusSpent");
        combat.AdvanceToPartyDecision(); hero.Tactics.Supplies.FocusActions = 3;
        for (int i = 2; i >= 0; i--)
        {
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(i, hero.Tactics.Supplies.FocusActions);
        }
        Assert.Equal(hero.Power, combat.Preview(attack).Damage);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void SupplyWardIgnoresZeroDamageAbsorbsOneWholeHitAndExpiresAfterThreePaidActions()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        hero.Tactics!.Supplies.WardActions = 3;
        var hit = new LockedAttack(combat.State.NextAction++, 0, hero.Position, [hero.Position], "test:ward-hazard",
            AttackDelivery.Area, DamageElement.Fire, 0, combat.Clock, DamageCause.GroundHazard);
        combat.ResolveAttack(hit); Assert.Equal(3, hero.Tactics.Supplies.WardActions);
        combat.ResolveAttack(hit with { Damage = 7 }); Assert.Equal(hero.MaxHealth, hero.Health);
        Assert.Equal(0, hero.Tactics.Supplies.WardActions);
        combat.ResolveAttack(hit with { Damage = 1 }); Assert.Equal(hero.MaxHealth - 1, hero.Health);
        hero.Tactics.Supplies.WardActions = 3;
        for (int i = 2; i >= 0; i--)
        {
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(i, hero.Tactics.Supplies.WardActions);
        }
        combat.ResolveAttack(hit with { Damage = 1 }); Assert.Equal(hero.MaxHealth - 2, hero.Health);
        Assert.Single(combat.State.Events, e => e.Kind == "SupplyWardAbsorbed");
        TacticalValidation.Validate(world.Plan, world.State);
    }
}
