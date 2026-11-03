using Roag.Core.Expeditions;
using Roag.Core.Content;

namespace Roag.Core.World;

public sealed record WorldLootQuote(bool Valid, string Reason, IReadOnlyDictionary<string, int> Items, string Selected = "");

public sealed partial class WorldSession
{
    public WorldLootQuote QuoteWorldLoot(long memberId, string cacheId, int choice = 0)
    {
        var member = Party.FirstOrDefault(a => a.Id == memberId && a.Life == LifeState.Alive);
        var cache = State.Loot.FirstOrDefault(c => c.Id == cacheId && !c.Taken);
        if (!HasNativeParty || member == null || cache == null || member.Position.Distance(cache.Position) > 1
            || !VisionFrom(member.Position).Contains(cache.Position))
            return InvalidLoot("Stand on or beside an observed unopened cache or remains.");
        if (choice < 0 || choice > cache.Choices.Length || cache.Choices.Length > 0 && choice == 0)
            return InvalidLoot("Choose one of this cache's offered pieces of equipment.");
        return QuoteLootAt(member.Position, cache, choice == 0 ? "" : cache.Choices[choice - 1]);
    }

    private static WorldLootQuote InvalidLoot(string reason) => new(false, reason, new Dictionary<string, int>());

    private WorldLootQuote QuoteLootAt(WorldPosition position, LootCache? cache, string choice)
    {
        if (cache == null || cache.Taken || position.Distance(cache.Position) > 1)
            return InvalidLoot("Approach an unopened cache.");
        var discovery = State.Discoveries.FirstOrDefault(d => d.Id == cache.Id);
        if (discovery != null && DiscoveryGuarded(discovery)) return InvalidLoot("Living foes still guard these supplies.");
        bool partial = IsWorkSupplyCache(cache);
        var items = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (stack, amount) in cache.Contents)
            items[stack] = partial ? Math.Min(amount, Math.Max(0, 9999 - Count(stack))) : amount;
        string selected = "";
        if (!partial && cache.Choices.Length > 0)
        {
            if (!cache.Choices.Contains(choice)) return InvalidLoot("Choose one of the three offered pieces of equipment.");
            selected = choice;
            string stack = Item(choice).Stack;
            if ((long)Count(stack) + items.GetValueOrDefault(stack) + 1 > 9999)
                return InvalidLoot("Make room in the shared pack before recovering this cache.");
            items[stack] = items.GetValueOrDefault(stack) + 1;
        }
        if (items.Any(p => (long)Count(p.Key) + p.Value > 9999))
            return InvalidLoot("Make room in the shared pack before recovering this cache.");
        foreach (string stack in items.Where(p => p.Value == 0).Select(p => p.Key).ToArray()) items.Remove(stack);
        if (partial && items.Count == 0) return InvalidLoot("Make room in the pack before recovering this supply bundle.");
        return new(true, "", items, selected);
    }

    private ActionResult ApplyLoot(LootCache cache, WorldLootQuote quote)
    {
        foreach (var (stack, count) in quote.Items) Add(stack, count);
        if (IsWorkSupplyCache(cache))
        {
            foreach (var (stack, count) in quote.Items) cache.Contents[stack] -= count;
            cache.Taken = cache.Contents.Values.All(v => v == 0);
            return new(true, "Recovered " + quote.Items.Values.Sum() + " supplies." + (cache.Taken ? "" : " The remaining bundle stays here."));
        }
        cache.Selected = quote.Selected.Length == 0 ? null : quote.Selected;
        cache.Taken = true;
        string recovered = "Recovered " + string.Join(", ", cache.Contents.Select(p => p.Value + " "
            + (Content.All<ItemDefinition>().FirstOrDefault(i => i.Stack == p.Key)?.Name ?? p.Key)))
            + (cache.Selected is { } selected ? "; chose " + Item(selected).Name : "") + ".";
        Log(recovered);
        return new(true, recovered);
    }

    internal WorldLootQuote ApplyNativeWorldLoot(Actor member, string cacheId, int choice)
    {
        var quote = QuoteWorldLoot(member.Id, cacheId, choice);
        if (!quote.Valid) throw new InvalidOperationException("World loot changed after preview: " + quote.Reason);
        ApplyLoot(State.Loot.Single(c => c.Id == cacheId), quote);
        return quote;
    }
}
