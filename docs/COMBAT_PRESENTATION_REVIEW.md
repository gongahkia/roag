# Combat rules and presentation review

2026-10-23. Read-only game audit and design proposal after the agent-tooling milestone. The requested references are classic Final Fantasy, Darkest Dungeon, Slay the Spire and **One Step From Eden**. The user subsequently selected direction A, improving existing combat presentation first; its implementation and authoring guide are in [COMBAT_PRESENTATION.md](COMBAT_PRESENTATION.md). This document preserves the preceding audit and alternatives. Combat balance and tactical positioning remain separate decisions.

## What exists now

The default living-world client uses `WorldSession` / `WorldBattle`, rather than the retained legacy `Campaign` battle renderer.

- One to four conscious party members face up to four enemies. Nearby hostiles or an encounter's authored followers can join. The world remains the persistent source of health, mana, wounds, equipment and encounter identity.
- Initiative is rolled each round from Speed plus 0–2, with stable identity breaking ties. Enemies choose abilities/targets at the beginning of the round; plans persist through saves and a fallen intended target can waste an enemy action.
- Each class supplies three abilities, supplemented by Guard, Medicine and Retreat. A multi-member battle adds Twin strike and Shared aid. Twin strike spends two actions and both mana costs but resolves as **one combined damaging hit**, not two independent strikes.
- Damage incorporates ability power, level/power, equipment, a 0–3 roll, armour, guard, mark and high stress. Burn, bleed, stun, healing/cleanse, drain, rally, camp preparations and shield protection add existing tactical interactions. Heavy hits can cause lasting wounds; retreat preserves enemy damage; downed companions and party wipes have persistent consequences.
- Pure player-action previews already expose valid targets, reasons for rejection and damage/healing ranges. Selection/cancellation does not spend time or RNG. The enemy intent display currently gives an ability and target, without damage ranges or a turn-order strip.
- Ordinary real-time world advancement freezes in combat. Completing a combat round explicitly advances five simulation ticks; presentation changes should preserve this established clock rule unless a separate design decision changes it.
- Battle positions are assigned placeholder coordinates. The current world resolver has no combat movement, range, line-of-sight, terrain cover or tile-area targeting. Exploration collisions and formation are separate systems.

Sources: [combat resolution](../src/Roag.Core/World/WorldCombat.cs), [enemy plans](../src/Roag.Core/World/WorldEndgame.cs), [queries](../src/Roag.Core/World/WorldActionQueries.cs), [cooperation](../src/Roag.Core/World/WorldCooperation.cs), [clock](../src/Roag.Core/World/WorldSession.cs), [equipment](../src/Roag.Core/World/WorldEquipment.cs).

## Presentation findings

These findings come from source inspection and the real 1024×720 cooperative battle capture in `artifacts/agent/runs/20261022-164752-53175d77/native/cooperation-strike.png`. They are not a long-form balance playtest.

| Finding | Consequence | Recommended response |
| --- | --- | --- |
| Fighters are 48×48 sprites inside fixed 252-pixel-wide panels, arranged as two lists. Much of the center is empty. | The eye reads a dashboard rather than a confrontation. | Put actors on a shared stage; place persistent statistics in a compact HUD. |
| Every damage event uses the same small slash, and healing uses a heart. Panels flash briefly; numbers float for 1.35 seconds. | A sword, arrow, magical bolt and cooperative action have little visual distinction. | Use reusable motion/effect families with separate launch, impact and recovery beats. |
| `CheckBattleEnd` clears `State.Battle` immediately; feedback rendering requires a live battle, and ordinary victory opens spoils immediately. | The finishing strike/fall has no opportunity to finish on the battle screen. | Retain a presentation snapshot until the final impact/fall completes, then show rewards. |
| `_enemyTimer` accumulates during friendly turns and can exceed .65 seconds while the player thinks. An action can advance to an enemy before the same update's automatic enemy check. | An enemy response can appear immediately after the player's click. | Drive automatic enemy progression from completed presentation, with an explicit next-action delay. |
| Effects from one resolution and subsequent status/turn events can arrive together, with no action-level presentation queue. | Numbers, status messages and multiple turns can visually overlap. | Group ordered events by action and play them in a short sequence. |
| Conditions use abbreviations such as G/B/L/M/S; abilities are text buttons despite already having icon aliases. | Existing mechanics take more reading than necessary. | Use icons plus short labels, counters and explanatory hover text. |
| Enemy intentions are truncated text; full initiative order is absent. The right sidebar repeats the expedition and final objective during combat. | The important next threat competes with information that rarely changes mid-fight. | Prioritize order, target portraits, intent icons and truthful damage ranges. |
| Boss content has 48 definitions but only seven base sprite aliases. | Mechanically different threats can look alike. | Give bosses distinct presentation profiles: silhouettes where artwork supports them, scale, sigils, palettes, entrances and phase cues. |
| Audio playback found in the living-world client is conversation speech; the combat presentation path has no attack/hit sound calls. | Impacts lack another strong source of feedback. | Add restrained weapon, impact, guard, heal and status sounds synchronized to the visual sequence. |

The timing and finishing-hit findings are source-level control-flow findings, supported by existing tests that establish immediate battle teardown. They were not separately reproduced as new native timing regressions during this read-only audit. Relevant code: [battle drawing and automatic enemy turns](../src/Roag.Client/WorldGame.cs), [feedback collection/drawing](../src/Roag.Client/WorldCombatFeedback.cs), [controls](../src/Roag.Client/WorldMouseInterface.cs), [feedback tests](../tests/Roag.Core.Tests/World/CombatFeedbackTests.cs).

## What to borrow from the references

The table separates observed reference characteristics from my proposed application to Roag. These are compatible influences, not a requirement to copy four complete combat systems.

| Reference | Useful contribution | Application here | Larger mechanical commitment |
| --- | --- | --- | --- |
| Classic Final Fantasy | Distinct party/enemy staging, a setting-specific backdrop, recognizable actors and attacks, menus below the action | A clear party stage, stepped-forward attacker, visible projectile travel and companion combination poses | An ATB meter or different turn system would be a separate choice |
| Darkest Dungeon | Strong silhouettes, focused compositions and dramatic attack/impact moments | Brief camera emphasis, target recoil, darkened surroundings and distinctive wounds/stress feedback | Rank restrictions, forced movement and positional ability legality would change the rules |
| Slay the Spire | Visible combat information around actors and readable choices in a compact command area | Intent icons, target portraits, damage ranges, status stacks and an inspectable order strip | Cards, draws, discard piles and deckbuilding would change party progression |
| One Step From Eden | Fast spell travel, recognizable spatial patterns and energetic effects on a grid | Distinct beams, arcs, pulses and short multi-actor choreography | Movement, dodging and attack footprints require real spatial rules; real-time reactions would also conflict with the currently agreed turn-based direction |

Square Enix's [official Pixel Remaster page](https://finalfantasypixelremaster.square-enix-games.com/en_US/) includes the FFII forest battle screenshot inspected for staging. Red Hook's [official description/art](https://www.darkestdungeon.com/darkest-dungeon/about/) establishes its party/stress presentation, while [animator Brooks Gordon's breakdown](https://brx.artstation.com/projects/VAVxX) documents authored cutouts and Spine rigging: that asset pipeline is more extensive than moving a single small tile. Mega Crit's [official game page/trailer](https://www.megacrit.com/games/) and [developer-published Steam screenshots](https://store.steampowered.com/app/646570/Slay_the_Spire/) provide the Slay the Spire reference. Eden's [developer press kit](https://www.onestepfromeden.com/presskit/) explicitly describes real-time grid combat and its Battle Network influence. Borrowing its effects does not require adopting its input or deck system.

[Anthony Giovannetti's GDC presentation](https://media.gdcvault.com/gdc2019/presentations/Giovannetti_Anthony_SlayTheSpire.pdf) also supports iterating with both playtests and evidence. Its relevance here is methodological: prototype a few representative actions and compare readability and repetition before expanding the whole library.

## Three coherent directions

**A. A cinematic party stage — recommended first.** Keep current legal targets and costs. Give the encounter the central/full-width play area, with generated companions in a loose formation on one side and enemies on the other. Place initiative at the top, health/status information close to the characters and the selected actor's icon-based ability tray below. Use the local biome/site/depth for a restrained backdrop. A short Darkest Dungeon-like emphasis happens inside the stage when an attack lands. Slay the Spire-like threat information remains readable; Eden-like effects distinguish spells. This offers substantial improvement without first rebuilding tactics.

**B. A close side-view confrontation.** Use a tighter horizontal lineup, larger figures and stronger dramatic cuts. This best emphasizes vulnerable generated companions and dangerous expeditions. With our tiny sprites, aggressive zoom exposes limited detail, so curated larger silhouettes/poses would help. It can initially retain existing targeting; assigning gameplay meaning to front/back ranks would need explicit design work. Its main tradeoff is less room for four enemies, large bosses and readable UI at 1024×720.

**C. A turn-based tactical grid inspired by Eden.** Characters occupy meaningful battle cells; attacks indicate lines, cones, crosses or delayed areas. A mouse click chooses a move/ability, and turns still pause for thought. This could strongly connect to digging, terrain and party positioning later. It requires decisions about movement costs, range, blockers, friendly fire, enemy navigation, generated-arena reachability and how four companions share space. It is a new tactical milestone, not an animation upgrade. An alternative is fighting directly on the exploration map, but that adds further generation/collision and camera obligations.

Slay the Spire's readability is useful in all three. I would retain the current mana/ability model unless deckbuilding itself becomes an explicit goal.

## A concrete first attack sequence

Suggested prototype timings, not measurements from the reference games:

| Beat | Ordinary attack | Twin strike |
| --- | --- | --- |
| Preview | Cursor highlights target; expected damage range and status result appear | Active actor and chosen helper are linked; both mana/action costs are visible |
| Anticipation, roughly 0.1–0.15 s | Actor shifts weight/raises an icon or weapon overlay | Both companions lean/step forward |
| Travel/strike, roughly 0.15–0.25 s | Short lunge, arrow flight or spell arc | Both contribute to one crossing impact |
| Impact, roughly 0.08–0.12 s | Brief recoil, restrained flash, sound and one damage value | One combined damage value, matching the current resolver |
| Recovery, roughly 0.15–0.25 s | Health display settles; applied status appears; actor returns | Both action markers become spent before the next turn |

Aim for ordinary actions around half to three-quarters of a second, with modestly longer cooperative/boss moments. Provide faster playback, reduced motion/flash and sound controls. Hold the final hit long enough to recognize victory, then show rewards. The player should be able to identify the source, target, result and next threat without reading the log. A large number on its own is insufficient.

Spell examples: an Ember burst with an outward spark and persistent burn icon; a readable arrow from bow to target; a shield intercepted between protector and companion; gold particles converging on a healed companion; a dread pulse that visibly affects stress; linked Twin strike participants whose mana decreases together. Preserve distinctions between temporary conditions, a skipped action and lasting wounds.

## Existing artwork worth using

The canonical catalog contains **54 assets classified as effects**, of which **49 have no RPG binding**. All 54 have unconfirmed animation metadata. They are candidates for authored effects, not verified sequential animation frames. The query and metadata were inspected; individual new crops still need review before binding.

| Candidate IDs | Possible use |
| --- | --- |
| `urizen-onebit-r25-c000`, `r25-c001`, `r25-c003` | Different cut, scratch and bite impact silhouettes |
| `urizen-onebit-r24-c094` through `r24-c098` | Red beam components, used after checking alignment |
| `urizen-onebit-r25-c094` through `r25-c102` | Cyan beam/zigzag components for spell travel |
| `urizen-onebit-r13-c026`, `r13-c028` | Ritual marker and portal entrance motifs |
| `urizen-onebit-r43-c029` through `r43-c032`; `r44-c029` through `r44-c032` | Blue/gold spark clusters for status, healing and ward effects |
| `urizen-onebit-r49-c000`, `r49-c001` | Rings for selected targets, wards or pulse effects |

Keep existing IDs/crops/provenance. Use pixel-aligned scaling and deliberate palettes; do not recolor original source sheets. Small actor motions, separate weapon/effect sprites and layered rings can provide a first pass without requiring unique frame animations for every generated person. Full directional combat poses and articulated rigs are a later artwork task.

## Implementation shape if direction A is chosen

1. **Create an ordered presentation layer.** Resolve game actions once through Core; let Client replay an immutable before/after view and ordered events. Retain the battle view through victory/defeat/retreat. Prevent duplicate submissions and automatic enemy progression while the current action is presenting. Skipping playback must not reroll or resolve again.
2. **Make the current decision visible.** Reuse pure previews; expose initiative, meaningful status icons, actor-linked costs and valid targets. Add a pure enemy damage-range query derived from the same rules, without consuming combat RNG. Use stable target hit areas and preserve mouse selection/right-click cancellation.
3. **Stage representative actions.** Start with a melee strike, ranged shot, spell, heal, shield protection and Twin strike, including a finishing hit. Compare restrained and stronger camera emphasis before filling every ability.
4. **Author reusable visual profiles.** Add stable action/ability IDs to presentation data and group events by action. The current event labels are readable strings, and `Skill` does not retain the content ID; names should not become animation lookup keys. Separate presentation profiles from gameplay hashes, following the existing dialogue separation, so a timing/color tweak does not demand a new campaign. Validate actual compatibility before claiming it.
5. **Extend the development loop.** Add presentation phase/action/target/progress to the semantic UI report; capture anticipation, impact, recovery and rewards through the native harness. Pair those checks with scenario assertions that health, costs, RNG, save continuation and completed-round clock behavior match with normal, fast and skipped playback. Loading an already-resolved action should settle the view without replaying its game effects.

Acceptance should include four-versus-four at minimum resolution, long generated names, a large boss, multiple simultaneous status effects, an unavailable helper, a finishing blow, retreat, a wipe, focus loss and save/load during presentation. Visual acceptance needs inspected captures and playtesting; the completed tooling run establishes the current baseline, not the quality of this proposed redesign.

**The first decision:** should the next milestone improve the existing rules' presentation (A/B), or introduce meaningful combat positioning (C)? My recommendation is A first, keeping the architecture open for a later tactical experiment. The amount of cinematic emphasis can then be chosen from a representative playable slice.
