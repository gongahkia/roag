using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeTradeTests
{
    private sealed class Market : IDisposable
    {
        public WorldSession World { get; } = WorldSession.Create(42);
        public TacticalCombat Combat { get; }
        public Actor Hero { get; }
        public int Town { get; }
        public Market()
        {
            Hero = World.Party.Single(); Town = Hero.HomeTown;
            Hero.Position = World.State.Position = World.Plan.Towns[Town].Center;
            World.State.Inventory["gold"] = 1000;
            Combat = World.CreateTacticalSimulation();
            Combat.Register(Hero, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42), AutoEat = false });
            Assert.Same(Hero, Combat.AdvanceToDecision());
        }
        public void Dispose() => World.Dispose();
    }

    [Fact]
    public void NativeTradeQuotesAreFreeAndBuyingPaysOwnRecoveryWithoutFeedingOrHealing()
    {
        using var f = new Market(); int stock = f.World.State.Towns[f.Town].Stocks["food"], food = f.World.Count("food");
        var command = new WorldCommand(WorldCommandKind.Buy, Item: "food", Amount: 2);
        string before = JsonSerializer.Serialize(f.World.State);
        var quote = f.World.QuoteTrade(f.Hero.Id, command); Assert.True(quote.Valid);
        var preview = f.Combat.Preview(new(TacticalAction.Trade, Work: command)); Assert.True(preview.Valid);
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
        double hunger = f.Hero.Body.Hunger; int health = f.Hero.Health;
        Assert.True(f.Combat.Commit(new(TacticalAction.Trade, Work: command)).Success);
        Assert.Equal(1000 + quote.GoldChange, f.World.Count("gold")); Assert.Equal(food + 2, f.World.Count("food"));
        Assert.Equal(stock - 2, f.World.State.Towns[f.Town].Stocks["food"]);
        Assert.Equal(hunger, f.Hero.Body.Hunger); Assert.Equal(health, f.Hero.Health);
        Assert.Equal(preview.Recovery, Assert.Single(f.Combat.State.Timeline.Pending, w => w.ActorId == f.Hero.Id).Due);
        Assert.Contains(f.Combat.State.Events, e => e.Kind == "CurrencySpent" && e.Source == f.Hero.Id && e.Amount == -quote.GoldChange);
        TacticalValidation.Validate(f.World.Plan, f.World.State);
    }

    [Theory]
    [InlineData("floor")]
    [InlineData("outside")]
    [InlineData("gold")]
    [InlineData("quantity")]
    [InlineData("capacity")]
    [InlineData("malformed")]
    public void InvalidNativeTradesLeaveWorldInventoryAndTimelineExactlyUnchanged(string reason)
    {
        using var f = new Market(); var command = new WorldCommand(WorldCommandKind.Buy, Item: "food");
        if (reason == "floor") f.Hero.Position = f.Hero.Position with { Floor = 1 };
        if (reason == "outside") f.Hero.Position = new(100, 100);
        if (reason == "gold") f.World.State.Inventory["gold"] = 0;
        if (reason == "quantity") command = command with { Amount = 1000 };
        if (reason == "capacity") f.World.State.Inventory["food"] = int.MaxValue;
        if (reason == "malformed") command = command with { Kind = WorldCommandKind.Equip };
        string before = JsonSerializer.Serialize(f.World.State);
        var result = f.Combat.Commit(new(TacticalAction.Trade, Work: command));
        Assert.False(result.Success); Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
    }

    [Fact]
    public void TradingUsesReadyMembersLocationAndOnlyTheirRecoveryWhileAnotherFloorIsInCombat()
    {
        using var f = new Market(); var friend = f.World.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        friend.Recruited = true; friend.Position = new(100, 100, 1); f.World.State.Party.Add(friend.Id);
        for (int y = 98; y <= 103; y++) for (int x = 98; x <= 103; x++) f.World.Chunks.Edit(new(x, y, 1), Material.Floor);
        f.Combat.Register(friend, new() { Build = CharacterBuild.Create(ClassKind.Ranger, 43) }, 500);
        var enemy = ActorFactory.Create(f.World.Content, "character.raider", 42, f.World.State.NextActorId++, new(101, 100, 1), 0);
        f.World.State.Actors.Add(enemy); f.Combat.RegisterRetainedEnemy(enemy, 600);
        Assert.True(f.Combat.IsLocallyEngaged(friend.Id)); Assert.False(f.Combat.IsLocallyEngaged(f.Hero.Id));
        f.World.State.Position = friend.Position; // Free inspection follows the other member, not the trader.
        var readiness = f.Combat.State.Timeline.Pending.Single(w => w.ActorId == friend.Id);
        f.World.State.Inventory["wood"] = 5; int stock = f.World.State.Towns[f.Town].Stocks["wood"];
        var command = new WorldCommand(WorldCommandKind.Sell, Item: "wood", Amount: 2);
        var quote = f.World.QuoteTrade(f.Hero.Id, command); Assert.True(quote.Valid);
        Assert.True(f.Combat.Commit(new(TacticalAction.Trade, Work: command)).Success);
        Assert.Equal(3, f.World.Count("wood")); Assert.Equal(stock + 2, f.World.State.Towns[f.Town].Stocks["wood"]);
        Assert.Equal(1000 + quote.GoldChange, f.World.Count("gold"));
        Assert.Contains(readiness, f.Combat.State.Timeline.Pending);
        Assert.Contains(f.Combat.State.Events, e => e.Kind == "CurrencyCollected" && e.Source == f.Hero.Id && e.Amount == quote.GoldChange);
        TacticalValidation.Validate(f.World.Plan, f.World.State);
    }

    [Fact]
    public void TradeReceiptsStocksSharedInventoryAndPendingRecoveryRoundTripWithoutRepeatingTheExchange()
    {
        using var f = new Market(); Assert.True(f.Combat.Commit(new(TacticalAction.Trade,
            Work: new(WorldCommandKind.Buy, Item: "medicine", Amount: 2))).Success);
        string path = Path.Combine(Path.GetTempPath(), "roag-native-market-" + Guid.NewGuid().ToString("N"));
        try
        {
            f.World.Save(path); using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(loaded.State));
            var other = loaded.CreateTacticalSimulation();
            Assert.Equal(f.Combat.AdvanceToDecision()!.Id, other.AdvanceToDecision()!.Id);
            Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
}
