# Build-content production matrix

This matrix records the direct production resolver for retained build content. It is an implementation index, not a promise of future mechanics.

## Passive discoveries

| Discovery | Production action or reducer |
|---|---|
| reed sole wraps | `move` avoids mud noise; `guard` carries control into movement |
| counterweight ring | `attack` moves the billhook user into the target's former cell |
| waxed bowstring | `attack` preserves crossbow preparation in hard rain |
| mill-tooth wedge | `attack`/floor interaction permits controlled cudgel or axe breach |
| cliff cord | `_fall` turns an authored drop into a safe descent |
| smoke lens | `sight_radius` preserves adjacent vision inside smoke |
| river hooks | flood controls, container access, water movement and cargo recovery accept the rig |
| witness token | `negotiate` gains leverage when the courier visibly carries valuables |
| rain cape | `move` avoids the extra hard-rain action |
| echo bead | `emit_sound` reports danger stirred on an adjacent level |
| salted dressing | `apply_damage` strengthens the finite field-care intervention |
| high tread | `attack` from above drives a target one pace |
| tide ledger | Ebb Reader can alter Greywash terms; its combination is shown in status |
| cork float | `_lose_goods` preserves one commodity during a current defeat |
| salt veil | `terrain_status_for` filters grit and light smoke |
| wreck key | `open_container` opens a keyed coast locker without tools |
| gull cord | `_fall` keeps physical cargo secured during a vertical descent |
| storm vane | `attack` extends prepared ranged reach in visible adverse wind |
| charcoal mask | smoke FOV increases and smoke inhalation clears sooner |
| resin grip | `attack` preserves bow or tool preparation in wet weather |
| bird whistle | `use_gear` emits a physical decoy sound away from the courier |
| coppice map | `move` uses a quieter, one-action firm gap through Greenwold thorns |
| thorn weave | `guard` stores momentum in thorns; the next close attack gains force and morale pressure |
| ember cloth | `apply_terrain_status` absorbs one smoke crossing per expedition |
| limestone cleat | `terrain_status_for` prevents scree and sharp-ground conditions without boot mass |
| echo slate | `emit_sound` can alert or expose actors across two connected levels |
| quarry brace | floor destruction avoids a laden courier's fall at the authored weak floor |
| sling cup | a sling cast from above ignores partial cover and dazes |
| chalk cipher | `open_container` reads keyed quarry-cache marks without tools |
| fall sail | `_fall` plus rope converts a drop into lateral movement |
| sighting knot | `effective_weapon_range` extends the staff sling and targeting names the longer arcing lane |
| gullbone reel | a hooked-javelin attack with readied rope immediately recovers its physical shaft |
| sluice token | regional control actions operate quietly without role or support substitution |
| cache bell | `use_gear` marks the nearest unopened physical cache once per region and emits an attracting sound |
| scar salve recipe | a finite willow dressing restores five rather than three health |
| load ledger | physical cargo gains two bulk and four weight capacity while still increasing valuables pressure |
| roof nail | one upper-structure move preserves an already prepared ranged target |
| fen sledge | mud applies neither bogging nor first-step broadcast to the carried load |
| ice awl | frozen shallows do not spoil footing; `F` can break a selected thin-ice cell |
| fire rake tooth | `F` breaks burning timber or reeds into smoking ash without a heavy tool |
| limewash seal | wet lime does not abrade the courier or their physically carried items |
| smoke braid | a close attack from smoke adds one harm and morale pressure |
| salvage tally | the first difficult cache recovered per region adds witnessed credit and account confidence |
| counterbrace pin | guard restores one support beneath the courier and cancels its warning |
| pitch cup | one ignition per expedition spends measured pitch instead of lamp oil and gains fuel |
| shingle skids | released-water and current delays do not add an action to a cargo crossing |
| signal mirror | an elevated flash interrupts one aim but shares the courier's position with its group |
| market weights | a physical dependency delivery adds verified stock and institutional confidence |

## Recruit techniques

| Technique | Production effect |
|---|---|
| Ebb Reader | Greywash water movement stays one action; tide-ledger terms alter the objective |
| Cast Bind | a weighted-net hit pulls the restrained target and presses morale |
| Wind Listener | Greenwold crosswind expands sight and makes passage quiet; bird-whistle decoys combine with it |
| Green Poultice | one physical pine-resin dressing clears an injury and restores four health |
| Scree Step | scree and sharp limestone do not impose poor footing or cut feet |
| High Arc | sling range increases; elevated casts ignore partial cover and daze |

## Armour and temporary conditions

| Data | Production use |
|---|---|
| body slot, cut/pierce/blunt protection | `apply_damage` selects a contextual location and reduces that damage kind |
| coverage | low-coverage armour loses one protection unless the courier is guarded |
| condition | armour degrades on a protected hit; low condition reduces protection |
| noise | worn armour adds pressure noise at a displayed deterministic cadence |
| mobility | combined heavy mobility cost makes rough movement and upward climbing take two actions |
| terrain/weather tags | status application checks mud, water, cold, scree, sharp ground, thorns, smoke and salt |
| bogged/current/net drag/brine chill/coalheart chill/fatigue | movement takes two actions until the shown action-count duration clears |
| poor footing/thorns/smoke inhalation | guard weakens unless a directly relevant protection or passive applies |
| chilled/salt grit/smoke inhalation | ranged effective reach is reduced |

The pike and trestle arbalest are obtainable from Whitecairn-influenced merchant stock: the pike while its objective is unresolved and the arbalest after completion. Their existing reach/minimum-range and two-step setup behavior remains distinct from spear and crossbow play.

## New bounded weapon and reward interactions

| Weapon or relic | Production distinction |
|---|---|
| oak staff sling | range 10 with a three-pace minimum; casts over partial cover using physical sling stones |
| hooked river javelin | pulls a target, consumes one physical shaft, and leaves it on the ground unless rope and gullbone reel recover it |
| crossbar boar spear | attacks only outside adjacent range and strongly presses a charging animal's morale |
| powder handgonne | requires aim plus a two-action reload, uses physical charges, produces loud powder smoke, and presses morale |
| stillwater filament | finite selected relic clears current while emitting a strength-five sound that redirects perceived danger |
| flood-mark clasp | lowers nearby water and braces wet timber, then imposes fatigue |
| ashglass lens | clears local smoke and marks a cache, while nearby watchers acquire the courier |
| quarry echo pin | restores at most eight nearby supports and cancels collapse; its loud report raises alerts |
| winter sounding bead | only in winter, freezes at most eight fresh shallows and applies chill |
| red-clay seal | settles a witnessed human claim without harm and records two institutional obligations |
| wreck-light prism | spends lamp oil to mark a cache and turn animals; human lookouts read the flash |

The added objects use recognizable regional containers. Relic rejection paths
do not consume the packed object. Their finite count and exact physical item
are reduced together on success.

## Twenty-four executable build demonstrations

`jomon.build_scenarios.BUILD_SCENARIOS` is the inspectable source for these
rows. The automated scenario test assembles every load from physical weapon,
secondary and passive items, verifies its distinct production combination,
round-trips the state, and re-verifies the effect after load. Focused reducer
tests cited above establish the corresponding action outcome; these are
automated demonstrations, not claims of 24 manually completed expeditions.

| Identity | Physical composition | Decision changed | Production reducer |
|---|---|---|---|
| Quiet route scout | staff, quiet shoes, route survey | trade support for lower ordinary travel noise | `move` |
| Mobile hook fighter | billhook and rope | pull a guard and occupy its old cell | `attack` |
| Armoured brace guard | spear, buckler, Set Stance | spend a turn denying a telegraph and pressing morale | `guard` |
| Weatherproof crossbow courier | crossbow and waxed bowstring | retain prepared aim in rain | `attack` |
| Controlled floor breaker | cudgel, wedge and repair tools | breach a marked floor without an axe | `interact` |
| Smoke walker | long knife, smoke pot and smoke lens | buy cover without losing adjacent sight | `sight_radius` |
| Flood controller | boat hook, rope and river hooks | cross or redirect released water | `move` |
| Healer-survivor | staff, field care and salted dressing | absorb a severe first injury | `apply_damage` |
| Cargo-backed negotiator | sword, seals and witnessed valuables | offer material terms instead of attacking | `negotiate` |
| Elevated marksman | longbow and high tread | turn height into forced movement | `attack` |
| Ebb accountant | net, seals, tide ledger and Ebb Reader | couple route timing to coast terms | `move` |
| Buoyant cargo porter | harness, porter watch and cork float | preserve one lot after current defeat | `_lose_goods` |
| Wind-read bow hunter | longbow, storm vane and Wind Listener | exploit a forecast adverse-wind lane | `effective_weapon_range` |
| Masked smoke hunter | knife, smoke pot and charcoal mask | break pursuit while remaining sighted | `sight_radius` |
| Crosswind decoy scout | longbow, lantern and bird whistle | place perceived sound away from the courier | `use_gear` |
| Thorn brace fighter | sword, buckler and thorn weave | bank guard for a stronger close counter | `attack` |
| Quiet scree courier | staff, quiet shoes and limestone cleat | choose a low-noise ridge route without heavy boots | `terrain_status_for` |
| Elevated sling controller | sling, sling cup and High Arc | arc over low cover and daze from height | `attack` |
| Weighted floor salvager | staff, rope, quarry brace and timber load | use cargo as a breach counterweight | `_destroy_floor` |
| Directed rope descender | staff, rope and fall sail | turn an intentional drop into lateral access | `_fall` |
| Thrown-weapon retriever | hooked javelin, rope and gullbone reel | pull while recovering the physical shaft | `attack` |
| Grounded animal ward | boar spear, Sure Footing and cleat | pin a charge before adjacency | `attack` |
| Fixed roof marksman | crossbow and roof nail | reposition once without losing aim | `move` |
| Verified market factor | knife, seals and market weights | convert one dependency lot into extra stock/confidence | `deliver_dependency` |
# Pressure-family balance audit

The executable `python -m jomon.build_balance --json` audit runs all 24 named
build demonstrations against six recurring pressure families: steady,
strained, critical, elite, voyage and environmental. Answers are derived from
the actual weapon description and range/minimum-range data, secondary gear,
support, technique, physical passives, combo and named production reducer—not
from a hand-authored win flag. The audit rejects a build with no answer to any
family, a family answered by fewer than six builds, or a weapon whose only
identity is damage.

The first run exposed the controlled floor breaker as having no recognised
critical-pressure exit. Its existing, tested controlled-breach reducer already
opens a marked weak floor and therefore supplies a physical escape lane; the
classifier now recognises that production effect as mobility. No damage value
was raised to make the matrix pass. Frontier ranged kit was also diversified:
Rillscar working shooters now use crossbows, Frostmere shooters longbows, and
the fen/terrace throwers retain slings. This changes reload and wet-weather
decisions without adding nominal enemy variants.
