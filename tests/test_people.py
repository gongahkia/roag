from __future__ import annotations

from pathlib import Path
import tempfile
import unittest

from jomon.actions import choose_courier, depart, guard, move, recruit_person, return_to_jomon
from jomon.inventory import basic_courier_kit, equipped_item, weight_capacity
from jomon.vessel import TAVERN_MAP, VESSEL_LEVELS
from jomon.people import RECRUIT_REQUIREMENTS, adjacent_person, person_at
from jomon.save import load_game, save_game
from jomon.state import Position, create_world
from jomon.terminal import _overlay_lines
from jomon.world import JOMON_GANGPLANK


class PhysicalTavernTests(unittest.TestCase):
    def test_bar_preparation_tile_is_physically_reachable(self):
        start = Position(3, 4)
        main = VESSEL_LEVELS[0]
        target = next(
            Position(x, y)
            for y, row in enumerate(main)
            for x, tile in enumerate(row)
            if tile == "C"
        )
        frontier, seen = [start], {start}
        while frontier:
            point = frontier.pop()
            for candidate in (
                Position(point.x + 1, point.y), Position(point.x - 1, point.y),
                Position(point.x, point.y + 1), Position(point.x, point.y - 1),
            ):
                if candidate in seen:
                    continue
                if (
                    0 <= candidate.y < len(main)
                    and 0 <= candidate.x < len(main[candidate.y])
                    and main[candidate.y][candidate.x] not in {"#", "="}
                ):
                    seen.add(candidate)
                    frontier.append(candidate)
        self.assertIn(target, seen)

    def test_off_duty_household_adults_have_distinct_physical_seats(self):
        state = create_world("seated household")
        state.jomon_space = "tavern"
        off_duty = [person for person in state.household if person.id != state.active_courier_id]
        seats = [state.tavern_positions[person.id] for person in off_duty]
        self.assertEqual(len(seats), 5)
        self.assertEqual(len(set(seats)), 5)
        self.assertTrue(all(person_at(state, seat) is not None for seat in seats))

    def test_aboard_movement_and_conversation_are_zero_time(self):
        state = create_world("zero time tavern")
        state.jomon_space = "tavern"
        person = state.household[1]
        seat = state.tavern_positions[person.id]
        state.position = Position(seat.x - 2, seat.y)
        started = state.world_time
        result = move(state, 1, 0)
        self.assertTrue(result.changed)
        self.assertFalse(result.time_advanced)
        self.assertEqual(adjacent_person(state), person)
        self.assertEqual(state.world_time, started)

    def test_switching_couriers_physically_exchanges_places(self):
        state = create_world("physical exchange")
        state.jomon_space = "tavern"
        first, second = state.household[:2]
        state.position = Position(state.tavern_positions[second.id].x - 1, state.tavern_positions[second.id].y)
        old_position = state.position
        second_seat = state.tavern_positions[second.id]
        choose_courier(state, second.id)
        self.assertEqual(state.position, second_seat)
        self.assertEqual(state.tavern_positions[first.id], old_position)
        self.assertNotIn(second.id, state.tavern_positions)

    def test_voluntary_recruitment_and_position_persist(self):
        state = create_world("persistent visitor")
        visitor = next(person for person in state.visitors if state.visitor_status[person.id] == "visiting")
        region_id, markers, _ = RECRUIT_REQUIREMENTS[visitor.id]
        state.regions[region_id].changes[markers[0]] = True
        result = recruit_person(state, visitor.id)
        self.assertTrue(result.changed)
        self.assertIn(visitor, state.household)
        self.assertEqual(state.visitor_status[visitor.id], "joined")
        self.assertTrue(visitor.recruited)
        for slot, kind in basic_courier_kit(visitor).items():
            self.assertEqual(equipped_item(state, slot, visitor.id).kind, kind)
        self.assertTrue(all(visitor.id in person.relationships for person in state.household if person.id != visitor.id))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "save.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertEqual(len(loaded.household), 7)
        self.assertEqual(loaded.visitor_status[visitor.id], "joined")
        self.assertEqual(loaded.tavern_positions[visitor.id], state.tavern_positions[visitor.id])

    def test_each_recruit_requires_its_own_witnessed_condition(self):
        for visitor_id, (region_id, markers, witnessed) in RECRUIT_REQUIREMENTS.items():
            with self.subTest(visitor=visitor_id):
                state = create_world(f"personal term {visitor_id}")
                state.visitor_status[visitor_id] = "visiting"
                visitor = next(person for person in state.visitors if person.id == visitor_id)
                state.trade_credit = 99
                refused = recruit_person(state, visitor_id)
                self.assertFalse(refused.changed)
                state.regions[region_id].changes[markers[0]] = True
                accepted = recruit_person(state, visitor_id)
                self.assertTrue(accepted.changed)
                self.assertIn(witnessed, visitor.memories[-1])

    def test_two_returns_develop_only_the_embodied_courier(self):
        state = create_world("personal return development")
        courier, other = state.household[:2]
        initial_capacity = weight_capacity(state)
        for _ in range(2):
            self.assertTrue(depart(state).changed)
            state.position = state.region.landmarks["landing"]
            result = return_to_jomon(state)
            self.assertTrue(result.changed)
        self.assertIn(f"seasoned {courier.role}", courier.learned_techniques)
        self.assertNotIn(f"seasoned {other.role}", other.learned_techniques)
        self.assertEqual(weight_capacity(state), initial_capacity + 4)
        self.assertIn("four more weight capacity", result.message)

    def test_personal_practice_reinforces_guard_and_survives_save(self):
        state = create_world("personal practiced guard")
        courier = state.courier
        courier.learned_techniques.append(f"seasoned {courier.role}")
        depart(state)
        threat = state.threats[0]
        threat.position, threat.status = Position(state.position.x + 1, state.position.y), "engaged"
        state.gear, state.weapon = "rope", "billhook"
        morale = threat.morale
        result = guard(state)
        self.assertTrue(result.changed)
        self.assertLess(threat.morale, morale)
        _, lines = _overlay_lines(state, f"person:{courier.id}")
        self.assertIn(f"Learned practices: seasoned {courier.role}", lines)
        self.assertIn("Seasoned return effect: +4 weight capacity; reinforced guard.", lines)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "save.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertIn(f"seasoned {courier.role}", loaded.courier.learned_techniques)

    def test_a_recruited_adult_earns_the_same_personal_return_path(self):
        state = create_world("recruited return development")
        recruit = next(
            person for person in state.visitors
            if state.visitor_status[person.id] == "visiting"
        )
        region_id, markers, _ = RECRUIT_REQUIREMENTS[recruit.id]
        state.regions[region_id].changes[markers[0]] = True
        self.assertTrue(recruit_person(state, recruit.id).changed)
        self.assertTrue(choose_courier(state, recruit.id).changed)
        state.jomon_space, state.position = "vessel", JOMON_GANGPLANK
        for _ in range(2):
            self.assertTrue(depart(state).changed)
            state.position = state.region.landmarks["landing"]
            self.assertTrue(return_to_jomon(state).changed)
        self.assertIn(f"seasoned {recruit.role}", recruit.learned_techniques)
