# Workplaces, roads, events and individual residents

This tranche extends the existing local Flutter/Flame settlement. The world and
its construction brush remain the primary controls. New work, service and social
venues use actual resident journeys and productive time; there are no goods,
wallets, recipes, individual orders or extra resource bars.

## Local play and controls

From the repository root:

```sh
python3 -B apps/settlement/tool/serve.py --port 8765
```

Open or refresh <http://127.0.0.1:8765/settlement/>. The existing server can serve
the rebuilt release; a port-in-use message means it is already running. Use a
hard refresh if an old toolbar is cached. Another port has separate browser
storage; export/import transfers a copy without resetting the original town.
Nothing in this tranche is deployed or published.

The **Build…** popup (`B`) groups Homes, Work, Social and Roads. The original
five tools stay directly available; the popup shows a new selection for repeated
painting. Existing `1/C`, `2/P`, `3/R`, `4/G`, `5/V` shortcuts retain Cottage,
free dirt Path, Remove, Farm and Park. New shortcuts:

| Key | Tool |
| --- | --- |
| 6 | Workshop |
| 7 | Market |
| 8 | Community Hall |
| 9 | Stone Road |
| 0 | Garden Path |

Click/drag and release still makes one construction transaction. Escape cancels;
Ctrl/⌘ Z and Shift Z undo/redo. Space/right/middle drag pans; scroll or +/− zooms;
H returns to the lane; T cycles pause/1×/2×/4×. The grouped popup scrolls where
needed rather than adding all tools to the permanent row.

`I`, the header inspection button or Alt-click selects a resident or place.
Selection happens on release with a five-pixel drag threshold. Repeated clicks
at the same overlapping group cycle its nearby residents, with a selected-person
ring and a hint in the card. Right/middle/Space dragging still pans. Ordinary
construction gestures are not intercepted by residents. Escape dismisses
inspection. Cards are read-only and show the real sprite, stable name, trait,
activity, home/workplace, mood and its reasons.

## Buildings and economy

| Building | Cost | Work / visitors | Actual role |
| --- | --- | --- | --- |
| Cottage | 5 | One resident | Existing home and arrival rules |
| Farm | 8 | 2 jobs | 0.2 coin per validated worker-second; existing 12-second bouts |
| Workshop | 16 | 1 job | 0.4 coin per validated worker-second; 12-second bouts |
| Market | 14 | 1 vendor, 3 customers | Vendor work pays 0.1 coin/second; completed service separately earns 0.25 coin |
| Park | 6 | 6 visitors | Existing six-second social visits and evening gatherings |
| Community Hall | 12 | 8 visitors | Covered social venue; preferred for new relaxation visits during rain |

Every building has one cell and requires the same adjoining arrival-connected
path network. Farms supply two inexpensive jobs; Workshops cost twice as much
and supply the same total full-staff income using one person. Parks remain the
cheapest social venue. The ordinary 20-coin grant per 150-second day, happiness
bounds, demolition traces and existing financial history rules remain.

A vendor must actually be working at a reachable Market. Customers must arrive,
fit its customer capacity and spend six valid service seconds. Missing staff
stops service progress; travellers and waiting people do not occupy served
visitor slots. Vendor work income and trade income have separate counters and
inspection explanations. The displayed individual trade value is **0.25**, not
rounded to a misleading 0.3.

The resident's persisted `tradeDay` allows at most one base trade credit per
simulation day **across all Markets**. A Market-day bonus attaches to that same
transaction; already-credited visits can still be real social visits but pay no
second base or bonus. Removing, rebuilding, importing or reloading a Market does
not clear a person's record. There are no household wallets or goods.

Daily shopping uses the existing routed visit machinery after productive work
or a fruitless job search, while a vendor is present. It is distinct from an
ordinary relaxation preference, so commerce can still happen in rain. A sole
vendor never visits its own stall as a customer. Vendors work longer 35-second
bouts and can retry after six seconds; this gives customer journeys a usable
service window. Completed valid visits still feed the existing social happiness
system. In towns containing a Market, idle unemployed residents can seek later
social visits after the existing 18-second interval. No new need meter is added.

## Roads, movement and daily traversal credit

All surfaces occupy the existing road cell and share coherent ports and access.

| Surface | Cost per changed cell | Traversal / effect |
| --- | --- | --- |
| Dirt Path | Free | Existing baseline; rain makes travel 20% slower |
| Stone Road | 2 | 18% faster; retains that speed in rain |
| Garden Path | 1 | Normal pace; rain slows it; actual traversal can grant +3 mood once per day |

Changing to a different paid surface charges its full displayed price once.
Repainting the same surface is a no-op. Switching to dirt or removing a road
refunds nothing automatically. A stroke deduplicates changed cells and keeps its
original budget; cancellation charges nothing. Undo/Redo reverse/reapply the
recorded charge, with existing atomic affordability checks and a forward-moving
ID allocator. A surface change never temporarily removes the road or recreates
adjacent residents.

Weighted navigation and movement share the same edge cost: the mean of the
endpoint surface time multipliers. Brisk is a constant resident multiplier, so
shared destination fields remain valid for different traits. A segment records
its speed and Garden Path eligibility when it starts. A surface/weather change
applies to later segments; a removed target still fails the immediate legality
check. Segment metadata survives save/reload.

The arrival-time test found that the old integrator discarded leftover movement
time at every tile boundary, sometimes erasing an 8% Brisk advantage. Movement
now consumes that leftover time on the next legal segment. Reaching work does
not turn the leftover fraction into paid productive time: arrivals still pay
nothing in their arrival tick.

`gardenDay` changes only on a completed qualifying traversal, not from painting
beneath someone, hovering, inspection or standing. It grants +3 for that day,
less than a completed Park visit. Crossing more tiles does not stack it. The
record survives history and persistence, and a new day requires a new traversal.

## Traits, identity and special events

Names use frozen local first/last-name pools. Explicit deterministic mappings
assign missing names/traits once from durable resident identity; stored choices
do not depend on catalogue length, population, camera, frames or event RNG.
Existing bodies and palette indices are retained, including inactive records.

| Trait | One authoritative effect |
| --- | --- |
| Brisk | 8% faster walking, verified by actual arrival ticks |
| Sociable | +3 extra mood from an otherwise qualifying completed social visit |
| Green thumb | Prefers an available Farm when making a new job assignment; keeps an existing valid job |

Special events use a separate bounded Park–Miller stream. The first opportunity
is 12 simulation seconds ahead for a new run/migrated checkpoint; events last
25 seconds with a 15-second cooldown. Eligible choices are sampled only at
explicit simulation boundaries. A busy existing gathering defers a new special
event by five seconds without drawing RNG. At most one special event is active.
Identity, times, next opportunity, counter and RNG persist. No historical event
days are replayed on load.

| Event | Eligibility and actual effect |
| --- | --- |
| Rain shower | A populated town; restrained rain, 20% slower dirt/Garden travel; Stone stays quick; new relaxation visits prefer an available reachable Hall |
| Good harvest | A connected usable Farm; +25% on actual productive Farm time, no bonus for Workshop/vendor work or ownership alone |
| Market day | A present vendor and residents with available time; greater Market interest and +0.25 on the same daily qualifying trade transaction |

Changed preferences apply at decision boundaries, so a valid visit is not
abandoned when rain starts. Outdoor venues remain usable. Events do not reserve
a second actor or visitor slot: existing activity and capacity validation still
owns participation. Gathering invitations take priority at routine boundaries.
A removed/disconnected destination stops its invalid work/service or visit;
the selected event is not rerolled. Harvest can lapse with no valid workers;
a removed Market earns no trade. Construction Undo preserves event time,
legitimate earnings and daily credits.

A short announcement states the effect, remaining time and relevant build
choice. Its persistent compact label opens read-only event inspection after the
announcement shortens. Rain respects reduced motion; no new audio layer bypasses
existing mute settings. Harvest highlights a real productive farmer, while
Workshop hammer gestures and Market service use existing resident presentation.

## Source art

Original sheets, catalogue and audit images are unchanged. New buildings use
verified distinct 12×12 Urizen crops from
`assets/urizen/urizen_onebit_tileset__v2d0.png` (origin 1, pitch 13):

| Use | Source ID | Rectangle x,y,w,h | Treatment |
| --- | --- | --- | --- |
| Workshop | `urizen-onebit-r33-c005` | 66,430,12,12 | Warehouse miniature, slate tint; procedural working hammer cue |
| Market | `urizen-onebit-r33-c004` | 53,430,12,12 | Farm/fence miniature adapted as a stall with separately drawn four-stripe awning |
| Community Hall | `urizen-onebit-r33-c002` | 27,430,12,12 | Granary miniature, plum tint, covered-venue interpretation |

These are deliberately documented interpretations/compositions, not claims of
new authored Market or Hall art. Dirt/Stone/Garden surfaces reuse the verified
row-11 road crops c000–c012 and existing clipped cardinal end recipes. Stone and
Garden colours and small paving accents are derived renderer treatments using
the same connection geometry. The approved four resident bodies and palettes
are unchanged; source art stays static, with procedural activity motion.
Exact hashes/provenance are in the app asset manifest.

## Coordinated simulation and navigation

`Colony` retains a single fixed 0.05-second simulation clock. Movement/activity,
productive time, capacity checks, event boundaries and payments advance in that
clock; off-screen people use the same update path. Hidden-tab/focus suspension
keeps the existing no-catch-up policy. There are no per-resident Flutter widgets
or independent asynchronous timers.

Expensive decisions and replans use one deterministic FIFO with an eight-request
budget per fixed tick. Entries identify their navigation revision; stale entries
are discarded and requeued fairly. A monotonic cost revision invalidates reverse
weighted destination fields on committed edits, Undo/Redo, import and rain
changes. Tentative strokes do not touch navigation. A committed stroke coalesces
invalidation. Existing legal segments finish with their recorded duration; invalid
cells are never traversed while waiting for a replan.

Up to 96 reverse weighted fields are reused with LRU eviction. Cached job,
worker and visitor counts replace repeated whole-population scans. Derived
caches can be rebuilt; authoritative queue order and segment state are saved.
Import rebases fresh queue entries to a newer revision while retaining genuinely
stale entries, so invalidation does not change logical continuation.

Rendering culls off-screen foreground objects and people, while the model keeps
simulating them. Existing cached ground rasterization and shared sprite assets
remain. Profiling is opt-in with `?profile=1`; its bounded timing samples and
operation counters do not enter player storage or the normal UI.

Resident bodies are now drawn from a **single 416×260 atlas** in depth bands with
`Canvas.drawAtlas`. Each approved body/palette has an explicit mirrored copy;
nearest-neighbour 4× source pixels and a baked outline occupy a 52-pixel cell.
The foot anchor is (26,50), drawn at 9/48 world units per atlas pixel. The shadow
shares the atlas. Tool gestures, rings, speech and arrival accents still follow
real resident activity. The source bodies and colours are unchanged. Batching
approximates the old tiny body skew with a small rotation; it does not add new
source animation frames. Atlas layout, mappings and bytes are checked by tests
and the asset validator. This followed a measured rendering bottleneck after
model coordination and off-screen culling alone were insufficient at 500 people.

## Ownership and save compatibility

| File | Responsibility |
| --- | --- |
| `apps/settlement/lib/model.dart`, `rules.dart` | Tool catalogue, prices/capacities, physical road kinds, additive lot decoding and transactional charges |
| `lib/residents.dart`, `resident_identity.dart` | Real movement, frozen segments, stable names/traits and daily resident records |
| `lib/colony.dart`, `navigation.dart`, `special_event.dart` | Fixed-step routines, jobs/service/payments, bounded planning, weighted shared fields, events, validation and persistence |
| `lib/settlement_game.dart`, `main.dart` | Batched/cullable world rendering, real activity cues, inspection, grouped catalogue and input |
| `lib/browser.dart`, `web/platform.js` | Opt-in bounded profiling bridge; no player-storage writes from metrics |
| `tool/assets.py`, `assets/manifest.json` | Auditable derived crops and resident atlas |
| `test/tranche05_test.dart` | Focused new model, persistence, movement, event, queue and atlas contracts |
| `tool/tranche05_*`, `tool/scaling_*` | Explicit developer fixtures, browser/native journeys and release measurements |

Paths abbreviated to `lib/…` above live under `apps/settlement/`. Changes stay
inside that application and this documentation/evidence. The original Lua game
and the source artwork/catalogue are unchanged.

The existing schema-3 storage key remains `roag.settlement.landscape.v1`.
Ordinary Dirt/non-road lots retain their eight-field representation; only a
non-Dirt road adds a ninth field. Old five/six-field lots and older schema-3
checkpoints still load. Missing special-event fields schedule the first future
opportunity, without replaying historical days. Names/traits are assigned from
stable identity without replacing appearances or IDs. Existing migration backup
and failed-backup protection remain in place.

Authoritative queue order, cost revision, active segment metadata, new building
assignments, trade/Garden day records, event IDs/times/RNG and separate trade
income round-trip together. Derived navigation fields are rebuilt. Import
intentionally raises the navigation revision; the browser regression now compares
all gameplay fields while checking that fresh queue tags rebase and stale tags
remain stale. It no longer incorrectly requires byte identity for that explicitly
monotonic invalidation counter. Reload of a paused checkpoint still agrees exactly.

Construction history remains forward-moving: Undo restores the recorded layout
and charge, not clock, legitimate income, benefit records, selected events or the
ID allocator. No normal Lua profile/save, achievement or unlock system is involved.
