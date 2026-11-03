using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewWorldLoot(Actor actor, TacticalIntent intent)
    {
        if (!Friendly(actor)) return Invalid("Only a party member can recover supplies into the shared pack.");
        if (IsLocallyEngaged(actor.Id)) return Invalid("Leave immediate danger before opening a world cache or remains.");
        var quote = _world.QuoteWorldLoot(actor.Id, intent.Item, intent.Choice);
        return quote.Valid ? new(true, "", ActionTiming.Recovery(Tuning.WaitCost, EffectiveSpeed(actor)), []) : Invalid(quote.Reason);
    }

    private void CommitWorldLoot(Actor actor, TacticalIntent intent, long action)
    {
        var quote = _world.ApplyNativeWorldLoot(actor, intent.Item, intent.Choice);
        Emit("WorldLootClaimed", actor.Id, 0, action, actor.Position, quote.Items.Values.Sum(), ability: intent.Item);
        int gold = quote.Items.GetValueOrDefault("gold");
        if (gold > 0)
            Emit("CurrencyCollected", actor.Id, 0, action, actor.Position, gold,
                snapshot: CaptureBoonEvent(BoonTrigger.CurrencyCollected, actor.Id, 0, action, actor.Position) with { Amount = gold });
        int food = quote.Items.GetValueOrDefault("food");
        if (food > 0) Emit("ProvisionsCollected", actor.Id, 0, action, actor.Position, food);
    }
}
