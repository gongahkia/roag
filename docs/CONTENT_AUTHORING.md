# Content authoring and developer tools

Wildlife population recovery uses the existing species definition and its habitat rules. Registered Core limits are a three-day minimum after death, four wild-food units per returning individual, pressure below 200, and four attempts per 50 ticks. Oldest deadlines take priority. At most 2,048 population records and 4,096 retained wildlife actors can be materialized; each population retains at most four past individuals and its current one. The current home follows actual migration. Birth creates a fresh actor identity and never repeats the old hunt reward or changes an old corpse into a live animal. Untaken drops and referenced identities remain; history pressure pauses recovery if no safe taken/unreferenced record can be reclaimed. Residual old personal cargo is accounted as lost on reclamation. Population origins/generation counters remain as provenance after old actor metadata is safely reclaimed. `WildlifeRecovery` values are persisted; append instead of reorder. No new content family or hash change is required. `preview recovery` executes actual hunting and three days; private `world-recovery` uses an explicit near-deadline clock fixture for mouse reload/presentation, not a balance claim. Older saves register their existing untagged wildlife gradually, never during load. Future changes to corpse references, rewards or actor removal must update `WildlifeHistoryReferenced` and save validation together.

Wildlife migration uses each character's existing `ecology` biome/floor/resource rules. Food depletion in a hungry animal without carried food, pressure at 600+, a destroyed home or exhausted home resource can start a search. Healthy home decisions consult saved edits/stocks without loading natural terrain. A global budget of 128 candidate visits per five ticks searches a Manhattan radius of 96, rotating by logical tick and actor order. Only unclaimed, food-bearing, low-pressure natural targets are accepted. The shared resident route engine supplies actual steps; it does not move or duplicate an animal when selecting a target. Flee/defense, rest, foraging and combat still take priority. `MigrationReason` is persisted: append values instead of reordering. Optional `NatalHabitat`, `WildlifeAfter` and complete migration records retain provenance, search cursor, target and timing; a beast's saved shared route must match its migration target. Live homes and reservations are unique per chunk; old dead identities/remains are retained. `./dev preview migration --seed 42` reproduces the chain, and private `world-migration` verifies reload/offscreen arrival/return. No new content family, content hash or save-version change. Population recovery now tracks the same current individual/home as described above.

Wild-forage budgets and habitat pressure are registered deterministic Core rules, separate from authored resource-node stocks and regional farming. One optional saved habitat per chunk stores food, pressure, settled hourly epoch and distinct weighted vegetation loss. Base capacities are forest 64, grassland 48, marsh 40, highland 24, snow 16, badland 12 and underground 8. Hourly recovery is at least one, otherwise base capacity / 8; pressure below 200 permits full recovery, 200–599 halves it (minimum one), and 600+ stops it. Pressure drops 20 per game hour, capped at 1,000. Each two-food emergency forage spends two wild food and adds eight pressure; node harvests add eight and a wildlife death adds 120 once. Tree loss weighs eight, grass loss two; every started 32 units of net loss removes one food capacity. Actual vegetation restoration removes loss but does not create food instantly. These are simulation design constants in `WorldHabitats.cs`, not new content definitions or schema fields. `review-tooling` discovers the limits and `preview habitat`; private `world-habitat` exercises presentation. Compatible saves with absent habitat metadata retain their other state and start default wild-food budgets; historical clearing is not reconstructed. Migration uses these stocks as described above; population recovery uses the same food/pressure as described above.

Resident routine priorities are registered Core rules (`RoutineKind`), using existing simulation hunger/rest thresholds, character jobs, stable actor/friend identities and real work orders. Add enum values by appending; persisted numeric kinds must keep their meaning. `./dev preview routine --seed 42` executes actual food and rest trips, and `./dev verify-native --scenes world-routines` checks their presentation (run this under private Xvfb during unattended work). `review-tooling` lists the kinds and previews. Routines themselves add no content family; physical occupations use the rules below.

Physical occupations activate for `farmer`, `woodcutter`, `quarrier` and `healer` jobs. Farmers choose surface resource definitions with a single food yield; `resource.field-grain` supplies eight food per 30-work gather, three gathers and 1,800-tick recovery. Woodcutters/quarriers consume natural, unedited tree/rock cells without another resource/discovery claim. A derived per-town source list is bounded by the 64-tile work radius; actual edits, stocks and work claims remain live. Healers use the registered camp construction and medicine recipes, collecting their real costs from home stocks before work. They repair worn owned camps using wood. No new definition family or schema fields are required.

`OccupationKind` and `OccupationPhase` are persisted enums: append values rather than reorder. Optional actor occupation version one retains one job's identity, source/workplace, progress, input/output ownership, delivered and consumed counts, retry deadline and last movement. Validate every phase and share actual-cargo checks with player jobs. Version zero stays regional until activation; version one permanently excludes a worker from aggregate production, including interruptions. `RegionalResourceFlow` accounts for imports/exports as well as production, consumption and losses; named/regional subtotals exclude duplicate production. Personal emergency foraging uses the finite wild-food budget above. `./dev preview occupation --seed 42` reproduces all four chains; `world-occupations` is its private native gathering/reload/delivery scene. The grain definition changes the gameplay content hash, so use a fresh isolated slot; previous content revisions fail safe instead of silently regenerating a campaign.

Activated residents have optional saved `ResidentJourney` data: origin/target, at most 64 cardinal steps, next-step index, retry tick and terrain revision. Keep these fields validated and geometry consistent with the actor’s position. Search segments retain the 24-tile/256-node local bound; blocked empty routes retry within 25 ticks. Those are Core limits, not new content definitions. Transient generated-ground snapshots include landmark terrain and check live edits/objects/occupants before movement. `./dev preview journey --seed 42` executes matching near/far food and rest trips plus exact saved continuation; `world-routines` exercises leaving/reloading/returning privately. Existing job/routine definitions remain the extension points; no new content family or hash change is introduced.

Recipe, camp-duty and timed discovery costs now use physical work supplies. Queueing reserves available counts from the pack and observed rally storage; pickup, actor cargo and onsite delivery are distinct saved phases. At most 64 source/stack reservations belong to one order. Insufficient supplies or an excessive number of source stacks reject the order without spending anything. `SupplyPhase` values are persisted: append new values rather than reordering them. Supply version zero with absent metadata is the older onsite-reservation representation; version one requires a complete source/phase/carrier ledger matching the declared cost. `./dev preview work-supplies --seed 42` executes the real construction chain; `world-supplies` is the private native fixture. Cancellation can leave remote material in returned supply bundles rather than moving it instantly into the pack. Work outputs use the distinct extraction/delivery ownership state described below; ordinary physical occupations use their own job ledger above.

New material-producing work uses output version one: completing extraction puts the positive authored yield in the worker’s actual cargo, then delivery deposits it in the shared pack or selected observed rally storage. Remaining plus delivered counts must exactly equal the yield and actual carrier cargo must cover every outstanding claim across jobs. Inputs are consumed at extraction. Output version zero with absent metadata retains legacy immediate completion. `./dev preview work-output --seed 42` runs a natural resource-to-storage chain; private native scene `world-output` exercises the destination control, reload and full-storage rerouting. The existing resource/recipe IDs, yield definitions and work rates remain the extension points; no new content family or hash change is introduced.

Run `./dev` from any working directory. The graphics-free `Vagabonds.Tools` project uses the same Core content loader, actor factory, abilities and interactions as the game. Game definitions are embedded at build time; developer commands can inspect edits from `content/` before rebuilding the client. Previews and scenarios never open player saves. `diagnose save FILE` reads only the explicitly selected save and never writes it.

## Discover and edit

```sh
./dev content list
./dev content list character
./dev content inspect character.healer
./dev content references ability.mend
./dev content scaffold character --id character.wayfarer
./dev content validate
./dev preview character character.wayfarer --seed 42
./dev preview interaction interaction.healing --seed 42
./dev content remove character.wayfarer
./dev content remove character.wayfarer --apply
./dev test
./dev review-tooling
```

Edit the corresponding readable JSON file directly. Scaffolding refuses to overwrite existing files. Removal defaults to a dry run and blocks references, including the runtime's required `game.vagabonds` entry point. Update those dependents deliberately before removal. Original asset IDs/crops/bindings remain under their established authoring workflow; content refers to existing Vagabonds sprite aliases.

Definitions have `kind`, stable `id`, readable `name`, optional `description`, `sprite` and `tags`. IDs use `kind.lowercase-name` and remain separate from generated actor instance IDs. `game/vagabonds.json` selects the traveller/NPC archetypes, four classes, common combat actions, boss and recipe pools, and initial inventory. A new definition joins the game by being referenced from an appropriate pool; an unused definition can be previewed without changing spawning.

| Kind | Authoring fields / runtime use |
| --- | --- |
| character | Name/profile pool, class pool or explicit appearance/stats, interactions and enemy behaviour rules |
| names / trait | Generated identity pools, backgrounds, motivations and disposition modifiers |
| class | Role slot, base stats, sprite and three class abilities |
| talent | Generated combat dispositions and permanent class developments; see [companion combat development](PROGRESSION_AUTHORING.md) |
| ability | Target, mana, power, registered effect, status duration and enemy-only preparation turns |
| encounter / reward | Composed enemy roles, habitat eligibility, weighted placement and persistent themed victory grants; see [encounter authoring](ENCOUNTER_AUTHORING.md) |
| interaction | Registered action, maximum distance, item costs and effect amount |
| dialogue | Presentation-only interaction reference, conditional player/reply variants and world tokens |
| item | Inventory key, value, appearance, equipment slot and stat modifiers |
| recipe | Buildable material or camp crafting output, reserved item costs, required work and output amount |
| simulation | Shared hunger/fatigue/rest, meal thresholds, structure wear, memory duration and forage work |
| incident | Registered eligibility condition, effect, mood and cooldown |
| generation | Four-floor layout selection per site purpose and biome tree/rock/water profiles; shared by runtime and audit |
| boss / variant | Authored identity/tactics/weakness, stats, conditional behaviour phases, minions and variation pools |

Bosses, enemy behaviours, equipment, character creation, party abilities and conversations use the content catalog at runtime. Timed recipes, reservations, work/cancellation and crafting now use the same definitions. Enemy rules select weighted eligible actions at the start of each round; cadence, health threshold and phase conditions apply then. Intentions persist through saves; a dead chosen target wastes a single-target action. Enemy medicine/retreat handlers are rejected. `game.enemyFallback` must reference a free damaging attack.

## Validation and previews

`content/schema.json` supplies editor completion and structural validation. Core adds cross-reference/type checks, known sprite/effect/action checks, numeric limits, duplicate-ID/role/inventory-key checks and required game pools. Errors identify the file and field. Unknown properties are errors, so spelling mistakes do not silently become defaults.

After changing the C# definition types, regenerate the schema explicitly:

```sh
./dev content schema --output content/schema.json --replace
```

The test suite checks that the tracked schema matches the exporter. `--content DIRECTORY` previews/validates an isolated content copy. Preview results include the actual generated identity/stats or the result of a real interaction command. Interaction previews preserve logical time and random state, pay actual inventory costs, and use no player save.

Content definitions describe possible people. Campaign actors retain their own identities, resolved stats and changing state. World saves pin the content hash; incompatible content fails safely before replacing an active session. This development expansion uses world save version three and does not migrate earlier world slots. Use an explicit new slot/run after content changes; never delete a player's selected save as a development shortcut.

## Extend a mechanic

1. Add the typed definition fields in Core/Content/Definitions.cs and their validation/reference rules in ContentCatalog.cs.
2. Register and implement a deterministic Core handler. Data cannot execute arbitrary code. Validate costs/targets before mutation and preserve invalid-action no-cost/no-RNG behaviour.
3. Add real authored content that exercises the handler, a reproducible preview and meaningful tests of its effects/failures.
4. Regenerate the schema, update this guide/project state, and run the relevant developer commands yourself.

At each substantial milestone, inspect `review-tooling` and evaluate actual authoring friction, diagnostics and preview coverage. Promote repeated manual checks into commands. Definition counts and passing validation establish consistency, not whether an encounter or world is enjoyable; those require inspection and playtesting.

## Inspect procedural worlds

`./dev preview generation generation.world --seed 42` exercises the configured algorithms. `./dev audit-world --seed 40 --samples 8 --output artifacts/world-audit` writes JSON measurements and an SVG sheet; use `--replace` deliberately for an existing output. It reports raw/final hashes, open-cell density, components, stairs, fallbacks and repairs separately. Inspect the sheet alongside the numbers: distinct hashes alone do not establish interesting worlds.

WFC is retained for towns/ruins. Registered structural algorithms are `bsp-workings`, `bsp-fort`, `caverns` and `ring-sanctuary`; profiles select these without changing code. New algorithms require a Core implementation and validation registration. The gradient-noise implementation was adapted from the pinned Ithica `070558d` source; there is no runtime dependency on that checkout. Macro placement and connected road loops remain Core invariants.

## Preview authored encounters

`./dev preview encounter boss.briar-matriarch --seed 42` runs a generated four-member party against the actual boss factory, variant, minions, intentions and resolver for up to four rounds. It reports actions, health, intentions and shared ticks without saving. This is a deterministic diagnostic using basic attacks, not a combat-balance claim. Every shipped archetype is exercised by the suite. `--scene world-boss` provides the graphical preview at the selected seed. Equipment snapshots preserve item identity and resolved modifiers across saves; swaps return previous equipment to the shared pack.

## Exercise work and shared simulation

`./dev preview recipe recipe.field-medicine --seed 42` queues real work with paid reservations and reports actual progress, output and body effects. `./dev preview simulation simulation.shared --seed 42` assigns a generated resident, advances work, then recruits the same actor. `./dev preview incident incident.helping-hands --seed 42` isolates an incident with explicit eligible fixture conditions and executes its real shared-clock handler. These commands do not save. Every shipped work recipe and incident is exercised by tests.

Simulation profiles apply to everyone; level and traits augment the shared rates. Incident conditions are `hunger`, `exhaustion`, `injury`, `craft`, `trust`; handlers are `memory`, `rest`, `aid`. Aid consumes actual town medicine. Memories are bounded/expiring, trust is directional, and repeated identical conversations have a memory cooldown. A recipe with `output` crafts that item at camp; one without it constructs `material`. Construction receipts record exact paid stacks for dismantling; changing authoring defaults never invents a new refund. New recipe outputs need valid item IDs and new mechanics need registered Core handlers.

## Complete preview coverage and native acceptance

All nineteen content families have a runtime context: `preview character`, `class`, `names`, `trait`, `ability`, `interaction`, `item`, `recipe`, `generation`, `simulation`, `incident`, `variant`, `game`, `discovery`, `dialogue`, `resource`, `camp`, `affix`, and `encounter` for a boss ID. For example, `./dev preview item item.chainmail --seed 42` actually equips the item; `preview trait trait.stubborn` exercises shared work/fatigue, and `preview variant variant.swift` uses an actual boss encounter. Ability previews now resolve the effect in a supplied half-health/stressed traveller fixture, rather than just echoing JSON. Read the diagnostic setup in each report; these are reproducible mechanisms, not balance scores.

CLI typos, invalid numeric flags and invalid/path-like IDs fail explicitly. Scaffolding validates the stable ID before deriving its path. The tooling review was dogfooded against authoring, generation, every boss, every recipe/incident and actual native input; counts remain consistency indicators rather than quality ratings.

After a Release build, `./dev verify-native` runs mouse-only X11/WSLg acceptance (requires xdotool, ffmpeg and DISPLAY). `--dll PATH` selects a published application; `--output DIRECTORY` selects reports/captures. All saves live in a temporary directory. Checks cover ignored gameplay keys, close-camera bounds, automatic approach/conversation/trade/recruitment, reservations/assignment/refunds, timed rest/crafting, equipment, travel, caches/stairs, rejected Load preservation, combat choices/targeting, an explicitly wounded-boss victory fixture and a fresh run. It is a local input check rather than a balance playthrough or cross-platform CI claim.

`--ui-report PATH` writes an atomic development snapshot of control IDs, bounds, enabled/selected states, visible actors, visible cells/materials/solid props/occupancy, context-menu bounds/cost hints, control sprite IDs/bounds/detail/progress/worker badges, illustrated journal rows, tile selections, work assignments/status/priority/suspension, rally, dialogue speaker/text/voice state, camera, route, clock, focus/cursor and battle targets. It defaults to preview isolation: no player save is opened or autosaved unless explicitly loaded/saved. The native verifier uses these semantic targets instead of brittle pixel coordinates or keyboard shortcuts. It forces the client to use X11 to match its xdotool input backend, selects its own process window, waits for rendering/load completion and confirms focus and the actual in-game cursor position before pressing a mouse button; failed runs retain diagnostic UI/captures. New mouse controls need stable IDs so this same verifier can exercise them. The tooling review retains the shared authoring/preview commands and upgrades this repeatedly used native workflow. Its tile/context/drag helpers exercise bulk building, saved digging continuation, click-consumed dismissal and leader changes through real mouse events. Recipe and headless scenarios build camps beside characters and approach recruits without entering occupied cells.

For an isolated Linux display when desktop focus interrupts automation (requires Xvfb/Mesa):

```sh
SDL_VIDEODRIVER=x11 LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a -s '-screen 0 1920x1200x24 +extension GLX +render -noreset' ./dev verify-native --output artifacts/native-mouse-xvfb
```

This still runs the actual MonoGame client, mouse events, rendered panels and save commands. Software-rendered timings are unsuitable for desktop performance comparisons.

## Author speech and inspect delegated work

```sh
./dev content inspect dialogue.stories
./dev content references interaction.stories
./dev preview dialogue dialogue.stories --seed 42
./dev preview dialogue dialogue.rumours --seed 1337
./dev preview party-work --seed 42
```

Edit the existing `content/dialogue/*.json` definition for an interaction; one definition maps to each interaction and the sole definition without `interaction` supplies greetings. Variants have `when`, optional `player` and 1–6 `replies`. Include an `always` fallback. Other supported conditions are `trusted`, `injured`, `hungry` and disposition values `craft`, `security`, `discovery`, `fellowship`. Eligible specific variants take precedence; stable actor hashing selects among them without advancing any simulation RNG. Lines are capped at 400 authored characters and paginate after token expansion. Supported tokens: `{name}`, `{player}`, `{background}`, `{motivation}`, `{job}`, `{friend}`, `{town}`, `{faction}`, `{result}`. `{background}` includes its a/an article and `{motivation}` uses first-person possessives for spoken lines. `{result}` is the actual once-applied interaction result, including local rumour coordinates and facts. New interaction mechanics still require a Core handler; dialogue cannot execute commands.

Dialogue previews apply the referenced interaction in an isolated real world, then verify speech resolution leaves that world unchanged. Text-only changes preserve the gameplay content hash and compatible saves; their separate presentation hash tracks revisions. Changing interaction costs or mechanics still changes the required gameplay hash. The seven dialogue sets cover greetings and every existing interaction. Scaffolding supplies a starting template; choose an unused interaction reference or edit the corresponding existing set before validation.

`preview party-work` generates four real companions and queues four local excavations, reporting concurrent workers, collision/rally invariants, completion and individual work. The native verifier now advances dialogue through semantic controls, checks speaker changes and settings, and verifies independent workers and running time in Party/Work. It uses explicit Pause for reservation snapshots. This replaces manual timing assumptions that stopped working once management panels became live. Automated checks establish sequencing/consistency, not pleasing voice timbre or long-form dialogue quality; those still need listening and playtesting.

Management panels use the existing authored `item.sprite` and each generated actor's actual sprite, so content edits appear in both the world and UI after rebuilding. Work material icons reuse the terrain bindings. Journal presentation resolves only unambiguous named subjects in existing history strings; generic or ambiguous records use a banner without changing save data. Native acceptance covers sprite-bearing controls, worn equipment, worker badges, illustrated history, and a low-food fixture that recovers through contextual gathering. Captures supplement semantic checks; neither alone establishes visual quality.

## Resource nodes and work tools

`resource.*` definitions are registered through `game.vagabonds.resources`. They choose a natural `terrain`, distinct `biomes`, surface/`underground`, coordinate `chancePerThousand`, `capacity`, `work`, `activity` (`harvest`, `fish`, `mine`), item-ID `yield`, and full/depleted sprite aliases. `regrowTicks: 0` means finite; positive values refill an exhausted node after that many running world ticks (300 per hour). Partial stock does not refill. Fishing nodes require adjacent walkable bank terrain. Edits suppress natural nodes.

Items may use `slot: tool` and `workBonuses`, with activity keys `dig`, `build`, `repair`, `craft`, `harvest`, `fish`, `mine` and additive percentages 1–100. All equipped bonuses total at most +200%. Tools speed work; none is required to access resources. Stable ingredient item stacks remain available for camp recipes. Do not add arbitrary mechanic names without a Core handler and validation.

Use `./dev content scaffold resource --id resource.new`, `./dev content references resource.apple-tree`, `./dev preview resource resource.apple-tree --seed 42` and `./dev review-tooling`. The resource preview finds an actual naturally generated node, clears only a worker approach in its isolated fixture, and executes the normal designation/dispatcher pipeline. It reports location, time, stock change and actual inventory output. `./vagabonds.sh --scene world-resources` gives the corresponding native mouse fixture. Removal is blocked while the game pool references a resource. Regenerate `content/schema.json` after changing typed fields.

## Camp activities

`camp.*` definitions are registered in `game.vagabonds.campActivities`. `role` is `cook`, `treat`, `watch` or `stories`; `work` and item-ID `cost` control normal reserved work. Cooking uses `effect: armor|burn-resist|bleed-resist|focus`; other roles use their matching effect. `power` is bounded 1–4 and `duration` is 300–7200 ticks. Meal/watch preparations reach conscious companions within six tiles, replace the same effect rather than stacking, and expire only with running simulation time. Story duration sets its speaker cooldown. Treatment heals `power * 6` HP and removes `power` wounds.

`./dev content scaffold camp --id camp.new`, `./dev content references camp.apple-broth`, `./dev preview camp camp.apple-broth --seed 42`, `./dev preview camp camp.stories --seed 1337` and `./dev review-tooling` exercise the complete authoring loop. The preview creates an isolated serviceable camp, supplies its recipes and executes normal named-worker commands. All preparation types require registered Core mechanics and validation.

Native verification can now target scenarios: `./dev verify-native --scenes world-camp --output artifacts/native-camp`. Choices are `world`, `world-resources`, `world-wildlife`, `world-camp`, `world-discovery`, `world-battle` and `world-boss`; omitting `--scenes` runs all. This was added after repeated full native runs slowed iteration on individual panels. Headless `./dev preview` and native `--scene` fixtures still execute real command paths and use no player saves.

## Equipment effects and seeded variants

`affix.*` definitions provide a readable name, sprite/description, registered `effect`, bounded `power`, and eligible `slots`. Items have up to two fixed `effects` plus an `affixPool` of at most sixteen candidates. These are separate content references; use `content references` before editing/removing an effect. Ordinary crafted items retain fixed effects. Reward alternatives roll one or two different permitted affixes, preserving their base statistics; each generated piece has a stable `gear.*` identity in world state. They never enter the authored content catalog or alter its hash.

| Handler | Runtime behaviour |
| --- | --- |
| `protect` | Blocks damage for a different ally at half HP or less, once per wearer per round; cannot reduce an attack below one damage |
| `burn-power` | Adds direct power to a burning ability (not its later damage ticks) |
| `healing` | Adds healing to heal, cleanse and medicine abilities |
| `resolve` | Reduces stress inflicted by dread |
| `dig`, `mine`, `fish` | Adds percentage work speed to the matching terrain/resource activity |

Combat effect totals cap at 12; combined work bonuses cap at +200%. Shields choose the strongest available protector, then stable unit ID. Their per-round usage is saved. New handlers require explicit Core registration, validation, preview behaviour and tests; JSON does not execute code.

Use `./dev content scaffold affix --id affix.new`, `./dev preview affix affix.sentinel --seed 42` or another registered effect, and `./dev review-tooling`. The preview isolates the requested effect on real compatible gear and submits actual combat/work actions. `./dev preview recipe recipe.wardens-shield --seed 42` supplies its declared ingredients before exercising crafting. Native `verify-native --scenes world-discovery` verifies reward selection, item sprites/effect descriptions, equipping and reload.

### Wildlife and habitats

Wildlife uses `character` definitions with an `ecology` object and references in `game.wildlife`. The registered behaviours are `flee`, `defend` and `roost`; fields select biomes, surface/underground, notice and pursuit radii, optional resource habitat and bounded ingredient drops. Defenders require a real resource reference. Ecology characters spawn through this pool, separately from town/site roles and authored encounter groups. Their combat `behaviors` use the ordinary validated ability rules. The four shipped species reuse the rabbit, spider, bee and bat bindings; silk nests and silk dressings connect spiders to gathering/crafting.

One potential habitat is sampled per observed 64×64 chunk, using coordinate randomness without touching combat/incident streams. Surface habitats avoid town centres; underground habitats stay inside dungeon footprints. Habitats need an open adjacent spawn cell. Initial activation respects occupancy and is saved even for empty chunks. Defeated individuals stay dead; the population recovery rules above can return a distinct new animal after the minimum delay and habitat checks. Rabbits flee without auto-combat; defenders warn by proximity, pursue within a bounded radius and return when intrusion ends. Working at their resource extends provocation for 75 ticks. Only visible defenders appear in gathering warnings. Death leaves one walkable ingredient cache; hunting awards six experience, without the generic gold/food reward.

`./dev content inspect character.bee` and `./dev content references resource.honey-tree` expose habitat/drop links, and reference-safe removal protects them. Use the existing character scaffold and copy the validated ecology object from an inspected species when authoring another animal. Regenerate the schema after typed changes. `./dev preview character character.bee --seed 42` now exercises real habitat activation, movement and contact for ecology characters; ordinary character previews retain their profile output. `./dev audit-wildlife --seed 42 --samples 8 --output artifacts/wildlife-audit.json` checks all configured species across seeds and reports actual escape/encounter outcomes and distinct habitat locations. Its isolated fixtures clear approaches while preserving the naturally selected habitat; it is not a population or balance simulation. `./vagabonds.sh --scene world-wildlife` and `./dev verify-native --scenes world-wildlife` exercise the mouse hunting/harvesting danger loop.

### Discoveries with alternative approaches

An encounter discovery may have `situation` rules: `kind` is `trap`, `spring` or `crossing`; `resolvedSprite` shows its result. `approaches` contains distinct registered actions with name, sprite, description, work and item costs. `disarm` belongs to traps, `repair` to springs/crossings, and immediate `negotiate` to crossings. Work reserves costs and uses the ordinary dispatcher; negotiation pays once immediately. Defenders initially permit inspection/work/parley. Explicit combat provokes them; retreat preserves both injuries and hostility. Resolutions keep actors and references intact.

`layout` now also accepts `~` water. `tunnelWalls` contains two to four offsets relative to the layout centre, matching `#` walls and including opposite offsets. The engine rotates them with the layout and selects one outside the entrance trail. Actual completion of a dig job at that wall unlocks the bypass; a menu click alone does not remove it. Crossings similarly declare opposite `bridgeCells` matching water; non-tunnel resolutions replace the preserved water cell with a real bridge. Every approach reaches the same one-time equipment/supply cache. Resolving a place invalidates/refunds other pending approaches on the next work tick. Outcomes and pending work are strictly validated on load.

Use `content inspect discovery.trapped-storeroom`, `content references item.herbs`, the existing discovery scaffold/schema/removal workflow and `preview discovery discovery.guarded-crossing --seed 42`. The situation preview runs **each** registered approach plus actual combat and excavation, then attempts both a legitimate and a repeated claim. `./dev audit-situations --seed 42 --samples 8 --output artifacts/situations.json` checks all generated situation geometry and the real approach handlers across seeds. Fixtures supply the authored costs and strengthen their test fighter; they verify mechanics rather than balance. `./vagabonds.sh --scene world-situation` and `./dev verify-native --scenes world-situation` cover contextual disarming and physical tunnelling with the native mouse UI.

### Cooperative abilities

`game.cooperativeAbilities` contains up to two distinct ability IDs. These extend the three class and three common actions for parties with at least two battle participants. An ability's optional `cooperation` object declares `partnerMana` (1..20) and `partnerPowerPercent` (0..100); the initiator also needs 1..20 MP. Supported combinations are `damage` / `Enemy` and `cleanse` / `AllAllies`. Cooperative actions cannot enter class/common pools or enemy behaviour/fallback rules. Existing ability scaffolding, reference-safe removal and schema support these typed fields; inspect `ability.twin-strike` or `ability.shared-aid` as examples.

The helper must be a different living, unstunned friendly fighter, have enough mana and an unspent scheduled turn later in this round. Committing spends both mana costs and both actions. Damage/healing adds the declared percentage of the helper's power to the ordinary initiator bonuses. Pending helpers still process their normal status ticks and guard expiry when their scheduled turn arrives, then skip that action. They cannot assist twice; commitments clear at the next round. The save manifest validates pending helper indices against the current order.

`./dev preview ability ability.twin-strike --seed 42` and `./dev preview ability ability.shared-aid --seed 1337` run actual cooperative resolution with four generated wounded companions, normal initiative and a durable foe. They report before/after battle, costs, preview and feedback. `./vagabonds.sh --scene world-cooperation` and `./dev verify-native --scenes world-cooperation` exercise helper selection, cancellation, targeting, skipped turns, group recovery and exact save/load. No fixed protagonist or class pairing is required.


## Validated content tweaks

`content patch ID --patch FILE` applies an object [JSON merge patch](https://www.rfc-editor.org/info/rfc7396/) to a candidate copy and reports validation issues, field differences, transitive dependents, source SHA256 and gameplay hashes. Objects merge recursively, arrays replace in full, and `null` removes a property. Keep patch files outside `content/`, where every JSON document is interpreted as a definition.

For example, write `{"armor":3}` to `artifacts/armor.patch.json`, then run:

```sh
./dev content patch item.chainmail --patch artifacts/armor.patch.json
./dev content patch item.chainmail --patch artifacts/armor.patch.json --apply --expect BEFORE_SHA256
./dev content impact item.chainmail
./dev content diff --against artifacts/content-before
```

Only `--apply` writes. It requires the original SHA256, validates the complete candidate catalog and checks the source again immediately before atomic replacement. Validation failures and stale hashes preserve the file. This is an optimistic single-file edit, not a multi-file transaction or distributed lock; coordinate concurrent edits. IDs and kinds cannot be renamed through patches. `content diff` compares another content directory, reporting added/removed/modified definitions and whether the gameplay hash changes. Dialogue-only presentation changes remain compatible under the existing hash policy.

## Declarative game reproductions

Author scenarios outside `content/`, using [devtools/scenario.schema.json](../devtools/scenario.schema.json). Regenerate it with `./dev scenario schema --output devtools/scenario.schema.json --replace` when the typed scenario model changes.

```json
{
  "schemaVersion":1,
  "name":"A fresh world saves exactly",
  "seed":1337,
  "fixture":"world",
  "steps":[
    {"op":"assert","path":"/partyCount","expected":1},
    {"op":"save-load"},
    {"op":"assert","path":"/tick","expected":0}
  ]
}
```

`./dev scenario run FILE [--seed N] [--content DIRECTORY]` executes it against real Core rules. `scenario suite DIRECTORY` runs sorted JSON scenarios; empty suites fail. With `--output FILE`, stdout is a compact summary and the full trace is saved. Existing outputs need `--replace`. Failed assertions exit nonzero and stop at the first failing step. Reports include the seed/content hash, exact state hashes, compact state and per-step field differences. Hashes cover persistent world state including chunk deltas and random states; they do not measure rendered output or wall-clock performance. Compact snapshots show at most 24 work orders and 12 recent combat events, with total work count/truncation flags; assertions target that snapshot.

Fixtures are `world` (ordinary new world), `resource` (`definition` names a resource), `camp`, `cooperation`, `situation` (`definition` names a discovery), and `wildlife` (`definition` names an ecological character). Diagnostic fixtures explicitly arrange the context through the existing preview helpers; subsequent actions use the normal handlers. The five shipped examples cover gathering, named camp work, discovery resolution, cooperative combat, rejected actions and save continuation.

| Operation | Fields and behavior |
| --- | --- |
| `command` | `kind` is a `WorldCommandKind`; `item`, `amount`, `material`, `actor` or `partyIndex` supply the normal command arguments |
| `designate` | `kind`, `cells:[{"x":0,"y":0}]`, optional `material`; queues normal selection work |
| `advance` | `ticks`; unpauses then advances ordinary simulation, respecting battle/continuation freezes |
| `combat` | `skill`, `target`, `partner` indices; uses the actual turn resolver |
| `enemy` | Advances the real enemy turn |
| `assert` | `path` is a JSON pointer into the report state; use exact `expected` (including null/arrays) or numeric `minimum`/`maximum` |
| `save-load` | Writes a temporary slot, loads through strict validation using the same content, compares all persistent state and restores pause state |

Command/designation targets default to the fixture anchor, offset by `dx`/`dy`; `anchor:"party"` uses the current party leader instead. `partyIndex` resolves a current companion ID. `success:false` expects rejection for command/designate/combat/enemy operations and additionally proves the persistent state was unchanged. Invalid pointers, unknown properties/operations and failed expectations do not silently pass. Scenarios allow 1–256 steps, require an assertion/save check, and bound ticks to 6,000 per step/20,000 total and offsets to 100 tiles. They are a local reproduction language, not a second gameplay engine.

`./dev diagnose save FILE` reports compatibility, hashes and a compact state through the normal loader without saving. Custom content is accepted only if its exact gameplay hash matches. Tools read the selected directory (default `content/`); the client uses its embedded build, so rebuild after changing definitions. Invalid/mismatched saves remain untouched. `diagnose diff BEFORE AFTER` returns at most 100 JSON-pointer differences with large values summarized by hashes; absent and null-valued properties remain distinguishable. Compare explicit chunk JSON files too when investigating terrain changes.

For durable verification, task handoff and evidence-based completion, use [AGENT_TOOLING.md](AGENT_TOOLING.md).


## Combat visuals

Ability mechanics remain in typed gameplay content. Their presentation mappings live separately in `assets/combat-presentation.json`, keyed by stable ability IDs, with effect fallbacks. These timing, color, sprite and sound profiles do not join the gameplay save hash. See [COMBAT_PRESENTATION.md](COMBAT_PRESENTATION.md) for validated fields, references, extension points and isolated native previews. `Skill.Id` and combat-event `AbilityId` retain identity through runtime projection; optional event target snapshots make the visual sequence independent of later resolver mutations.
