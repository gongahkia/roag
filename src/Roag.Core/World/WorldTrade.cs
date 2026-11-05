namespace Roag.Core.World;

public sealed record WorldTradeQuote(bool Valid, string Reason, int Town = -1, int GoldChange = 0);

public sealed partial class WorldSession
{
    public bool IsTownMarketOpen => State.NativeCampaign == null || HourOfDay is >= 6 and < 20;
    /// <summary>The acting member's physical location, independent of the inspected member/camera.</summary>
    public WorldTradeQuote QuoteTrade(long memberId, WorldCommand? command)
    {
        var member = Party.FirstOrDefault(a => a.Id == memberId && a.Life == LifeState.Alive);
        return member == null ? new(false, "Choose a conscious party member.") : QuoteTradeAt(member.Position, command);
    }

    private WorldTradeQuote QuoteTradeAt(WorldPosition position, WorldCommand? command)
    {
        if (command == null || command.Kind is not (WorldCommandKind.Buy or WorldCommandKind.Sell)
            || command.Amount is < 1 or > 999 || command.Item is not ("food" or "wood" or "stone" or "medicine"))
            return new(false, "Choose food, wood, stone or medicine and an amount from one to 999.");
        var townPlan = Plan.Towns.FirstOrDefault(t => position.Distance(t.Center) <= 28);
        if (townPlan == null) return new(false, "Trade on the surface near a town.");
        if (!IsTownMarketOpen)
            return new(false, "The town market is closed. Trading hours are 06:00–20:00.");
        var town = State.Towns[townPlan.Id]; int amount = command.Amount, price = town.Price(command.Item);
        bool buy = command.Kind == WorldCommandKind.Buy;
        long gold = buy ? -(long)price * amount : (long)Math.Max(1, price / 2) * amount;
        if (buy && (town.Stocks[command.Item] < amount || Count("gold") < -gold))
            return new(false, "The stock or your gold is insufficient.");
        if (!buy && Count(command.Item) < amount) return new(false, "You do not have that stack.");
        if (gold is < int.MinValue or > int.MaxValue || Count("gold") + gold > int.MaxValue
            || buy && (long)Count(command.Item) + amount > int.MaxValue
            || !buy && (long)town.Stocks[command.Item] + amount > int.MaxValue)
            return new(false, "This exchange would exceed storage capacity.");
        return new(true, "", townPlan.Id, (int)gold);
    }

    internal WorldTradeQuote ApplyNativeTrade(Actor member, WorldCommand command)
    {
        var quote = QuoteTrade(member.Id, command);
        if (!quote.Valid) throw new InvalidOperationException("Trade changed after its preview: " + quote.Reason);
        ApplyTrade(command, quote);
        return quote;
    }

    private void ApplyTrade(WorldCommand command, WorldTradeQuote quote)
    {
        bool buy = command.Kind == WorldCommandKind.Buy; int amount = command.Amount;
        State.Towns[quote.Town].Stocks[command.Item] += buy ? -amount : amount;
        Add(command.Item, buy ? amount : -amount); Add("gold", quote.GoldChange);
        if (buy) State.ResourcesExported += amount;
        else State.ResourcesImported += amount;
    }
}
