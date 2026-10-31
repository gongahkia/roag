using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    [Fact]
    public void ChestOffersMatchOriginalClassFilteringAndRarityDrawOrder()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-chest-offers.json")));
        var state = new TacticalChestState(); int count = 0;
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input");
            var chest = new BoonChest(1, input.GetProperty("chest").GetString()!, "reference", new(100, 100), input.GetProperty("seed").GetInt32());
            var offers = state.Offers(chest, ClassCatalog.Get(input.GetProperty("class_id").GetString()!).Kind);
            Assert.Equal(sample.GetProperty("offers").EnumerateArray().Select(o => (o.GetProperty("boon").GetString(), o.GetProperty("rarity").GetString())),
                offers.Select(o => ((string?)o.Boon, (string?)o.Rarity.ToString().ToLowerInvariant())));
            count++;
        }
        Assert.Equal(120, count);
    }

    [Fact]
    public void ChestInspectionUsesChosenRecipientAndCannotRerollOrMutateTimeOrCombatRandom()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Geomancer);
        a.Friend.Position = new(101, 101);
        var chest = a.Combat.SpawnBoonChest("core:chest/standard", "room:chest/one", new(101, 100));
        string before = JsonSerializer.Serialize(a.World.State);
        var first = a.Combat.ChestOffers(chest.Id, a.Hero.Id);
        Assert.Equal(3, first.Count); Assert.Equal(3, first.Select(o => o.Boon).Distinct().Count());
        Assert.All(first, o => Assert.True(BoonCatalog.Default.Get(o.Boon).Allows(ClassKind.Adventurer)));
        var other = a.Combat.ChestOffers(chest.Id, a.Friend.Id);
        Assert.All(other, o => Assert.True(BoonCatalog.Default.Get(o.Boon).Allows(ClassKind.Geomancer)));
        Assert.Equal(first, a.Combat.ChestOffers(chest.Id, a.Hero.Id));
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void ChestClaimPaysOnceGrantsAnOwnedBoonAndReleasesItsPhysicalTile()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.World.State.Inventory["gold"] = 10;
        var chest = a.Combat.SpawnBoonChest("core:chest/standard", "room:chest/one", new(101, 100));
        var offer = a.Combat.ChestOffers(chest.Id, a.Hero.Id)[0];
        Assert.False(a.World.CanEnter(chest.Position));
        Assert.True(a.Combat.Commit(new(TacticalAction.ClaimChest, Target: chest.Id, Choice: 1)).Success);
        Assert.Equal(5, a.World.Count("gold")); Assert.Equal(1, a.Hero.Tactics!.Boons.Inventory.Stacks[offer.Boon][offer.Rarity]);
        Assert.True(a.World.CanEnter(chest.Position));
        Assert.Equal(100, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Hero.Id).Due);
        a.Next(); string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(new(TacticalAction.ClaimChest, Target: chest.Id, Choice: 1)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("poor")]
    [InlineData("far")]
    [InlineData("choice")]
    [InlineData("overflow")]
    public void InvalidChestClaimsRejectWithoutSpendingResourcesTimeOrRandom(string reason)
    {
        using var a = new Arena(ClassKind.Adventurer);
        var chest = a.Combat.SpawnBoonChest("core:chest/standard", "room:chest/one", new(101, 100));
        a.World.State.Inventory["gold"] = reason == "poor" ? 0 : 10;
        if (reason == "far") a.Hero.Position = new(99, 100);
        if (reason == "overflow")
        {
            var offer = a.Combat.ChestOffers(chest.Id, a.Hero.Id)[0];
            a.Hero.Tactics!.Boons.Inventory = a.Hero.Tactics.Boons.Inventory.Grant(offer.Boon, offer.Rarity, ClassKind.Adventurer, int.MaxValue);
        }
        int choice = reason == "choice" ? 4 : 1;
        string before = JsonSerializer.Serialize(a.World.State);
        Assert.False(a.Combat.Commit(new(TacticalAction.ClaimChest, Target: chest.Id, Choice: choice)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(a.World.State));
    }

    [Fact]
    public void ChestClaimsAndUnclaimedOffersSurviveAnActualWorldSave()
    {
        using var a = new Arena(ClassKind.Adventurer); a.World.State.Inventory["gold"] = 10;
        var chest = a.Combat.SpawnBoonChest("core:chest/free_elite", "room:elite/one", new(101, 100));
        var offers = a.Combat.ChestOffers(chest.Id, a.Hero.Id);
        string directory = Path.Combine(Path.GetTempPath(), "roag-chests-" + Guid.NewGuid().ToString("N"));
        try
        {
            a.World.Save(directory);
            using var loaded = WorldSession.Load(directory); var resumed = new TacticalCombat(loaded, _ => { });
            Assert.Equal(offers, resumed.ChestOffers(chest.Id, a.Hero.Id));
            var intent = new TacticalIntent(TacticalAction.ClaimChest, Target: chest.Id, Choice: 2);
            Assert.True(a.Combat.Commit(intent).Success); Assert.True(resumed.Commit(intent).Success);
            Assert.Equal(NativeActorSnapshot(a.World), NativeActorSnapshot(loaded));
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(resumed.State));
            loaded.Save(directory);
            using var claimed = WorldSession.Load(directory);
            Assert.True(claimed.State.Tactics!.Chests.Chests.Single().Claimed);
            Assert.Equal(a.Hero.Id, claimed.State.Tactics.Chests.Chests.Single().Recipient);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void ChestClaimRunsTheOriginalPurchaseRefundListenerWithoutASecondPickup()
    {
        using var a = new Arena(ClassKind.Adventurer); a.World.State.Inventory["gold"] = 10;
        var refund = BoonCatalog.Default.Definitions.Single(d => d.Effects.Any(e => e.Kind == BoonEffectKind.RefundPurchase));
        a.Hero.Tactics!.Boons.Inventory = a.Hero.Tactics.Boons.Inventory.Grant(refund.Id, BoonRarity.Common, ClassKind.Adventurer);
        var chest = a.Combat.SpawnBoonChest("core:chest/standard", "room:refund/one", new(101, 100));
        int choice = a.Combat.ChestOffers(chest.Id, a.Hero.Id).ToList().FindIndex(o => o.Boon != refund.Id) + 1;
        int expectedRefund = (int)Math.Min(5, a.Hero.Tactics.Boons.Inventory.Magnitude(refund.Id).Power);
        Assert.True(a.Combat.Commit(new(TacticalAction.ClaimChest, Target: chest.Id, Choice: choice)).Success);
        Assert.Equal(5 + expectedRefund, a.World.Count("gold"));
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "CurrencyCollected");
        Assert.Single(a.Combat.State.Events, e => e.Kind == "ChestClaimed");
    }

    [Fact]
    public void SavedChestsRejectDuplicateMarkersAndForgedRecipientClaims()
    {
        using var a = new Arena(ClassKind.Adventurer);
        var chest = a.Combat.SpawnBoonChest("core:chest/standard", "room:validation/one", new(101, 100));
        var state = a.Combat.State.Chests;
        state.Chests.Add(chest with { Id = state.NextChest++, Position = new(102, 100) });
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        state.Chests.RemoveAt(1);
        state.Chests[0] = chest with { Claimed = true, Recipient = a.Enemy.Id, Choice = 1 };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        state.Chests[0] = chest with { Seed = 0 };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
    }
}
