# TOOL-01 Expedition Workbench review

Use Studio → **Expedition Workbench**. Do this without editing Lua or JSON manually:

1. Create a chamber; paint walls and water; place player/enemy spawns and an exit; validate and save.
2. Create an encounter; add ranged and rusher role rows; set its threat budget/topology compatibility; validate and save.
3. Select the chamber and encounter; preview the same seed; enter Play Preview.
4. Change the board, reset the same seed, and confirm the actual runtime reflects it.

Record:

- Could I tell tiles from markers at a glance?
- Did validation errors say what and where the problem was?
- Did save/undo/duplicate behavior feel safe?
- Did the preview look like the game rather than a fake simulator?
- Could I iterate on an encounter without opening Lua or JSON?

Also try switching records with unsaved work and deleting a definition. The Workbench must make the consequence clear before content is lost.
