# Goal 09 — Preparedness, incidents and a town with history

Goal 09 extends the existing Flutter/Flame settlement. Building remains the player's main interaction: draw roads, place services, prepare access and shelter, then watch inhabitants respond. There are no individual emergency orders or tactical combat controls.

## Play and controls

The release uses Flutter **3.50.0**, Dart **3.14.0**, Flame **1.38.2** and material_ui **1.11.0**, with the existing lockfile and CI version check. From the repository:

```sh
cd apps/settlement
flutter pub get --enforce-lockfile
bash tool/release.sh /roag/settlement/
python3 -B tool/serve.py --port 8765 --base-path /roag/settlement/ --build-dir build/site/settlement
```

Open **http://127.0.0.1:8765/roag/settlement/**. If that port belongs to an existing server, open it or use `--port 0` and the printed URL. A release rebuild can replace the served directory without breaking the server's working directory.

- Click/drag and release: one construction transaction. Escape cancels.
- **B / Build…:** all buildings, including the new **Preparedness** category and Inn.
- **I:** inspect a person or building. Click a visible resident's body; overlapping people can be cycled. **Talk → How is the town prepared?** opens a short contextual conversation.
- **Repair:** select through B, then click/drag damaged safe buildings. The preview states the actual charge.
- **M / Move building:** existing free relocation, retaining building and resident IDs. A burning or disturbed source cannot be moved while unsafe.
- **Ctrl/⌘ Z**, **Shift Z:** Undo/Redo construction. Completed simulation and incident damage keep moving forward.
- **Ⅱ / T:** pause; existing 1×/2×/4× controls remain available. The menu retains 16× year watching.
- Right/middle/Space-drag: pan; scroll/+/-: zoom; **H:** return to the lane.
- **Landscape menu → Incidents:** Peaceful/Standard, current explanation and the last six incident outcomes. **New landscape** also offers the choice. Export before replacing the active town.

Warnings and brief aftermath notices are nonmodal. Their locate/details button is optional. Money, work, social life, weather, visitors, landscaping, relocation and previous conversations remain available during an incident.

## New infrastructure

All four buildings occupy **one existing tile**, need normal adjacent connected road access and use the existing construction, employment, routing, payment, relocation and save mechanisms.

| Building | Cost | Actual function |
|---|---:|---|
| Firehouse | 18 coins | Two automatic responder positions, each paying 0.10 coin/s for actual duty or suppression. A worker trains by physically reaching work. |
| Watchpost | 14 | One automatic guard position, paying 0.10 coin/s for actual duty or shooing. A Farm must be within **10 weighted road-cost units** of the station. |
| Refuge | 10 | **8 temporary shelter reservations**, reduced to 4 when damaged. Actual arrival reduces exposure; it never supplies permanent housing. |
| Inn | 16 | One innkeeper at 0.15 coin/s and **4 covered social spaces**. Completed visiting characters can walk here and rest six actual staffed seconds before departure. |

Assignments stay stable. Green Thumb and Imp work preferences remain; otherwise unfilled protective roles are considered before ordinary vacancies. Existing employees retain their jobs. Protective duty bouts last 60 seconds. An employee is on call for 60 seconds after actual duty, or while already assigned to the current response. Merely owning an unstaffed building gives no protection.

An incident uses at most **four distinct firefighters** or **one distinct guard** town-wide. Losing a station cannot manufacture extra responders. Previously dispatched residents may resume if their own station becomes usable and accessible again. An underway legal edge finishes before replanning. Missing roads and broken bridges cause real rerouting or waiting.

The Inn is the additional architectural form. Its innkeeper has a 35-second work bout. An arriving guest reserves social capacity and waits for actual service; unstaffed time never counts as rest. Completing rest advances that role's next visitor opportunity by 30 seconds, bounded to at least 30 seconds in the future. It gives no second merchant payout. A removed, unreachable, unassigned or timed-out Inn produces a legal departure/wait through the existing visitor lifecycle. Ordinary covered social visits use the same capacity and mood rules as existing venues.

## Condition, repair and history

Condition belongs to a **durable building ID**, separately from reversible landscape snapshots:

| Health | Player state | Authoritative effect |
|---|---|---|
| 100 | Operational | Full function. |
| 40–99 | Damaged | Work income and responder effectiveness ×0.60; social capacity halved, minimum one if the building has social capacity; Refuge capacity 4. |
| 0–39 | Temporarily unusable | No work, service, social or protective function. A home's resident keeps the same identity and cottage association and seeks safety. |

Burning buildings and a Farm currently being grazed are unsafe/unavailable regardless of structural health. Productive and social activity stops immediately when it becomes unsafe, so an immediate checkpoint cannot claim work inside a burning building. Farm season, weather, harvest and condition factors each apply once.

Repair is immediate authored construction. Price is `clamp(missing_health × 0.05 coin, 1, 5)` per building. Healthy, unsafe or unaffordable targets are rejected. Repeated cells are deduplicated. A stroke stages all accepted targets, captures its budget and applies one charge/history entry on release. If any staged building changes health, damage revision or safety before release, the entire staged repair is refused without a charge. Cancellation changes nothing.

A fresh unused repair can be undone and redone for its exact original cost. If the building has subsequently worked, been used or met new damage, Undo consumes that repair history entry **without a refund or health rewind** and explains why; another Undo can reach earlier construction. New damage after Undo invalidates the old Redo. Prior use is captured at repair time, so a long-established building still permits a genuinely fresh repair to be undone.

Relocating, demolishing and undoing layout changes retain the ID's condition. Restoring an old cottage does not erase a fire, duplicate its resident or grant historical income. Existing construction high-water IDs and the bounded history remain. As before, Undo history is session-local; persisted repair counts/times, damage and actual usage survive reload.

Scorch marks remain restrained at a building's base. Cracks and a warm condition marker distinguish damage/unusability; a brief gold repair outline acknowledges restoration. After **45 seconds of actual productive work or four actual home/social/shelter/station uses**, a building gains small established details. These details change no income, capacity, identity or footprint. Repaired and developed buildings continue using their original art.

## Fire

The shared fixed simulation clock produces a six-second smoke warning, then ignition. Initial heat is Summer 26, Winter 18, otherwise 22. A new adjacent fire starts at 18. There are at most **three sites**, each tied to a real building; only four-neighbour touching buildings can spread. Roads, empty ground, water and other gaps are firebreaks.

Each burning site naturally loses one heat/second, or two in rain/snow. Every two active seconds it takes six damage, or four in rain/snow, up to **70 damage per building / 210 total**. A site attempts one spread after four active seconds in Summer/wind, otherwise six, while heat is above four. Rain and snow prevent spread. Eligible neighbouring buildings must be healthy and not already in this fire. Spread chooses using the dedicated incident stream. Intensity never scales with preparation.

Actual firefighters walk to a legal road beside a site. Suppression begins after arrival: **four heat/second each**, ×0.60 from a damaged Firehouse. Waiting, travelling, disconnected or unusable-station workers suppress nothing and earn no response income. Reaching and helping adds that resident to the outcome's arrived-responder count. Heat reaching zero ends a site. When all sites end, the incident records containment or burnout, actual damage, affected IDs and real shelter/response counts.

Threatened inhabitants leave productive routines. They reserve reachable safe Refuge space and travel there, or follow a bounded search for a safe road. A full/unreachable Refuge never teleports or shelters them. An isolated actor waits physically until access is repaired. Unusable homes retain their residents through recovery. Incident exposure lasts 75 seconds: −8 mood, reduced to −2 only after actual shelter; a still-unusable home contributes −8 until repaired. These compose with existing needs and mood bounds.

## Rabbit disturbance

The audited rabbit is a real temporary actor, entering at the road's map-edge arrival point. Following the warning, it travels through the same weighted road/bridge graph at 2.5 base cells/second, with terrain/weather/road factors and the shared frozen-edge convention. It cannot cross a missing bridge or take an imaginary terrain shortcut.

Only physical arrival at a Farm starts grazing. Grazing interrupts that Farm's operation for at most ten actual seconds, doing four damage at 2/4/6/8 seconds: **16 total**. If the animal never arrives, crop damage remains zero. Losing access stops grazing; reconnection permits normal replanning. Its remaining progress is retained rather than granting repeated crop damage.

An eligible guard travels to the threatened Farm and can shoo only within 1.1 cells of the real animal on the same/legal adjacent road. Two actual shoo seconds drive it away; a damaged Watchpost supplies 60% effectiveness. Its outcome is recorded when it legally returns to the entry road. There is no direct combat, kill reward, health loss or immediate fictional financial deduction.

If every legal retreat route is removed, the animal waits and the single active incident remains until a route is restored. This deliberately favours safe recoverability over teleporting or inventing another destructive event.

## Scheduling and fairness

New generated worlds default to Standard with **450 seconds / three days of grace**. New World can choose Peaceful. All schema-1–4 towns migrate healthy and **Peaceful**; opting in explicitly starts at least 450 seconds of new grace. Peaceful stops new destructive incidents; an incident already underway finishes.

The existing fixed-clock boundary checks an opportunity every 30 seconds, subject to:

- At least six residents, ordinary work hours and an eligible connected target.
- One serious incident at a time, with **600 seconds / four days** cooldown after it ends.
- No ongoing communal gathering; defer while any extant building is unusable or six extant buildings are damaged. Retired/demolished IDs do not indefinitely block future opportunities.

Fire selection weight is 1 in rain/snow, otherwise 4 in Summer or 2 elsewhere. Wildlife weight is 3 in Spring/Summer, otherwise 1. Both require real eligible targets. Fire targets are healthy connected buildings; rabbit targets are healthy connected Farms reachable from arrival. The independent Park–Miller incident stream starts at `1 + (world_seed + 95189) mod 2147483646`, advances by `state × 48271 mod 2147483647`, and persists. Existing terrain, visitor, positive-event and gathering streams retain their owners. Rendering, inspection, camera motion and decorative details consume none of them.

Positive events, visitors and ordinary construction continue. No incident intensity is adjusted to defeat infrastructure. Firehouse/Watchpost effectiveness depends on actual staffing, paths, distance, arrival and condition.

## Assets and implementation ownership

All selected source crops were reviewed at native and enlarged sizes: [review sheet](evidence/goal-09/sprite-review.png), [crop records](evidence/goal-09/review-crops.json), [review HTML](evidence/goal-09/sprite-review.html). They come from the existing CC0 Urizen v2d0 source and existing catalogue grid. No source sheet, catalogue record or invented atlas rectangle was added.

| Cast | Existing row/column and rectangle | Runtime use |
|---|---|---|
| Firehouse | r06 c053 `[690,79,12,12]` | Factory pictogram recast as a response depot; warm tint. |
| Watchpost | r05 c027 `[352,66,12,12]` | Stockade tower; sand tint. |
| Refuge | r05 c030 `[391,66,12,12]` plus existing Hall canopy | Gate/canopy composition; temporary shelter. |
| Inn | r33 c003 `[40,430,12,12]` | Distinct household miniature; subdued cream tint. |
| Fire | r42 c000 `[1,547,12,12]` | Static bonfire crop, restrained lift/smoke composition. |
| Rabbit | r16 c003 `[40,209,12,12]` | Real rabbit silhouette, four earth-tone variants and existing movement presentation. |

Existing 75 exported PNGs remain byte-identical. A separate nine-body incident actor atlas extends the old eight-body mapping without changing its frame indices. Original static sprites remain static source art; runtime movement/gesture and fire/smoke offsets are derived presentation. Reduced motion retains information with static fire/smoke and existing still poses. Creature inspection is read-only, without a resident Talk command.

Important files under `apps/settlement/`:

- `lib/model.dart`, `condition.dart`, `rules.dart`: catalogue, condition ownership, authoritative repair transactions, affordability and layout history.
- `lib/colony.dart`, `incidents.dart`, `residents.dart`: fixed clock, stable employment, real route/movement integration, bounded hazards and safety planning. Temporary actors retain the same queued-request ownership across world edits.
- `lib/preparedness_codec.dart`, `visitors.dart`: strict checkpoint validation and exact incident/Inn continuation.
- `lib/build_art.dart`, `settlement_game.dart`, `appearance.dart`: new building compositions, stable extended atlas, condition/development/fire/response cues and truthful inspection.
- `lib/main.dart`, `world_creation.dart`, `dialogue.dart`, `reasons.dart`: grouped controls, settings, warnings/outcomes, opt-in migration and contextual conversations, retaining two-bubble cause suppression.
- `lib/browser.dart`, `web/platform.js`: migration-backup boundary. `tool/serve.py`: directory-based HTTP serving resilient to a release rebuild.
- `tool/assets.py`, `goal09_asset_review.py`: reproducible audited derivations. `tool/goal09_fixture.dart` and browser scripts: explicit developer fixtures and normal release-UI journeys; no production incident trigger button.

## Save integrity

Schema **5** persists concrete terrain/generation metadata and all existing colony fields, plus condition, actual usage/repair metadata, mode, cooldowns, incident IDs/history/RNG, active sites/animal movement, participants, safety destinations and response progress. Inn rest/reservation progress belongs to existing visitor checkpoints. A loaded world never regenerates terrain.

Schemas 1–4 preserve existing identities, species, traits, appearances, clock, money, events, visitor memories and original land. They acquire neutral condition and opt out of destructive incidents. Before the first migrated write, the browser preserves the original text in `roag.settlement.landscape.v1.before-incidents-v5`; older v3/v4 safeguards remain for the corresponding older formats. If backup fails, the original stays protected and autosave pauses. Import decodes and validates before asking to replace the current town. New World cancellation never writes a draft over it.

Export/import retains authoritative state. Import intentionally rebases navigation generation to reject requests from the replaced screen, retaining relative pending-request validity. Reload preserves the exact saved generation. Tests and browser comparisons explicitly account for this existing distinction.

## Verification and browser evidence

Baseline: integration begins at `f2c3bf9b11cba3fc771fd5276741a72787c08d4c` on `release/settlement-goal-08`, with a clean tree. Work is isolated on `feat/settlement-goal-09`; previous main/release branches are retained. The original **155 Flutter tests** were rerun successfully in an extracted baseline tree before feature verification.

Verification results, performance measurements, release publication and preservation fingerprints are recorded below when final checks finish.

<!-- GOAL09_FINAL_VERIFICATION -->

## Remaining limits and playtest questions

This is the first bounded implementation: one serious incident, three fire sites, one rabbit, simple on-call rules and temporary shelter. Firebreaks are strong, Watchpost range is deliberately short, and an isolated animal can await a repaired route. The source assets are static small pictograms; response motion and damage/development cues are derived compositions. Human judgement about frequency, attractiveness and satisfaction is separate from deterministic functional victories.

1. Do incidents feel like actual events happening to my town?
2. Does infrastructure make a noticeable difference?
3. Can I understand why responders succeeded or failed?
4. Can I recover without tedious micromanagement?
5. Does the settlement develop a visible history?
6. Are incidents infrequent enough that ordinary building remains enjoyable?
7. Does the town remain responsive at larger populations?

Goal 10 is outside this work.
