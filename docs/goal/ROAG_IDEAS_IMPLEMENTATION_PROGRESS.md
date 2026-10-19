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
| `reanimator` | Grave Cantor + finite resurrection | headless verified; GUI pending | 407/407 full, nine focused cases, 210-stage natural company corpus, fresh-process pending-revival Continue; this feature commit |
| `haunted-cache` | Haunted cache / chest mimic | pending | — |
| `heavy-rammer` | Heavy Rammer + positional shove | headless verified; GUI pending | Source `38c34a96`, hardening `0f643d9e`; 389/389 full, 12 focused, fresh-process rotating-store Continue |
| `web-weaver` | Web Weaver + anchored webs | pending | — |
| `hollow-wraith` | Hollow Wraith + announced phase step | headless verified; GUI pending | 398/398 full, nine focused phase/art/save checks, fresh-process Continue; `707c0aec` |
| `gulp-toad` | Gulp Toad + recoverable stolen drop | pending | — |
| `mirror-hexer` | Mirror Hexer + reflected lane | pending | — |
| `trail-worm` | Silt Worm + temporary bog trail | pending | — |
| `pain-linked-twins` | Oath Twins + breakable pain link | pending | — |
| `alarm-keeper` | Alarm Keeper + one interruptible call | pending | — |
| `ember-moth` | Ember Moth + readable burning landing | pending | — |
| `bone-splitter` | Bone Splitter + reward-bounded fragments | pending | — |
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
- Both Kenney choices now resolve **334/334** required bindings, 47 actor IDs and
  94 static pose aliases; the user-reviewed 324-binding archive remains unchanged.
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

Next: commit the verified Cantor slice, then integrate and verify Toad in a
separate task commit. A Toad staging run exposed two obsolete pre-Wraith
catalogue-count assertions; its log is retained at
`/var/tmp/roag-toad-final-headless-20261019.log`. The corrected staging has all
eight native-art checks green and its full regression run is underway; this
is not a passing full-suite claim yet. The Haunted Cache is staged separately
at `/var/tmp/roag-cache-xl1hbwa3`: six focused tests, five disk scenarios and
the combined 43 encounter checks pass; broad acceptance is still pending.
All remaining ledger entries stay in scope. Goal ACTIVE; no full-goal completion
claim is made.
