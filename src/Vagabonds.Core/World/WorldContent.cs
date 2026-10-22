using Vagabonds.Core.Content;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed partial class WorldSession
{
    public ContentCatalog Content { get; }
    public ClassDefinition Class(Actor actor) => Content.Classes.Single(c => c.Role == actor.Role);
    public IReadOnlyList<InteractionDefinition> Interactions(long actorId)
    {
        var actor = State.Actors.FirstOrDefault(a => a.Id == actorId && a.Kind == ActorKind.Person && !a.Recruited && a.Life == LifeState.Alive);
        if (actor == null || !Content.Entries.TryGetValue(actor.Archetype, out var definition) || definition is not CharacterDefinition character) return [];
        return character.Interactions.Select(Content.Get<InteractionDefinition>).ToArray();
    }
    public ActionResult PerformInteraction(long actorId, string interactionId)
    {
        if (IsInCombat || State.AwaitingContinuation || DeathPending) return new(false, "Finish the encounter or expedition first.");
        var interaction = Interactions(actorId).FirstOrDefault(i => i.Id == interactionId);
        var actor = State.Actors.FirstOrDefault(a => a.Id == actorId);
        if (interaction == null || actor == null || actor.Position.Distance(State.Position) > interaction.Distance) return new(false, "Approach someone who offers this interaction.");
        var costs = interaction.Cost.Select(p => (Stack: Content.Get<ItemDefinition>(p.Key).Stack, p.Value)).ToArray();
        if (costs.Any(p => Count(p.Stack) < p.Value)) return new(false, "You cannot afford this interaction.");
        var injured = Party.Where(a => a.Life == LifeState.Alive && a.Health < a.MaxHealth).MinBy(a => a.Health / (double)a.MaxHealth);
        if (interaction.Action == "heal" && injured == null) return new(false, "No conscious traveller needs treatment.");
        if (interaction.Action == "recruit" && State.Party.Count >= 4) return new(false, "Four travellers already fill the active party.");
        // Every check precedes costs or effects. Interactions never consume RNG.
        if (interaction.Action == "recruit")
        {
            var result = Submit(new(WorldCommandKind.Recruit, ActorId: actorId));
            if (!result.Success) return result;
        }
        foreach (var (stack, count) in costs) Add(stack, -count);
        switch (interaction.Action)
        {
            case "heal": injured!.Health = Math.Min(injured.MaxHealth, injured.Health + interaction.Amount); return new(true, actor.Name + " treated " + injured.Name + ".");
            case "share-meal":
                foreach (var friend in Party) friend.Stress = Math.Max(0, friend.Stress - interaction.Amount);
                actor.Stress = Math.Max(0, actor.Stress - interaction.Amount); return new(true, "A meal shared with " + actor.Name + ".");
            case "converse": return new(true, $"{actor.Name}, {actor.Trait} {actor.Background}, hopes to {actor.Motivation}. Their friend is {State.Actors.FirstOrDefault(a => a.Id == actor.FriendId)?.Name ?? "far away"}.");
            case "trade": return new(true, "Choose supplies to trade.");
            default: return new(true, actor.Name + " joined your expedition.");
        }
    }
}
