# Farm, Park and a recognisable little cast

This is a local presentation pass on the existing Flutter/Flame settlement.
Farm and Park now explain their different purposes; residents have four verified
source bodies, detailed colour treatments and activity-specific procedural motion.
Speech is smaller and limited by shared cause as well as by person. The economy,
routes, activity schedules, happiness, gatherings and construction history retain
the previous rules. Nothing was published or deployed.

## Play locally

From the repository root:

```sh
python3 -B apps/settlement/tool/serve.py --port 8765
```

Open or refresh <http://127.0.0.1:8765/settlement/>. A local server was left serving
the rebuilt release there. If the command says the port is already in use, use
that existing URL; `--port 0` starts an additional copy on an available port.
Browser storage belongs to the origin: another port has a separate town.
Export before transferring a town between origins. The build is also packaged
at `artifacts/settlement/roag-settlement-web.zip`.

The existing controls remain: `1/C` Cottage, `2/P` Path, `3/R` Remove, `4/G` Farm,
`5/V` Park; click/drag and release to construct; Escape cancels; Ctrl/⌘ Z and
Shift Z undo/redo; `I` or Alt-click inspects; Space/right/middle drag pans;
scroll or +/− zooms; H returns to the lane; T cycles pause/1×/2×/4×.
Help, tooltips, selected-tool hints, notices, inspection and relevant speech use
the new names. Accessible tool buttons retain their numeric labels and prices.

Farm supplies **2 work positions**, earning **0.2 coin per actual worker-second**.
Park supplies **6 visitor spaces** for actual social visits, relaxation and
eligible evening gatherings. Capacities and explanatory rates read `Rules`.
Prices remain Cottage 5, Farm 8, Park 6; paths/removal remain free. The internal
and JSON names are still `garden` and `green`. No new food resource exists.
The existing cultivated brown furrows/crops and planted green tree/benches remain
distinct at gameplay size; neither footprint nor access changed.

## See the result

These are recordings of the running release, with normal browser input and the
HUD at **1×**. They are continuous excerpts with no time scaling, generated
animation substitutes or cuts within an excerpt:

- [Small town, 26 seconds](evidence/tranche-04/small-normal-speed.mp4): four
  different residents, actual tending and social visits, movement, positive
  experience speech and a genuine full-staff complaint.
- [85-resident town, 20 seconds](evidence/tranche-04/crowd-normal-speed.mp4): mixed
  travel and destination activities, bounded crowd positions and compact speech.
- [Consecutive rendered motion frames](evidence/tranche-04/work-motion-frames.png)
  supplement the videos; these were extracted from the small-town recording.
- Tools: [Farm](evidence/tranche-04/farm-tool.png),
  [Park](evidence/tranche-04/park-tool.png).
- Inspection: [Farm work positions](evidence/tranche-04/farm-inspection.png),
  [Park visitor spaces](evidence/tranche-04/park-inspection.png),
  [crowded-town needs](evidence/tranche-04/crowded-inspection.png).
- Cast: [actual gameplay dimensions](evidence/tranche-04/resident-lineup-gameplay.png),
  [separate enlargement](evidence/tranche-04/resident-lineup-enlarged.png).
  These show the exact exported sprite pixels; the running game also adds its
  dark outline, grounded shadow and activity treatment.
- Layout: [85 people](evidence/tranche-04/crowded-85.png),
  [1440×900](evidence/tranche-04/large-desktop.png),
  [native 110% browser zoom](evidence/tranche-04/native-browser-110.png),
  [reduced-motion night rest](evidence/tranche-04/night-reduced-motion.png).

## Exact art and stable identity

All four bodies come from `assets/urizen/urizen_onebit_tileset__v2d0.png`,
2679×651, using the audited 12×12 cells, origin 1 and pitch 13. Local shape names
below describe our cast; they do not claim authored professions or frame order.

| Saved body ID | Source ID | Rectangle x,y,w,h | Opaque bounds x0,y0,x1,y1 |
| --- | --- | --- | --- |
| `plain-v1` | `urizen-onebit-r00-c104` | 1353,1,12,12 | 3,1,10,12 |
| `longhair-v1` | `urizen-onebit-r01-c104` | 1353,14,12,12 | 1,0,10,12 |
| `hood-v1` | `urizen-onebit-r02-c104` | 1353,27,12,12 | 3,3,10,12 |
| `cap-v1` | `urizen-onebit-r03-c104` | 1353,40,12,12 | 3,2,11,12 |

The alpha masks are distinct. The audit's duplicate r46-c104 was not counted as
another body, nor were held-tool aliases counted as new people. Body/head shapes
and proportions differ; palette variants are explicitly additional treatments,
not additional source bodies. No suitable walk cycle was verified.

All figures use source feet anchor **(6,12)** and a **9×9 world-pixel** drawing.
At ordinary camera zoom 4, that is 36×36 screen pixels, with each source pixel
covering 3×3 screen pixels. Nearest-neighbour rendering remains in use.
Black RGB≤8 is keyed transparent, including the source eye/neck gaps. The asset
exporter applies separate hair, skin, coat and trouser colours to original opaque
pixels and preserves their relative luminance. Four frozen palette indices retain
the cream/teal/clay/lilac families; saved legacy indices are unchanged. Original
sheets and existing exported crops were not overwritten. Each of the 16 new PNGs
has exact provenance, feet anchor and hash in `apps/settlement/assets/manifest.json`.

Schema **3** and `roag.settlement.landscape.v1` remain unchanged. `bodyId` is an
additive resident field. Missing legacy choices are assigned deterministically
from durable home identity with an explicit version-one four-way mapping, then
saved. Assignment does not depend on render randomness, catalogue length, list
order or population. Unknown well-formed body IDs are retained in the save and
render with the plain fallback; malformed fields are rejected. The existing
palette integer remains authoritative. Appearance survives inactive resident
retention, demolition/Undo, save/reload and export/import. It has no employment,
pathfinding, income or mood meaning.

## Motion, crowds and speech

Before editing, a live seed-202 settlement was observed for 68 seconds at 1×,
covering travel, productive work, completed social visits and night rest. Read-only
checkpoint samples confirmed actual activity/progress, rather than judging still
frames as stuck residents. No routing or scheduling defect was found or changed.
The visible defects were one flat-tinted body, shared gesture phases, repeated
large bubbles and a group spread that grew with group size and shifted existing
people when someone joined.

The cast remains **static source art with procedural motion**. Walking gets a
small alternating lift/lean; actual workers get staggered tending gestures;
actual social/gathering participants face inward and make occasional gestures.
Idle glances have still intervals, and rest stays grounded. Stable identity seeds
cosmetic phases without consuming gameplay RNG. Presentation time is independent
of the simulation rate; no animation completion pays money or advances routines.

Stationary residents receive retained slots within their actual road cell,
up to ±3 world pixels horizontally and ±1.5 vertically. Arrivals use available
slots without moving existing occupants. Travellers use narrow ±1.1-pixel lanes
perpendicular to their actual cardinal edge. No collision, occupancy, access or
routing rule changes. Feet, depth sorting, inspection picking and bubble anchors
use the same visible pose. These bounded slots improve readability; they cannot
fully separate an arbitrarily large crowd in one cell.

The existing Flutter/browser **prefers-reduced-motion** setting disables the new
lift, lean, gestures and idle looks. Pausing also leaves poses grounded. There is
no new saved preference, audio layer or resident behaviour framework.

Speech uses content-sized cards (11px text, 7px horizontal and 6px vertical
padding), with a leader to the visible resident. At most **two** full bubbles
appear. Layout tries viewport-contained alternatives, avoids other bubbles and
reserves the existing inspector/instruction areas. Menu opening clears speech
presentation; bubbles are drawn on the canvas and intercept no input.

A shared cause has a **24-presentation-second** complaint cooldown, as well as
the previous per-person 20-second and repeat-person/cause 60-second cooldowns.
There is no simultaneous duplicate cause; positive causes have a short 4-second
shared cooldown. Eligible speakers rotate per cause, with room for a truthful
positive event. Display time remains 3 presentation seconds at all speeds.
Candidates are rechecked each frame; resolved, offscreen or inactive complaints
clear immediately. Inspection remains available during suppression.

Missing work, all staffed positions, an assigned unreachable Farm, unreachable
unassigned Farms, a missing Park, a disconnected Park and actual full-Park waiting
have distinct explanations. The unassigned-disconnected-Farm wording is derived
from existing facility/access state; it does not change the AI's stored reason or
job search. Farm inspection also counts residents currently seeking reachable
work. Positive utterances still come from real existing work/visit/gathering
signals; gestures and render queries invent no experiences.

## Ownership and verification

- `model.dart`, `main.dart`: display labels/purposes, help, notices, tooltips and
  existing menu/inspection boundaries; serialized identifiers retained.
- `appearance.dart`, `residents.dart`, `colony.dart`: frozen body mapping,
  additive saved identity and fallback; no scheduler/economy refactor.
- `resident_presentation.dart`, `settlement_game.dart`: cosmetic clock, bounded
  slots, shared visible feet, source sprites and activity-specific rendering.
- `speech.dart`, `reasons.dart`, presentation constants in `rules.dart`: compact
  layout, per-cause suppression, fair speakers and truthful wording.
- `tool/assets.py`: reproducible derived body palettes, provenance and distinct
  alpha-mask validation. Original catalogue/audit remains authoritative.

The current baseline at `b878e25b4eda261b7c34c92938b80f79feebbbce` was established
before edits: **58 tests passed**, asset validation passed. Final checks:

| Check | Result |
| --- | --- |
| `flutter test --concurrency=1 --reporter expanded` | **66 passed**, including 8 added/refined appearance/speech cases |
| `flutter analyze` | No issues |
| `dart format --output=none --set-exit-if-changed lib test tool/resident_fixture.dart` | No changes |
| `python3 -B tool/assets.py --check` | 24 original crops; 4 distinct bodies × 4 palettes; manifest/font verified |
| `flutter build web --release --base-href /settlement/ --no-web-resources-cdn` | Release built; packaged locally |
| `python3 -B tool/check_resident_equivalence.py` | 85 residents, 220 simulation seconds, 44 full checkpoints identical to baseline after removing only additive `bodyId` |
| `node tool/tranche04_browser.cjs …` | Small/crowded towns, real activity, labels, aliases, appearance, inspection, cancellation, pan/zoom, export/import, resize, reduced motion and night rest passed |
| `node tool/browser_native_scaling.cjs …` | Native XTest mouse/keys, actual 110% zoom, priced/focus-loss cancellation, drag, inspection, pan/zoom and visible actual workers passed |
| `node tool/tranche03_edges.cjs …` | Input, hit-through speech, DPR1.5, mute/audio, storage failures, legacy backup, night repair and reload passed |
| `node tool/browser_test.cjs …` | Existing building/history/import/resize/audio regressions passed; no page errors or failed requests |
| Root `python3 -B tools/build_classic_assets.py --check` | 599 bindings from 67 unchanged source PNGs |

Use the app directory for Flutter/Dart/node commands. Browser helpers use
Playwright and system Chromium; `NODE_PATH` may point to an external installation.
Set `SETTLEMENT_URL=http://127.0.0.1:8767/settlement/` for isolated tests.
`DART_BIN` selects Dart for the equivalence helper. Run
`dart run tool/resident_fixture.dart /tmp/roag-tranche04-fixtures` before the new
browser helper. `tool/resident_lineup.cjs` reproduces the cast evidence.

[Full test output](evidence/tranche-04/tests.txt),
[browser checks](evidence/tranche-04/browser-results.json),
[native checks](evidence/tranche-04/native-results.json),
[input/storage edges](evidence/tranche-04/edge-results.json),
[brush regressions](evidence/tranche-04/brush-results.json) and
[protection/equivalence fingerprints](evidence/tranche-04/verification.json)
record the results.

Native evidence uses XTest mouse/keyboard on Chromium in an isolated Xvfb display;
raw CDP observes the result, with a one-time accessibility setup. Main recordings
use Playwright's browser mouse/keyboard and actual file chooser, not Dart/game
callbacks. Headless unit tests and the isolated baseline replay are separate
from those GUI checks. Fault injection in the existing edge suite is explicitly
fixture-assisted. This is automated interaction and rendered-motion inspection,
not human preference testing.

The [85-person fixture](evidence/tranche-04/residents-85-start.json) is a safe
copy of the existing 74-home legacy fixture plus eleven paid cottages, six paid
Farms and three paid Parks. It is not the user's private town. Its
[active checkpoint](evidence/tranche-04/residents-85-active.json) follows 22 normal
simulation seconds: real work and completed social visits coexist with genuine
unfilled work needs. Recording samples preserve
[small-town](evidence/tranche-04/small-samples.json) and
[crowded-town](evidence/tranche-04/crowd-samples.json) activity evidence.

No baseline failures remain. Early new-harness failures came from querying a
merged Flutter accessibility label, moving a held stroke outside the canvas
(which intentionally commits its prefix), and assuming wall time equals simulation
progress under concurrent software browsers. Selectors, capture placement and
clock-based waiting were corrected; those did not require gameplay changes.

## Protection and limits

All **six README files** kept identical hashes; none was created or edited.
Root README before/after SHA-256:

```text
3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
```

All original source artwork, catalogue/audit/contact sheets, Lua sources and the
four normal Lua player-storage files remain byte-identical to baseline. Test
browsers used fresh isolated profiles on port 8767; the user's existing browser
profile/town was never opened, replaced or reset. The final server only serves
the rebuilt files locally on 127.0.0.1. No hosting/publishing tools were used.

Verified platform: Linux Chromium 155, Flutter 3.50.0, Flame 1.38.2. Safari,
Firefox and mobile were not tested. The four bodies are modest static silhouettes,
not authored walk/work animation sets. Busy access cells still overlap, with
bounded visual positions rather than crowd physics. Legacy palette indices are
preserved, so an old town can still contain many cream/teal residents. The actual
private 85-person save was unavailable; the documented comparable fixture was
used. No economy, happiness or schedule tuning is included.

Human playtest questions:

1. Can I immediately tell what a Farm and a Park do?
2. Do the inhabitants now look like different little people?
3. Can I recognise someone as they move through the settlement?
4. Does the town feel livelier at its normal speed?
5. Can I understand what residents need without repeated speech covering the scene?
