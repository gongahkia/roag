using Roag.Core.Content;
using Roag.Core.Expeditions;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed record NativeInteractionQuote(bool Valid, string Reason, string Action, IReadOnlyDictionary<string, int> Costs);

public sealed partial class WorldSession
{
    private string _nativeSocialReply = "";

    public NativeInteractionQuote QuoteNativeInteraction(long memberId, long personId, string interactionId)
    {
        NativeInteractionQuote Invalid(string reason) => new(false, reason, "", new Dictionary<string, int>());
        var member = Party.FirstOrDefault(a => a.Id == memberId && a.Life == LifeState.Alive);
        var person = State.Actors.FirstOrDefault(a => a.Id == personId);
        var interaction = Interactions(personId).FirstOrDefault(i => i.Id == interactionId);
        if (!HasNativeParty || State.AwaitingContinuation || member?.Tactics?.Build == null || interaction == null || person == null
            || person.Position.Distance(member.Position) > interaction.Distance || !VisionFrom(member.Position).Contains(person.Position))
            return Invalid("Speak to an observed person within this member's physical reach.");
        if (interaction.Action == "recruit") return Invalid("Fixed-class adventurers join through their recruitment page.");
        if (interaction.Action is not ("converse" or "rumour" or "heal" or "share-meal" or "trade"))
            return Invalid("This interaction has no native action.");
        var costs = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (item, count) in interaction.Cost)
        {
            string stack = Content.Get<ItemDefinition>(item).Stack;
            costs[stack] = checked(costs.GetValueOrDefault(stack) + count);
        }
        if (costs.Any(p => Count(p.Key) < p.Value)) return Invalid("You cannot afford this interaction.");
        if (interaction.Action == "heal" && member.Health >= member.MaxHealth && member.Body.Wounds == 0)
            return Invalid("This conscious member needs no treatment.");
        if (interaction.Action == "rumour" && !State.Discoveries.Any(d => d.TownId == person.HomeTown))
            return Invalid("No local places are recorded here.");
        return new(true, "", interaction.Action, costs);
    }

    private ActionResult PerformNativeInteraction(long personId, string interactionId)
    {
        var interaction = Interactions(personId).FirstOrDefault(i => i.Id == interactionId);
        if (interaction?.Action == "recruit") return SubmitDungeon(new(TacticalAction.Recruit, Target: personId));
        var quote = QuoteNativeInteraction(DungeonMember.Id, personId, interactionId);
        if (!quote.Valid) return new(false, quote.Reason);
        // Opening a price list is free; only an actual exchange spends recovery.
        if (quote.Action == "trade") return new(true, "Choose supplies to trade.");
        var result = SubmitDungeon(new(TacticalAction.SocialInteraction, Target: personId, Item: interactionId));
        return result.Success ? new(true, _nativeSocialReply) : result;
    }

    internal void ApplyNativeInteraction(Actor member, Actor person, InteractionDefinition interaction)
    {
        var quote = QuoteNativeInteraction(member.Id, person.Id, interaction.Id);
        if (!quote.Valid) throw new InvalidOperationException("Native interaction changed after preview: " + quote.Reason);
        foreach (var (stack, count) in quote.Costs)
        {
            Add(stack, -count);
            if (stack != "gold") State.ResourcesConsumed += count;
        }
        switch (interaction.Action)
        {
            case "rumour":
                _nativeSocialReply = LocalRumour(person, member.Position).Message;
                return;
            case "converse":
                if (Remember(person, "conversation", "We exchanged stories with " + member.Name + ".", 3, member.Id))
                { ChangeTrust(person, member, 3); ChangeTrust(member, person, 3); }
                _nativeSocialReply = $"{person.Name}, {person.Trait} {person.Background}, hopes to {person.Motivation}. Their friend is {State.Actors.FirstOrDefault(a => a.Id == person.FriendId)?.Name ?? "far away"}.";
                break;
            case "heal":
                member.Body.Wounds = Math.Max(0, member.Body.Wounds - 1);
                Remember(member, "treated", person.Name + " tended my injuries.", 6, person.Id);
                ChangeTrust(member, person, 5);
                _nativeSocialReply = person.Name + " treated " + member.Name + ".";
                break;
            case "share-meal":
                bool needed = person.Body.Hunger >= Simulation.MealAt || State.Towns[person.HomeTown].Shortage > 0;
                if (Remember(member, "shared-meal", "A meal with " + person.Name + ".", interaction.Amount, person.Id))
                    ChangeTrust(member, person, 5);
                person.Body.Hunger = Math.Max(0, person.Body.Hunger - Simulation.MealReduction);
                person.Stress = Math.Max(0, person.Stress - interaction.Amount);
                if (needed && quote.Costs.GetValueOrDefault("food") > 0)
                    ApplyConsequence(person, member, ConsequenceKind.SuppliedFood, "meal:" + interaction.Id + ":" + State.Tick);
                _nativeSocialReply = "A meal shared with " + person.Name + ".";
                break;
            default: throw new InvalidOperationException("Only a paid native social action reaches this transaction.");
        }
        Log(_nativeSocialReply);
    }
}
