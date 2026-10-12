# Seven-biome route and generated layouts (expansion M3)

A run still has three biome stages and a separate Final Sanctum. Each biome
stage now consists of two connected, generated floors sharing the twelve-room,
five-paid-chest and two-secret budget. Its biome elite lives on floor 2. The player
visits three of the seven biome identities in a run; visiting every biome is
neither required nor possible in one run.

| Biome | Layout identity | Current regular pool |
|---|---|---|
| Overgrown Ruins | Existing broken stone graph | Guard, Thornspitter, Skitter |
| Fungal Hollows | Winding warrens and offset clusters | Spore Herald, Skitter, Guard |
| Sunken Aqueduct | Parallel channel banks and bridge gaps | Canal Lancer, Cinder Mender, Thornspitter |
| Ashen Bastion | Courtyard wings and widened chokepoints | Cinder Bellkeeper, Guard, Skitter |
| Glacial Observatory | Two-loop radial graph, star-node side pockets, icy authored room cells | Glass Charger, Crystal Bombardier, Vault Lurker |
| Fractured Archive | Staggered shelf aisles, deliberately aligned corridors and parallel reading lanes, mist cells | Mire Binder, Pack Chorister, Gloom Strider |
| Sundered Forge | Two-loop offset furnace bays, asymmetric widened passages, crystal cells | Forge Conjurer, Mossbreaker, Bastion Bulwark |

The three new template families are authored as plain-data room definitions
with protected socket/marker approaches, transforms and editable masks. They
share the generator's map contract and existing combat system. M4 replaced
the original temporary encounter pools and gave each non-Ruins biome its own
elite; [encounter notes](ROEG_ENCOUNTER_EXPANSION.md) list their footprints,
roles and provisional values. Thorn Warden remains exclusive to Ruins.

Route choices are cached in the spawned extraction exit and spend zero world
time when browsed or confirmed:

| Stage 2 choice after Ruins | Stage 3 offers |
|---|---|
| Fungal Hollows | Sunken Aqueduct, Ashen Bastion, Fractured Archive |
| Sunken Aqueduct | Fungal Hollows, Ashen Bastion, Sundered Forge |
| Glacial Observatory | Fungal Hollows, Fractured Archive, Sundered Forge |

The menu accepts **1/2/3**, **Enter** and **Esc**. Stage 3 always offers the
existing Final Sanctum confirmation after its elite. Old saved extraction
menus with two cached choices retain those choices on Continue; current new
runs use three. Active-run schema remains `roeg-run/11`; concrete floors are
stored rather than regenerated on restore.

Validation at this checkpoint: 100 root seeds × seven biomes × two floors =
**1,400 generated maps**, all valid, zero repairs or fallbacks. The three new
biomes had 1/0/0 retries respectively. Headless tests verify route coverage,
no repeats, room families/topology metrics, actual stage creation, encounter
pools and exact snapshot restoration. An assisted two-handoff fixture confirms
Ruins → Observatory → Forge with zero-time transitions. A separate ordinary
`Game.submit` guide uses the retained single-floor test mode to reach
Observatory and defeat a Lancer in 371 actions; it does **not** establish a
complete two-floor Victory. Three isolated GUI rounds captured each new biome
on both floors, and a fresh-process Save & Quit / Continue check restored each
new biome in a separate disposable save directory.

Reproduce focused checks:

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp sh tests/expansion_biome_gui_smoke.sh
TMPDIR=/var/tmp sh tests/expansion_biome_disk_resume.sh
```

Studio E03's **B/F/J** biome assignment/preview controls include these IDs
without changing authored stable room IDs. No new editor topology algorithm
or animation workflow was added.
