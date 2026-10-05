# Internal tooling audit — VIS-01

This is an inspection report, not an editor-refactor plan. Findings reflect the
repository after the Loveable Rogue visual migration. No broad editor work was
performed for this audit.

## Inventory

| Tool/editor | Entry point | Purpose | Source of truth | Status |
|---|---|---|---|---|
| Studio shell | `make studio` / `love studio` | Links focused authoring surfaces | No content itself | PARTIALLY USEFUL |
| Screen Composer | Studio → Screen Composer | Game-screen copy/layout/accent tokens | `content/screens/legacy.json` | USEFUL NOW, narrow |
| Modifier Workbench | Studio → Modifier Workbench | Serialized passive definitions | `content/expedition/modifiers/*.json`, manifest | PARTIALLY USEFUL |
| Room Template Editor | `make room-editor` / `love level_editor --room-editor` | Legacy Dungeon/Reactor room templates | `content/rooms/{dungeon,reactor}/*.room.json` + manifests | USEFUL NOW for its corpus only |
| Generation Inspector | `make generation-inspector` / `love level_editor` | Inspect a deterministic generated world | Runtime-generated isolated world | USEFUL NOW, read-only |
| Loveable Rogue atlas contract | JSON + `tools/validate_loveable_rogue.lua` | Fixed visual semantic binding | `content/presentation/loveable_rogue_atlas.json` | USEFUL NOW, hand-authored |
| Debug Cockpit | `make debug-*` / `tools/roag_debug.lua` | Repro scenarios, modifier/Expedition diagnostics | Simulation inputs, no write | USEFUL NOW |
| Headless analyzers/validators | `tools/analyze_*.lua`, `tools/validate_*.lua` | Determinism/content/balance diagnostics | Production content | USEFUL NOW, not CRUD editors |

The old Sprite Workbench was retired. It edited a removed arbitrary pack map,
not the selected Loveable Rogue semantic contract.

## Studio shell

`studio/main.lua` contains three meaningful links: Screen Composer, Modifier
Workbench, and Room Workbench/Generation Inspector external commands. The
Visual Atlas card is informational only; it identifies the fixed mapping file,
but there is no atlas viewer or click-to-select mapping tool.

- **Navigation:** a static card home screen; no workspace/project chooser.
- **Saving:** delegated to pages; there is no global unsaved-work guard.
- **Preview:** Screen Composer has a synthetic preview; Modifier page reports
  simulator counts; Room Editor is an external tool with no Expedition preview.
- **Stale/dead surface:** no old Sprite Workbench link remains. Its replacement
  is not an editor, only a documented validation contract.
- **Usability:** commands are displayed as card text rather than being launched
  by Studio. The Studio uses ordinary development fonts intentionally; the
  game bitmap font is not forced onto authoring UI.

## Modifier Workbench

### Actual capability

`src/expedition/modifier_editor_model.lua` is genuinely capable at the model
level: list/search, select with dirty protection, create, duplicate, validate,
canonical save, deletion confirmation, hook/condition/effect/static-stat
construction, stack previews, and isolated real-runtime simulation.

The Studio page (`studio/modifier_editor.lua`) exposes list selection and edits
only `id`, `name`, and `description` text fields directly. It shows tags,
static effects, reactive hooks, a ×3/×4 preview, and buttons for New,
Duplicate, Save, Validate, Simulate, Delete and Back.

| Concern | Result |
|---|---|
| CRUD | Create/duplicate/save/delete-confirm exists; visual delete has a second-click confirmation. |
| Validation | Production `Definitions.validate` and canonical store are used. |
| Runtime simulation | Uses `ModifierSimulator.run` through real compiled definitions; save/meta state is isolated. |
| Stack expressions | Model supports previews and all registry expression kinds; Studio only presents a fixed ×3/×4 description preview. |
| Trigger/condition/effect edit | Model supports add/remove from registries; Studio does not expose field-level hook editing. |
| Trace | Simulator returns real trace data; Studio reduces it to an effect/node count, not a readable hierarchy. |
| Dirty/undo | Dirty protection exists. No undo/redo stack. |
| File safety | Safe filename mapping/canonical JSON; model save is production-shaped. Atomic-write guarantees depend on `modifier_store`; screen editor is weaker. |

**Verdict:** useful for inspecting and lightly editing production modifier
metadata, but not production-ready for authoring rich hooks at 65-item scale.
It is a developer surface rather than a complete Workbench.

## Visual/sprite tooling

There is no visual atlas editor after VIS-01. The current contract is:

```text
local source collage
→ deterministic crop script
→ runtime atlas PNG
→ reviewed JSON rectangle/binding map
→ validator/runtime loader
```

The mapping JSON is currently hand-authored. There is no visual quad picker,
font preview, wall-autotile preview, or semantic-map editor. The removed Sprite
Workbench cannot help because it only manipulated the old generic 49×22 sheet.
An atlas mapping tool might be useful later, but is not automatically justified
until the fixed map actually becomes painful to maintain.

## Content-editor audit

Most shared Campaign/Sandbox content is declarative Lua under `content/*/legacy.lua`.
Expedition modifiers and room templates are JSON-authoritative. No general
content browser exists. There is no editor for classes, weapons, abilities,
enemies, bosses, hazards, liquids, materials, encounters, rewards, chamber
plans, or visual bindings beyond hand-edited JSON.

Validation is strong at the loader/analyzer layer (`Registry.load`, modifier
validators, room validators), but editor coverage is uneven. The game has no
duplicate registry for modifier content; gameplay systems remain appropriately
hard-coded engine logic. The biggest authoring gaps are Expedition chambers
and encounter composition, not more generic data entry fields.

## Chamber / level editor audit

### What exists

The retained Room Template Editor supports the legacy 11×11 Dungeon and Reactor
corpora only. It can open/new/duplicate templates, paint existing legend glyphs,
toggle boundary connectors, cycle tags, change weight/rotation, preview rotation,
validate, save, and protect unsaved changes. It writes through the room store
and validator and is backed by tests.

The read-only Generation Inspector can recreate a seed, toggle world overlays,
pan/zoom, inspect cells, and inspect room provenance. It cannot author content.

### What it cannot author

| Production chamber requirement | Current editor support |
|---|---|
| Expedition chamber dimensions/topology | No — Expedition chamber plans are generated in Lua. |
| Floor/wall tile semantics | Partial — legacy room ASCII glyphs/materials only. |
| Doors | No dedicated placement/control. |
| Water, gas, fire, hazards, conductive areas | No direct editor placement. |
| Breakables | Only indirectly through legacy material selection. |
| Player/enemy spawn points or roles | No. |
| Reinforcement sources/waves | No. |
| Exit/cache/reward placement | No. |
| Boss chamber authoring | No. |
| Expedition runtime preview | No. |
| Encounter/topology compatibility validation | No. |

**Reality check:** a designer cannot currently create a fun production
Expedition chamber visually without editing source data/code by hand. The
existing room editor is useful legacy world-template tooling, not a chamber
combat workbench.

## Encounter editor audit

No independent encounter editor exists. Expedition archetypes, role mixes,
threat budgets, waves/reinforcements, reward intent, and topology selection are
currently coupled in Expedition Lua planning/content. Headless analyzers can
measure resulting plans but cannot edit them. This is the strongest identified
coupling between combat content, chamber geometry, and rewards.

## Workflow checks attempted

- **Modifier:** model-level production workflow is covered in
  `tests/test_modifiers.lua`: create/duplicate, alter a stack expression,
  validate, simulate with the real resolver/trace, save and delete-confirm in
  an isolated writable test store. The Studio surface was inspected against
  that model and does not expose all model operations.
- **Room:** model-level tests cover create/duplicate, paint, connector changes,
  validation, deterministic JSON save/reload, and unsaved-change protection.
  It can safely author its two existing corpora but cannot make an Expedition
  chamber.
- **Screen:** Screen Composer loads, edits copy/layout/accent, validates through
  `ScreenManager`, and writes JSON. Its write path is direct rather than an
  explicit atomic temporary-file rename, and it has no undo/redo.
- **Visual:** `tools/validate_loveable_rogue.lua` validates source/runtime PNG
  dimensions and metadata; `tests/test_loveable_rogue_assets.lua` validates
  binding completeness and deterministic font behavior. No editor workflow
  exists by design.

## Source-of-truth map

| Content | Authoritative location | Runtime loader | Editor/tool | Validator |
|---|---|---|---|---|
| Expedition modifiers | `content/expedition/modifiers/*.json` | `src.expedition.modifiers` | Modifier Workbench/model | `analyze_modifiers.lua`, modifier tests |
| Expedition encounters/chambers/rewards | `src/expedition/run.lua`, `src/expedition/content.lua` and supporting Lua | `ExpeditionRun` | No editor | Expedition/analyzer tests |
| Classes/start kits | `src/expedition/content.lua` | Expedition content | No editor | Expedition tests |
| Legacy rooms | `content/rooms/*/*.room.json` | `src.rooms.registry` | Room Template Editor | room/content validators |
| Legacy world/actors/enemies/bosses | `content/*/legacy.lua` | `src.content.registry` | No editor | `Registry.load`, focused validators |
| Screen copy/layout | `content/screens/legacy.json` | `ScreenManager` | Screen Composer | screen tests/loader |
| Menu flow | `content/presentation/flow.json` | `PresentationFlow` | No editor | flow tests/loader |
| Loveable Rogue mappings/font | `content/presentation/loveable_rogue_atlas.json` | `LoveableRogueAssets` | Hand-edited JSON only | visual validator/content validation |
| Runtime atlas | `assets/visual/loveable_rogue_atlas.png` | `LoveableRogueAssets:load` | deterministic extraction script only | PNG/metadata validator |
| Modifier/runtime diagnostics | Generated reports only | real simulation | Debug Cockpit/analyzers | deterministic test suite |

## Hard-coded content audit

| Area | Classification | Why |
|---|---|---|
| Damage, Force, electricity, fire, explosions, chain safety | Acceptable engine logic | Reusable primitive implementation, not ordinary authored content. |
| Expedition topology/encounter/reward schedule | Should become data later / editor gap | Main source of chamber-content friction. |
| Expedition classes/start kits | Should become data later | Small current set; no urgent framework rewrite required. |
| Legacy registry Lua content | Data in Lua; editor gap | Active Sandbox/Campaign compatibility content. |
| Loveable Rogue mapping | Hand-authored data; acceptable now | Fixed reviewed atlas; a visual tool is optional, not required. |
| Retired art packs/procedural glyphs | Removed obsolete legacy | Replaced by one atlas path. |

## Top 10 practical tooling problems

1. No Expedition chamber + encounter authoring surface.
2. No independent encounter composition/reward intent editor.
3. Modifier Studio page hides most real model capabilities.
4. Modifier simulator does not render/display its hierarchical trace.
5. Studio lacks a unified searchable content browser.
6. Screen Composer saves directly and has no undo/redo.
7. Room Editor is limited to old 11×11 Dungeon/Reactor templates, not compact Expedition boards.
8. No runtime chamber-preview loop from authored content.
9. No visual atlas mapping preview/quad selection for the fixed Loveable Rogue contract.
10. Studio shell’s navigation is shallow and external-tool cards are not integrated workflows.

## Candidate next refactor directions

### Option A — Chamber + Encounter Workbench

**Benefit:** directly enables authored combat boards, role placement, hazards,
rewards, validation, and a real Expedition preview. **Effort:** high.
**Dependencies:** first formalize Expedition chamber/encounter descriptors.
**Why it matters:** highest leverage for core-loop iteration.

### Option B — Unified Content Browser

**Benefit:** navigates JSON/Lua-authoritative content and clearly exposes
which types are editable. **Effort:** medium. **Dependencies:** stable loader
metadata and a decision about Lua-backed content. **Why it matters:** reduces
discoverability and workflow fragmentation.

### Option C — Modifier Workbench refinement

**Benefit:** exposes existing registry-driven hook/effect/condition editors,
stack-preview controls, trace tree, filter/search, and safer save UX.
**Effort:** medium. **Dependencies:** none significant. **Why it matters:**
turns an already sound model into a practical content tool.

### Option D — Loveable Rogue Atlas Mapping Tool

**Benefit:** visual selection/preview of atlas rectangles and glyphs.
**Effort:** low-to-medium. **Dependencies:** stable fixed atlas contract.
**Why it matters:** helps only if visual mappings are expected to change often.

### Option E — Studio shell cleanup

**Benefit:** clearer launch/save/preview flows and explicit tool status.
**Effort:** low-to-medium. **Dependencies:** decisions about Options A–D.
**Why it matters:** makes future tools feel coherent but does not create new
gameplay content by itself.

## Recommended priority

Option A, a Chamber + Encounter Workbench, appears highest leverage: FEEL-01
made compact chambers the game’s primary spatial unit, while current tooling
cannot author one. The human should decide whether to invest there before a
broader content browser or visual mapping utility.
