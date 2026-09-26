from __future__ import annotations

import json
from pathlib import Path
import tempfile
import unittest

from jomon.people import normalize_personal_return_state, record_personal_return
from jomon.state import combat_seed_identity, create_world, game_state_from_dict


class ContentIdentity14ETests(unittest.TestCase):
    def test_combat_seed_identity_ignores_rendered_threat_name(self):
        state = create_world("14e combat identity")
        for threat in state.threats[:3]:
            token = combat_seed_identity(threat)
            threat.name = "rewritten selected-pack threat"
            self.assertEqual(combat_seed_identity(threat), token)

    def test_personal_return_recovery_is_exact_and_runtime_is_state_only(self):
        state = create_world("14e personal recovery")
        person = state.household[0]
        person.memories.append("Courier return: totally unrelated prose")
        normalize_personal_return_state(state)
        self.assertNotIn(f"personal-return:{person.id}", state.vessel_changes)
        person.memories.append("Courier return: survived working passage through Hearthford.")
        normalize_personal_return_state(state)
        self.assertEqual(state.vessel_changes[f"personal-return:{person.id}"], 1)
        person.memories.append("Courier return: totally unrelated prose")
        record_personal_return(state, person, "hearthford")
        self.assertEqual(state.vessel_changes[f"personal-return:{person.id}"], 2)

    def test_people_catalog_prose_is_not_runtime_authority(self):
        # A new state is built from the validated selected presentation fields;
        # raw people.json text no longer supplies household or visitor wording.
        state = create_world("14e people presentation")
        self.assertTrue(state.household[0].name)
        self.assertTrue(state.visitors[0].background)
        self.assertTrue(state.region.condition)

    def test_normal_load_does_not_add_zero_personal_return_fields(self):
        state = create_world("14e no zero migration")
        payload = state.to_dict()
        restored = game_state_from_dict(payload)
        self.assertEqual(restored.to_dict(), payload)
