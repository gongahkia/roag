# Renderer-independent effect model

`src/medieval/effects.ts` defines the v1 pure effect-resolution boundary. It is a semantic foundation for later owned systems, not gameplay: it does not mutate a world, advance time, draw randomness, resolve combat, heal or injure anyone, recover a courier, revive a person, damage Jomon, or write a save.

## Inputs, ownership, and safety

The caller supplies one bounded `EffectResolutionRequest`: canonical action-world minutes, a revisioned set of facts already known to that caller, effect candidates, and selected counterplay IDs. The request has no `FoundationWorld`, manifest, renderer, persistence, browser, callback, worker, cache, or wall-clock field. It does not inspect another system's state or create facts.

Every candidate has content-safety metadata that is validated by the existing `content-safety.ts` owner. All sources, targets, evidence, and counterplay refer to a supplied known fact at its exact revision. An unknown, stale, or ineligible reference rejects the entire request; output only repeats supplied source, target, evidence, and counterplay IDs. This keeps future consumers from turning an effect into hidden-frontier information, universal persuasion, or control of a person.

| Semantic source category | Required known-fact source | Grounding rule |
| --- | --- | --- |
| Health, injury, exhaustion | Condition | Ordinary condition only |
| Preparation | Preparation | Material object or place-bound practice |
| Equipment | Equipment | Material object |
| Relic, totem | Grounded object | Material object |
| Boon, curse | Grounded object or practice | Material object or place-bound practice |
| Crew support | Crew-support fact | Crew relationship |
| Enemy weakness | Known enemy | Ordinary condition |
| Environmental interaction | Known environment | Environmental condition |

This is a closed semantic vocabulary. It has no mage, spell, arbitrary target, public content, extension hook, or user-authored source. Relics, totems, boons, and curses are only explicitly grounded references here; the companion [mystical effect policy and audit](mystical-effect-policy.md) constrains their future rarity, source, cost, condition, availability, and audit evidence without adding authored mystical content.

The model inherits the project-wide exclusion of sexual violence, slavery, torture, and harm/endangerment of children. It creates no child participant category or player-facing content.

## Canonical resolution

Candidate, known-fact, and selected-counterplay top-level collections are caller sets, so their insertion order has no meaning. Resolution uses a locale-independent ordinal comparison and produces one canonical result. Nested effect descriptor lists—evidence, conflict IDs, and chain prerequisites—are meaning-bearing data and must already be unique, ordinally sorted, and valid; noncanonical descriptors fail closed.

Finite duration is inclusive at `startsAtWorldTime` and exclusive at `expiresAtWorldTime`. The only clock is the request's canonical `minute` action-world time. A pure projection at the same time is repeatable and does not advance or expire anything; wall-clock/date/timestamp-shaped action-time input is rejected.

The resolution order is:

1. Suppress candidates that have not started, are expired, or are met by a selected known counterplay response.
2. Resolve explicit reciprocal conflicts by higher priority, then ordinal effect ID; record the losing effect and winner ID.
3. Process remaining candidates by chain depth, descending priority, then ordinal ID. A suppressed prerequisite suppresses its child. Cycles and chains deeper than four prerequisites are rejected.
4. Apply an exact shared stack policy. `additive` retains the first canonical contributors through its cap; `exclusive` has cap one. A group must agree on target, polarity, mode, and cap. The output group sums only retained semantic magnitudes; that number is not damage, healing, chance, persuasion, or a state mutation.

The v1 bounds are 32 known facts, 24 effects/counterplay selections, four evidence references and stack contributors per effect, eight conflict references, four chain prerequisites, depth four, magnitude three, priority nine, and a maximum 1,440-minute duration. Duplicates, malformed data, unknown categories, unsafe classifications, invalid grounding, stale facts, ineligible source/target/counterplay, conflicting stack rules, unknown counterplay, and cyclic/noncanonical data reject rather than being repaired.

Each result exposes active and suppressed effect IDs, semantic category/polarity/magnitude, source/target/evidence IDs, counterplay ID, chain depth, group totals, and a per-effect explanation. It has no renderer layout, colour, glyph, text authority, spatial coordinate, or hidden fact.

## Compatibility and non-goals

`effects.ts` owns only this v1 pure resolution contract. `temporal.ts` remains the owner of action-time advancement, `world-state.ts` and `world.ts` remain the sole mutable-world owners, `causal-history.ts` remains replay authority, and terminal/detailed-renderer contracts remain presentation owners. The effect model is intentionally not registered in the fixed internal-content catalogue: it introduces no authored effect content.

This slice changes no `FoundationWorld`, world/manifest/replay schema, save envelope, IndexedDB layout v4, derived index, valid v3 envelope loading, or explicit-save-only metadata. It adds no migration, compatibility shim, persistent cache, worker, packed data, spatial system, random stream, browser feature, gameplay action, or public mod/content-pack surface.

Before a later owner stores or uses an effect candidate, it must make that owner's explicit provenance, replay, safety, and schema/version decision. The companion policy audit is not that implementation decision. Neither contract authorizes combat, recovery, death/revival, Jomon loss, social control, or any other simulation behavior.

The companion [courier loss policy assessment](courier-loss-policy.md) consumes only the mystical policy's explicit ultra-rare courier-safeguard reservation as unavailable future provenance. It neither resolves an effect nor authorizes prevention, revival, succession, or chronicle finalization.

The companion [Jomon integrity policy assessment](jomon-integrity-policy.md) likewise consumes only the unavailable ultra-rare Jomon-loss reservation. It never resolves an effect or authorizes repair, rescue, collapse prevention, or terminal-loss override.
