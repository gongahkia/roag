# Main-world mechanical compatibility

`jomon.mechanical_compatibility` identifies the effective main-world mechanics
provided by the selected content pack. It does not hash raw JSON files: raw
bytes would treat formatting, path changes, and retained fiction as mechanics.

The module loads each required catalog through the ordinary selected-pack loader,
then projects only explicitly classified mechanical data. Catalog and row
projectors list every known field as mechanical, presentation, or ignored. An
unknown field raises `MechanicalProjectionError`; adding a catalog field must be
an intentional compatibility decision.

`MECHANICAL_COMPATIBILITY_VERSION` changes when the engine's interpretation of
stable mechanics or projection semantics changes. `MECHANICAL_PROJECTION_FORMAT`
identifies the shape of the canonical projection. Neither changes for prose,
presentation slots, manifest display names, JSON whitespace, or object-key
ordering.

`main_world_mechanical_fingerprint()` serializes the projection with canonical
sorted JSON and returns its SHA-256 digest. It includes the selected pack's
effective catalog root, but never the pack ID, filesystem path, manifest display
name, or selected-pack presentation files. Lists retain source order whenever
that order may affect selection or generation.

The fingerprint is a catalog-environment compatibility input for persistence; it
is not a runtime-state snapshot and does not participate in RNG, ordering, or
gameplay. Save-format metadata and compatibility enforcement are deliberately
left to Milestone 12C.
