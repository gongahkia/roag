# Content authoring and developer tools

Run `./dev` from any working directory. The graphics-free `Vagabonds.Tools` project uses the same Core content loader, actor factory, abilities and interactions as the game. Game definitions are embedded at build time; developer commands can inspect edits from `content/` before rebuilding the client. No player save is opened by these tools.

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
| ability | Target, mana, power, registered effect and status duration |
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

All sixteen content families have a runtime context: `preview character`, `class`, `names`, `trait`, `ability`, `interaction`, `item`, `recipe`, `generation`, `simulation`, `incident`, `variant`, `game`, `discovery`, `dialogue`, and `encounter` for a boss ID. For example, `./dev preview item item.chainmail --seed 42` actually equips the item; `preview trait trait.stubborn` exercises shared work/fatigue, and `preview variant variant.swift` uses an actual boss encounter. Ability previews now resolve the effect in a supplied half-health/stressed traveller fixture, rather than just echoing JSON. Read the diagnostic setup in each report; these are reproducible mechanisms, not balance scores.

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

Native verification can now target scenarios: `./dev verify-native --scenes world-camp --output artifacts/native-camp`. Choices are `world`, `world-resources`, `world-camp`, `world-battle` and `world-boss`; omitting `--scenes` runs all. This was added after repeated full native runs slowed iteration on individual panels. Headless `./dev preview` and native `--scene` fixtures still execute real command paths and use no player saves.

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
