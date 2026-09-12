"""New couriers can be named and point-bought without rerolling the household."""

import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from jomon.actions import interact
from jomon.character import (
    ANCESTRIES, ORIGINS, TRAITS, apply_character_spec, character_sheet,
    clean_name, default_allocation, effective_competency,
)
from jomon.character_ui import run_character_creation
from jomon.inventory import weight_capacity
from jomon.save import load_game, save_game
from jomon.state import StateError, create_world, game_state_from_dict, validate_state
from jomon.terminal import _overlay_lines, dialogue_choices
from jomon.vessel import JOMON_GANGPLANK
from jomon.world import sight_radius


class CharacterCreationTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("named courier")

    def test_role_presets_spend_the_same_point_budgets(self):
        for person in self.state.household:
            attributes, skills = default_allocation(person.role)
            self.assertEqual(sum(attributes.values()), 44)
            self.assertEqual(sum(skills.values()), 5)
            self.assertTrue(all(4 <= value <= 10 for value in attributes.values()))

    def test_name_and_point_budget_fail_before_mutating_world(self):
        attributes, skills = default_allocation(self.state.household[0].role)
        before = self.state.to_dict()
        with self.assertRaisesRegex(ValueError, "already has that name"):
            apply_character_spec(self.state, crew_index=0, name=self.state.household[1].name,
                                 ancestry=ANCESTRIES[0], origin=ORIGINS[0], trait=next(iter(TRAITS)),
                                 attributes=attributes, competencies=skills)
        self.assertEqual(self.state.to_dict(), before)
        attributes["strength"] += 1
        with self.assertRaisesRegex(ValueError, "eight attribute points"):
            apply_character_spec(self.state, crew_index=0, name="Mira Vale",
                                 ancestry=ANCESTRIES[0], origin=ORIGINS[0], trait=next(iter(TRAITS)),
                                 attributes=attributes, competencies=skills)
        self.assertEqual(self.state.to_dict(), before)
        with self.assertRaises(ValueError):
            clean_name("\x1bBad")

    def test_selected_named_courier_keeps_role_equipment_and_zero_time(self):
        chosen = self.state.household[1]
        original_bonds = chosen.relationships.copy()
        attributes, skills = default_allocation(chosen.role)
        person = apply_character_spec(self.state, crew_index=1, name="Mira Vale",
                                      ancestry=ANCESTRIES[1], origin=ORIGINS[1], trait="wayfarer",
                                      attributes=attributes, competencies=skills)
        self.assertIs(person, chosen)
        self.assertEqual((person.name, person.role, person.relationships), ("Mira Vale", "pilot", original_bonds))
        self.assertEqual((self.state.position, self.state.world_time, self.state.expedition_count), (JOMON_GANGPLANK, 0, 0))
        self.assertIn("Mira Vale", self.state.messages[-1])
        self.assertEqual(self.state.actor_schedules[person.id].position, JOMON_GANGPLANK)
        self.assertTrue(person.character_specified)
        self.assertEqual(person.max_health, 12)
        validate_state(self.state)
        self.assertTrue(interact(self.state).time_advanced)
        self.assertEqual(self.state.location, "region")

    def test_profile_modifiers_affect_existing_sight_load_and_competencies(self):
        plain_capacity = weight_capacity(self.state)
        attributes, skills = default_allocation(self.state.household[0].role)
        attributes["perception"] += 2
        attributes["intellect"] -= 2
        person = apply_character_spec(self.state, crew_index=0, name="Nera Reed",
                                      ancestry=ANCESTRIES[0], origin="greenwold", trait="observant",
                                      attributes=attributes, competencies=skills)
        self.assertEqual(weight_capacity(self.state), plain_capacity + 2)
        self.assertGreater(effective_competency(person, "fieldcraft"), person.fieldcraft)
        self.state.location = "region"
        from jomon.state import Position

        self.state.position = Position(40, 25)
        radius = sight_radius(self.state)
        person.character_specified = False
        self.assertGreater(radius, sight_radius(self.state))
        person.character_specified = True

    def test_character_sheet_and_save_roundtrip(self):
        attributes, skills = default_allocation(self.state.household[0].role)
        apply_character_spec(self.state, crew_index=0, name="Nera Reed",
                             ancestry="Tidekin", origin=ORIGINS[0], trait="diplomatic",
                             attributes=attributes, competencies=skills)
        sheet = character_sheet(self.state.courier)
        self.assertTrue(any("Strength" in row and "carrying" in row for row in sheet))
        self.assertTrue(any("Speech" in row and "effective" in row for row in sheet))
        title, rows = _overlay_lines(self.state, f"character-sheet:{self.state.courier.id}")
        self.assertIn("CHARACTER SHEET", title)
        self.assertEqual(rows, sheet)
        self.assertTrue(any(option.key == "C" for option in dialogue_choices(self.state, f"person:{self.state.household[1].id}")))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "jomon.json"
            save_game(self.state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.courier.name, "Nera Reed")
        self.assertEqual(loaded.courier.ancestry, "Tidekin")
        self.assertEqual(loaded.courier.attributes, attributes)

    def test_fantasy_peoples_have_distinct_persistent_mechanics(self):
        attributes, skills = default_allocation(self.state.household[0].role)
        person = apply_character_spec(self.state, crew_index=0, name="Nera Reed",
                                      ancestry="Human", origin="hearthford", trait="methodical",
                                      attributes=attributes, competencies=skills)
        human_strategy = effective_competency(person, "strategy")
        person.ancestry = "Reedfolk"
        self.assertEqual(effective_competency(person, "strategy"), human_strategy - 1)
        self.assertEqual(effective_competency(person, "fieldcraft"), person.fieldcraft + 1)
        self.state.location = "region"
        self.state.active_region_id = "greywash"
        reed_sight = sight_radius(self.state)
        person.ancestry = "Human"
        self.assertEqual(reed_sight, sight_radius(self.state) + 1)
        person.ancestry = "Stonefolk"
        stone_craft = effective_competency(person, "craft")
        stone_capacity = weight_capacity(self.state)
        person.ancestry = "Human"
        self.assertEqual(stone_craft, effective_competency(person, "craft") + 1)
        self.assertEqual(stone_capacity, weight_capacity(self.state) + 4)
        person.ancestry = "Tidekin"
        tide_wayfinding = effective_competency(person, "wayfinding")
        person.ancestry = "Human"
        self.assertEqual(tide_wayfinding, effective_competency(person, "wayfinding") + 1)
        person.ancestry = "Tidekin"
        self.state.weather = "river fog"
        tide_sight = sight_radius(self.state)
        person.ancestry = "Human"
        self.assertEqual(tide_sight, sight_radius(self.state) + 1)
        person.ancestry = "Tidekin"
        self.assertTrue(any("fog" in row for row in character_sheet(person)))
        validate_state(self.state)

    def test_named_visitors_include_playable_fantasy_peoples(self):
        by_id = {person.id: person for person in self.state.visitors}
        self.assertEqual(by_id["recruit-maelin"].ancestry, "Tidekin")
        self.assertEqual(by_id["recruit-orra"].ancestry, "Reedfolk")
        self.assertEqual(by_id["recruit-teren"].ancestry, "Stonefolk")
        self.assertEqual(effective_competency(by_id["recruit-orra"], "fieldcraft"),
                         by_id["recruit-orra"].fieldcraft + 1)
        title, rows = _overlay_lines(self.state, "person:recruit-maelin")
        self.assertIn("Tidekin", rows[0])

    def test_old_people_get_safe_defaults_and_corrupt_profiles_fail(self):
        data = self.state.to_dict()
        for person in [*data["household"], *data["visitors"], data["merchant"], data["bartender"]]:
            for key in ("attributes", "ancestry", "origin", "trait", "character_specified"):
                person.pop(key)
        loaded = game_state_from_dict(data)
        self.assertEqual(loaded.courier.attributes["strength"], 6)
        self.assertFalse(loaded.courier.character_specified)
        loaded.courier.attributes["strength"] = 99
        with self.assertRaises(StateError):
            validate_state(loaded)

    def test_keyboard_creation_can_choose_crew_and_name(self):
        class Screen:
            def __init__(self):
                self.keys = iter((ord("l"), ord("j"), 13, ord("j"), ord("l"), ord("s")))

            def getmaxyx(self):
                return 24, 80

            def erase(self):
                pass

            def refresh(self):
                pass

            def addnstr(self, row, col, value, count, attr=0):
                pass

            def getch(self):
                return next(self.keys)

            def getstr(self, row, col, count):
                return b"Mira Vale"

        with patch("curses.echo"), patch("curses.noecho"), patch("curses.curs_set"):
            self.assertTrue(run_character_creation(Screen(), self.state))
        self.assertEqual(self.state.courier.name, "Mira Vale")
        self.assertEqual(self.state.courier.role, "pilot")
        self.assertEqual(self.state.courier.ancestry, "Reedfolk")
        validate_state(self.state)


if __name__ == "__main__":
    unittest.main()
