# Settlement builder — tranche 01

The playable candidate is **A place to return**, an isolated Dart / Flutter /
Flame browser application in [apps/settlement](../../apps/settlement/). Arrange
miniature cottages around a connected lane, extend branches, and build on the
pads and scars left by demolition. There is no controlled character, economy,
construction timer, worker simulation or connection to dungeon progression.

## Play

Private deployed preview: <https://roag-settlement.epic-oasis-9331.chatgpt.site>.
It retains owner-only Sites access; sign in with the owning account. Publication
succeeded, version 2, using the packaged local release. The authenticated hosted
gameplay journey was not repeated in the test browser; the complete journey ran
against the same release files over local HTTP. This is not an anonymous public
release.

To run the already built local release from the repository root:

```sh
python3 -B apps/settlement/tool/serve.py --port 8765
# open http://127.0.0.1:8765/settlement/
```

To reproduce the build, install **Flutter 3.50.0**, then:

```sh
cd apps/settlement
flutter pub get --enforce-lockfile
flutter build web --release --base-href /settlement/ --no-web-resources-cdn
python3 -B tool/serve.py --port 8765
```

The local static archive is `artifacts/settlement/roag-settlement-web.zip`.
It contains a `settlement/` directory ready to serve below an HTTP origin. Do not
open `index.html` as a file URL. GET and HEAD return 200 at `/settlement/`; the
local server redirects `/` there. A different hosting prefix needs a matching
`--base-href`, including its trailing slash.

Controls:

| Control | Result |
| --- | --- |
| 1 / C, 2 / P, 3 / R | Cottage, Path, Remove |
| Click | Commit the snapped preview |
| Hover | Footprint, access choice, affected junctions and specific validity reason |
| Right/middle drag, or Space + left drag | Pan without building |
| Scroll, +/−, zoom buttons | Integer zoom steps, 2–6× native pixels |
| H / Home, centre button | Return to the arrival lane |
| Ctrl/⌘ Z | Undo |
| Ctrl/⌘ Shift Z, Ctrl/⌘ Y | Redo |
| Escape | Clear the current hover / close a menu |
| Speaker | Session-local mute |
| Landscape menu | Export, Import, New Landscape, controls and save status |

There is deliberately no path drag painting. Click placement is the supported
edit; a left drag cancels placement. Tool and menu hit regions are outside the
world listener. Feedback lasts 300 ms and does not block another edit.

## Rules and visible history

The default landscape is 40×30 with seed **202**. Terrain, sparse trees/plants and
visual variants are deterministic. The first 20 columns of the nine-row starting strip are clear; an
eight-cell lane starts at the western edge, `(0,15)` through `(7,15)`. The fixed
arrival cell cannot be removed. Camera movement and previews generate nothing.

All footprints are one logical cell, including cottages. Sprites retain their
verified front-facing elevation; they are never rotated. A cottage requires
free ground and one cardinally adjoining **active path**, connected to arrival.
When several paths qualify, access prefers south, east, west, then north. Its
small ground connection and the path's visual spur show that choice. These spurs
never conduct road connectivity through a cottage.

A new path must extend an active path. Connectivity is a cardinal flood fill
from arrival, recalculated from immutable world state. Existing disconnected
fragments remain, dimmed, and provide no access. A cottage losing its connection
remains standing, with a quiet amber mark and a `No access` count. Reconnection
clears those indications automatically. There is no damage or penalty.

Remove targets exactly one cottage or path. A removed cottage leaves a buildable
dirt pad. A removed road leaves a subdued scar. Neither trace has collision or
provides access. Empty ground, existing traces and scenery are removal no-ops.
Rebuilding uses the ordinary visible placement rules; there is no hidden access
from a former road. Trees block building; vegetable plants are replaceable ground
decoration, explicitly identified in the preview and restored by undo.

Previews and commits use the same `evaluate` function. Removal previews identify
cottages that would lose access and show the changed road network. Plans retain
their source landscape identity; a stale plan cannot replace a newer scene.
Invalid edits add no history and change no state. Up to **80 edits** support exact
undo/redo, including greenery, traces, ground and cottage variants. A new edit
clears redo. History is intentionally not retained across reload or import.

## Artwork and implementation

The audit, inventory entries, four requested contact/assembly images and source
crops were inspected before choosing the scene. No original artwork or audit
output was changed. [The small manifest](../../apps/settlement/assets/manifest.json)
contains exact source IDs, rectangles, footprints, crop hashes and road port
mappings for **20 world crops and three Godot UI icons**. Runtime bundles contain
these extracted frames and one font, not the audit, contact sheets or catalogue.

| Use | Original Urizen crop IDs |
| --- | --- |
| Two grass textures | `urizen-onebit-r05-c015`, `r05-c016` |
| Dirt / pads | `urizen-onebit-r05-c001` |
| Oak / small plants | `urizen-onebit-r08-c001`, `r24-c005` |
| Two cottage miniatures | `urizen-onebit-r33-c000`, `r33-c001` |
| Roads / junctions | `urizen-onebit-r11-c000` through `c012` |

The source is `assets/urizen/urizen_onebit_tileset__v2d0.png`: 12×12 frames,
13-pixel pitch, origin `(1,1)`. Extraction keys RGB ≤8 transparent; other source
pixels remain. Rendering applies restrained sage, warm clay and tan semantic
tints, nearest filtering and native-pixel alignment. The Source Code Pro font is
unmodified. Publisher declarations and the font licence are packaged in
`assets/ATTRIBUTION.txt` and `assets/LICENSE-SourceCodePro.md`.

The verified roads supply straights, four bends, four T junctions and a cross.
Straight variants are selected deterministically from seed and cell. The source
has no verified isolated/end tile: these cases clip existing dirt into a centre
and its actual cardinal ports. Pads, scars, access stubs, ghost outlines and tiny
feedback particles are explicitly derived ground/presentation details. They are
not additional source art or invented building elevations.

| Owner | File |
| --- | --- |
| Immutable world, rules, flood fill, visual variants, history, JSON validation | [model.dart](../../apps/settlement/lib/model.dart) |
| Flame canvas, native-resolution landscape cache, camera, picking, previews and feedback | [settlement_game.dart](../../apps/settlement/lib/settlement_game.dart) |
| Flutter palette, keyboard/pointer ownership, menus, save-status messages | [main.dart](../../apps/settlement/lib/main.dart) |
| Dart/browser capability boundary | [browser.dart](../../apps/settlement/lib/browser.dart) |
| Local storage, file chooser/download, procedural WebAudio | [platform.js](../../apps/settlement/web/platform.js) |
| Bounded crop export / verification | [assets.py](../../apps/settlement/tool/assets.py) |
| Scoped CI build and artifact | [settlement.yml](../../.github/workflows/settlement.yml) |

The static landscape is rasterized once at native resolution for each immutable
world revision. Camera and feedback redraw that image, keeping decorative work
separate from the model. There is no gameplay RNG consumed by rendering. Short
triangle-wave place/path/remove/invalid/undo cues are generated only after user
interaction. Mute suppresses their creation. Audio and visual effects never gate
the next edit.

## Saves and delivery

Autosave uses only **`roag.settlement.landscape.v1`** in localStorage. It stores
schema 1, seed, width, height and a row-major `lots` array. Each lot contains the
stable grass ID, scenery ID, trace ID, path flag and nullable cottage variant ID.
Road appearances and chosen access are deterministic derivatives of those
values; no renderer object is serialized. Cottage/grass variants are explicit.
The bundled content mapping and deterministic road variant function belong to
schema 1.

Import validates the entire candidate before replacing the live scene: schema,
integer seed, dimensions (16–80 × 12–60), exact cell count, content IDs,
non-overlap, trace consistency and protected arrival. Files over 2 MB are refused.
Invalid input leaves the scene and history untouched. Import success clears
history. A storage failure preserves the in-memory scene and reports
`Not saved · export a copy`; it does not claim success. New Landscape asks for
confirmation. Saves belong to the current browser and origin; use JSON export
to move between localhost and the private hosted preview.

Exact tested toolchain: Flutter **3.50.0** (`ab2912035e`), Dart **3.14.0**, Flame
**1.38.2**, Material UI **1.11.0**. `pubspec.lock` and the browser tool's
`package-lock.json` are included. The SDK download was checked against the
official release SHA-256
`6ea2d806fd57269ae783e6788e2a81fc26eaad782f1e0d0ff38623bb765ff9f5`.
See the [official Flutter archive](https://docs.flutter.dev/install/archive) and
[Flame package](https://pub.dev/packages/flame/versions/1.38.2).

The GitHub workflow pins that SDK, enforces the lockfile, verifies crops,
formatting, analysis and tests, builds the release and uploads
`roag-settlement-web`. Equivalent local steps passed. The remote workflow was
not run in this session; pushing this feature triggers it. Existing GitHub Pages
was not configured and was not enabled. The old Lua workflow is unchanged.

Sites publication used a separate temporary source checkout, keeping ROAG's Git
history and pending audit changes separate. Hosting source commit:
`75ff32e36ca17aae7bc55dd748f1cda2a25ee50c`. Deployment
`appgdep_6af8a81083d881918ad5189fec4319f7` reported `succeeded`. The static package
contains `/settlement/` and a root redirect. No sharing permissions were changed.

## Verification and evidence

Run from `apps/settlement`:

```sh
flutter pub get --enforce-lockfile
python3 -B tool/assets.py --check
dart format --output=none --set-exit-if-changed lib test
flutter analyze
flutter test
flutter build web --release --base-href /settlement/ --no-web-resources-cdn
```

All pass: **10 focused tests**, no analyzer issues, all **23 crop bounds and
bytes** verified, release build succeeds. Tests cover deterministic creation,
preview/commit agreement and purity, stale plans, cardinal junctions and access
preference, disconnection/reconnection, nonfunctional traces, invalid no-ops,
blocking/replaced scenery, exact bounded undo/redo, JSON round trips and rejected
imports preserving scene/history.

Browser verification uses Chromium **155.0.8059.84**, Playwright **1.65.1** on
Linux. Install the pinned browser harness dependencies with `npm ci --prefix
tool`; set `CHROMIUM_BIN` if Chromium is elsewhere. Serve the release first:

```sh
node tool/browser_test.cjs /tmp/settlement-evidence
node tool/browser_edges.cjs /tmp/settlement-edge-evidence
# optional headed X11 / xdotool check, with an isolated display already running:
DISPLAY=:96 node tool/browser_native_scaling.cjs /tmp/settlement-native-evidence
```

The full journey uses browser mouse/keyboard events and the actual menu/file
chooser/download. It never invokes game callbacks or injects a world. It extends
the lane, places nine initial cottages and branches, removes a cottage, removes
a connecting road, reconnects it, builds over the pad, undoes/redoes three edits,
rejects invalid placement, tests drag isolation, pans, zooms, resizes to 1440×900,
refreshes, exports/changes/imports the scene, rejects an unsupported import,
cancels New Landscape, checks mute, and reaches **22 cottages / 36 paths**.
There are no JavaScript errors or failed resource requests in the final run.

- [Actual release recording](evidence/tranche-01/settlement-journey.mp4): placement,
  changing junctions, pad/scar demolition, reconnection and undo; silent recording.
- [Settled 1024×640](evidence/tranche-01/07-final-1024.png),
  [larger fuller settlement](evidence/tranche-01/06-fuller-settlement.png),
  [retained pad](evidence/tranche-01/03-retained-pad.png),
  [disconnected cottages](evidence/tranche-01/04-disconnected.png).
- [Importable fuller scene](evidence/tranche-01/fuller-landscape.json),
  [journey results](evidence/tranche-01/browser-results.json).
- [DPR 1.5](evidence/tranche-01/hidpi-150.png) and
  [truthful failed-save status](evidence/tranche-01/save-failure.png),
  [edge results](evidence/tranche-01/edge-results.json).
- [Actual Chromium 110% zoom](evidence/tranche-01/native-browser-zoom.png),
  [native results](evidence/tranche-01/native-results.json). XTest/xdotool supplied
  the browser's Ctrl+plus accelerator and the successful world click. Palette
  selection used Playwright. This is separate from the headless browser journey;
  neither is human manual playtesting.

The storage-failure test injects a failure into the **browser storage API only**;
it confirms an unchanged saved copy, a visible failure status, recovery by refresh
and confirmed New Landscape. Audio API observation confirms a user-gesture
context reaches `running`, and mute prevents oscillator creation. Audible
loudness/timbre has not been judged by a human.

In the final recorded fuller-scene sample, frame intervals had a **16.7 ms median
and 33.4 ms maximum**. Input-to-observed-localStorage-write measurements were
66–310 ms, median 98 ms, including Playwright pointer work and polling. These
are not physical display latency measurements. No separate animation wait is
introduced. A concurrent-browser run was slower (33.3 ms median / 50 ms maximum);
headless software rendering and recording are material measurement limitations.

During implementation, browser checks exposed and corrected painting outside the
world clip, competing keyboard focus, irrelevant empty-ground preview outlines,
and missing preview visibility of disconnection. Browser harnesses were also
corrected to wait for initial asset loading and to account for OS/browser scaling;
the final checks above pass. No old catalogue failure was repaired or treated as
a new application regression. The audit's existing 47-pass / one media-accounting
failure logs remain untouched; unrelated legacy suites were not rerun.

The original `./roag.sh` opened through real LÖVE on isolated Xvfb with isolated
`XDG_DATA_HOME`; no Lua error was logged before the timed shutdown. OpenAL logged
an unavailable RTKit service in this test environment. The current artwork check
also passed: 599 bindings from 67 unchanged source PNGs.

## Boundaries and limits

Root README SHA-256 before **and** after:
`3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a`.
No README file was created or edited. Every file present at the task baseline,
including pending audit edits, retained identical bytes. All four normal LÖVE
storage files retained identical names and hashes, with no additions. See
[preservation result](evidence/tranche-01/preservation.json).

[Inference] The implementation is adequate for evaluating this desktop settlement prototype:
the deterministic rules, exact history, persistence and full browser journey
work with the audited pixel art. That establishes a working candidate, not
enjoyment or a broad engine recommendation. Mobile/touch and other browsers are
unverified. There is no path painting, seed-picker UI or persisted undo stack.
Muted state is session-local. The 41 MB uncompressed release includes Flutter's
local CanvasKit distributions; the artwork itself is a small bounded set.
The hosted authenticated gameplay journey and remote GitHub Actions execution
remain unverified. Public sharing was not enabled.

Human playtest questions:

- Did I want to keep placing structures to see the settlement develop?
- Could I predict the effect of a placement?
- Did paths create useful spatial choices?
- Did old pads and scars make the place feel lived in, or merely messy?
- Did the interface let me build at the pace I wanted?
