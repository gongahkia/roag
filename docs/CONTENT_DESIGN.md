# Content and interaction reference

This reference describes implemented contracts. Future expansion requirements
remain in `IMPLEMENTATION_PASS_3.md` until their code and validation land.

## Expedition Pressure contract

Expedition Pressure is the irreversible global escalation axis; Light remains
the depletable local visibility, ambush, stress and reward resource. The public
bands are QUIET (0), WATCHFUL (240), HUNTED (480), LOCKDOWN (760) and OVERRUN
(1100). A status preview returns the exact value, current floor, progress, next
threshold, remaining amount and the next band's plain-language unlock forecast.

The closed source vocabulary is weighted travel, completed enemy round,
objective stage, facility action, explicitly priced event/bargain/power reward,
and loop entry. Base prices are integer rules: one per weighted travel tick,
eight per completed enemy phase, 18 per objective stage and 24 per major facility
action. Event, bargain and exceptional-reward prices default to zero and must opt
in explicitly. No menu, help, inspection, animation, resize, input delay or wall
clock appears in the vocabulary. Save schema 35 serializes the value, recent
causes and any migration gap; the four priced base actions advance it exactly.

Each visible band has one typed director profile. Coordination, patrol cadence
and aggression, reward choices, mutation/reinforcement/hazard slots, and bounded
health/damage basis points advance independently. Health is capped to a 10%
band envelope and damage to 5%; qualitative slots advance first. Only serialized
Pressure selects this profile, never current HP, deck strength, or recent results.
Save schema 35 freezes encounter-entry Pressure before the opening intent is
chosen. Pressure earned during that combat can only select a later encounter's
profile. Migration preserves an already active pre-director combat at QUIET.
Archived content-schema-20 rules remain on their recorded QUIET director so a
new engine can continue those saves without changing combat, hazard, patrol or
reward semantics; Pressure is still recorded and disclosed.

### Mutation contracts

Content schema 22 defines 16 visible tactical modules. Each has a stable
namespaced ID, concise marker, unique registered effect, amount, minimum Pressure
band, deterministic priority, encounter-kind and biome compatibility, and
explicit exclusions. The vocabulary covers opening protection/targeting,
movement, finite reactions, ally-death responses, round-end sustain/control,
partial stun resistance and one disclosed reinforcement. Arbitrary callbacks
and stat-only affixes are not mutation effects. Embedded schema-21 rules load an
empty mutation set rather than borrowing definitions from the installed game.

Seven opening modules apply at encounter entry: front block, rear guard,
weakest-crew mark, rear dodge, front riposte, striker focus and an opening
crew-rank shove. The
selector enumerates compatible definitions in priority/ID order, applies
biome/kind/band and formation-size constraints, excludes incompatible pairs,
penalizes modules already seen in the run, and makes one weighted choice per
available slot. A named SHA-256-derived stream isolates those choices from the
serialized combat RNG. Chosen IDs and their markers are frozen and visible
before the first intent. The other nine modules resolve through finite typed
reactions, stable enemy-phase hooks, partial status resistance or a frozen
reinforcement reserve; all 16 definitions are selectable only where their live
handler and compatibility constraints apply.

## Inert content input

### Bounded mastery definitions

Content schema 24 defines exactly two mastery candidates for each of the 25
existing owners. Each candidate exposes two irreversible branch IDs: `engine`
intensifies one authored effect operand, while `coverage` expands legal origin
ranks by one adjacent rank. The loader validates the referenced technique,
signature/rare role, branch names, mode/operand agreement, effect index and
bounded integer amount. An upgraded eligible copy may take one irreversible
branch at a workshop. Its branch is serialized on the durable copy and queued
card continuation, recorded with copy identity in run history, and shown on card
labels, full inspection and the choice preview. Transformation preserves the
copy ID but explicitly clears upgrade and mastery.

### Card infusion definitions

Content schema 25 defines 16 single-copy modifiers. Their closed modes cover
retain, exhaust, origin-rank access, four conditional costs, opening priority,
one bounded echo, follow-through draw, movement-to-energy conversion, cleanse,
front-rank focus, mark application, wound transfer and a visible HUNTED-band
bonus. Compatibility is expressed only through broad card target and mechanical
tags. Every automatic mode declares a turn or combat limit; definitions cannot
introduce callbacks or named-card dependencies. A workshop offers three
compatible definitions using a stable named RNG domain, and choosing one spends
that workshop's other card-modification opportunities. The durable copy, card
continuation and history retain its ID. Automatic follow-throughs enqueue typed
effects and use explicit turn/combat keys; an echo repeats only the first effect
that actually resolved and cannot recursively play the card. Transforming the
copy explicitly removes its infusion.

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
NDJSON export are implemented; future expansion choices extend this ledger.

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
coverage is intentionally visible rather than falsely assigning a card. Queued combat effects include trigger ancestry. Remaining unassigned environment
sources must be named as their pressure and hazard rules are integrated. These
records do not impose new combat limits.

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

The live queue preserves initiating card/turn/combat identities across automatic
descendants. Automatic content handlers cannot replenish queue limiter counters.
Registered reactive rules and host dependency edges are checked together.

## Acquisition, pack and threat boundaries

The typed acquisition vocabulary is normal, elite, objective, facility, bargain,
guardian, finale and loop. Technique eligibility intersects living owners, the
requested lane and enabled pack members; stable sorted IDs are returned.
Optional authored `lanes` reject unknown or duplicate values. Legacy techniques
remain available in all lanes until the reward refactor authors narrower jobs;
the live generator now intersects owner and lane eligibility before its existing
bridge/corrective/pivot selection. Normal contacts and events request `normal`,
elites request `elite`, and objective combats request `objective`. Eligibility
is order-independent, then the result is restored to the manifest's frozen RNG
enumeration so filtering does not rewrite supported historical seeded play.

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

## Exact stack-policy arithmetic

`StackRule` supports linear, multiplicative, independent chance, hyperbolic,
threshold, duration refresh, unique, conversion and authored table policies.
Current/next results, formula, cap/soft cap, conversion source and effective-stack
bound are returned together. Probabilities and multipliers use integer basis
points; independent-chance tests compare against exact rational arithmetic.
Tables begin at the zero-stack result, and a promised nondecreasing table may
not decrease. Counts and magnitudes reject booleans and negative integers.

Linear and hyperbolic arithmetic accepts arbitrarily large nonnegative counts.
Exponential policies require an explicitly disclosed effective-stack bound of
at most 10,000, allowing extra copies to remain recorded while preventing exponent
size from growing with unbounded loop depth. This is an authored per-rule limit,
not a global damage cap. A zero count means the rule is absent and returns zero;
present multiplicative rules return their full multiplier in basis points.

All eighteen live items now use typed integer policies. Trauma Mesh, Nerve
Dampener and Targeting Prism use exact hyperbolic basis-point curves: their first
copy supplies the authored 2%, 3% and 4%, while later copies approach disclosed
20%, 30% and 35% soft caps without reaching them. Historical saves continue to
use their embedded or archived rules. Boon and curse conversion, reward-lane
integration and conversion/corruption rewards remain separate boundaries.

The nine behavior-preserving linear boon effects also use typed units and
integer rules. Count effects retain their established caps; Blood Price stores
its 10% step and 25% cap as basis points. Reactive timing remains owned by the
event queue and its limiters, rather than being inferred from a stack formula.

Gentle Hands, Marked Quarry, Calm Under Fire and Last Word now use hyperbolic
basis-point policies. Their first copies retain 8%, 8%, 7% and 5%; later copies
approach disclosed soft caps. Death's Door resistance therefore cannot become
an additive route to removing its interaction entirely.

Hunter's Rhythm, Quick Hands, Countercurrent and Resonant Circuit declare exact
linear activation/value caps consumed by their finite queue listeners. Vigilance
is two explicit layers: a duration-refresh opening dodge and an authored block
table for copies after the first. Runtime opening and card-cost logic read these
contracts instead of inspecting raw stack counts.

Ten linear curse penalties now declare exact count policies, and Dead Channel
declares a unique zero-value rule: its cost is the owned card occupying the
shared deck, not a hidden scalar. Bound curse-card descendants remain limited
by the `curse_card` proc-family exclusion and each drawn copy still resolves.

Glass Bones, Thin Blood and Panic Echo use exact hyperbolic basis-point rules.
Their first penalties remain 6%, 8% and 8%; later copies approach disclosed
45%, 50% and 60% soft caps. These burdens grow meaningfully without allowing an
unbounded percentage input to overflow combat arithmetic.

Frayed Focus and Tremors declare the exact number of affected cards. Lead Feet
is a unique one-rank hostile-movement burden plus a three-copy vulnerability
threshold; Brittle Guard uses a separate 0/1/1/2 duration table. Opening
vulnerability takes the strongest authored curse result rather than silently
adding unrelated duration sources. Every live item, boon and curse effect is
now typed; legacy curves exist only in self-contained historical rulesets.

Every registered persistent effect also has a closed trigger disclosure. The
effect browser appends its timing, finite activation scope and descendant rule
to exact current/next arithmetic. Passive modifiers explicitly say descendants
do not apply; queued draw/block/energy/curse responses state their per-card,
round, phase or combat boundary. Adding an effect key without a disclosure is a
module-load error and the catalog-wide inspection test covers every definition.

## Queued resolution component

`resolution.EventQueue` dispatches typed events through the six phases. Each
event has stable event/root/parent IDs, depth, type, source, targets, typed
operands, ancestry and proc-family history. Listeners are snapshotted when an
event begins and ordered by phase, priority, entity creation ID and stable effect
ID. The primary handler runs before primary-phase listeners. Callbacks enqueue
children; attempting recursive dispatch is rejected. Prevention is confined to
the replace phase. Checkpoints are accepted between dispatch steps, never halfway
through a Python callback, and preserve the active phase/listener snapshot.

The default per-root live-chain allocation is 4,096 generated events. If it is
exhausted, the largest contributing chain is sealed (stable ID breaks ties), its
queued automatic descendants are removed, and its allocation is released so
independent sibling chains can finish. A sealed chain cannot restart in that
root. Thus the budget is not a global cap on damage or an instruction to erase
the rest of a card. Total work can exceed one allocation when several independent
branches resolve. Mandatory death/cleanup events preserve ancestry, survive
sealing and cannot trigger fresh descendants of a sealed branch. Only primary
handlers can request mandatory events. Traces retain `CHAIN SEALED`, root and
event IDs, ancestry, source/target operands and dispatch order.

The component and its live integration are tested separately, including complete
card transactions, registered responses, checkpoints and terminal inspection.

Primary card and enemy effects now use the queue. Their metrics carry event/root
IDs and depth. The engine resumes saved pending effects before accepting another
terminal action, and validates the payload's actor/opcode relationship on load.
Compound cards retain their sequential behavior: a later effect drops targets
killed by an earlier effect. A dead owner cannot execute a pending owned effect.
Legacy reactive responses are registered below. Scalar replacement arithmetic
and atomic rank/card cleanup remain bounded primary operations.

Live riposte is an AFTER damage listener. It inspects the actual guarded target,
requires an unabsorbed hit and a living attacker/defender, and emits a raw 4-damage
child. Riposte descendants cannot trigger the riposte family. Its established
raw-hit behavior bypasses outgoing bonuses and dodge; vulnerability and block
still apply. Area damage finishes its primary targets before AFTER responses are
queued. This explicit phase boundary replaces the old interleaved Python calls.
Saved listener definitions must match registered Python contracts before resume.

Combat's V key opens a scrollable resolution record for the current encounter.
It includes root/event/parent IDs, stable phase and listener order, damage before
and after target modifiers, absorbed block, deflection, actual health loss,
overkill, healing waste, status changes and Death's Door checks. The ordinary
80×24 combat footer remains one line. Inspection only reads recorded data;
opening, scrolling and resizing it cannot consume simulation RNG. Pre-instrumented
arithmetic is labeled unrecorded instead of reconstructed from today's rules.

A card play now owns one queue root. Its authored effect index, original targets,
owner and upgrade state are durable. Each next effect waits for the previous
effect's automatic descendants, so conditions observe resolved prior effects.
Seven registered post-card steps preserve resonance, movement draw, forced
discard, damage draw, block-card counting, focus draw and reserve-energy order.
Each declares a once-per-card limiter; descendants drain between those steps.
Victory cleanup waits for them. A dead owner loses remaining owned continuations;
the surviving crew's expedition remains active. Host continuation count is bounded
by the validated card effect list and the fixed seven-step registry.

`play_card(..., resolve=False)` starts the same validated transaction and exposes
its dispatch boundaries for checkpoint tests. Ordinary commands use synchronous
resolution. Another card play or end-turn command cannot interrupt a pending root.
The terminal resumes saved pending work before accepting its next command.

Draw and held-curse resolution use attributed queued events. Each bound curse
checks its living owner and emits registered stress, wound, energy or movement
operations. Curse descendants exclude the curse-card proc family; manually drawn
copies still each apply normally. The trigger dependency graph includes the host
DRAW→CARD_DRAW edge, so a future draw reaction cannot hide a cycle behind that
host operation. Energy drain floors at zero and records its actual change.

Mercy Circuit's healing response and Adrenal Coil's first actual injury response
now emit attributed BLOCK descendants with proc-family exclusions. Adrenal's
existing counter resets before each enemy phase; fully absorbed hits do not use
it. Automatic block uses the authored stack result directly, preserving the
previous passive arithmetic. Second Wind remains an atomic lethal-hit replacement
with its existing finite combat use; its restoration is now recorded explicitly.
Death immediately repairs rank/card invariants inside damage, then emits a
mandatory casualty notification before the next card continuation. The normal
morgue retains sealed-chain traces even when detailed NDJSON export is disabled.

Conditional listeners return an explicit nonactivation without spending their
root/card/turn/combat/charge allowance. A nonactivation cannot emit children.
Inspection distinguishes those checks from activated triggers; limiter counters
advance only after an actual activation. This matters for conditions that become
true later in the same turn.

Persistent effects can now declare exactly `key`, `unit`, and `stack`. Registered
Python effect keys fix whether the unit is a count or basis points; contradictory
units, unknown/unused operands, booleans, nonmonotonic tables and misleading
conversion fields are rejected. Authored table length is at most 65 and base
magnitudes are at most 1,000,000,000; accumulated runtime results are not capped
by that input bound. Typed immutable runtime effects expose cached frozen
`PersistentEffectContract` values. Basis-point calculations return exact fractions.
Multiplicative previews show the full factor; additive bonus consumers receive
its excess above one, so 2.25x is a +125% contribution rather than +225%.

The engine accepts these contracts alongside archived legacy curves. Inspection
shows count, exact current/next value, formula and cap. Live bundled assignments
are the next content boundary; the archived fingerprint remains unchanged.

### Opening and reserve stacks

Capacitor Bank refreshes one opening round of focus at its first copy. At three
and six copies it also adds one and two opening energy respectively. Spare
Magazine draws one extra opening card at one copy and two at three; normal turns
retain five cards. Reserve Cell releases one energy after the first played card
that leaves energy at zero, once per combat; four copies release two. Generated
energy cannot trigger another release. Focusing Lens covers the first one or two
focus-granting techniques per round according to its one/two-copy cap. These rules
are embedded in new saves; archived rules preserve their original thresholds.
