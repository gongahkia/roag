# Persistence contracts

The run save currently uses schema 32, content schema 20, and the existing Python
`random.Random` state. Profile, telemetry, manifest, and domain-separated RNG
contracts are being implemented in Pass 3; they are not interchangeable versions.

`versions.py` names the engine, content, run, profile, telemetry, manifest and RNG
versions independently. Profile/telemetry version 1 reserve their first contracts;
they do not imply those features are finished. `Catalog.manifest` records the
engine, content and RNG versions, enabled pack IDs (currently `base:core`), and a
SHA-256 fingerprint. Its ASCII canonical JSON sorts object keys and indexes
top-level definitions by stable ID. Reordering catalog declarations or JSON keys
does not change identity. Ordered effects, enemy actions, formations, balance and
art do affect identity. Display-name changes retain IDs but change the fingerprint.
The manifest is cached with its immutable catalog. No Python `hash()` is used.

Schema 27 embeds the manifest. Loading checks it against the installed catalog;
missing or mismatched fingerprints, pack IDs or RNG versions are rejected.
`migrations.run_26_to_27` accepts exactly schema 26/content 20, copies the input,
and adds the recorded content-20 manifest. Its SHA-256 is
`b6b8c6fe837b9035b498cd867ffe29c80620429dd56d50bc6c29197389f5502b`.
It never reads current content or regenerates rooms, intents, rewards or RNG state.
The dispatcher checks that each migration advances exactly one version. Older
unknown schemas and newer unknown versions are rejected explicitly. Future content
changes need a declared compatibility/migration decision before historical saves
can be accepted; silently replacing this legacy constant is not a migration.

Schema 28 adds a deterministic telemetry-schema-1 run ledger. Each record has a
contiguous sequence number, kind, source ID, simulated travel tick, combat round
and finite JSON operands. Recording consumes no RNG and reads no clock. The
27->28 migration adds an empty ledger with `incomplete_before_tick` set to the
saved tick: events that occurred before recording existed are explicitly unknown,
not synthesized. New runs have no missing-history boundary. Instrumentation and
local history presentation are added separately from this serialization contract.

Schema 29 embeds the resolution queue's budget, pending typed events, active
phase, snapshotted listeners, limiter counters and sealed traces. Migration
28->29 adds the authored empty schema-1 queue because the old engine had no
pending queue to preserve; it does not invent historical triggers. Current saves
must contain this durable field. Resume validates event ancestry, phase, actor,
card and status references before executing callbacks. Engine-level tests save
after a partial dispatch and compare the remaining healing chain and ledger with
an uninterrupted continuation.

## Morgue files

`history.write_run` writes history-schema-1 summaries with the same atomic file
mechanism as saves. The filename is the SHA-256 of the canonical run snapshot;
reopening the same completed state is idempotent. Identical seed/command runs
with identical recorded state share a record, not separate attempt counts.
History contains decisions, routes, encounter results, card counts and arithmetic
totals; it is not uploaded. `python3 -m dumbest_dungeon.history --filter warden
--detail` reads the default local history directory in a terminal. Malformed files
are reported without discarding readable records. `--directory` selects an
explicit alternative local directory.

Detailed NDJSON export is disabled by default. An explicit `detailed=True` write
adds a separate atomic `telemetry/<run-id>.ndjson` file with a version/manifest
header followed by the detailed deterministic records. No account or network
code is involved. Elapsed wall time, when supplied by the interface, is report
metadata rather than simulated state; absent measurements are labeled unmeasured.

The terminal writes a morgue at victory, defeat or explicit abandonment. The title
screen's Run history entry supports text filtering and scrolling; the CLI reader
provides the same information for shell workflows. `--telemetry` opts into the
additional NDJSON export. Manual UI saves include an optional `local_session`
envelope with elapsed seconds. The UI validates and restores it separately from
the simulation snapshot, so menu time and resumed duration cannot consume RNG or
advance travel/combat. The measurement includes reading, animations and pauses;
it is not a controlled active-play-duration estimate.

## RNG-1 content enumeration

The simulation retains its serializable `random.Random` state. To make catalog
declaration order irrelevant without changing the calibrated RNG-1 seeds,
`data/legacy_order.json` freezes the starting catalog's ID order. Runtime indexes
use that order for historical IDs and lexical order for new IDs. The table is
strictly decoded once and checked against SHA-256
`3c2f52af04e162871d8b6720432ce247b96757d47e17c1805e5b7293dd8680b4`.
It is a compatibility artifact, not a second source of content definitions; no
names or mechanics are inferred from it. Changing it requires an explicit RNG
compatibility decision. Effect lists, action lists and formations remain ordered
rules. JSON keys and top-level definition order are not rules.

Tests reverse every top-level catalog definition list and compare new-world and
50-command continuation hashes. Three subprocesses using `PYTHONHASHSEED=0`, `1`
and `987654` compare another 50-command continuation. A retained 344-command
ordinary seed-0 calibration transcript compares final gameplay state and RNG
against the pre-ledger evidence, excluding only the newly added ledger.

## Atomic local files

`save.write_save` serializes finite JSON before touching the destination. It creates
a private, uniquely named temporary file in the destination directory, writes and
flushes it, fsyncs the file, replaces the destination atomically, then fsyncs the
directory. Failure before replacement leaves the previous destination intact;
tests exercise serialization, file-sync, and replacement failures. A directory-sync
failure after replacement reports an error because crash durability is uncertain;
the new file may already be installed. Concurrent writers cannot share a temporary
filename, but there is no merge protocol: the last successful replacement wins.

Reads reject duplicate JSON keys, non-finite numbers, invalid UTF-8, and non-object
roots. Schema validation remains the engine's responsibility. These files use no
network service or account. The default destination is
`$XDG_STATE_HOME/dullest-dungeon/run.save.json`, falling back to
`~/.local/state/dullest-dungeon/run.save.json`.

## Historical evidence

`docs/evidence/pass3/terminal44-before-final-play.json.gz` contains an actual
schema-26 save made during a normal terminal run, before the final two legal card
plays. It is a migration fixture, not an injected starting build or a post-victory
save. The calibration transcripts in the same directory also retain schema-26
final snapshots.

Migration 29→30 advances the queue to schema 2 and explicitly marks every saved
primary payload as `raw_damage=false`. This distinguishes already computed hits
from ordinary card damage before queued counterattacks are registered. Missing
payload fields in current saves are errors, not implicit defaults. The migration
copies its input and accepts exactly schema 29 with a schema-1 queue.

Migration 30→31 adds queue schema 3: explicit deferred continuations, the saved
card upgrade flag and the next authored effect index. Historical queued effects
receive `deferred=false`, `card_upgraded=false`, and `effect_index=null`; their
already resolved primary payloads remain unchanged. Deferred host continuations
wait for ordinary descendants to drain. Content listeners cannot request deferred
or mandatory work. This keeps card sequencing and cleanup serializable without
letting JSON define control flow.

Migration 31→32 records engine 0.2.0 for the deterministic queue release. It accepts
only the preceding engine 0.1.0/content-20 contract and changes that engine marker;
it preserves the exact content fingerprint, enabled packs, world, RNG, queue and
ledger. The installed catalog still has to match the retained fingerprint. The
historical content-20 fixture and ordinary command transcript remain supported.
