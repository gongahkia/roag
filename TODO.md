# Jomon — Authoritative Development Plan

## Authority and maintenance

This file is the source of truth for Jomon’s delivery process: product constraints, work order, task status, acceptance criteria, and open decisions. `LORE.md` is the canonical setting and content reference. If the documents conflict, do not implement the disputed behaviour; resolve and record the product decision in both documents first.

Status markers have a strict meaning:

- `[x]` complete: implemented where applicable and verified against its stated acceptance criteria.
- `[-]` active: the single current implementation slice.
- `[ ]` planned: not yet started. Do not infer implementation from a planned item.
- `Decision`: needs explicit product-owner direction; do not fill it with an unrecorded assumption.

At the end of every completed slice, update this document: mark only verified work complete, add the verification actually run, split the next item when it becomes clearer, and record any decision that changes scope or direction. Do not recreate superseded plans or treat Git history as active direction.

## Current state

Jomon is being rebuilt as an original low-mysticism late-medieval river-and-coast roguelike. The checked-in game is a substantial but superseded space-fiction prototype. It may inform architecture and testing technique, but its user-facing lore, vocabulary, assets, progression, save migrations, routes, and content are not requirements for the medieval game.

The documentation reset and Phase 1.1 are complete. The rebuild now has a deliberately minimal, non-gameplay bootstrap: a deterministic foundation household, initial-courier choice, isolated local records, and an original keyboard canvas shell; its authored Chromium keyboard flow also passes. The generation-configuration, manifest-provenance, content-boundary, initial-world-pipeline, expanding-frontier, sixteen-colour presentation, settings UI, generation-test, action-clock, mutable-world-state, and persistent-person contracts are complete. Deterministic fidelity tiers are the sole active Phase 1.3 slice; no walkable map, deck interaction, trade, combat, travel, or simulation gameplay is represented as complete.

## Context-free implementation handoff

Any coding agent begins by reading `LORE.md`, this file, and `README.md`, in that order; then checks `git status` and preserves unrelated work. These three root documents define the medieval game without requiring chat history. This repository’s existing TypeScript application is a non-canon space-fiction prototype. It is useful only for narrow technical reference—especially its terminal canvas, test conventions, local persistence error handling, and the spatial idea of a walkable vessel with a sidebar.

- Begin all new medieval domain code under `src/medieval/`, with small, named modules and colocated focused tests. The medieval domain must not import prototype gameplay, save schemas, lore, content, or progression types.
- Do not rename, migrate, or reinterpret prototype modules into medieval systems. Keep the prototype isolated until a completed roadmap item specifically replaces a user-facing path; preserve unrelated prototype tests unless their removal is itself a verified task.
- The medieval app receives its own root state, renderer-independent domain contracts, IndexedDB namespace, and browser bootstrap path. Its only temporary shared dependencies may be generic browser/tooling primitives that carry no prototype game meaning.
- Treat a seed plus resolved `WorldGenerationConfig` plus generator version as the reproducibility boundary. Do not add per-person rerolls, unseeded randomness, wall-clock simulation, network authority, or hidden state mutations.
- Add or update a focused test before each new behaviour. A passing prototype test suite is not evidence that a medieval feature exists. Keep exactly one `[-]` roadmap item active and update this file only after verification.

## Non-negotiable product decisions

- Jomon is a persistent itinerant household on a river-basin-to-coast network.
- Every new world starts with a wholly generated Jomon household. Its roster, identities, roles, relationships, histories, equipment, and active-courier eligibility derive deterministically from that world’s seed and resolved configuration, so the same manifest recreates the same crew and world.
- New-world creation shows the generated eligible crew and lets the player choose one initial courier. There are no individual roster rerolls: changing the crew means changing the seed or generation settings. Later voluntary perspective changes happen physically at Jomon’s tavern.
- The player inhabits rotating Jomon crew members. A truly lost courier is permanent, subject only to exceptionally rare and explicit mystical safeguards. If no living eligible crew member remains, active play ends and the world becomes a read-only, exportable chronicle even when Jomon still physically exists.
- Jomon’s tavern, chart table, cargo hold, repair space, stores, berths, galley, and gangplank are walkable map spaces. Physical props use compact contextual key-choice prompts. A deliberately comprehensive management sidebar is visible by default, compact, and collapsible; it shows all facts known by the household—each with source and freshness where relevant—but never undiscovered global truth. It complements physical surfaces without replacing their direct action or situated information, and the map remains primary.
- Gangplanks and quays replace airlocks and landing terminals.
- Trade uses physical commodities with capacity, condition, handling, loss, recovery, local supply, demand, and market consequences.
- Human conflict, wilderness, and operational hazards must receive equal systemic depth.
- Combat remains turn-based and grid-based, but its old content and progression model will be redesigned.
- Jomon is low-mysticism medieval fantasy. Rare relics, totems, boons, curses, rites, omens, and other uncanny forces can have real mechanical effects, but there is no generic mage class or unlimited free-form spellcasting.
- Simulation time is action-driven, not wall-clock-driven. It advances only when the player takes or confirms a time-bearing in-world action—such as movement, waiting, travel, work, rest, or delegated-task commitment/resolution. Pure inspection, sidebar use, and an idle open tab never advance it. All consequential randomness is seeded and inspectable.
- Jomon is an open-ended persistent world simulation, not a finite campaign to be won. Short-term goals provide direction; exploration, relationships, and the changing household/world provide the long-term play.
- The player is not bound to a single protagonist. On an active courier’s death, control transfers to another eligible crew member; voluntary perspective changes occur at the tavern.
- An active courier directly controls only themself. They can delegate work to crew and NPCs through conversation; delegation depends on the courier’s `conversation` stat and the other person’s relationship, role, capacity, interests, and current situation.
- Failure is causal and diegetic. A courier death, lost cargo, unpaid debt, damaged route, broken relationship, or ruined vessel can permanently change the world. The irreversible collapse or loss of Jomon ends active play in that world and converts it to a read-only, exportable chronicle.
- Jomon can grow through new rooms, refitted workspaces, tools, crew capacity, and small craft. Every expansion is physical, persistent, grounded in available materials/labour, and balanced by upkeep, space, staffing, cargo, route, or social trade-offs.
- A continuing world has escalating difficulty eras: base, NG+, and NG++. Active-play progression in the same persistent world moves it toward later eras without a reset or finite campaign ending. Each era adds durable, diegetic pressures and new possibilities rather than merely increasing enemy numbers.
- The initial release target is a desktop browser game for players who enjoy ASCII graphics and keyboard-first control.
- Jomon is single-player and offline-first. It requires no account, server, cloud sync, network connection, remote telemetry, or multiplayer authority. A local browser may retain multiple active worlds and read-only chronicles, subject to quota; only one world may be open and mutate in a browser session at a time.
- Every instantiated person has a full individual persistent record—including family, work, needs, relationships, injury, possessions, birth, and death—while distant people advance through deterministic summary simulation at an appropriate fidelity level.
- The world has an expanding procedural frontier. Newly explored regions are generated causally and become persistently explorable as Jomon travels; rumours, charts, travellers, and trade can establish their existence before arrival. An ungenerated frontier has deterministic regional commitments rather than latent mutable people; once a region or named person must exist, it is instantiated with its full persistent record and cannot later contradict prior knowledge.
- No user mod/content-pack compatibility is planned. Internal content remains data-driven and documented for maintainability, but does not promise a public extension API.
- Do not include or procedurally generate sexual violence, slavery, torture, or harm/endangerment of children. This applies to player actions, events, histories, simulation summaries, enemy behaviour, contracts, hazards, rumours, and player-facing text.
- The reset is a clean persistence break. Do not migrate the superseded space-era saves or reinterpret them as medieval campaigns.
- Do not copy lore, text, names, assets, or exact mechanics from other games. Historical and game references are influence constraints only.

## Rogue-inspired visual and interaction direction

Jomon takes clear, original inspiration from **Rogue (1980)**: a terminal-first, keyboard-led, turn-based grid game whose map is the primary visual explanation of play. This is an aesthetic and interaction constraint, not permission to copy Rogue’s copyrighted presentation or fantasy content.

- ASCII is a first-class, release-quality presentation—not a temporary fallback. The detailed renderer must expose exactly the same consequential state.
- Favor a sparse character-cell grid, limited purposeful colour, strong contrast, and immediate silhouettes over decorative pixels, painterly scenes, or interface clutter.
- The map communicates position, architecture, actors, cargo, props, hazards, routes, and discoveries directly. Every symbol must have a stable, original, documented meaning.
- Use a compact, persistent status readout and terse message feedback. The player should normally understand the current danger and affordances without opening a separate screen.
- Keep direct keyboard control and compact contextual prompts. Support familiar eight-direction movement where it improves play, alongside discoverable/remappable controls and accessible alternatives.
- Use original glyph assignments, layout, copy, assets, sounds, names, and mechanics. Jomon deliberately uses a Dwarf Fortress Classic-inspired sixteen-colour console palette and its dark/bright pairing discipline, but retains original semantic colour assignments and does not reproduce any other game’s graphics, box art, interface layout, source code, or fiction.
- The medieval material world is never weakened to imitate Rogue: Jomon’s readable symbols must describe vessels, work, people, weather, cargo, tools, grounded danger, and the rare uncanny phenomena that materially affect them—not default spellcasting or generic fantasy monsters.

## Emergent world and people direction

Jomon’s world should feel alive in the sense of Dwarf Fortress Adventure Mode, Caves of Qud, RimWorld, Rogue, and Risk of Rain: it has a remembered past, people pursue their own material and mystical goals, the player can build tactical power through legible combinations, and the player’s actions become part of later situations. The player experiences the world through a courier on Jomon, supported by both situated in-world surfaces and a comprehensive management sidebar that is visible by default while keeping the map primary. The sidebar provides complete household-known information, not supernatural knowledge of hidden world state.

- The world evolves only through action-driven in-world time. Each time-bearing action processes due deterministic events across all fidelity tiers, so distant people and institutions can advance while the courier acts elsewhere. Inspection, an idle browser tab, pause, and a closed browser do not advance time.
- A world begins with a causally generated history: geography and waterways first; then ecology, resources, settlements, institutions, people, routes, trade, and historical events. Its starting state must be inspectable. The world continues into a deterministic expanding frontier as Jomon travels rather than ending at that initial generated region. Before a frontier region is materialized, persist only its deterministic regional commitments and any already revealed facts; materialize full people and mutable local state when a region or named person must exist.
- World generation aims for Dwarf Fortress-like depth and configurability, adapted to Jomon’s scale. Advanced settings cover region size, history length, climate, terrain and waterways, settlement and population density, political fragmentation, resource scarcity, ecology, dangers, era pace, and simulation fidelity. Players can select, save, inspect, and reproduce presets and advanced settings; every generated world records its seed, resolved settings, generator version, and validation/rejection result.
- Content-validation rules must reject sexual violence, slavery, torture, and harm/endangerment of children from generated history, people, places, events, contracts, hazards, rumours, and simulation summaries.
- Every instantiated person has persistent identity, location or home, role, material interests, family, work, needs, relationships, injury, possessions, birth, death, memories, and a readable history of consequential encounters.
- NPC actions must arise from original needs, opportunities, relationships, and local conditions—not an authored sequence disguised as simulation. Nearby and important people receive detailed simulation; every distant person retains full individual state and advances through deterministic scheduled summaries.
- The world must surface change through physical places, conversations, ledgers, rumours, goods, routes, visible work, and the always-visible-by-default management sidebar. The sidebar labels information source, discovery time, and freshness when relevant. These surfaces are complementary and must not merely duplicate one another. A simulation that players cannot discover or act upon is out of scope.
- Jomon expansion and exploration are the provisional long-term motivations. Every future vessel upgrade must add a physical space, a new material capability, or a meaningful new trade-off.
- Short-term goals must always be available through local pressures, contracts, favours, shortages, discoveries, threats, or crew needs. They guide play without creating a mandatory campaign finish line.
- Escalation must keep the world generative after NG++: later eras remix and extend the same systemic content families, change world conditions and relationships, and create new material problems, opportunities, and vessel choices. They do not require a new save or a campaign restart.
- Base, NG+, and NG++ are tracked world eras, not separate worlds. Accumulated in-world active-play time and Jomon’s growth advance the era; Jomon and surviving crew remain continuous while the wider world changes. NG++ plateaus in raw escalation and continues to remix systemic content instead of becoming numerical inflation.
- Shadow of Mordor is only a high-level reference for the feeling that remembered people can react and recur. Do not implement or market a “Nemesis System,” reproduce its hierarchy/vendetta design, or derive from its protected implementation. Use an original, documented social-memory model instead.

## Ordered implementation phases

### 0. Documentation reset `[x]`

- [x] Replace the old canon with `LORE.md` and establish root `TODO.md` as the governing delivery plan.
- [x] Remove superseded active plans and obsolete playtest documentation rather than reviving or archiving them as current direction.
- [x] Keep `README.md` truthful: the checked-in game is a prototype and the medieval implementation begins with a clean break.
- [x] Record Rogue-inspired terminal presentation, emergent-world direction, and non-copying boundaries as product constraints.
- [x] Supersede the no-magic setting constraint with the bounded low-mysticism direction in `LORE.md` and this plan.

Verification: manual repository review on 2026-09-01; documentation only. No build, test, or browser execution was run for this documentation slice.

### 1. Foundational world systems

#### 1.1 Clean game boundary `[x]`

- [x] Define the new medieval save namespace, version, and invalid-save behavior. Existing prototype saves are ignored, never migrated: only `jomon-medieval-worlds-v1` is opened, and malformed records resolve as unavailable.
- [x] Establish a clean `src/medieval/` root with renderer-independent foundation types for worlds, Jomon identity, deck partitions, quays, vessel props, crew, manifests, and zero-time action-clock seed state. It has no prototype gameplay/state imports.
- [x] Define a medieval browser bootstrap path, root state, routing boundary, and temporary generic-tooling adapters. `/` enters only medieval state; `?prototype` remains an explicitly isolated diagnostic path.
- [x] Define seeded game creation, a multi-world/chronicle index, an initial-courier selection surface, and read-only chronicle inspection/export. `MutableWorldSession` makes single-world mutation ownership explicit within a browser root.
- [x] Define the offline-first bootstrap contract in implementation: the medieval entry path has no account, remote telemetry, cloud, multiplayer, or network client; it uses local IndexedDB and packaged CSS only.
- [x] Remove prototype terminology from every current medieval user-facing canvas surface.
- [x] Add focused unit tests for clean creation, seed/manifest determinism, initial-courier selection, malformed-record invalidation/no migration, multiple-world indexing, finalized chronicles, and one-active-world ownership.
- [x] Run the authored Chromium browser test for the keyboard creation/select/save/resume flow and its no-external-request assertion. On 2026-09-01, `npx playwright test e2e/medieval-foundation.spec.ts` passed its one Chromium test after installing the local Playwright Chromium binary. The flow uses keyboard input to create, select, save, and resume a world, and asserts that no request leaves the local preview origin.

Acceptance: a fresh medieval world can be created and an initial courier selected deterministically; a prototype save cannot load as one; multiple local worlds can be indexed without loading each other; no network request or space-era term appears anywhere on the medieval new-game path.

Implementation design for this slice:

- Create `src/medieval/` as the only home for new medieval code. Start with `types.ts`, `rng.ts`, `world.ts`, `storage.ts`, `app.ts`, and colocated tests. Each domain module must be deterministic and renderer-independent; the app module is the only browser/UI coordinator.
- Use a distinct IndexedDB database named `jomon-medieval-worlds-v1`. Keep a small world index separate from active-world records and finalized chronicles. Never open, read, migrate, delete, or rewrite `jomon-expedition-v2` as part of medieval bootstrapping.
- Define a foundation manifest now—seed, resolved foundation configuration, generator version, creation metadata, and chosen initial courier ID—so Phase 1.2 can extend it without changing its identity. Create the foundation household deterministically from this manifest; the richer configurable geography/history pipeline remains Phase 1.2 work.
- Implement a minimal keyboard-first medieval bootstrap only: list local worlds and chronicles, create a seeded foundation world, choose from its generated eligible crew, resume one selected world, or inspect/export a finalized chronicle. It is a state/persistence proof, not the walkable deck, trade loop, combat, map renderer, or content slice.
- Change `src/entry.ts` to bootstrap the medieval app. Do not route ordinary users through the prototype. Prototype-specific browser tests must be replaced only by medieval coverage as the corresponding new path becomes implemented; direct prototype unit tests remain isolated until separately retired.
- Do not share `src/main.ts`, `src/renderer.ts`, `src/storage.ts`, `src/types.ts`, prototype content, prototype game state, or prototype save keys with the new domain. A small generic browser canvas or DOM shell may be rebuilt from first principles; no UI surface may expose prototype language.
- Verify this slice with new unit tests for manifests, generated crew, initial selection, catalog isolation, and save ownership; browser coverage for the keyboard creation/select/resume flow; `npm run build`; and `git diff --check`. Record any temporarily skipped legacy browser coverage plainly rather than treating it as medieval evidence.

#### 1.1.a Prototype reuse and disposition audit `[x]`

- [x] Classify every prototype foundation before reuse. Reuse only neutral tooling and the bare browser mount; rebuild all game-facing systems in `src/medieval/` from first principles.
- [x] Retain the Vite, TypeScript, Vitest, Playwright, package-lock, and generic `index.html` canvas-mount infrastructure. These provide build, type-check, test, and local browser boot capabilities only; they confer no medieval gameplay or visual requirements.
- [x] Do not reuse the prototype’s `src/main.ts`, `src/renderer.ts`, `src/storage.ts`, `src/types.ts`, `src/rng.ts`, `src/engine/**`, `src/world.ts`, `src/content.ts`, `src/props.ts`, autoplay/telemetry/campaign code, legacy browser flows, save keys, migrations, source art, sprite atlases, audio, or CSS. The sole approved font exception is the unmodified, separately attributed BigBlue Terminal 437 Nerd Font Mono, consumed only by the medieval stylesheet under its CC BY-SA 4.0 terms.
- [x] Isolate the legacy browser application behind `?prototype` so existing prototype browser tests can remain diagnostic without making it the ordinary product path. New browser coverage must open `/` and exercise only the medieval application.

Verification: manual import/dependency audit on 2026-09-01. `src/medieval/` has no prototype domain imports; `src/entry.ts` selects the medieval bootstrap by default and legacy bootstrap only with `?prototype`.

Detailed disposition record (completed 2026-09-01):

| Existing codebase area | Disposition for medieval Jomon | Why |
| --- | --- | --- |
| `src/main.ts`, `src/renderer.ts`, root UI state and `src/style.css` | Do not reuse | They encode the prototype's space-era screen structure, vocabulary, font, and visual identity. |
| `src/engine/**`, `src/world.ts`, `src/content.ts`, `src/props.ts`, `src/area-gates.ts`, `src/events.ts`, and adjacent progression/economy/escalation modules | Do not reuse | They encode the prototype's campaign, destinations, combat/content assumptions, and world model; importing them would create hidden medieval constraints. |
| `src/storage.ts`, root types/RNG/geometry/input/shared modules, and all save migrations | Do not reuse | Medieval persistence has a new database/schema and deterministic contracts; it must never load or rewrite `jomon-expedition-v2`. |
| `src/assets/**`, generated sprites, `src/audio.ts`, and legacy CSS | Do not reuse | They are prototype presentation/assets or carry an inherited visual/licensing identity; medieval gets original code-native ASCII presentation first. |
| `public/fonts/BigBlueTerm437NerdFontMono-Regular.ttf` | Reuse as a licensed exception | The user selected this local font as the medieval default. `src/medieval/style.css` imports it without changing the binary; `public/fonts/BigBlueTerm-NOTICE.txt` gives VileR and Nerd Fonts attribution, and the bundled CC BY-SA 4.0 license remains alongside it. |
| `src/autoplay*`, telemetry, campaign/balance reports, content validators, and product-specific scripts | Preserve only as legacy diagnostics; do not repurpose | Their metrics and fixtures assert a different game. Medieval gains its own focused fixtures as its systems exist. |
| `e2e/jomon.spec.ts` and legacy unit tests | Preserve behind `?prototype`; do not count as medieval verification | They protect unrelated existing work while the ordinary route is rebuilt. New coverage lives in `e2e/medieval-foundation.spec.ts` and `src/medieval/*.test.ts`. |
| Vite, TypeScript, Vitest, Playwright, package-lock, `vite.config.ts`, `playwright.config.ts`, and generic `index.html`/`#game` mount | Reuse | They are neutral local build, type-check, test, and canvas-boot infrastructure only. `src/vite-env.d.ts` restores the standard Vite CSS-module declaration after the split. |

Verification: on 2026-09-01, `npx vitest run src/medieval/world.test.ts src/medieval/storage.test.ts src/medieval/session.test.ts --maxWorkers=1 --no-file-parallelism` passed 10 tests; `npx playwright test e2e/medieval-foundation.spec.ts` passed 1 Chromium browser test; `npm run build` and `git diff --check` passed. Playwright Chromium, its headless shell, and its FFmpeg support were installed in the user Playwright cache before the browser run. npm reported an existing unsupported `email` configuration warning; no verification command failed.

Licensed terminal-font and wrapping correction (2026-09-01): the medieval stylesheet now registers the local BigBlue Terminal 437 Nerd Font Mono file as `BigBlueTerm`, and the canvas explicitly draws with that family after the browser font is ready. Canvas text uses measured, word-aware wrapping inside the panel, including long seed text, generated histories, causal records, errors, and explanatory copy; a long unbroken token is split only when necessary. The source font is VileR’s BigBlue Terminal, distributed as CC BY-SA 4.0 and patched by Nerd Fonts. The project retains the full license at `public/fonts/BigBlueTerm-LICENSE.txt` and adds the required attribution, source links, license link, and modification status at `public/fonts/BigBlueTerm-NOTICE.txt`; the bundled binary is unmodified.

Verification for the licensed font and wrapping correction: on 2026-09-01, `npm exec vitest -- run src/medieval/content-safety.test.ts src/medieval/world.test.ts src/medieval/storage.test.ts src/medieval/session.test.ts src/medieval/generation-config.test.ts` passed 28 tests; `npm run build`, `npx playwright test e2e/medieval-foundation.spec.ts` (1 Chromium test, including the BigBlueTerm readiness assertion), and `git diff --check` passed. A 2048×982 Chromium screenshot was manually inspected and confirms the roadmap line wraps inside the canvas panel. npm reported its existing unsupported `email` configuration warning, and Playwright reported the existing `NO_COLOR`/`FORCE_COLOR` environment warning; neither command failed.

#### 1.2 Deterministic world generation and configuration

- [x] Define a versioned `WorldGenerationConfig`, named presets, advanced settings, validation constraints, and deterministic rejection/retry rules. `src/medieval/generation-config.ts` provides pure resolution, rejection diagnostics, and a four-attempt deterministic candidate plan with focused coverage.
- [x] Define controls for region size, history length, climate, terrain and waterways, settlement and population density, political fragmentation, resource scarcity, ecology, dangers, era pace, and simulation fidelity; preserve every selected and resolved value in the manifest. `WorldManifest` v5 retains canonical selected settings, their full resolution, normalized seed, versioned generator/policy/frontier contracts, deterministic candidate streams and diagnostics, initial-world/frontier identities and digests, and content-safety provenance.
- [x] Define an enforceable content-boundary taxonomy and deterministic validation/rejection rules that exclude sexual violence, slavery, torture, and harm/endangerment of children from all generated world history and content. `src/medieval/content-safety.ts` supplies policy v1, canonical structured classifications, stable rejection codes, and fail-closed audit validation; foundation creation and local save validation apply it to generated labels, Jomon, initial-world records, people, histories, events, and player-facing text.
- [x] Build the dependency-ordered initial-world pipeline: watershed geography and hydrology; climate and seasons; resources and ecology; settlement sites; institutions and people; then routes, trade, and pre-play history. `src/medieval/initial-world.ts` now creates a bounded, renderer-independent foundation-region graph in that exact order and accepts only the first valid candidate from the existing four-attempt plan.

Implementation design for the completed initial-world pipeline:

- `src/medieval/initial-world.ts` owns immutable initial-world records, the ordered candidate stages, validation, safety-record collection, and bounded candidate selection; `world.ts` owns foundation assembly/recreation, `types.ts` owns manifest/world references, and `storage.ts` accepts only a structurally valid world whose initial-world output and manifest diagnostics reproduce exactly.
- The generated record graph is watershed/waterways → climate/seasons → resources/ecologies → settlements → institutions and person seeds → routes/trade/history. Records use stable `initial:` IDs; downstream fields are IDs only, and the validator rejects missing, duplicate, out-of-stage, or non-viable references. Person seeds are generation/history participants only, not Phase 1.3 mutable people.
- Each retry candidate starts from the existing `GenerationAttempt.streamSeed`; every stage creates its own named `SeededRng` stream from that value. Stages receive only resolved configuration, explicit upstream records, and that stream. No stage reads time, network, globals, or another stage’s RNG state.
- `InitialWorldGenerationDiagnostics` records each attempted candidate’s stream, ordered validation issues, and selected attempt. The pipeline selects the first valid member of the four existing attempts; a valid configuration whose four candidates fail throws a bounded exhaustion error carrying those diagnostics. The successful diagnostics and initial-world output are reproduced from the manifest.
- Every resolved configuration field has a direct output effect: region span, history horizon, climate/season profile, relief, waterways, settlement count, population band, institutional plurality, resource availability, ecology variety, route pressures, seasonal pace, and static generation detail respectively. Per-stage limits bound all collections.
- Every generated named or player-visible record supplies the existing content-safety classification. Candidate validation gathers those records through the policy validator and rejects missing, unsafe, or unclassified content before it reaches a world or local save.
- Focused tests cover deterministic reproduction, stage/reference causality, each configuration effect, first-valid/rejection/exhaustion behaviour, safety rejection, bounds, manifest recreation, and local-record invalidation. No browser surface, frontier, action clock, simulation, map, or trade gameplay is part of this slice.

- [x] Define the deterministic expanding-frontier contract: regional coordinates, persistent identities, pre-arrival knowledge/rumours, causal links to known regions, region generation order, regional commitments before materialization, named-person instantiation, and no-contradiction guarantees. `src/medieval/frontier.ts` now provides the pure, bounded contract and validation layer; it does not introduce travel, simulation, map rendering, or a save-schema change.

Implementation design for the completed expanding-frontier contract:

- `src/medieval/frontier.ts` owns the versioned, renderer-independent frontier state, coordinate/identity helpers, regional-commitment and materialization planners, knowledge reducers, no-contradiction validator, and content-safety record collection. It consumes `InitialWorld` and resolved configuration as immutable inputs without changing the v4 world manifest or storage schema; the following manifest task chooses its durable envelope.
- A region has an integer basin coordinate and a stable ID derived from the initial-world ID plus that coordinate. It is committed in a total generation order and has either an initial-world causal anchor or an earlier parent-region connection. Initial anchors must cite real watershed, waterway, settlement, route, trade, climate, ecology, and history IDs; child commitments inherit that grounded link and may not be disconnected biomes.
- `ungenerated` regions contain only bounded anonymous population/role/institution commitments and connection metadata. `known-but-unvisited` additionally retains immutable source-labelled facts with supplied world-time/freshness fields and optional named-person commitments. `materialized` retains a deterministic static site/materialization plan and stable future-person instantiation plans, but creates neither a map nor Phase 1.3 mutable people, simulation records, travel time, or routes.
- Each commitment, name, fact, rumour source, anonymous template, and materialized record carries the existing structured content-safety classification. Validation is fail-closed and canonically ordered: it rejects unknown/missing classifications, unsafe policy metadata, duplicate identities, invalid anchors, invalid parent/order links, over-budget state, malformed source timing, and a second claim with a different value for the same subject/fact key.
- Frontier creation and each derived commitment/materialization use named `SeededRng` streams based only on the normalized seed, resolved configuration fingerprint, initial-world ID, coordinate, and operation key. Knowledge application is a pure immutable reducer: it validates before accepting a fact, is idempotent for an identical stable fact ID, and leaves all time advancement to a later action-clock caller.
- Focused tests cover coordinate and ID stability; rooted causal links; deterministic materialization; immutable pre-arrival knowledge; name/future-person stability; contradiction and safety rejection; and commitment/fact/person bounds. Browser UI, font/CSS, map rendering, actual travel, persistent person simulation, and IndexedDB manifest persistence are outside this slice.

- [x] Record a reproducible world manifest containing seed, resolved configuration, generator version, initial and frontier region identifiers, and validation/rejection history. `WorldManifest` v5 is a canonical immutable creation record plus bounded mutable courier state; it reconstructs and validates the initial-world and frontier-root evidence before local storage accepts it.

Implementation design for the completed reproducible-manifest contract:

- `types.ts` will make manifest v5 an immutable `creation` provenance record plus the existing bounded mutable courier-selection field. `world.ts` will create, canonicalize, validate, digest, and reconstruct that record; `storage.ts` will continue to accept only a fully reproducible medieval world. `initial-world.ts`, `frontier.ts`, and `content-safety.ts` remain the owners of their domain contracts and supply only their public deterministic data.
- Creation provenance will canonically retain the normalized seed; selected/resolved configuration and fingerprint; foundation, initial-world, frontier, and safety-policy versions; the exact four candidate streams and ordered candidate diagnostics; an initial-world digest and bounded stable-ID index; and a bounded frontier-root commitment index/digest. It intentionally records neither a mutable frontier state, latent people, nor player discoveries.
- The manifest builder will derive the initial world and frontier roots from the recorded seed/configuration, audit their classified content, and calculate deterministic digests over fixed-shape, canonically ordered data. Reconstruction will consume the nested provenance itself, regenerate the same records, and reject changed versions, diagnostics, ordering, IDs, references, or digest/audit data before a world is restored.
- A v4-or-earlier medieval manifest is a clean-break incompatibility: it lacks the frontier-root and canonical creation evidence required here, so storage will reject it rather than infer, migrate, or touch any prototype save. The database namespace and outer world envelope remain medieval-only.
- Focused tests will cover canonical serialization, immutable reconstruction equivalence, initial/frontier identity stability, candidate rejection-history integrity, malformed/tampered provenance rejection, and medieval-storage isolation. No UI, travel, simulation, mutable frontier exploration, or map work is part of this slice.

Completed reproducible-manifest decision (2026-09-01): `WorldManifest` v5 separates immutable `creation` provenance from the bounded mutable `initialCourierId` and its current-state safety audit. Creation canonically records the normalized seed, exact selected/resolved configuration and fingerprint, foundation/initial-world/frontier/content-policy versions, the four retry streams and ordered initial-world candidate diagnostics, a full bounded initial-world stable-ID index and reproducibility digest, plus a bounded frontier-root commitment index and digest. The roots retain coordinates, order, streams, anchors, and connections, but retain neither materialized regions, named people, nor discovered facts.

Creation generates and audits the initial world and frontier roots before deriving both the creation digest and foundation-world ID; reconstruction consumes that record to regenerate and compare every canonical field. Validation rejects changed contract versions or safety provenance, malformed/reordered diagnostics, altered digests, noncanonical root order, and initial/frontier identity mismatches. v4 and older medieval manifests are intentionally rejected without migration because they cannot evidence this frontier contract; the medieval-only IndexedDB namespace and outer foundation-world envelope remain separate from every prototype save.

Verification for the completed reproducible-manifest contract: on 2026-09-01, `npx vitest run src/medieval/*.test.ts --maxWorkers=1 --no-file-parallelism` passed 49 tests in 8 files. `npm run build` passed (Vite transformed 149 modules and the bundle-size check passed), and `git diff --check` passed. npm emitted the existing unsupported `email` configuration warning; no required final command failed. Playwright was not run because this slice preserves browser-visible behaviour.

#### 1.2.a Dwarf Fortress Classic-inspired sixteen-colour palette `[x]`

This intermediate visual task deliberately precedes the settings UI so every forthcoming creation/control surface shares one small, testable palette contract. It implements only colour and colour-dependent legibility—not a map, glyph vocabulary, settings screen, world-generation behaviour, or detailed renderer.

- [x] Define a versioned, renderer-independent palette module with the sixteen historical Dwarf Fortress Classic console colour values and dark/bright pairs: black/dark gray; blue/light blue; green/light green; cyan/light cyan; red/light red; magenta/light magenta; brown/yellow; and light gray/white. Use [the Dwarf Fortress Classic default scheme](https://dwarffortresswiki.org/index.php/Color_scheme) as the value reference, while Jomon owns its colour-role names and assignments.
- [x] Replace all current medieval canvas and stylesheet colour literals with semantic Jomon palette tokens. The foundation screen uses only the approved sixteen colours for its background, panel, border, body text, muted information, selected entry, action prompt, error state, and reserved future water/route/status roles.
- [x] Preserve non-colour readability: selection, urgency, and state retain textual, glyph, or layout cues; no consequential current or future terminal state is distinguished by colour alone. Contrast and dark/bright pairing remain deliberate on the black console ground.
- [x] Add focused unit tests for exact palette membership/pairing, semantic-token completeness, and non-colour status cues. Add browser coverage and a manually inspected screenshot of the foundation screen confirming the actual rendered palette and readable wrapping at the existing reference viewport.
- [x] Document the palette as a Jomon presentation contract and a Dwarf Fortress Classic visual influence only. Do not copy Dwarf Fortress glyphs, layout, UI vocabulary, data, art, or gameplay semantics, and do not add a player palette-customisation UI in this task.

Completed sixteen-colour palette decision (2026-09-01): `src/medieval/palette.ts` owns palette v1 as serializable RGB/hex data for the historical pre-v50 default values and their eight ordered pairs. `JOMON_PALETTE` maps that finite set to original Jomon console-ground, panel, text, selection, action, warning/error, water, route, and status roles; `JOMON_NON_COLOR_STATE_CUES` specifies the accompanying marker/labels. `main.ts` applies generated CSS custom properties from the same module, while `app.ts` and `style.css` consume semantic tokens only. BigBlue Terminal, its attribution/notice, keyboard behaviour, wrapping, and canvas dimensions were unchanged.

Verification for the completed sixteen-colour palette: on 2026-09-01, `npx vitest run src/medieval/*.test.ts --maxWorkers=1 --no-file-parallelism` passed 53 tests in 9 files; `npm run build` passed (Vite transformed 150 modules and the bundle-size check passed); `npx playwright test e2e/medieval-foundation.spec.ts` passed 1 Chromium test including font readiness, palette CSS variables, and a black canvas sample; and `git diff --check` passed. `npx playwright screenshot --viewport-size="2048,982" --wait-for-selector="#game" --wait-for-timeout=500 http://127.0.0.1:4173 /tmp/jomon-medieval-palette-2048x982.png` captured a temporary screenshot that was manually inspected: the black ground, gray rules/border, yellow headings, green actions, and wrapped copy are legible. A duplicate `npm run preview -- --host 127.0.0.1 --port 4173 --strictPort` could not start because the already-running local preview owned port 4173; it did not block the browser or screenshot verification. npm emitted the existing unsupported `email` warning and Playwright emitted the existing `NO_COLOR`/`FORCE_COLOR` warning; no required check failed. No screenshot artefact was added to the repository.

Acceptance: the playable medieval foundation route visibly uses the coherent sixteen-colour console scheme; every rendered medieval colour comes from the palette contract; selection and error state remain readable without colour; automated/browser checks and a visual inspection are recorded.

#### 1.2.b Settings UI and generation diagnostics

- [x] Add settings UI for preset selection, advanced configuration, seed entry/display, world-creation progress, result inspection, and saving/loading settings.

Implementation design for the active settings UI and generation-diagnostics slice:

- `src/medieval/settings.ts` will own versioned, serializable creation-setting drafts, normalized seeds, named-profile validation, and the six-profile cap. It delegates every preset/advanced resolution and rejection to `generation-config.ts`; it owns no parallel configuration rules. `storage.ts` will add one medieval-only `creation-settings` record in a version-2 upgrade of the existing medieval database, reject malformed records to defaults, and replace an existing canonical profile name in place while refusing a seventh distinct profile.
- The keyboard canvas will use settings, profile, generation-progress, and result-inspection routes before the existing courier-choice route. The basic page edits seed/preset and opens advanced/profile pages; the advanced page exposes every configured field through options exported by `generation-config.ts`; the profile page saves/loads exact drafts. Seed normalization is displayed explicitly, invalid selections show the resolver diagnostics and cannot create a world, and loaded/saved setting data never mutates an existing world or its manifest.
- Initial-world generation will expose a synchronous, deterministic trace for the actual six causal stages plus validation and accepted/rejected candidate outcome. Creation records those events while it executes the generator, then presents a progress ledger followed by paged result inspection of seed, selected/resolved configuration, candidate diagnostics, bounded initial-world counts, immutable manifest IDs/digests, and the generated crew. The trace schedules no timer, uses no new RNG, and advances no world time.
- The palette module remains the sole colour authority. Each setting selection has the existing `>` marker, action key text, explicit validity/error labels, and descriptive canvas aria text, so state never depends on colour. BigBlue Terminal, wrapping, canvas size, prototype isolation, and existing world/courier flow remain intact.
- Focused tests will cover setting resolution and immutable-manifest separation; local last-used/profile persistence, invalid-record rejection, cap, and replacement; actual stage trace; and a keyboard browser path that changes a preset/advanced field, saves and reloads a profile, generates/inspects a world, selects a courier, resumes it, and observes no external request.
- [x] Add generation snapshots and property/fuzz tests for determinism, valid geography-to-settlement dependencies, constrained settings, content-boundary rejection, expanding-frontier continuity, bounded generation, and readable diagnostics.

Completed generation snapshots and property/fuzz testing decision (2026-09-01): `generation-snapshots.test.ts` fixes two intentionally small, readable projections: `snapshot-reed-17` with the watershed preset and `snapshot-coast-43` with documented far-coast overrides. Each projection records the normalized seed, world/manifest identifiers, selected configuration, accepted candidate and issue codes, bounded stage counts, representative settlement/water/resource/ecology and route/history edges, frontier region coordinates/connections plus the chart fact, and two generated crew records. They are direct in-test fixtures rather than opaque serialized-world dumps, so a change reports a meaningful contract difference.

`generation-properties.test.ts` uses installed `fast-check` with deterministic seeds `20260901`–`20260906`, verbose shrinking, and bounded ordinary-development run counts: 24 world/manifest/frontier reproductions, 48 valid all-control configurations, 64 malformed/incompatible configurations, 24 safety injections, 32 frontier cases, and 20 forced retry exhaustions. It covers initial-world graph/budget invariants across every preset and two representative advanced extremes; exact repeated manifest/frontier reconstruction; closed invalid configuration diagnostics; accepted content audits plus prohibited/unclassified rejection; coordinate/identity/anchor stability, idempotent knowledge, materialization, and contradiction rejection; and four-attempt canonical exhaustion diagnostics. A malformed resource correctly reports both its own invalid record and any downstream invalid settlement, so the exhaustion assertion requires stable bounded causal diagnostics instead of hiding that dependency.

No production contract changed: the existing resolver, generator, manifest, content-safety policy, and frontier functions already supplied the necessary public invariants. The browser UI, font, palette, local storage, and prototype boundary were not changed.

Verification for the completed generation-test slice: on 2026-09-01, the initial `npx vitest run src/medieval/generation-snapshots.test.ts src/medieval/generation-properties.test.ts --maxWorkers=1 --no-file-parallelism` execution failed only because the new exhaustion assertion incorrectly expected one diagnostic rather than the validator’s deliberate resource-plus-dependent-settlement result; the assertion was corrected without changing production code. The same focused command then passed 8 tests in 2 files, and the required `npx vitest run src/medieval/*.test.ts --maxWorkers=1 --no-file-parallelism` passed 69 tests in 12 files. `npm run build` passed (Vite transformed 151 modules and the bundle-size check passed), and `git diff --check` passed. npm emitted the existing unsupported `email` configuration warning; all final required checks passed. Playwright was not run because this slice makes no browser-visible production change.

Completed settings UI and generation-diagnostics decision (2026-09-01): `settings.ts` owns v1 canonical local creation drafts (normalized seed, selected generation configuration, and advanced-mode preference), profile validation, and a six-profile cap; `generation-config.ts` remains the only resolver and source of selectable advanced values. The canvas basic settings page offers seed entry with explicit normalized display, preset cycling, advanced/settings-profile routes, and creation. Advanced settings exposes all thirteen resolved controls—region size, history years, climate, terrain ruggedness, waterway density, settlement and population density, political fragmentation, scarcity, ecology, danger, era pace, and simulation fidelity—with `*` marking explicit overrides and `R` returning a field to its preset value. Resolver diagnostics prevent creation rather than silently repairing invalid combinations.

The medieval-only IndexedDB database now uses a version-2 upgrade with one `creation-settings` record, separate from catalogues, worlds, and chronicles and never from a prototype namespace. It loads a malformed record as the documented default instead of interpreting it; it records the last successfully saved/created draft; and it permits at most six named profiles of 1–32 normalized characters. A matching profile name replaces its existing position, while a seventh distinct name is rejected. Loading or changing settings never changes any existing immutable world manifest; creating a new world passes the canonical selected settings through the existing resolver and manifest provenance contract.

The creation route records an actual deterministic initial-world trace: the six generator stages (watershed/hydrology, climate/seasons, resources/ecology, settlements, institutions/people, routes/trade/history) followed by candidate validation and its accepted/rejected result. It uses no timer, unseeded work, or world-time increment. After the trace, result pages expose the normalized seed, selected preset/overrides and full resolved configuration, candidate diagnostics, initial-region counts, immutable world/initial/frontier identifiers and digests, policy/contract versions, and generated crew before the existing courier confirmation. Keyboard focus, `>` selection, `+` readiness, explicit labels/errors, canvas aria text, measured wrapping, BigBlue Terminal attribution/setup, and semantic palette tokens remain intact.

Verification for the completed settings UI and generation diagnostics: on 2026-09-01, `npx vitest run src/medieval/*.test.ts --maxWorkers=1 --no-file-parallelism` passed 61 tests in 10 files; `npx playwright test e2e/medieval-foundation.spec.ts --project=chromium` passed 1 Chromium keyboard-flow test covering preset/advanced edits, seed normalization, profile save/load, creation progress/result inspection, courier selection, page reload/resume, palette/font readiness, and no external requests; `npm run build` passed (Vite transformed 151 modules and the bundle-size check passed); and `git diff --check` passed. A fresh 1280×720 temporary screenshot at `/tmp/jomon-medieval-settings-final.png` was manually inspected: the advanced configuration fits the fixed canvas, uses readable wrapping/cues, and retains the approved black/gray/yellow/cyan/green console presentation. No screenshot artefact was added. A separate `npm run preview -- --host 127.0.0.1 --port 4173 --strictPort` invocation could not bind because an already-running local preview held port 4173; it did not block Chromium or screenshot verification. npm emitted the existing unsupported `email` configuration warning and Playwright emitted the existing `NO_COLOR`/`FORCE_COLOR` warning; no required command failed.

Acceptance: the same seed and resolved configuration reproduce the same initial world and explored frontier; changing a documented setting predictably changes the intended world property; prohibited content is absent from generated output; a player can inspect, share, and restore the world manifest.

Completed expanding-frontier contract decision (2026-09-01): `frontier.ts` defines contract v1 as a pure state machine keyed by a normalized seed, resolved-configuration fingerprint, and initial-world ID. Integer basin coordinates deterministically derive stable region IDs and named streams. Root and child commitments have a total, contiguous generation order; roots cite real initial watershed, waterway, settlement, route, trade, climate, ecology, and history records, while children inherit that anchor and cite an earlier parent connection. Three initial commitments demonstrate upstream, chart-known coastward, and parent-linked coastward expansion without disconnected biomes; subsequent adjacent commitments remain capped at 12.

An `ungenerated` region has only anonymous adult household, role, institution, anchor, and connection commitments. A `known-but-unvisited` region adds immutable source-labelled facts whose reported, freshness, and known world times are supplied by a future action-clock caller; a `materialized` region adds a deterministic static site and future-person instantiation plans only. The contract creates no mutable people, no world-time updates, no travel routes, no map, and no persistence writes. A revealed named person has a seed/region/revelation-key-derived commitment ID and future persistent-person ID; its immutable name fact, role, anonymous institution, and `region-materialized` trigger are reproduced exactly. Site, route, history, person-role, and person-relationship facts use stable subject IDs and cannot change a prior subject/fact value.

Validation rejects malformed context, noncanonical identity/order/anchor/connection links, budget excess, invalid fact subjects or source timing, changed claims, invalid name/materialization plans, and all unclassified or unsafe content. It uses the existing policy audit for regional data, anonymous templates, source labels, facts/rumours/history links, names, and materialized plans. The state reducer is immutable and idempotent for an identical fact or name revelation; materialization has a named stream independent of its recorded timestamp so equivalent source world/facts always produce the same static region details. The v4 manifest and local storage remain unchanged until the newly active manifest-record task determines a durable frontier envelope. The BigBlue Terminal font, attribution, wrapping correction, and browser path were untouched.

Verification for the completed expanding-frontier contract: on 2026-09-01, `npx vitest run src/medieval/frontier.test.ts src/medieval/initial-world.test.ts src/medieval/world.test.ts src/medieval/storage.test.ts src/medieval/content-safety.test.ts src/medieval/generation-config.test.ts src/medieval/session.test.ts --maxWorkers=1 --no-file-parallelism` passed 42 tests in 7 files. `npm run build` passed (Vite transformed 148 modules and the bundle-size check passed), and `git diff --check` passed. npm emitted the existing unsupported `email` configuration warning; no required command failed. Playwright was not run because this slice changes no browser-visible behaviour.

Completed initial-world pipeline decision (2026-09-01): `src/medieval/initial-world.ts` owns the immutable, versioned initial-region graph, six ordered stage functions, candidate validation, policy-record collection, and bounded candidate selection. `world.ts` assembles and recreates the foundation world; `types.ts` owns the v4 manifest/world references; `storage.ts` rejects records whose initial region or generation provenance cannot be reproduced exactly. The graph uses stable `initial:` IDs and validated references from watershed/waterways through climate/seasons, resources/ecologies, viable settlements, institutions and non-mutable person seeds, routes/trade links/hazards, and history events. Person seeds are generation/history participants only; Phase 1.3 persistent people, fidelity scheduling, and the action clock remain unimplemented.

Every candidate stage receives only the resolved configuration, explicit upstream records, and its own named `SeededRng` stream derived from the existing attempt stream. The successful pipeline records diagnostics v1: its generator and content-policy versions, selected attempt, and ordered attempted-candidate diagnostics. It rejects duplicate IDs, invalid stage references, non-viable settlement water/resource links, out-of-bounds output, configuration mismatches, and missing/unsafe/unclassified content. The generator selects the first valid candidate; bounded exhaustion returns all four recorded rejections without altering configuration or drawing non-seeded entropy. The v4 clean-break manifest persists these diagnostics while the existing safety audit persists the classified initial-world records; local saves additionally require the actual initial-world output to match the manifest reconstruction.

The resolved controls directly set region span, history horizon/event count, climate profile, relief, waterway count, settlement count, population band/person-seed count, institutional plurality, resource count/availability, ecology count, route-hazard pressure, seasonal pace, and static generation detail. Foundation caps are 7 waterways/resources, 4 seasons, 5 ecologies/routes/trade links/route hazards, 6 settlements, 30 institutions, 60 person seeds, and 8 history events. No browser surface, expanding frontier, simulation, map, trade gameplay, or settings UI was changed; the existing BigBlue Terminal font attribution and wrapping correction remain untouched.

Verification for the completed initial-world pipeline: on 2026-09-01, `npx vitest run src/medieval/initial-world.test.ts src/medieval/world.test.ts src/medieval/storage.test.ts src/medieval/content-safety.test.ts src/medieval/generation-config.test.ts --maxWorkers=1 --no-file-parallelism` passed 34 tests, and the final `npx vitest run src/medieval/*.test.ts --maxWorkers=1 --no-file-parallelism` passed 35 tests in 6 files. `npm run build` passed (Vite transformed 148 modules and the bundle-size check passed), and `git diff --check` passed. npm emitted the existing unsupported `email` configuration warning; no required command failed. Playwright was not run because this slice changes no browser-visible behaviour.

Completed configuration-contract decision (2026-09-01): `WorldGenerationConfig` v1 resolves one named preset plus explicit advanced overrides before any generation work. Its current preset defaults are:

| Preset | Region / history / climate | Terrain / waterway | Settlement / population | Fragmentation / scarcity / ecology / danger | Era / fidelity |
| --- | --- | --- | --- | --- | --- |
| `sheltered-reach` | `compact` / 200 years / `temperate` | 2 / 4 | 3 / 2 | 2 / 2 / 3 / 2 | `measured` / `focused` |
| `watershed` (default) | `standard` / 300 years / `temperate` | 3 / 4 | 3 / 3 | 3 / 3 / 3 / 3 | `measured` / `balanced` |
| `far-coast` | `broad` / 400 years / `cool-wet` | 4 / 3 | 3 / 3 | 4 / 4 / 4 / 4 | `brisk` / `deep` |

`regionSize` is `compact`, `standard`, or `broad`; `historyYears` is an integer from 100 through 600 in 25-year steps; `climate` is `cool-wet`, `temperate`, or `warm-dry`; `eraPace` is `measured`, `brisk`, or `pressing`; and `simulationFidelity` is `focused`, `balanced`, or `deep`. Terrain ruggedness, waterway density, settlement density, population density, political fragmentation, resource scarcity, ecology complexity, and danger pressure are integer levels 1–5. Inputs are never coerced or silently repaired: unknown/malformed settings and incompatible combinations are rejected with field diagnostics. Population density may be at most one level above settlement density, and a broad region requires at least 200 history years. Future generators must evaluate exactly four seed/config-derived candidates in attempt order 0–3, select the first valid result, and report bounded exhaustion if all are rejected; retries cannot alter the configuration or use non-seeded entropy.

Verification for the completed configuration contract: on 2026-09-01, `npx vitest run src/medieval/world.test.ts src/medieval/storage.test.ts src/medieval/session.test.ts src/medieval/generation-config.test.ts --maxWorkers=1 --no-file-parallelism` passed 15 tests; `npx playwright test e2e/medieval-foundation.spec.ts` passed 1 Chromium browser test; and `npm run build` passed. npm reported the existing unsupported `email` configuration warning, but no command required by this medieval slice failed. The active manifest-preservation task has not changed the foundation manifest or creation UI yet.

Broader diagnostic verification on 2026-09-01: legacy `npm run test:autoplay:tasks` passed. Full `npm run test:e2e` ran 9 tests: the medieval foundation test and three legacy diagnostics passed, while five legacy `e2e/jomon.spec.ts` flows failed after their prototype route remained `loading` instead of reaching `approach` within 30 seconds. These legacy failures do not invalidate the completed medieval browser test and are not medieval feature evidence. `npm test` was started but its legacy Vitest matrix remained idle for more than six minutes in one serial invocation (0% CPU, no further output), so that diagnostic run was terminated and is recorded as incomplete rather than passing.

Completed manifest-provenance decision (2026-09-01): the manifest schema began independently versioned at v3 and is now v4 for the verified initial-world pipeline, while the clean medieval IndexedDB namespace and outer foundation-world envelope remain unchanged. Creation resolves the requested configuration before it creates a manifest, derives the world ID, label, foundation-household RNG stream, and initial-world candidates from that resolved configuration, and records both the canonical selection and exact resolved values. A persisted manifest is accepted only when its selection resolves to its recorded configuration, its accepted empty validation record and four retry streams match the normalized seed/configuration, its label and initial-world candidate diagnostics match, its policy audit matches the generated classified records, and its world ID matches; otherwise it is unavailable. This is a fail-closed replacement for incomplete earlier medieval manifest records, not a migration, and it never opens or changes any prototype save. `recreateFoundationWorld` rebuilds the same zero-time world from a valid manifest's normalized seed and selection.

Verification for the completed manifest-provenance contract: on 2026-09-01, `npx vitest run src/medieval/generation-config.test.ts src/medieval/world.test.ts src/medieval/storage.test.ts --maxWorkers=1 --no-file-parallelism` passed 18 tests; `npm run build` and `git diff --check` passed. The browser test was not rerun because this slice does not alter a browser UI path; its completed Phase 1.1 Chromium result remains recorded above. npm reported the existing unsupported `email` configuration warning, but no command required for this slice failed.

Completed content-boundary decision (2026-09-01): `src/medieval/content-safety.ts` defines medieval content-safety policy v1. Every generated record passed to the policy supplies a primary domain plus a canonical structured classification: the classified domains, adult-only or not-applicable participant scope, approved descriptive tags, and an exact exclusion entry for sexual violence, slavery, torture, and child harm or endangerment. Missing, unknown, incomplete, or noncanonical classification data rejects with stable codes; no keyword blacklist is treated as the authority. The taxonomy explicitly covers history, people, places, events, contracts, hazards, rumours, enemy behaviour, simulation summaries, player-facing text, and future templates/data. `WorldManifest` v4 persists an accepted policy audit (including policy version and classified-record snapshots). Foundation world creation creates and audits classified label, Jomon, crew, initial-world records, history, and causal-event records deterministically; save/load validation rejects an altered record or audit, and manifest recreation requires the same provenance snapshot. This does not migrate or open prototype storage.

Verification for the completed content-boundary contract: on 2026-09-01, `npm exec vitest -- run src/medieval/content-safety.test.ts src/medieval/world.test.ts src/medieval/storage.test.ts src/medieval/generation-config.test.ts` passed 27 tests; `npm run build` and `git diff --check` passed. Browser tests were not run because this slice does not alter browser-visible behaviour. npm reported the existing unsupported `email` configuration warning, but no required command failed.

#### 1.3 Active-play world simulation and persistence kernel

- [x] Define the action-driven simulation clock, event ordering, seeded random streams, and deterministic scheduler. Only state-changing in-world commands advance time; pure UI inspection and idle/paused/closed browser time must be zero-time.

Implementation design for the completed action-clock and scheduler slice:

- `src/medieval/temporal.ts` will own temporal contract v1: integer in-world minutes, a zero-time clock, action sequence, bounded pending-event queue, canonical due-event results/causal records, command validation, and pure state transitions. `world.ts` will bind that state to immutable creation provenance and append its causal records; `types.ts` will own the versioned mutable envelope; `storage.ts` will reject any temporal state that does not reproduce those contracts. No renderer, browser timer, or gameplay system owns simulation authority.
- Time-bearing action inputs are the closed future-compatible families `movement`, `wait`, `travel`, `work`, `rest`, `delegated-task-commitment`, and `delegated-task-resolution`. Each supplies a positive, safe integer minute duration within its documented family bound. Inspection, settings, routes, browser idle/pause/reload, and arbitrary UI inputs are explicit pure command kinds and are rejected by the advancing reducer with no partial mutation.
- A pending event has a stable ID, non-past due minute, closed priority, scheduler-assigned sequence, source-action/provenance payload, and content-safety classification. Due events sort strictly by due minute, priority, ID, then sequence; requested events are canonically sorted before sequences are assigned, so insertion order cannot diverge state. An action advances the clock once, then resolves every due event in that total order and emits bounded, inspectable causal results.
- Random values use fresh `SeededRng` instances from named temporal stream seeds derived only from temporal-contract version, immutable world ID/creation digest/normalized seed, stream name, and action or event ID. There is no mutable RNG state, wall clock, timer, network, or hidden global input. The initial temporal state has no pending events, causal records, or RNG state.
- Validation fails closed with stable diagnostics for malformed provenance/state/payload, pure commands, non-positive or out-of-bound duration, unsafe time or sequence overflow, invalid/noncanonical priority/order, duplicate action/event IDs, past scheduling, over-cap queue/history, and unsafe or unclassified action/event records. The queue cap is 32 events and the causal-record cap is 256; content safety audits cover both pending and resolved scheduler records.
- `FoundationWorld` will become mutable-envelope v2 while `WorldManifest` v5 remains unchanged immutable creation provenance. Creation and manifest reconstruction produce an exact empty temporal state; save/load preserves validated later temporal state and verifies that its causal projection matches the world history. Existing medieval v1 world/chronicle envelopes are rejected without migration because their lone zero-time field cannot prove action sequence, queue, event ordering, or scheduler safety data; prototype storage remains untouched.
- Focused tests will cover zero-time creation/reconstruction, explicit action advancement, pure-command/idle/reload invariants, named-stream and replay stability, order independence, invalid inputs, persisted pending-event equivalence, queue/time bounds, content-safety rejection, and the old-envelope clean break. No fidelity, distant simulation, people, era, travel, UI, map, trade, or combat behaviour is implemented here.

Completed action-clock and scheduler decision (2026-09-01): `src/medieval/temporal.ts` owns temporal contract v1. Its stable unit is the integer in-world minute; creation and manifest reconstruction start at minute zero with no actions, pending events, causal records, or mutable RNG cursor. The closed time-bearing families are movement (1–60 minutes), wait/work/rest (1–1,440), travel (1–10,080), and delegated-task commitment/resolution (1–720). A valid action explicitly supplies one bounded duration; `inspect`, settings open/close, route change, browser idle/pause/reload, and arbitrary UI commands are explicit pure kinds that the advancing reducer rejects without changing any temporal value.

Pending events retain a stable ID, due minute, `urgent`/`ordinary`/`deferred` priority, canonical scheduler sequence, source-action/creation-digest payload, and structured content-safety classification. Requested events sort before sequence assignment; all due events then resolve by due minute, priority, ID, sequence. The reducer advances one action and resolves every due event at the first qualifying action, recording deterministic random result values and readable causal records. It bounds pending events at 32 and scheduler causal records at 256, rejects unsafe integer overflow, and validates duplicate IDs, noncanonical order, past events, malformed payload/provenance, and unsafe/unclassified action or event data with stable diagnostics. A fresh named `SeededRng` stream is derived from temporal-contract version, immutable world ID, creation digest, normalized seed, named stream, and action/event ID; there is no wall-clock or mutable random state.

The mutable medieval `FoundationWorld` envelope is now v2 and persists this state plus its own content-safety audit; `WorldManifest` v5 remains immutable creation provenance. `world.ts` validates the temporal provenance against the manifest and projects scheduler records into causal history, while storage rejects a save whose projection, safety audit, queue, or ordering does not reproduce. v1 medieval world envelopes (and older chronicles containing them) are rejected without migration because their zero-time field cannot prove the action sequence, pending queue, ordering, or dynamic policy audit; no prototype namespace is opened or changed. The renderer/browser path, BigBlue Terminal setup, palette, and gameplay systems remain untouched.

Verification for the completed action-clock and scheduler slice: on 2026-09-01, `npx vitest run src/medieval/temporal.test.ts src/medieval/world.test.ts src/medieval/storage.test.ts src/medieval/world-manifest.test.ts --maxWorkers=1 --no-file-parallelism` passed 38 tests in 4 files; `npx vitest run src/medieval/*.test.ts --maxWorkers=1 --no-file-parallelism` passed 81 tests in 13 files; `npm run build` passed (Vite transformed 152 modules and the bundle-size check passed); and `git diff --check` passed. npm emitted the existing unsupported `email` configuration warning; no required final command failed. Playwright was not run because this renderer-independent slice changes no browser-visible behaviour.

- [x] Define versioned world state and persistence contracts for geography, sites, routes, markets, people, institutions, history, and Jomon.

Implementation design for the active versioned-world-state slice:

- `src/medieval/world-state.ts` will own mutable-world-state contract v1 and independently version its geography/frontier, sites/quays, routes/conditions, markets, people registry, institutions, history, Jomon, courier, and temporal subdomains. `types.ts` will keep the immutable foundation records and root envelope; `world.ts` will construct/reconstruct validated state from immutable provenance; and `storage.ts` will atomically persist only validated complete v3 world/ v2 chronicle envelopes in the existing medieval IndexedDB namespace.
- `WorldManifest` will advance to v6 and contain only immutable creation provenance. The mutable courier choice and current safety audit move into `MedievalWorldState`, alongside the temporal scheduler and causal-history projection. Initial-world data and frontier-root provenance remain immutable outer evidence; the mutable geography container retains only the bounded validated `FrontierState`, whose revealed/materialized facts are cross-checked against those roots.
- State records use stable existing IDs and sorted arrays, never maps or untyped extension bags: initial settlement/materialized-region sites reference an initial world or frontier region; routes reference sites and existing waterways; market placeholders reference sites; crew and initial person seeds enter a registry with explicit vessel/site residences; institutions reference sites; Jomon references its vessel and a site/quay location; history is the canonical foundation-plus-temporal causal projection. v1 placeholder commodity, capacity, and condition values are closed and deliberately minimal.
- Construction uses only validated manifest seed/configuration/initial world plus the deterministic frontier and temporal contracts. Validation is pure, bounded, canonical, fail-closed, and checks aggregate/subdomain versions, exact record shapes, lexical ordering, ID uniqueness, references, expected seeded records, temporal provenance, causal projection, and content-safety audits. Unknown versions, malformed/tampered values, and earlier medieval world/chronicle envelopes are rejected rather than migrated because their missing state cannot be exactly reconstructed; settings records remain independently compatible.
- Focused tests will cover canonical construction/order, reconstruction and atomic save/load equivalence, temporal persistence, cross-domain and duplicate/reference rejection, immutable-manifest separation, version/malformed rejection, settings survival, and medieval/prototype storage isolation. No UI, simulation, market, travel, map, or persistent-person behaviour is added.

Completed versioned-world-state decision (2026-09-01): `src/medieval/world-state.ts` owns aggregate mutable-state v1 and independently versioned geography/frontier, sites/quays, routes, markets, people, institutions, history, Jomon, and courier subdomains. It stores only bounded canonical arrays: initial-settlement and materialized-frontier sites; Jomon quays; seeded initial-route conditions; empty closed commodity placeholders; references to existing crew and initial person seeds; initial institutions; canonical foundation-plus-scheduler causal history; and Jomon’s moored location, integrity, and capacity values. No named ungenerated-frontier person becomes a mutable record, and no full person, market, travel, or simulation behaviour is implied.

`WorldManifest` is now immutable v6 creation evidence only. `FoundationWorld` v3 retains immutable Jomon, crew, and initial-world data, while courier selection, frontier facts/materialization, temporal scheduler state, mutable history, and the current content-safety audit live in `MedievalWorldState`; `WorldChronicle` is v2 because it embeds that world envelope. Construction and validation recompute seeded references, initial frontier roots, canonical causal projection, content-safety audits, and temporal provenance, reject unknown/malformed/substituted versions, unordered or duplicate IDs, broken references, and extension bags, and return no unsafe record to storage. v1/v2 medieval worlds, v1 chronicles, and v5 manifests are deliberately rejected without migration: their missing/mixed mutable-state boundary cannot be proven exactly equivalent. The IndexedDB database name and creation-settings record stay unchanged; world/catalog writes remain one transaction, load/save clone values, settings profiles survive, and no prototype namespace is opened.

Verification for the completed versioned-world-state slice: on 2026-09-01, `npx tsc --noEmit && npx vitest run src/medieval/world-state.test.ts src/medieval/world.test.ts src/medieval/world-manifest.test.ts src/medieval/temporal.test.ts src/medieval/storage.test.ts --maxWorkers=1 --no-file-parallelism` passed 43 tests in 5 files; `npx vitest run src/medieval/*.test.ts --maxWorkers=1 --no-file-parallelism` passed 86 tests in 14 files; `npm run build` passed (Vite transformed 153 modules and the bundle-size check passed); and `git diff --check` passed. npm emitted the existing unsupported `email` configuration warning; no required command failed. Playwright was not run because the schema-driven property-path change preserves browser-visible behaviour.

- [x] Define persistent individual records for every instantiated person: family, work, needs, relationships, injury, possessions, birth, death, location, memory, and commitments.

Implementation design for the active persistent-person slice:

- `src/medieval/persistent-person.ts` will own person contract v1, its bounded record types, deterministic foundation-crew instantiation, structured content records, and pure validation. `world-state.ts` will advance its aggregate and people subdomain versions, own the reference context and state audit, while `world.ts` and `storage.ts` will construct and accept only the new v4 world/v3 chronicle envelopes. The immutable v6 manifest and initial-world data remain untouched.
- Only the six generated Jomon crew become records now, using their existing crew IDs, names, roles, histories, relationships, conversation values, and equipment plus person-specific named RNG streams derived from immutable seed/configuration provenance. Initial-world person seeds and anonymous or merely named frontier commitments remain sources for bounded known-uninstantiated references, never entries in the mutable registry; a later materialization slice owns any additional instantiation.
- Each person record has adult life chronology and living/dead status; an explicit Jomon/site/future-region location; role, closed skills, capacity, assignment, and availability; needs, health, recovery, bounded injuries; owned possessions; directed reciprocal household relationships; family links to either an instantiated person or a clearly typed known-uninstantiated reference; classified memories; and typed commitments. Dead records persist but must be unavailable, unassigned, absent from courier selection, and have no active commitment.
- Every list is bounded and canonically ID-sorted. Validation rejects malformed/unknown versions and shapes, duplicate IDs, missing or non-adult birth data, incompatible life/death states, invalid locations/references/reciprocity/ownership, uninstantiated seed masquerading, active dead work, unsafe or unclassified names/memories/possessions/family facts, and a changed immutable crew source. The manifest reconstructs the same initial six records, while later valid mutable records remain save data rather than creation provenance.
- Focused tests will cover deterministic crew construction/reconstruction, complete-field and chronology/life validation, locations/work/ownership/family/relationship ordering, dead-person restrictions, seed/frontier non-instantiation, safety/malformed rejection, and isolated settings/world save-load continuity. No clock progression, autonomous simulation, fidelity, travel, delegation outcomes, map, tavern, trade, or combat behaviour is added.

Completed persistent-person decision (2026-09-01): `src/medieval/persistent-person.ts` owns persistent-person contract v1 and bounded, canonical records for the only currently instantiated people: Jomon’s six deterministic foundation crew. Each record retains the immutable crew ID, name, adult status, conversation value, role, generated history, directed reciprocal relationships, and equipment, then adds deterministic provenance-derived adult birth data, Jomon/site/future-region location, closed skills and capacity, assignment/availability, needs, health/injury/recovery, owned possessions, typed family links, memories, and commitments. A family link can refer either to an instantiated record or to a bounded, source-checked `known-uninstantiated-person` from an initial person seed or named frontier commitment; it never allocates that person in the mutable registry.

Aggregate mutable state is now v2 and its people subdomain is v2. Its safety audit covers all classified persistent-person text and records; validation rejects unknown shapes/versions, noncanonical or duplicate IDs, missing crew records, changed immutable crew evidence, non-adult or impossible life/death chronology, invalid site/frontier references, invalid ownership/reciprocity, unsafe or unclassified content, and active work, courier selection, or active commitments for dead people. Courier selection also requires the corresponding living, available persistent record. `FoundationWorld` is v4 and `WorldChronicle` is v3; v3/v1 mutable-world/person envelopes are intentionally rejected without migration because their person state cannot be exactly reconstructed. The immutable v6 manifest, deterministic action clock, medieval-only IndexedDB namespace, and independently compatible settings profiles remain separate and unchanged.

Verification for the completed persistent-person slice: on 2026-09-01, `npx tsc --noEmit && npx vitest run src/medieval/persistent-person.test.ts src/medieval/world-state.test.ts src/medieval/storage.test.ts` passed 26 tests in 3 files. The required `npx vitest run src/medieval/*.test.ts --maxWorkers=1 --no-file-parallelism` passed 97 tests in 16 files; `npm run build` passed (Vite transformed 154 modules and the bundle-size check passed); and `git diff --check` passed. npm emitted the existing unsupported `email` configuration warning; no required command failed. Playwright was not run because this renderer-independent schema slice makes no browser-visible change.

- [-] Define deterministic fidelity tiers for loaded places, nearby people, recurring agents, and distant individual/settlement summaries without discarding individual state.
- [ ] Define due-event catch-up for every fidelity tier on each time-bearing action so distant people, institutions, markets, and delegated work continue to advance deterministically while the courier is elsewhere.
- [ ] Ensure detailed and summary simulation cannot emit prohibited content or encode it as an undiscoverable background cause.
- [ ] Define a versioned world-era model for base, NG+, and NG++ states. Accumulated in-world active-play time and Jomon’s growth advance it; transition conditions, inspectable causes, persistence, and configuration hooks must be deterministic.
- [ ] Add event sourcing or an equivalent inspectable causal history so world changes can be explained, replayed, and persisted within bounded storage.
- [ ] Add focused tests for reload equivalence, simulation determinism, due-event catch-up, prohibited-content rejection in detailed and summary ticks, zero-time UI/idle/paused/closed sessions, corruption recovery, and bounded state growth.

Acceptance: equivalent active-play time produces the same world state and era across replay and reload; the world never advances while inactive; a visible change or escalation has an inspectable causal record.

#### 1.4 People, conversation, and delegated work foundation

- [ ] Define persistent-person state: identity, household/site, role, material interests, skills, relationships, memories, current work, capacity, health, and commitments.
- [ ] Define the active courier’s `conversation` stat and how it changes delegation eligibility, negotiation, task clarity, trust, risk, and outcome without becoming universal or mind-controlling persuasion.
- [ ] Define a constrained task/delegation model for crew and NPCs: offer, agreement/refusal, assignment, progress, interruption, outcome, and later memory. Initial task families include maintenance, rigging, cooking, treatment, cargo handling, trade research, barter, bookkeeping, scouting, charting, gathering, hunting, guiding, watch duty, guarding, rescue, evacuation, recruitment, correspondence, witness work, and negotiation.
- [ ] Make autonomous choices arise from original needs, opportunities, relationships, and local conditions. Distant people use deterministic summary simulation; nearby and recurring people use richer state and behaviour.
- [ ] Define the compact, collapsible management sidebar that is visible by default and provides comprehensive household-known task, people, work, risk, site, route, and history information without displacing the primary map. Label source, discovery time, and freshness; do not expose hidden global state. Assign complementary, non-duplicative roles to map marks, physical notices, messages, ledgers, and tavern conversations.
- [ ] Add original social-memory records and player-readable evidence through physical surfaces, messages, conversations, ledgers, rumours, goods, routes, visible work, and the management sidebar. Do not reproduce proprietary named-system hierarchies or vendettas.
- [ ] Add deterministic tests for delegation, refusal, task interruption, memory, recurrence, and no-player-control autonomy.

Acceptance: the active courier can delegate a task through a conversation; the recipient’s response and outcome follow inspectable state; a later encounter visibly reflects the remembered result.

#### 1.5 Terminal presentation and interaction foundation

- [ ] Define a renderer-independent map, glyph, palette, status, message, prompt, input, and accessibility contract.
- [ ] Create an original ASCII glyph vocabulary for terrain, vessel parts, people, goods, work, hazards, weather, and routes; validate unique/legible use in a character cell.
- [ ] Implement keyboard-first eight-direction movement, compact contextual prompts, remapping, focus handling, command help, and accessible text labels.
- [ ] Specify the future detailed-renderer adapter and its equal-information parity contract, but defer its implementation until Phase 9.
- [ ] Add renderer and browser tests for glyph meaning, prompt cancellation, status/message visibility, keyboard control, and renderer-contract compatibility.

Acceptance: all consequential state can be understood through the ASCII map, status, and message surfaces; the future detailed-view adapter has no authority to hide or invent gameplay information.

#### 1.6 Performance, storage, and diagnostics foundation

- [ ] Establish desktop-browser performance and storage budgets for an 8 GB RAM machine. Support current stable desktop Chrome, Edge, Firefox, and Safari through standards-based browser APIs; Chromium is the automated-browser baseline. State the measurement hardware, browser versions, world sizes, population settings, and acceptable interactive responsiveness in the repository.
- [ ] Design compact, indexed local persistence for a multi-world index, active worlds, read-only chronicles, world/person/event/region/task/history records, and exports. Use atomic short-lived writes, quota/error handling, recoverable snapshots, and explicit import/export backups; do not rely on a server, cloud, account, or network connection.
- [ ] Define profile-guided optimization boundaries: spatial indexing, incremental generation, deterministic scheduled summaries, packed/compact data where measured, memoization, and worker-based generation/simulation only when they preserve reproducibility and readable ownership.
- [ ] Add performance fixtures across generation presets and simulation-fidelity settings, with regression checks for memory growth, persistence size, generation time, due-event simulation time, UI responsiveness, and multi-world/chronicle index operations.
- [ ] Do not add user mod/content-pack compatibility. Keep internal content data-driven, validated, and documented without committing to a public extension surface.

Acceptance: declared 8 GB baseline fixtures generate, simulate, save, reload, and remain responsive within recorded budgets; quota or write failure preserves the last good state and offers recovery/export.

#### 1.7 Effects, recovery, and irreversible-loss foundation

- [ ] Define a renderer-independent effect model for health, injury, exhaustion, preparation, equipment, relics, totems, boons, curses, crew support, enemy weaknesses, environmental interactions, duration, stacking, chaining, and counterplay.
- [ ] Define rarity, source, cost, condition, and audit rules for mystical effects. They must remain finite, legible, in-world, and compatible with low-mysticism tone; no generic mage class is introduced.
- [ ] Define courier death, prevention, revival, household-extinction, and lasting-consequence rules. Revival safeguards are exceptional and explicit; the default outcome remains permanent loss. When no eligible living crew member remains, finalise the world as a read-only, exportable chronicle.
- [ ] Define Jomon integrity, partial disaster, repair, rescue, collapse, and loss rules. A relic or equivalent able to save Jomon from terminal loss is ultra-rare and must have a visible causal chain, cost, and recovery trade-off. Terminal loss ends active play and finalizes a read-only, exportable chronicle.
- [ ] Add deterministic tests for effect combinations, cap/chain behavior, death prevention/revival, Jomon collapse, terminal-world finalization/chronicle export, and state recovery.

Acceptance: a player can inspect why an effect or recovery occurred; every powerful safeguard has an explicit rarity/cost/counterplay contract; terminal Jomon loss ends active play cleanly, preserves its history as a read-only chronicle, and offers export without corrupting the record.

### 2. Physical Jomon foundation

#### 2.1 Walkable Jomon and quay

- [ ] Create a compact, original ASCII deck plan with a connected quay approach, gangplank, tavern, chart table, cargo hold, repair space, stores, berths, and galley.
- [ ] Render the plan in the primary ASCII mode from the common map state; document its original glyph vocabulary and preserve the Phase 1 detailed-renderer adapter contract.
- [ ] Implement grid movement, collision, camera/focus behavior, visibility rules if used, and inspectable seed state.
- [ ] Add a player-visible map legend/help surface without replacing in-world readability.

Acceptance: the entire vessel/quay plan is navigable and every required space is identifiable from map symbols alone.

#### 2.2 Crew continuity

- [ ] Generate the initial household roster, roles, personal equipment, relationships, histories, eligibility, and active-crew representation wholly from the world seed and resolved configuration; define its reproducibility and validation contract.
- [ ] Implement initial-courier selection in world creation. It presents the deterministic eligible roster, changes no world time, permits no individual rerolls, and fixes the selected courier as active until a later tavern-based switch or succession.
- [ ] Implement tavern-based voluntary switching through an operated physical prop.
- [ ] Implement permanent courier death/departure and deterministic transfer of the active perspective to an eligible surviving courier.
- [ ] Display crew availability and loss consequences in a physical vessel surface.

Acceptance: the same world manifest recreates the same valid initial household; choose an initial courier without rerolling; switch active crew in the tavern; lose an active courier; continue as the same eligible successor after reload; observe the lasting household consequence; finalise a crewless household as a read-only chronicle.

#### 2.3 Physical vessel interactions

- [ ] Implement proximity/operation rules for every Phase 2 vessel prop.
- [ ] Add compact contextual key-choice prompts, cancellation, keyboard remapping compatibility, and accessible text labels.
- [ ] Make the chart table, hold, repair space, stores, berths, galley, tavern, and gangplank each expose a distinct minimal action or readout.
- [ ] Persist prop state and show action feedback in the message/status surfaces.

Acceptance: every named space has a represented prop, an operation affordance, an accessible label, and a tested outcome.

#### 2.4 Quay-to-vessel browser proof

- [ ] Start at a quay; board by gangplank; walk to the tavern; select another crew member; operate a vessel station; return to and leave through the gangplank.
- [ ] Add actual-browser coverage for the full path and focused headless determinism coverage.
- [ ] Verify the flow in ASCII mode and ensure its world-state/output contract is consumable by the later detailed renderer.

Acceptance: the browser test completes this exact path through real user input with no abstract hub or terminal shortcut.

### 3. Physical trade and settlement economy

- [ ] Define a small, named commodity set with source, use, weight/bulk, condition, handling requirement, failure mode, and buyer for every entry.
- [ ] Add cargo capacity, loading, unloading, spoilage/damage/loss, recovery, and vessel-hold interaction.
- [ ] Build one named settlement profile with water relationship, labour, authority, services, demand, local pressure, and visible civilian purpose.
- [ ] Add a physical settlement trading location and one contract that creates a material burden, refusal outcome, delivery outcome, and later visible consequence.
- [ ] Model price/stock/demand changes as seeded persistent local state—not a global money-only shop.

Acceptance: acquire cargo at one physical location, transport it, deliver or fail it at another, and observe a durable market or relationship change on return.

### 4. Waterway routes and settlement network

- [ ] Define the first connected river/canal/estuary/coastal settlement network with named route profiles.
- [ ] Define how routes, travellers, charts, rumours, cargo marks, and institutions reveal frontier regions before Jomon reaches them; travel can then trigger deterministic generation and persistent exploration of those regions.
- [ ] Add seeded route knowledge, river condition, weather/season constraint, toll/access condition, and competing traffic where relevant.
- [ ] Make route comparison a chart-table action and departure/arrival a gangplank/quay action.
- [ ] Implement active-play-only route time and an inspectable route-reckoning readout.
- [ ] Make a route outcome change cargo, access, conditions, or local history in a later decision.

Acceptance: choose a route for a visible reason, travel only while actively playing, arrive through a quay, and see a persistent consequence on a revisited route or settlement.

### 5. Persistent world partitions and revisitation

- [ ] Give the vessel, quays, settlements, approaches, roads, waterways, and expedition sites stable world-space identities.
- [ ] Apply the foundation simulation-fidelity tiers to partition streaming, including deterministic catch-up and replay of distant settlement summaries.
- [ ] Persist crew, cargo, altered props, caches, actors, weather effects, route consequences, time, and camera direction within the new medieval schema.
- [ ] Stream only distant partitions; specify and test the resident partition budget.
- [ ] Define corruption recovery, incompatible-save behavior, and schema evolution before each persistence change.

Acceptance: alter a place, leave it, reload, return through normal play, and find the same consequential state while an unseen settlement and recurring agent have advanced deterministically without loading unrelated partitions.

### 6. Equal-depth hazards, conflicts, and regional ecology

- [ ] Apply the social-memory model to human-conflict content so the same people can recur as allies, rivals, witnesses, employers, or consequences without scripted plot progression.
- [ ] Exercise the active-play simulation scheduler through changing named people, settlements, markets, and routes.
- [ ] Populate the foundation history surfaces at Jomon and settlements with witness accounts, ledgers, rumours, notices, cargo evidence, and changed work sites.
- [ ] Implement one complete human-conflict contract: setup, readable intent, choice, resolution, persistent mutation, and later follow-up.
- [ ] Implement one complete wilderness contract using weather, terrain, exposure, animal behavior, disease, or a specifically sourced mystical phenomenon with readable counterplay.
- [ ] Implement one complete operational-hazard contract involving vessels, cargo, works, or infrastructure.
- [ ] Establish common telegraph, consequence, and follow-up interfaces so all three danger families have comparable depth.
- [ ] Add seasonal and local ecology state that changes trade, settlement life, routes, or tactics.

Acceptance: each danger family has a distinct playable scenario with readable counterplay and a visible later-world effect; at least one named person reacts to a remembered outcome through the original social-memory model.

### 7. Medieval tactical overhaul and grounded guardians

- [ ] Define a dense but fast tactical vocabulary: movement, exertion/stamina, commitment, guard, parry, posture, targeted components, retreat, recovery, health, injury, and seeded chance.
- [ ] Implement renderer-independent intent data, target previews, and positional responses for both presentation modes.
- [ ] Implement bounded, legible power combinations through equipment, relics, learned techniques, temporary preparations, crew support, enemy weaknesses, and environmental interactions. Avoid numerical-only stacking and keep resolution fast.
- [ ] Replace prototype content in the new path with original people, animals, machinery, wrecks, siege works, fortified positions, industrial hazards, and rare sourced mystical threats; do not add a generic mage class.
- [ ] Create procedural multi-cell grounded or mystical guardians from original body, component, attack, terrain, reward, and aftermath modules.
- [ ] Test tactical determinism, readability, combat-resolution speed, defeat/escape consequences, and interaction with crew/cargo/world state.

Acceptance: a player can read a telegraphed tactical problem, choose a grounded response, and carry its material consequence back to Jomon or a settlement.

### 8. Content families and procedural composition

- [ ] Create reusable authored families for distinct regions, waterways, settlements, workshops, markets, hazards, wildlife, human groups, enemies, tools, cargo, contracts, crew roles, relics, and guardians.
- [ ] Create vessel-expansion families whose upgrades visibly alter Jomon’s deck, capabilities, upkeep, crew work, cargo decisions, or route options.
- [ ] Create era-aware content variants and generative combinations so base, NG+, and NG++ create new pressures and possibilities from the same grounded systems without content exhaustion or numerical-only scaling.
- [ ] Require generated places to declare material purpose, water/terrain/season relationship, local pressures, rewards, services, and downstream world links.
- [ ] Add seeded fixtures, validity checks, encounter-readability checks, and reward-distribution checks for each family.
- [ ] Grow content only after the family has a player-visible purpose in the vessel/trade/history simulation.

Acceptance: generation sweeps produce valid, distinct locations whose content influences a real later choice.

### 9. Coverage, playtesting, and art

- [ ] Cover every player-visible vessel, crew, trade, route, persistence, tactical, and death/replacement capability with focused tests and actual browser input.
- [ ] Recreate the playtest protocol only after the relevant medieval player surfaces and content families are stable.
- [ ] Produce original Rogue-inspired visual assets only after the map grammar, glyph taxonomy, and renderer requirements are stable.
- [ ] Implement the optional detailed renderer from the Phase 1 adapter contract only after the ASCII game and its systems are stable; verify it conveys identical consequential information and meets accessibility requirements.
- [ ] Run full verification and record exact pass/fail/skip status for each completed slice.

Acceptance: browser playtests and automation cover the complete core loop without prototype terminology or presentation dependencies.

## Deferred tuning decisions

The direction above is settled. These implementation values are intentionally delegated to system design and must be recorded here before the corresponding system is marked complete:

- `WorldGenerationConfig` preset names, defaults, ranges, validation/rejection rules, and user-facing descriptions for every advanced setting.
- The initial world size, initial population, frontier-generation cadence, and storage/performance budgets that fit the 8 GB desktop-browser baseline.
- The management sidebar’s exact information density, tab/layout rules, source/freshness labels, and the precise split of information among it, map marks, physical notices, messages, ledgers, and conversations.
- Delegated-task risk policy: which tasks are safe, hazardous, or require an explicit player confirmation; what warnings protect a person or Jomon from a terminal outcome.
- The base/NG+/NG++ thresholds and signatures: how active-play time and Jomon growth are weighted, which world pressures shift, and how the NG++ plateau remixes without raw numerical inflation.
- The first mystical-effect families, their rarity bands, provenance, counterplay, and visual language; especially the ultra-rare rule for an effect that can save Jomon.

## Delivery and verification rules

- Work in one cohesive, maintainable system unit at a time. Keep exactly one roadmap item marked `[-]`; build foundations and their tests before expanding player-facing content.
- Before a high-risk save, renderer, world-streaming, or combat change, add a decision-complete implementation plan beneath the relevant item or in a narrowly scoped linked design document.
- Prefer documented domain types, small modules with explicit ownership, deterministic/pure simulation functions, bounded state, and narrow public APIs. Do not add a content exception that bypasses a foundation contract.
- Keep the code navigable for humans and AI agents: name data by its game meaning, state invariants beside their types, provide seeded fixtures, and update this plan when an architecture decision changes.
- Preserve unrelated changes. Do not clean, migrate, or reuse old saves outside the explicit clean-break implementation.
- Each implementation task needs focused tests first. Completed slices also run, when available: `npm test`, `npm run test:autoplay:tasks`, `npm run test:e2e`, `npm run build`, and `git diff --check`.
- Browser coverage must execute the feature through actual UI input; task-ID mapping is not sufficient.
- Record failed, skipped, unavailable, and incomplete verification plainly. A task is not complete merely because it compiles, has a test file, or resembles prototype capability.
- Keep consequential randomness seeded and expose enough state to reproduce a reported play session.

## Final legacy prototype retirement `[ ]`

This is intentionally the final repository task. Do not start it while the isolated prototype is still useful for implementation comparison or regression diagnosis. Start only after every applicable medieval phase through Phase 9 is complete, the fully medieval browser build is verified, and no open task names a prototype module as a temporary diagnostic dependency.

- [ ] Perform a final import, route, build-output, dependency, asset, script, and test audit. Identify the exact legacy files that are unused, unreachable from the completed medieval application, or no longer useful as diagnostics; keep only neutral tooling/configuration and any files still demonstrably required by the finished medieval build.
- [ ] Remove the superseded space-fiction application and all now-irrelevant implementation, tests, browser routes, persistence migrations, assets, fonts, audio, generated sprite artefacts, autoplay/telemetry/campaign tooling, and package scripts. Remove `?prototype` and its browser coverage once its diagnostic purpose has ended.
- [ ] Simplify the repository's public commands, documentation, ignore rules, and build configuration so they describe and execute only Jomon's completed medieval product and its current maintenance tooling.
- [ ] Re-run the complete medieval verification suite, inspect the production output to confirm it contains no legacy chunks or assets, confirm no old save namespace is opened, and record every retained compatibility/tooling file with its reason.

Acceptance: a clean checkout contains only the completed medieval game and neutral supporting tooling; the normal build, tests, documentation, browser routes, assets, and package commands contain no unreachable or obsolete prototype product code. This task is complete only after the removal audit and full verification are recorded here.
