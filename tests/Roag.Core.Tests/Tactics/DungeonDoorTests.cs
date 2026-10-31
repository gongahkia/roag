using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void DoorPublicCommandsMatchOriginalPaidBumpsMovementAndWardDispatch()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-doors.json")));
        int commands = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            using var world = Floor(ClassCatalog.Get("core:actor/" + input.GetProperty("class").GetString()).Kind);
            var hero = world.Party.Single(); var combat = world.DungeonCombat; var origin = hero.Position;
            var door = combat.SpawnDoor("test:reference-door", origin.Offset(1, 0));
            if (input.TryGetProperty("ward", out _)) hero.Tactics!.Boons.Inventory = hero.Tactics.Boons.Inventory.Grant("core:boon/doorward_lamp", BoonRarity.Common, hero.Tactics.Build!.Class);
            combat.State.RandomState = 37; world.Reveal(); int index = 0;
            foreach (var command in input.GetProperty("commands").EnumerateArray())
            {
                var action = command.GetString() switch { "move" => TacticalAction.Move, "dash" => TacticalAction.Dash, _ => TacticalAction.BasicAttack };
                var intent = new TacticalIntent(action, Facing.East); var expected = sample.GetProperty("expected")[index++]; long first = combat.State.NextEvent;
                string before = JsonSerializer.Serialize(world.State); var preview = combat.Preview(intent);
                Assert.Equal(before, JsonSerializer.Serialize(world.State));
                Assert.Equal(expected.GetProperty("valid").GetBoolean(), preview.Valid);
                Assert.Equal(expected.GetProperty("recovery").GetInt64(), preview.Valid ? preview.Recovery : 0);
                Assert.Equal(preview.Valid, world.SubmitDungeon(intent).Success);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock); Assert.Equal(expected.GetProperty("hp").GetInt32(), hero.Health);
                Assert.Equal(origin.Offset(expected.GetProperty("x").GetInt32(), 0), hero.Position);
                Assert.Equal(expected.GetProperty("open").GetBoolean(), world.Chunks.At(door.Position) == Material.Door);
                Assert.Equal(expected.GetProperty("ward").GetInt32(), hero.Tactics!.Boons.WardTotal(combat.Clock));
                foreach (var (key, kind) in new[] { ("opened", "DoorOpened"), ("moved", "Moved"), ("entered", "TileEntered") })
                    Assert.Equal(expected.GetProperty(key).GetInt32(), combat.State.Events.Count(e => e.Sequence >= first && e.Kind == kind));
                TacticalValidation.Validate(world.Plan, world.State); commands++;
            }
        }
        Assert.Equal(10, commands);
    }

    [Fact]
    public void ClosedDoorsBlockPrivateSightAndProjectilesUntilOnePaidOpeningWithoutMovement()
    {
        using var world = Floor(ClassKind.Ranger); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var origin = hero.Position; var door = combat.SpawnDoor("test:sight-door", origin.Offset(1, 0));
        var target = ReplaceRegular(world, "core:enemy/mossbound_guard"); target.Position = origin.Offset(2, 0);
        target.Tactics!.InnateStats = target.Tactics.InnateStats! with { Defense = 0 };
        combat.State.Queue.CancelActor(target.Id, ScheduledKind.ActorReady); combat.State.Queue.Schedule(ScheduledKind.ActorReady, 10000, target.Id);
        world.Reveal(); Assert.DoesNotContain(target.Position, world.Visible); Assert.False(world.CanEnter(door.Position, hero.Id));
        int hp = target.Health;
        Assert.True(world.SubmitDungeon(new(TacticalAction.BasicAttack, Facing.East)).Success); Assert.Equal(hp, target.Health);
        long first = combat.State.NextEvent; long clock = combat.Clock;
        var preview = combat.Preview(new(TacticalAction.Move, Facing.East)); Assert.Equal(door.Id, preview.Door);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal(clock + preview.Recovery, combat.Clock); Assert.Equal(origin, hero.Position);
        Assert.Contains(target.Position, world.Visible); Assert.True(world.CanEnter(door.Position, hero.Id));
        Assert.Single(combat.State.Events.Where(e => e.Sequence >= first), e => e.Kind == "DoorOpened");
        Assert.DoesNotContain(combat.State.Events.Where(e => e.Sequence >= first), e => e.Kind is "Moved" or "TileEntered" or "AttackPerformed");
        Assert.True(world.SubmitDungeon(new(TacticalAction.BasicAttack, Facing.East)).Success); Assert.Equal(hp - hero.Power, target.Health);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosedAndOpenedDoorTilesContinueThroughActualWorldSaves(bool open)
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var door = combat.SpawnDoor("test:saved-door", hero.Position.Offset(1, 0)); world.Reveal();
        if (open) Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        string root = Path.Combine(Path.GetTempPath(), "roag-door-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.Equal(open ? Material.Door : Material.ClosedDoor, loaded.Chunks.At(door.Position));
            Assert.Equal(JsonSerializer.Serialize(combat.State.Doors), JsonSerializer.Serialize(loaded.DungeonCombat.State.Doors));
            for (int i = 0; i < 2; i++)
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
                Assert.True(loaded.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
                Assert.Equal(hero.Position, loaded.Party.Single().Position);
                Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            }
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void OpenDoorGroundStillSupportsAnEnemyMothsFiniteLandingAndEmber()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var door = combat.SpawnDoor("test:plain-door", hero.Position.Offset(1, 0)); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.OpenDoor, Target: door.Id)).Success);
        var moth = ReplaceRegular(world, EnemyEmbers.Definition); moth.Position = door.Position.Offset(2, 0); moth.Tactics!.Enemy!.EmberDives = 1;
        combat.ResolveAttack(new(combat.State.NextAction++, moth.Id, moth.Position, [door.Position], EnemyEmbers.Dive,
            AttackDelivery.Area, DamageElement.Fire, 1, combat.Clock, EmberLanding: door.Position));
        Assert.Equal(door.Position, moth.Position); Assert.Contains(combat.State.Embers, e => e.Position == door.Position);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void SavedDoorIdentityCannotDisagreeWithItsPhysicalTile()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var door = combat.SpawnDoor("test:invalid-door", hero.Position.Offset(1, 0));
        world.Chunks.Edit(door.Position, Material.Floor);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        world.Chunks.Edit(door.Position, Material.ClosedDoor); TacticalValidation.Validate(world.Plan, world.State);
    }
}
