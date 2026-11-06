# Preservation checkpoint and recovery

Created 2026-11-06 before web implementation. Original gameplay, artwork, catalogs,
tooling, content, and existing documentation have not been edited or deleted.

## Source checkpoint

- Git tag: `archive/pre-web-colony-2026-11-06`.
- Commit: `4d89585c13cfcb6874e48a0d8ef64c3a2a1001df`.
- Complete tracked source snapshot: 2,393 files.
- No tag or archive has been pushed/uploaded. These are local preservation copies,
  not an off-machine backup. Keep the archives when transferring to another instance.

The complete source archive preserves old C#/Lua code, tests, room prototypes,
desktop tools, and historical design documents for later reference. A source archive
is a file snapshot, not a backup of the full Git object history. The local tag anchors
that history while this Git repository remains available.

## Portable archives

Archives are under the ignored directory `artifacts/preservation/2026-11-06-pre-web/`.
The small [receipt](preservation-receipt.json) records exact sizes and SHA-256 values.

| Archive | Contents | Verification |
| --- | --- | --- |
| `legacy-source.tar.gz` | Every tracked file at the baseline commit. | All 2,393 file contents compared with the clean baseline tree. |
| `asset-workbench.tar.gz` | Entire actual assets directory, all baseline docs/content/asset references, selected catalog tools, tests/catalog and preservation manifest. | All 1,164 source files compared byte-for-byte by SHA-256. Includes the ignored uncompressed catalog export. |

The workbench's source-file count excludes the added manifest packaged with it.
The archive receipt/validation logs are also retained beside the archives. The
workbench is sufficient for catalog authoring/query work without the game runtime.
It retains original relative paths because the Python builders and reference images
depend on that layout.

## Files protected in place

[preservation-manifest.json](preservation-manifest.json) records 1,163 tracked files
with original paths, byte counts, and SHA-256 hashes:

- All tracked `assets/`, including source PNGs, three ZIP archives, fonts/licenses,
  `catalog.json.gz`, `sprites.json`, the atlas, semantic authoring, evidence, mappings,
  and old tactical data.
- All baseline `docs/`, including `docs/images/`, sprite/room reviews and design records.
- All `content/` and `asset/` references.
- `tests/catalog/`.
- `tools/build_game_catalog.py`, `build_runtime_assets.py`, `asset_catalog.py`,
  `catalog_io.py`, `query_assets.py`, `build_sprite_reference.py`, `build_ui_fonts.py`,
  `make_asset_review.py`, `match_upstream_silhouettes.py`, and `verify_asset_atlas.py`.

Run from the repository root:

```sh
python3 -B tools/verify_preservation.py
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/catalog -v
python3 -B tools/query_assets.py --subject ghost --subject-confidence high --format ids
```

The first command is read-only, permits additions, and fails for a changed, missing
or symlinked original. It does not auto-regenerate or rebaseline anything. New colony
content belongs in `web/`; updating preserved IDs/uncertainty to match new game roles
would destroy evidence. Preserve the manifest itself as the original checkpoint.

The additional ignored `assets/catalog.json` was archived but is deliberately not a
required tracked-file dependency. Fresh clones do not have it. It is stale (318 old
RPG-selected IDs versus 319 in the current compressed catalog) and was left unchanged.
Use `catalog.json.gz` as authority. The full generator `--check` encounters that stale
export in this live checkout; this is a pre-existing derived-file issue, not artwork
damage. Do not overwrite the canonical gzip with the stale JSON.

All 664 original cataloged PNGs and three archives matched their recorded hashes in
the initial review. All 11,887 records retained original coordinates/identity evidence;
the atlas index matched all 7,115 occupied records. Identification uncertainty remains
part of the preserved data: neighboring crops are not confirmed animation frames.

## Recovery without overwriting the working tree

Recover into a new directory, inspect, and copy only the specifically needed files.
For example, create a directory with `mktemp -d /tmp/roag-recovery.XXXXXX`, then use
its returned path in the extraction command:

```sh
tar -xzf artifacts/preservation/2026-11-06-pre-web/legacy-source.tar.gz -C /tmp/roag-recovery.REPLACE
```

For the asset workbench, substitute `asset-workbench.tar.gz`. Do not extract over a
modified checkout. Verify the recovered files using the checker from this repository:

```sh
python3 -B tools/verify_preservation.py --root /tmp/roag-recovery.REPLACE --manifest docs/colony-web/preservation-manifest.json
```

The `--manifest` argument is relative to the command's working directory. A workbench
contains a manifest at its original path too; the legacy source snapshot predates it.

Inspect individual archived source files without checkout changes:

```sh
git show archive/pre-web-colony-2026-11-06:src/Roag.Core/World/WorldWorkSupplies.cs
```

To recreate the tracked snapshot if the local archive is unavailable but the tag is
available, use `git archive --format=tar.gz --output=/tmp/roag-legacy-recovered.tar.gz
archive/pre-web-colony-2026-11-06` as one command. This recreates tracked content only;
the ignored stale export exists only in the asset workbench/live checkout.

Before P09 removal, verify the stored archive hashes from the receipt, confirm the
source commit/tag, and repeat extraction/checking. In a different clone, report a
missing local archive honestly and recreate it from the baseline revision if possible.
Keep at least one recoverable source snapshot outside any runtime deletion list.

## Executed preparation checks

- Original-file checker: PASS, 1,163 files, zero failures.
- Catalog test suite: PASS, 38 tests, zero failures/skips, 4.274 seconds in this run.
- Complete source archive extracted to a fresh temporary directory: PASS.
- Checker against restored source: PASS.
- Deliberately modified and then missing `assets/CREDITS.txt` in the temporary copy:
  both correctly rejected; original restored afterward. Live assets never changed.
- `python3 -B tools/build_game_catalog.py --check` in the restored tracked snapshot:
  PASS. The tracked snapshot has no stale ignored JSON export. This checked the full
  catalog/viewer generation without rewriting the live asset directory.

Machine-readable archive validation is in
`artifacts/preservation/2026-11-06-pre-web/validation.json`; generator output is in
`catalog-freshness.log` beside it. Preparation did not run gameplay tests or claim
the old game is accepted. The new browser game is not implemented yet.

Core catalog queries/builds use Python's standard library. Optional inspection tools
use Pillow, and the offline browser acceptance tool uses Playwright. Keep these as
development dependencies only. The web runtime will use only selected exported art.
