# Approved idea implementation — sustained goal

Status: **ACTIVE — user resumed the 50+ additional-boon batch and final acceptance**.
The previous terrain/environment stop was honored. The user has now explicitly
requested the remaining boon and final-integration passes, web research as
needed, atomic commits, and LÖVE testing for final acceptance. Earlier paused
notes below are historical checkpoints. The full goal remains incomplete until
both passes are implemented and independently verified.
No feature is complete merely because a schema, fixture or metadata record exists.

## Authority and invariants

- User feedback: [original export](../sprite-audit/ideas-feedback.json), [interpretation](../sprite-audit/ideas-feedback-summary.md), [constraints](../sprite-audit/ideas-decisions.json).
- 55 approve / 30 reject / 4 redirect; six approved classes, 12 approved enemy concepts plus the changed Wraith, six items, eight rooms, three terrains, 14 environment kits plus merged Sanctuary, six presentation directions.
- Skull dice outcome: ordinary regular enemies only; miniboss/final/special encounter exemptions.
- Pilgrim becomes a boon; Cantor class and Oracle room remain rejected. Use the requested hat/cup/dice/skull/cross art for suitable boons.
- All exact sprite exclusions and previous rejections apply. Webs use drawn lines; use only 416 for Wraith. No rejected bulk-kit/bitmap/UI-skin feature is smuggled back in.
- Keep deterministic scheduler ordering, batch HP semantics, friendly fire, authoritative RNG, frozen inactive floors, exact save continuation, legal progression and stable visual IDs.
- LÖVE GUI testing is now authorized for the final acceptance pass. Use isolated
  save directories, bounded processes and retained logs/screenshots; never imply
  callback testing is human play or physical keyboard/mouse/tablet acceptance.
- Commit each completed task separately; keep partial tasks unclaimed. Never reset or clean user changes.

## Baseline

- `TMPDIR=/var/tmp luajit tests/run.lua`: **377 passed, 0 failed**, unmodified gameplay; log `/var/tmp/roag-ideas-baseline-20261019.log`.
- Baseline includes ordinary-action full-run Victory/replay and the 210-floor squad corpus.
- Review archive/freshness and six feedback state checks pass. Source PNGs and user-exported bytes are preserved.

## Work ledger

| ID | Approved direction | State | Evidence / task commit |
| --- | --- | --- | --- |
| `reanimator` | Grave Cantor + finite resurrection | headless verified; GUI pending | 407/407 full, nine focused cases, 210-stage natural company corpus, fresh-process pending-revival Continue; `f8140a41` |
| `haunted-cache` | Haunted cache / chest mimic | headless verified; GUI pending | 421/421 full, six focused wake/bite/art/save tests, five fresh-process disk scenarios; `b73eeb4a` |
| `heavy-rammer` | Heavy Rammer + positional shove | headless verified; GUI pending | Source `38c34a96`, hardening `0f643d9e`; 389/389 full, 12 focused, fresh-process rotating-store Continue |
| `web-weaver` | Web Weaver + anchored webs | headless verified; GUI pending | 444/444 full, nine focused cases, eight disk scenarios including third-process web crossing and save generation 3; `e6c911d7` |
| `hollow-wraith` | Hollow Wraith + announced phase step | headless verified; GUI pending | 398/398 full, nine focused phase/art/save checks, fresh-process Continue; `707c0aec` |
| `gulp-toad` | Gulp Toad + recoverable stolen drop | headless verified; GUI pending | 415/415 full, seven drop tests plus a committed-box Dash fixture, four fresh-process disk scenarios, 337/337 native bindings; `ae231d75` |
| `mirror-hexer` | Mirror Hexer + reflected lane | headless verified; GUI pending | 475/475 full, twelve focused cases and eleven disk scenarios including third-process reflected fire; `54f13add` |
| `trail-worm` | Silt Worm + temporary bog trail | headless verified; GUI pending | full 485/485, current combined 109/109 and all-enemy full 498/498; eleven focused cases and twelve disk scenarios; `caedcd77` |
| `pain-linked-twins` | Oath Twins + breakable pain link | headless verified; GUI pending | 453/453 full, nine focused cases, 100-stage pair corpus, nine fresh-process disk scenarios; `23a8b7f8` |
| `alarm-keeper` | Alarm Keeper + one interruptible call | headless verified; GUI pending | 434/434 full, seven focused cases, seven fresh-process scenarios; `33f22d03` |
| `ember-moth` | Ember Moth + readable burning landing | headless verified; GUI pending | 498/498 full, twelve focused cases, 120 combined and thirteen disk scenarios including third-process actual fire pulse; `401e0c28` |
| `bone-splitter` | Bone Splitter + reward-bounded fragments | headless verified; GUI pending | 427/427 full, six focused cases, six fresh-process disk scenarios, native skeleton/fragment art; `087ec1ad` |
| `rotating-sentinel` | Rotating Sentinel encounter | headless verified; GUI pending | 463/463 full, eight focused cases, 100-stage optional-room corpus, ten disk scenarios and four real-action driver fixtures; `199ddfd2` |
| `duelist-class` | Duelist — precision and paid riposte | headless verified; GUI pending | 510/510 full, twelve focused cases, 133 combined, legal seed-9 Victory at 977 actions / 26 HP, two-process checked-store riposte Continue; see [class guide](../ROAG_IDEA_CLASSES.md) |
| `geomancer-class` | Geomancer — one temporary bulwark | headless verified; GUI pending | nine focused, 46 contracts, fresh-process finite-expiry Continue; frozen full regression running separately |
| `spiritbinder-class` | Spiritbinder — one commanded familiar | headless verified; GUI pending | six focused, 52 contracts, checked-store fresh-process commanded attack; source `/var/tmp/roag-spiritbinder-4t12eze6` |
| `pilgrim-class` | Pilgrim — user redirect into a boon, not a class | pending boon batch | Original explicit redirect; no Pilgrim class |
| `tinkerer-class` | Tinkerer — one limited bolt sentry | headless verified; GUI pending | seven focused, 59 contracts, fresh-process pending-shot Continue; source `/var/tmp/roag-tinkerer-d3ip3v1q` |
| `stormcaller-class` | Stormcaller — charge, ground, discharge | headless verified; GUI pending | seven focused, 66 contracts, fresh-process pending-cross Continue; source `/var/tmp/roag-stormcaller-dd_9qvib` |
| `wayfinder-class` | Wayfinder — a visible return beacon | headless verified; GUI pending | eight focused, twelve-seed room-bound checks, checked-store fresh-process return; combined all-class 553/553 gate |
| `offering-font` | One-use HP offering font | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `ration-cache` | Sealed ration cache | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `cleanse-flask` | Single-use cleansing flask | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `escape-scroll` | One-charge escape scroll | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `return-token` | Room-bound return token | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `charm-well` | Charm well — choose one short condition | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `attunement-forge` | Attunement forge — trade one stack | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `travelling-trader` | One-visit supply trader | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `memorial-choice` | Memorial — retire a boon for recovery | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `bell-sequence` | Bell-order side room | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `heat-vents` | Clocked heat vents | headless verified; GUI pending | 20 focused, 56-floor route corpus, three natural legal scenarios and three fresh-process saves; see [terrain guide](../ROAG_IDEA_TERRAINS.md) |
| `push-floor` | Push-floor strip | headless verified; GUI pending | 20 focused, 56-floor route corpus, three natural legal scenarios and three fresh-process saves; see [terrain guide](../ROAG_IDEA_TERRAINS.md) |
| `fragile-crossing` | Fragile optional crossing | headless verified; GUI pending | 20 focused, 56-floor route corpus, three natural legal scenarios and three fresh-process saves; see [terrain guide](../ROAG_IDEA_TERRAINS.md) |
| `falling-fixture` | Announced falling fixture | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `signposted-routes` | Pictorial route signposts | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `camp-choice` | One-use camp choice | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `archive-contract` | Archivist’s optional contract | headless verified; GUI pending | 35 focused cases, 70 generated floors, seven fresh-process save scenarios; see [item/room guide](../ROAG_IDEA_INTERACTIONS.md) |
| `secret-mural` | Readable secret murals | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `crypt-kit` | Ossuary / crypt environment kit | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `sandstone-kit` | Pale sanctuary — ground and masonry | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `observatory-mosaic` | Observatory mosaic and cold stone | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `root-garden-kit` | Root garden and overgrown courts | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `drowned-sanctum` | Drowned sanctum water kit | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `forge-masonry` | Forge masonry and ironworks | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `library-carpet` | Archive carpets and patterned ground | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `broken-cavern-kit` | Broken cavern — rubble, damaged edges, cave mouths | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `ruin-masonry-kit` | Ruins masonry variants | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `camp-furniture` | Camp and inn furniture | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `workshop-fixtures` | Workshop fixtures and supplies | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `archive-shelves` | Archive bookshelves and empty shelf states | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `chapel-windows` | Chapel windows and alcoves | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `prison-dressing` | Prison cells, bars and grates | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `stone-column-kit` | Pillars, plinths and architectural supports | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `weapon-icon-families` | Weapon and attack icon families | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `status-icon-family` | Status, supplies and reward pictograms | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `combat-bursts` | Static combat impact cue family | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `trap-signals` | Switch, hazard and trap state markers | headless verified; LÖVE/human pending | 20 focused, 70-floor family corpus, two fresh-process saves and 18 real-render-path CPU pixel views; see [environment guide](../ROAG_IDEA_ENVIRONMENTS.md) |
| `oracle-choice` | Oracle art redirect; no Oracle room | pending boon/presentation batch | Preserve the rejected room constraint |
| `onebit-symbol-library` | Remaining 1-Bit item, cue and label library | pending | — |
| `cantor-art-redirect` | Hat 33 / cup 824 for boon/item concepts; no Cantor class | first two uses headless/GUI verified | Beggar's Cap / Overflow Chalice; see [new boon guide](../ROAG_IDEA_BOONS.md) |
| `remaining-icon-boons` | 50+ distinct meaningful boon concepts, suitable unused icons, catalogue comparison and deterministic integration | in progress — first 7 additions / 60 total | 22 focused, 77 combined, 3 separate actual GUI processes; supply/reward/save/native pixel checks; full updated gate pending; at least 43 more required |

## Dependency-based batches

1. Archive user-reviewed snapshot and preserve source/decision checks while the live catalogue grows.
2. Real enemy slices: Rammer/displacement; Wraith phase/attack; revival and finite fragments; cache/drop swallowing; web/links/reflection; finite alarm/ember/sentinel. Each enters actual generation and renderer, with headless/save/seed tests.
3. Six class slices: Duelist, Geomancer, Spiritbinder, Tinkerer, Stormcaller, Wayfinder. Authoritative state, targeting previews, controls, title selection and saves.
4. Finite items and optional room interactions with real discoverable choice UI and explicit costs/cancellation.
5. Heat vents, push floors, optional fragile crossings; saved clocks and safe connectivity, not render-dt mechanics.
6. Crypt/Sanctuary merge and each approved existing-biome/dressing kit. Native/monochrome art, correct ground/wall/prop assembly and source separation.
7. Pilgrim and dice redirects, Cantor-art boons, 50+ distinct additional boon designs/integration, weighted offers/stack/proc/preview/save tests.
8. Full acceptance audit: every ledger row gets runtime/code/test evidence, full headless and legal-run corpus, persistence, no excluded feature/asset leaks. GUI/hardware limits remain explicit.

## Research supporting concrete choices

- [Lua 5.1 manual: deterministic numeric iteration and unspecified `next` order](https://www.lua.org/manual/5.1/manual.html#pdf-next). Use explicit array ordering/tie breaks for effects, revivals and spawns.
- [LÖVE ImageData:mapPixel](https://love2d.org/wiki/ImageData:mapPixel) and [11.5 source](https://github.com/love2d/love/blob/11.5/src/modules/image/wrap_ImageData.cpp). Cached derived monochrome pixels retain alpha; original files remain unchanged. Wiki fetch may return 403, so use the tagged official source as API evidence.
- [LÖVE 11.5 graphics source](https://github.com/love2d/love/blob/11.5/src/modules/graphics/wrap_Graphics.cpp). Validate resource setup against supported APIs, not removed Image:getData behavior.
- [Kenney 1-Bit](https://kenney.nl/assets/1-bit-pack), [Caves & Dungeons](https://kenney.nl/assets/roguelike-caves-dungeons), [Tiny Dungeon](https://kenney.nl/assets/tiny-dungeon). Current upstream attribution/CC0 checked; use the reviewed local sheet checksums/strides as the exact mapping authority.

## Last verified action

- Task 0: reviewed inventory, user feedback and original PNG hashes remain frozen;
  live sprite assignments are separate. Six feedback state checks and all prior
  30/30 squad decisions pass. Task commit `0f9fbd9f`.
- Rammer/driver hardening: **389 passed, 0 failed**, full frozen source run
  `/var/tmp/roag-rammer-final-headless-20261019.log`; task commit `0f643d9e`.
  Twelve focused tests and a fresh-process rotating-store Continue pass.
- Wraith: integrated into the live source. **398 passed, 0 failed**, full run
  `/var/tmp/roag-wraith-final-headless-20261019.log`. Nine added focused tests
  cover visible wall crossing, separate attacks, occupied landing cancellation,
  sight queries, interruption, Trapper contact, exact saves and only 416 in both
  packs. The live combined focus run is **21/21**; both encounter disk scenarios
  pass write → new process → Continue → second checked save generation.
- Cantor: **407 passed, 0 failed**, frozen full source run
  `/var/tmp/roag-cantor-final-headless-20261019.log`; includes all six original
  class Victories, all seven miniboss routes, exact replay and disk continuation.
  Nine focused tests include the 210-stage / 420-floor company corpus.
- Toad: **415 passed, 0 failed**, final frozen source
  `/var/tmp/roag-toad-final-verified-l34a8ps4`, log
  `/var/tmp/roag-toad-final-verified-headless-20261019.log`. All legal class and
  route gates, old seed-3 Victory/profile, fresh-process drops and native art pass.
- Haunted Cache: **421 passed, 0 failed**, full source
  `/var/tmp/roag-cache-strategy-check`, log
  `/var/tmp/roag-cache-final-verified-headless-20261019.log`. The copied live
  implementation matches those source/test bytes exactly. Five disk scenarios
  pass; the original review and all PNG bytes remain unchanged.
- Bone Splitter: **427 passed, 0 failed**, source
  `/var/tmp/roag-bones-qj45lqig`, log
  `/var/tmp/roag-bone-final-headless-20261019.log`. Six disk scenarios and native
  art checks pass. The new fragment deliberately shares skeleton 323 with a
  smaller whole-pixel render size; all other standalone actor bodies stay unique.
- Alarm Keeper: **434 passed, 0 failed**, source
  `/var/tmp/roag-alarm-_f_mxyws`, final log
  `/var/tmp/roag-alarm-final-verified-headless-20261019.log`. Seven disk
  scenarios pass. A caster must be visible before committing its one call.
  Paired-floor melee combat now breaks the same verified chase cycle as ranged
  combat; no AI, HP, geometry or timing rule changed. The formerly failing
  seed-21 Aqueduct route clears at 22 HP after 646 normal actions.
- Web Weaver: **444 passed, 0 failed**, frozen source
  `/var/tmp/roag-web-ty1eeyte`, final log
  `/var/tmp/roag-web-final-verified-headless-20261019.log`. All source/test bytes
  copied into the live checkout match this verified tree. Nine focused web
  cases and eight fresh-process disk scenarios pass. The web scenario also
  crosses the same saved line in a third process and checks generation 3.
- Oath Twins: **453 passed, 0 failed**, frozen source
  `/var/tmp/roag-oaths-8hajnz9s`, log
  `/var/tmp/roag-oath-final-headless-20261019.log` (the initial log filename
  says combined/focused, but this was the complete unfiltered suite). Nine
  focused cases, a 100-stage natural pair corpus and nine disk scenarios pass.
  The integrated runtime/tests match that verified source exactly.
- Rotating Sentinel: **463 passed, 0 failed**, frozen source
  `/var/tmp/roag-sentinel-ol4fnv3b`, log
  `/var/tmp/roag-sentinel-accepted-headless-20261019.log`. Runtime and tests
  match the accepted tree. The final gate retains all original legal Victories,
  seven miniboss routes, deterministic replay and generation corpora.
- Mirror Hexer: **475 passed, 0 failed**, frozen source
  `/var/tmp/roag-mirrors-m1xoqczk`, log
  `/var/tmp/roag-mirror-accepted-headless-20261019.log`. Integrated runtime
  and tests exactly match that source. Twelve focused cases, eleven disk
  scenarios and generation-3 actual reflected fire pass.
- Silt Worm: full **485/485** at
  `/var/tmp/roag-worm-final-headless-20261019.log`; current four driver cases
  and combined **109/109** at `/var/tmp/roag-worm-accepted-combined-20261019.log`.
  Only test-driver hardening changed after that first full gate. The same Worm
  gameplay also passes in the complete all-enemy **498/498** gate. Twelve
  fresh-process scenarios pass, including the concrete pending charge/trail.
- Ember Moth / all approved enemies: **498 passed, 0 failed**, frozen source
  `/var/tmp/roag-moths-c89j2ljy`, log
  `/var/tmp/roag-moth-final-headless-20261019.log`. Every integrated runtime/test
  file matches this full verified tree. Thirteen disk scenarios also pass; the
  Moth case resolves a real saved pulse in a third process. All 13 enemy
  concepts are now headless integrated; GUI/game-scale readability is pending.
- Duelist: **510 passed, 0 failed**, frozen source
  `/var/tmp/roag-duelist-y5_43u37`, log
  `/var/tmp/roag-duelist-final-verified-headless-20261019.log`. Twelve focused
  cases, 133 combined checks, a genuine legal seed-9 Victory (977 actions,
  26 HP) and fresh-process checked-store riposte continuation pass. Runtime
  and tests match the verified source; the separately added disk script also
  passes. Five approved classes and the other batches remain unfinished.
- Both Kenney choices now resolve **379/379** required bindings, 60 actor IDs and
  120 static pose aliases; the user-reviewed 324-binding archive remains unchanged.
  Original source PNGs, archived comments and prior selected bindings pass checks.
- No LÖVE window, physical keyboard/mouse/tablet or native GPU was exercised.

### Regressions diagnosed and corrected

The first expanded full run was 383/3: an obsolete encounter-pool assertion,
an invalid route-history fixture, and a guided navigation limit. Later driver
revisions exposed more strategy failures; their `/var/tmp/roag-*` logs are kept.
Verified automation defects included endless safe-cell dodges, shooting through
blocked projectile corners, ignoring ward-counterplay and treating support
markers as damage. The legal driver now consults actual attack geometry and
uses ordinary attacks, lures or movement after a proven stall. No game actor
stats, geometry, scheduler rules or combat logic were changed by these fixes.

The older single-floor seed-3 route abandoned its earned third-elite chest and
health supply, reaching the finale at 5 HP. It now claims that real reward and
collects the existing pickup by normal actions. It wins at 9 HP after 1,039
legal actions and replays to an identical full snapshot; the Victory/profile
checks pass. See `/var/tmp/roag-seed3-earned-recovery.log` and
`/var/tmp/roag-seed3-gates-accepted.log`.

### Current work and retained diagnostics

Web Weaver is integrated. Two spent-on-commit casts place locked, nonblocking
lines; a crossing consumes its line for +50 movement recovery, or a direct
attack cuts an anchor. Expiry uses simulation time and pauses with its floor.
Source Tiny 122 is retained; native 1-Bit 366 is the inspected alternate
crawler. Rejected 414/463/Micro 60 remain excluded. Original PNGs are intact.

The first Web full run was **442/1**, log
`/var/tmp/roag-web-final-headless-20261019.log`. Arcanist seed 45 chased a
nearby off-axis Warden with 1 HP instead of using its real Fire Sigil. The
driver now checks that existing area attack before chasing alignment; a
prepared regression checks real action cost/hit and the full legal run wins
at **32 HP after 1,436 actions**. No game stats, RNG or enemy timing changed.
The final full result is **444/444**. Failed evidence remains available.

Earlier failure logs are also retained: Alarm **432/2** (stale derived audit
and melee chase cycle), Toad **412/2** (Alchemist/Forge strategies), a broader
Dash policy **413/2** and Cache **417/4** (legacy/Regent tactics). The final
precise policy preserves old single-floor/Regent strategy and passes all
legacy gates. The earlier Bone gate using an outdated copied driver was
stopped and retained as `roag-bone-stale-strategy-aborted-20261019.log`.
These are strategy/staging diagnoses, not claimed simulation bug fixes.

Oath Twins is integrated. The expanded natural pool exposed a narrow Bone
spawn fixture: its first 50 seeds no longer selected a Splitter. A pure
selection query now identifies a candidate, then the test constructs and
validates that genuine generated stage. Exact before/after snapshots of
24 generated seeds confirm that extracting the query changes no generation.
The initial combined run was 75/1; its failure log is retained at
`/var/tmp/roag-oath-combined-focused-20261019.log`. Final full gate: 453/453.

Rotating Sentinel is isolated at `/var/tmp/roag-sentinel-ol4fnv3b`. Eight
focused cases, all 84 combined encounter checks and ten fresh-process disk
scenarios pass. It is an optional champion in noncritical rooms, with four
separately locked clockwise strikes. Stun/displacement ends the sequence even
in the recovery gap. The first 6/2 focused result was fixture timing/map API
assumptions, corrected against actual event traces and `Floors.map`. Its
first full gate was 460/1: Arcanist seed 45 repeated axial corridor dodges.
A safe lateral tie-break wins at 32 HP / 1,544 legal actions. The next gate
reached 461/1: Berserker seed 21 repeated harmless, fully armored dodges on
the stair route. After a proven six-move cycle, stair travel can ignore zero
HP-loss attacks while still avoiding Rammer shoves and damaging footprints.
The same legal run now wins at 36 HP / 1,242 actions. Four prepared driver
fixtures pass. No actor stats, game input, RNG or scheduler were changed.
Its corrected **463/463** full gate passes and this slice is integrated.

Mirror Hexer is isolated at `/var/tmp/roag-mirrors-m1xoqczk`. Eleven focused
checks cover one directional bounce, range preservation, pre-damage blocking,
original shooter/friendly fire, fixed enemy windups and exact saves. Disk
checking caught the missing new support kind in the pending-windup save
allowlist. That missing allowlist entry is corrected. Eleven disk scenarios,
including a third-process actual reflected shot, and the initial 95 combined
checks pass. Charges and Hookrunner grapples are explicitly excluded from
reflection to keep their displacement aligned with their damage. That adds
one real-action regression; a full 474/474 gate passed. The final driver
hardening is propagated and the corrected full gate passes **475/475**;
the slice is integrated. All unused/held/rejected art constraints remain in force.

Silt Worm is isolated at `/var/tmp/roag-worms-efcksv09`. Eleven focused
cases, 108 combined checks and twelve fresh-process scenarios pass. Its two
spent charges can leave only genuinely vacated plain floor as a finite bog
overlay. Walking / Dash pay +40 / +20 per affected cell in the real preview
and scheduler; underlying row bytes never change. The full **485/485** gate passes. Its gameplay code is unchanged by the
subsequent fourth driver fixture; the combined current checks pass **109/109**.
The complete all-enemy **498/498** gate also passes; this slice is integrated.

Ember Moth is isolated at `/var/tmp/roag-moths-c89j2ljy`. Twelve focused
cases, 120 combined checks and thirteen disk scenarios pass, including a
third-process fire pulse. Fixed landings cannot overlap actors. Two separately
scheduled fire pulses remain visible through the existing pending-threat query
and expire after 200 world time. Burning and Temporal Echo continue exactly
alongside them. The complete all-enemy source passes **498/498**, log
`/var/tmp/roag-moth-final-headless-20261019.log`; this slice is integrated.

Headless browser inspection of the catalogue passed 12 pack/filter cases,
mobile layout and search without JS errors at the earlier 341-binding state:
`/var/tmp/roag-ideas-current-catalogue-20261019`. Current derived catalogue
freshness and native references are checked automatically. Browser artifacts
are HTML inspection, not LÖVE rendering or physical input.

All remaining ledger entries stay in scope. Goal ACTIVE; no full-goal
completion claim is made. No LÖVE windows were opened.

Native encounter cells were inspected in a headless HTML contact sheet:
`/var/tmp/roag-approved-encounter-native-pixels.png`. This is not a game
screenshot. The initial full Chromium screenshot command stalled and was
stopped only for its dedicated test profile; its log is retained. The bounded
headless-shell retry succeeded. No LÖVE window or physical input was used.

Duelist is integrated from `/var/tmp/roag-duelist-y5_43u37`. Twelve focused
cases pass after correcting two fixture assumptions (Guard cardinal reach and
blocked-cell preview). Parry costs a normal action, blocks one eligible frontal
melee impact during that recovery only, and funds one next paid thrust for
+2 damage. The combined 133 checks and full **510/510** gate pass. A persistent
saved-riposte HUD cue was included in that final gate; the earlier unfinished
log is retained. Separate-process paid-parry/riposte Continue passes, including
exact events, HP, RNG, schedule and checked save generation 2. The repeatable
script is `tests/idea_duelist_disk_resume.sh`; its accepted log is
`/var/tmp/roag-duelist-final-disk-20261019.log`. This proves headless behavior,
not GUI readability or human balance. The Geomancer prototype remains isolated
at `/var/tmp/roag-geomancer-ceeabc3f`, unwired and unclaimed. All remaining class,
item, room, terrain, environment, presentation and 50+ boon work stays in scope.

## Five-class batch — 19 October

Rechecked the unmodified baseline: **510/510**, log `/var/tmp/roag-five-classes-baseline-20261019.log`. Geomancer is integrated from `/var/tmp/roag-geomancer-ceeabc3f`: nine focused tests, 46 class/art/UI contracts and two-process expiry/save continuation pass. Its isolated full gate is running; no result is presumed. New blocks are entity overlays, cannot seal accessible routes, and expire after 300 simulation time. Character selection now uses bounded nine-row pages and matching mouse hitboxes; Classic fallback colors cover additional classes. No LÖVE windows opened.

Spiritbinder is integrated: six final focused cases and two-process paid-command continuation pass; 52 combined class/art/UI contracts passed before the deployment-near-enemy usability correction, and final integration reruns remain required. Deployment now stops before an enemy rather than requiring the first familiar to be placed in a different direction. The initial disk assertion exposed that usability issue; its diagnostic was retained. Geomancer task commit: `6fecf05c`. LÖVE remains closed.

Tinkerer is integrated: seven final focused cases, 59 combined contracts and new-process pending-shot continuation pass. A regression caught guaranteed Temporal Echo still activating at coefficient zero; the new sentry-generated event flag explicitly excludes these automatic records from boon activation. Existing attack/proc rules remain unchanged for untagged events. Failure logs are retained. The Geomancer frozen full gate has now passed **519/519** at `/var/tmp/roag-geomancer-full-20261019.log`; Spiritbinder commit `9507efcb`. No LÖVE windows opened.

Stormcaller is integrated: seven focused cases, 66 combined contracts and fresh-process pending-cross continuation pass. Charge only comes from positive direct bolts, capped at three; discharge and ground ward are alternative spent-charge choices. Its delayed cross is fixed and explicitly cannot activate boon chains. Fixture corrections handled a real wall truncating the cross and made the disk fixture earn charge before casting. Tinkerer commit `0b084f9f`. Four of this five-class batch are integrated; Wayfinder is next.

### Five classes implemented — integration gate

The Geomancer, Spiritbinder, Tinkerer, Stormcaller and Wayfinder are integrated.
Full pre-handoff-fix gate: **553 passed, 0 failed**, frozen source
`/var/tmp/roag-five-classes-final-m5yyxtkz`, log
`/var/tmp/roag-five-classes-final-headless-20261019.log`. This includes all
original legal Victories, seven miniboss routes, generation corpora and prior
combat/save/asset/audio checks. The new focus gate is **43/43**.

- Fifteen genuine first-encounter runs: seeds **3, 9, 21** for each of the five
  classes, with exact ordered event/action replay and final full snapshots.
  These are not full-run Victory claims.
- All five have real checked-store write → separate-process Continue → second
  generation checks. Spiritbinder orders a saved familiar strike; Tinkerer
  resumes a pending bolt; Stormcaller resumes a delayed cross; Geomancer
  expires its saved block; Wayfinder performs its saved beacon return.
- 100 prepared repeated-use cycles per class retain bounded entity/scheduler
  state, valid saves and unchanged RNG. These are fixtures, not legal runs or
  renderer benchmarks. CPU per 100 cycles: Geomancer .0293 s, Spiritbinder
  .0159 s, Tinkerer .0099 s, Stormcaller .0288 s, Wayfinder .0218 s, under
  Linux 6.18 WSL2 x86_64 / LuaJIT 2.1. No GPU measurement was made.
  The Lua heap samples vary with GC/JIT; bounded saved state (5.1–5.7 KB) and
  queues are the reliable assertion, not a general memory-leak claim.
- All 13 enemy disk scenarios and the original art/review checks pass. The
  first combined disk command lacked the copied Duelist fixture in the
  staging directory; it was supplied and the actual Duelist disk gate passed.
- Both Kenney choices resolve **399/399** bindings: 65 actor IDs / 130 static
  pose aliases. Original source PNGs and user-reviewed HTML/feedback remain
  unchanged. The catalogue now records 330 used / 3,264 unused native cells.
- Source and tests are frozen separately during full runs. An earlier
  unfinished 552-test run was stopped only for its verified own process to
  add Sentry/Mirror integration. The later 553/553 run completed. Assertion
  logs from corrected fixtures remain available; no GUI crash fix is claimed.

Final focus: `/var/tmp/roag-five-classes-final-focused-20261019.log`;
legal/repeated-use evidence: `/var/tmp/roag-five-classes-final-probe-20261019.log`;
disk evidence: `/var/tmp/roag-five-classes-final-disk-20261019.log`.
`tools/idea_classes_probe.lua` reproduces the legal prefixes and labelled stress.
The class guide records actual controls and source art reuse.

During final audit a separate pre-existing stage-handoff defect was reproduced:
a seed-2 Geomancer could receive Adventurer-only Longblade Geometry on stage 2
because the fresh stage generated offers before restoring the chosen class.
The release candidate passes the class ID into fresh stage/finale construction;
a new assisted fixture checks later offers and compatible saves for all fifteen
classes. Its **44/44** focus and all-class fresh-process checks pass. The full
release gate passed **554/554** at `/var/tmp/roag-five-classes-release-headless-20261019.log`
from `/var/tmp/roag-five-classes-release-pu71oihc`. All established legal full-run
Victories and seven miniboss routes remain passing with correct class offers.

This user-requested batch is classes only. Items/rooms, three terrains,
environment/presentation directions and 50+ additional boons remain pending.
LÖVE windows, physical input, game-scale readability and human balance
acceptance remain deferred. No later batch is started in this turn.

### Class checkpoint — finished in code, GUI deferred

The current requested five-class batch is finished in code and headless
verification. All six approved class additions, including the earlier Duelist,
are integrated; there are now **15 playable classes**. The Pilgrim ledger row
is a boon redirect, not a seventh unfinished class.

| Class task | Atomic commit |
| --- | --- |
| Geomancer | `6fecf05c` |
| Spiritbinder | `9507efcb` |
| Tinkerer | `0b084f9f` |
| Stormcaller | `6857875b` |
| Wayfinder / class integration | `30421894` |

Final evidence: **510/510 baseline → 554/554 full release gate**, plus
**44/44** final live focused checks, fifteen legal encounter/replay cases,
five class-specific fresh-process saves, Duelist and thirteen enemy disk
scenarios, original feedback/source-art checks and `git diff --check`.
The last small UI-only correction makes disabled PREV/NEXT controls retain
the current selection; two added assertions in the existing page case and
the full live 44-case focus pass after it. The frozen full gate predates only
that small pure UI boundary correction; authoritative game source matches
the 554-test release tree. No LÖVE window, actual mouse/keyboard/tablet or
native GPU was exercised. No claim of human balance/visual acceptance.

The final shared fix preserves class eligibility on future stage/finale
construction and lets urgent timing warnings precede class resource hints.
Existing concrete saved chest offers are preserved. No schema migration,
combat scheduler, enemy balance, PNG source or personal profile changes.
The initial stress fixture needed correct observer recompute after its
prepared player relocation, and an allowance for player + target + familiar;
those failed fixture logs remain retained. Final 100-cycle checks pass for
all five, with valid saves, bounded queues and unchanged RNG.

Resume with **items and interactive rooms**, then the three terrains,
environment/presentation work, 50+ new boons and final integration/visual
acceptance. The user requested this checkpoint before proceeding. No later
batch has been implemented in this turn. The full expansion stays incomplete.

## Next requested checkpoint — item/room detail vote (2026-10-19)

The user requested voting on the next items and interactive rooms. Prepared
the separate [14-card review](../sprite-audit/items-rooms.html) and
[instructions](../sprite-audit/items-rooms-guide.md): six items and eight
interactions already broadly approved, now with concrete action/cost/cancel
rules, proposed numbers, examples, persistence requirements and both art looks.
New detail votes are blank. Original 55/30/4 decisions and their exact archived
files remain intact. The scope is still incomplete; **no item/room mechanic was
implemented by this review**. The ledger rows remain pending.

Current 399-binding audit/source hashes are checked when building. There are
111 candidate cells, including three existing cue uses explicitly labelled
(574 Oath Twins, 539 revival corpse, 915 alarm). Prior user exclusions are
removed from both art pools. Screenshot inspection additionally caught the
old potion pool's Tiny 117 hammer: omitted and explained without rewriting
the earlier approval. Ambiguous bell and 1-Bit fixture silhouettes are flagged;
no primary 1-Bit assignment is suggested for those two. Shared rules propose
one utility slot and bounded node frequency; these are proposals, not new
inventory or generator code.

The review has its own feedback schema, fingerprint-scoped local draft and
`roag-items-rooms-feedback.json` export. `tools/item_room_feedback.js` validates
returned JSON read-only. Comments, art decisions and earlier approval context
remain available for the implementation handoff. Old feedback cannot overwrite
this draft or be mistaken for the new detail vote. The archived client/template
are untouched; the new page uses a separate client derived from that frozen UI.

Verification: seven focused Node data/state checks; isolated headless Chromium
exercises all 14 cards/28 art panels, preference changes, real download/import,
stale/old-file rejection, local draft reload, literal comments, filters and
390px mobile layout. Original six idea-state checks, preserved original/surplus
and 30/30 enemy review archives, live catalogue freshness and diff hygiene pass.
HTML screenshots/logs: `/var/tmp/roag-items-rooms-review-20261019/`.
No runtime source, PNG, save/profile or game balance changed. The prior
554/554 full game release gate remains the last full gameplay run; it was not
rerun for an offline review page. LÖVE windows remain closed as requested;
browser screenshots do not constitute game/physical-input acceptance.

Resume: read the user's downloaded `roag-items-rooms-feedback.json`, preserve
its matching proposal snapshot, resolve exact comments and art ambiguities,
then implement this batch in finite vertical slices with deterministic,
legal-action and fresh-process save tests. No review fixture vote is user
approval. Later terrain/environment/boon batches remain pending.

## Items / rooms — approved implementation checkpoint (2026-10-19)

The user explicitly approved **all fourteen** concrete proposals, then requested
that work stop after this tranche with a remaining-work report. This checkpoint
supersedes the earlier instruction to await a downloaded detail-feedback file.
The [approval record](../sprite-audit/items-rooms-approval.json) quotes that
message and hashes the frozen review, client, template, proposals and historical
399-binding inventory. No browser feedback or preferred-art votes were invented.

All fourteen now have finite authoritative behavior, genuine generated placement,
E/click inspection, explicit two-step confirmation, carried utility controls and
both Kenney mappings. The [implementation guide](../ROAG_IDEA_INTERACTIONS.md)
records exact costs, effects, expiry, constraints, controls and art refinements.
Two services, at most one bell/fixture assembly and one sign per floor keep this
optional. Start/elite/exit rooms are excluded; no existing collision geometry,
enemy placement, combat RNG, scheduler rule or normal reward value was changed.

An optional version-1 plain-data store holds fixed offers/trades/targets, spent
flags, bell progress and fixture commitments. The existing run/content/boon
versions remain unchanged. Old saves without that store stay unpopulated; pending
fixture time parks/rebases with its floor. A carried unplanted utility survives
stage handoffs and finale entry. A planted token remains strictly room-bound.
Committed Temporal Echo work survives retiring its boon. Camp/memorial recovery
uses Healed rather than falsely incrementing food-pickup achievements.

The mixed and standalone catalogues now resolve **415/415** required bindings,
with no missing or empty source cells. Sixteen new object bindings include the
plaque/marker aliases. Original sheets and archived decisions remain intact.
The 1-Bit fixture is a labelled falling slab; bells use numbered gong motifs.
Stormcaller's actual Grounding Dash moves, so it fails a movement-constrained
contract; the review's hypothetical stationary example is not implemented as an
incorrect exception.

Verification and final release results are recorded below.
No LÖVE windows or physical input were used. Actual main callbacks/draw calls
have headless injected-table coverage; that is not rasterized visual acceptance.

### Stop / resumption boundary

**Stop after the items-and-rooms checkpoint**, as the user requested. Do not start
terrain, environment or boons automatically. The approved expansion remains
incomplete, with these remaining work packages:

1. Three mechanical terrains: clocked heat vents, push-floor strips and optional
   fragile crossings, with deterministic clocks, connectivity and save tests.
2. Environment/art and presentation: approved biome/dressing kits and icon/cue
   families; preserve rejected Oracle-room, bitmap/UI-skin and bulk-kit constraints.
3. **50+ additional meaningful boons**, including the explicit Pilgrim, dice and
   Cantor-art redirects; frozen offer pools, rarity stacks, chains, previews and
   persistence. This remains the largest gameplay batch; current catalogue is 53.
4. Final cross-feature integration, legal-run/save/seed regression and visual/human
   verification. GUI/physical testing remains pending under the no-window request.

Previously requested classes and approved enemy concepts are implemented with
headless evidence; their human readability/balance acceptance is still pending.
Do not describe this as completion of every approved idea or the whole game.

### Current batch evidence

- Baseline: **554 passed, 0 failed**, frozen source
  `/var/tmp/roag-items-baseline-nuercz86`, log
  `/var/tmp/roag-items-baseline-20261019.log`.
- Focused mechanics/UI callback set: **35 passed, 0 failed**, command
  `TMPDIR=/var/tmp ROAG_TEST_RANGE=555-589 luajit tests/run.lua`, log
  `/var/tmp/roag-items-focused-final-20261019.log`.
- Seven two-process checked-store scenarios pass: utility, Burning + committed
  Echo, fixed forge choice, bell progress, planted token, pending fixture and
  active contract. Command `sh tests/idea_interaction_disk_resume.sh`, log
  `/var/tmp/roag-items-disk-20261019.log`; generation 1 → fresh-process exact
  continuation → checked generation 2, isolated `/var/tmp` fixtures.
- Existing thirteen enemy scenarios, their three third-process follow-ups,
  five class scenarios and Duelist riposte all pass separate-process Continue.
  Logs: `/var/tmp/roag-items-enemy-regression-disk-20261019.log`,
  `/var/tmp/roag-items-class-regression-disk-20261019.log` and
  `/var/tmp/roag-items-duelist-regression-disk-20261019.log`.
- Natural procedural acceptance: **14/14** with normal Sentinel stats, actual
  earned chest/currency/boon costs and public `Game.submit` only. Each successful
  trace replays every exact event and final snapshot, including a mid-trace
  restore. No HP/position assistance, enemy weakening or reward injection.
  Command `TMPDIR=/var/tmp luajit tests/idea_interaction_acceptance.lua`, log
  `/var/tmp/roag-items-legal-all-20261019.log`.

| Natural interaction | Seed | Actions | World time | Final HP | Gold |
| --- | ---: | ---: | ---: | --- | ---: |
| `offering_font` | 27 | 233 | 25088 | 23/29 | 28 |
| `ration_cache` | 3 | 33 | 3855 | 29/29 | 2 |
| `cleanse_flask` | 25 | 187 | 20842 | 33/33 | 30 |
| `escape_scroll` | 13 | 100 | 11688 | 33/33 | 16 |
| `return_token` | 34 | 42 | 4940 | 29/29 | 2 |
| `charm_well` | 22 | 54 | 6137 | 29/29 | 4 |
| `bell` | 36 | 31 | 3609 | 29/29 | 7 |
| `falling_fixture` | 25 | 50 | 5932 | 33/33 | 4 |
| `route_sign` | 5 | 219 | 24015 | 29/29 | 25 |
| `camp` | 29 | 233 | 26128 | 29/29 | 64 |
| `attunement_forge` | 21 | 273 | 31688 | 33/33 | 55 |
| `travelling_trader` | 19 | 70 | 8184 | 33/33 | 3 |
| `memorial` | 8 | 277 | 32661 | 33/33 | 52 |
| `archive_contract` | 17 | 188 | 22072 | 33/33 | 25 |

The guided driver reads the generated map; these are automated scenarios, not
human acceptance. They establish collection for the flask, not natural Burning
exposure; that cleanse is separately exercised with real scheduled Burning in
prepared fixtures. Ten earlier natural scenarios also passed before expanding
the probe to forge/trader/memorial/contracts, log
`/var/tmp/roag-items-legal-20261019.log`.

All rejected attempts remain in the log: ten navigation action-limit exits,
one west-side fixture approach with no path, one correctly disabled full-health
memorial, and five contracts whose fixed targets had already left/died before
inspection. These do not mutate the map or reroll the same run. The contract
succeeds in seed 17, but availability/readability remains a human usability risk;
no AI, geometry or timing rule was weakened to force it.

Review/source checks pass: seven focused item-review state tests, six original
idea-state tests, original/surplus/30-decision enemy archives, surplus source
checks and live HTML/JSON/CSV freshness. Logs:
`/var/tmp/roag-items-review-final-20261019.log`,
`/var/tmp/roag-items-surplus-final-20261019.log`,
`/var/tmp/roag-items-audit-final-20261019.log`.
The runtime's 83 Lua files match the frozen full-release tree byte-for-byte.

The implementation checkpoint `9f89fe19` is present in the checkout. No push,
reset, clean or history rewrite was performed for this batch. Final full-suite
result and stopping status follow below.

### Final release gate and stop

**589 passed, 0 failed**, exit 0: `TMPDIR=/var/tmp luajit tests/run.lua`
from frozen `/var/tmp/roag-items-release-e4duflb3`, log
`/var/tmp/roag-items-release-20261019.log`. This preserves all **554** baseline
checks and adds **35** focused interaction checks. The complete gate includes
legal six-floor/final-boss Victories for the six original classes, all seven
miniboss route checks, enemy/class mechanics, deterministic generation/replay,
save/profile behavior, native art resolution and fallback integration.
The runtime's 83 Lua files and all existing suite files match the frozen tree;
the separately passing 14-scenario natural probe was added after that freeze.
The natural probe, disk scenarios and review checks are additional evidence,
not inflated additions to the 589 suite total. Main Lua parsing and
`git diff --check` pass. No LÖVE app, GPU render, physical keyboard/mouse/tablet
or speaker was operated during this batch.

**Stop here, as requested.** The six items/eight room interactions are
implemented and headlessly verified; GUI/human readability acceptance remains
pending. The next work is the four remaining packages listed above. In
particular, do not automatically begin the three new terrains. The whole
approved-ideas goal is not COMPLETE.

## Resumed scope — terrain, then environment/presentation (2026-10-19)

Explicit user instruction: implement the three terrains, make a commit, then do
approved environment/art and presentation improvements, then stop. No 50+ boon
batch, new biome, Oracle room, animation tooling or rejected UI skin is in this
resume. The standing no-LÖVE-window restriction remains in force.

Baseline is the immediately preceding **589/589** gate. Before editing, all 83
runtime Lua files matched `/var/tmp/roag-items-release-e4duflb3` byte-for-byte and
the working tree was clean. That existing full gate is used without claiming a
second identical baseline run. Terrain's complete suite is run from a new frozen
source tree; subsequent environment edits cannot contaminate it.

### Terrain implementation

See [rules, controls and reproduction](../ROAG_IDEA_TERRAINS.md). Actual generated
nodes now provide visible clocked vents (100 warning, 300 cooldown, base 2 fire),
cardinal legal pushes with a four-push bound, and occupied-safe one-use shortcuts.
New optional version-1 data retains `roag-run/11` compatibility. E inspection is
free, warning outlines use committed cells, and source PNG bytes are preserved.
The ten new terrain phase/direction bindings bring both packs to 425/425.

Seventeen focused checks and the combined prior item/terrain **52/52** pass.
Three checked-store fresh-process scenarios pass exact events/state/RNG/scheduler
through generations 1 and 2. A 56-floor corpus covers seven biomes, validates
alternate routes and compares exact deterministic reopening. The private
source-art contacts inspected all related Micro 112–115 forms; clear approved
1-Bit 1003–1006 arrows are used. Tiny vent art uses a cached true two-tone
monochrome derivative with alpha intact; native fallback/refresh signatures are
checked. No raw source PNG changed.

Retained failures led to a concrete placement fix: initial fragile fixtures
exposed authored `protected_changed`, and a natural legal run exposed
`corridor_blocked` on Continue. A valid geometric detour does not permit closure
of a primary corridor's explicit saved cells. Placement and restore now exclude
both protected authored cells and primary corridor cells, with full-component
route checks. Generator/save validation was not relaxed. The first superseded
full run was stopped after its verified failure; log
`/var/tmp/roag-terrain-release-20261019.log` and focus diagnostics remain intact.
That old placement also changed the seed-3 guided encounter so one expected
Regent pattern was absent. With the placement fix, the unchanged legal strategy
wins at **1,125 actions / 11 HP** and sees line, crown and sigil. No AI, stats,
combat timing or driver tactic was changed to force it.

Natural legal scenarios now pass exact event/snapshot replay and mid-trace
Continue: heat seed 5 (218 actions, 33/33 HP, 42 gold), push seed 10 (30 actions,
29/29 HP, 4 gold), fragile seed 7 (11 actions, 29/29 HP). The driver is map-aware
but all state changes are public actions with ordinary Sentinel stats. One
navigation limit and one occupied arrow landing remain logged; restore/replay
errors are never silently treated as ordinary navigation failures.

Logs: `/var/tmp/roag-terrain-combined-final-20261019.log`,
`/var/tmp/roag-terrain-protected-final-20261019.log`,
`/var/tmp/roag-terrain-disk-verified-20261019.log`,
`/var/tmp/roag-terrain-legal-verified-20261019.log`,
`/var/tmp/roag-terrain-seed3-legal-20261019.log`.
Full frozen gate and terrain commit follow below when finished.

### Terrain checkpoint — commit boundary before environment work

Current mechanics pass **20 focused / 55 combined** interaction tests, three
fresh-process saves and all three natural legal/replay scenarios. Cross-feature
regressions additionally preserve collected-drop history on verified gaps, hold
live supplies/placed class objects/revival commitments, prune invalid transient
ground effects on closure, and reject orphan heat work when its store is missing.
The checked save contract remains strict about live supplies and arbitrary holes.

The previous frozen run completed at **603 passed / 3 failed**. Its failures
identified a practical arrow chokepoint and driver assumptions: Ranger repeatedly
shot a pushed target through a blocked diagonal; Sentinel could not route out;
Arcanist's stair routing cycled into forced displacement. Generated arrows now
require a clear local area and a proven bypass. The legal-only driver routes
around optional arrows rather than treating them as ordinary standing cells.
No enemy AI, HP, ranged limit, scheduler priority or reward value was changed.
All **15** actual starting classes now pass their ordinary first encounter,
including Ranger at 14 HP after 30 actions. The failed run/log remains intact;
passing reruns are not presented as an engine crash fix.

A complete **609-test** gate of the current terrain snapshot is running from
`/var/tmp/roag-terrain-accepted-ansm7mto`, log
`/var/tmp/roag-terrain-accepted-headless-20261019.log`. Its result is pending at
this commit boundary; targeted fixes above are verified independently. It is
isolated from later presentation edits. Final release evidence will include
its actual result and the combined environment gate. The requested terrain
commit is made before environment runtime implementation begins.

Current logs: `/var/tmp/roag-terrain-hardened-focus-20261019.log` (55/55),
`/var/tmp/roag-terrain-cross-feature-verified-20261019.log` (20/20),
`/var/tmp/roag-terrain-final-disk-20261019.log`,
`/var/tmp/roag-terrain-hardened-legal-20261019.log` (3/3), and
`/var/tmp/roag-terrain-first-encounters-20261019.log` (15/15).
The latest legal vent trace is seed 5 / 215 actions / 29 HP / 25 gold; push and
fragile remain seed 10 / 30 actions and seed 7 / 11 actions. Source hashes,
review archives, live catalogue and `git diff --check` pass. LÖVE GUI and physical
input are still untested as requested. No boon batch has begun.


### Terrain gate completed before environment release

The frozen terrain snapshot completed **609 passed, 0 failed**; exact log
`/var/tmp/roag-terrain-accepted-headless-20261019.log`, source
`/var/tmp/roag-terrain-accepted-ansm7mto`. The requested terrain commit is
**`40dce936` — Add clocked vents, safe push floors and finite shortcuts**.
It preceded environment runtime work. The historical pending-result notes
above describe that earlier commit boundary, not the final terrain verdict.

### Environment and presentation — requested final batch

Implemented the approved crypt/Sanctuary merge, existing-biome room materials,
bounded wall dressing, furniture/workshop/shelf/window/bar/column artwork,
secret mural variants, contextual F/R/X icons, actual status/state badges and
small semantic combat/recovery cues. There is **no eighth biome**, crafting
system, new class, boon, full UI skin or bitmap-text implementation. Both
Kenney choices resolve **491/491** active IDs. Actual source-cell usage is
**504** of 3,594 nonempty cells; the remainder is still excess, not missing
mandatory art. Native image originals and the archived review exports remain
byte-identical. See [the implementation guide](../ROAG_IDEA_ENVIRONMENTS.md).

Twenty new focused checks pass, including 70 genuine generated floors across
seven biomes, all 256 neighbor cases, exact sampled pixels, monochrome output,
cache bounds, optional-source recovery, exact snapshot/RNG invariance, class
icons, genuine DamageTaken cue lookup and real `main.lua` world/HUD draw traces
with the minimap hidden. The new traces found dressing/rewards in the wrong
minimap pass; they now render in the world pass behind tactical outlines. The
closed-upvalue diagnostic initially reused JIT traces after changing its fixture;
explicit trace retirement makes that injected test deterministic. This is a
harness issue, not evidence of an engine crash or a physical-input fix.

The source-pixel gallery executes the real world renderer and native surface
compositors through a CPU adapter in a separate LuaJIT process. Eighteen views
cover both looks, all seven biomes, the combined crypt and action/status cues.
They were inspected offline and exact PNG regeneration passes. Inspection
caught a two-column carpet offset that included adjacent masonry; corrected
922–924 / 979–981 / 1036–1038 now forms complete carpet borders, with a center
pixel assertion. These are **not LÖVE screenshots**, omit text and GPU edge
antialiasing, and use spectator visibility for art inspection.

Fresh-process checked-store tests pass for both Kenney choices, comparing all
491 handle metadata records, exact room plans, snapshot and future legal wait
events. Twenty-five resolver replacements per choice leave the run/RNG intact;
checked generation 1→2 matches. The existing seven item/room and three terrain
disk scenarios also pass again. No live player save/profile was touched.

Logs: `/var/tmp/roag-environment-focus-accepted-20261019.log` (20/20),
`/var/tmp/roag-environment-disk-verified-20261019.log` (2/2),
`/var/tmp/roag-environment-items-disk-20261019.log` (7/7),
`/var/tmp/roag-environment-terrain-disk-20261019.log` (3/3),
`/var/tmp/roag-environment-pixels-check-20261019.log` (18 byte-identical views),
`/var/tmp/roag-environment-reviews-20261019.log` (frozen review checks).
The initial diagnostic/focus failures are retained separately; no failing
LÖVE test was hidden or removed. No LÖVE process/window or GUI harness ran.
The final frozen full-suite result follows when it completes.

The first environment full attempt was stopped after its confirmed obsolete
six-page cache assertion (`/var/tmp/roag-environment-release-headless-20261019.log`,
controlled termination, exit 143). The actual renderer now needs two additional
fixed derived material pages. The updated check requires **exactly eight**
pages with their precise paths/modes, no optional resource failures, and no
additional allocation across repeated lookups. All eight native-renderer
tests pass independently (`/var/tmp/roag-environment-native-cache-20261019.log`).
The complete rerun uses `/var/tmp/roag-environment-final-hbfbpwdc` and
`/var/tmp/roag-environment-final-headless-20261019.log`; it is isolated from
documentation edits. No test was removed or turned into a no-op.


### Final requested stop — 2026-10-19

**629 passed, 0 failed**, complete unfiltered `TMPDIR=/var/tmp luajit tests/run.lua`,
exit **0**. Frozen source `/var/tmp/roag-environment-final-hbfbpwdc`, log
`/var/tmp/roag-environment-final-headless-20261019.log`. This preserves the prior
589 baseline, adds 20 terrain cases (609 checkpoint) and 20 environment cases.
Every current runtime Lua and test Lua file matches the final frozen source;
Game, Floors, terrain mechanics and legal-action driver also still match the
accepted 609-test terrain snapshot exactly. Existing legal class Victories,
seven-biome routes, deterministic replay, strict scheduling, room generation,
rewards, profiles, corruption checks and persistence remain passing.

Final separate checks: **12/12** fresh-process scenarios (seven items, three
terrains, two environment looks); **18** byte-identical real-render-path CPU
pixel views; source hashes and all frozen review checks; live **491/491**
native bindings; `git diff --check`. No LÖVE GUI application or GUI harness was
started. Real runtime contrast, text sizing, physical input and native GPU
performance are not accepted by these tests. Historical GUI instability has
not been reproduced or declared fixed.

**Stop now as requested.** Remaining approved work is the 50+ additional-boon
batch (including the Pilgrim/dice/Cantor-art/Oracle redirects and remaining
symbol-library decisions), followed by final integration and permitted LÖVE/
human visual acceptance. Do not start that work on an automatic continuation.
The existing broad goal stays paused/incomplete; it is not marked COMPLETE.
Resume only when the user asks, using the preserved feedback and ledger.


## Approved fixed-art follow-up — 2026-10-19

The user approved **the existing Kenney 1-Bit look, including all 13 monochrome
derivatives**, as the only game appearance, and requested removal of every
art-pack switching control. This follow-up does **not** resume the pending
additional-boon batch. All prior features, feedback, redirects and remaining
work above remain preserved. The broad goal remains paused at that boundary.

Baseline: the current 177 runtime/test Lua files exactly matched the accepted
629/629 snapshot before editing. The relevant baseline subset independently
passed **32** checks. The fixed implementation removes the sprite settings
tab, preference reads/writes and `ROAG_SPRITE_PACK` override, and removes the
`make kenney`/`make onebit` launch targets. Settings now contain Sound, Effects
and Accessibility. New Run, Continue and spectator all use the same artwork.
F5 reloads only that artwork and rejects corrupt/missing required replacements
while preserving previous good resources and the authoritative run.

The game uses 1-Bit plus Tiny/Caves/RPG solely for the approved counterparts.
Micro is no longer in production code or runtime inventory. Its original PNG
and license are retained at their reviewed paths because frozen feedback/source
hashes and historical tests still depend on them. The old renderer moved from
`src` to `tests/legacy_kenney_visuals.lua`, and its three pack-specific GUI
harnesses are preserved under `tests/legacy`, not counted as current passes.
The current catalogue has no art-style selector; its source filter inspects
four active sheets. A separately labelled Micro archive foldout preserves
original review links. No frozen user review/export or original PNG changed.

Six new focused checks pass: actual native loader ignores legacy overrides;
real settings callbacks/render trace expose only the three remaining categories;
actual refresh logic retains resources after corrupt and missing required art;
all **491** bindings and exactly **13** derivative handles remain; one cached
shader restores graphics state; unsupported shaders warn once and allow safe
unshaded drawing. The existing relevant subset also passes **44** checks.
Source-pixel regeneration produces **nine** fixed-look views, byte-identical
to the previous accepted 1-Bit images. Current sources reference 273 of 3,435
nonempty cells; the 159 historical Micro cells are outside that runtime total.

Three independent LuaJIT processes verify that old classic/mixed choices are
ignored without overwriting their records or changing deterministic snapshots.
The fixed-look checked-store fixture passes fresh-process reopening, identical
491 handles and room plan, 25 resource replacements, exact action events/RNG,
and save generation 1→2. Current menu/facing/outline/squad smoke expectations
were adjusted for the fixed artwork and grayscale output; five shell and five
embedded-Lua syntax checks pass. They were **not** run in LÖVE, as requested.
Historical GUI instability and physical/human/native-GPU acceptance remain open.

Logs: `/var/tmp/roag-fixed-onebit-focus-verified-20261019.log`,
`/var/tmp/roag-fixed-onebit-process-20261019.log`,
`/var/tmp/roag-fixed-onebit-environment-disk-20261019.log`,
`/var/tmp/roag-fixed-onebit-pixels-check-20261019.log`,
`/var/tmp/roag-fixed-onebit-reviews-verified-20261019.log`.
The initial new refresh fixture omitted a graphics stub; its retained failure
log is `/var/tmp/roag-fixed-onebit-focus-20261019.log`. It was a headless
fixture error, not a LÖVE crash. The prior two-pack archive checker also needed
to separate retired choices from current runtime bindings; it continues to
verify the frozen source/page/feedback hashes and all current approved 1-Bit
assignments. No failed GUI run was hidden or claimed repaired.

The final unfiltered suite is running from frozen source
`/var/tmp/roag-fixed-onebit-final-kcuxbolx`, log
`/var/tmp/roag-fixed-onebit-final-headless-20261019.log`. Its actual result will
be appended before this follow-up is reported finished. No new enemy, class,
boon, biome, collision, simulation, scheduler, profile or save-schema change.


### Fixed-look regression follow-up

The first full fixed-look run completed **622 passed / 13 failed** (exit 1),
not a GUI crash. Every failure was an old art assertion indexing the deliberately
removed `packs.kenney` entry in the live catalogue. The exact log is retained at
`/var/tmp/roag-fixed-onebit-final-headless-20261019.log`. All other checks,
including gameplay, legal Victories, saves, environments and six new fixed-look
checks, passed in that attempt. Thirteen enemy/class art checks now assert the
approved **current 1-Bit assignments**, retaining their actual generation,
geometry, body-reuse, save and rejected-art assertions. No test was deleted.
Seventeen targeted art/generation contracts pass at
`/var/tmp/roag-fixed-onebit-art-contracts-20261019.log`.

The final unfiltered rerun uses `/var/tmp/roag-fixed-onebit-accepted-sunixfxe`,
with `/var/tmp/roag-fixed-onebit-accepted-headless-20261019.log`. It includes all
635 registered cases. The old source snapshot remains intact; source/runtime
and Lua-test equivalence are checked against this new final snapshot before
reporting success. Live catalogue, review-source checks and fixed preview
regeneration also pass. Original review decisions are never rewritten to make
these checks pass.


### Fixed-look final gate and stop

**635 passed, 0 failed**, complete unfiltered `TMPDIR=/var/tmp luajit tests/run.lua`,
exit **0**, at `/var/tmp/roag-fixed-onebit-accepted-headless-20261019.log`.
Final frozen source: `/var/tmp/roag-fixed-onebit-accepted-sunixfxe`.
This retains 629 existing registered cases (updating retired style-specific
expectations) and adds six fixed-look cases. Runtime and Lua-test bytes match
that accepted source exactly. Compared with the prior 629 baseline, **84 other
runtime modules**, including Game, scheduler, AI, boons, generation and save
logic, remain byte-identical. No simulation, class, reward or progression edit.

The three stale-preference process checks, the fixed-look generation-1→2
Continue/resource fixture, seventeen current art/generation contracts, review
archives/source hashes, live 491/491 coverage, exact thirteen derivatives,
nine unchanged pixel views and `git diff --check` pass. Current browser tooling
uses a source-sheet filter, not an art-style selector. Historical comparison
controls remain only in the frozen review archive; they cannot affect ROAG.

No LÖVE GUI/hardware test was run. Source inspection also notes that the old
unexecuted `onebit_gui_smoke.sh` gallery has a four-row actor fixture; the
expanded 50-form roster will require pagination in the separate permitted GUI
acceptance pass. That is a harness capacity issue, not a missing game sprite,
and is not represented as a passing rendered-frame test here.

This requested follow-up is implemented and headless verified. **Stop here**;
the existing larger goal remains paused with all remaining additional-boon,
redirect and final GUI/human integration work preserved above. There is no
unrequested continuation into that backlog.

### Settings follow-up — volume sliders and Small text

Replaced the crowded Sound minus/plus controls with click/drag sliders and a
separate right-aligned percentage. They preview master/music/effects volume
immediately in one-percent increments, clamp to 0–100%, and save once on release
or when navigation/focus ends a drag. Arrow keys retain ten-percent adjustments.
Audio-only adjustments no longer allocate UI fonts. Storage failures remain
visible. Title and pause settings share these controls.

Accessibility now cycles **Small / Normal / Large**, in either direction with
arrow keys or forward by clicking/Enter. Small uses body/display/menu font sizes
10/20/14; Normal remains the default (12/24/16), Large remains 14/26/18. Both
font faces retain unsupported-glyph fallback. Existing version-1 Normal/Large
preferences load unchanged; the accepted value set adds Small and integer
volume percentages. No run/profile schema, gameplay, art or source-asset change.

Four new cases exercise persistence/invalid values, real font loading with
graphics stubs, actual main.lua click/drag/render/keyboard callbacks, safe drag
termination and failed saves. They check exact snapshots before/after, no
per-motion writes and no audio-adjustment font allocation. These and six
fixed-look cases pass **10/10**; the existing preference case separately passes
**1/1**. Menu shell and embedded-Lua syntax checks pass. Logs:
`/var/tmp/roag-settings-focus-20261019.log` and
`/var/tmp/roag-settings-preferences-20261019.log`.

The prior full baseline is **635/635**. The full slider/Small-text checkpoint
passed **639/639**, zero failures, exit **0**, from frozen source
`/var/tmp/roag-settings-final-bpyvl0g8`, with log
`/var/tmp/roag-settings-final-headless-20261019.log`.
No LÖVE application or GUI harness was opened, as requested.
Actual rendered-frame/physical-input acceptance remains unverified. The broader
approved-ideas goal remains **paused**; no additional-boon work is resumed.

The user then requested an inventory-footer adjustment while that frozen suite
was running. **U · Utility / Items** now sits under the Boons page/inspect hint,
above **I / Esc close**. Its mouse hitbox moved with the instruction; the old
character-panel hitbox is inactive. The carried item/status remains character
information. All player-facing occurrences of “no world time” were removed,
including help/notices; menu/simulation timing is unchanged.

Final UI delta checks: **11/11** fixed-art/settings/footer cases, **2/2** existing
service/inventory/scroll input cases, and the **1/1** existing preference case.
The new footer case draws seven boon rows through the actual inventory callback,
checks all three footer positions, clicks the moved target, rejects the old
target and verifies exact authoritative snapshots. Logs:
`/var/tmp/roag-settings-footer-focus-20261019.log` and
`/var/tmp/roag-inventory-input-focus-20261019.log`. The passed 639-case frozen
suite covers the slider/Small-text checkpoint before this later footer delta;
the final tree registers 640 cases. No GUI run is implied by these callback
checks. Final runtime modules and assets match the fully accepted checkpoint;
only main.lua's footer/text/hitbox changes and their two test files differ.
`git diff --check` and main.lua/menu-fixture bytecode/shell checks pass. The
broader goal remains paused; this follow-up stops here.

### Selective color and damage-number follow-up

The user requested restrained color while retaining the fixed Kenney artwork.
`visual_palette.lua` now assigns muted chest/spent-chest brown, food tan and
gold reward/boon/utility ink, plus blue-gray enemy bodies. Dormant mimics retain
chest ink. Terrain, player bodies and UI remain neutral. Hostile windup/impact
and pending-strike outlines are red; player attacks, allied actions and previews
are green; enemy support is blue-gray and blocked movement/cancelled windups stay
neutral. Existing dotted borders, locked cells, fog and gameplay timing remain.

The cached `onebit_shader` accepts scoped ink after luminance conversion and
preserves alpha. Initialization validates the uniform before activation, warns
once on failure, and restores the caller's shader. Source handles are never
mutated: generation-owned proxies preserve texture/Quad/geometry and separate
different semantic uses of a shared cell. No new textures or source PNG edits.

Positive damage numbers smoothly traverse the rainbow with a gentle 0.8–1.0
brightness pulse; blocked zeros stay calm. Color depends on the existing
presentation age/location/amount, never wall time or gameplay RNG. Critical `!`,
black edging, rise/fade, 0.78-second lifetime, 24-number limit and the existing
Effects toggle remain. No new event or simulation update was introduced.

**74/74 focused headless cases pass**, including five new palette cases plus
motion/numbers, visual/atlas resolution, source coverage, environment rendering,
settings and footer regressions. Actual main.lua draw callbacks verify semantic
colors, neutral restoration, mirrored Quad geometry/alpha, hidden-cell/number
culling, failed-shader native-sprite fallback, and rainbow text from an actual
resolved legal hit with exact before/after snapshots. All 491 bindings and 13
approved derivatives remain covered. Log:
`/var/tmp/roag-accents-regression-20261019.log`. The runner now registers 645
cases; this is a focused regression result, not a new full-suite count.

Nine regenerated offline world/icon views execute the real world pass and
evaluate the palette formula on a CPU raster adapter. Source PNG hashes and
authoritative snapshots remain unchanged. Neutral terrain and blue-gray enemies
were visually inspected in these CPU views. See the
[gallery](../visuals/environments/index.html). This does not compile GLSL or
verify a GPU framebuffer. GUI expectations for preview/enemy/boon colors were
updated and shell/embedded Lua syntax checked; no LÖVE application or GUI harness
was opened, as requested. Physical input/native GPU acceptance remains pending.
The broader approved-ideas goal remains paused; no boon/content work is resumed.

Final follow-up checks: nine CPU PNG/HTML views reproduce byte-for-byte
(`tools/environment_preview.py --check`), including the inspected warm chest.
Logs: `/var/tmp/roag-accent-pixels-20261019.log` and
`/var/tmp/roag-accent-pixels-check-20261019.log`. **86 other runtime modules** and
all **15 source asset files** remain byte-identical to the prior accepted full
checkpoint. Main.lua bytecode compilation, three shell/embedded-Lua fixture
syntax checks and `git diff --check` pass. The previously recorded GUI gallery
capacity issue/historical GUI instability remain unverified; no GUI fix or pass
is claimed here. Stop after this requested presentation follow-up.

### Reported terrain-notice crash — missing Visibility import

The user reported `main.lua:1035: attempt to index global 'Visibility'` from a
normal keyboard action. Both HeatVentWarned and FragileCollapsed notice branches
referenced Visibility without importing it. Two new tests reproduce the exact
nil-global error through the real main.lua keyboard/submit/notice handler,
using genuine legal wait/move events: **20 passed / 2 failed** before the fix
(`/var/tmp/roag-visibility-before-fix-20261019.log`). The earlier simulation-only
terrain tests did not exercise these application notice branches.

Added the local `src.visibility` import. Both reproductions now pass, including
the entire **22/22 terrain module**. **96/96 focused terrain, motion, visual,
environment, settings/footer and palette regressions pass**, with exact
simulation continuation against direct Game.submit and codec save/reopen checks.
Logs: `/var/tmp/roag-visibility-fixed-20261019.log` and
`/var/tmp/roag-visibility-regression-20261019.log`. The runner registers 647 cases;
96 is the focused result, not a full-suite claim. Bytecode compilation and an
audit of all main.lua GGET instructions find only standard globals and LÖVE;
Visibility is now a local dependency. `git diff --check` passes. No simulation,
save schema, art/palette or profile change; no LÖVE GUI application was opened.
This fixes the reproduced Lua error, without claiming a fix for historical
unexplained engine/GUI instability. The broader goal remains paused.


## Resumed boon pass — first supply/reward group (2026-10-19)

The user's resumed scope is 50+ additional distinct boons and final acceptance;
this group is **7 additions**, not a completion claim. The catalogue is now 60,
normal unearned New Run pool 56; the old 49-ID saved pool remains frozen. At
least 43 further additions, Pilgrim/dice and full combined acceptance remain.
See [actual mechanics, limits and remaining acceptance](../ROAG_IDEA_BOONS.md).

Actual starting full baseline: **647/647**, frozen source
`/var/tmp/roag-boons-resume-baseline-l0z_dr93`, log
`/var/tmp/roag-boons-resume-baseline-retry-20261019.log`. The earlier attempt's
terminal signal 143 is retained in its original log and is not a successful run.
The prior status-only goal turn made no implementation progress; this resumed
turn implements the next real slice and independently tests it.

Implemented Beggar's Cap (approved hat 33), Overflow Chalice (approved cup 824),
Road Flask, Field Dressing, Ration Reserve, Broker's Seal and Funeral Coin.
No class/enemy/chest/terrain parameter or earlier boon definition changed.
Stable ordered multi-ward consumption and additive temporary speed replace the
old single-Betrayal/single-Bloodrush lookup. Real Burning cleansing removes only
its pending tick. Refunds cannot waive initial affordability or trigger pickup
loops. Exempt resurrected/called enemies cannot generate bonus gold. An expired
large captured ward cannot be renewed by a smaller waste amount.

Evidence:

- **22/22** new focused cases; **77/77** combined boon/native-art/environment
  checks. First focused run was 20/21: its food/Burning test incorrectly assumed
  one damage per Common Emberbrand tick; actual content is two. It now derives
  the assertion from the real declared power, still requiring one real tick and
  no cleanse/heal. A separate expired-ward regression adds case 22.
- Updated full gate: **668 passed / 1 failed**, 437.76 s, peak LuaJIT RSS
  151,620 KiB. The only failure was a leftover fixed 50-entry achievement-pool
  assertion. Its corrected separate rerun passes the actual legal elite defeat,
  one-time profile award and future Guardian's Vow offer. A complete clean
  retry remains required; no failed result is counted as a full green gate.
- Fresh processes: new supply state and legacy state both resume exactly;
  separately frozen **pre-change** source writes pending Burning/Echo, speed/
  ward memory and the old 49-ID pool, then current source continues with
  byte-identical events and authoritative snapshot. Old-source artifact:
  `/var/tmp/roag-boons-disk-ufWkhb`. Checked stores advance to generation 2.
- Existing Geomancer (1), item/room (7), terrain (3), environment (1) disk
  scenarios all pass write -> separate process Continue -> checked generation 2.
- **3/3 actual new boon GUI processes**, source-pixel comparisons for all seven
  icons, actual pickup callbacks, read-only inventory/details and retained PNGs.
  Passing artifact directories: `/var/tmp/roag-idea-boon-gui-0PUwTs`,
  `/var/tmp/roag-idea-boon-gui-IIuxXR`, `/var/tmp/roag-idea-boon-gui-tAEDpo`.
  The initial failed attempt `/var/tmp/roag-idea-boon-gui-PpF5KC` remains intact:
  startup's developer-showcase reset had cleared fixture grants. Granting after
  that existing reset fixes the fixture; there was no engine crash/signal 139.
- Native coverage **505/505**, same 13 approved monochrome counterparts, no
  missing bindings. The live catalogue is regenerated. Original PNGs and frozen
  review/feedback remain intact; **6/6** idea and **7/7** item review safety
  checks pass. `git diff --check` passes.
- LÖVE **11.5**, Mesa 26.2.4 / OpenGL 4.6 **llvmpipe LLVM 22.1.8** in Fedora
  WSL. Callback-driven screenshots, not physical keyboard/mouse/tablet testing
  or human/native-GPU acceptance. No real player save/profile was used.

The GUI exposed two presentation follow-ups for final acceptance: temporary
speed is not yet reflected in the character stat line, and capped bonus healing
can add zero-gain notices. These remain recorded, not silently called polished.
