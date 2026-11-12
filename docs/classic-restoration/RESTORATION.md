# Classic ROAG restoration — 2026-11-12

The user explicitly chose the Lua/LÖVE2D roguelike again and dropped the web
requirement. This restoration is a new forward commit; it does not reset, rewrite
or delete the earlier Git history.

## Audit and selected baseline

The original game is tag `1.0.0`, commit
`fd4f670597e6b2f7be213c3db6aa3dfb4b374ee9` (2026-10-20). Its README describes
single-tile cardinal movement, aimed paid attacks, waits, fifteen classes,
103 boons and seven two-floor biomes. The simulation advances on committed player
actions; idle frames, inspection and menus are free.

All 92 original runtime Lua files were byte-identical in the paused colony
checkpoint. No reconstruction or mechanical TypeScript-to-Lua port was necessary.
The classic simulation, scheduler, seeded randomness, generation, classes, combat,
boons, rewards and save format remain the original Lua implementation. `main.lua`
changes presentation/loading only; the world tile uses integer 4×12px art scaling.
The README and desktop launch/test/package commands make that game primary again.

The old renderer used Kenney 1-Bit plus thirteen approved Tiny/RPG/Caves
counterparts. Its frozen audit lists 599 required semantic bindings. The newer
canonical `assets/catalog.json.gz` instead owns Urizen, Godot-node icons and
keyboard/controller prompts. It has 11,887 records, including empty/uncertain cells;
record count is not a count of usable or unique game sprites.

## Modular replacement

[assets/classic-bindings.json](../../assets/classic-bindings.json) explicitly maps
all 599 game roles to 237 unique existing crops from 67 preserved source PNGs.
The catalogue already supplied casts for all 15 classes, the 50 original enemy roles
and all 103 boons; the remaining scenery, terrain, objects and status cues now have
explicit selections too. [ART_MAPPING.md](ART_MAPPING.md) records representative
motifs, low-confidence identities, static aliases and assembly decisions.

`tools/build_classic_assets.py` validates the full requirement set, source image
and crop hashes, rectangles, foreground, pack ownership and uncertainty, then emits
`src/classic_sprite_data.lua`. The game does not parse a large gzip/JSON catalogue
at runtime. `src/catalog_visuals.lua` resolves those stable roles without putting
sprite IDs into game state or save rules. Unknown/missing roles reject an incomplete
resource generation rather than quietly loading a partial reskin. F5 reads fresh
generated data and retains the previous resources if the replacement is invalid.

To change a cast, edit its role in the binding JSON, run `make assets`, and press F5
(or restart). The catalogue and source pixels remain untouched. The classic ink
palette uses an in-memory alpha mask for legibility; opaque terrain uses original
pixels with derived borders. Adjacent sprites are never inferred to be animation
frames or native autotile families. Attack/hurt and boon-detail aliases remain
explicit static reuse.

## History and preservation

The committed colony checkpoint is
`archive/colony-paused-2026-11-12`, commit
`f7589d81135365673ca41922700400f4ba5c0453`, which is the parent of this restoration.
The earlier preservation tag/source archives and all historical design documents,
reports and evidence are retained. P06/P07 browser runs were stopped at the user's
request; P08 was unfinished. Their incomplete work is not marked accepted.

Paused worktrees remain in `/home/gongahkia/coding/projects/roag-worktrees/`.
Uncommitted P08 patches and untracked-source archives were also retained under
`artifacts/classic-restoration/paused-colony/` in the main workspace. Those local
archives are not claimed to exist in a fresh Git clone.

[retired-paths.json](retired-paths.json) records the exact 918 files removed from the
active tree: C#/generated reference projects and tests, the TypeScript web game,
superseded build files, and 12 Kenney source/license files. Earlier commits remain
reachable; the active game/package loads only the newer catalogued sprites.

The original preservation manifest is unchanged. The new verifier checks all 1,163
originals: 1,151 byte-identical files in place and 12 deliberately retired Kenney
files recovered byte-for-byte from the recorded Git baseline. Only those explicit
Kenney paths may use archived verification. All current source packs, compressed
catalogue, sprite descriptions, uncertainty, images used by catalogue tools,
original design documents and catalogue tooling/tests stay in place.

Canonical gzip SHA-256:
`ed7b5cbbdfb21b82092197035694f3454582a8822df783e31171a8ec56beede3`.
There is no old-pack remapping inside that file. The ignored uncompressed
`assets/catalog.json` in the older main workspace is a historical stale export;
`tools/catalog_io.py` continues to use the tracked gzip as authority.

## Validation

All required local checks pass on the final sources:

- **830 active Lua cases**: 821 unchanged classic cases plus 9 new catalogue
  contracts. The active runner retains every gameplay case and adapts the mixed
  loader/settings/refresh/fog/HUD assertions. Exactly 30 obsolete Kenney-image/layout
  cases are explicitly retired by name; `tests/run.lua` and its original test
  modules remain unchanged. `tests/classic_run.lua` documents the substitutions.
- The original full test invocation was interrupted externally. It is not counted
  as a pass. Eight successful `ROAG_TEST_RANGE` batches cover each active index
  from 1 through 830 exactly once, with ordered case-name/exit-code verification.
  Accepted batches took 339.5 seconds of elapsed wall time (776.7 seconds aggregate).
- **47 Python catalogue/exporter tests** pass, including all 38 preserved tests.
  Deterministic generation and `make assets-check` pass. The canonical gzip and
  original source PNG hashes remain unchanged.
- Two fresh actual LÖVE processes select a class, create an ordinary seeded run,
  move, wait, aim/commit an attack, inspect boons, save/quit and Continue. Saved
  state and the next real action match exact uninterrupted continuation. Each
  process verifies 120 idle frames leave the actual world unchanged. The resumed
  observer was corrected to track the authoritative Continue object; the final
  GUI run passed again with that correction.
- The isolated presentation review checks 3,024 GPU/source pixel channels across
  seven original crops and six derived terrain cases. Menus, actual gameplay,
  readable dark-source actors, water, doors, pillars and save continuation images
  were opened and inspected. The contact sheet is presentation evidence, not a
  natural encounter or injected-inventory gameplay claim.
- `make preservation` verifies all 1,163 original files as described above.
- `make package` produces a deterministic **847,385-byte** `dist/roag.love`, 170 files,
  with only current catalogued artwork and fonts. No Kenney PNGs, C# or web runtime
  enter the archive. Standalone packaging also rejects stale/corrupt asset data
  before creating an output. Package SHA-256:
  `099f6f97e0dff86d9383a994a7cb760685a9ef6303ce6951269e2b861c05d153`.

Command receipts, source hashes, all eight batch logs, indexed coverage, failed
exploratory runs, screenshots, downloaded-state bytes and inspected-art evidence
remain under `artifacts/classic-restoration/` in the main workspace. The root
coordinator separately passed asset checks, 47 catalogue tests, preservation,
GUI, packaging and whitespace checks in `acceptance-01/`. Its final resumed-state
GUI rerun is in `acceptance-01/gui-authoritative/`.

Tested here: Linux, LÖVE 11.5, LuaJIT 2.1.1788856981, Python 3.14.8, Xvfb/software GL.
macOS, Windows and physical controller hardware were not tested in this restoration.
The desktop CI workflow has been updated; local evidence is not a claim of a
remote CI result. Web deployment/Vercel is intentionally retired by the latest
user decision.
