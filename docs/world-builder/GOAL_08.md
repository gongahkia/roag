# Goal 08 — A living fantasy settlement

Local play, from the repository root:

```sh
cd apps/settlement
flutter pub get --enforce-lockfile
flutter build web --release --base-href /settlement/ --no-web-resources-cdn
python3 -B tool/serve.py --port 8765
```

Open `http://127.0.0.1:8765/settlement/`. If 8765 already serves the game, reuse it or choose another port; do not terminate an unidentified listener. Production uses the existing [GitHub Pages address](https://gabrielongzm.com/roag/settlement/). Publication verification is recorded below.

## Player experience and controls

The main interaction remains construction. Drag cottages beside connected paths; residents arrive, work and socialise themselves. B opens all existing building, road and Landscape tools. I inspects a resident, building or terrain; Talk opens an optional conversation. M relocates an inspected building for free. Escape cancels or closes. Ctrl/Cmd+Z and Shift+Ctrl/Cmd+Z undo/redo construction. Right drag or Space+drag pans, the wheel/buttons zoom, H returns to the lane. Existing 1–0 construction shortcuts and pause/1×/2×/4× controls remain.

The date control now shows season and current weather on a second line. Click it, or choose **Calendar & forecast** in the existing menu, for the next day's actual scheduled weather, effects and simple rules. **Watch a year · 16×** is an optional menu speed; any ordinary speed button exits it. The default remains 1×. A complete 24-day year takes about 225 seconds at 16×, excluding pauses and foreground-frame limits.

New homes attract Humans, Imps and Djinn. Inspect or talk to see their people and individual trait. A merchant or storyteller appears only when a suitable real venue has access. Their small activity buttons locate and inspect the actual guest; they do not issue movement orders. Guests can also be selected in the world. Construction continues during visits and conversations. The storyteller's optional **Sponsor an encore · 2 coins** extends the current performance by six simulation seconds; it does not directly award happiness.

## Baseline and integration

The inspected starting main was `474a32a7c1593224a40a0accfc28399298a0b2a6`; the Goal 07 release branch was `d9079a3ca7667898fff0a67dbfcdfbd5e336ff71`. Their settlement application and Pages workflow were byte-identical despite divergent histories. The working tree was initially clean. Work continued on `feat/settlement-goal-08`, without merging, resetting or rebasing unrelated histories. The workspace's automatic `work~` commits were retained.

The current baseline passed **134 Flutter tests**. Catalogue tests passed 47/48; `test_all_tracked_media_are_originals_or_explicit_references` already failed for tracked application/evidence PNGs before Goal 08. This is a catalogue-wide tracked-media allow-list issue, separate from the settlement's reproducible asset exporter. The original failure was captured before editing, rather than inferred from earlier reports.

Toolchain: Flutter **3.50.0**, Dart **3.14.0**, Flame **1.38.2**, material_ui **1.11.0**, Chromium **155**, Playwright **1.65.1**. The existing lockfile and CI Flutter pin remain authoritative. No additional runtime package or cloud service was introduced.

## Calendar, seasons and weather

There is still one authoritative `Colony.clock`, a 150-second day and a fixed 0.05-second simulation step. Existing Morning/Day/Evening/Night routines, foreground delta limit, background-tab pause, events and construction economy remain. Calendar version 1 defaults to six days per season and supports a persisted 2–12-day configuration. A year is four seasons. Calendar arithmetic and weather inspection do not consume any mutable random stream.

| Season | Actual Farm output | Ordinary future leisure decisions | Ground/vegetation |
|---|---:|---|---|
| Spring | ×1.10 | Existing routines | Restrained audited flower details |
| Summer | ×1.00 | Leisure repeat interval 18→14.4 seconds; Parks preferred in suitable weather | Existing green landscape |
| Autumn | ×1.15 | Good Harvest receives three eligible event tickets instead of the ordinary one | Warm canopies and audited fallen leaves |
| Winter | ×0.80 | Covered Halls preferred | Frost-coloured grass and the audited leafless tree drawing |

Spring has two Good Harvest tickets; Summer/Winter one. Market day retains one ticket when eligible. Automatic special events no longer select Rain: **Climate owns all weather**. The old Rain event identifier and entry API remain for legacy checkpoints and deterministic tests. Calling that API creates a bounded 25-second rain window in the same Climate owner; its expiry returns to the scheduled condition. It is not another weather simulation.

Weather version 1 chooses one condition per absolute simulation day. Each day hashes `(seed + day×104729 + 15485863) mod 2147483647`, then applies two Park–Miller multiplications by 48271. The result samples these cumulative weights:

| Season | Clear | Rain | Windy | Snow |
|---|---:|---:|---:|---:|
| Spring | 45 | 35 | 20 | 0 |
| Summer | 65 | 15 | 20 | 0 |
| Autumn | 40 | 35 | 25 | 0 |
| Winter | 35 | 10 | 20 | 35 |

River Country adds five Rain tickets; weights are normalised by their sum. Geometry, vegetation, existing events, visitors, people identity and cosmetics do not draw from this day stream. Current and next scheduled conditions are persisted and checked against the versioned algorithm. Forecasts read that schedule, including any remaining carried-over legacy shower, rather than making another random choice.

Actual Farm pay is:

`productive seconds × ordinary Farm rate × clamp(season × weather × GoodHarvest, 0.65, 1.50)`

Weather Farm factors are Clear/Rain 1, Wind .95, Snow .90; Good Harvest is 1.25. The factor applies once in the existing productive-pay pipeline, with the existing fractional milli-coin accounting. Travel, assignments, ownership and previews pay nothing. Workshop and Market vendor rates remain unchanged.

Dirt/Garden road-cell travel costs are `max(terrain, weather)`: ordinary terrain 1, marsh 1.5; Clear 1, Rain 1.25, Wind 1.10, Snow 1.40. Bridges retain ordinary path costs. Stone is always `1 / 1.18`. An edge uses the mean of its endpoint costs; actual walking speed divides by exactly that edge cost, then retains the existing Brisk factor once. Wet ground and rain/snow do not stack. Water/rock and bridge validity remain unchanged; winter never freezes an imaginary crossing.

A changed weather cost invalidates shared navigation once. Actors finish their still-legal current segment at its frozen speed, then plan subsequent edges on the new graph. Weather does not teleport anyone or cancel an already valid work/social trip. Seasonal preferences apply at normal future decisions. The renderer caches seasonal land by world identity plus season. Buildings and resident sprites are not globally recoloured. Snow/wind/rain are bounded procedural marks, not particle entities or gameplay RNG. Reduced motion removes them and the existing cosmetic movement; mute remains the existing audio control.

## Peoples and selected artwork

The [full asset review](GOAL_08_ASSET_REVIEW.md) includes source rectangles, inspected contact pages, provenance, ranked alternatives and deferred opportunities. Human drawings and all old exported PNGs remain unchanged. The selection uses two genuinely separate publisher-identified Urizen drawings: Imp `r20-c104`, and genie `r18-c104`, presented as Djinn. They have different masks, horns/tail versus crest/curled lower silhouette; these are not human recolours. Four compatible palette variations preserve each drawing's shape.

| Stable ID | Arrival weight | Behaviour |
|---|---:|---|
| `human-v1` | 60/100 | Existing individual routines |
| `imp-v1` | 20/100 | Prefers an available Workshop when choosing new work |
| `djinn-v1` | 20/100 | Prefers a reachable Hall when choosing ordinary leisure |

A new home hashes the world seed and durable home ID with versioned integer arithmetic below JavaScript's exact-integer limit, then maps its 0–99 result to 60/20/20 ranges. There is no corrective reroll to force a mixed small neighbourhood. Identity is assigned once, not on arrival animation, inspection, relocation or Undo. Nonhuman names use small local fixed name pools. Individual traits remain independently assigned; Green Thumb's Farm preference precedes an Imp's Workshop preference. Existing valid job assignments stay put. Shopping remains a specific Market activity, independent of ordinary leisure preference. Humans keep their existing names, bodies, palettes and traits.

Original `resident-atlas.png` remains unchanged. New `world-actors-atlas.png` retains its original 32 frames/shadow positions and appends nonhuman/visitor frames beyond the shadow row. New body IDs are separate from old human body assignment. The runtime still batches depth-sorted actor sprites through `drawAtlas`, with existing standing slots, poses, feet, silhouettes and shadows. All selected pixels derive from verified source crops through `tool/assets.py`; the small `living_world` manifest distinguishes source, crop, visual variant, composite prop and entity role. No source sheet or original catalogue record changed.

## Visitors and stories

At most **two** temporary actors exist, one per role. They occupy a separate map, never a cottage or permanent population slot. Their stable guest names are Mira Pack and Orin Reed; `visit-N` IDs identify individual visits. An independent Park–Miller visitor stream starts from `1 + (worldSeed + 67211) mod 2147483646`. An opportunity begins at 18 seconds and repeats every 15 seconds, with no new nighttime arrivals. Each role has a 150-second cooldown from arrival opportunity. Ineligible checks do not consume its RNG.

| Role | Eligibility and actual activity | Consequence |
|---|---|---|
| Travelling merchant | Real accessible Market with a vendor present at selection; enters at the actual arrival cell, walks, spends six valid seconds trading with a present vendor | Exactly 2 coins through the existing income pipeline, once; then legal departure |
| Wandering storyteller | Real reachable Park or Hall; covered Halls receive an extra eligible ticket in winter/rain/snow, Parks in suitable Summer weather | Listeners who actually socialise/gather at that venue for four seconds receive +4 mood for 75 seconds, once per visit; 18-second story, optional 24-second encore; then departure |

Clear weather gives a merchant venue one extra ticket. No arbitrary population threshold or compulsory conversation is needed. A selected inbound guest reserves one future venue place; existing occupants finish normally. Actual occupancy is still counted only when the guest arrives. This lets busy developed towns admit guests without exceeding capacity or continually stealing their just-vacated slot. Residents choose performances at normal leisure boundaries and after finished work; no working person is interrupted or teleported into an audience.

Visitors use the same weighted navigation, frozen edge movement, settling, eight-request planning budget, route invalidation and presentation as residents. A vendor's break pauses real trade rather than granting empty-Market income. A removed venue ends the visit; a moved venue can be reached again. Broken routes make guests reroute or wait. An interrupted half-edge can retreat only to its legal near side; no water crossing is invented. After a day an unfinished visitor seeks departure. If the exit is cut, they wait for a valid road, with no arbitrary disappearance across terrain.

Actual participants, one-time awards, progress, optional encore charge, current route, cooldowns and guest completions persist. The latest 24 completed visit records retain visit/role/venue/time/reward/listener count; two lifetime completion counters support familiar-guest dialogue after old records age out. Read-only conversations have no side effects. Only the explicit encore command charges 2 coins once and extends that actual visit; it cannot mint coins or instant happiness. Successful Market stops, covered seasonal story circles and familiar returning guests form the implemented conditional stories. The bag/staff carriers are verified drawings, but their merchant/storyteller occupations are game casting, not asserted publisher metadata. A bag/torch prop marks actual venue activity only.

## Save and history compatibility

Colony exports now use schema **4**; the concrete terrain representation remains compatible with Goal 07's modern landscape. Schema 1/2 layouts and schema 3 colony checkpoints remain accepted. Loading never regenerates terrain. All old resident fields, old clock, money, productive progress, events, requests, civic decisions and historical identities are preserved. Legacy residents remain Humans. Their calendar anchors at `floor(existingClock / 150)`, beginning Spring day one without rewinding the absolute day or replaying pay/events. A live old Rain shower carries its original remaining duration into Climate.

Before first migrated browser write, the exact old primary is copied once to `roag.settlement.landscape.v1.before-living-v4`. Existing schema 1/2 safeguards also retain `.before-colony-v3`. If backup fails, autosave pauses and the original primary stays protected. Imports decode fully before the existing replace warning; New World previews/cancellation do not modify the active town. Tests use isolated profiles and disposable imports, never the player's browser profile.

A bug found during interrupted-route reload testing was corrected: constructor-generated planning requests must be cleared before restoring the saved authoritative queue. Otherwise a disconnected restored world could gain extra queued requests. This narrow migration fix does not alter the construction/history implementation.

Construction Undo/Redo still changes exact physical lots, IDs and recorded construction charges, at the current time. It does not rewind clock, weather, guest history, earned income, people identity, requests or completed visitor rewards. Routes are legitimately invalidated after physical edits; their derived paths are not historical state to rewind. Restored buildings retain their durable ID and reconnect to today's simulation.

## Implementation ownership

- `climate.dart`: versioned calendar, deterministic weather day stream, forecast and seasonal formulas.
- `peoples.dart`, `appearance.dart`: stable people policy, compatible bodies/names and old human mapping.
- `colony.dart`: clock, pay/preferences, shared planning/capacity, schema 4 migration; `visitors.dart` is a part of that same authoritative library for bounded guest lifecycle/history/codec.
- `residents.dart`: extracted reusable `advanceTravel`/`moveSegment`; residents and visitors share the existing edge physics.
- `navigation.dart`: shared weather-aware endpoint costs and cached reverse fields.
- `settlement_game.dart`, `build_art.dart`: seasonal cache, weather marks, original renderer plus extended atlas, actual actor/venue feedback.
- `main.dart`, `dialogue.dart`: compact date/forecast, year watch, people/guest inspection, optional existing conversation UI. Browser verification found and corrected an event/guest button overlap.
- `browser.dart`, `web/platform.js`: one additional migration backup boundary; existing persistence/audio APIs remain.
- `tool/assets.py`, generated application assets/manifest: verified derivations, no source edits.
- `test/goal08_test.dart`, `tool/goal08_{fixture,browser,native}.*`: focused tests and reproducible browser evidence. Old base-pay/weather unit fixtures explicitly select ordinary Summer/Clear, disable unrelated event opportunities, and retain their original rate/route assertions. New tests separately exercise the real seasonal formulas.

## Verification and evidence

Commands and outcomes:

```sh
cd apps/settlement
dart format --output=none --set-exit-if-changed lib test
flutter analyze
flutter test
python3 -B tool/assets.py --check
FLUTTER_BIN=flutter bash tool/release.sh /roag/settlement/
# from repository root
python3 -B tools/build_classic_assets.py --check
python3 -B -m unittest discover -s tests/catalog -v
```

Formatting and analysis pass. **155 Flutter tests pass**, including 21 Goal 08 tests and the retained construction, terrain (600 seeds per preset), scheduler, navigation, needs, speech, requests, civic decisions and migration regressions. The [test log](evidence/goal-08/tests.log) includes full names/results. Asset checks verify 41 source crops, eight actor bodies × four palettes, the extended atlas, exact manifest/font and all 599 original Lua bindings from 67 unchanged source PNGs. The release builds with its real `/roag/settlement/` base and local CanvasKit/fonts. The catalogue suite retains its single baseline failure; [before](evidence/goal-08/catalogue-baseline.log) and [after](evidence/goal-08/catalogue-final.log) record the same failing test, with 47 others passing. The legacy catalogue assertion also lists new derived app/review PNGs; the settlement exporter separately validates their explicit provenance. It was not weakened or hidden.

The new model tests cover calendar anchors/boundaries, deterministic weather/forecast, actual seasonal payment, exact road/terrain/weather composition, frozen edges, human migration, stable distinct bodies, real work/leisure preferences, actual guest arrival/activity/attendees/departure, interrupted paths/bridges, venue relocation, Garden Path bookkeeping through reload, one-time encore/trade rewards, familiar memories, bounded history, forward Undo, invalid import, 1×/4× equivalence, differing render cadences and bounded 500-person planning. A disconnected-world queue duplication and missing visitor Garden Path bookkeeping were found and corrected through exact checkpoint tests.

**Automated browser input:** [main results](evidence/goal-08/browser-results.json) and [edge results](evidence/goal-08/edge-results.json) use actual pointer/keyboard, menus, downloads and file choosers against a served release. They do not call gameplay callbacks or change live model fields. They verify old 85-person towns; cancelled New World; seed-202 paid construction of 12 homes, Farm/Workshop/Market/Hall/Park; real mixed arrivals and ordinary work; both guests' travel/service/actual listeners/departure; one-time encore; active-visit reload; export/import; relevant venue relocation/removal and recovery; familiar returning dialogue; observed Snow travel speeds and paid Stone Road mitigation; reduced motion; audio controls; and 931×582, 1024×640, 1440×900 layouts.

**Native browser input:** [native results](evidence/goal-08/native-results.json) use XTest mouse/keyboard in headful Chromium at 110% browser zoom (931×582 CSS pixels in a 1024×640 viewport). Raw CDP performs initial browser/accessibility setup and read-only observation/screenshots. Native controls construct the paid 12-person town, complete both roles at 1× with actual listener benefits and merchant payment, sponsor an encore, inspect both distinct nonhumans, open forecast, toggle mute, select 16×, cancel a held stroke, and Undo/Redo. Clicking again cycles crowded inhabitants; the final harness asserts the selected Imp/Djinn label before capturing portraits. It does not infer inspection success merely from a screenshot filename.

**Existing interface regression:** the retained `tool/tranche06_browser.cjs` also passes against the staged production build: 85 residents; all nine earlier catalogue choices; cottage/workplace relocation and real work; promises, one gratitude award and history; both paid/free civic branches; depleted-treasury and no-customer eligibility; import/reload; pan/zoom and 931/1024/1440 layouts. [Results](evidence/goal-08/prior-dialogue-results.json).

**Whole year:** the actual clock ran from the first day through all four six-day seasons to day 25 at 16×. [The journey log](evidence/goal-08/year-watch.log) records this passing checkpoint. Its subsequent winter import initially failed because the developer fixture moved its clock without advancing the fixture's pending special-event deadline. The fixture now sets coherent future opportunities and validates every checkpoint before writing. The final browser run repeats the corrected winter/import checks and explicitly skips only the already-passed 225-second year wait. Save validation was preserved. Native and browser footage are actual rendered game output; edited recordings cut waiting time, rather than fabricating animations or simulation outcomes.

Reduced-motion information/controls and paused state are checked; mute/unmute button behaviour is checked. No claim is made that automated tests heard speaker output or establish subjective enjoyment. The asset review and screenshots were visually inspected. Public release verification is recorded separately below.

## Performance

Chromium 155 software rendering, 1024×640, 1× simulation. The existing `tool/scaling_browser.cjs` samples 16 wall-seconds ordinary operation and 16 after one real 13-cell road-removal stroke. Compilation and other browser journeys were stopped before the retained measurements. An overlapping sample was discarded before completion. These are observed browser timings, not physical input/display latency or hardware-independent guarantees.

Comparable imports use the exact original 85/250/500-person fixture geometry and Humans. New seasonal/weather rules intentionally change future decisions and pay; this is not a claim of unchanged future simulation.

| People / phase | Before frame mean / p95 ms | Goal 08 frame mean / p95 ms | Before → Goal 08 model p95 ms |
|---|---:|---:|---:|
| 85 / ordinary | 16.85 / 16.8 | 16.66 / 16.7 | 0.40 → 0.50 |
| 85 / road cut | 16.68 / 16.7 | 16.63 / 16.7 | 0.40 → 0.50 |
| 250 / ordinary | 16.73 / 16.8 | 16.63 / 16.7 | 0.90 → 1.10 |
| 250 / road cut | 16.69 / 16.8 | 16.68 / 16.7 | 0.90 → 1.10 |
| 500 / ordinary | 17.53 / 33.3 | 16.80 / 16.8 | 1.90 → 2.20 |
| 500 / road cut | 17.95 / 33.3 | 16.74 / 16.8 | 2.10 → 1.60 |

New developed fixtures additionally contain mixed peoples, a Market/Hall/Workshop, Autumn/Rain and **two real active guests** at import. They are deliberately different geometry/content from the baseline, so their timings are listed separately.

| People / phase | Frame mean / p95 / max ms | Model p95 ms | Presentation p95 ms | Queue peak → final |
|---|---:|---:|---:|---:|
| 85 / ordinary | 16.66 / 16.7 / 33.3 | 0.50 | 0.30 | 1 → 0 |
| 85 / road cut | 16.65 / 16.7 / 16.8 | 0.40 | 0.30 | 31 → 0 |
| 250 / ordinary | 16.70 / 16.8 / 33.3 | 0.90 | 0.50 | 1 → 0 |
| 250 / road cut | 18.43 / 16.8 / 133.4 | 1.20 | 0.80 | 188 → 0 |
| 500 / ordinary | 17.07 / 16.8 / 50.0 | 1.90 | 1.10 | 0 → 0 |
| 500 / road cut | 17.49 / 33.2 / 33.4 | 2.10 | 1.30 | 444 → 0 |

The first 250-person recovery run had a 133.4 ms maximum and 18.43 ms mean despite a 16.8 ms p95. A targeted repeat retained in the evidence measured 16.71 ms mean, 16.8 ms p95 and 50.0 ms maximum (model p95 0.90 ms). The large spike did not reproduce. Its cause is unverified; it is not silently excluded from the original data or asserted to be a game fix.

A separate 500-person town was legitimately simulated to 897.05 seconds, with six completed storyteller visits and five merchant visits, then saved. The browser crosses Spring/Rain → Summer/Clear at 900 seconds: frame mean 16.81 ms, p95 16.8, max 66.8; model p95 1.90 ms. Its 444-request peak drains to 0; stale requests are rejected/requeued. Subsequent road-cut recovery averages 16.63 ms, model p95 1.90 ms, queue 429 → 0. The edit increments navigation revision once, and reverse fields remain shared/LRU-bounded. Native/render cadence model tests independently establish off-screen activity and clock equivalence.

[Baseline data](evidence/goal-08/performance-before.json), [comparable release](evidence/goal-08/performance-comparable.json), [two-guest fixtures](evidence/goal-08/performance-living.json), [250 repeat](evidence/goal-08/performance-250-repeat.json), [season boundary](evidence/goal-08/performance-transition.json). Reproduce with:

```sh
cd apps/settlement
dart run tool/goal08_fixture.dart
dart run tool/goal08_transition_fixture.dart
SETTLEMENT_URL=http://127.0.0.1:8769/roag/settlement/?profile=1 \
  SETTLEMENT_FIXTURE_DIR=/tmp/roag-goal08-fixtures \
  node tool/scaling_browser.cjs /tmp/living-performance.json
```

Playwright 1.65.1 must be installed outside the repository or resolved through the development package. Fixtures label their accelerated/saved seasonal settings explicitly; the playable mode does not choose weather or guests to improve screenshots.

## Moving evidence and inspected screenshots

[46-second actual-game recording](evidence/goal-08/living-world.mp4): seed entry and paid drag construction; real guests, movement and optional conversation; Summer/Autumn clock changes; final release Winter/Snow. It is an edit of real browser recordings with waits cut. The first town/year is the ordinary seed-202 UI journey; the final Snow shot explicitly imports the validated seasonal fixture. No separate mock scene or invented animation was substituted.

- [Native Imp portrait and truthful trait/type inspection](evidence/goal-08/native-imp-v1-inspection-110.png), [native Djinn](evidence/goal-08/native-djinn-v1-inspection-110.png).
- [Paid seeded neighbourhood](evidence/goal-08/02-seeded-neighbourhood.png), [real visitor benefits](evidence/goal-08/05-visits-earned-real-benefits.png), [optional native visitor conversation](evidence/goal-08/native-story-dialogue-110.png).
- The continuous-year checkpoints: [Spring](evidence/goal-08/year-spring.png), [Summer](evidence/goal-08/year-summer.png), [Autumn](evidence/goal-08/year-autumn.png), [Winter](evidence/goal-08/year-winter.png). Winter's grass contrast was subsequently reduced after visual inspection; [final staged Winter](evidence/goal-08/staged-winter.png) shows the restrained frost treatment.
- [Real venue relocation](evidence/goal-08/edge-01-moved-active-venue.png), [removed Market](evidence/goal-08/edge-02-market-removed.png), [familiar guest dialogue](evidence/goal-08/edge-03-familiar-visitor.png).
- [Stone Roads in Snow](evidence/goal-08/edge-04-snow-stone-roads.png), [reduced motion](evidence/goal-08/edge-05-reduced-motion.png), [110% forecast](evidence/goal-08/native-forecast-110.png).

## Release and preservation

The existing Pages workflow now additionally accepts `release/settlement-goal-08`; main and Goal 07 remain launch/deploy paths. The existing `github-pages` environment was given an exact policy for that release branch, retaining both existing policies. Production is the same static hosting account/domain, with no new paid resource or credentials. The local production build uses the actual `/roag/settlement/` base, local renderer/fonts, and the existing staging/redirect layout. [Staged browser results](evidence/goal-08/staged-results.json) verify old-save autoload/backup, new worlds, ordinary drag input, distinct people/portraits, guest rewards/attendees, Winter assets, forecast and exact reload with no page errors or failed asset requests. **Published and verified:** [GitHub Actions run 43738135089](https://github.com/gongahkia/roag/actions/runs/43738135089) passed both `web` and `publish` for source `d76f10c03ebe1d5f142bb0384c9137407200ab12` on `release/settlement-goal-08`. [Deployment metadata](evidence/goal-08/deployment.json). The real [public game](https://gabrielongzm.com/roag/settlement/) was then opened in Chromium and exercised through normal browser input. [Public results](evidence/goal-08/public-results.json) confirm HTTPS, schema-3 old-town autoload/backup with 85 Humans and retained lots/money, seeded New World and drag construction, distinct nonhuman render/inspection, actual merchant/storyteller consequences, Winter/Snow, forecast and exact reload. There were **zero page errors and zero failed asset responses**. [Public mixed town](evidence/goal-08/public-mixed-living-world.png), [public Imp portrait](evidence/goal-08/public-imp-inspection.png), [public Winter](evidence/goal-08/public-winter.png), [public forecast](evidence/goal-08/public-forecast.png). No deployment blocker remains. The subsequent documentation/evidence commit does not change the published application.

[Preservation fingerprints](evidence/goal-08/preservation.json) verify all **six README files**, **1,677 protected repository files**, **49 existing settlement PNGs**, and **four original Lua player-storage files** unchanged. Root README SHA-256 remains `3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a`. No README was created, edited, renamed or temporarily changed. Original source sheets/catalogue and Lua trials/gameplay remain byte-identical. Private browser profiles were never opened; all browser testing used disposable contexts or isolated X11 profiles. Existing exported towns remain available unchanged.

## Limits and future review

Weather currently lasts a whole day, apart from legacy rain windows. Its next-day forecast is exact, not probabilistic. Four simple conditions and moderate modifiers deliberately avoid survival stockpiles or destructive weather. People identities use a fixed v1 arrival policy; a small set of homes need not contain every group. Visitor diversity currently means two recurring named guests, not a procedural quest generator. Scarce or unreachable venues can delay visits; a cut exit can keep a bounded guest waiting until infrastructure is repaired.

There is no new wildlife AI, combat, inventory, religion, diplomacy, injury, fire or hostile encounter. The asset review records suitable wildlife, camps, masonry, hostile drawings and hazards for later design, without implementing Goals 09/10. Optional 16× is intended for watching a year in a small town; ordinary speeds remain preferable for building and large populations. Rendering evidence is from Chromium software rendering; it is not a claim about every GPU/browser or physical display latency.

Human playtest questions:

- Do the seasons make the settlement feel different over time?
- Can I recognise changing weather and understand its consequences?
- Do fantasy peoples feel genuinely distinct rather than recoloured humans?
- Do visiting characters create interesting little stories?
- Does the world give me new reasons to construct or reorganise buildings?
- Does the town remain enjoyable to watch without demanding micromanagement?
- Does it remain responsive as the population grows?
