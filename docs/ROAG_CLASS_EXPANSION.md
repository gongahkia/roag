# Expansion M5: playable class kits

The title screen now selects one of four classes with keys **1–4** before **N** starts a normal run. The same movement, aim and confirmation keys submit all four kits through the existing deterministic action pipeline. Class content IDs are the player actor IDs; the original Adventurer is unchanged. No class has a mana meter or a separate combat resolver.

| Key | Class / content ID | Starting kit | Basic / special / mobility |
| --- | --- | --- | --- |
| 1 | Adventurer `core:actor/adventurer` | 24 HP, damage 4, speed 100 | Sword Strike / Sweeping Slash / 2-cell Dash |
| 2 | Ranger `core:actor/ranger` | 20 HP, damage 3, speed 115 | 4-cell Bow Shot, first hostile/cover stops projectile / 5-cell Piercing Volley / 3-cell Evasive Dash |
| 3 | Arcanist `core:actor/arcanist` | 21 HP, damage 3, speed 100 | 3-cell lightning Arc Bolt / target-centered 3×3 Fire Sigil / 2-cell Phase Step |
| 4 | Berserker `core:actor/berserker` | 32 HP, damage 6, defense 1, speed 75 | heavy adjacent Crushing Blow / five-cell Broad Cleave / 2-cell Heavy Lunge |

Action recovery is class data. Ranger move/basic/special/dash base costs are 100/100/155/115; Arcanist 100/110/170/115; Berserker 115/130/180/165. The existing speed formula converts these to scheduled recovery. Directional aim remains eight-way for attacks and cardinal for movement. Bow/bolt respect solid projectile geometry and diagonal corner blocking; primary multi-target attacks use the existing simultaneous damage batch. Elemental damage uses the canonical damage/event pipeline. The existing ice Dash extension can add a tile to classes whose base Dash is two cells.

Class ID, stats and abilities are plain data in the normal run snapshot. Route/floor/finale transfers carry the player object and therefore retain class identity, current build and progression. Old Adventurer snapshots remain loadable. Victory summary identifies the class. Placeholder colors distinguish classes until authored Studio art is published; asset IDs remain stable.

This milestone provides the class ability integration seam. M6 must add class-specific transformation boons and class-aware chest eligibility. Numbers are provisional until default two-floor full-run acceptance and human playtesting.

## Later pickup and area-kit addition

The title now accepts **1–6**. **5 Sentinel** (`core:actor/sentinel`) starts with 29 HP, 2 defense and 90 speed. Shield Strike hits one adjacent cell; Shield Pulse hits all eight neighboring cells in one damage batch for 165 base recovery. **6 Alchemist** (`core:actor/alchemist`) starts with 22 HP and 105 speed. Vial Shot is a three-cell, first-target acid projectile; Blast Flask reaches three clear cells and hits a five-cell cross centered there for 165 base recovery. Both retain the ordinary cardinal movement, Dash and scheduler. These new kits have not had a physical playtest.

Normal stage enemies now sometimes drop green **food** (+4 HP) or a red **health pickup** (+10 HP); biome elites guarantee one health pickup. The drop roll derives from the run seed and killed entity ID, so it does not consume combat or chest RNG. Walking over a pickup collects it only while injured, caps healing at maximum HP, and consumes no extra time beyond the committed move. Supply drops and collection are saved with the existing run snapshot. The new class-exclusive, normal-chest boons **Aftershock** and **Reprisal Nova** burst for 2/3/4/6 and 2/3/4/6 raw damage per rarity copy, respectively. Aftershock follows a positive direct hit around its target; Reprisal follows actual hostile damage around the owner. Existing class offer pools remain unchanged.

The headless legal-action driver reached Victory with Sentinel on seed 28 in 896 actions and Alchemist on seed 9 in 1,207 actions. Its Alchemist policy was extended to use the class's three-cell basic projectile. Sentinel did not win with that policy on seeds 9 or 3; those failures do not by themselves establish an encounter defect. These are scripted decisions, not physical playtests.

## M5 evidence

`TMPDIR=/var/tmp luajit tests/run.lua`: **330/330**. The seven new focused tests cover all four registries/snapshots, ranged projectile rules, Ranger volley/Dash, Arcanist terrain and batching, Berserker costs and bump, ordinary-action early combat on default paired floors and Ranger route transfer. Early encounters at seed 3 took 42/28/42/23 legal actions for Adventurer/Ranger/Arcanist/Berserker, ending at 7/17/4/27 HP respectively. This is not a default two-floor legal Victory.

The established game GUI suite passed **24/24** over three isolated rounds; the class title/New Run/ability/HUD GUI smoke passed **3/3** (four fresh processes per round). A separate two-process LÖVE disk test selected Ranger from the title, saved after a legal wait, continued in a new process and compared exact snapshots and the next ordered continuation. No physical keyboard or mouse was operated. A previous local draft of that disk harness followed the `Game.restore` validation copy made during New Run instead of the live game object; the corrected harness tracks the restored live state only in the Continue process.
