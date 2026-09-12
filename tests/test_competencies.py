"""Earned courier competencies have bounded, inspectable effects."""

import unittest
from unittest.mock import patch

from dumbest_dungeon.expedition import finish_match, start_match
from dumbest_dungeon.tabletop import patrons
from jomon.actions import negotiate
from jomon.inventory import equipped_item
from jomon.route_chart import route_preview
from jomon.state import Position, SocialIncident, StateError, Threat, create_world, game_state_from_dict, validate_state
from jomon.travel import choose_destination
from jomon.vessel import resolve_social_incident
from jomon.workshop import WORKBENCH, repair
from jomon.world import _remember_visible, sight_radius


class CompetencyTests(unittest.TestCase):
    def test_old_person_records_load_and_invalid_values_fail(self):
        state = create_world("competency migration")
        data = state.to_dict()
        for person in [*data["household"], *data["visitors"], data["merchant"], data["bartender"]]:
            for skill in ("speech", "wayfinding", "fieldcraft", "craft"):
                person.pop(skill, None)
        loaded = game_state_from_dict(data)
        self.assertEqual((loaded.courier.speech, loaded.courier.craft), (0, 0))
        loaded.courier.speech = 21
        with self.assertRaises(StateError):
            validate_state(loaded)

    def test_both_table_winners_gain_strategy_and_threshold_supplies(self):
        state = create_world("competency table")
        state.jomon_space = "tavern"
        patron = patrons(state)[0]
        state.courier.strategy = 5
        match = start_match(state, patron.id)
        self.assertEqual(match["teams"][0]["supplies"], 5)
        match["winner"] = 0
        finish_match(state)
        self.assertEqual(state.courier.strategy, 6)
        match = start_match(state, patron.id)
        match["winner"] = 0
        finish_match(state)
        self.assertEqual(state.courier.strategy, 7)
        match = start_match(state, patron.id)
        match["winner"] = 1
        finish_match(state)
        self.assertEqual(patron.strategy, 1)

    def test_speech_still_requires_terms_but_can_reach_one_more_group_member(self):
        state = create_world("competency speech")
        state.location = "region"
        state.position = Position(40, 25)
        state.gear = None
        state.courier.speech = 5
        state.threats = [Threat(str(index), f"ward {index}", "pursuer", Position(41 + index, 25), 4, 4,
                                status="engaged", group="one") for index in range(3)]
        self.assertFalse(negotiate(state).time_advanced)
        state.gear = "trade seals"
        self.assertTrue(negotiate(state).time_advanced)
        self.assertTrue(all(threat.status == "negotiated" for threat in state.threats))
        self.assertEqual(state.courier.speech, 6)

    def test_mediation_speech_bonus_and_growth(self):
        state = create_world("competency mediation")
        first, second = state.household[:2]
        first.relationships[second.id] = second.relationships[first.id] = 0
        state.courier.speech = 10
        state.pending_incident = SocialIncident("argument", "dispute", [first.id, second.id], "work", "pending", 0)
        self.assertTrue(resolve_social_incident(state, "mediate")[0])
        self.assertEqual(first.relationships[second.id], 2)
        self.assertEqual(state.courier.speech, 11)

    def test_wayfinding_shortens_voyage_and_new_route_teaches(self):
        state = create_world("competency route")
        state.courier.wayfinding = 5
        self.assertTrue(any("3 actions" in line for line in route_preview(state, "reed-anchor")))
        before = state.world_time
        with patch("jomon.travel.voyage_for", return_value=None):
            changed, message = choose_destination(state, "reed-anchor")
        self.assertTrue(changed)
        self.assertEqual(state.world_time - before, 3)
        self.assertIn("3 action-clock measures", message)
        self.assertEqual(state.courier.wayfinding, 6)

    def test_fieldcraft_extends_sight_and_new_ground_teaches(self):
        state = create_world("competency fieldcraft")
        state.location = "region"
        state.position = Position(40, 25)
        initial = sight_radius(state)
        state.courier.fieldcraft = 5
        self.assertEqual(sight_radius(state), initial + 1)
        state.region.seen = [f"{x},{y},0" for y in range(8) for x in range(10)][:79]
        _remember_visible(state, {Position(40, 25)})
        self.assertEqual(state.courier.fieldcraft, 6)

    def test_workshop_repairs_improve_without_changing_material_cost(self):
        state = create_world("competency craft")
        state.position = WORKBENCH
        state.trade_credit = 10
        state.courier.craft = 5
        target = equipped_item(state, "readied")
        target.condition = 20
        self.assertTrue(repair(state, target.id)[0])
        self.assertEqual((target.condition, state.trade_credit, state.courier.craft), (60, 8, 6))


if __name__ == "__main__":
    unittest.main()
