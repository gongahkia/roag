# Conversations, civic choices and moving buildings

The settlement remains a construction-led colony. Residents choose their own
work and visits. This tranche adds optional conversations and three open promises,
a one-time Market-day decision, free building relocation, and real sprite previews.
There are no individual orders, quests screen, dialogue parser, external AI,
new resource types or fantasy-species mechanics.

## Local play

From the repository root, if a local server is not already running:

```sh
python3 -B apps/settlement/tool/serve.py --port 8765
```

Open or hard-refresh <http://127.0.0.1:8765/settlement/>. Port 8765 already hosts
the app on the verification machine; a second server is unnecessary. The rebuilt
local archive is `artifacts/settlement/roag-settlement-web.zip`. Nothing in this
tranche is deployed or published. Test profiles use a separate loopback origin
and fresh browser storage; the player's private town is not opened or reset.

## Controls and construction art

`B` opens the existing grouped catalogue. Every buildable entry has a real runtime
composition: Cottage, Farm, Park, Workshop, Market, Community Hall, Dirt Path,
Stone Road and Garden Path. Names, prices, short purposes and the connected-path
requirement remain readable. The selection has a check and accent; the nine
choices live in a scrolling 360-pixel popup, not a permanent sidebar.

The original `1/C`, `2/P`, `3/R`, `4/G`, `5/V`, and new `6` through `0` shortcuts
retain their meanings. Drag painting, stroke budgets, Escape cancellation,
Ctrl/⌘ Z and Shift Z, pan/zoom, H and speed controls remain available. Footer
tools now use the same construction art, with the original clear icon for Remove.

`BuildArt` is shared by the world, placement ghosts and `BuildThumbnail`. Existing
frame drawing, semantic tint, Farm beds, Park benches, Market awning and road
paving were extracted without creating a second crop implementation. Thumbnails
use the already-loaded runtime sprites, nearest filtering and square native
proportions; 36-pixel catalogue images are 3× the original 12-pixel miniatures.
Cottage 0 represents the two unchanged roof variants. Roads show a connected
horizontal lane with the same mask/variant selection and surface treatment as
world roads. Thumbnails do not show fictional working residents or animation.
The existing previews now draw all current road and building kinds.

The treasury uses the audited **`godot-white-coin`** source,
`assets/godot-node-icons/white/icon_coin.png`, full rectangle **[0,0,16,16]**,
SHA-256 `9801557ba43d134a4d7ce75288ceff44b05263dd536cf533d02085b8759d0226`.
It is copied by the existing exporter, tinted gold and drawn with nearest filtering.
The source sheet/image is unchanged. Pixel-boy's CC0 credit is retained and the
app attribution now names coin alongside hammer/grid/clear. Existing Urizen crops,
palettes and compositions retain the exact provenance in the manifest and
[Tranche 05](TRANCHE_05.md). No new asset pack or species recolour was introduced.

The numerical amount, fractional accounting and grant line are unchanged. An
accessible treasury label still names coins. The icon adds no resource. The money
tooltip also identifies cumulative civic spending when there has been any.

## Free relocation

Use `I` or Alt-click to inspect a building, then **Move building · Free (M)**.
`M` also starts a move from building inspection. A short right-click on a building
offers Inspect and Move; a right-drag beyond five pixels continues to pan. Roads
stay under the normal painting tools. Right-click is a shortcut, not the only route.

Point at a destination to see the actual ghost, footprint, changed road spurs and
validity. Click to commit. Escape, tool/menu changes and focus loss cancel.
Panning/zooming can retain the selected move. After commitment, ordinary building
controls work immediately with the previous tool and a fresh pointer gesture.

Cottages, Farms, Parks, Workshops, Markets and Halls all move for **zero coins**.
The final layout uses normal placement validation after vacating the original
one-cell footprint. Occupied plots, roads, trees, out-of-bounds positions and
missing connected access are rejected. Replaceable plants remain replaceable.
Preview changes neither world, IDs, money, clock nor RNG. Final execution checks
the current world again and rejects a stale plan.

The building ID, type and cottage variant are retained; moving never allocates a
new resident or building ID. Destination ground comes from that plot. The old
site retains its ordinary removal pad. The complete before/after worlds form one
zero-charge history entry: Undo restores the original site and the destination's
former grass/scenery/trace exactly; Redo reapplies the move. Both preserve forward
clock, earnings, requests, civic choices and the ID high-water mark. No placement
refund or construction reward is issued by relocation.

A person keeps their home ID, name, body, palette, trait and past experiences.
Legal underway edges can finish with their recorded duration. The new layout
invalidates navigation once and uses the existing bounded replan queue. Moving
an occupied workplace/social venue stops invalid productive presence immediately,
before the post-edit checkpoint. Queued work belonging to removed residents is pruned without changing surviving queue order/revisions; visitors remain at their actual positions and
reroute, return or wait. Travel is never counted as productive work.

That immediate validation is a narrow shared correction: a moved/removed
occupied destination cannot be saved as a worker still working at its old access.
Removing the assignment reports missing work rather than an imaginary unreachable
assignment. All previous valid progress/earnings remain; a later arrival itself
still pays nothing. A moved active gathering venue ends attendance safely while
retaining event identity, awards and remaining legitimate benefits. Undo does
not restart that gathering. Global Rain/Harvest/Market-day timing is not rerolled.

## Short contextual conversations

Inspect a visible resident with `I` or Alt-click, then choose **Talk**. Repeat
clicks still cycle overlapping people. The compact scrolling panel shows the
same sprite/name/trait, current activity, one short authored line and relevant
choices. Close/Maybe later returns to inspection; **Back to building** leaves
inspection. Escape dismisses the interaction. Choosing a construction tool also
closes conversation; the canvas outside the panel and existing camera controls
remain usable. The speech director avoids the panel's screen region.

The normal world clock continues while a modeless conversation is open, unless
the player paused it. Viewing/closing lines has no action-time charge and never
calls a simulation step, assigns work, improves mood or pays money. Consequential
choices recheck the current situation at one authoritative boundary. A situation
or event that changes while the panel is open can make a stale choice unavailable.

The small catalogue has stable nodes (`hello`, `mood`, `recent`, `offer`,
`promised`, `declined`, `withdrawn`, `civic`, `civic-done`) and stable choice IDs,
condition predicates, authored lines and explicit effects. It is in `dialogue.dart`,
separate from widgets. Typical paths are greeting → mood/recent experience → need
→ promise/decline → close. Choices are evaluated only for the selected conversation.
No population-wide dialogue-tree search runs each frame.

Lines use actual records: a failed job search, cut assigned workplace, actual
productive work, completed social visit, credited gathering attendance, current
activity and happiness. Sociable wording can emphasise its real recent social
visit. No invented profession, family, belongings or relationship is assigned.
A resolved request gives a truthful later acknowledgment without issuing another
benefit merely because the player reads it.

## Optional resident promises

The player may accept a request only when the selected resident genuinely has:

| Concern | Acceptance condition | Real completion |
| --- | --- | --- |
| Suitable work | Arrived resident with an actual failed search, no assignment and no reachable available job | Their own validated productive worker tick |
| Work connection | An existing assignment with missing access or an unreachable route | Their own validated productive worker tick after regaining suitable work |
| Social space | An actual missing/cut social concern and no reachable suitable venue | Their own completed qualifying social visit |

There are at most **three open promises**. Nothing generates a request automatically.
Declining changes no authoritative state and has no penalty. An accepted promise
has a stable `request-N` ID, resident ID, need, acceptance time and status.
Inspection shows promised/helped/withdrawn state; **Locate request** centres the
relevant home or assigned workplace. The player can withdraw a promise through
conversation. There is no deadline or expiry pressure. Removing its home withdraws
an open promise; Undo restores the person but does not rewind that conversation.

A construction operation alone never completes a promise. Placing a distant or
unrelated Farm does not solve inaccessible work; an actual resident must reach
valid work. Visiting travel/arrival does not satisfy social space; service/visiting
time must qualify under the existing model.

Completion gives **+5 gratitude for 75 simulation seconds**, once per resident/need
for this run. Multiple gratitude records do not stack beyond +5. A later distinct
fulfilled need can refresh the active source. Status and exact reward expiry
persist. Resolved/withdrawn concerns are not repeatedly offered as new promises;
reloading, moving/replacing a destination, repeated conversations and construction
Undo/Redo cannot reissue that reward. An acknowledgment is available in Talk,
and the existing speech system can show the authoritative thanks cue.

The request book indexes the three need types by resident and keeps an open map
bounded to three. Only real experience hooks resolve open records. Inspection
and mood query bounded per-person records; historical requests are not searched
as dialogue trees every render frame.

## One civic choice: Market day

During an active, currently eligible Market day with a present vendor and available residents, inspect its persistent event label and
choose **Discuss Market day**. A real present vendor hosts the conversation.
The same topic can appear in a deliberately opened resident conversation. There
is no automatic popup, mandatory pause or repeated demand for attention.

- **Sponsor · 3.0 coins:** pay exactly 3 coins once, from the existing treasury.
  The next **six distinct qualifying served visitors** during that event receive
  **+5 Market hospitality for 75 simulation seconds**. This is a mood benefit;
  ordinary base trade, the normal Market-day trade bonus and vendor income stay
  unchanged. Completed eligible service is required. Already-credited daily visits
  cannot claim hospitality through an uncredited repeat transaction.
- **Let it happen normally · Free:** record the ordinary choice; all existing
  service and trade rules continue, with no hospitality benefit or charge.
- **Maybe later:** close without choosing or spending.

The panel states cost, cap, duration and the need to finish service before the
Market day ends. Insufficient funds disable sponsorship with a reason and leave
the ordinary option available. Late sponsorship can benefit fewer than six or
nobody; it does not manufacture customers. Losing all available residents hides the decision. A missing vendor/destination can lapse
service; it cannot reroll the selected event or replay a fee/award.

`chooseCivic` checks the event identity, active Market day, real host, previous
choice and funds. Payment is separate from construction history; Unlimited
building does not waive it. Current-event choice/credited guest IDs and each
recipient's remaining benefit persist. A compact event-ID decision ledger retains
prior choices when later special events replace the current one. Cumulative
`civicSpent` is an accounting total, not another resource. Reopening, import,
reload and Undo cannot spend again for the same event or rewind a prior decision.

## Saves, ownership and future peoples

The existing schema-3 storage key and legacy backup/error protections remain.
All additions are optional: old towns get no fabricated requests, past choices,
rewards, charges or dialogue sessions. Building/resident IDs, existing body/name/
trait fields, money, existing participation and landscape traces retain their
meanings. Conversation node selection is transient; explicit consequences are
saved synchronously. Import still validates before replacing the current town
and monotonically rebases navigation. Existing construction history remains
session-local and clears on reload/import, as it did before this tranche.

New persisted fields are the request book/counter/status/times, civic decision
ledger/spending, current-event choice/guest set and per-resident hospitality expiry.
Their owners validate IDs, duplicate concerns/guests, active limit, times, bounded
rewards, event decisions and required spending. They are not part of world Undo.
No migration replays old days or decisions.

| Owner | Responsibility |
| --- | --- |
| `lib/model.dart` | Pure final-layout move validation; free atomic history transaction |
| `lib/colony.dart` | Actual need predicates, experience-based resolution, civic fees/participation, immediate occupancy validation, full checkpoint validation |
| `lib/resident_request.dart`, `lib/dialogue.dart` | Small request records/index and data-driven authored conversations/explicit commands |
| `lib/residents.dart`, `lib/special_event.dart`, `lib/rules.dart`, `lib/reasons.dart` | Existing actors/events plus bounded benefit state, rules and truthful thanks cue |
| `lib/build_art.dart`, `lib/build_thumbnail.dart` | Shared runtime compositions and sprite catalogue widget |
| `lib/main.dart`, `lib/settlement_game.dart` | Compact interactive panels, Move/input, ghosts, camera locating and existing speech avoidance |
| `tool/assets.py`, `assets/manifest.json`, `assets/ATTRIBUTION.txt` | Existing verified exporter plus audited coin copy/credit |
| `test/tranche06_test.dart`, `tool/tranche06_*` | Focused verification, safe fixtures, real browser/native journeys and no-choice replay |

Paths above are relative to `apps/settlement/`. Shared changes are limited to the
needed renderer extraction, move transaction, valid post-edit occupancy and new
explicit consequences. The coordinated clock, queue budget, weighted navigation,
resident atlas and off-screen simulation remain in place.

No species was implemented. Existing identity is independent of `bodyId`, palette
and trait, so it can retain stable IDs through a future people identifier. The
current appearance registry has four explicit versioned body IDs, fixed palettes
and a plain-body fallback for unknown IDs; the atlas layout assumes those four
human silhouettes. A future people requires a validated separate sprite registry,
asset/codec versioning and actual rules before behaviour changes. One-resident-per-
cottage ownership is another present simplification. Recolouring these humans
would not establish a new species. No speculative refactor was made for that work.

## Verification

Baseline `b5f7ea543c75503e241d649d6d296a6d5739e958` had a clean worktree and
**93 passing tests**. The final suite has **118 passing tests**, including 25
focused new contracts. Analysis and formatting are clean; all 28 source crops,
resident atlas/variants, manifest and unmodified font verify. The root art check
still validates 599 explicit bindings from 67 unchanged source PNGs. The local
JavaScript release builds successfully. Wasm was not a separate tested release.

The new tests cover all six relocation kinds, actual scenery restoration,
collisions/trees/access/stale plans, free history, same existing resident, occupied
work/social destinations, active gathering movement, valid immediate checkpoints,
real conditional dialogue, no viewing/decline consequences, three-request limit,
withdrawal, actual work/visit completion, one-shot gratitude, event branches,
insufficient funds/no available visitors, the real six-person hospitality cap,
remaining benefits/past decisions across later events, strict save validation and
new state surviving history/import/reload.

A no-choice replay is **byte-identical to baseline**: 85 real residents, 180
simulation seconds, 36 complete checkpoints including all saved actor/world/time/
RNG/progression fields and computed individual moods. Both output SHA-256 hashes:
`5c447d730444836ae8e5a9bf0b8911fa585f92c67a15d67ef5b6784cb3766fec`.
This verifies ordinary simulation when no dialogue decision is made; it does not
claim that deliberate sponsorship/promise benefits leave happiness unchanged.
Viewing relevant branches and repeatedly inspecting also leaves authoritative
state unchanged in focused tests and paused browser checks.

The main browser journey uses seed 202 and a safe 85-person fixture. It inspects
the sprite catalogue, previews/cancels/rejects/commits a cottage move, observes the
same resident return to its new home, undoes/redoes, moves an actual productive
workplace through the context menu, observes work resume, reloads and exports/
imports. It then follows two contextual branches, declines without effects,
accepts a genuine work need, builds four real job slots, observes that specific
person work and acknowledge the improvement, and continues painting normally.

Both civic branches use separate imports of an explicitly eligible Market-day
fixture. The journey checks exact payment, no repeated payment on reopening,
actual qualifying service/benefits, history, reload and real file export/import.
A town left with only its vendor offers no civic decision. A low-budget fixture
exhausts funds through **actual paid Stone construction**, rather than changing
currency arithmetic; its sponsorship control is disabled with an honest reason
and its ordinary choice remains free. The long decision panel stays usable
through pan/zoom and 1440×900, 1024×640 and 931×582 CSS viewport changes, with no
paused gameplay-state changes.

**Native X11/XTest** separately exercises actual mouse/keyboard input: real
Ctrl-plus to 110% browser zoom, four-home drag, inspection Move and M, ghost and
Escape, free identity-preserving move, Undo/Redo, right-click Move, read-only
decline, acceptance, two-Farm painting, actual work and truthful thanks, catalogue
scrolling, currency and footer bounds. Its original content viewport is 1024×640;
real browser zoom produces 931×582 CSS pixels at DPR 1.10000002384. The extra
931×582 long-civic-panel check is scripted CSS geometry, not another native-zoom
claim.

Native commands run in a fresh Chromium profile under Xvfb. Raw CDP observes
screens/checkpoints and enables accessibility once; game commands are XTest.
Main/story journeys use Playwright mouse/keyboard, real menu/download/file chooser
flows and read-only checkpoint assertions. They do not invoke game callbacks.
A focused UI check also uses Locate request to centre the correct existing home, proves that identity by selecting/moving its centred roof, and withdraws the actual promise without cost, time or reward. Model tests and fixture generation are separate headless checks. None of these
is described as human manual play or preference testing.

The existing compatibility, edge and prior-content journeys pass: brush budgets,
hover/cancellation, exact layout/history, pan/zoom/resize, audio/mute, storage
failure, migration/failed backup, active reload, work/service/social activity,
naturally scheduled Rain, fixture-assisted Harvest/Market day and overlap cycling.
The earlier inspector selector was updated to read ordinary semantic text as well
as aria labels inside the new scrollable panel; identity/outcome assertions were
retained. No game rule was relaxed to accommodate selectors.

A real paused-demolition bug was found by the additional civic eligibility test.
Multiple removed homes could leave their owners in the planning queue, so the
world rejected its own checkpoint. A separate extraction of the **baseline source**
reproduces it: one active resident, four queued requests, `Invalid planning queue`.
The fix prunes only removed owners, retaining surviving order/revisions. Strict
import validation stays intact. The new regression checks every demolition's
immediate round-trip and Undo; the actual browser import now passes. This is a
verified baseline bug, not attributed to an earlier report.

Early new-harness failures also assumed a particular resident would win one of
two job slots despite other residents' autonomous choices. The fixture now builds
four slots for four people and still requires the promised person to benefit.
Native helpers wait for actual semantic controls after popup dismissal, rather
than treating a transition frame as a missing button. The capture helper retains
the pointer for ghost screenshots so they show an actual aimed preview. No
additional gameplay wait was introduced. No required check remains failing.

### Reproduce

From `apps/settlement/`, with the pinned SDK and browser test dependencies:

```sh
flutter test
flutter analyze
dart format --output=none --set-exit-if-changed lib test tool/tranche06_fixture.dart tool/tranche06_replay.dart
python3 -B tool/assets.py --check
flutter build web --release --base-href /settlement/ --no-web-resources-cdn
dart run tool/tranche06_fixture.dart
python3 -B tool/serve.py --port 8768
```

Use a second terminal for the browser journeys:

```sh
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche06_browser.cjs /tmp/tranche06-browser
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche06_story.cjs /tmp/tranche06-story
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche06_locate.cjs /tmp/tranche06-locate
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/browser_test.cjs /tmp/tranche06-compatibility
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche03_edges.cjs /tmp/tranche06-edges
SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche05_browser.cjs /tmp/tranche06-previous-content
DISPLAY=:98 SETTLEMENT_URL=http://127.0.0.1:8768/settlement/ node tool/tranche06_native.cjs /tmp/tranche06-native
dart run tool/tranche06_replay.dart /tmp/no-choice-current.json
```

The native helper needs Chromium, xdotool and an isolated X11 display. This machine
used Flutter 3.50.0 / Dart 3.14.0 from `/tmp/flutter/bin`, Chromium 155.0.8059.84,
Playwright 1.65.1 via `NODE_PATH=/tmp/settlement-browser/node_modules`, and
`XDOTOOL_BIN=/tmp/roag-runtime/usr/bin/xdotool` with its local runtime library path.
No project dependency changes or dependency README files were installed here.
The fixture tools use explicit `/tmp/roag-tranche06-fixtures` paths and are not
playable event controls. Civic fixtures call the established test-only event
entry after actual work/service eligibility. The readable story uses a four-person
genuine-need fixture with events held outside its recording window; discussion
and construction start paused, actual productive work runs at 1×. Reading holds
exist only in the recording script, not in the game.

## Performance observations

The release was measured before and after on the same Linux machine, Chromium
155 software rendering, 1024×640, using the existing opt-in profiler and the same
connected 85/250/500-resident fixture files. They contain real homes/actors,
615 connected Dirt cells, ten Farms and five Parks; no population counter is
inflated. Events are held beyond this comparison window in both fixtures.
Each case samples 16 wall seconds ordinarily and 16 after a 13-cell road removal.
No compilation, browser journey or video encoding ran alongside the final sample.

| Residents / phase | Before frame median / p95 ms | After frame median / p95 ms | Before model mean ms | After model mean ms |
| --- | --- | --- | --- | --- |
| 85 / ordinary | 16.7 / 16.8 | 16.7 / 16.8 | 0.115 | 0.105 |
| 85 / recovery | 16.7 / 16.8 | 16.7 / 16.8 | 0.140 | 0.108 |
| 250 / ordinary | 16.7 / 49.9 | 16.7 / 16.8 | 0.444 | 0.245 |
| 250 / recovery | 16.7 / 16.7 | 16.7 / 16.7 | 0.239 | 0.214 |
| 500 / ordinary | 16.7 / 33.3 | 16.7 / 16.8 | 0.559 | 0.411 |
| 500 / recovery | 16.7 / 16.8 | 16.7 / 33.3 | 0.437 | 0.426 |

All measured medians remain 16.7 ms. No material model-cost increase was observed.
Frame tails vary: 500-person recovery p95 rose from 16.8 to 33.3 ms, while its
ordinary p95 fell from 33.3 to 16.8 ms. The raw samples are retained rather than
selecting only favourable numbers. These shared-machine observations do not
prove identical frame latency or a universal frame-rate improvement. Model means
are per `Game.update` call, including frames with no fixed simulation tick;
this is not physical input/display latency.

The final 500-person recovery advances 16.00 simulation seconds in the sample,
drains its queue from a peak of 429 to zero, and ends at 13,980 cumulative route
queries, 1,046 built fields, 12,409 cache hits and 626,344 cell expansions. The
existing eight-request budget, 96-field cache, fixed clock and off-screen model
remain. Dialogue is evaluated on demand for one selected actor; request hooks
query the small indexed concern set, not full trees across the town. Real populated
UI journeys additionally exercise the new panels and moves at 85 residents.

[Before measurements](evidence/tranche-06/performance-before.json) and
[after measurements](evidence/tranche-06/performance-after.json) contain frame,
model, presentation, queue and navigation data. Reproduce with the existing
`dart run tool/scaling_fixture.dart`, then
`SETTLEMENT_URL='http://127.0.0.1:8768/settlement/?profile=1' node tool/scaling_browser.cjs /tmp/scaling.json`.
Timing assertions are deliberately absent from unit tests.

## Evidence

- [19-second request → choice → construction → actual work → acknowledgment](evidence/tranche-06/request-choice-build-response.mp4).
  Continuous running-game footage, with only its ends trimmed (source seconds
  3.4–22.4), no cuts, mock frames or speed changes. The capture script holds lines
  briefly for reading; these holds are not gameplay delays. A paused four-person
  fixture supplies a genuine failed work search. Both Farms are paid for, and
  actual work runs at 1× before that person's truthful response.
- [33-second populated relocation journey](evidence/tranche-06/populated-relocation.mp4).
  Continuous source seconds 7–40: 85 residents, real aimed ghost, free cottage
  move, the same resident returning, workplace relocation and resumed work.
- [Consecutive rendered request frames](evidence/tranche-06/request-motion-frames.png),
  extracted every two seconds and inspected to confirm actual motion/consequences.
  Recordings are silent MP4 conversions of browser video; they do not claim
  manual human play or physical audio listening.
- [Building catalogue](evidence/tranche-06/catalogue-buildings.png),
  [road catalogue](evidence/tranche-06/catalogue-roads.png),
  [relocation preview](evidence/tranche-06/relocation-preview.png),
  [same resident at the moved home](evidence/tranche-06/same-resident-new-home.png),
  [right-click menu](evidence/tranche-06/building-context-menu.png),
  [workplace preview](evidence/tranche-06/workplace-preview.png),
  [work resumed](evidence/tranche-06/work-resumed.png).
- [Conversation](evidence/tranche-06/conversation.png),
  [real work need](evidence/tranche-06/work-request.png),
  [decline](evidence/tranche-06/declined.png),
  [promise](evidence/tranche-06/promise.png),
  [located request](evidence/tranche-06/request-located.png),
  [withdrawal](evidence/tranche-06/promise-withdrawn.png),
  [actual acknowledgment](evidence/tranche-06/acknowledgment.png).
- [Ordinary event decision](evidence/tranche-06/ordinary-decision.png),
  [sponsored decision](evidence/tranche-06/sponsored-decision.png),
  [actual served visitors](evidence/tranche-06/sponsored-service.png),
  [no available participants](evidence/tranche-06/no-customers.png),
  [insufficient funds](evidence/tranche-06/insufficient-funds.png),
  [long civic panel at 931×582 CSS](evidence/tranche-06/civic-931.png),
  [civic panel at 1440×900](evidence/tranche-06/civic-1440.png).
- [Native 110% ghost](evidence/tranche-06/native-move-ghost-110.png),
  [native need](evidence/tranche-06/native-request-offer-110.png),
  [native thanks](evidence/tranche-06/native-thanks-110.png),
  [native catalogue/roads](evidence/tranche-06/native-catalogue-110-roads.png),
  [native settled UI](evidence/tranche-06/native-settled-110.png).

Results: [full tests](evidence/tranche-06/tests.log),
[baseline tests](evidence/tranche-06/baseline-tests.log),
[analysis](evidence/tranche-06/analyze.log), [release build](evidence/tranche-06/build.log),
[main browser journey](evidence/tranche-06/browser-results.json),
[native input](evidence/tranche-06/native-results.json),
[readable story](evidence/tranche-06/story-results.json),
[request locating/withdrawal](evidence/tranche-06/locate-results.json),
[existing compatibility](evidence/tranche-06/compatibility-results.json),
[persistence/edge cases](evidence/tranche-06/edge-results.json),
[previous content/events](evidence/tranche-06/previous-content-results.json),
[reduced-motion/event inspection](evidence/tranche-06/settings-results.json),
[no-choice equivalence](evidence/tranche-06/equivalence.json),
[compressed baseline checkpoints](evidence/tranche-06/no-choice-baseline.json.gz),
[baseline queue bug reproduction](evidence/tranche-06/baseline-queue.log).

The [baseline queue repro](evidence/tranche-06/baseline-queue-repro.dart) runs with
`colony`, `model`, `residents`, `rules`, `reasons`, `navigation`,
`resident_identity`, `special_event` and `appearance` Dart files extracted from
baseline commit `b5f7ea543c75503e241d649d6d296a6d5739e958` into the same temporary
directory. Generate the current safe fixtures first; the repro imports their
pre-choice Market-day checkpoint, removes all homes except the actual vendor's
through normal authored operations, then tries its own immediate round-trip.
No checkout/reset or README write is needed.

Safe fixture and result exports accompany this evidence: the 85-person town,
four-person need town, eligible Market day, no-customer and low-budget variants,
and post-relocation/request/civic checkpoints. They are developer verification
copies, not the user's private settlement.

## Protection and remaining limits

All six README.md paths and SHA-256 hashes match the initial checkout. Root hash:
`3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a`.
The four normal Lua storage files and file set also match. Original root source
artwork, Lua code/launchers, catalogue/audits and every original app image crop
remain byte-identical. The app attribution/manifest add the coin; original source
images are not edited. [Final protection fingerprints](evidence/tranche-06/protection.json)
record the comparison and the local archive/served-release match.

All browser tests use fresh isolated contexts/profiles on the test origin.
Normal player browser storage was not opened, reset or overwritten. No hosting
or publication tool was used. The existing local player server remains available;
verification-only servers/windows can be closed without affecting it.

No known required failure remains. Tested platform: Linux/Chromium with the
pinned Flutter/Flame stack. Safari, Firefox, mobile/touch conversation ergonomics,
physical display latency and human preference/balance remain unverified. Dense
sprites still overlap; inspection cycles their actual nearby identities rather
than adding crowd physics. Static source art still uses procedural activity
motion, not new authored animation sets.

These are small authored conversations and can repeat. Promises do not reserve
jobs for a named person: someone else can take a new slot first, so resolution
may require more capacity or access. That preserves resident autonomy; there is
no deadline or failure penalty. A withdrawn/resolved concern is one-shot for that
person/run. Late civic support can help fewer than six people, or nobody, and
its three-coin value needs human evaluation. The observed frame-tail variability
is retained above. Additional peoples, seasons, major incidents, narrative
campaigns and building families remain outside this tranche.

Human playtest questions:

1. Does selecting and moving buildings feel immediate and convenient?
2. Can I understand what I am placing from its sprite preview?
3. Do conversations make individual residents feel more memorable?
4. Do dialogue choices create worthwhile consequences without becoming micromanagement?
5. Do I naturally want to respond to a resident by improving the town?
6. Can I keep building freely without conversations interrupting me?
