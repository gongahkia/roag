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

## Offline backups

`persistence-layout.ts` defines deterministic v1 ordered-JSON backup bundles for an active world and for a read-only chronicle. A bundle includes only its validated complete source record plus source identity/digest/byte metadata. It contains no browser quota, host, path, machine, clock, user, or random export data.

Exports are read-only and deterministic for the same supplied record. Imports reject malformed, noncanonical, incompatible, unsafe, replay-invalid, mixed-ID, digest-mismatched, oversized (over 1 MiB), or collision records before mutation. Active-world import rejects an existing ID by default; explicit `replace` snapshots the valid old active envelope atomically. Chronicle imports remain read-only and reject collisions; they never become active worlds. The existing chronicle-export API remains unchanged.

UI affordances for preflight warnings, backup download/upload, and recovery choice are deliberately deferred. This contract does not yet split envelopes, add optimization, or claim large-world persistence capacity.
