# Expansion M6: 26 tactical and class boons

Normal title **New Run** freezes 46 immediately available boon IDs: the original 20 plus the 26 below. The four previously earned achievement boons remain locked until their existing achievements are earned, bringing a fully unlocked catalogue to 50. Paid, secret and elite/free chests still cache three offers and use their existing rarity weights. Class-specific offers are filtered before sampling, so a class cannot draw a transformation it cannot use. Old direct `Game.new_stage` fixtures and previously saved runs retain their frozen pool; the 20 original identities and their behavior remain unchanged. The boon-content version remains `roeg-boons/3` because older definitions/snapshot representations are compatible; new owned boon state is optional plain data.

All values below are **per copy** in Common / Uncommon / Rare / Legendary order. Mixed rarity copies add power without an ownership cap. Effects use the common event/ancestry/damage pipeline and the player’s current effective stats; these are provisional balance values.

| New boon | Actual trigger and effect | Strength C/U/R/L |
| --- | --- | --- |
| Stonewake | Leave sticky bog; next direct strike within 400 world time adds stone damage | 1/2/3/4 |
| Bramble Compact | A bramble contact sparks the nearest foe within 3 cells | 2/3/4/6 |
| Froststep Catalyst | Dash across ice; strike each distinct hostile adjacent to its actual path | 1/2/3/4 |
| Fracture Dividend | Break a barricade; one-cell shard burst at its event position | 1/2/3/4 |
| Mist Veil | Direct hit while standing in obscuring mist adds damage | 1/2/3/4 |
| Crystal Harmony | Player lightning damage next to crystal arcs to another foe within 3 cells | 1/2/3/4 |
| Sunderer's Mark | Repeated direct hits on the same foe within 400 time build a capped 3-mark fracture; hits 2–3 deal extra damage | 1/2/3/4 |
| Counterweight | Fully blocked hostile hit primes the next special hit within 400 time | 2/3/4/6 |
| Bloodrush | Credited kill grants +speed for 300 simulated time; later recovery uses it | 10/15/20/25 |
| Pain Dividend | Real damage to owner primes the next direct hit within 400 time | 1/2/3/4 |
| Executioner's Thread | Direct hit on an enemy at or below half pre-hit HP adds damage | 1/2/3/4 |
| Shared Agony | Damage to Burning foe with adjacent Burning ally shares fire damage with each such ally | 1/2/3/4 |
| Windup Hunter | Visible hostile locks a cell the owner occupies; retaliatory hit, no automatic interrupt | 1/2/3/4 |
| Crossfire Omen | Visible hostile commits a locked area overlapping another live hostile windup; burst near attacker | 1/2/3/4 |
| Pursuit Spark | Visible hostile moves closer within six cells; retaliatory lightning | 1/2/3/4 |
| Ordered Ranks | Direct hit on a row/column containing at least three living hostiles adds formation damage | 1/2/3/4 |
| Far Witness | Visible distant hostile damage 4–9 cells away sends lightning to the victim | 1/2/3/4 |
| Betrayal Ward | Enemy friendly fire gives a 300-time ward, absorbing up to its power on one future positive hit | 1/2/3/4 |
| Rhythm of Steel | Owner movement primes one direct-hit bonus within 250 time | 1/2/3/4 |
| Cluster Rupture | Every disjoint group of three distinct hostiles damaged in a chain bursts around the third | 1/2/3/4 |
| Ranger Splitflight | Bow Shot branches from its first hit to up to two adjacent, clear hostile cells; bonus shot damage | 1/2/3/4 |
| Ranger Recurve | Piercing Volley extends one clear cell; bonus volley damage | 1/2/3/4 |
| Arcanist Expanding Sigil | Fire Sigil adds a two-cell outer ring where projectile geometry permits; bonus spell damage | 1/2/3/4 |
| Arcanist Alchemical Flip | Arc Bolt on Burning target links to adjacent Burning foes; bonus bolt damage | 1/2/3/4 |
| Berserker Earthshaker | Crushing Blow adds adjacent clear shockwave cells; bonus crush damage | 1/2/3/4 |
| Berserker Battle March | Heavy Lunge primes bonus damage on the next Crushing Blow within 300 time | 1/2/3/4 |

The six class transformations keep their original ability IDs and recovery costs. Splitflight and Alchemical Flip preserve first-hit projectile stop before deterministic branch target selection. Recurve uses the existing piercing volley. Expanding Sigil only adds passable cells. Earthshaker excludes the owner cell. Battle March follows actual legal Dash traversal and adds no free move. Incompatible modifier lists remain explicit/validated; these six affect separate abilities, so none conflict with each other or existing Adventurer transformations.

`prime`, `speed_boost`, `ward` and capped `mark_damage` use compact, validated owner memory with simulated-time expiries, not per-stack timer objects. The 26 definitions themselves contain no special-case branch in `game.lua`; new predicates, ability geometry and effects are reusable. The source art/presentation pipeline and all active-run/profile save formats are unchanged. Boon activation cues remain subject to the existing presentation coalescing/input gate. No physical keyboard balance testing occurred.

## Verification and limits

`tests/expansion_boon_tests.lua` covers all 26 identities in actual game actions, four-tier content validation, 950 seeded three-offer rolls for each class, mixed-rarity serialization, terrain and enemy-originated events, marks/wards/speed/prime save data, ten old/new synergies, all six class transformations, and an assisted stair-boundary bonus. The seed-50 **ordinary-action** paired-floor trace earns currency from ordinary kills, buys Common Rhythm of Steel from paid chest 7 offer 2, then primes it by movement and applies its hit bonus: **59 actions, 21 HP, clock 5,900**; write it with `TMPDIR=/var/tmp luajit tests/expansion_boon_trace.lua /var/tmp/roeg-m6-seed50.trace`. The stair-boundary fixture is assisted and is not counted as a legal full run.

`TMPDIR=/var/tmp tests/expansion_boon_gui_smoke.sh` uses callback-driven LÖVE input to render Ranger Splitflight/Rhythm, confirms the movement input gate and captures four frames. `TMPDIR=/var/tmp tests/expansion_boon_disk_resume.sh` saves a new boon prime in one LÖVE process, continues in another and compares exact snapshots and next action. The full default two-floor legal Victory, all-class Victory, numerical balance and human gameplay feel remain M8 acceptance work.
