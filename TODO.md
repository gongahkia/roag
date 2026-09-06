# Jomon — Operational Tracker

## Current State

- The medieval foundation is retained: deterministic creation, action-driven time, persistent household records, physical Jomon, local persistence, deck interaction, bounded cargo, a named Hearthford market/worksite consequence, and contextual courier loss are implemented.
- The v19 Mill Lease slice is complete: an accepted ironwork case can be delivered to Jomon, then fitted to relieve the lease or retained for lease credit. Both browser paths persist.
- This is not yet a playable expedition. There is no complete settlement-and-wilderness loop, tactical encounter, or contextual return/defeat loop.

## Now

Build one complete playable expedition, without a general-purpose expedition framework:

1. Prepare one courier aboard Jomon.
2. Choose a small loadout and one crew-support preparation.
3. Leave through the gangplank.
4. Explore Hearthford and its surrounding wilderness.
5. Meet one persistent named contact with one material problem.
6. Accept, refuse, or alter the objective.
7. Show pressure from elapsed time, depth, noise, and valuables.
8. Encounter a turn-based tactical threat.
9. Support combat and one genuinely different non-combat resolution.
10. Include one useful environmental interaction.
11. Earn, deliver, spend, or lose a useful material resource.
12. Return to Jomon, or resolve defeat through injury/escape, material loss, or permanent death and succession.
13. Persist a visible household, contact, cargo, market, or location consequence.

One bounded `ExpeditionState` is permitted only for state exercised by this slice. Do not introduce an encounter DSL, narrative engine, generic economy, world-partition engine, or universal expedition architecture.

The project owner completes and evaluates three full expeditions before scope expands.

## Next

- Evaluate those three owner-completed expeditions and revise only demonstrated friction, missing choices, or unclear consequences in the bounded loop.
- Add capture or rescue only if the played loop makes a specific contextual need clear.

## Later

- Grow locations, contacts, equipment combinations, threats, and return consequences only from evidence gathered in the first expedition and its evaluations.
- Reconsider wider simulation, frontier, management, and era work only after the owner gate is met.

## Frozen

- expanding-frontier development and advanced generation expansion;
- full-person, distant-person, fidelity, and catch-up simulation expansion;
- delegation, autonomy, and social-memory generalisation;
- era, NG+, and NG++ work;
- detailed-renderer work and management-sidebar expansion;
- broad content-family frameworks;
- further provenance/schema sophistication; and
- speculative large-world optimisation.

Frozen systems remain in the repository and must keep their existing behaviour; they receive no new scope until the first expedition has been personally played and evaluated.

## Done

- Medieval foundation slices and their detailed records are retained in [`docs/archive/medieval-foundation-2026-09/`](docs/archive/medieval-foundation-2026-09/).
- Live implementation contracts remain in `docs/` while production code relies on them.
