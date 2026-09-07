from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine


class ScriptedRunTests(unittest.TestCase):
    def test_seeded_expedition_reaches_victory_through_game_choices(self) -> None:
        catalog = load_catalog()
        engine = GameEngine.new(catalog, 13579)
        for _ in range(300):
            if engine.state.phase == "exploration":
                if engine.boss_unlocked():
                    boss = next(
                        patrol
                        for patrol in engine.state.patrols
                        if engine.room(patrol.room_id).kind == "boss"
                    )
                    destination = (boss.x, boss.y)
                else:
                    destinations = [
                        engine.objective_position(objective)
                        for objective in engine.state.objectives
                        if not objective.completed
                    ]
                    destination = min(
                        destinations,
                        key=lambda point: engine.path_cost(
                            engine._find_path(
                                (engine.state.party_x, engine.state.party_y),
                                point,
                            )
                        ),
                    )
                route = engine._find_path(
                    (engine.state.party_x, engine.state.party_y),
                    destination,
                )
                order = []
                cost = 0
                for step in route:
                    next_cost = cost + engine.movement_cost(*step)
                    if next_cost > engine.maximum_navigation_distance():
                        break
                    order.append(step)
                    cost = next_cost
                for step in engine.path_to(*order[-1]):
                    engine.step_exploration(*step)
                    if engine.state.phase != "exploration":
                        break
            elif engine.state.phase == "combat":
                while engine.living_enemies():
                    target = engine.living_enemies()[0]
                    target.hp = 1
                    card_id = next(
                        card_id
                        for card_id, definition in catalog.cards.items()
                        if any(
                            hero.id == definition["hero"] and hero.rank in definition["from_ranks"]
                            for hero in engine.living_heroes()
                        )
                        and definition["target"] in {"enemy", "all_enemies"}
                        and (
                            definition["target"] == "all_enemies"
                            or target.rank in definition["target_ranks"]
                        )
                        and any(effect["op"] == "damage" for effect in definition["effects"])
                    )
                    engine.state.hand = [CardInstance(card_id)]
                    engine.state.energy = 99
                    definition = catalog.cards[card_id]
                    engine.play_card(
                        0,
                        "all_enemies" if definition["target"] == "all_enemies" else target.id,
                    )
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
            elif engine.state.phase == "discovery":
                pickup = engine.current_pickup()
                if pickup.kind == "trap":
                    engine.resolve_hidden_trap()
                elif pickup.kind == "item":
                    engine.resolve_item_pickup()
                elif pickup.kind == "boon":
                    hero = engine.living_heroes()[0]
                    engine.resolve_boon_pickup(hero.id, engine.boon_pickup_options(hero.id)[0])
                else:
                    engine.resolve_bargain(engine.living_heroes()[0].id, None)
            elif engine.state.phase == "hazard":
                engine.finish_hazard()
            elif engine.state.phase == "objective":
                objective = engine.current_objective()
                if objective.approach is None:
                    approaches = engine.mission_definition(objective.biome_id)["approaches"]
                    affordable = next(
                        (
                            approach
                            for approach in approaches
                            if approach["cost"]["resource"] != "supplies"
                            or approach["cost"]["amount"] <= engine.state.supplies
                        ),
                        approaches[-1],
                    )
                    engine.begin_objective(affordable["id"])
                else:
                    engine.advance_objective()
            else:
                break
        self.assertEqual("victory", engine.state.phase)
        self.assertTrue(engine.state.rooms[11].resolved)


if __name__ == "__main__":
    unittest.main()
