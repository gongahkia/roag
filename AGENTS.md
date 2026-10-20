# Repository guidance

Read [docs/PROJECT_STATE.md](docs/PROJECT_STATE.md) before working in this repository. It is the master reference for implemented features, content counts, code/data ownership, saves, asset authoring, limits and verification. Read [docs/ITHICA_PLAN.md](docs/ITHICA_PLAN.md) for the approved combined-game direction and staged implementation. The README and specialist guides provide deeper task-specific detail.

## Maintain the project reference

- Update the master document in the same change when features, content counts, controls, formats, tooling or known boundaries change. Reconcile its date and verification notes with what was actually checked.
- Verify current behavior in code and authored configuration. Keep implemented systems, catalog ideas and future work distinct.
- The game is **Ithica**, a mysterious lunar salvage colony. The approved target combines colony and RPG play around the same residents, inventory and campaign. Regolith and Lantern Below are legacy implementation names, not separate intended products. Preserve their tested systems and runnable regression paths while integrating them; Python under `legacy/` is archived.
- Keep the plan and implemented inventory distinct. A renamed project is not evidence that the shared loop, clock or save format is implemented. Mark implementation stages complete only after their acceptance checks pass.
- Apply the approved timing contract: continuous colony time while unpaused; both colony and expedition wait at RPG decisions; accepted actions advance one shared logical clock. No background wall-time colony advancement while the player thinks.
- Keep the Moon setting and sparse observational naming. Existing fantasy place/enemy names are prototype content to recast deliberately, not final lunar canon.

## Implementation boundaries

- Keep gameplay graphics-free in Core and input/rendering in the client. Until the planned consolidation lands, shared graphics changes can affect both existing clients.
- Preserve original artwork, stable sprite IDs/crops and existing mapping semantics. Edit the documented authoring inputs and regenerate catalogs/manifests instead of hand-editing generated sections.
- Preserve quest reachability, protected routes, RNG independence and save continuation when changing RPG generation. `LegacyWorldBuilder` is required for version-one saves and fallback.
- Preserve original pack names and historical save-format identifiers during the Ithica rebrand. Use explicit migrations/readers; do not globally replace strings in third-party provenance or overwrite existing player saves.
- Use the verification commands in the master reference appropriate to the change. Update verification claims with actual results; distinguish local graphics checks from CI configuration.
