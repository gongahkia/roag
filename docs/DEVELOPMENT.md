# Vagabonds development guidance

Read [PROJECT_STATE.md](PROJECT_STATE.md) before working. Update it alongside behavior, controls, saves, tooling, content or known limits; record actual checks in [VERIFICATION.md](VERIFICATION.md).

Vagabonds is the independent fantasy party RPG extracted from Ithica. Ithica's lunar colony is a separate sibling repository. Keep this game runnable with its own source, assets, catalog, mappings and project references; do not introduce asset symlinks, submodules or runtime paths into Ithica.

Keep graphics-free deterministic rules in Core and presentation/input in Client. Preserve invalid-action no-cost behavior, independent map/combat RNG, reachable quest prerequisites, persistent maps and exact saved continuation. Normal launcher: `vagabonds.sh`; `play.sh` and `rpg.sh` forward to it.

Preserve original source images, IDs, crops, provenance and uncertain identities. Edit authoring inputs and regenerate derived catalogs. Historical `ithica-*` asset filenames/catalog fields, original pack names and `lantern-rpg` save identifiers are intentional origin/compatibility records; do not globally rename them. New saves belong under `~/.vagabonds/`, and legacy imports must leave their source untouched.

Preserve unrelated user changes and use cohesive commits when authorized. Keep build output, player saves and readable generated `assets/catalog.json` out of source control. Distinguish configured CI from executed checks and local graphical testing from untested platforms.
