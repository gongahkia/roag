using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeWorldLootTests
{
    private sealed class Cache : IDisposable
    {
        public WorldSession World { get; }
        public Actor Hero { get; }
        public TacticalCombat Combat => World.DungeonCombat;
        public LootCache Loot { get; }
        public Cache(bool campaign = false)
        {
            World = campaign ? WorldSession.CreateNativeCampaign(42) : WorldSession.CreateDungeonRun(42, survival: true);
            Hero = World.Party.Single();
            Hero.Tactics!.AutoEat = false;
            var position = Hero.Position.Offset(1, 0); World.Chunks.Edit(position, Material.Floor);
            Loot = new() { Id = "unit-world-cache", Position = position, Contents = new() { ["food"] = 3, ["gold"] = 7 } };
            World.State.Loot.Add(Loot); World.Reveal();
        }
        public void Dispose() => World.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SuppliesEnterTheSharedPackForOnePaidActionWithoutEatingOrHealing(bool campaign)
    {
        using var f = new Cache(campaign); f.Hero.Health -= 4; f.Hero.Body.Hunger = 30;
        int health = f.Hero.Health, food = f.World.Count("food"), gold = f.World.Count("gold");
        var intent = new TacticalIntent(TacticalAction.CollectCache, Item: f.Loot.Id);
        string before = JsonSerializer.Serialize(f.World.State);
        Assert.True(f.World.QuoteWorldLoot(f.Hero.Id, f.Loot.Id).Valid);
        var preview = f.Combat.Preview(intent); Assert.True(preview.Valid);
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
        Assert.True(f.Combat.Commit(intent).Success);
        Assert.True(f.Loot.Taken); Assert.Equal(food + 3, f.World.Count("food")); Assert.Equal(gold + 7, f.World.Count("gold"));
        Assert.Equal(health, f.Hero.Health); Assert.Equal(30, f.Hero.Body.Hunger);
        Assert.Equal(preview.Recovery, Assert.Single(f.Combat.State.Timeline.Pending, w => w.ActorId == f.Hero.Id).Due);
        Assert.Contains(f.Combat.State.Events, e => e.Kind == "CurrencyCollected" && e.Source == f.Hero.Id && e.Amount == 7);
        Assert.Contains(f.Combat.State.Events, e => e.Kind == "ProvisionsCollected" && e.Source == f.Hero.Id && e.Amount == 3);
        f.Combat.AdvanceToPartyDecision(); before = JsonSerializer.Serialize(f.World.State);
        Assert.False(f.Combat.Commit(intent).Success); Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
        TacticalValidation.Validate(f.World.Plan, f.World.State);
    }

    [Fact]
    public void RecoveryUsesTheActingMembersPrivateLocationWhileAnotherFloorIsInCombat()
    {
        using var f = new Cache();
        var friend = DungeonPartyTests.AddMember(f.World, ClassKind.Ranger, 2);
        var enemyCell = friend.Position.Offset(1, 0); f.World.Chunks.Edit(enemyCell, Material.Floor);
        var enemy = ActorFactory.Create(f.World.Content, f.World.Content.Game.Raider, 42, f.World.State.NextActorId++, enemyCell, 0);
        f.World.State.Actors.Add(enemy); f.Combat.RegisterRetainedEnemy(enemy, 600);
        Assert.True(f.Combat.IsLocallyEngaged(friend.Id)); Assert.False(f.Combat.IsLocallyEngaged(f.Hero.Id));
        Assert.True(f.World.SelectDungeonMember(friend.Id).Success);
        var readiness = f.Combat.State.Timeline.Pending.Single(w => w.ActorId == friend.Id);
        Assert.True(f.World.QuoteWorldLoot(f.Hero.Id, f.Loot.Id).Valid);
        Assert.False(f.World.QuoteWorldLoot(friend.Id, f.Loot.Id).Valid);
        Assert.True(f.Combat.Commit(new(TacticalAction.CollectCache, Item: f.Loot.Id)).Success);
        Assert.True(f.Loot.Taken); Assert.Contains(readiness, f.Combat.State.Timeline.Pending);
        Assert.Equal(friend.Position, f.World.State.Position);
    }

    [Theory]
    [InlineData("remote")]
    [InlineData("floor")]
    [InlineData("capacity")]
    [InlineData("choice")]
    [InlineData("missing")]
    public void RejectedRecoveryLeavesInventoryCacheAndTimelineUnchanged(string reason)
    {
        using var f = new Cache(); string id = f.Loot.Id; int choice = 0;
        if (reason == "remote") f.Loot.Position = f.Hero.Position.Offset(4, 0);
        if (reason == "floor") f.Loot.Position = f.Loot.Position with { Floor = 2 };
        if (reason == "capacity") f.World.State.Inventory["food"] = 9999;
        if (reason == "choice") choice = 1;
        if (reason == "missing") id = "missing";
        string before = JsonSerializer.Serialize(f.World.State);
        Assert.False(f.Combat.Commit(new(TacticalAction.CollectCache, Item: id, Choice: choice)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
    }

    [Fact]
    public void OneEquipmentChoiceAndTheCacheReceiptPersistWithExactPaidContinuation()
    {
        using var f = new Cache();
        f.Loot.Choices = f.World.Content.All<ItemDefinition>().Where(i => i.Slot != "none" && !i.Tags.Contains("signature")).Take(3).Select(i => i.Id).ToArray();
        string selected = f.Loot.Choices[1], stack = f.World.Item(selected).Stack;
        int count = f.World.Count(stack);
        Assert.False(f.Combat.Preview(new(TacticalAction.CollectCache, Item: f.Loot.Id)).Valid);
        Assert.True(f.World.SubmitDungeon(new(TacticalAction.CollectCache, Item: f.Loot.Id, Choice: 2)).Success);
        Assert.Equal(selected, f.Loot.Selected); Assert.Equal(count + 1, f.World.Count(stack));
        string folder = Path.Combine(Path.GetTempPath(), "roag-native-world-loot-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(folder, "campaign.json"); f.World.Save(path);
            using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(f.World.SubmitRoag(new("wait")).Success); Assert.True(loaded.SubmitRoag(new("wait")).Success);
            Assert.Equal(JsonSerializer.Serialize(f.World.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public void ReturnedWorkBundleKeepsOverflowAndOnlyChargesForWhatFits()
    {
        using var f = new Cache(); f.Loot.Id = "work-return-unit"; f.World.State.Inventory["food"] = 9998;
        Assert.True(f.World.SubmitDungeon(new(TacticalAction.CollectCache, Item: f.Loot.Id)).Success);
        Assert.Equal(9999, f.World.Count("food")); Assert.Equal(2, f.Loot.Contents["food"]);
        Assert.Equal(0, f.Loot.Contents["gold"]); Assert.False(f.Loot.Taken);
        string before = JsonSerializer.Serialize(f.World.State);
        Assert.False(f.World.SubmitDungeon(new(TacticalAction.CollectCache, Item: f.Loot.Id)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(f.World.State));
    }
}
