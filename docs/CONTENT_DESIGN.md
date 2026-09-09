# Content and interaction reference

This reference describes implemented contracts. Future expansion requirements
remain in `IMPLEMENTATION_PASS_3.md` until their code and validation land.

## Inert content input

Content is UTF-8 JSON, decoded through `json_data.loads`. Duplicate object keys,
non-finite constants and overflowing float exponents are rejected before content
validation. Code strings are never evaluated. Catalog, art and metadata roots
must be objects with registered fields. Catalog records and action effect
records reject unknown fields. Nested contracts are being tightened in separate
steps; this is not a claim of complete typed-content conversion yet.

Identity uses stable IDs. Existing unprefixed IDs are retained as legacy IDs.
The accepted grammar is lowercase alphanumeric/underscore segments beginning
with a letter, optionally separated by `:` for namespaces. Display names are
not identity. New expansion IDs should use a namespace; renaming old IDs needs
an explicit migration or alias, never reassignment of their meaning.

Action opcodes remain a registered Python vocabulary. Action magnitudes are
integers in -1,000,000..1,000,000. Damage, block, healing, draw, discard, energy,
guard and status applications cannot have negative magnitudes; signed movement,
stress and expedition resource changes retain their existing interpretation.
Booleans are not ranks or magnitudes. This is an input bound on authored base
effects, **not** a cap on accumulated combat damage, stacks or loop arithmetic.
Legacy persistent scaling fractions are finite numeric input pending conversion
to the explicit stack contracts.

## Structural census

The bundled catalog is validated and cached once per process. Definitions,
nested lists, effects, balance and art reject normal mutation. Runtime techniques,
enemies, enemy actions and effects have named record types; `Opcode` and `Target`
are closed enums. The records retain JSON-compatible lookups to avoid rewriting
unrelated simulation code. Other validated definitions use a read-only definition
record. There is no content script evaluator. Test-only balance variants explicitly
replace a catalog rather than mutate the shared cache. Explicit alternate file
loads are validated afresh and do not reuse a stale path cache.

Enemy actions reject unknown fields, duplicate names, and nonpositive selection
weights. Action names currently identify frozen intents within an enemy; changing
one therefore requires a save compatibility decision.

`python3 -m dumbest_dungeon.content_audit` reports all owner pools and rank access,
tag and opcode counts, native encounter density, action counts and structural
groups. The normalizer removes numerical magnitude but keeps its sign, effect
order, energy, rank access, targeting and conditions. Similarity is a review
signal. It neither rejects useful shared glue automatically nor labels scalar
variants as new mechanics.

The starting census is retained at `docs/evidence/pass3/starting-census.json`.
The first expansion must preserve 25 crew and deepen those owners' interactions.

## Synergy and eligible pools

`python3 -m dumbest_dungeon.synergy` emits typed, canonical edges using `produces`,
`exploits`, `spends`, `converts`, `requires` and `covers`. Existing action opcodes
derive the applicable relationships; no converter is invented where no conversion
exists. Edges retain owner, legal ranks, base/upgrade layer and crew/enemy/shared
scope. Conditions inspect the card's primary targets even when the conditional
reward itself targets the owner. A bonus is an exploit; an effect that cannot
execute without setup is a requirement. Tags unsupported by either card layer
are rejected during content loading, including falsely advertised payoffs.

Diagnostics report unsupported payoffs, orphan producers without an innate use,
thin producer pools, single-owner setup dependencies, rank bottlenecks and
normalized structural groups. Enemy-applied conditions are labeled encounter
dependent instead of falsely requiring the crew to generate every injury.
The initial warning threshold is two distinct producer definitions and 5% of
the eligible technique pool. It is a review threshold, not a claim that two
producers guarantee a build. Reports cover the global catalog and every curated
squad's owner-filtered pool. Upcoming acquisition lanes must run the same analysis
on their actual eligible subsets. Expansion must not satisfy the threshold by
adding scalar clones.

Rank reports show expected invalid cards in a uniformly drawn five-card starter
hand and the exact hypergeometric chance that all five are rank invalid, in basis
points. They do not model stunned owners, draw order, retention or tactical
movement. Single-owner warnings disclose loss of a setup source, not automatic
loss of every card's fallback effect. Persistent effects, biome-specific enemy
availability and actual payoff activation remain separate work; this graph does
not yet claim complete engine discovery.

## Decision recording

The run ledger records departure formation/deck, generated technique and boon
offers, picks, every unpicked technique, played cards with owner/rank/energy,
upgrades, transformations with their offered alternatives and lost upgrade,
removals, item stack acquisitions, boons, curses and owner deaths. Lost cards
are counted once from the permanent deck rather than again in every combat pile.
Objective-driven card edits and curse treatment use the same records. Invalid
card plays do not produce a play record.

`telemetry.decision_counts` reports offer, pick, skip, play and edit counts
separately. An offered or acquired card is not automatically counted as played.
These are local deterministic records, not a popularity or win-rate conclusion.
Detailed arithmetic, encounter records, the normal history screen and optional
NDJSON export are subsequent instrumentation work.

Combat records now include encounter composition/plan, start/end rounds, starting
hands and rank/owner clogging, unused energy, resolved effects, conditional payoff
activation, and damage/healing/status arithmetic. Damage separates requested hit,
absorbed block, post-mitigation amount, HP loss and overkill. Healing separates
effective recovery and overheal. Source IDs distinguish played techniques, enemy
actions, wounds and ripostes. Death's Door records the actual existing roll in
exact hexadecimal float notation and the displayed chance in basis points;
recording introduces no additional random call. Status records retain previous,
requested and resulting duration. Expired block is recorded on phase reset.

Some legacy environment/passive paths still use `world:unattributed`; source
coverage is intentionally visible rather than falsely assigning a card. The
interaction-kernel migration must finish those paths and add trigger ancestry.
These records do not impose new combat limits or change effect arithmetic.

Travel records retain actual tile, weighted ticks and light spent. Objective
records retain offered/chosen approaches, costs, stages and outcomes; facility
records retain choices and skips. Bargain records retain generated offers and
the accepted or declined choice. Route inspection records no world action.
Exploration effects inherit their objective, facility or event source ID.

## Automatic-trigger contract

`triggers.py` defines stable phases: replace/prevent, before, primary, after,
death, cleanup. Trigger declarations name their input/output event types,
priority, proc family and optional explicit limiter. Registered limiters are
once per root, initiating card, turn or combat; finite charges; finite generated
retriggers; and ancestry-based exclusion of the trigger's own proc family.
Capacities are authored positive integers. A family exclusion naming an unrelated
family cannot be presented as a termination proof.

Validation builds the dependency graph, removes finite-limited nodes and rejects
every remaining cyclic strongly connected component. Merely placing one limited
trigger somewhere in a larger component is insufficient: a different unlimited
cycle inside that component is rejected. The graph walk is iterative, including
the 3,000-node chain regression. Acyclic triggers may be unlimited.

The queue must preserve the initiating card/turn/combat identities across automatic
descendants and must not let automatic handlers replenish limiter counters. These
are contracts for the next runtime change; declaring them alone does not replace
the existing direct effect resolver or implement `CHAIN SEALED` yet.

## Acquisition, pack and threat boundaries

The typed acquisition vocabulary is normal, elite, objective, facility, bargain,
guardian, finale and loop. Technique eligibility intersects living owners, the
requested lane and enabled pack members; stable sorted IDs are returned.
Optional authored `lanes` reject unknown or duplicate values. Legacy techniques
remain available in all lanes until the reward refactor authors narrower jobs;
the existing generator has not yet switched to the new eligibility function.

Content packs contain typed section/ID references, explicit requirements and
exclusions. Unknown members, duplicate IDs, disabled requirements and conflicting
enabled packs are rejected. Enabling a pack does not silently enable its required
packs. The current shipped manifest still contains only `base:core`; these
contracts precede authored optional packs.

`Threat` stores nonnegative integer dimensions for durability, sustained damage,
burst, control, sustain, reach and tempo. A `ThreatBudget` requires both its total
and every dimensional ceiling to pass. Cheap durability cannot excuse a burst
ceiling violation. These contracts are tested independently; deriving calibrated
two/three-phase estimates and replacing existing HP-only generation budgets
remains the encounter milestone, not an already-completed balance change.
