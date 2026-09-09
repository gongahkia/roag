# Persistence contracts

The run save currently uses schema 26, content schema 20, and the existing Python
`random.Random` state. Profile, telemetry, manifest, and domain-separated RNG
contracts are being implemented in Pass 3; they are not interchangeable versions.

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
