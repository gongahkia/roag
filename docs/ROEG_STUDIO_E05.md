# ROEG Studio E05 — deterministic static texture atlases

E05 compiles the **published loose PNGs**, not editable Studio layers. Stable
asset IDs, room tile IDs, UI theme bindings and source documents do not change.
Normal `love .` uses a complete compatible atlas generation when present, then
falls back to the published loose PNG and finally the built-in drawing. Studio
continues to edit independent E01 source art.

## Publish and inspect

Save art in `love . --studio`, then publish its loose visual manifest first.
The save directory is displayed in Studio and is available from
`love.filesystem.getSaveDirectory()`; it contains the temporary atlas candidate.

```sh
luajit tools/studio_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project
luajit tools/studio_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project --apply
love . --atlas-build
luajit tools/studio_atlas_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project
luajit tools/studio_atlas_publish.lua --save-dir '/absolute/LOVE/save/directory' --project my_project --apply
```

`love . --atlas-build` launches a short LÖVE 11.5 compositor process and exits.
It writes only a **candidate** under `studio/atlas-build/` in the LÖVE save
directory. It does not activate it. The publisher's dry run lists every page,
filter/group, dimensions, ID, region, excluded asset, occupancy and byte total.
The separate `--apply` writes content-named PNG pages first, reads them back,
then activates the inactive `assets/studio/atlas-a.pack` or `atlas-b.pack`
record. Once both record slots are occupied, review the dry run and add
`--overwrite` to subsequent applies. There is no arbitrary output path in the
LÖVE builder. For a smaller, power-of-two page limit, use for example
`love . --atlas-build --atlas-max=256`; the range is 32–2048 and must fit the
current renderer's texture-size limit.

To disable packing while keeping the loose art and both atlas generations:

```sh
luajit tools/studio_atlas_publish.lua --disable
luajit tools/studio_atlas_publish.lua --disable --apply
luajit tools/studio_atlas_publish.lua --enable --apply
```

After editing the same stable ID, save the E01 document, republish loose art
(use `--overwrite` after reviewing that publisher's dry run), run
`love . --atlas-build` again, then dry-run/apply the atlas publisher again.
Until rebuilt, the stale atlas is ignored and loose art is shown. World and
theme pack publication remain separate operations; publish their referenced
visuals first. Atlas publication is not an atomic transaction with either pack.

## Format and fallback

The pure-Lua shelf planner sorts by filter, category/group, descending height,
descending width and stable ID. It produces power-of-two pages, preserves
untrimmed source dimensions and alpha, and extrudes a two-pixel edge on every
side. Nearest and linear samples use separate pages. Actor, world, UI, effects
and small backgrounds have separate default groups; an asset's validated
`atlas_group` can override the default. Pages use LÖVE 11.5 ImageData and
zlib-compressed lossless PNGs. The same inputs/configuration produce the same
bytes on the same LÖVE/zlib build. Renderer-specific zlib versions may produce
different compressed bytes while retaining identical decoded pixels.

The inert `roeg-studio-atlas/1` record maps IDs to page, untrimmed rectangle,
padding, filter, source checksum and source-manifest fingerprint. Page PNGs are
immutable and content named. The game validates the checked record, complete
pages, decoded dimensions, current loose inputs and deterministic mapping
before creating cached Image/Quad handles. It may recover the previous complete
generation only if that generation matches the **currently published** loose
images. If neither does, it draws loose images. A corrupt optional atlas never
blocks game startup or Continue. Atlas references do not enter run snapshots.

Bitmap glyph sheets remain **standalone** because E04 examines source pixels
to choose readable built-in-font fallback per glyph. Backgrounds larger than
512 pixels on either axis remain standalone to avoid wasting atlas space. Shader
references are likewise excluded because they are not static sprites. These
retain their stable IDs. Unsupported/missing/malformed published PNGs fail the
candidate build clearly; no image is silently dropped.

## Verification and limits

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp sh tests/studio_e05_gui_smoke.sh
TMPDIR=/var/tmp sh tests/studio_e05_stress.sh
TMPDIR=/var/tmp sh tests/studio_e05_profile.sh
TMPDIR=/var/tmp sh tests/repeat_gui_smokes.sh 3 /var/tmp/roeg-e05-game-gui
```

The E05 GUI smoke creates E01 and E03/E04 isolated fixtures, compares real
loose and atlas renders, tests a genuine `DamageTaken` effect, disables and
corrupts an atlas, detects stale art, re-edits E01 pixels at the same ID,
rebuilds, and saves/continues across processes. The generated-room tile is
checked separately. These callback-driven LÖVE tests are not physical mouse,
keyboard or tablet playtests.

The current renderer issues Quads through the existing draw calls; it does not
batch world tiles, so no draw-call or native GPU speedup is promised. Atlas
pages can use more GPU texture memory than small loose PNGs because of padding
and power-of-two sizing. CLI writes have readback and a prior checked record,
but neither LÖVE's save writes nor the CLI promise hardware fsync. Old
content-named pages are retained for rollback and may need deliberate later
garbage collection. No E02 animation timeline or E06 hot reload is included.
