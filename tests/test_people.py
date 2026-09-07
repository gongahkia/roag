from __future__ import annotations

from pathlib import Path
import tempfile
import unittest

from jomon.actions import choose_courier, move, recruit_person
from jomon.people import adjacent_person, person_at
from jomon.save import load_game, save_game
from jomon.state import Position, create_world


class PhysicalTavernTests(unittest.TestCase):
    def test_six_household_adults_have_distinct_physical_seats(self):
        state = create_world("seated household")
        seats = [state.tavern_positions[person.id] for person in state.household]
        self.assertEqual(len(seats), 6)
        self.assertEqual(len(set(seats)), 6)
        self.assertTrue(all(person_at(state, seat) is not None for seat in seats))

    def test_aboard_movement_and_conversation_are_zero_time(self):
        state = create_world("zero time tavern")
        person = state.household[0]
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
        first, second = state.household[:2]
        first_seat = state.tavern_positions[first.id]
        choose_courier(state, first.id)
        state.position = Position(state.tavern_positions[second.id].x - 1, state.tavern_positions[second.id].y)
        old_position = state.position
        second_seat = state.tavern_positions[second.id]
        choose_courier(state, second.id)
        self.assertEqual(state.position, second_seat)
        self.assertEqual(state.tavern_positions[first.id], old_position)
        self.assertNotIn(second.id, state.tavern_positions)
        self.assertNotEqual(first_seat, second_seat)

    def test_voluntary_recruitment_and_position_persist(self):
        state = create_world("persistent visitor")
        visitor = next(person for person in state.visitors if state.visitor_status[person.id] == "visiting")
        state.trade_credit = 2
        result = recruit_person(state, visitor.id)
        self.assertTrue(result.changed)
        self.assertIn(visitor, state.household)
        self.assertEqual(state.visitor_status[visitor.id], "joined")
        self.assertTrue(visitor.recruited)
        self.assertTrue(all(visitor.id in person.relationships for person in state.household if person.id != visitor.id))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "save.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertEqual(len(loaded.household), 7)
        self.assertEqual(loaded.visitor_status[visitor.id], "joined")
        self.assertEqual(loaded.tavern_positions[visitor.id], state.tavern_positions[visitor.id])

