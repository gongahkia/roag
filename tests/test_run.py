from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine


class ScriptedRunTests(unittest.TestCase):
    def test_seeded_expedition_reaches_victory_through_game_choices(self) -> None:
        catalog = load_catalog()
        engine = GameEngine.new(catalog, 13579)
        for destination in (1, 2, 4, 5, 7, 8, 10, 11):
            engine.move_to(destination)
            while engine.state.phase not in {"exploration", "victory", "defeat"}:
                if engine.state.phase == "combat":
                    while engine.living_enemies():
                        target = engine.living_enemies()[0]
                        target.hp = 1
                        engine.state.hand = [CardInstance("snap_shot")]
                        engine.state.energy = 99
                        engine.play_card(0, target.id)
                elif engine.state.phase == "reward":
                    engine.choose_reward(None)
                elif engine.state.phase == "event":
                    event = catalog.events[engine.state.current_event or ""]
                    choice = next(
                        index
                        for index, item in enumerate(event["choices"])
                        if item.get("cost_supplies", 0) <= engine.state.supplies
                    )
                    engine.choose_event(choice)
                elif engine.state.phase == "service":
                    if engine.state.service_type == "camp":
                        engine.service("recover")
                    else:
                        engine.service("upgrade", 0)
            if engine.state.phase in {"victory", "defeat"}:
                break
        self.assertEqual("victory", engine.state.phase)
        self.assertTrue(engine.state.rooms[11].resolved)


if __name__ == "__main__":
    unittest.main()
