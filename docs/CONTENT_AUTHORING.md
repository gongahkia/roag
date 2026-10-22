# Content authoring and developer tools

Run `./dev` from any working directory. The graphics-free `Vagabonds.Tools` project uses the same Core content loader, actor factory, abilities and interactions as the game. Game definitions are embedded at build time; developer commands can inspect edits from `content/` before rebuilding the client. No player save is opened by these tools.

## Discover and edit

```sh
./dev content list
./dev content list character
./dev content inspect character.healer
./dev content references ability.mend
./dev content scaffold character --id character.wayfarer
./dev content validate
./dev preview character character.wayfarer --seed 42
./dev preview interaction interaction.healing --seed 42
./dev content remove character.wayfarer
./dev content remove character.wayfarer --apply
./dev test
./dev review-tooling
```

Edit the corresponding readable JSON file directly. Scaffolding refuses to overwrite existing files. Removal defaults to a dry run and blocks references, including the runtime's required `game.vagabonds` entry point. Update those dependents deliberately before removal. Original asset IDs/crops/bindings remain under their established authoring workflow; content refers to existing Vagabonds sprite aliases.

Definitions have `kind`, stable `id`, readable `name`, optional `description`, `sprite` and `tags`. IDs use `kind.lowercase-name` and remain separate from generated actor instance IDs. `game/vagabonds.json` selects the traveller/NPC archetypes, four classes, common combat actions, boss and recipe pools, and initial inventory. A new definition joins the game by being referenced from an appropriate pool; an unused definition can be previewed without changing spawning.

| Kind | Authoring fields / runtime use |
| --- | --- |
| character | Name/profile pool, class pool or explicit appearance/stats, interactions and enemy behaviour rules |
| names / trait | Generated identity pools, backgrounds, motivations and disposition modifiers |
| class | Role slot, base stats, sprite and three class abilities |
| ability | Target, mana, power, registered effect and status duration |
| interaction | Registered action, maximum distance, item costs and effect amount |
| item | Inventory key, value, appearance, equipment slot and stat modifiers |
| recipe | Buildable material, reserved item costs and required work |
| generation | Four-floor layout selection per site purpose and biome tree/rock/water profiles; shared by runtime and audit |
| boss / variant | Authored identity/tactics/weakness, stats, conditional behaviour phases, minions and variation pools |

Bosses, enemy behaviours, equipment, character creation, party abilities and conversations use the content catalog at runtime. Timed recipes are the next checkpoint. Enemy rules select weighted eligible actions at the start of each round; cadence, health threshold and phase conditions apply then. Intentions persist through saves; a dead chosen target wastes a single-target action. Enemy medicine/retreat handlers are rejected. `game.enemyFallback` must reference a free damaging attack.

## Validation and previews

`content/schema.json` supplies editor completion and structural validation. Core adds cross-reference/type checks, known sprite/effect/action checks, numeric limits, duplicate-ID/role/inventory-key checks and required game pools. Errors identify the file and field. Unknown properties are errors, so spelling mistakes do not silently become defaults.

After changing the C# definition types, regenerate the schema explicitly:

```sh
./dev content schema --output content/schema.json --replace
```

The test suite checks that the tracked schema matches the exporter. `--content DIRECTORY` previews/validates an isolated content copy. Preview results include the actual generated identity/stats or the result of a real interaction command. Interaction previews preserve logical time and random state, pay actual inventory costs, and use no player save.

Content definitions describe possible people. Campaign actors retain their own identities, resolved stats and changing state. World saves pin the content hash; incompatible content fails safely before replacing an active session. This development expansion uses world save version two and does not migrate earlier world slots. Use an explicit new slot/run after content changes; never delete a player's selected save as a development shortcut.

## Extend a mechanic

1. Add the typed definition fields in Core/Content/Definitions.cs and their validation/reference rules in ContentCatalog.cs.
2. Register and implement a deterministic Core handler. Data cannot execute arbitrary code. Validate costs/targets before mutation and preserve invalid-action no-cost/no-RNG behaviour.
3. Add real authored content that exercises the handler, a reproducible preview and meaningful tests of its effects/failures.
4. Regenerate the schema, update this guide/project state, and run the relevant developer commands yourself.

At each substantial milestone, inspect `review-tooling` and evaluate actual authoring friction, diagnostics and preview coverage. Promote repeated manual checks into commands. Definition counts and passing validation establish consistency, not whether an encounter or world is enjoyable; those require inspection and playtesting.

## Inspect procedural worlds

`./dev preview generation generation.world --seed 42` exercises the configured algorithms. `./dev audit-world --seed 40 --samples 8 --output artifacts/world-audit` writes JSON measurements and an SVG sheet; use `--replace` deliberately for an existing output. It reports raw/final hashes, open-cell density, components, stairs, fallbacks and repairs separately. Inspect the sheet alongside the numbers: distinct hashes alone do not establish interesting worlds.

WFC is retained for towns/ruins. Registered structural algorithms are `bsp-workings`, `bsp-fort`, `caverns` and `ring-sanctuary`; profiles select these without changing code. New algorithms require a Core implementation and validation registration. The gradient-noise implementation was adapted from the pinned Ithica `070558d` source; there is no runtime dependency on that checkout. Macro placement and connected road loops remain Core invariants.

## Preview authored encounters

`./dev preview encounter boss.briar-matriarch --seed 42` runs a generated four-member party against the actual boss factory, variant, minions, intentions and resolver for up to four rounds. It reports actions, health, intentions and shared ticks without saving. This is a deterministic diagnostic using basic attacks, not a combat-balance claim. Every shipped archetype is exercised by the suite. `--scene world-boss` provides the graphical preview at the selected seed. Equipment snapshots preserve item identity and resolved modifiers across saves; swaps return previous equipment to the shared pack.
