# Medieval local persistence layout

This is the Phase 1.6 v1 local-only persistence boundary. It stores no account, server identity, cloud state, telemetry, or network authority.

## Authority and stores

`FoundationWorld` remains the sole mutable-world authority. A complete validated envelope is stored in `worlds`; people, tasks, regions, events, and history are not independently mutable records.

Database `jomon-medieval-worlds-v1` uses IndexedDB layout version 4:

- `catalog`: compact active-world and read-only-chronicle browser index;
- `worlds`: authoritative active `FoundationWorld` envelopes;
- `chronicles`: authoritative read-only finalized chronicles;
- `creation-settings` and `terminal-controls`: independent UI records;
- `world-record-indexes`: digest-bound, non-authoritative locators for the envelope, instantiated people, known stored regions, delegation tasks, temporal events, causal history/checkpoints/segments, and social memory;
- `world-snapshots`: bounded full-envelope recovery rings.

Index entries are derived only from a validated envelope and bind its canonical byte count, causal revision, and non-cryptographic source digest. They are for compact browsing, integrity checking, and future indexed loading. They never authorize simulation, reveal hidden commitments in UI, or replace the envelope.

Version 4 is a metadata-store upgrade only. A valid version-3 world loads unchanged with absent index/snapshot metadata. An explicit later save derives metadata; no world schema migration occurs. Malformed/incompatible envelopes remain unavailable and are never repaired or migrated.

## Atomic active-world replacement

Every active-world write uses one short-lived read-write transaction across catalog, worlds, derived indexes, and snapshots. It reads the current catalog, active envelope, derived index, and snapshot ring inside that transaction; validates the submitted and existing records; derives canonical metadata; then writes the envelope, catalog, and exact index together.

When a valid existing envelope differs, it is copied into the per-world snapshot ring before replacement. The ring retains three complete envelopes, ordered by monotonically increasing snapshot sequence; `sequence % 3` chooses the rotation slot. Only that documented bounded rotation may remove a snapshot. A corrupt active envelope, derived index, or snapshot ring makes ordinary save refuse without overwriting it. Catalog data is derived metadata and may be rebuilt by an explicit save while full envelopes remain protected.

Transaction abort, request failure, unavailable storage, blocked upgrade, and quota failure return typed stable failures. No save is attempted from `unload` or `beforeunload`; there is no automatic quota request, cleanup, or `navigator.storage.persist()` call. Storage estimates and Phase 1.6 byte bands are advisory preflight input only.

Snapshots can be inspected without mutation. Restore is explicit; it uses the same replacement transaction and snapshots a different valid record being replaced. Current recovery is containment plus explicit inspection/restore/import APIs, not automatic repair or a browser recovery screen.

`FoundationWorld` v15 keeps bounded `WorldDeckNavigationState` v1 inside the same authoritative full envelope. It adds only the deterministic canonical eight-prop `FoundationJomon` provenance. Mutable `MedievalWorldState` v16 / courier v3 carries fixed `initialCourierId`, optional mutable `activeCourierId`, canonical permanent `departedCourierIds`, Jomon v3's exact eight-record latest-action projection, and the bounded cargo-hold lot projection; causal history v6 and replay projection v8 prove both mutable results. IndexedDB remains layout v4: there is no new store, split record, or database migration. A strict read-only v14-to-v15 conversion accepts only a fully validated original three-prop v14 envelope, deterministically adds its five fixed static props, and proves current replay/state validation before returning a v15 value. The v15/state-v14 bridge first validates and replays the original legacy source before deriving its bounded action state; the v15/state-v15 cargo bridge then validates/replays that original no-cargo source before deriving an empty Jomon v3 cargo projection. Earlier state-v13/state-v12 conversion composes before those proofs. A corrupt, ambiguous, stale, or incompatible old envelope is neither repaired in place nor overwritten; repository reads leave it stored and fail closed. A successful later normal save may write the current full v16-state envelope and refreshes only validated derived index/snapshot metadata. The separate compiled commodity catalogue v1 remains compiled content rather than an envelope copy; `commodityStates` remains empty until a later market owner establishes its own facts.

`MedievalWorldRepository.resolveCourierContinuityLoss()` is the narrow transactional bridge for the renderer-independent loss reducer. A continuing result validates and atomically replaces the exact stored active source, updates its derived index/snapshot ring, and rejects stale or forged submitted source without touching the stored world. A crew-extinction result uses the existing active-to-read-only-chronicle finalization transaction, atomically deleting the matching active source and adding its chronicle. No browser record is rewritten on read, and no mutable household/person/departure side record exists.

Terminal presentation v13 and detailed-renderer adapter v7 project source-backed physical station readouts plus the authoritative latest vessel-prop action status/message feedback after a world is validated. The bounded cargo lots and action records live only inside the existing full envelope; they add no store, index, snapshot format, migration, layout change, or read-time rewrite. A later normal save persists the upgraded envelope through the existing atomic transaction.

Focused cross-contract coverage uses a deterministic active-world replacement to create a valid snapshot, then proves that a stale finalization source and a forged snapshot source both fail closed without changing the current active envelope, catalog entry, or corrupt stored record. It does not connect a courier-loss or Jomon-integrity policy intent to repository finalization: callers must still explicitly invoke their owning world and repository operations.

## Offline backups

`persistence-layout.ts` defines deterministic v1 ordered-JSON backup bundles for an active world and for a read-only chronicle. A bundle includes only its validated complete source record plus source identity/digest/byte metadata. It contains no browser quota, host, path, machine, clock, user, or random export data.

Exports are read-only and deterministic for the same supplied record. Imports reject malformed, noncanonical, incompatible, unsafe, replay-invalid, mixed-ID, digest-mismatched, oversized (over 1 MiB), or collision records before mutation. Active-world import rejects an existing ID by default; explicit `replace` snapshots the valid old active envelope atomically. Chronicle imports remain read-only and reject collisions; they never become active worlds. The existing chronicle-export API remains unchanged.

UI affordances for preflight warnings, backup download/upload, and recovery choice are deliberately deferred. This contract does not yet split envelopes, add optimization, or claim large-world persistence capacity.

## Fixture coverage and future derived optimization forms

`performance-fixtures.ts` v1 constructs three valid active envelopes and four read-only chronicles through existing pure world/finalization and catalogue APIs, then checks the bounded canonical index ordering and byte size in memory. This is fixture coverage only: it opens no database, adds no store, persists no cache, and does not treat canonical bytes as browser quota or IndexedDB timing.

[Profile-guided optimization boundaries](optimization-boundaries.md) defines the only permitted future design shape for derived indexes, compact/packed projections, memoization, and optional worker results. The [performance fixture matrix](performance-storage-baseline.md#deterministic-fixture-matrix-and-scale) supplies reproducible current inputs, not optimization authorization. Each durable derived form must bind the authoritative full-envelope ID, causal revision, digest, canonical byte count, and its relevant contract version; any mismatch invalidates and rebuilds it. It remains read-only, known-facts-only, and never becomes a second mutable authority or a hidden-frontier discovery surface.

This design adds no derived cache store and does not alter layout v4. `FoundationWorld` remains the sole mutable authority, valid v3 envelopes continue to load without a metadata rewrite, and layout metadata remains explicit-save-only.
