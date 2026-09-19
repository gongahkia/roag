# Changelog

## 0.2.0 — Deepward side-view colony prototype

Replaces top-down height fields with a fine material grid; introduces physical
worker footprints, coarse structures, autonomous work, inventory reservations,
construction delivery, crops/irrigation, manual pumping, needs and lethal hazards.

Adds permanent challenge-run losses and read-only archive rewind, separate practice
branching, full colony checkpoints, safe data-only saves, replay/context exports,
body-aware BFS navigation, resource-budget evaluations and headless/GUI-adapter tests.

Carries forward the authored random/value-noise primitives. The old 0.1 renderer,
generation presets, simulation state and save model are not compatible. No 0.1
files are overwritten by the new project archive.

This version has no combat, heat/pressure solver, social engine or rigid-body collapse.
