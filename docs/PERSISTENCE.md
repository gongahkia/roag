# Persistence contracts

The run save currently uses schema 28, content schema 20, and the existing Python
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
