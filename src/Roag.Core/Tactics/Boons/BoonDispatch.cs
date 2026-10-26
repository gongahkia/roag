using System.Collections.Immutable;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record BoonActivation(long Id, string BoonId, long OwnerId, string SourceFaction,
    WorldPosition SourcePosition, long ChainId, long? RootActionId, long ParentEventId,
    ImmutableArray<BoonActivationKey> Ancestry, long Power, BoonEvent Event, long? PairTargetId = null);

/// <summary>Persistable causal identities/history. The engine owns the RNG and drains effect work separately.</summary>
public sealed class BoonDispatchState
{
    public long NextActivationId { get; set; } = 1;
    public Dictionary<long, BoonChainHistory> Chains { get; init; } = [];

    public void Prune(IEnumerable<long> retainedChains)
    {
        var retained = retainedChains.ToHashSet();
        foreach (long id in Chains.Keys.Where(id => !retained.Contains(id)).ToArray()) Chains.Remove(id);
    }
}

public static class BoonDispatch
{
    /// <summary>Emit activations and enqueue effects in source order. Callbacks must not resolve queued effects recursively.</summary>
    public static void OnEvent(BoonDispatchState state, BoonEvaluationContext world, BoonEvent e,
        RoagRandom random, Action<BoonActivation> emit, Action<BoonActivation, BoonEffect> enqueue,
        BoonCatalog? catalog = null)
    {
        catalog ??= BoonCatalog.Default;
        e.Validate(catalog);
        if (state.NextActivationId <= 0) throw new InvalidDataException("Invalid next boon activation identity.");
        if (e.FateMiss && e.Kind == BoonTrigger.AttackPerformed || e.OathSharedOnly && e.Kind != BoonTrigger.EntityKilled) return;
        var subscriptions = catalog.Subscribers(e.Kind);
        if (subscriptions.IsEmpty) return;
        if (!state.Chains.TryGetValue(e.ChainId, out var history)) state.Chains.Add(e.ChainId, history = new());
        foreach (var owner in world.Actors)
        {
            if (owner.Actor.Tactics?.Boons.Inventory is not { } inventory || inventory.Stacks.Count == 0) continue;
            foreach (var definition in subscriptions)
            {
                if (!inventory.Stacks.TryGetValue(definition.Id, out var stacks)) continue;
                var key = new BoonActivationKey(owner.Actor.Id, definition.Id);
                if (e.Ancestry.Contains(key) || !BoonConditions.Observes(owner, definition.Scope, e)
                    || !BoonConditions.Evaluate(definition.Predicate, world, owner, definition.Id, e, history)) continue;
                var neighborhood = PairNeighborhood(definition.Predicate);
                var pairs = neighborhood is { } area ? history.Candidates(owner, definition.Id, e, area) : null;
                var magnitude = BoonInventory.Aggregate(definition, stacks);
                // Roll once before pair multiplicity, even when a consumed predicate leaves no candidate.
                long count = definition.StackPolicy == BoonStackPolicy.ChanceOverflow
                    ? BoonChance.Roll(random, magnitude.Chance, e.ProcCoefficient) : 1;
                for (int pairIndex = 0; pairIndex < (pairs?.Count ?? 1); pairIndex++)
                {
                    var pair = pairs?[pairIndex];
                    if (pair != null) history.Pairs.Add(pair.Key); // A failed roll still consumes the unordered pair.
                    for (long i = 0; i < count; i++)
                    {
                        long id = state.NextActivationId;
                        state.NextActivationId = checked(id + 1);
                        var activation = new BoonActivation(id, definition.Id, owner.Actor.Id, owner.Faction,
                            owner.Actor.Position, e.ChainId, e.RootActionId, e.Id, e.Ancestry.Add(key), magnitude.Power, e, pair?.Prior.Target);
                        emit(activation);
                        foreach (var effect in definition.Effects)
                            if (BoonConditions.Evaluate(effect.When, world, owner, definition.Id, e, history)) enqueue(activation, effect);
                    }
                }
            }
        }
        history.RecordDamage(e); // Current damage cannot pair with itself; all owners observe the same prior history.
    }

    private static BoonNeighborhood? PairNeighborhood(BoonPredicate? node)
    {
        if (node is BoonCondition { Op: BoonPredicateKind.PairAdjacentDamage } c) return c.Neighborhood;
        if (node is NotBoonPredicate not) return PairNeighborhood(not.Child);
        var children = node switch { AllBoonPredicates all => all.Children, AnyBoonPredicates any => any.Children, _ => [] };
        foreach (var child in children) if (PairNeighborhood(child) is { } area) return area;
        return null;
    }
}
