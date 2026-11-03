using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewSocialInteraction(Actor actor, TacticalIntent intent)
    {
        if (!Friendly(actor)) return Invalid("Only a party member can choose this interaction.");
        if (IsLocallyEngaged(actor.Id)) return Invalid("Leave immediate danger before speaking.");
        var quote = _world.QuoteNativeInteraction(actor.Id, intent.Target, intent.Item);
        if (!quote.Valid) return Invalid(quote.Reason);
        if (quote.Action == "trade") return Invalid("Browse trade freely, then confirm a paid exchange.");
        int cost = quote.Action == "share-meal" ? Tuning.EatCost : Tuning.WaitCost;
        return new(true, "", ActionTiming.Recovery(cost, EffectiveSpeed(actor)), [Actor(intent.Target).Position]);
    }

    private void CommitSocialInteraction(Actor member, TacticalIntent intent, long action)
    {
        var interaction = _world.Content.Get<InteractionDefinition>(intent.Item);
        var quote = _world.QuoteNativeInteraction(member.Id, intent.Target, intent.Item);
        var person = Actor(intent.Target);
        _world.ApplyNativeInteraction(member, person, interaction);
        Emit("SocialInteractionCompleted", member.Id, person.Id, action, member.Position, ability: interaction.Id);
        int gold = quote.Costs.GetValueOrDefault("gold");
        if (gold > 0) Emit("CurrencySpent", member.Id, person.Id, action, member.Position, gold);
        if (interaction.Action == "heal")
            HealCampCompanion(person, member, interaction.Amount, action, interaction.Id);
        // A fed traveller may still offer an NPC a meal and enjoy the social
        // benefit, but cannot use that gift to trigger personal eating boons.
        if (interaction.Action == "share-meal" && member.Body.Hunger > 0)
            RestoreFoodHunger(member, action, _world.Simulation.MealReduction, interaction.Id);
    }
}
