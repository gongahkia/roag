from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine


class ScriptedRunTests(unittest.TestCase):
    def test_seeded_expedition_reaches_victory_through_game_choices(self) -> None:
        catalog = load_catalog()
        engine = GameEngine.new(catalog, 13579)
        for _ in range(100):
            if engine.state.phase == "exploration":
                route = engine._find_path(
                    (engine.state.party_x, engine.state.party_y),
                    engine.room_position(11),
                )
                order = route[: int(catalog.balance["maximum_navigation_distance"])]
                for step in engine.path_to(*order[-1]):
                    engine.step_exploration(*step)
                    if engine.state.phase != "exploration":
                        break
            elif engine.state.phase == "combat":
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
            else:
                break
        self.assertEqual("victory", engine.state.phase)
        self.assertTrue(engine.state.rooms[11].resolved)


if __name__ == "__main__":
    unittest.main()
