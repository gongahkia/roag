# Goal 11 · A connected region

**Implementation in progress.** This record is not a completion or deployment
claim. Goal 12 has not started. The requested Sudo/Debug panel and 20× controls
are explicitly deferred until Goal 11 is complete.

## Baseline and preservation

Started from clean `7e013b9e5c33db4197443c6cb3b571c86cabc708`, on
`feat/settlement-goal-10`, including the approved right-hand sidebar. Work is on
`feat/settlement-goal-11`; automatic workspace checkpoints and prior release
branches are retained. The latest existing published application workflow is
[44350273041](https://github.com/gongahkia/roag/actions/runs/44350273041) on
`release/settlement-goal-10` at `3564b070`.

The current baseline reran **214 passing Flutter tests**, the app validator
(47 crops, nine bodies × four palettes), and original Lua bindings (599 roles /
67 source PNGs). The catalogue command reran 48 tests with one failure in its
tracked-media ownership allow-list, reproducing its omission of existing derived
settlement artwork and evidence. It is not an entirely green catalogue baseline.

Initial preservation fingerprints were captured before editing for all six
READMEs, 238 Lua files, 755 original asset/catalogue-related files, 86 existing
app PNGs and four original Lua storage files. Private browser profiles are not
used. All new browser journeys must use disposable contexts and explicit fixtures.

## Geography and terrain

`region_geometry.dart` owns regional generation version 1. It reuses the existing
canonical numeric/text/UTF-8 seed semantics and a separate Park–Miller stream with
domain 715225739. Ten sites occupy a jittered, coherent west-to-east network with
16 weighted links and alternate routes. IDs are stable `site-00` through
`site-09`, independent of display names. Site terrain seeds use a separate bounded
formula; geometry draws do not advance any simulation RNG. The capital retains
its exact existing world. Concrete regional geography is serialized and validated,
never regenerated on load. Travel uses actual graph paths restricted to discovered
sites and open connections.

The original six environments keep their version-2/3 generation paths. Two
version-4 environments are implemented:

- **Pine Uplands:** dense 60% ranked conifer woodland, rocky ridge/pass rules,
  wet threshold 18 and rock threshold 36. The protected opening remains viable.
  More wind and winter snow affect actual travel. Standing forest enables and
  improves forestry production.
- **Salt Coast:** a coherent northern shoreline with a three-cell inland inlet,
  wet coastal approaches and ordinary short bridge crossings. Forest 22%, wet
  threshold 45 and rock threshold 14. Wind/rain have increased weights; prolonged
  rain waterlogs marsh travel using the existing maximum-penalty rule.

The audited spruce (`urizen-onebit-r08-c003`, `[40,105,12,12]`) and obelisk
(`urizen-onebit-r25-c035`, `[456,326,12,12]`) are two new derivative exports from
the unchanged Urizen source. The latter is explicitly cast as a Wayfarer Monument,
not a claim about the original drawing's gameplay purpose. The Lumber Camp uses
a warehouse/oak composition from already audited frames. No source sheet or
catalogue record is edited.

## Founding, identity and clocks

Village recognition requires four residents, at least three coins from actual
work and a staffed connected Market. Initial neighbouring discovery costs two
capital coins. Farther discovery requires two towns and a completed delivery.
Founding costs 60 capital coins: 50 transfer to the new town and ten fund its
expedition. It creates the selected site's concrete world exactly once. Free
Build relaxes these costs and development gates. New towns share the regional
date and inherit incident consent, with their own terrain/weather/event streams
and introductory incident grace.

Separate town treasuries preserve the existing construction budget, refund and
forward Undo/Redo semantics. The region provides a combined summary; trade handles
payments automatically. No second currency or manual transfer interface is used.

The capital clock orders every town through the original 50 ms colony step.
Inactive towns run the same pure authoritative model, without another Flame world,
rendering, camera, speech presentation or per-frame animation. This initial
contract uses exact model work, not inferred successful activities. Switching does
not advance time. Positions, trips, employment, incidents and session-local
construction histories remain owned by each existing Colony instance. Performance
and practical limits still require final measurements.

## Production and shipments

Real qualifying work produces bounded town stock: Provisions from Farms, Crafted
Wares from Workshops, and Materials from a staffed Lumber Camp with standing
forest within a three-step local land catchment. Water, rock and occupied building cells stop this work catchment, including bridged water; actual commuting still uses normal roads and bridges. Actual weather and condition output are
passed through once. Forest abundance improves forestry; removing its source or
access stops production. Clear itself grants no Materials. Managed standing
woodland is a renewable source while worked. Stock caps at 24; surplus work does
not become an unlimited latent backlog.

A relationship alternates fairly among three goods and both directions. It
reserves at most three surplus units and the buyer's real coin payment. A pack
visitor legally walks from the origin entry to a staffed Market, collects for two
seconds and walks back out. Only actual departure begins regional travel. Distance,
terrain, weather and closures affect that journey. At the destination, a real
visitor must enter, reach a staffed Market and unload for two seconds before goods
and payment are delivered once. Existing resident work income remains separate;
trade payments transfer coins between towns, rather than minting a second work
payment. Imported goods are not automatically re-exported.

Shipment IDs and loading/transit/unloading/delivered/returned/cancelled/lost states
persist. Uncollected cancellation restores stock and escrow once. A failed local
stop can return along an actual regional journey and unload at the original Market.
Prolonged Standard-mode obstruction can lose collected cargo after 180 blocked
seconds, refunding buyer escrow without paying the exporter. Peaceful mode does
not automatically destroy cargo. Local stranded actors retreat only along legal
roads. Disabling a relationship stops new dispatches; collected journeys finish.

Imported Provisions are consumed by a real populated, connected Market town for
a bounded +3 mood benefit. Consumed Wares create opportunities for the existing
visitor pipeline, which still requires actual arrival/service for any ordinary
reward. Materials support optional projects. Regional interruptions derive from
actual snowbound passes and the existing incident director; no unrelated random
threat scheduler is added.

## Development and optional projects

Recognition stays forward even when a building is removed. No old building or
ability is taken away. Legacy towns receive recognition from their existing facts.

| Stage | Requirements / opportunity |
|---|---|
| Hamlet | Starting recognition; all existing building tools remain available. |
| Village | Four residents, three coins of actual work, staffed connected Market. Opens initial discovery/founding. |
| Town | Eight residents, 120 productive resident-seconds, connected Park/Hall, and one delivery or 20 coins of work income. Enables the Monument. |
| Regional Centre | Twelve residents, two enabled relationships that actually delivered, a standing accessible Monument/Hall and a genuinely trained/on-call protective resident. |

Three optional projects use ordinary coins and genuinely produced/imported goods.
They do not end the game or restrict creative building:

- **Trade Network:** three towns, two reliable relationships, four deliveries;
  consumes four Materials and two Wares in the selected town, then gives +2 mood
  region-wide. No recurring coin mint.
- **Wayfarer Monument:** developed Town, healthy connected Hall, six Materials,
  four Wares and 40 coins. Adds the audited obelisk to that exact Hall and improves
  existing visitor opportunities while it stands. Demolition removes the physical
  benefit; it does not refund goods or erase recognition.
- **Resilient Region:** two towns with trained protection and accessible Refuges,
  all extant damage repaired, no active incident. Standard cargo's recoverable
  obstruction window extends from 180 to 240 seconds.

Winter snow can close exposed passes when both endpoints are snowbound. Actual
brigands at a Market approach and floods cutting a Market connection close affected
regional links. Existing Watchpost response, road/Stone/bridge repair, recession or
retreat reopens them. Paths are chosen only through discovered, open links;
caravans may take alternatives at reached nodes, not teleport midway through a
closed edge. Factual interruption/recovery history comes from the existing
incident/weather state. Peaceful consent survives migration and founding.

## Player controls and local build

From the repository root, with Flutter 3.50.0 available:

```sh
make settlement
# if Flutter is outside PATH:
make settlement FLUTTER_BIN=/absolute/path/to/flutter/bin/flutter
# serve an already-built release using only Python:
make settlement-serve
```

The server binds localhost on an available port and prints its URL. `flutter:
command not found` means the SDK is absent from PATH; the explicit override above
uses an installed SDK without changing system configuration. `make game` still
launches the original LÖVE game. Build dependencies remain locked: Dart 3.14,
Flame 1.38.2, material_ui 1.11.0. No new runtime package or hosted service.

**Region / W** opens the map. Select an adjoining question marker, discover,
then explicitly confirm founding and its name/cost. Select a founded town to visit
or connect it to another town. A relationship automatically exchanges native
surplus for actual need. Expand the production/accounts section for quantities,
reserved buyer payments and trade value. Routes show journey time, local Market
requirements, cargo phase and disruption reasons. Copy the regional seed from the
map footer. Newly discovered biome presets become available in New World;
the original six presets remain available, and Free Build exposes all eight.

Ordinary drag building, sidebar scrolling, B tool finding, I inspection/dialogue,
M relocation, Escape cancellation, H lane, +/-/scroll zoom, pan and Ctrl/Command
Z / Shift-Z remain. The ordinary speed controls stay 0/1/2/4; the existing menu
retains 16× year watching. Debug/20× work is a separate, deferred user request.

The map is a temporary panel, not the default game screen. Its scaled miniature
terrain illustrations reveal known environments, houses show founded sites and
pack icons use actual regional shipment progress. One Flame renderer displays
only the selected town. Camera positions are remembered on switching towns;
opening/reloading uses the established home framing for the active town.

## Persistence and transaction boundaries

Regional **schema 7**, regional geography version 1, wraps each unchanged
schema-6 Colony serialization. Concrete sites, links, discovery, towns, RNG,
identities, positions, routes, stocks/progress, clocks, shipments/escrow, projects,
financial counters and bounded histories are stored. Loading never regenerates
geography or town terrain. Strict decode checks local and regional ownership,
clocks, site seeds, inventories, actors and shipment chronology.

Existing schema-1–6 standalone worlds become the capital without regenerating or
resetting anything. The exact original text is backed up at the existing origin's
`roag.settlement.landscape.v1.before-region-v7` key before the first regional
write. Previous migration backups remain. Backup failure protects the old bytes
and pauses autosave; invalid imports are rejected before replacement. New World
review/cancellation changes no live region. Export/import replaces the **whole
region**, with an explicit warning and export option.

The existing main storage key remains. Browser storage dictionary-packs repeated
field names for schema 7 (format `roag-region-dictionary-v1`); exports are ordinary
readable JSON. Old standalone bytes are read/backed up unchanged. Packing is
lossless, validates duplicate/unsafe fields and fails atomically on quota errors.
No private user browser profile was used for verification.

Construction retains each Colony's existing forward Undo/Redo. It never rewinds
work, dates, deliveries, another town, damage or projects. Founding, discovery,
route establishment, dispatch, delivery and project completion are explicit
idempotent regional operations, outside ordinary brush history. As before,
construction Undo stacks are session-local, not a new persisted time machine.

## Implementation ownership

| File | Responsibility |
|---|---|
| `lib/region_geometry.dart` | Versioned independent regional generation, graph validation, known/open shortest paths |
| `lib/region.dart` | Town identity, shared clock, founding/discovery, genuine production, schema-7 codec |
| `lib/regional_trade.dart` | Reservation/escrow, real local Visit integration, regional travel, refunds/loss, source-derived closures |
| `lib/regional_progression.dart` | Latched stages, one-time projects, goods/coin consumption and standing Hall benefit |
| `lib/region_map.dart` | Pixel-art map, known-site inspection, founding, relationships and compact progression |
| `lib/colony.dart`, `lib/visitors.dart` | Narrow work/mood/freight hooks into existing authoritative model, exact per-tick stepping |
| `lib/model.dart` | Appended Lumber Camp catalogue entry and shared reachable timber validation |
| `lib/terrain.dart`, `lib/world_generation.dart`, `lib/climate.dart` | New v4 environments and climate weights, frozen original six generation branches |
| `lib/main.dart`, `lib/settlement_game.dart` | Existing sidebar integration, one renderer/town switching, whole-region saves, audited compositions and opt-in profiling |
| `lib/browser.dart`, `web/platform.js` | Existing save bridge, migration backup and lossless regional field dictionary |
| `tool/assets.py`, `assets/manifest.json` | Two new reproducible derived crops; original art and old PNGs unchanged |
| `test/goal11_*`, `tool/goal11_*` | Focused deterministic regressions, labelled fixtures, actual release input, motion and measurements |
| `.github/workflows/settlement.yml` | Existing pinned test/build/Pages workflow extended to the exact Goal11 release branch |

## Verification record (final audit in progress)

The final app suite currently passes **242 tests**, followed by an additional
Centre/Monument recognition regression. Formatting covers all 52 library/test
files. Asset checks pass 49 exact app crops / nine bodies × four palettes and
599 original Lua bindings / 67 PNGs. Six storage-codec fault/round-trip checks pass.
The root catalogue allow-list failure was reproduced before editing and remains
separate; no assertion was weakened.

New focused coverage includes 600 deterministic regional graphs and 600 seeds per
each of eight terrain presets, plus 100 targeted seeds per new biome; paid founding,
independent worlds/histories, exact migration, equal standalone/background work,
30/120-Hz continuation, real goods/forestry, actual bidirectional transport,
zero-sum accounting including escrow, exact transit reload, real Peaceful return,
Standard loss/refund, project costs, latched progress and forward Undo, early and
repeated callback rejection, malformed cargo and profiling neutrality.

Actual browser normal-play seed **202 / Peaceful**: First Hearth begins with four
paid cottages, Farm, Workshop, Market and Park (64 coins). Actual work earns
Village. Discover/found **Alder Reach (Woodlands)** for 60 capital coins; its real
50-coin allocation buys three cottages, planted timber, Lumber Camp, Market and
Park. Establish the relationship, observe collection/departure, reload in transit
and see the actual destination Market delivery. Discover/found **Pine Crossing
(Pine Uplands)**, build four cottages/Farm/Market/Park, connect it, deliver enough
goods and complete Trade Network. Inspect a resident's real Regional supply +2,
continue paid construction, consume delivered Wares for an actual merchant
opportunity, cut forestry access while viewing another town and Undo reconnect it.
No Free Build or edited model state was used after New World creation.

The first long harness stopped on a browser-side closure-reference mistake. The
corrected run completed all ten progression/forestry checks, then its exact-name
Home lookup encountered Flutter's duplicate active tooltip accessibility label
during resize. Prefix matching resolves that harness issue. Continuation loads the
**unchanged saved normal-play bytes**, not a rebuilt developer town, for seasonal,
resize, export/import and switch checks. These distinctions remain in raw logs.

Browser inputs are Playwright mouse/wheel/keyboard against the actual release,
plus a separate native XTest run for genuine 110% browser zoom. Headless Dart
fixtures are explicitly labelled; they are not manual play. Human enjoyment and
balance remain unverified. Final performance, hosted URL and evidence results are
added below after their actual runs.
