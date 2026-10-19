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
| `web-weaver` | Web Weaver + anchored webs | headless verified; GUI pending | 444/444 full, nine focused cases, eight disk scenarios including third-process web crossing and save generation 3; this feature commit |
| `hollow-wraith` | Hollow Wraith + announced phase step | headless verified; GUI pending | 398/398 full, nine focused phase/art/save checks, fresh-process Continue; `707c0aec` |
| `gulp-toad` | Gulp Toad + recoverable stolen drop | headless verified; GUI pending | 415/415 full, seven drop tests plus a committed-box Dash fixture, four fresh-process disk scenarios, 337/337 native bindings; `ae231d75` |
| `mirror-hexer` | Mirror Hexer + reflected lane | pending | — |
| `trail-worm` | Silt Worm + temporary bog trail | pending | — |
| `pain-linked-twins` | Oath Twins + breakable pain link | pending | — |
| `alarm-keeper` | Alarm Keeper + one interruptible call | headless verified; GUI pending | 434/434 full, seven focused cases, seven fresh-process scenarios; `33f22d03` |
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
- Web Weaver: **444 passed, 0 failed**, frozen source
  `/var/tmp/roag-web-ty1eeyte`, final log
  `/var/tmp/roag-web-final-verified-headless-20261019.log`. All source/test bytes
  copied into the live checkout match this verified tree. Nine focused web
  cases and eight fresh-process disk scenarios pass. The web scenario also
  crosses the same saved line in a third process and checks generation 3.
- Both Kenney choices now resolve **354/354** required bindings, 53 actor IDs and
  106 static pose aliases; the user-reviewed 324-binding archive remains unchanged.
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

Oath Twins development is isolated at `/var/tmp/roag-oaths-8hajnz9s`.
Focused pair/batch/interrupt/reward/save tests and a fresh-process continuation
pass; broad acceptance is running. Twins share half the mitigated loss in the
same primary HP batch. Distance or blocked LOS breaks their link permanently;
shared loss cannot manufacture another hit proc. The expanded natural pool
exposed a narrow Bone spawn fixture: its first 50 seeds no longer selected a
Splitter. A pure selection query identifies a candidate, then tests construct
and validate the genuine generated stage. Exact before/after snapshots of
24 generated seeds confirm that extracting this query changes no generation.
It is not integrated or claimed complete until its full gate passes.

Headless browser inspection of the catalogue passed 12 pack/filter cases,
mobile layout and search without JS errors at the earlier 341-binding state:
`/var/tmp/roag-ideas-current-catalogue-20261019`. Current derived catalogue
freshness and native references are checked automatically. Browser artifacts
are HTML inspection, not LÖVE rendering or physical input.

All remaining ledger entries stay in scope. Goal ACTIVE; no full-goal
completion claim is made. No LÖVE windows were opened.
