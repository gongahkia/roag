# COS-P02 implementation handoff

## Scope delivered

COS-P02 adds a versioned `region=1` campaign above the P01 one-site core. A new
frontier campaign generates and persists one owned Home Planet landing region and
two unowned moon landing regions. All three run every campaign tick under the
existing one global history. The existing local run, save, map, replay, and `N`
lab default remain available.

This stops before P03/P04: no shuttle, cargo transfer, flight, landing, lunar
founding, cross-site inventory, new physics, procedural names/cultures, or remote
simulation abstraction was added.

Starting checkout: `a606bf53edea3c7d75a239e5ec8e56bfe4d2880e`, branch
`main...origin/main`, clean. Ending checkout is the same HEAD with the uncommitted
P02 paths listed below. No commit, push, reset, clean, dependency installation, or
real-save migration occurred.

## P01 prerequisite preflight

Before edits, `luajit tests/syntax.lua` passed (70 files), `luajit tests/run.lua`
passed (76 groups, 118,514 assertions), and `luajit tests/benchmark_smoke.lua`
passed. The P01 implementation/test mapping was read and retained:

- Local/campaign projection and 2,000 ticks: `tests/campaign.lua` P01-B;
  `compareLocal` removes only `frontier` and worker `personId`.
- One begin, ordered command application, one body: `src/sim.lua`,
  `src/campaign.lua`, and P01-B/P01-D.
- ID/clone/non-mutating validation: `Campaign.new`/`Campaign.clone` and P01-F/G/H.
- Whole timeline seek/branch/checkpoint behavior: `src/campaign_history.lua` and
  P01-C/E/J.
- Structural restore versus bounded explicit replay verification:
  `CampaignHistory.restore`, `beginVerify`/`updateVerify`, and P01-C.
- Separate 32 MiB campaign replacement and legacy sentinel: `src/storage.lua` and
  P01-I.

No narrow P01 repair was necessary. P01 behavior still executes in the post-change
suite (P01-A through P01-J all pass).

## Source changes and public contracts

- `src/campaign.lua`: accepts legacy `{core=1}` unchanged and adds strict
  `{core=1,region=1}` state, stable body/site records, bounded notices, global
  extinction, sorted site/body processing, `newRegion`, campaign metrics, and
  three-site staged stepping. Regional state has fixed names `Home Planet`,
  `Moon I`, `Moon II`; moon recipes persist version, layout, climate, dimensions,
  terrain seed, and current supported options.
- `src/campaign_random.lua`: frozen campaign-only v1 Park--Miller stream,
  ASCII namespace derivation, and rejection-sampling integer selection. It does
  not modify legacy map RNG behavior.
- `src/expedition.lua`, `src/generation/frontier.lua`, `src/generate.lua`: split
  landing geometry from population and expose `makeUnpopulated`. Legacy
  `Frontier.make` still calls populated generation; moon worlds use exactly the
  existing Frontier terrain/content rules but get no crew/supplies/structures/jobs.
- `src/campaign_commands.lua`: requires current society ownership at queue and
  application for a site envelope.
- `main.lua`, `src/render.lua`, `src/ui/crew.lua`, `src/ui/fieldnotes.lua`: thin
  local/campaign view routing. Local UI payloads are wrapped at submission with the
  selected `siteId`; render/inspection views use only the selected world from the
  viewed history state.
- `N` remains the lab. `C` cycles its explicit actions: local creation (default),
  New frontier campaign, Continue frontier campaign. The first campaign Enter
  generates/validates all three worlds; the second confirms, replaces the session,
  and writes only `campaign.run.dat`.
  Continue reads only that slot. A local preview is labelled as local whenever the
  selected action is campaign creation.
- Existing `F7` remains biome view. `Shift+F7` and the visible Region button open
  the campaign-only overlay. The overlay consumes input; Space is the global clock;
  Escape/toggle closes it. Owned sites are selectable in stable ID order, including
  an empty fixture settlement. Unvisited moons remain summary-only with
  `Unvisited — Transport pending`.
- `tests/region.lua`, `tests/region_gui.lua`, `tools/region_benchmark.lua`,
  `tests/all.lua`, and `tests/syntax.lua`: P02 coverage and manifest registration.
- `docs/FRONTIER_CONTRACT.md`: actual feature/state/UI compatibility contract.

Commands retain `{scope='site',siteId,payload}`. `personId` remains campaign-wide;
all local jobs/navigation use `worker.id`. `Campaign.newRegion` accepts test-only
`bodyOrder` so construction order can be verified without making the production
schema variable. Moons choose sorted `Layouts.names`, then sorted `Biomes.climates`
using the recipe namespace. All sites have existing gravity/material/ecology rules.

## Verification

Post-change commands and results:

```text
luajit tests/syntax.lua                         PASS, 74 files
luajit tests/run.lua                            PASS, 85 groups; 120,816 assertions
luajit tests/region_gui.lua /tmp/cos-p02-region-gui
                                                PASS P02-I (mocked UI/storage)
luajit tests/benchmark_smoke.lua                PASS
luajit tools/headless.lua 12345 frontier 2000  PASS, CPU-only trace/accounting
luajit tools/expansion_soak.lua                 PASS, six replay/accounting cases
luajit tests/maximum_size.lua                   PASS, 11 max-map smoke cases
luajit tools/region_benchmark.lua 2000          PASS P02-J diagnostic
git diff --check                                PASS
```

`tests/region.lua` covers P02-A through H plus malformed regional state. P02-D
runs 2,000 ticks and checks shared ticks, an off-screen worker need, an owned-fixture
farm, fuse, unviewed falling water, and unviewed fauna progression. P02-E checks
colliding local worker IDs at distinct sites, site-bound routing, and reordered
site storage. P02-I is the separately runnable mock UI adapter: explicit lab
actions, isolated legacy sentinel, corrupt continuation preservation, overlay modal
input, global Space, unvisited visibility, empty owned selection, same-tick cache
identity, and historical ownership visibility.

P02-J on this host (`os.clock`, 2,000 no-command ticks, 128x80 home plus two
192x112 moons) observed:

| Simulated maps | Whole CPU seconds | Per-site tick CPU seconds |
| --- | ---: | ---: |
| 1 | 0.341851 | 0.000170926 |
| 2 | 0.938138 | 0.000234534 |
| 3 | 1.919507 | 0.000319918 |

The three-site checkpoint diagnostic took 0.236691 CPU seconds; encoded campaign
bundle was 3,183,760 bytes; Lua heap sample was 35.574 MiB. Generation sample was
0.067053 CPU seconds. These are single-host CPU observations, not GPU FPS, RSS,
portable performance evidence, or a reliable P01/P02 percentage comparison.

All mock save writes used disposable `/tmp/cos-p02-*` directories. No real LÖVE
window was launched because save/display isolation was not established. Mock tests
do not verify SDL delivery, GPU rendering, actual filesystem behavior, or FPS.

## Manual isolated-play checklist

With a disposable LÖVE save directory: open `N`; verify local action remains the
default and Enter retains its preview/confirm flow. Cycle with `C` to create a
frontier campaign, then verify Home Planet selected, the legacy `run.dat` unchanged,
and `campaign.run.dat` created. Open Region via its button or Shift+F7; verify the
two moons read `Unvisited — Transport pending`, cannot switch to terrain, and Space
changes the one global pause. In an explicit owned-site fixture, select an empty
site and verify simulation continues at home. Seek to an earlier view and verify a
future-owned selector is not exposed. Do not use a player save for this checklist.

## Remaining limits

Campaign structural load intentionally does not prove history consistency; use the
bounded explicit replay verifier. P02 keeps the P01 32 MiB cap and save replacement
limitations. Notice coalescing is limited to deterministic typed local events and
depopulation; it is not an event engine. The region UI is mock-verified only.
Performance overhead relative to P01 remains unverified because no comparable,
stable repeated baseline sample was obtained. Stop at P02.
