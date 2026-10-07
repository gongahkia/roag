from __future__ import annotations

import copy
import unittest

from jomon.chemistry import UNKNOWN_REAGENT_ID, predicted_reactions
from jomon.inventory import auto_place, create_item
from jomon.state import create_world, game_state_from_dict


class ChemistryIdentityTests(unittest.TestCase):
    def _flask_state(self):
        state = create_world("chemistry identity save")
        flask = create_item(state, "field flask", "legacy chemistry fixture")
        self.assertTrue(auto_place(state, flask.id, "pack", owner_id=state.active_courier_id))
        return state, flask

    def test_known_legacy_reagent_contents_keep_the_stable_identity(self):
        state, flask = self._flask_state()
        flask.contents = {"healing herb": 1, "spring water": 1}
        loaded = game_state_from_dict(state.to_dict())
        restored = next(item for item in loaded.items if item.id == flask.id)
        self.assertEqual(restored.contents, {"healing herb": 1, "spring water": 1})
        self.assertEqual(predicted_reactions(restored.contents), ["healing draft"])

    def test_unknown_legacy_reagent_contents_are_inert_not_guessed(self):
        state, flask = self._flask_state()
        flask.contents = {"unrecorded old residue": 1, "spring water": 1}
        loaded = game_state_from_dict(state.to_dict())
        restored = next(item for item in loaded.items if item.id == flask.id)
        self.assertEqual(restored.contents, {UNKNOWN_REAGENT_ID: 1, "spring water": 1})
        self.assertEqual(predicted_reactions(restored.contents), [])

    def test_formula_journals_keep_known_ids_and_drop_unknown_legacy_prose(self):
        state, _flask = self._flask_state()
        state.courier.known_formulas = ["healing draft", "unrecorded old formula"]
        state.household_formulas = ["breath tonic", "unrecorded old formula"]
        loaded = game_state_from_dict(state.to_dict())
        self.assertEqual(loaded.courier.known_formulas, ["healing draft"])
        self.assertEqual(loaded.household_formulas, ["breath tonic"])

    def test_new_round_trip_preserves_chemistry_ids_exactly(self):
        state, flask = self._flask_state()
        flask.contents = {"tree resin": 1, "cinder salt": 1}
        state.courier.known_formulas = ["flame bloom"]
        state.household_formulas = ["flame bloom"]
        loaded = game_state_from_dict(copy.deepcopy(state.to_dict()))
        restored = next(item for item in loaded.items if item.id == flask.id)
        self.assertEqual(restored.contents, flask.contents)
        self.assertEqual(loaded.courier.known_formulas, ["flame bloom"])
        self.assertEqual(loaded.household_formulas, ["flame bloom"])


if __name__ == "__main__":
    unittest.main()
