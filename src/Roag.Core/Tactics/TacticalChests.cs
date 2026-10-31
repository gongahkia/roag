using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record BoonOffer(string Boon, BoonRarity Rarity);
public sealed record BoonChest(long Id, string Definition, string Marker, WorldPosition Position, int Seed,
    bool Claimed = false, long Recipient = 0, int Choice = 0);
public sealed record BoonChestDefinition(string Id, string Name, int Price, int[] RarityWeights)
{
    public static BoonChestDefinition Get(string id) => id switch
    {
        "core:chest/standard" => new(id, "Boon Chest", 5, [70, 25, 4, 1]),
        "core:chest/free_elite" => new(id, "Elite Boon Chest", 0, [20, 45, 28, 7]),
        _ => throw new ArgumentException("Unknown boon chest.", nameof(id))
    };
}

public sealed class TacticalChestState
{
    public long NextChest { get; set; } = 1;
    // Profile unlocks are a later tranche. Preserve this run's pool in its save.
    public string[] AvailableBoons { get; set; } = BoonCatalog.Default.NormalPool.ToArray();
    public List<BoonChest> Chests { get; set; } = [];

    internal IReadOnlyList<BoonOffer> Offers(BoonChest chest, ClassKind recipient)
        => Offers(chest.Seed, chest.Definition, recipient);

    internal IReadOnlyList<BoonOffer> Offers(int seed, string definitionId, ClassKind recipient)
    {
        var pool = AvailableBoons.Where(id => BoonCatalog.Default.Get(id).Allows(recipient)).ToList();
        if (pool.Count < 3) throw new InvalidDataException("Chest pool needs three class-eligible boons.");
        var random = new RoagRandom(seed);
        var definition = BoonChestDefinition.Get(definitionId);
        var offers = new List<BoonOffer>();
        for (int i = 0; i < 3; i++)
        {
            int index = random.NextOneBased(pool.Count) - 1;
            string id = pool[index]; pool.RemoveAt(index);
            int roll = random.NextOneBased(definition.RarityWeights.Sum());
            for (int r = 0; r < definition.RarityWeights.Length; r++)
            {
                roll -= definition.RarityWeights[r];
                if (roll > 0) continue;
                offers.Add(new(id, (BoonRarity)r)); break;
            }
        }
        return offers;
    }

    internal void Validate(WorldPlan plan, IReadOnlyDictionary<long, Actor> actors)
    {
        if (NextChest <= 0 || AvailableBoons == null || AvailableBoons.Distinct().Count() != AvailableBoons.Length
            || AvailableBoons.Any(id => !BoonCatalog.Default.NormalPool.Contains(id)) || Chests == null
            || Chests.Select(c => c?.Id).Distinct().Count() != Chests.Count
            || Chests.Select(c => c?.Marker).Distinct(StringComparer.Ordinal).Count() != Chests.Count
            || Chests.Select(c => c?.Position).Distinct().Count() != Chests.Count)
            throw new InvalidDataException("Invalid chest registry or available boon pool.");
        long previous = 0;
        foreach (var chest in Chests)
        {
            if (chest == null || chest.Id <= previous || chest.Id >= NextChest || string.IsNullOrWhiteSpace(chest.Marker)
                || !plan.Contains(chest.Position) || chest.Seed <= 0 || chest.Seed >= RoagRandom.Modulus
                || chest.Definition is not ("core:chest/standard" or "core:chest/free_elite")
                || !chest.Claimed && (chest.Recipient != 0 || chest.Choice != 0)
                || chest.Claimed && (chest.Choice is < 1 or > 3 || !actors.TryGetValue(chest.Recipient, out var recipient)
                    || recipient.Tactics?.Build == null))
                throw new InvalidDataException("Invalid chest identity, claim or recipient.");
            previous = chest.Id;
        }
    }
}

public sealed partial class TacticalCombat
{
    public BoonChest SpawnBoonChest(string definition, string marker, WorldPosition position)
    {
        _ = BoonChestDefinition.Get(definition);
        if (string.IsNullOrWhiteSpace(marker) || State.Chests.Chests.Any(c => c.Marker == marker || c.Position == position)
            || !_world.CanEnter(position) || ProtectedForcedLanding(position))
            throw new ArgumentException("A chest needs a unique marker and clear physical ground.");
        int seed = RoagRandom.Derive((int)(WorldRandom.Hash(_world.Seed, 0, salt: 9310) % (RoagRandom.Modulus - 1)) + 1, marker);
        var chest = new BoonChest(State.Chests.NextChest++, definition, marker, position, seed);
        State.Chests.Chests.Add(chest);
        return chest;
    }

    /// <summary>Recipient is selected before filtering/drawing; inspection advances no random stream.</summary>
    public IReadOnlyList<BoonOffer> ChestOffers(long chestId, long recipientId)
    {
        var recipient = Actor(recipientId);
        var chest = State.Chests.Chests.SingleOrDefault(c => c.Id == chestId);
        if (chest == null || chest.Claimed || !Friendly(recipient) || recipient.Life != LifeState.Alive
            || recipient.Tactics?.Build is not { } build || recipient.Position.Distance(chest.Position) != 1
            || !_world.VisionFrom(recipient.Position).Contains(chest.Position)) return [];
        return State.Chests.Offers(chest, build.Class);
    }

    private TacticalPreview PreviewChest(Actor actor, TacticalIntent intent)
    {
        if (intent.Choice is < 1 or > 3) return Invalid("Choose offer 1, 2 or 3.");
        var offers = ChestOffers(intent.Target, actor.Id);
        if (offers.Count != 3) return Invalid("Stand beside an unclaimed chest with the chosen recipient.");
        var chest = State.Chests.Chests.Single(c => c.Id == intent.Target);
        if (_world.State.Inventory.GetValueOrDefault("gold") < BoonChestDefinition.Get(chest.Definition).Price)
            return Invalid("The shared purse cannot afford this chest.");
        var offer = offers[intent.Choice - 1];
        try { _ = actor.Tactics!.Boons.Inventory.Grant(offer.Boon, offer.Rarity, actor.Tactics.Build!.Class); }
        catch (Exception e) when (e is InvalidOperationException or OverflowException) { return Invalid(e.Message); }
        return new(true, "", ActionTiming.Recovery(100, EffectiveSpeed(actor)), [chest.Position]);
    }

    private void ClaimChest(Actor actor, TacticalIntent intent, long action)
    {
        int index = State.Chests.Chests.FindIndex(c => c.Id == intent.Target);
        var chest = State.Chests.Chests[index];
        var offer = State.Chests.Offers(chest, actor.Tactics!.Build!.Class)[intent.Choice - 1];
        int price = BoonChestDefinition.Get(chest.Definition).Price;
        actor.Tactics.Boons.Inventory = actor.Tactics.Boons.Inventory.Grant(offer.Boon, offer.Rarity, actor.Tactics.Build.Class);
        _world.State.Inventory["gold"] = _world.State.Inventory.GetValueOrDefault("gold") - price;
        State.Chests.Chests[index] = chest with { Claimed = true, Recipient = actor.Id, Choice = intent.Choice };
        Emit("CurrencySpent", actor.Id, 0, action, chest.Position, price);
        Emit("BoonGranted", actor.Id, actor.Id, action, chest.Position, ability: offer.Boon);
        var snapshot = CaptureBoonEvent(BoonTrigger.ChestClaimed, actor.Id, 0, action, chest.Position)
            with { Price = price, BoonId = offer.Boon };
        Emit("ChestClaimed", actor.Id, actor.Id, action, chest.Position, price, snapshot: snapshot);
    }
}
