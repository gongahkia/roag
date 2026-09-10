# Persistence contracts

The run save currently uses schema 42, content schema 39, and the existing Python
`random.Random` state. Profile, telemetry, manifest and RNG contracts have independent versions.
Telemetry and manifests are implemented; the persistent profile is still pending.

Schema 42 records engine 1.1.0 and serializes zero to two recycler targeting
credits. The pure 41→42 migration adds zero. Generated salvage alternatives are
stored in the pickup payload before selection, so save/load cannot reroll them.

Content schema 27 adds the first six multi-axis item engines. No run migration
is needed: saves embed and fingerprint their complete content rules, while
schema-26 rules remain independently loadable.

Content schema 28 adds six threshold item bridges for focus, reserve energy,
recovery, stealth, reward choice and first-hit mitigation.

Content schema 29 completes the +18 item target with six late-stack pivots and
one bounded multiplicative marked-damage engine.

Content schema 30 adds twelve unique boon bridges. Their component rules use
the existing typed trigger contracts, but their paired build identities are
kept out of the stackable item lane.

Content schema 31 adds six explicit curse contracts: conditional exceptional
power paired with a separately scaling burden. Every new operand has a typed
unit and trigger disclosure; activations are source-recorded.

Content schema 32 completes the +12 curse target with six paired sequencing,
route, formation, recovery and salvage burdens rather than hidden scalar taxes.

Schema 41 records engine 1.0.0 and adds the selected owner-to-loadout mapping
and optional party doctrine. The pure 40→41 migration supplies an empty mapping
and no doctrine, exactly preserving every pre-feature starting deck and run.
Departure records the resolved configuration in run history. Invalid owners,
foreign loadouts, unavailable definitions, and incompatible doctrines are
rejected on selection and load.

Schema 40 records engine 0.9.0 and resolution-queue schema 5. Card continuations
carry their durable copy ID and infusion ID so a checkpoint cannot change a
conditional modifier or automatic follow-through. The pure 39→40 migration adds
copy ID zero and an explicit null infusion to old queued events; schema 39 could
not contain an infusion, and zero retains that historical identity gap.

Content schema 25 adds 16 immutable infusion definitions with one unique mode
each. Compatibility uses broad targets and mechanical tags; automatic modes must
declare a per-turn or per-combat limiter. Embedded schema-24 rules load with no
infusions and never borrow the installed set.

Content schema 26 adds one horizontal advanced loadout per crew archetype,
eleven tag/role-derived doctrines, and thirteen curated squads with compatible
recommended doctrines. These are immutable content contracts; selected loadout
and doctrine state receive their own run-save migration before runtime use.

Schema 39 records engine 0.8.0 and resolution-queue schema 4. Card continuation
payloads carry the selected mastery branch, so checkpointing between effect
steps cannot reinterpret a played copy after its hand/discard movement. The
pure 38→39 migration adds an explicit null branch to every pending or active
event; schema 38 could not contain an active mastery.

Content schema 24 adds 50 immutable mastery definitions: exactly two signature
or rare techniques per existing crew owner. Each definition has an engine branch
with one bounded authored effect bonus and a coverage branch that widens printed
rank access by one adjacent rank. Embedded schema-23 rules load with no mastery
catalog; definitions are never borrowed from the installed catalog.

Schema 38 records engine 0.7.0 and gives every durable card copy a positive,
monotonically allocated run-local ID plus reserved mastery and infusion fields.
The pure 37→38 migration assigns IDs in durable deck order, then matches active
combat-zone copies by their complete old card value and occurrence. It rejects
an omitted, extra or mismatched combat copy rather than silently regenerating
one. New rewards and curse cards allocate fresh IDs; shuffling and combat zones
preserve them.

Content schema 23 adds an optional, validated four-role technique-expansion
contract. Once an owner uses it, that owner must have exactly one `deepener_a`,
`deepener_b`, `bridge` and `rule_breaker`. Embedded schema-22 rules remain
readable and are never filled with newly installed techniques.

Content schema 22 adds typed elite-mutation definitions. Embedded schema-21
rules load with an empty mutation catalog; definitions are never filled from the
installed catalog. Content-20 remains the exact archived bundle.

Schema 37 records engine 0.6.0 and the exact hostile definition ID of a disclosed
reinforcement reserve. The pure 36→37 migration requires zero old reinforcement
tickets and adds a null reserve: the prior engine could not select that module, so
there is no identity to reconstruct. New combats freeze the reserve at entry and
clear it with the rest of the encounter director state.

Schema 36 records engine 0.5.0 and RNG architecture 2. Migration 35→36 changes
only those version markers. Mutation selection uses a SHA-256-derived local
`encounter_mutation` stream keyed by run seed and stable encounter identity; it
does not consume or replace the serialized `random.Random` simulation stream.
Selected module IDs are immediately frozen in the existing encounter state.

Schema 35 records engine 0.4.0 and freezes the Pressure seen at encounter entry,
selected director modules and reinforcement tickets. Migration 34→35 gives an
already active combat the QUIET profile so its previously shown composition and
arithmetic do not change; outside combat the marker remains null. It does not
consume RNG or regenerate an encounter.

Schema 34 records engine 0.3.0 and requires integer Expedition Pressure, up to
eight validated recent causes, and an explicit historical-gap marker. The pure
33→34 migration accepts only engine 0.2.0, adds pressure zero with
`pressure_incomplete_before_tick` equal to the saved travel tick, and changes no
world, combat, content or RNG state. This is disclosure of unavailable history,
not reconstruction. New runs use a null marker. Missing fields in schema-34
saves are rejected.

`versions.py` names the engine, content, run, profile, telemetry, manifest and RNG
versions independently. Profile version 1 reserves its first contract. Telemetry version 1 records
local deterministic events. `Catalog.manifest` records the
engine, content and RNG versions, enabled pack IDs (currently `base:core`), and a
SHA-256 fingerprint. Its ASCII canonical JSON sorts object keys and indexes
top-level definitions by stable ID. Reordering catalog declarations or JSON keys
does not change identity. Ordered effects, enemy actions, formations, balance and
art do affect identity. Display-name changes retain IDs but change the fingerprint.
The manifest is cached with its immutable catalog. No Python `hash()` is used.

Schema 27 embeds the manifest. Loading checks it against installed, embedded or
archived rules; missing or mismatched fingerprints, pack IDs or RNG versions are
rejected.
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

The immutable `data/legacy20/` bundle retains the calibrated game, card metadata
and ASCII definitions independently of future live content. `load_legacy_catalog`
strictly loads it once and requires the recorded content-20 fingerprint. Manifest
schema comes from that bundle's actual schema, not the engine's newest content
constant. This archive is compatibility data; it adds no entries to live pools.

On load, the engine first compares the requested manifest with the supplied
installed catalog. If it differs, an exact match with strictly validated embedded rules or the
independently validated archive is required. Unavailable rules and unsupported
pack combinations still fail. World, cards, targets, rewards and random state come from the save;
archive selection supplies their recorded definitions rather than new content.
The terminal adopts the restored catalog for all labels and rules and announces
when recorded content is used. New expeditions return to the current catalog.


Schema 33 makes new runs self-contained by storing the immutable effective rules
behind their fingerprint: indexed definitions, balance, card metadata and ASCII
art. A later content edit cannot silently replace those definitions. Loading
checks the canonical SHA-256 before using an already validated catalog or strictly
compiling the saved rules through the same closed content validator. Unknown
opcodes, malformed references and forged fingerprints fail before simulation
state is restored. Up to eight validated alternate rulesets are cached locally.
JSON definition order and object key order are irrelevant; ordered effects,
actions and formations retain their order. No saved content supplies Python code.

The pure 32→33 migration adds an explicit null archive reference only for the
recorded engine-0.2.0/content-20 fingerprint. That historical format did not embed
rules. Missing `content_rules` in a current save is an error; null is accepted
only when its full manifest matches the known archive. Migration never reads or
copies today's content. Subsequent snapshots materialize the selected immutable
rules into the save. This also preserves supported intermediate content revisions
without requiring a new repository archive for each atomic content commit.
