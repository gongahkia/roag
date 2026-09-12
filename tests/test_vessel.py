from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from jomon.actions import (
    _advance_world,
    can_alter_objective,
    choose_courier,
    guard,
    interact,
    move,
    purchase_bar_drink,
)
from jomon.save import load_game, save_game
from jomon.state import Position, SocialIncident, create_world
from jomon.vessel import (
    BARTENDER_POSITION,
    DRINKS,
    LOWER_HATCH,
    MAIN_LOWER_HATCH,
    MAIN_UPPER_STAIR,
    TAVERN_ENTRANCE,
    TAVERN_EXIT,
    TAVERN_MAP,
    TABLE_PATRON_SEATS,
    TABLE_PLAYER_SEAT,
    TABLE_SURFACE,
    UPPER_STAIR,
    VESSEL_HEIGHT,
    VESSEL_LEVELS,
    VESSEL_WIDTH,
    resolve_social_incident,
    _walkable,
    vessel_vertical_destination,
)
from jomon.world import displayed_tile, is_walkable, sight_radius


class VesselMapAndScheduleTests(unittest.TestCase):
    def test_gaming_table_has_a_walkable_player_chair_and_blocking_surface(self):
        state = create_world("gaming furniture")
        state.jomon_space = "tavern"
        state.position = TABLE_PLAYER_SEAT
        self.assertEqual(len(TABLE_SURFACE), 75)
        self.assertEqual(TAVERN_MAP[TABLE_PLAYER_SEAT.y][TABLE_PLAYER_SEAT.x], "D")
        self.assertEqual(sum(row.count("D") for row in TAVERN_MAP), 1)
        self.assertEqual(displayed_tile(state, Position(31, 11)), "d")
        self.assertTrue(any(displayed_tile(state, seat) in {"a", "v"} for seat in TABLE_PATRON_SEATS))
        self.assertTrue(is_walkable(state, TABLE_PLAYER_SEAT))
        self.assertEqual(len(set(TABLE_PATRON_SEATS)), 13)
        self.assertTrue(all(_walkable("tavern", seat) for seat in TABLE_PATRON_SEATS))
        self.assertTrue(all(not is_walkable(state, point) for point in TABLE_SURFACE))
        self.assertEqual(interact(state).overlay, "tabletop")

    def test_off_duty_patron_walks_to_the_gaming_table_chair(self):
        state = create_world("game table walk")
        state.jomon_space = "tavern"
        state.position = TABLE_PLAYER_SEAT
        schedule = state.actor_schedules[state.household[1].id]
        schedule.position = Position(31, 4)
        schedule.next_boundary = state.world_time + 99
        start = schedule.position
        _advance_world(state)
        self.assertEqual(abs(schedule.position.x - start.x) + abs(schedule.position.y - start.y), 1)
        self.assertNotIn(schedule.position, TABLE_SURFACE)
        self.assertEqual(schedule.destination_area, "tavern")

    def test_saved_people_inside_new_furniture_are_reseated_without_time(self):
        state = create_world("old furniture save")
        state.jomon_space = "tavern"
        state.position = Position(32, 11)
        schedule = state.actor_schedules[state.household[1].id]
        schedule.position = schedule.destination = Position(31, 11)
        before = state.world_time
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "save.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.world_time, before)
        self.assertEqual(loaded.position, TABLE_PLAYER_SEAT)
        self.assertNotIn(loaded.actor_schedules[state.household[1].id].position, TABLE_SURFACE)

    def test_switching_courier_from_game_chair_leaves_it_available(self):
        state = create_world("switch at table")
        state.jomon_space = "tavern"
        state.position = TABLE_PLAYER_SEAT
        former = state.active_courier_id
        selected = state.household[1]
        self.assertTrue(choose_courier(state, selected.id).changed)
        self.assertIn(state.actor_schedules[former].position, TABLE_PATRON_SEATS)
        self.assertNotEqual(state.actor_schedules[former].position, TABLE_PLAYER_SEAT)
        self.assertTrue(is_walkable(state, TABLE_PLAYER_SEAT))

    def test_three_decks_and_tavern_have_valid_physical_links(self):
        self.assertEqual(set(VESSEL_LEVELS), {-1, 0, 1})
        self.assertTrue(all(len(rows) == VESSEL_HEIGHT for rows in VESSEL_LEVELS.values()))
        self.assertTrue(all(len(row) == VESSEL_WIDTH for rows in VESSEL_LEVELS.values() for row in rows))
        self.assertEqual(vessel_vertical_destination(LOWER_HATCH), MAIN_LOWER_HATCH)
        self.assertEqual(vessel_vertical_destination(MAIN_UPPER_STAIR), UPPER_STAIR)
        self.assertEqual(TAVERN_MAP[TAVERN_EXIT.y][TAVERN_EXIT.x], "+")
        self.assertEqual(VESSEL_LEVELS[0][TAVERN_ENTRANCE.y][TAVERN_ENTRANCE.x], "C")

    def test_functional_deck_stations_are_inspectable_in_place(self):
        state = create_world("deck stations")
        cases = (
            (Position(8, 15, -1), "ship-work:pump"),
            (Position(39, 5, -1), "station:workshop"),
            (Position(8, 5, 0), "ship-work:meal"),
            (Position(50, 5, 0), "ship-work:repair"),
            (Position(35, 10, 1), "station:helm"),
            (Position(60, 10, 1), "station:lookout"),
        )
        for position, overlay in cases:
            with self.subTest(overlay=overlay):
                state.position = position
                before = state.world_time
                self.assertEqual(interact(state).overlay, overlay)
                self.assertEqual(state.world_time, before)

    def test_schedule_is_deterministic_and_visible_actor_crosses_deck_link(self):
        first = create_world("schedule")
        second = create_world("schedule")
        actor_id = first.household[0].id
        for state in (first, second):
            state.jomon_space = "vessel"
            state.position = Position(13, 10, 0)
            schedule = state.actor_schedules[actor_id]
            schedule.area = "vessel:0"
            schedule.position = MAIN_UPPER_STAIR
            schedule.destination_area = "vessel:1"
            schedule.destination = Position(35, 10, 1)
            schedule.next_boundary = state.world_time + 99
            _advance_world(state)
        self.assertEqual(first.actor_schedules[actor_id], second.actor_schedules[actor_id])
        self.assertEqual(first.actor_schedules[actor_id].area, "vessel:1")
        self.assertEqual(first.actor_schedules[actor_id].position, UPPER_STAIR)

    def test_active_voyage_interrupts_ordinary_work_schedule(self):
        state = create_world("schedule alarm")
        state.voyage_kind = "raiders"
        state.voyage_status = "active"
        _advance_world(state)
        activities = {
            state.actor_schedules[person.id].activity for person in state.household
        }
        self.assertEqual(activities, {"defending cargo"})

    def test_saved_workers_never_occupy_required_control_glyphs(self):
        state = create_world("clear stations")
        actor_id = state.household[0].id
        schedule = state.actor_schedules[actor_id]
        schedule.area = schedule.destination_area = "vessel:1"
        schedule.position = schedule.destination = Position(28, 10, 1)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "save.json"
            save_game(state, path)
            loaded = load_game(path)
        repaired = loaded.actor_schedules[actor_id]
        self.assertNotEqual(repaired.position, Position(28, 10, 1))
        self.assertNotEqual(repaired.destination, Position(28, 10, 1))
        loaded.location = "jomon"
        loaded.jomon_space = "vessel"
        loaded.position = Position(28, 10, 1)
        self.assertEqual(interact(loaded).overlay, "route-chart")

    def test_offscreen_catch_up_is_bounded_and_no_load_time_passes(self):
        state = create_world("offscreen")
        state.jomon_space = "tavern"
        actor = state.household[0]
        schedule = state.actor_schedules[actor.id]
        schedule.area = "vessel:-1"
        schedule.next_boundary = state.world_time
        _advance_world(state)
        self.assertEqual(schedule.position, schedule.destination)
        before = state.world_time
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "save.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.world_time, before)


class BartenderAndIncidentTests(unittest.TestCase):
    def test_bartender_is_scheduled_and_stock_is_bounded(self):
        state = create_world("bar stock")
        schedule = state.actor_schedules[state.bartender.id]
        self.assertEqual(schedule.position, BARTENDER_POSITION)
        self.assertEqual(set(state.bartender_stock), set(DRINKS))
        self.assertTrue(all(0 <= amount <= 3 for amount in state.bartender_stock.values()))

    def test_drink_is_time_bearing_and_duration_clears(self):
        state = create_world("bar drink")
        state.location = "jomon"
        state.jomon_space = "tavern"
        state.trade_credit = 10
        drink_id = next(drink_id for drink_id, amount in state.bartender_stock.items() if amount)
        before = state.world_time
        result = purchase_bar_drink(state, drink_id, bottle=False)
        self.assertTrue(result.changed)
        self.assertEqual(state.world_time, before + 1)
        self.assertIn(drink_id, state.drink_effects)
        _advance_world(state, steps=DRINKS[drink_id].duration)
        self.assertNotIn(drink_id, state.drink_effects)

    def test_drinks_change_movement_guard_awareness_and_material_leverage(self):
        from jomon.state import TerrainStatus

        state = create_world("drink interactions")
        state.location = "region"
        state.position = state.region.landmarks["landing"]
        state.weapon = "staff"
        threat = state.threats[0]
        threat.position = Position(state.position.x + 5, state.position.y)
        threat.status = "engaged"
        state.drink_effects["hearth-ale"] = TerrainStatus("bar", 8, "noise")
        before_noise = state.noise
        self.assertTrue(move(state, 1, 0).time_advanced)
        self.assertGreater(state.noise, before_noise)
        state.drink_effects["miller-small-beer"] = TerrainStatus("bar", 8, "slow guard")
        guard(state)
        self.assertTrue(state.guarded_step)
        state.drink_effects["stillroom-cordial"] = TerrainStatus("bar", 8, "wary")
        self.assertTrue(can_alter_objective(state))
        plain = create_world("drink awareness")
        plain.location = "region"
        plain.position = plain.region.landmarks["landing"]
        plain_radius = sight_radius(plain)
        aware = create_world("drink awareness")
        aware.location = "region"
        aware.position = aware.region.landmarks["landing"]
        aware.drink_effects["reed-tonic"] = TerrainStatus("bar", 8, "dull")
        self.assertEqual(sight_radius(aware), max(3, plain_radius - 2))

    def test_causal_argument_and_fight_cannot_kill_offscreen(self):
        state = create_world("causal argument")
        state.jomon_space = "tavern"
        first, second = state.household[:2]
        state.pending_incident = SocialIncident(
            "test-incident", "argument", [first.id, second.id],
            "their standing is -1 after a cargo-credit disagreement", "pending", state.world_time,
        )
        changed, text = resolve_social_incident(state, "let-fight")
        self.assertTrue(changed)
        self.assertIn("bounded", text)
        self.assertGreaterEqual(first.health, 2)
        self.assertGreaterEqual(second.health, 2)
        self.assertTrue(state.chronicle)


if __name__ == "__main__":
    unittest.main()
