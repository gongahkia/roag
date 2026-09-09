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
