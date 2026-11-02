using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewTrade(Actor actor, WorldCommand? command)
    {
        if (!Friendly(actor) || actor.Tactics?.Build == null) return Invalid("Only a party member can trade shared supplies.");
        if (IsLocallyEngaged(actor.Id)) return Invalid("Leave immediate danger before trading.");
        var quote = _world.QuoteTrade(actor.Id, command);
        return quote.Valid ? new(true, "", ActionTiming.Recovery(Tuning.WaitCost, EffectiveSpeed(actor)), []) : Invalid(quote.Reason);
    }

    private void CommitTrade(Actor actor, WorldCommand command, long action)
    {
        var quote = _world.ApplyNativeTrade(actor, command);
        Emit("TradeCompleted", actor.Id, 0, action, actor.Position, command.Amount, ability: command.Kind + ":" + command.Item);
        if (quote.GoldChange < 0) Emit("CurrencySpent", actor.Id, 0, action, actor.Position, -quote.GoldChange);
        else
        {
            var snapshot = CaptureBoonEvent(BoonTrigger.CurrencyCollected, actor.Id, 0, action, actor.Position) with { Amount = quote.GoldChange };
            Emit("CurrencyCollected", actor.Id, 0, action, actor.Position, quote.GoldChange, snapshot: snapshot);
        }
    }
}
