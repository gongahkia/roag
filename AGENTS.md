# Repository guidance

Read [docs/PROJECT_STATE.md](docs/PROJECT_STATE.md) before working in this repository. It is the master reference for implemented features, content counts, code/data ownership, saves, asset authoring, limits and verification. The README and specialist guides provide deeper task-specific detail.

## Maintain the project reference

- Update the master document in the same change when features, content counts, controls, formats, tooling or known boundaries change. Reconcile its date and verification notes with what was actually checked.
- Verify current behavior in code and authored configuration. Keep implemented systems, catalog ideas and future work distinct.
- Preserve both playable prototypes unless the user's task changes that scope. Regolith is the colony game; Lantern Below is the RPG; Python under `legacy/` is archived.

## Implementation boundaries

- Keep gameplay in the respective Core project and input/rendering in its client. Shared graphics changes can affect both clients.
- Preserve original artwork, stable sprite IDs/crops and existing mapping semantics. Edit the documented authoring inputs and regenerate catalogs/manifests instead of hand-editing generated sections.
- Preserve quest reachability, protected routes, RNG independence and save continuation when changing RPG generation. `LegacyWorldBuilder` is required for version-one saves and fallback.
- Use the verification commands in the master reference appropriate to the change. Update verification claims with actual results; distinguish local graphics checks from CI configuration.
