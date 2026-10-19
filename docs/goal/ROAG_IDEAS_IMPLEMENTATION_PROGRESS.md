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
| `reanimator` | Grave Cantor + finite resurrection | pending | — |
| `haunted-cache` | Haunted cache / chest mimic | pending | — |
| `heavy-rammer` | Heavy Rammer + positional shove | implemented; regression hardening | Source in `38c34a96`; 12 focused checks, real rotating-store Continue; complete legal corpus gate remains open |
| `web-weaver` | Web Weaver + anchored webs | pending | — |
| `hollow-wraith` | Hollow Wraith + announced phase step | pending | — |
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

Task 0 complete: preserved original reviewed inventory and archive/source hashes; freshness tooling validates the historical user-reviewed snapshot independently of live assignments. Six feedback state tests, source checks and previous 30/30 squad acceptance pass.

Rammer integration is present; 12 focused checks cover strict locked shove timing,
blocked landings, hazards/pickups/snares, friendly fire, interruptions, exact save
continuation, presentation-only slides and a seven-biome paired-floor corpus.
`sh tests/idea_enemy_disk_resume.sh` passes two separate LuaJIT processes with
real checked save generations. GUI remains untested.

The first expanded full run was **383 passed / 3 failed** (log
`/var/tmp/roag-ideas-rammer-20261019.log`): one obsolete encounter-pool assertion,
one invalid route-history fixture and one guided navigation limit. The two
fixtures are corrected. Guided routes expose pre-existing automation errors:
repeated safe-cell dodges, firing through blocked projectile corners, attacking
protected brutes without luring/flanking, and dodging fully blocked damage.
These are driver changes; actor stats, combat rules, RNG and map geometry are
unchanged. Some attempted driver revisions changed routes adversely; their logs
are preserved, and no green full-suite claim is made for those revisions.
`/var/tmp/roag-rammer-focused-12.log` is **12/12**. The two remaining failures
in frozen broad runs were the older single-floor seed-3 Victory/profile gates:
the guide abandoned an earned chest and ten-HP supply, entering the finale at
5 HP. It now claims the normal reward and collects the existing supply through
legal actions. Seed 3 wins at 9 HP after 1,039 actions, with exact full snapshot
replay (`/var/tmp/roag-seed3-earned-recovery.log`). Both actual failing gates
pass in `/var/tmp/roag-seed3-gates-accepted.log`; the complete final rerun is
live in `/var/tmp/roag-rammer-final-headless-20261019.log`.

Wraith is staged in `/var/tmp/roag-idea-wraith-0_uo5rfw`: actual announced wall
crossing, separate normal attacks, only 416 in both packs, protected/visible
landings, Trapper contact and fresh-process continuation. Its focused checks
pass; a frozen full run was **395 passed / 1 failed**, the remaining guided-run
failure. Grave Cantor is staged in `/var/tmp/roag-idea-revival-kf0ggjgt`: eight
focused tests and three fresh-process encounter scenarios pass. Neither staged
feature is claimed live or complete yet. Wraith's revised full legal-run group
passes **4/4**, including six class victories and all seven miniboss routes
(`/var/tmp/roag-wraith-legal-accepted-latest.log`). Its complete rerun is live in
`/var/tmp/roag-wraith-final-headless-20261019.log`. Gulp Toad is also staged in
`/var/tmp/roag-idea-toad-wdfn7s52`: loose-drop interception, one original carried
drop, death release, source references, save/replay and a seeded population/art
corpus. It is not yet integrated. Keep these task-local workspaces until
their reviewed changes are integrated; they are not personal project/save data.

Next: finish verified legal-driver hardening, then integrate Wraith and Cantor
in separate task commits. Every other ledger row remains pending. Goal ACTIVE.
