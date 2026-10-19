# Approved idea implementation — sustained goal

Status: **ACTIVE — incomplete**. Every approved feature and explicit redirect remains in scope.
No feature is complete merely because a schema, fixture or metadata record exists.

## Authority and invariants

- User feedback: [original export](../sprite-audit/ideas-feedback.json), [interpretation](../sprite-audit/ideas-feedback-summary.md), [constraints](../sprite-audit/ideas-decisions.json).
- 55 approve / 30 reject / 4 redirect; six approved classes, 12 approved enemy concepts plus the changed Wraith, six items, eight rooms, three terrains, 14 environment kits plus merged Sanctuary, six presentation directions.
- Skull dice outcome: ordinary regular enemies only; miniboss/final/special encounter exemptions.
- Pilgrim becomes a boon; Cantor class and Oracle room remain rejected. Use the requested hat/cup/dice/skull/cross art for suitable boons.
- All exact sprite exclusions and previous rejections apply. Webs use drawn lines; use only 416 for Wraith. No rejected bulk-kit/bitmap/UI-skin feature is smuggled back in.
- Keep deterministic scheduler ordering, batch HP semantics, friendly fire, authoritative RNG, frozen inactive floors, exact save continuation, legal progression and stable visual IDs.
- No LÖVE app/window tests: the user’s restriction remains in force. Headless, native-API/source checks, source-pixel/browser artifacts and static renderer traces can run; report real GUI/playtest evidence as pending.
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
| `web-weaver` | Web Weaver + anchored webs | pending | — |
| `hollow-wraith` | Hollow Wraith + announced phase step | headless verified; GUI pending | 398/398 full, nine focused phase/art/save checks, fresh-process Continue; `707c0aec` |
| `gulp-toad` | Gulp Toad + recoverable stolen drop | headless verified; GUI pending | 415/415 full, seven drop tests plus a committed-box Dash fixture, four fresh-process disk scenarios, 337/337 native bindings; `ae231d75` |
| `mirror-hexer` | Mirror Hexer + reflected lane | pending | — |
| `trail-worm` | Silt Worm + temporary bog trail | pending | — |
| `pain-linked-twins` | Oath Twins + breakable pain link | pending | — |
| `alarm-keeper` | Alarm Keeper + one interruptible call | headless verified; GUI pending | 434/434 full, seven focused cases, seven fresh-process scenarios; this feature commit |
| `ember-moth` | Ember Moth + readable burning landing | pending | — |
| `bone-splitter` | Bone Splitter + reward-bounded fragments | headless verified; GUI pending | 427/427 full, six focused cases, six fresh-process disk scenarios, native skeleton/fragment art; `087ec1ad` |
| `rotating-sentinel` | Rotating Sentinel encounter | pending | — |
| `duelist-class` | Duelist — precision and paid riposte | pending | — |
| `geomancer-class` | Geomancer — one temporary bulwark | pending | — |
| `spiritbinder-class` | Spiritbinder — one commanded familiar | pending | — |
| `pilgrim-class` | Pilgrim — spend health for a measured vow | pending | — |
| `tinkerer-class` | Tinkerer — one limited bolt sentry | pending | — |
| `stormcaller-class` | Stormcaller — charge, ground, discharge | pending | — |
| `wayfinder-class` | Wayfinder — a visible return beacon | pending | — |
| `offering-font` | One-use HP offering font | pending | — |
| `ration-cache` | Sealed ration cache | pending | — |
| `cleanse-flask` | Single-use cleansing flask | pending | — |
| `escape-scroll` | One-charge escape scroll | pending | — |
| `return-token` | Room-bound return token | pending | — |
| `charm-well` | Charm well — choose one short condition | pending | — |
| `attunement-forge` | Attunement forge — trade one stack | pending | — |
| `travelling-trader` | One-visit supply trader | pending | — |
| `memorial-choice` | Memorial — retire a boon for recovery | pending | — |
| `bell-sequence` | Bell-order side room | pending | — |
| `heat-vents` | Clocked heat vents | pending | — |
| `push-floor` | Push-floor strip | pending | — |
| `fragile-crossing` | Fragile optional crossing | pending | — |
| `falling-fixture` | Announced falling fixture | pending | — |
| `signposted-routes` | Pictorial route signposts | pending | — |
| `camp-choice` | One-use camp choice | pending | — |
| `archive-contract` | Archivist’s optional contract | pending | — |
| `secret-mural` | Readable secret murals | pending | — |
| `crypt-kit` | Ossuary / crypt environment kit | pending | — |
| `sandstone-kit` | Pale sanctuary — ground and masonry | pending | — |
| `observatory-mosaic` | Observatory mosaic and cold stone | pending | — |
| `root-garden-kit` | Root garden and overgrown courts | pending | — |
| `drowned-sanctum` | Drowned sanctum water kit | pending | — |
| `forge-masonry` | Forge masonry and ironworks | pending | — |
| `library-carpet` | Archive carpets and patterned ground | pending | — |
| `broken-cavern-kit` | Broken cavern — rubble, damaged edges, cave mouths | pending | — |
| `ruin-masonry-kit` | Ruins masonry variants | pending | — |
| `camp-furniture` | Camp and inn furniture | pending | — |
| `workshop-fixtures` | Workshop fixtures and supplies | pending | — |
| `archive-shelves` | Archive bookshelves and empty shelf states | pending | — |
| `chapel-windows` | Chapel windows and alcoves | pending | — |
| `prison-dressing` | Prison cells, bars and grates | pending | — |
| `stone-column-kit` | Pillars, plinths and architectural supports | pending | — |
| `weapon-icon-families` | Weapon and attack icon families | pending | — |
| `status-icon-family` | Status, supplies and reward pictograms | pending | — |
| `combat-bursts` | Static combat impact cue family | pending | — |
| `trap-signals` | Switch, hazard and trap state markers | pending | — |
| `oracle-choice` | Oracle — visible finite omen choice | pending | — |
| `onebit-symbol-library` | Remaining 1-Bit item, cue and label library | pending | — |
| `cantor-art-redirect` | Hat 33 / cup 824 for boon/item concepts; no Cantor class | pending | — |
| `remaining-icon-boons` | 50+ distinct meaningful boon concepts, suitable unused icons, catalogue comparison and deterministic integration | pending | — |

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
- Both Kenney choices now resolve **351/351** required bindings, 52 actor IDs and
  104 static pose aliases; the user-reviewed 324-binding archive remains unchanged.
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

### Current work under verification

- Grave Cantor is now integrated in the live checkout. Nine focused tests and
  fresh-process pending-revival continuation pass. Full gate passed from
  frozen source `/var/tmp/roag-cantor-final-yq2akx43` into
  `/var/tmp/roag-cantor-final-headless-20261019.log`. One committed revival budget,
  blocked/cancelled attempts, weaker revived bodies, bounded corpse records and
  no repeat XP/currency/supply rewards. Broad acceptance is next.
- Gulp Toad: updated staging `/var/tmp/roag-toad-current-rz1bt_ya`, seven focused tests and a real
  fresh-process gulp/cargo scenario pass. One original loose drop is carried and
  released on death; interception and competing gulps cannot duplicate loot.
- Keep these task workspaces until their reviewed changes are integrated. They
  are disposable implementation fixtures, not personal project/save data.

Cantor placement probe: 26 casters across 210 paired stages initially had zero
eligible allies in clear four-tile range. The revised placement reuses a nearby
ally or adds one ordinary companion on free, unreserved adjacent floor; it
skips an unfit formation and never changes geometry. Every Cantor in the same
corpus now has company. Corpse caps and fog-clipped messages are also checked.

Next: commit the verified Alarm Keeper slice, then finish Web Weaver
acceptance and the remaining approved encounters. Alarm Keeper
finished 432/2: an outdated derived inventory checksum and a melee chase
cycle on seed-21 stage-one Warden. The catalogue is rebuilt; the exact combat
trace is `/var/tmp/roag-alarm-seed21-diagnosis-20261019.log`. The rebuilt catalogue and paired-floor melee cycle correction both pass
in the final **434/434** full run; the initial 432/2 log is retained. The staged Weaver passes nine focused cases, the combined **66/66** encounter
checks, and an actual third-process +50 crossing / save-generation-3 test. The initial Bone full check was stopped before
broad acceptance because its copied ranged driver was outdated; its partial
log is `/var/tmp/roag-bone-stale-strategy-aborted-20261019.log`. Future workspaces
now all match the verified strategy files before starting a full gate. A Toad staging run exposed two obsolete pre-Wraith
catalogue-count assertions; its log is retained at
`/var/tmp/roag-toad-final-headless-20261019.log`. The corrected catalogue assertions have all
eight native-art checks green. That full run finished **412/2**: Alchemist
seed 9 and Forge route seed 25 died during legal combat. Their complete traces
are `/var/tmp/roag-toad-legal-failure-diagnosis-20261019.log`; snapshots and
geometry inspection are in `/var/tmp/roag-toad-stall-inspection-20261019.log`.
The ranged combat driver was oscillating without firing; the combat escape
strategy also lacked a legal Dash when all adjacent cells were threatened.
A six-move cycle now clears a reachable add or holds one ordinary action, and
combat may use an actual safe Dash landing. Travel keeps its previous policy.
No game stats, scheduler rules, HP or positions were changed to make the runs
win. A broader Dash strategy caused regressions in the old single-floor
seed-3 route and a melee chase loop at the Regent; those failure logs remain
`/var/tmp/roag-toad-combat-final-headless-20261019.log` (413/2) and
`/var/tmp/roag-cache-combat-final-headless-20261019.log` (417/4). The final
strategy retains old single-floor and melee Regent tactics. Ranged combat can
Dash out of pinned areas; the paired-floor melee Forge strategy uses Dash for
its centered box. The restored legacy gates pass independently and in the
final full suites. The same cases now succeed: full Alchemist Victory at **20 HP / 1,158
actions**, and cleared Forge route at **13 HP / 1,153 actions**. See
`/var/tmp/roag-toad-driver-combat-only-20261019.log`. The final fresh full gate is **415/415** from
`/var/tmp/roag-toad-final-verified-l34a8ps4`. Earlier failed variants are retained
as diagnostic evidence, not reported as successful runs. The Haunted Cache is staged separately
at `/var/tmp/roag-cache-xl1hbwa3`: six focused tests, five disk scenarios and
the combined 43 encounter checks pass. Its original full run was **420/420**,
`/var/tmp/roag-cache-final-headless-20261019.log`; the final combat-driver strategy also passes **421/421** from
`/var/tmp/roag-cache-strategy-check`, log
`/var/tmp/roag-cache-final-verified-headless-20261019.log`.
- Bone Splitter staging `/var/tmp/roag-bones-qj45lqig`: six focused tests, six
fresh-process disk scenarios and 49 combined encounter checks pass. Primary
HP batches finish before two reward-exempt fragments appear. Native-pixel
inspection showed that proposed cell 374 is a rat, not a skeleton. The
fragments instead reuse approved 323 at a smaller whole-pixel render size;
that single shared body is explicitly permitted and reported by the audit. Two early fixture
assumptions about standing fragments and a supposed wall were corrected; logs
are retained. Broad acceptance remains pending.
- Alarm Keeper staging `/var/tmp/roag-alarm-_f_mxyws`: seven focused tests pass,
including one spent-on-commit call, cancellation, fixed legal cells, no extra
rewards and save checks. The first due-time assertion assumed ceiling instead
of the existing rounded scheduler cost; it was corrected to 343. Broad
acceptance remains pending. Its old ten-seed Wraith spawn fixture no longer
contained a Wraith after the pool grew; expanding it to 100 actual generated
seeds passes all nine Wraith tests without forcing a spawn.
- Web Weaver staging `/var/tmp/roag-web-ty1eeyte`: nine focused tests pass.
Tiny 122 remains the mixed body; inspected native 1-Bit 366 is the alternate
crawler (414/463/60 remain excluded). Webs use drawn lines, two spent attempts,
locked anchors, one +50 crossing delay, finite simulation lifetime and exact
saves. Projectile cuts capture pre-damage blockers; killed blockers cannot
allow the same shot to cut a hidden anchor. Disk acceptance passes all eight scenarios, including a third-process crossing
and save generation 3. Broad acceptance is pending.

All remaining ledger entries stay in scope. Goal ACTIVE; no full-goal completion
claim is made.

Headless browser inspection of the current live catalogue also passes 12 pack/
filter cases, mobile layout and search with no JS errors. Screenshots/logs:
`/var/tmp/roag-ideas-current-catalogue-20261019`. This is HTML inspection, not a
LÖVE GUI or physical-input test.

Oath Twins development is isolated at `/var/tmp/roag-oaths-8hajnz9s`. The
prototype pair distributes half mitigated HP loss in the same primary batch,
with a permanent distance/LOS break, no extra hit procs and normal real kill
rewards. It is not integrated or claimed verified; focused tests and rendering
are next.
