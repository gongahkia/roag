# A working colony loop

Drag out homes, connect a Productive Garden and watch residents earn construction
money by working. Add a Village Green for leisure. Their actual experiences feed
a small happiness model and speech bubbles; a successful neighbourhood can host
an evening gathering. There are no individual orders, inventories, wages,
maintenance, health or production chains.

This continues the existing Flutter/Flame application. It is **local only**:
no deployment or hosting configuration change was made.

## Play locally

From the repository root:

```sh
python3 -B apps/settlement/tool/serve.py --port 8765
```

Open <http://127.0.0.1:8765/settlement/>. The user's existing server on that port
was left running; refresh it to use the rebuilt release. If another server owns
the port, `--port 0` chooses a free port. Different ports have separate browser
storage, so export/import transfers a village between them.

| Control | Action |
| --- | --- |
| `1` / `C`, `2` / `P`, `3` / `R` | Cottage, Path, Remove |
| `4` / `G`, `5` / `V` | Productive Garden, Village Green |
| Primary click/drag, then release | One construction transaction |
| Escape | Cancel a stroke or leave inspection |
| Ctrl/⌘ Z; Ctrl/⌘ Shift Z or Ctrl/⌘ Y | Undo; Redo. Undo during a held stroke cancels it first |
| `I`, header inspection button; Alt-click | Toggle read-only inspection; quick inspection |
| Header `Ⅱ`, `1×`, `2×`, `4×`; `T` | Pause/speed; cycle speeds |
| Space/right/middle drag, arrows | Pan, with gesture mode fixed at pointer-down |
| Scroll, `+` / `−`, footer controls; `H` / Home | Zoom; return to lane |
| Landscape menu | Export/import, New Landscape, Unlimited building, help |

Choosing a building tool leaves inspection. The five price-labelled tools,
history and camera controls fit at 1024×640. Money, population, mood and time are
in the compact header; detailed explanations stay in tooltips and inspection.
Night adds only a mild shade to the world and remains fully buildable.

## Rules in this tranche

[rules.dart](../../apps/settlement/lib/rules.dart) owns the tunable economy,
activity, capacity, mood, movement and event values.

| Rule | Implemented value |
| --- | --- |
| Treasury | 200 coins for a new or migrated village; one currency, stored in thousandths |
| Construction | Cottage 5; Garden 8; Green 6; Path and demolition free; no maintenance |
| Recovery grant | 20 coins per 150 simulation seconds, accrued continuously and identified in the header |
| Garden | One-cell footprint; two stable job assignments, at most two actual workers |
| Productive work | 0.2 coin per validated worker-second; 12-second bouts; retry work after 24 seconds or a recent social visit |
| Green | One-cell footprint; six actual social/gathering visitors; travellers occupy no seats |
| Social visit | Six seconds at a valid green qualifies; a full-green arrival may wait two seconds; repeat leisure seeks wait 18 seconds |
| Day | 150 seconds: Morning 0–15, Day 15–90, Evening 90–125, Night 125–150 |
| Departures | Work opportunities start at second 3; new arrivals stagger by 0.18 seconds, capped at two seconds |
| Movement/rest | 3.5–3.8 cells/second; home pauses 1–1.72 seconds; initial saved inhabitants retain the earlier varied departure delay |
| Mood | Base 50; clamp 10–90; overall word is Uneasy below 45, Settled below 70, otherwise Content |
| Experience | Completed work +15, completed leisure +15, each recent for one day |
| Missing opportunities | −10 per concern after a 75-second newcomer/migration grace and a real unsuccessful search; valid travel is exempt |
| Connection | −20 after ten seconds disconnected, replacing missed-work/leisure penalties; recent positive experiences remain |
| Gathering benefit | +10 for actual attendees, lasting 75 simulation seconds; awarded once per event identity |
| Bubbles | At most three visible, normally three presentation seconds; resident cooldown 20 seconds, repeat-complaint suppression 60 seconds |
| Event | At evening: four connected arrived residents, at least two recent work experiences and two recent social experiences, reachable green; seeded 80% chance; two-day settlement cooldown |
| Attendance | Invite up to six; three actual arrivals start a 12-second gathering; 22-second invitation deadline |

Opportunity penalties are records of actual failed searches, not random needs.
A connection problem does not stack all three negative concerns. The −20
replacement also avoids improving happiness merely by cutting a lane that hid
two −10 opportunity penalties. Nobody dies, leaves town or disables recovery
income because mood is low.

Workplaces are selected by reachable route length and available assignments.
Existing valid assignments stay stable when another garden appears. A newly
built garden wakes idle searches promptly during work periods. A job remains
reserved while its resident travels or takes leisure; it is not an hourly
worker pool. Residents finish valid journeys and short activities across phase
boundaries, return at night and rest automatically.

## Construction and money

All five tools reuse the original `evaluate` placement rules and cardinal path
graph. Gardens and greens need adjoining arrival-connected roads. Existing
trees remain blockers; replaceable plants, pads, scars, access preference and
the protected arrival road retain their contracts. All three place types have
explicit one-cell footprints in this miniature map.

The brush still interpolates fast pointer samples, crossing X before Y at exact
diagonal ties. It deduplicates accepted changes and retries previously invalid
candidates if the staged world later permits them. Obstacles do not connect
cells beyond a rejected gap. Removal affects each eligible object once and
leaves a pad or road scar. Provisional removal keeps the committed infrastructure
visible until release.

A stroke captures its construction budget and Unlimited setting at pointer-down.
Accepted placements reserve their exact price; the footer shows price against
that original budget. Unaffordable targets are red, and duplicates/invalid cells
cost nothing. Background income does not enlarge the held stroke's budget.
Release commits one history entry and deducts the recorded amount once from the
**current** treasury, retaining earnings received while dragging. Cancellation
never restores an old treasury snapshot.

Escape, tool changes, pointer cancellation, focus loss and menu opening cancel
the stroke. Leaving the canvas commits only its accepted prefix and requires a
fresh pointer-down. Pan/build mode stays fixed despite modifier changes. No-op
or wholly unaffordable strokes create no construction history. Ordinary building
has no confirmation dialog.

Construction history is separate from the forward simulation:

- Undo placement refunds its recorded charge once; Redo deducts exactly that
  charge, even if Unlimited has since changed.
- An unaffordable Redo is rejected atomically and keeps its redo entry.
- Demolition has no refund; Undo demolition restores the object at no new cost.
- Unlimited waives **new** construction costs and records zero, without changing
  routines, income or mood rules.
- Clock, earned income, completed experiences, event state and resident records
  continue forward. Building fields/variants/IDs restore exactly; the ID allocator
  keeps its high-water mark rather than rewinding.
- History remains session-local and bounded to 80 entries. Reload/import clears
  history, as before; old construction does not acquire refundable prices.

These rules intentionally supersede tranche 2's allocator rewind. Cancelling an
uncommitted preview creates no durable person or building identity.

## Residents, speech and the gathering

Residents retain their cottage identity. Removing a home deactivates its person,
cancels routes and stops production. The inactive record retains appearance,
earnings, progress and experiences; Undo reactivates that person. Work capacity
is reconciled if another resident has since taken a vacant assignment.

Travel uses `Landscape.pathRoute`, `pathNeighbours` and the same access calculation
as placement. Future routes invalidate on committed edits. A still-legal current
edge can finish; a removed underfoot road uses the existing bounded local settling
behaviour, or waits in place if no local road survives. Reconnection replans the
same actor. Nobody walks directly through cottages or jumps across the map.

Only a resident already at a valid workplace and spending a productive tick
earns money. Assignment and the arrival tick award nothing. Actual visits count
green capacity; six seconds of valid social time awards recent leisure. Rendering,
camera position, inspection, sprite visibility and animation completion never
award money or experiences.

[reasons.dart](../../apps/settlement/lib/reasons.dart) is the authored phrase and
explanation catalogue: no job, assigned garden unreachable, jobs taken, work
journey, completed work, no leisure place, cut-off green, actual full-green wait,
completed social visit and actual gathering participation. Inspection uses the
same structured causes as speech. Journey utterances begin only while that journey is current, so delayed speech does not announce a superseded destination. A cut-off green remains a connection problem,
and restoring it clears that complaint immediately even at night.

Speech layout and cooldowns use presentation time rather than simulation speed.
The director rotates between eligible residents, prioritises actionable problems
and leaves room for a positive reaction. It tries non-overlapping screen-space
positions and defers messages without room. Off-screen/deactivated people and
resolved complaints clear promptly, which can shorten a complaint's display.
Bubbles and the inspector do not intercept construction or camera gestures.
Speech display is transient and changes no authoritative record.

Evening eligibility is checked once per day. A dedicated seeded event stream
selects whether the eligible gathering happens. Invited residents walk to the
existing green; a gold outline and label mark the gathering only after enough
arrive. Actual attendees receive the temporary benefit once. Late arrivals may
join within capacity. Timeout, removal or lost attendance ends it safely; already
legitimate benefits expire normally. Event identity, awards, RNG and cooldown
survive reload and construction Undo, so restoring a green does not replay an
event reward. There is one event type and no event-choice dialog.

## Persistence and migration

The primary key stays `roag.settlement.landscape.v1`; current checkpoints are
**schema 3**. Schema 1 and 2 villages retain every original lot field, layout,
scenery, trace and cottage variant. Schema-1 homes receive the existing stable
`legacy-x-y` identities; schema-2 IDs are retained. Migrated residents initialise
at home with their former cream/cyan appearance, neutral mood and grace. The
200-coin migration treasury is initialised once, not added again to a current
checkpoint.

Before overwriting a legacy browser save, the bridge preserves its original
string at `roag.settlement.landscape.v1.before-colony-v3`. If backup storage fails,
the old primary remains protected and the temporary migrated village can be
exported. Imported files are validated before replacing the current scene.

Schema 3 retains the world fields and stores garden/green kind and identity in
each lot. Its `colony` checkpoint holds treasury, fractional grant/work carry,
clock/remainder, speed and Unlimited settings, active/inactive residents,
appearance/ownership, routes/activities, assignments/progress, relevant mood
experiences, and event identity/state/RNG/cooldown/award records. Day and phase
derive from the one stored clock. The historical `nextCottageId` field now
allocates all building IDs and stays monotonic through Undo.

Checkpoints are synchronous: every committed stroke/history/settings change,
about every three foreground seconds while time moves, and on browser
blur/visibility/page-hide transitions. They contain the committed world and
simulation together, never a half-painted world. Stale snapshots are rejected.
A failed write preserves the previous copy and reports `Not saved · export a copy`.
A crash that bypasses page-hide may lose the latest uncheckpointed seconds.
Hidden or unfocused applications pause; elapsed deltas are bounded and there
are no offline earnings or catch-up queues.

## Artwork and implementation owners

No source PNG, catalogue mapping, existing app crop, manifest or audit output was
changed. The narrow review used [nature](contact-sheets/nature.png), the existing
inhabitants sheet, [ASSET_AUDIT.md](ASSET_AUDIT.md) and inventory records.

| Drawing | Existing source / ID / crop |
| --- | --- |
| Garden crops | `assets/urizen/urizen_onebit_tileset__v2d0.png`, `urizen-onebit-r24-c005`, `[66,313,12,12]` |
| Green's tree | Same source, `urizen-onebit-r08-c001`, `[14,105,12,12]` |
| Resident | Same source, `urizen-onebit-r00-c104`, `[1353,1,12,12]` |

The existing CC0 declaration is in `assets/CREDITS.txt`. Beds, miniature benches,
tending motion, harvest rings, gathering outline and speech are derived renderer
operations. The crops are static drawings, not verified growth/harvest frames.
People keep the verified static sprite, with stable cream, cyan, rose or lilac
tints and restrained translation/bob. No walk or construction animation atlas was
invented. Foreground depth sorting, nearest filtering, terrain cache and muted
grass remain from the preceding tranche.

| Owner | Responsibility |
| --- | --- |
| [model.dart](../../apps/settlement/lib/model.dart) | World/graph, place identities, shared validation, budgeted strokes, charged history and forward allocator |
| [colony.dart](../../apps/settlement/lib/colony.dart) | Clock, economy, routine decisions, productive/social validation, mood, gathering, full checkpoint/import |
| [residents.dart](../../apps/settlement/lib/residents.dart) | Existing movement/arrival/settling engine, durable active/inactive resident records |
| [rules.dart](../../apps/settlement/lib/rules.dart), [reasons.dart](../../apps/settlement/lib/reasons.dart) | Tunable rules; structured truthful explanations |
| [speech.dart](../../apps/settlement/lib/speech.dart) | Presentation-only bubble fairness, collision avoidance, cooldowns and culling |
| [settlement_game.dart](../../apps/settlement/lib/settlement_game.dart) | Existing renderer/camera, places, speech, inspection and periodic checkpoint requests |
| [main.dart](../../apps/settlement/lib/main.dart) | Five tools, lifecycle/input, price feedback, compact information and settings |
| [browser.dart](../../apps/settlement/lib/browser.dart), [platform.js](../../apps/settlement/web/platform.js) | Existing file/audio/storage bridge, migration backup and page lifecycle |

## Verification and reproduction

Baseline `fea481df` was clean: **26 existing tests passed**, 24 crops verified,
six README files and four normal Lua storage files fingerprinted before editing.
The old tests now check green visits rather than cottage visits, and exact
physical Undo while explicitly testing the new forward allocator. The history
stress test uses Unlimited building so it still reaches the 80-entry bound.

From `apps/settlement`:

```sh
flutter pub get --enforce-lockfile
python3 -B tool/assets.py --check
dart format --output=none --set-exit-if-changed lib test tool/colony_fixture.dart
flutter analyze
flutter test --concurrency=1
flutter build web --release --base-href /settlement/ --no-web-resources-cdn
dart tool/colony_fixture.dart /tmp/colony-fixtures
```

Final checks pass: **58 tests**, no analyzer issues, clean formatting, all 24
existing crops/manifest/font verified, successful JavaScript release build.
New tests cover capacities, actual productive time, no arrival reward, stable
assignments, real social qualification, grace and mood causes, speech limits and
purity, fixed-budget/cancellation/Undo during income, insufficient-funds Redo,
identity restoration, exact resumed checkpoints, clock/speed/background bounds,
gathering eligibility/attendance/cancellation/reward uniqueness, both legacy
schemas and camera-independent outcomes.

During debugging, a real night-time stale complaint and a mood increase after
cutting a lane were reproduced and corrected. Earlier browser harness failures
were unconnected test frontage and DOM text locators for Flutter's rendered or
disabled text; corrected selectors/targets passed. They were not baseline game
failures. A final rerun hit `Disk quota exceeded` while copying a Flutter test binary; 448.8 MiB of obsolete generated recordings/closed profiles were removed and the suite passed with one test worker. No current settlement test failure remains. The unrelated Lua suite
and remote CI were not rerun; Lua files and storage were verified unchanged.

Browser commands, with the release served and Playwright 1.65.1 available:

```sh
SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/tranche03_browser.cjs /tmp/colony-journey
SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/tranche03_edges.cjs /tmp/colony-edges
SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/browser_test.cjs /tmp/colony-compatibility
DISPLAY=:95 SETTLEMENT_URL=http://127.0.0.1:8765/settlement/ node tool/browser_native_scaling.cjs /tmp/colony-native
```

The harness package/lockfile pins Playwright. Dependencies were installed outside
the repository (`NODE_PATH=/tmp/settlement-browser/node_modules`) to avoid adding
dependency README files. Chromium 155.0.8059.84, Node 26.12.0, Flutter 3.50.0,
Dart 3.14.0 and Flame 1.38.2 were used. The native helper needs an isolated X11
display and xdotool; `CHROMIUM_BIN`/`XDOTOOL_BIN` override their paths.

The main journey used normal Playwright browser mouse/keyboard, menus, downloads
and file chooser, without game callbacks or injected new-game state. It completed
all requested cases: homes/garden/green, visible arrivals/work/income, an
actionable bubble and its construction remedy, real social satisfaction, the
seed-202 gathering, route cut/repair during activity, paid Undo after earnings,
held priced cancellation during earnings, partial affordability and ordinary
income recovery, refresh/export/import, 1×/4× legible feedback, and further input
after pan/zoom/1440×900 resize. It also migrated a comparable 74-home village,
built six gardens/two greens, observed work/social routines and confirmed work
income while the workplaces were off-screen. The final view returned to 1024×640.

Native XTest mouse/keyboard separately verified real **110% browser zoom**, path
painting, priced-stroke cancellation on tab focus loss and Garden/Green placement
with the exact 14-coin charge. CDP enabled accessibility once and observed the
document/checkpoints; it did not emulate focus or call game commands. This is
distinct from Playwright's browser input and from headless model tests.

The existing 22-home/36-road browser compatibility journey passed with simulation
paused and Unlimited building enabled. Separate edges passed priced cancellation,
canvas boundaries, modifier isolation, read-only inspection, input through the
speech layer, audio/mute, DPR 1.5, quota recovery, active reload, legacy backup
and backup-failure protection. Migration profiles and the night-repair check are
explicitly **fixture-assisted**; only their test browser storage/file input was
seeded. User browser profiles were not cleared or overwritten.

The 74-resident release sample with video capture measured approximately 33.4 ms
median and 83.4 ms maximum over 90 animation-frame intervals. This is browser
scheduling on this machine, not physical input/display latency or a 60-fps claim.
The simulation is independent of camera visibility and searches routes at
decisions/topology changes, not each render frame.

## Evidence and fixtures

- [Continuous causal-loop recording](evidence/tranche-03/colony-loop.mp4) (54 seconds): actual
  running release, garden construction → real work/income → leisure need → green
  → social visits → evening gathering. Silent, trimmed only at the ends, no mock
  frames or rearranged sequence.
- [Work need](evidence/tranche-03/02-work-needed.png),
  [working/income](evidence/tranche-03/04-working-income.png),
  [leisure need](evidence/tranche-03/05-leisure-needed.png),
  [gathering](evidence/tranche-03/08-evening-gathering.png),
  [inspection](evidence/tranche-03/11-inspection.png).
- [74-home production/social activity](evidence/tranche-03/13b-large-working.png),
  [final 1024×640](evidence/tranche-03/14-final-1024.png),
  [native 110%](evidence/tranche-03/native-browser-zoom.png),
  [night cut](evidence/tranche-03/night-cut.png) →
  [night repaired](evidence/tranche-03/night-restored.png).
- [Main browser results](evidence/tranche-03/browser-results.json),
  [edge results](evidence/tranche-03/edge-results.json),
  [compatibility results](evidence/tranche-03/compatibility-results.json),
  [native results](evidence/tranche-03/native-results.json),
  [release fingerprint](evidence/tranche-03/release.json).

[colony_fixture.dart](../../apps/settlement/tool/colony_fixture.dart) reproducibly
constructs a paid four-home/garden/green neighbourhood using normal model
operations, then advances normal simulation. It produces
[start](evidence/tranche-03/colony-start.json),
[evening-ready at second 85](evidence/tranche-03/colony-evening-ready.json),
[actual gathering](evidence/tranche-03/colony-gathering.json) and
[night cut](evidence/tranche-03/colony-night-cut.json) checkpoints. Import through
the normal menu; choose 1× or 4× to continue. Use the second-85 checkpoint to watch the gathering develop; the later checkpoint records its completed attendance. No forced event or reward exists in the playable app. The gathering fixture recorded four actual attendees.

The [74-home schema-2 fixture](evidence/tranche-03/legacy-74.json) was generated
using the previous model from baseline `fea481df`; it is not the user's private
browser save. Existing-format layout preservation was checked both headlessly
and through real browser import/autoload. Schema-1 coverage uses the original
tranche-1 exported village.

## Preservation and limits

All six README files retained their initial hashes; none was added or edited.
Root README SHA-256 before and after:

```text
3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
```

All original tracked source/documentation files outside the app remained
unchanged. The existing local release archive was intentionally refreshed at
`artifacts/settlement/roag-settlement-web.zip`. Original asset/catalogue/audit
bytes and all four normal Lua storage names/hashes remain equal. See
[preservation evidence](evidence/tranche-03/preservation.json). Nothing was
published, and test contexts used separate temporary browser profiles.

This is an initial rule set, not a claim of balanced or enjoyable play. Gardens
reserve jobs even during breaks; dense villages can generate many genuine
vacancy requests despite the limited bubble display. People can overlap on
crowded miniature roads/greens and have no collision management. Artwork remains
static with derived movement, not an authored walk/growth cycle. Recent positive
speech may be suppressed by cooldown/space; a resolved complaint can close early.
History is session-local, and older schema-1/2 applications cannot read schema 3.
Mobile usability, other browser engines, very large imported maps, subjective
audio quality and remote CI remain unverified. Automated routine checks do
not replace human preference testing.

Human playtest questions:

- Did the residents’ bubbles help me understand what to build?
- Did solving a need visibly change their behaviour?
- Did money guide expansion without repeatedly stopping the fun?
- Did the daily rhythm make the village enjoyable to watch?
- Did the gathering feel like something my settlement made possible?
