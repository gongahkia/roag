# COS-P03 implementation handoff

## Scope delivered

COS-P03 adds a feature-gated, headless-first logistics layer to regional
campaigns. New frontier campaigns have one empty home-docked shuttle, a bounded
physical cargo hold, one active manifest per craft, loading/unloading jobs, and
explicit passenger assembly. The implementation stops before departure: no travel,
off-map passenger custody, landing, founding, resupply, hull damage, cargo fuel,
planetary physics, personal discovery, culture, or schools was added.

The owner requested Cozette during this pass. `assets/fonts/cozette.otb` is the
upstream MIT Cozette v1.30.0 bitmap font; its notice is in
`THIRD_PARTY_NOTICES.md`. `src/render.lua` uses it for every renderer font size,
with a default-font failure fallback so an unreadable asset does not make an error
screen unreadable.

## Checkout and P02 gate

The P03 inspection began on `a606bf53edea3c7d75a239e5ec8e56bfe4d2880e` with the
reported P02 worktree changes present. During this pass, the owner advanced HEAD
concurrently through their own commits; final inspected HEAD is
`d2582059b5ecbac8161372782765c2af7f80f34a` (`d258205 clearedlogistics`). I did
not commit, push, reset, clean, install dependencies, or touch a real save.

At handoff, P03 refinement/doc changes remain uncommitted in
`docs/FRONTIER_CONTRACT.md`, `main.lua`, `src/logistics.lua`, `src/world.lua`,
`tests/logistics.lua`, `tests/logistics_gui.lua`, `tests/syntax.lua`, and new
`docs/implementation/COS-P03.md` and `tools/logistics_benchmark.lua`. Earlier
P03 files were included in the owner's concurrent commit. Recheck status before
further work; do not treat this HEAD as an agent-created clean milestone.

P02 gates were rerun before feature edits: `tests/region.lua` P02-A/B/C/F proves
seeded terrain/recipe isolation, reference RNG and unpopulated moons; P02-D/E/G
proves 2,000-tick simultaneous simulation, camera-independent outcomes,
site-bound commands, colliding local IDs and non-global home loss; P02-H proves
whole-campaign save/seek/branch; `tests/region_gui.lua` proves N-lab default,
isolated Continue behavior, Shift+F7/modal semantics, selector/cache and hidden
moon summary boundary. The final normal suite reran all P01 and P02 cases.

## Actual contracts

`src/campaign.lua` accepts `features={core=1,region=1,logistics=1}` only when a
validated `logistics` record is present; the feature requires `region=1`. P01/P02
and legacy state remain feature-off. `Campaign.newRegion(...,{logistics=true})`
creates:

```lua
logistics={version=1,
 rules={version=1,seats=3,cargoCapacity=24,maintenanceMetal=1,assemblyRadius=8},
 nextCraftId=2,nextManifestId=1,nextOperationId=1,
 crafts={{id=1,ownerSocietyId=1,seats=3,capacity=24,dockedSiteId=1,
          anchor={x=...,y=...},cargo={},activeManifestId=nil}},
 manifests={},operations={}}
```

`src/logistics.lua` bounds crafts to four, active manifests to four, operations to
16, cargo resources to `food`, `metal`, `soil`, `stone`, `water`, capacities to 24,
and all IDs/counters to positive safe integers below 100,000,000. A worker is never
copied into a global people table or craft passenger body. An active manifest stores
source/destination/craft IDs, sorted unique `personId` values, a positive target
cargo map, monotonic revision, and candidate assembly poses. Local `worker.id`
continues to identify jobs/navigation. Campaign validation rejects malformed
cross-record references, duplicate IDs, holes, incompatible feature combinations,
and campaign-local-world tick divergence.

`src/campaign_commands.lua` retains local envelopes and adds source-bound campaign
commands: `prepare_expedition`, `assemble_expedition`, `cancel_expedition`,
`unload_cargo`, and `cancel_cargo_unload`. Queue validation is pure. Application
revalidates state; if an earlier global command makes a later same-tick assembly
inapplicable, it returns a deterministic reason without partial mutation.

`Campaign.step` remains: campaign tick increment; each world begins once in stable
site-ID order; recorded commands apply in array order; logistics reconciliation
runs once; each existing local `Sim.body` runs once in stable site-ID order. The
context passed into `Sim.body`/`Colonists.step` is transient and not serialized.
`src/jobs.lua` adds `load`/`unload` work through the existing haul role, navigation,
item reservation, carry, route revalidation, interruption and drop-release paths.
Cargo is not a local stockpile: ordinary eating/building cannot debit it.

Custody is exactly pile -> carry -> craft or craft -> carry -> local pile.
`craft.cargo + current load commitments` cannot exceed capacity or manifest target.
Requests/reservations are not resources. Manifest cancellation stops matching jobs
and assembly directives but never teleports cargo back; changed lower targets expose
physical surplus for explicit unloading. `Campaign.metrics` adds docked cargo once
and exposes `Logistics.reconciliation` with ground/carry/escrow/cargo per-resource
breakdown.

Preparing does not summon passengers. `assemble_expedition` requires exact target
cargo, one metal unit aboard for a later flight, no operations, and living source
passengers. It selects distinct safe/reachable standing poses in the eight-cell
Manhattan anchor radius by person ID, then distance/y/x. A matching assembly
directive is only a normal movement directive: hunger, fatigue and hazards win;
a later rally wins; cancellation does not erase that later rally. `readiness` is
pure and checks actual cargo, route, living people, directive/pose match and safety.

## UI

`N` still defaults to local creation; New frontier campaign now creates
`logistics=1` campaigns. Continue retains feature-off P01/P02 behavior and does
not inject a shuttle. Region's home card exposes **Prepare craft**; its modal
supports UI-only destination/passenger/cargo drafts, Apply, Assemble, Cancel and
surplus unload actions. It consumes clicks and Escape returns to Region; Space is
still the global clock. It states exactly: `Departure becomes available in the next
implementation tranche.` The marker is nonblocking presentation only. Shift+F7,
the Region button, selectors and unvisited-moon information boundaries remain P02
behavior.

## Acceptance coverage

`tests/logistics.lua` is registered from `tests/all.lua` and covers:

- P03-A: one empty 3-seat/24-slot craft and no altered starter world.
- P03-B: food/metal physical load/edit/surplus-unload conservation and campaign metrics.
- P03-C: unavailable local supply plus flooded route interruption/drop behavior.
- P03-D: malformed/cross-source rejection and safe same-tick invalidation.
- P03-E: repeated plan idempotency and completed cargo surviving cancellation.
- P03-F: death while carrying uses ordinary drop custody.
- P03-G: staged save/load, continuation, seek/branch and explicit replay verification.
- P03-H: pure readiness and changed passenger position failure.
- P03-I: fixture local-ID collision and independent craft/source routing.
- P03-J: hauling OFF remains OFF; logistics does not rewrite labour policy.
- P03-K: all three original people load before explicit assembly, then assemble.
- P03-L: edit revision, capacity rejection, surplus unloading and bounded records.
- P03-M: later rally supersedes assembly; cancellation preserves that later rally.
- P03-O: feature-off P02 boundary, invalid dependency rejection, save roundtrip and map-template exclusion.

`tests/logistics_gui.lua` is the separately runnable P03-N mock adapter. It checks
the Region-to-expedition path, bound source command, modal click isolation, pure
draw inspection, Escape return, and practice-history branching that clears the
stale modal draft. It is not a real-window proof.

## Verification

Final commands:

```text
luajit tests/syntax.lua                         PASS, 78 Lua files
luajit tests/run.lua                            PASS, 99 groups; 120,886 assertions
luajit tests/benchmark_smoke.lua                PASS, mocked benchmark adapter
luajit tools/headless.lua 12345 frontier 2000  PASS, CPU-only trace/accounting
luajit tools/expansion_soak.lua                 PASS, six replay/accounting cases
luajit tests/maximum_size.lua                   PASS, 11 maximum-map smoke cases
luajit tests/region_gui.lua <disposable /tmp>   PASS, mocked UI/storage
luajit tests/logistics_gui.lua <disposable /tmp>
                                                PASS P03-N, mocked UI/storage
luajit tools/logistics_benchmark.lua 500        PASS, CPU-only diagnostic
git diff --check                                PASS
```

For the seeded 128x80 home plus two 192x112 moons, an initial logistics campaign
encoded to 3,155,709 bytes. A 420-tick staged food/metal loading trace encoded to
3,173,612 bytes with two retained checkpoints. Both are below the unchanged 32 MiB
save bound. P01-I still covers successful, write-failure, oversize and rename-failure
campaign replacement against a legacy sentinel; P03-O covers a logistics history
roundtrip. All mock storage uses disposable `/tmp` paths.

The 500-tick P03 diagnostic observed generation 0.098846 CPU seconds; one/two/three
isolated-world subsets 0.144714/0.409540/0.719628 CPU seconds; a three-world
checkpoint 0.294254 CPU seconds; 3,184,494 encoded bytes; and 36.018 MiB Lua heap.
Seven alternating full three-world empty-logistics samples had medians P02 0.682175
CPU seconds and P03 0.664153 (reported delta -2.6%). This is a noisy, CPU-only,
host-specific observation—not GPU FPS, RSS, a cargo-loaded benchmark, or a portable
overhead guarantee.

LÖVE 11.5 loaded the bundled Cozette OTB in a minimal auto-closing temporary
project under an isolated `XDG_DATA_HOME`; Fontconfig also identified it as Cozette
Medium at 13px. The game itself was not manually played in a real window. The
temporary font-check directory remains under `/tmp/cosmonauts-cozette-font.FtvYii`
because the environment rejected its removal; it contains no game save.

## Manual isolated-play checklist

Use a disposable LÖVE save directory. Press N, cycle C to New frontier campaign,
confirm twice, then open Region with its button or Shift+F7. Choose Prepare craft.
Select one or all settlers, request food plus one metal, Apply, run until items move
from the home pile through carries into the docked hold, then Assemble. Verify a
selected hungry/tired person can leave and readiness becomes false. Lower a target,
unload the displayed surplus, and cancel preparation: loaded cargo must remain until
unloaded. Confirm departure remains unavailable and no moon becomes owned. Do not
run against a player save.

## Remaining limits

No real gameplay-window interaction was exercised beyond the isolated font-load
check; all UI-flow evidence is mocked. Ordinary structural campaign restore still
does not replay the full command log; use explicit bounded replay verification.
The storage replacement limitation remains non-fsync-backed. P03 has no transport
or foundation logic, and its single docked hold is not yet a passenger cabin. Stop
here; P04 requires a separate owner-authorized task.
