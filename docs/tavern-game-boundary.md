# Tavern-game code boundary

The in-world tables share one Roag integration layer, not one rules engine:

- `roag/tavern_games.py`: active-game occupancy, eligible named adults, chair assignment, NPC credit, and cross-game validation.
- `roag/tavern_draw.py` and `roag/tavern_dice.py`: each game's rules, random stream, payout, and own save validator.
- Their matching `_ui.py` files: table-specific keyboard flow and presentation. `roag/tavern_games_ui.py` contains only reusable ASCII drawing primitives.
- `roag/actions.py`: physical chair interactions; `roag/terminal.py`: launch dispatch; `roag/vessel.py`: the physical tavern map and ordinary NPC schedules.
- `roag/dumbest_dungeon/`: the Dullest Dungeon rules, content, and presentation in their own tavern-game subpackage.

Only one of the three saved game sessions may be active at once. An active game includes a completed result that has not yet been cleared. Roag blocks switching tables at the chair, and save validation rejects overlapping sessions. Each game's own start function also checks the other sessions where that function is Roag-owned.

NPC credit is accessed through `npc_credit` and `change_npc_credit`, including Quay Bones prizes. Its persisted value remains in `tavern_draw["bankrolls"]` so existing Roag saves load without a schema migration; the location is a compatibility detail, not an invitation for another game to read the draw ledger directly. The player's credit remains the existing `trade_credit`. Quay Bones has its own finite prize purse.

Keep new rules and renderers in separate modules. Add a saved session slot to `GAME_SLOTS`, route its physical chair through `actions.interact`, and include its validation in `state.validate_state`. Test table exclusion, save/reload, eligible seating, credit conservation or purse accounting, and courier switching. Do not move or edit Dullest Dungeon's internal engine to add a Roag tavern game.
