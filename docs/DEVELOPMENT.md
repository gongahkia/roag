# Vagabonds development guidance

Read [PROJECT_STATE.md](PROJECT_STATE.md) before working. Update it alongside behavior, controls, saves, tooling, content or known limits; record actual checks in [VERIFICATION.md](VERIFICATION.md).

Vagabonds is one fantasy party RPG, incorporating selected systems from Ithica's replayed history. Follow [INTEGRATION.md](INTEGRATION.md) for the user-approved direction and unresolved design choices. Adapt mechanics to Vagabonds' lore and bindings. The pinned local Ithica reference is development-only; keep this game runnable with its own source, assets, catalog, mappings and project references, without asset symlinks, submodules or runtime paths into the reference checkout.

Keep graphics-free deterministic rules in Core and presentation/input in Client. Preserve invalid-action no-cost behavior, independent map/combat RNG, reachable quest prerequisites, persistent maps and exact saved continuation. Use `./vagabonds.sh` to launch the game and `./dev` for developer tools. Both use `tools/dotnet.sh`; `tools/setup-dotnet.sh` provides the optional SDK installer.

Preserve original source images, IDs, crops, provenance and uncertain identities. Edit authoring inputs and regenerate derived catalogs. Historical `ithica-*` asset filenames/catalog fields, original pack names and `lantern-rpg` save identifiers are intentional origin/compatibility records; do not globally rename them. New saves belong under `~/.vagabonds/`, and legacy imports must leave their source untouched.

Preserve unrelated user changes and use cohesive commits when authorized. Keep build output, player saves and readable generated `assets/catalog.json` out of source control. Distinguish configured CI from executed checks and local graphical testing from untested platforms.

At every substantial milestone, review authoring/tooling usefulness. Each new content family needs stable IDs, validation, reference discovery, a reproducible preview/scenario and an updated authoring guide. Extend the shared developer entry point for repeated manual workflows, and use it against real game content. Data composes registered deterministic mechanics; new mechanics still need Core code, an explicit extension point and meaningful tests.

Use `./dev agent context` and `./dev agent impact` to orient and choose checks. For sustained tasks, persist explicit requirements/checkpoints and use `./dev agent verify --profile PROFILE --detach`; reconnect by run ID. Complete tasks only after fresh evidence covers their requested profiles/scenes and any visual review. Scenario files reproduce actual rule failures; validated content patches support safe iteration. See [AGENT_TOOLING.md](AGENT_TOOLING.md) for commands, limitations and examples.
