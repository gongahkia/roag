# Mystical effect policy and audit

`src/medieval/mystical-effect-policy.ts` is the v1 pure, compiled-TypeScript policy boundary for a future owner that proposes a mystical effect definition. It audits definitions only: it does not generate or discover content, select a rarity result, draw RNG, grant inventory, spend a cost, advance time, resolve an effect, mutate `FoundationWorld`, or write persistence.

## Low-mysticism ladder and availability

The closed rarity order is **scarce**, **rare**, **exceptional**, then **ultra-rare**. There is intentionally no common mystical tier. A definition declares an inspectable per-world upper bound; it never represents an existing world count or a spawning rule.

| Rarity | Maximum discoveries per world | Maximum concurrently eligible instances |
| --- | ---: | ---: |
| Scarce | 6 | 2 |
| Rare | 3 | 1 |
| Exceptional | 1 | 1 |
| Ultra-rare | 1 | 1 |

These are policy ceilings, not a claim that any mystical content currently exists. Future generation, discovery, provenance, RNG-stream, manifest, replay, and schema decisions remain with their respective owners.

## Grounded sources and effect-model binding

Every definition names a relic, totem, boon, or curse form and embeds one existing `effects.ts` semantic candidate. The audit delegates candidate category, known-fact revision, finite duration, source grounding, content safety, stacking, chain, and counterplay validation to `validateEffectResolutionRequest()`. It does not resolve the candidate or replace any `effects.ts` ordering authority.

| Grounded source class | Permitted form/category | Required known evidence |
| --- | --- | --- |
| Material object | Relic, totem, boon, or curse / matching semantic category | Grounded-object source and an object-available condition |
| Specific place | Boon or curse / boon or curse | Place-bound-practice source plus a known environment condition |
| Bounded practice | Boon or curse / boon or curse | Place-bound-practice source and observed-practice condition |
| Relationship condition | Boon / existing crew-support category | Known crew-support source and established relationship condition |

This keeps a relationship-bound boon tied to an actual known crew relationship, rather than universal persuasion or control. A definition never receives a world, hidden frontier, player-knowledge bypass, renderer, callback, browser, persistence, or network input.

## Conditions, costs, audits, and counterplay

A valid definition has a finite action-world-duration or limited-use boundary, at least one known-fact condition, an explicit finite per-world availability declaration, and one to three in-world cost/trade-off references. It does not execute those costs; a future owning system must decide their material state and replay behavior.

Audit evidence is a bounded, ordinally ordered list of known-fact references. It must cover source provenance, every eligibility condition, every cost/trade-off, and the availability boundary. Hindrances must additionally cite their existing known counterplay response. The accepted audit exposes only canonical policy, source, condition, cost, and evidence identifiers; it has status `policy-audited-only` and grants no gameplay capability.

Malformed, unknown, common, ungrounded, stale, free, unlimited, noncanonical, duplicate, contradictory, unsafe, or unauditable definitions reject with typed stable diagnostics. Definitions are application-owned compiled data only, not a public format, registry, loader, or compatibility commitment.

## Reservations and exclusions

Ultra-rare definitions may carry only a `deferred-no-authority` reservation for a future courier-loss or Jomon-loss safeguard. A reservation has no effect result and cannot implement or authorize death prevention, revival, Jomon rescue, collapse prevention, or an override of terminal loss. Those rules remain later roadmap tasks.

The policy explicitly excludes generic mage classes, spell lists, arbitrary casting, unlimited supernatural power, coercive social control, courier revival/death prevention, and Jomon-saving behavior. It preserves the project-wide metadata-first rejection of sexual violence, slavery, torture, and harm/endangerment of children; it creates no child participant or player-facing mystical catalogue.

## Compatibility and non-goals

This contract is not added to the fixed internal-content catalogue because it supplies no authored mystical definitions. It changes no current authored value, seed/RNG stream, generation output, frontier commitment, `FoundationWorld`, world/manifest/replay/causal-history schema, save envelope, IndexedDB v4 layout, or valid-v3 loading behavior. It adds no migration, shim, worker, cache, packing, UI, renderer, combat, recovery, death, Jomon-integrity, or physical-world implementation.
