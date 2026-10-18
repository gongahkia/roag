# Approved enemy combinations

## Review and provenance

The user's Downloads export `roag-enemy-combo-feedback.json`, exported
2026-10-18T06:55:45.336Z, approved **30/30** decisions (five behaviours,
twenty actor assignments, five cues). Its SHA-256 is
`c7926800d7164bc3b4c2d6ba911f49314ae7adf0327335d870734b78377a74b3`.
It matches proposal fingerprint
`f99cb126f2a59a65fc5e524008f3cc8537ed36edd87dea8136cff10a9f902a08`
and the review's original mapping fingerprint `f55c179042aea261`.

The [verbatim feedback](sprite-audit/enemy-combo-feedback.json),
[checked acceptance record](sprite-audit/enemy-combo-acceptance.json), and
[original review page](sprite-audit/enemy-review.html) are preserved. The page
describes the pre-implementation proposal and keeps its original draft/import
format. It is an archive; the [live catalogue](sprite-audit/index.html) shows
current mappings. `make enemy-review-check` verifies the archive, approval,
original PNG bytes, and **every selected live actor/cue binding**. Rebuilding
the catalogue does not overwrite the page or invalidate the user's export.

Two comments change the proposal:

> yes these look great thanks just that the guards should be ableto move slightly further fromthe wizardas required as long as they stay within a specific range, and the player can lure them away fromtheir mage as required

> but have more followers, from 2-5

## In-game behaviour

Newly generated stage floors can contain **one additional squad**, provided
an unoccupied open formation fits near an ordinary room's enemy marker.
Existing regular encounters remain. Start/elite cells, stairs, chests and
blocking objects are reserved. No terrain, room art or collision is repainted
to force a group into a cramped room. A failed fit retains the original floor.
Restoring an existing save never inserts a squad into its stored map.

| Group | Behaviour and counterplay |
| --- | --- |
| Ward Weaver + 2 Bodyguard Brutes | Fragile wizard first commits a channel, then protects its own guards within **4 Manhattan tiles** and clear LOS. It never protects itself. Guards pursue within a **6-tile leash**, leaving the field when lured beyond four; beyond six they return. Killing the caster, blocking the link or stunning the caster removes protection. Stun suppression ends with the stun. A flood check treats guards as walls and requires a route to a side of the wizard. |
| Mend Acolyte + 2 Rushwings | Acolyte heals **2 HP** to one wounded squad member within four tiles, through a normal locked channel. A displaced ally does not get followed by the heal. The healer retreats when threatened and follows at longer distance. Wounded bats can return toward support; attackers prefer different approach sides. No resurrection or free attacks. |
| Snare Scribe + Siege Wisp | Scribe locks a visible adjacent escape cell, retaining an untrapped exit when choosing it. At most **2 snares per caster**; each deals **1 raw damage** and adds **40 simulation units** to one recovery, then disappears. The setter is exempt, allies can trigger traps, and caster death clears its traps. Traps persist on their saved floor until consumed or their setter dies. Wisp locks a slow **2×2** blast, retaining ordinary friendly fire. |
| Shield Bearer + Pin Archer | Shield blocks direct hits from its committed **front 90° wedge**. Side/rear hits, DOT and secondary boon damage bypass it; stun suppresses it. The front changes only on an AI action, not when sprite art flips. Archer seeks a cardinal firing lane and repositions if allies block it; a later ally entering its locked shot can still be hit. No wall penetration. |
| Banner Captain + **2–5** Followers | Seeded follower count, no reinforcements. Followers receive **+1 raw damage on future committed attacks** within five tiles and clear LOS of their living unstunned captain. Killing/separating/stunning the captain removes future support. Already committed damage stays locked. |

Strict scheduler timing remains: a placement check does **not** guarantee a
reaction action. Existing attacks still resolve according to speed and due
time. Primary multi-target HP damage remains simultaneous: a brute protected
at the start of a batch remains protected for that batch even if its wizard
dies in the same batch; subsequent damage sees the collapsed field.

Natural homes: Ruins captain; Fungal healer; Aqueduct shield; Bastion wizard,
shield or captain; Observatory trap/artillery; Archive wizard or trap/artillery;
Forge captain or healer. Selection and roster counts derive a separate stream
from the run seed and floor ID. Combat/chest RNG is not consumed by placement.

## Exact accepted art

All indices are zero-based native sheet cells; PNGs remain untouched.

| Role | Mixed Kenney: Tiny Dungeon | Standalone 1-Bit |
| --- | ---: | ---: |
| Ward Weaver | **84** | **319** |
| Bodyguard Brute | 110 | 270 |
| Mend Acolyte | 86 | 123 |
| Rushwing | 120 | 415 |
| Snare Scribe | **124** | **221** |
| Siege Wisp | 108 | 321 |
| Shield Bearer | 96 | 126 |
| Pin Archer | 87 | 78 |
| Banner Captain | 98 | 172 |
| Banner Follower | 97 | 174 |

Shared 1-Bit cues: field **636**, heal **535**, snare **715**, directional
shield **239**, rally **527**. Stable IDs are `core:enemy/<role>` (plus
existing static `/attack` and `/hurt` aliases) and `core:ui/squad_<cue>_effect`.
Tiny actors use the correct 17px gutter stride; 1-Bit uses 16px packed cells.
Classic has labelled tokens and shape cues. No sprite animation is introduced.

Visible protected brutes get a field ring/badge; shield bearers show their
authoritative front; support badges identify healers/captains and bow users.
Links only draw with both endpoints visible, and individual link marks are
clipped to visible tiles. Hidden casters are not exposed by links. Support
windups use a separate outline colour and HUD label. Important cues also have
shape/symbol distinctions in monochrome. Blocked player attacks explain how
to defeat the field/shield; snares report their recovery penalty.

## Implementation boundaries

- `src/enemy_squads.lua`: reviewed definitions, seeded placement, support and
  protection queries, inert saved-data checks.
- `src/squad_ai.lua`: read-only decisions returning ordinary move/wait/windup
  intents. Ten old expansion regular roles remain separate and unchanged.
- `src/game.lua`: normal channel resolution, shared HP batch protection,
  snare contacts, rally commitment and optional snapshot validation.
- Native resolvers/registry/audit: accepted stable-ID art and five cues;
  **324/324** bindings in each Kenney choice, 44 actors, 88 static pose aliases.
  Mixed has 36 distinct bodies, standalone has 44.
- `main.lua`: fog-limited cues and concise feedback. F5 remains presentation
  only; source/world/theme/atlas publication formats are unchanged.

Run schema stays `roag-run/11`, content version stays `roag-content/8`.
The added state is optional; old concrete snapshots continue without newly
spawning squads. New saves preserve leaders, facing, channels, snares and
frozen-floor work. No player class, boon, reward table or existing enemy stat
was changed. New squad members have individual ordinary XP/currency rewards.

## Verification

Commands (use `xvfb-run` on Linux to keep test windows off the desktop):

```sh
TMPDIR=/var/tmp luajit tests/run.lua
make review-test sprite-audit-check
TMPDIR=/var/tmp xvfb-run -a sh tests/repeat_gui_smokes.sh 3 /var/tmp/roag-squad-regression-gui
TMPDIR=/var/tmp xvfb-run -a sh tests/enemy_squad_gui_smoke.sh /var/tmp/roag-squad-gui
TMPDIR=/var/tmp xvfb-run -a sh tests/onebit_gui_smoke.sh /var/tmp/roag-squad-onebit
git diff --check
```

The actual pre-change baseline was **361/361 headless**, **9/9 gameplay GUI**,
LÖVE **11.5**. Focused squad tests cover channels and interruptions, range/LOS,
guard lure/leash, batch protection, leader death, directional shields, healer
locks, canonical artillery friendly fire, trap bounds/cleanup, save bytes,
old snapshots and seeded placement. The 210-floor corpus placed **209 squads**,
all five types and every 2–5 follower count; one floor safely omitted a group.

The GUI harness renders all five groups in all three packs, checks the actual
native actor Quads drawn, captures 15 screenshots, then verifies a pending
channel across two separate disk processes with identical future events.
Snapshots must remain byte-identical through rendering. Artifacts/logs are
retained under the supplied `/var/tmp` output directory.

These are scripted headless/callback tests. Physical mouse, keyboard, Wacom
and native GPU performance are untested. The prepared GUI fixtures are not
claimed as legal full runs. Legal-run drivers submit normal actions and may
read complete state; they do not weaken enemies, give boons or teleport.

Final regression counts and any corrected test-strategy failures are recorded
below after verification completes.
