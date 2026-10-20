# Ithica repository guidance

Read [docs/PROJECT_STATE.md](docs/PROJECT_STATE.md) before working. It is the master reference for implemented gameplay, counts, controls, ownership, saves, assets, boundaries and verification. Specialist guides add detail; [docs/VERIFICATION.md](docs/VERIFICATION.md) records actual checks and rollback checkpoints.

## Maintain the source of truth

- Update the master reference in the same change when behavior, content, formats, controls, tooling or known limits change. Verify facts in source/authored configuration and record actual validation.
- The normal product is **Ithica**, one mysterious lunar salvage colony/RPG. Keep names sparse and mechanics readable. Fantasy content belongs to the explicit retained preview, not current lunar canon.
- Standalone fantasy RPG development belongs to the independent sibling **Vagabonds** repository. Keep Ithica's existing fantasy preview as a compatibility/regression fixture; neither game's launchers, source or assets should depend on the sibling checkout.
- Keep implemented systems, unused asset ideas and future work distinct. Preserve original source images, IDs, crops and publisher evidence; edit authoring inputs and regenerate derived catalogs/manifests/tables.
- When committing is authorized, use cohesive working checkpoints and preserve unrelated user changes/commits. Keep build output and player saves out of source control.

## Gameplay and implementation boundaries

- Keep rules in graphics-free Core and input/rendering in Client. One campaign owns residents, inventory and logical time; field heroes/units are controlled resident-ID projections. Away residents cannot also work or receive local care at home.
- Colony time runs continuously while unpaused. Both expedition and home wait at field decisions; accepted actions advance the shared clock. Invalid actions consume no time/RNG/resources. Mode transitions add no wall-time backlog or fixed-shift double charge.
- Preserve deterministic commands, RNG independence, protected routes, prerequisite reachability, persistent sites, one-time stock transfers and exact saved continuation. Failed undeposited components must remain recoverable.
- Retain `regolith-csharp` / `lantern-rpg` readers and `LegacyWorldBuilder` for compatibility/regression. Original pack names, historical formats/records and SDK fallback paths retain their identities intentionally. Never globally replace those strings or overwrite legacy player saves while rebranding.
- The normal launcher is `ithica.sh`; `play.sh` forwards to it. `rpg.sh` / `--preview` are the retained fantasy regression path. Python under `legacy/` is archived.
- Use the master reference's checks appropriate to the change. Distinguish configured CI from executed checks and native macOS verification from untested desktop graphics on other platforms.
