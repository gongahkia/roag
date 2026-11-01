using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonProvisionServiceTests
{
    private static DungeonService Place(WorldSession world, DungeonServiceKind kind)
    {
        var at = world.Party.Single().Position;
        var cell = new[] { at.Offset(1, 0), at.Offset(0, 1), at.Offset(-1, 0), at.Offset(0, -1) }
            .First(p => world.CanEnter(p) && world.ObjectAt(p) == null && world.DungeonTerrainAt(p) == null);
        return world.DungeonCombat.SpawnDungeonService(kind, "test:provision-source", cell);
    }

    [Theory]
    [InlineData(DungeonServiceKind.RationCache, 0)] [InlineData(DungeonServiceKind.TravellingTrader, 2)]
    public void FoodServicesStockThePackOnceAtFullHpWithoutEatingHealingOrActivatingFoodBoons(DungeonServiceKind kind, int price)
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        world.State.Inventory["gold"] = 10;
        hero.Tactics!.Boons.Inventory = hero.Tactics.Boons.Inventory.Grant("core:boon/road_flask", BoonRarity.Common, ClassKind.Adventurer);
        var node = Place(world, kind); int food = world.Count("food"), hp = hero.Health;
        var choices = world.DungeonCombat.DungeonServiceChoices(node.Id, hero.Id);
        var option = choices.Single(o => o.Provisions == 4); Assert.True(option.Enabled); Assert.Equal(price, option.Price);
        string before = JsonSerializer.Serialize(world.State);
        var intent = new TacticalIntent(TacticalAction.Interact, Target: node.Id, Choice: option.Choice);
        Assert.True(world.DungeonCombat.Preview(intent).Valid); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.SubmitDungeon(intent).Success);
        Assert.Equal(food + 4, world.Count("food")); Assert.Equal(10 - price, world.Count("gold"));
        Assert.Equal(hp, hero.Health); Assert.Equal(0, hero.Body.Hunger); Assert.Equal(0, hero.Tactics.Boons.SpeedBonus(world.DungeonCombat.Clock));
        Assert.Equal(hero.Id, world.State.Tactics!.Services.Nodes.Single(n => n.Id == node.Id).UsedBy);
        string after = JsonSerializer.Serialize(world.State); Assert.False(world.SubmitDungeon(intent).Success);
        Assert.Equal(after, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void ARecruitsRealFoodAndMedicineBecomeSharedWithoutDuplicatingTheirPersonalCargo()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var hero = world.Party.Single();
        var candidate = world.ActorById(world.State.Dungeon!.Recruits[0].Actor);
        candidate.Cargo["food"] = 3; candidate.Cargo["medicine"] = 1;
        hero.Position = world.State.Position = candidate.Position.Offset(1, 0); world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        int food = world.Count("food"), medicine = world.Count("medicine");
        Assert.True(world.SubmitDungeon(new(TacticalAction.Recruit, Target: candidate.Id)).Success);
        Assert.Equal(food + 3, world.Count("food")); Assert.Equal(medicine + 1, world.Count("medicine")); Assert.Empty(candidate.Cargo);
        Assert.True(candidate.Tactics!.AutoEat); Assert.False(candidate.Tactics.AutoEatInCombat);
    }
}
