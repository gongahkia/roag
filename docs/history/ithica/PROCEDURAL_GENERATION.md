# Ithica: lunar generation and persistent sites

Ithica combines CA terrain, WFC structures, cellular exposure and global validation. The Moon setting is implemented through explicit ground/collision and sprite casting, not a label change on a forest. [PROJECT_STATE.md](PROJECT_STATE.md) records current content; [LEGACY_GENERATION.md](LEGACY_GENERATION.md) preserves the detailed fantasy-preview algorithms and guarantees reused here.

## Site families

| Family | Size | Construction |
| --- | --- | --- |
| Surface | 48×32 | Existing CA masks and reserved approaches recast as rubble/rock; rescue or salvage objectives |
| Compartments | 51×37 | A 7×5 WFC graph of 7×7 rooms, cellular damage, repair prerequisite and sealed passage |

[LunarGenerator.cs](../src/Ithica.Core/Generation/LunarGenerator.cs) reuses `WorldBuilder.Forest` / `Abbey` as intermediate geometry. Trees and water become blocking lunar terrain; grass/wood become stone. All fantasy objects/dressing are discarded. Current ground uses grit, rock-floor, flagstone, cracked-wall and boulder bindings. Objects become return approaches, stranded people, encounters, recoverable components, a damaged pump and a seal.

Room variants are observed intact/service/damaged compartments. The generator explains no named origin. Art roles, collision, repair and reward rules are separate from publisher identity; current mappings are in [ithica-sprites.md](../assets/reference/ithica-sprites.md).

## WFC and validation

The solver uses authored north/east/south/west sockets and opposing-edge compatibility, retaining the bounded-attempt structural fallback. It does not infer sockets from Urizen indices. The required graph provides connected rooms and a controlled access cut. Global checks after recasting ensure:

- A walkable return exists and every required object is reachable.
- The pump is reachable while the seal is closed.
- The component cannot be reached by bypassing that seal.
- Object IDs are unique, ground is lunar-compatible and outdoor fire/spore sources are absent.

CA erosion cannot silently destroy protected approaches. Gate collision is explicit object state; the open appearance switches after a paid successful interaction. The original fantasy graph/quest tests remain in the compatibility suite.

## Time and cellular fields

The active site's exposure automaton advances every four shared logical ticks. Movement, combat, repairs, rest and travel all contribute their stated costs through the campaign scheduler; the legacy per-step hazard path is disabled for field sessions to prevent double updates. Home pressure, power, jobs and contact survival advance through those same ticks.

Exposed ground can affect health/stress and a kit suppresses nearby sources with an explicit cost. Protected approaches are excluded. The leaf-like overlay is a symbolic exposure/interference marker, not outdoor lunar vegetation. Ordinary fire remains an oxygen-dependent colony simulation. Old preview fire, spores, ponds and forests are not lunar field rules.

At a pending player decision, both sides freeze. Inactive site fields also freeze; known stranded contacts still age whenever campaign ticks advance. This is a scoped active-world simulation, not a claim of full distant ecology.

## Persistence and failures

A site's identity derives from campaign seed and a stable discovery salt. It instantiates once on preparation. Generated maps, seen cells, solver report/version, hazards, completed encounters, loot, repairs and seals are saved as actual state. Loading or revisiting never regenerates them.

Normal return deposits carried salvage once. If a party fails while carrying a component, the undeposited component becomes recoverable again at that same persistent site; cleared encounters stay cleared. This deliberate recovery rule prevents a first-upgrade softlock. A repaired deep pump remains secured and adds one home water each 32 campaign ticks.

## Inspect and validate

```sh
./ithica.sh --scene surface
./ithica.sh --scene structure
./ithica.sh --generation-report --seed 42
./ithica.sh --validate-seeds 1000 --seed -500
./ithica.sh --headless
```

Generation reports/seed validation open no window and write no save. Validation supports 1–10,000 consecutive signed 32-bit seeds with deterministic wraparound. Reports include terrain, room placements, solver statistics, fallback and validity. Native scenes use ordinary game commands to reach the requested phase and do not autosave on exit.

Recorded verification and fallback counts are in [VERIFICATION.md](VERIFICATION.md). Generation-valid layouts are not a substitute for complete command-driven loop checks or native play.
