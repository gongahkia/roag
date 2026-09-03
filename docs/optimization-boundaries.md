# Profile-guided optimization boundaries

This is the Phase 1.6 v1 decision boundary for future optimization. It is deliberately a design and validation contract, not an implementation authorization. The pure companion module is `src/medieval/optimization-boundaries.ts`; it creates deterministic current-fixture evidence and fails closed for missing, stale, noncanonical, malformed, or contract-mismatched evidence.

## Current measured foundation

The reproducible current profiling inputs are the nine deterministic `performance-fixtures.ts` rows: every supported sheltered-reach, watershed, and far-coast preset crossed with focused, balanced, and deep fidelity. The original sheltered/focused, watershed/balanced, and far-coast/deep rows remain unchanged for historical comparison. Each resolves through production generation configuration and uses its fixed seed, initial-world caps, six instantiated Jomon crew, fidelity plan, terminal/sidebar/deferred-adapter pure projections, and a valid `60 + 180` minute partition checked against one bounded 240-minute action across the 1/5/30/120/240-minute scheduler cadences. The benchmark's Node timing observations and deterministic UTF-8 byte accounting are in [the performance/storage baseline](performance-storage-baseline.md).

Timing observations are review evidence only. They are neither CI timing gates nor world inputs. The enforced facts are the deterministic fixture byte ceilings and retained-state growth proxies; the 16/24/32 MiB values are planning bands; browser-response figures are future targets. The fixture contract intentionally collects neither process-memory values nor browser/quota measurements.

Current facts deliberately make every strategy below **not yet eligible**:

- Terminal presentation materializes zero map cells, so it supplies no spatial-query workload.
- Initial-world output and frontier commitments are bounded; unrevealed commitments are not latent mutable map/population state.
- Catch-up is canonical scheduling/provenance with bounded records, not an economy, institution, people, travel, or combat simulation.
- The validated full `FoundationWorld` envelope remains the sole mutable authority. There is no packed/binary authority data, worker adapter, durable cache store, or incremental generation path.

## Evidence quality and shared invariants

An eventual evidence record must bind a reproducible workload profile to the full validated envelope's world ID, causal revision, non-cryptographic digest, canonical byte count, and relevant contract versions. It must show fixed seed/named RNG provenance, canonical-output equivalence, content-safety validation, causal-history replay, bounded execution with typed failures, and action-time semantics. A stale revision/digest, changed contract version, changed fixture configuration, omission, malformed field, or noncanonical workload order rejects evidence and cannot authorize an optimization.

Wall-clock samples may be reported outside saves for performance review, but may never be simulation input or canonical/persisted world state. Derived work must preserve identical canonical outputs, content policy, replay, and known-frontier information boundaries. No derived form may expose hidden frontier facts or become a second mutable authority.

## Decision matrix

| Technique | Future owner | Evidence trigger before eligibility | Current blocker |
| --- | --- | --- | --- |
| Spatial index | Known-world query projection | A measured, materialized-cell spatial query workload; observed pressure; equivalent canonical results; identity-bound, known-facts-only rebuild/failure path. | Zero materialized terminal map cells; no spatial workload. |
| Incremental initial/frontier generation | Initial-world and frontier generation authority | A measured broad generation workload; explicit cancellable/resumable commit boundary; canonical equivalence to one-shot generation; source/invalidation and known-facts proof. | Current initial/frontier commitments are bounded. |
| Scheduled-summary batching | Simulation catch-up canonical cursor/window owner | Measured scheduled-summary pressure after a real bounded domain workload exists; existing cursor/window ordering, cadence, outcome evidence, bounded-record behavior, and partition invariance proven unchanged. | Current catch-up is scheduling/provenance only. |
| Compact/packed derived representation | Derived-representation adapter | Measured byte/allocation pressure plus a proven read-only derived representation with canonical equivalence and exact invalidation. | Full-envelope JSON remains the only authority; no measured derived pressure. |
| Memoization | Caller-owned ephemeral projection cache | Measured repeated pure projection; explicit complete key (including source identity and inputs); bounded lifetime/owner; stale knowledge and world facts cannot be retained. | No repeated pure-projection profile or authorized cache lifetime. |
| Optional worker adapter | Authority-provided optional-worker adapter | Measured main-thread pressure plus a versioned serializable input, named deterministic RNG streams where needed, result validation/identity binding/stale rejection, and equality with canonical main-thread output. | No worker capability dependency, adapter, or measured pressure. |

“Observed pressure” must come from the reproducible fixture/profiling plan, rather than a speculative threshold. A later version may set a technique-specific threshold only alongside the next fixture work and its recorded platform evidence.

## Derived representations and execution adapters

Any future index, packed projection, cache, or worker result must have a `DerivedRepresentationBinding`: full-envelope ID/revision/digest/canonical-byte identity, its own positive contract version, read-only-derived ownership, and known-facts-only frontier visibility. A mismatch invalidates and rebuilds it; it is never incrementally patched into a competing authority. The v1 contract defines validation only and adds no persistence store.

Memoization can retain only a digest of an explicit complete key for a pure projection. Its owner is one caller and its lifetime is one projection pass or one action. It retains neither player knowledge nor world facts.

A future worker is optional. Its request must contain only authority-provided, versioned canonical serialized input and named deterministic RNG streams (or explicitly no randomness). It cannot access IndexedDB, mutate a world, become a simulation authority, or become a persistence authority. A result is usable only when its request/result identities match the current full envelope, its digest validates, and its canonical output exactly equals the normal validated main-thread result. Cancellation, unsupported capability, timeout, or worker failure never mutates or advances a world; the normal validated path remains authoritative. This document does not add worker code or browser capability requirements.

Scheduled-summary batching, if later eligible, remains a batching of the existing canonical windows only. It must not let player action partitions change outcomes, cadence, evidence, or bounded record behavior, and it must not introduce domain simulation in this foundation.

## Compatibility

This v1 contract changes no world or manifest schema, save content, `FoundationWorld` reducer, IndexedDB layout (still v4), or browser behavior. Valid v3 envelopes still load unchanged; layout metadata remains explicit-save-only. It creates no save migration, cache persistence, grid, binary serialization, packed live state, incremental generation, worker configuration, or full domain simulation.
