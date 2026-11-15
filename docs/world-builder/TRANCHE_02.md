# Continuous building and residents

The existing Flutter/Flame landscape now supports continuous Cottage, Path and
Remove brushes. A completed cottage stroke brings residents in from the arrival
lane. They walk on the actual road graph, idle near their homes, visit reachable
neighbours and respond to demolished or reconnected paths. Construction remains
immediate; residents never block another building action.

This tranche is **local only**. No site was deployed, no hosting configuration
was added, and the original Lua game was not changed.

## Launch and controls

From the repository root, serve the already built release:

```sh
python3 -B apps/settlement/tool/serve.py --port 8765
```

Open <http://127.0.0.1:8765/settlement/>. If that server is already running,
refresh the page rather than starting a second copy. `--port 0` selects a free
port, but a different port has a different browser-storage origin: export/import
is the way to transfer a landscape between origins.

| Control | Behaviour |
| --- | --- |
| `1` / `C`, `2` / `P`, `3` / `R` | Cottage, Path, Remove |
| Primary click or drag | Stage one piece or paint a stroke; release commits |
| Escape | Cancel the unfinished stroke |
| Ctrl/⌘ Z | Cancel an unfinished stroke first; otherwise undo one entire stroke |
| Ctrl/⌘ Shift Z, Ctrl/⌘ Y | Redo one entire stroke |
| Space + drag, right/middle drag | Pan; the gesture keeps its initial mode |
| Scroll, `+` / `−`, footer buttons | Zoom; scrolling retains its pointer anchor |
| Arrows, `H` / Home | Pan, return to the arrival lane |
| Landscape menu | Export/import JSON, New Landscape, controls |

The small instruction line and footer retain the main tools, history and camera
controls without a permanent left panel. The opening camera gives the lane a
24-pixel left margin at the normal 4× zoom. Grass is slightly muted. Cottage and
tree sprites, roads, terrain, pads, scars and the colour vocabulary remain from
the existing prototype.

## Brush transactions

`LandscapeSession` owns one optional `BrushStroke`. The stroke stores its
committed starting world and a progressively staged immutable world. Every
candidate uses the existing `evaluate` placement rules against that staged
world; dragging has no separate placement or access definition.

Pointer samples are interpolated centre-to-centre through cardinal grid cells.
An exact diagonal corner crosses **X before Y**, in either signed direction.
For example, `(7,15) → (10,12)` visits:

```text
(7,15), (8,15), (8,14), (9,14), (9,13), (10,13), (10,12)
```

Each accepted change is deduplicated for the rest of the gesture. Invalid
candidates are not deduplicated, so revisiting one can succeed after other cells
provide access. Existing paths can be crossed without creating edits. A tree
does not get bypassed, and a rejected road cell does not connect later cells
across its gap. Candidates that already have a separate legal connection still
use the ordinary placement rules.

Cottages currently have a **one-cell logical footprint**, matching the original
model and artwork. Removal operates on that complete object, or on a road cell;
it retains the original pad/scar contract, protects the arrival road and leaves
scenery alone. Empty ground and old traces are no-ops.

Accepted additions appear immediately with gold outlines. During removal, the
committed buildings/roads stay visible beneath red provisional marks. Residents
therefore continue to use visibly existing, committed infrastructure until
release. The footer shows the staged piece count.

Release commits all changes as one history entry, one synchronous browser save,
one resident reconciliation and one quiet sound. An entirely invalid or
unchanged stroke creates no history or save. Only the first invalid candidate
gets a short world-space message; raw pointer events do not emit sounds.

Escape, tool changes, pointer cancellation, application/browser focus loss and
opening a menu cancel a held stroke. Camera controls cancel it before changing
coordinates. Undo while painting cancels only that gesture. Pointer-down chooses
pan or build; pressing or releasing Space later does not switch it.

Leaving the world canvas finishes at the **last accepted canvas position** and
requires a fresh pointer-down. No line is interpolated through the footer,
header or other controls. A later pointer-up cannot revive the finished stroke.
The loading cover is removed when Flame has loaded the actual canvas assets,
rather than on Flutter's earlier first frame.

Export and autosave read the committed session snapshot, never `session.view`.
`SaveSnapshot` includes a session revision and encoded world; `persist` rejects
obsolete snapshots after commits, undo, redo or import. The real localStorage
writer is synchronous, with no pending asynchronous save queue. Storage failures
keep the in-memory edit and report `Not saved · export a copy`.

## Residents and the real path graph

Each cottage has one stable identity. Newly authored homes receive `home-N`;
the saved allocator is part of exact building history. Resident ownership uses
that identity rather than a sprite index or current list position. Unrelated
edits preserve existing actor objects and positions. Reconciliation removes
orphaned actors and clears a departing copy if Undo restores the same identity.

Newly committed homes create pending arrivals. The first is available on the
next simulation update, with subsequent arrivals staggered by 180 ms, capped at
two seconds. A person appears at the existing arrival road, follows a shortest
cardinal road route to their cottage's actual access cell, and produces a
one-second gold cue on that home. Players can continue building throughout.

`Landscape.pathNeighbours`, `pathComponent` and `pathRoute` own traversability;
`activePaths` and `access` remain the authority for connected building access.
The preference is south, east, west, then north. A visual cottage access spur
does not become a traversable graph edge. Residents stand at the road access
position rather than walking through a cottage sprite.

At home, stable identity-based idle durations of 2.3–4.64 seconds desynchronise
outings. Walking speeds are 3.5–3.8 cells/second. Residents select existing
reachable homes, pause there for 0.8 seconds and return. A single reachable
home permits a short neighbouring-road stroll instead. These are ambient visits,
not jobs, production, needs or inventories.

Committed topology changes invalidate future routes. A person may finish only
the still-legal current edge and then replan. A disconnected home stops starting
outings. A legal return along its surviving local road fragment is allowed,
using the same physical road graph; otherwise the person waits. Reconnection
replans the existing person without adding another arrival.

If a road is removed beneath a person, settling chooses a surviving current
edge endpoint or directly neighbouring road centre within 1.01 cells of their
actual position. The closest eligible centre is reached at five cells/second.
If none survives, the person waits in place. This is a short local retreat from
removed infrastructure, not a cross-map teleport or continuation along a stale
route. Removing the owned cottage cancels pending work immediately and fades
an already visible person over 220 ms. Visitors whose destination disappears
replan safely.

All delays are update-driven state, with no resident `Future.delayed` callbacks.
Browser blur/visibility suspends resident updates; each elapsed update is also
clamped to 50 ms. Returning from a background tab discards excess time rather
than replaying queued arrivals or jumping along routes.

## Exact history, transient activity and save compatibility

Undo/Redo exactly restores cottages, IDs and allocator, road/access state,
terrain/scenery, demolition traces and placement variants. History remains
bounded to 80 entries. It does **not** rewind every footstep. Existing matching
residents continue from their current safe positions; restored missing residents
may initialise at home. Import/reset rebuild transient actors; normal refresh
also initialises saved inhabitants without replaying new-construction arrivals.

Residents are not serialised. Their timers, routes, positions, facing and
decorative motion never create history entries, request saves or consume the
authored coordinate-based variation function. Connectivity and cottage indexes
are cached per immutable world. Routes are searched on reconciliation or an
actual departure, not every render frame.

The storage key remains `roag.settlement.landscape.v1`. **Schema 1 is accepted**;
old five-entry lots gain stable `legacy-x-y` cottage identities without changing
their original fields. Schema 2 writes a sixth cottage-ID entry and
`nextCottageId`. The decoder checks unique/non-orphan IDs and a valid allocator,
alongside all existing terrain, dimensions, overlap, trace and arrival checks.
New schema-2 exports require this version; the previous schema-1-only app cannot
read them. Failed imports preserve the scene and history. No browser storage
was cleared to implement migration.

## Artwork and rendering ownership

The narrow review used the existing
[inhabitants contact sheet](contact-sheets/inhabitants-1.png),
[audit](ASSET_AUDIT.md) and inventory. The selected neutral empty-handed figure is:

| Item | Exact source |
| --- | --- |
| Source PNG | `assets/urizen/urizen_onebit_tileset__v2d0.png` |
| Original ID | `urizen-onebit-r00-c104` |
| Crop rectangle | `[1353, 1, 12, 12]` |
| Derived app asset | `apps/settlement/assets/images/resident.png` |
| Local licence evidence | Urizen/vurmux CC0 declaration in `assets/CREDITS.txt` |

The original source and audit files are unchanged. Existing black-key extraction
makes RGB values ≤8 transparent. The renderer displays the static figure at
7×7 logical pixels inside a 12-pixel cell, with a dark outline/shadow and warm
cream or pale cyan tint. Horizontal mirroring, translation and a 0.3-pixel
procedural bob acknowledge walking. **No source walk cycle or construction
animation was verified or claimed.**

Ground/roads remain cached at native resolution. Cottage and tree foregrounds
are merged with people by their foot Y position, so inhabitants can pass behind
and in front of landmarks. The asset checker now verifies 24 bounded crops,
including this single additional source crop, plus the unmodified font.

| Owner | Main change |
| --- | --- |
| [model.dart](../../apps/settlement/lib/model.dart) | Shared graph, identities/migration, staged strokes, exact history and snapshot revision |
| [residents.dart](../../apps/settlement/lib/residents.dart) | Ownership, update-driven arrivals/visits, route invalidation and local settling |
| [main.dart](../../apps/settlement/lib/main.dart) | Pointer lifecycle, cancellation, one save/sound per stroke, compact controls |
| [settlement_game.dart](../../apps/settlement/lib/settlement_game.dart) | Staged rendering, person depth/contrast, camera margin and readiness |
| [browser.dart](../../apps/settlement/lib/browser.dart), [platform.js](../../apps/settlement/web/platform.js) | Browser activity notifications; existing storage/file/audio bridge |
| [assets.py](../../apps/settlement/tool/assets.py), app manifest/attribution | Reproducible exact resident crop |
| [brush_residents_test.dart](../../apps/settlement/test/brush_residents_test.dart) | Stroke, compatibility, ownership and routing checks |
| [tranche02_browser.cjs](../../apps/settlement/tool/tranche02_browser.cjs) | Release-browser transaction and resident journey |

## Verification and evidence

Baseline was the current clean checkout at `867fa84f`, not a reset to the earlier
reported commit. The original ten model tests and 23-crop check passed before
editing. Subsequent user commits to the work in progress were preserved.

From `apps/settlement`:

```sh
flutter pub get --enforce-lockfile
python3 -B tool/assets.py --check
dart format --output=none --set-exit-if-changed lib test
flutter analyze
flutter test
flutter build web --release --base-href /settlement/ --no-web-resources-cdn
```

All passed: **26 tests**, no analyzer issues, no formatter changes, 24 crops and
font verified, successful release build. The 16 new tests cover multi-cottage
transactions, fast/diagonal traversal, obstacles, retrying invalid cells,
deduplication/removal, no-op/cancel/Undo-during-stroke, partial/stale save
protection, real schema-1 migration, ID validation, commit-only arrivals,
visits/local walks, cut/reroute/reconnect, pending arrival and visit removal,
history/load/import reconciliation, local settling and bounded background time.

The served **release** was tested in Chromium 155.0.8059.84 with Playwright
1.65.1. Install the pinned harness dependency with `npm ci --prefix tool` and
serve the release before running:

```sh
SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/tranche02_browser.cjs /tmp/settlement-brush-evidence
SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/browser_test.cjs /tmp/settlement-regression
SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/browser_edges.cjs /tmp/settlement-storage
# optional X11 check: an isolated DISPLAY and xdotool must already be available
DISPLAY=:95 SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/browser_native_scaling.cjs /tmp/settlement-native
```

These harnesses create fresh isolated browser profiles. They do not clear or
overwrite an existing user's browser profile. Default browser-suite assertions
observe storage, downloads and rendered output, without game callbacks or an
injected world. The storage-failure regression injects an exception into the
browser storage API only. The native helper needs Node with built-in WebSocket
support (Node 26.12.0 was used), Chromium, X11 and xdotool; `CHROMIUM_BIN` and
`XDOTOOL_BIN` override executable locations.

The main journey covered every requested browser case: a one-sample fast lane
extension; a turn stopped by an actual oak; eleven cottages in one stroke;
visible arrivals and ongoing visits; multi-piece Remove with exact Undo/Redo;
Escape, tool switch, unfinished Undo and actual browser touch cancellation;
canvas exit; modifier changes during pan/build; cutting/reconnecting during
travel; removing a home before its arrival stagger elapsed; pan, zoom and
1440×900 resize followed by more painting; refresh, export/import and a real
schema-1 import. It finished with **42 cottages, 42 distinct IDs and 45 roads**,
then restored the 1024×640 viewport. No JavaScript errors or failed resources
were recorded. These are automated browser mouse/keyboard gestures, not manual
human playtesting.

Native XTest input separately exercised real Ctrl-plus browser zoom (**DPR
1 → 1.1**), palette clicks, held-stroke tab focus loss and a five-cell drag/release.
CDP enabled Flutter accessibility once, then observed the document/storage and
captured its screenshot. Game commands used native input; CDP did not emulate
focus or call the game. Switching tabs cancelled the held stroke
without saving; returning and making a fresh stroke produced exactly 13 road
cells. An initial native harness used xdotool `--clearmodifiers` while holding
the mouse, which released the button before the tab switch; this was a harness
error, not evidence of a game cancellation regression. A separate startup race
in reading Chromium's still-empty debugging-port file was corrected by waiting
for a valid port. The final native harness passed.

The existing browser suite also passed at 22 cottages/36 roads, including camera
picking, import refusal, menu isolation and user-gesture audio/mute. DPR 1.5
picking and injected storage-failure recovery passed. No baseline settlement
test failures remain. The unrelated broad Lua suite was not rerun: its sources
and storage were unchanged, verified below.

- [Continuous release recording](evidence/tranche-02/brush-and-residents.mp4)
  (12 seconds, silent, 1024×640): path drag → cottage stroke → arrivals → road cut
  → Undo/reconnection → continuing activity. This is the real browser capture,
  trimmed at the ends and encoded to H.264; no frames were rearranged or mocked.
- [Staged path](evidence/tranche-02/01-path-staged.png),
  [staged cottages](evidence/tranche-02/02-cottages-staged.png),
  [arrivals](evidence/tranche-02/03-arrivals.png),
  [connection cut](evidence/tranche-02/04-cut-during-arrival.png).
- [Fuller activity](evidence/tranche-02/07-fuller-activity.png),
  [1440×900](evidence/tranche-02/08-large-canvas.png),
  [1024×640 final](evidence/tranche-02/09-final-1024.png),
  [native 110% zoom](evidence/tranche-02/native-browser-zoom.png).
- [Main browser results](evidence/tranche-02/browser-results.json),
  [existing regression results](evidence/tranche-02/regression-results.json),
  [storage/DPR results](evidence/tranche-02/edge-results.json),
  [native results](evidence/tranche-02/native-results.json),
  [exported fuller landscape](evidence/tranche-02/village.json),
  [release fingerprint](evidence/tranche-02/release.json).

At the fuller settlement, 90 release-browser animation-frame intervals measured
a 33.3 ms median and 66.6 ms maximum while headless video capture was active.
The existing regression measured 33.3/50 ms. The recorded scene continued
animating during building; no long catch-up burst was observed. These are
browser scheduling observations on this machine, not physical display latency,
a 60-fps claim or a cross-device benchmark. Tests also confirm route searches
do not increase on every idle update.

## Preservation and remaining limits

Root README SHA-256 before **and** after:

```text
3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
```

No `README.md` file was added or edited. Original source and documentation files
outside `apps/settlement/` retained their baseline hashes, including Lua,
catalogue, source artwork and prior audit/evidence. The existing local release
archive was the sole refreshed original file outside the app. All four normal Lua storage files
retained identical names and hashes, with no additions. See
[preservation evidence](evidence/tranche-02/preservation.json).
The local release archive was rebuilt at
`artifacts/settlement/roag-settlement-web.zip`; it contains the `/settlement/`
directory and is not an online deployment. The user's existing port-8765
server was left running.

The figure is a static miniature with procedural motion, not a verified walk
cycle. People may overlap on busy roads; they do not have collision or traffic
management. A deleted isolated road can leave a waiting person at that local
position until a legal connection returns. Exact authored Undo does not promise
identical animation timing. Residents loaded from a save can start at home.
The map is finite, and zooming out can expose space beyond its edge.

Mobile usability, other browser engines, very large imported maps and remote
CI were not verified. Browser touch-cancel coverage is not a mobile playtest.
The audio regression verifies context/oscillator behaviour and mute; the
recording is silent and does not establish subjective sound quality. No needs,
economy, jobs, timers for construction or new building catalogue were added.

Human playtest questions:

- Could I lay out a village at the pace I wanted?
- Could I tell which building action caused someone to appear?
- Did people seem to use the settlement I built?
- Did changing the paths visibly change their behaviour?
- Did that activity make me want to keep building?
