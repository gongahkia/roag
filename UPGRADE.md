# Upgrade to 0.4.0

1. Close the running game. Copy the source folder and the directory reported by
   LÖVE's save adapter (`deepward_02`) somewhere safe. On Linux it is commonly under
   `~/.local/share/love/`; use the actual reported location if environment overrides
   are present. No tool in this delivery modifies your computer's save files.
2. Extract the complete ZIP into a separate directory and launch its `deepward/`
   folder. It contains the whole project, not an incremental patch.
3. Existing readable 0.2/0.3 saves load without regeneration or encounter injection.
   Press End to return to live time if viewing an archive. Press H to manage labour.
4. N opens the new generation lab. Pick frontier, a layout, biome profile and content
   mode. K chooses 3/6/9 workers; Space previews; Enter adopts the preview.
5. F10 runs the core regression suite in your actual LÖVE runtime. F6 exports a
   context record, metrics, recent chronicle and full live replay. Preserve the exact
   source with bug reports.

## Compatibility contracts

- Base world schema remains 0.2.0. New optional labour and living-content tables are
  validated when present. Old snapshots remain free of those additions until an
  explicit new action requires labour policy or new construction.
- Save **envelope** version is now 0.4.0. This release reads 0.2.0 and 0.4.0 envelopes.
  Old binaries reject new saves instead of silently misinterpreting new systems.
  Downgrade by restoring your backup, not by editing the version number.
- Map schema 2 is written; schema 1 is still read. Baked cell data is authoritative.
  The original six examples and frontier-v1 regeneration path remain available.
  New layouts/content use frontier-v2. Exact cross-runtime floating-point replay
  is not promised; use the same source/runtime/build.
- Full saves include active jobs, inventories, fuses, creatures, ecology and notes.
  Terrain templates do not. New expedition confirmation archives the prior live run
  and commits the new save before replacing the in-memory selection.
- Failed saves do not intentionally destroy the preceding committed run. Normal
  quit is cancelled on a reported save failure. Same-directory rename on Linux/macOS
  is not an fsync-guaranteed database transaction; crashes can lose recent progress.
- Do not apply this release over unshared local code edits without comparing them.
  It was built against the shared complete v0.3.0 ZIP.

Untouched older runs do not spontaneously acquire wildlife, larger crews, wards,
charges or dungeons. Terrain settings affect newly generated expeditions only.
