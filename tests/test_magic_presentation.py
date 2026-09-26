from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from jomon.catalog import ContentPackError, bundled_default_pack, load_content_pack
from jomon.magic import SPELLS, cast, spell_status
from jomon.state import Position, create_world, game_state_from_dict
from tests.test_content_packs import ROOT, alternate_pack


def magic_snapshot(environment: dict[str, str]) -> dict[str, object]:
    script = '''
import json
import jomon.actions
from jomon.magic import cast, spell_status
from jomon.state import Position, Threat, create_world
jomon.actions._advance_world = lambda *args, **kwargs: None
state = create_world("magic-presentation-proof")
state.location = "region"
state.position = Position(30, 23)
state.courier.mana = state.courier.max_mana = 10
state.courier.known_spells = ["wind-nudge", "ember-spark"]
target = Threat("fixture-threat", "fixture target", "melee", Position(32, 23), 8, 8, status="watching")
state.threats = [target]
changed, message = cast(state, "wind-nudge", target.position)
wind_position = [target.position.x, target.position.y, target.position.z]
target.position = Position(32, 23)
target.status = "watching"
state.courier.known_spells.append("ash-shot")
ash_changed, ash_message = cast(state, "ash-shot", target.position)
status = spell_status(state, "ember-spark", Position(37, 23))
print(json.dumps({"message": message, "ash_message": ash_message, "status": status, "mechanics": {
    "spell": "wind-nudge", "mana": state.courier.mana, "wind_position": wind_position,
    "intent_id": target.intent_id, "status": target.status, "health": target.health,
}, "intent": target.intent}))
'''
    result = subprocess.run([sys.executable, "-c", script], cwd=ROOT, env=environment, text=True, capture_output=True, check=False)
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class MagicPresentationTests(unittest.TestCase):
    def test_default_pack_preserves_spell_and_cast_text(self):
        from jomon.magic_presentation import spell_description, spell_display_name

        self.assertEqual(spell_display_name("ember-spark"), "Ember spark")
        self.assertEqual(spell_description(SPELLS["ember-spark"]), "fire 1; radius 0; 1 mana, reach 5")
        state = create_world("magic-default-text")
        state.courier.known_spells = ["ember-spark"]
        state.courier.mana = 0
        self.assertEqual(spell_status(state, "ember-spark", state.position), (False, "needs 1 mana; 0 remains"))

    def test_alternate_pack_changes_magic_words_not_mechanics(self):
        default = magic_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = magic_snapshot(environment)
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("FIXTURE", alternate["message"])
        self.assertEqual(alternate["intent"], "fixture wind displacement")
        self.assertIn("Fixture ash shot", alternate["ash_message"])
        self.assertEqual(alternate["mechanics"]["intent_id"], "intent.magic.push")

    def test_magic_defeat_uses_a_stable_intent_identity(self):
        import jomon.actions
        from jomon.state import Threat

        original = jomon.actions._advance_world
        jomon.actions._advance_world = lambda *args, **kwargs: None
        try:
            state = create_world("magic-presentation-proof")
            state.location = "region"
            state.position = Position(30, 23)
            state.courier.mana = state.courier.max_mana = 10
            state.courier.known_spells = ["magic-missile"]
            target = Threat("magic-target", "magic target", "melee", Position(32, 23), 1, 1, status="watching")
            state.threats = [target]
            self.assertTrue(cast(state, "magic-missile", target.position)[0])
            self.assertEqual(target.intent_id, "intent.magic.defeated")
            self.assertEqual(target.status, "defeated")
        finally:
            jomon.actions._advance_world = original

    def test_magic_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "magic_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing": lambda rows: rows.pop("magic.spell.ember-spark.name"),
                "unknown": lambda rows: rows.update({"magic.spell.extra.name": "extra"}),
                "empty": lambda rows: rows.update({"magic.cast.result": ""}),
                "unknown placeholder": lambda rows: rows.update({"magic.cast.result": "cast {other}"}),
                "missing placeholder": lambda rows: rows.update({"magic.cast.result": "cast {courier}"}),
                "malformed": lambda rows: rows.update({"magic.status.mana": "{"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name=name):
                    document = json.loads(original)
                    mutate(document["text"])
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*magic_text\.json"):
                        load_content_pack(root)
            source.write_text(original.replace('"magic.status.mana"', '"magic.status.ready"', 1), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*magic_text\.json"):
                load_content_pack(root)

    def test_legacy_magic_intents_recover_only_default_identities(self):
        state = create_world("magic-intent-save")
        threat = state.threats[0]
        raw = state.to_dict()
        raw["region_threats"]["hearthford"][0].pop("intent_id", None)
        raw["region_threats"]["hearthford"][0]["intent"] = "displaced by wind magic"
        self.assertEqual(game_state_from_dict(raw).threats[0].intent_id, "intent.magic.push")
        raw["region_threats"]["hearthford"][0]["intent"] = "unrecognised spell wording"
        self.assertEqual(game_state_from_dict(raw).threats[0].intent_id, "intent.legacy.unknown")
        state.threats[0].intent_id = "intent.magic.bind"
        state.threats[0].intent = "bound in enchanted reeds; loses a turn breaking free"
        self.assertEqual(game_state_from_dict(state.to_dict()).threats[0].intent_id, "intent.magic.bind")


if __name__ == "__main__":
    unittest.main()
