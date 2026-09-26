from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from jomon.catalog import ContentPackError, bundled_default_pack, load_content_pack
from tests.test_content_packs import ROOT, alternate_pack


class ShipCrisisPresentationTests(unittest.TestCase):
    def test_default_pack_preserves_representative_crisis_text(self):
        pack = bundled_default_pack()
        self.assertEqual(pack.ship_crisis_presentation("crisis.raiders.title").text, "Cargo-rail raiders")
        self.assertEqual(pack.ship_crisis_presentation("crisis.choice.raiders.repel").text, "Repel with readied reach")
        self.assertEqual(pack.ship_crisis_presentation("crisis.finish.abandon").text, "The courier orders withdrawal; {loss}; hull loses two integrity. Existing fire and water remain for repair.")

    def test_invalid_crisis_text_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "ship_crisis_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing": lambda rows: rows.pop("crisis.raiders.title"),
                "unknown": lambda rows: rows.update({"crisis.extra": "unexpected"}),
                "empty": lambda rows: rows.update({"crisis.raiders.title": ""}),
                "unknown placeholder": lambda rows: rows.update({"crisis.finish.abandon": "loss {other}"}),
                "missing placeholder": lambda rows: rows.update({"crisis.finish.abandon": "withdrawal"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name=name):
                    document = json.loads(original)
                    mutate(document["text"])
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*ship_crisis_text\.json"):
                        load_content_pack(root)
            with self.subTest(name="duplicate key"):
                source.write_text('{"text": {"crisis.raiders.title": "one", "crisis.raiders.title": "two"}}', encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*ship_crisis_text\.json"):
                    load_content_pack(root)

    def test_old_rendered_crisis_intent_loads_to_its_stable_identity(self):
        from jomon.ship_crises import begin_deck
        from jomon.state import create_world, game_state_from_dict
        from jomon.travel import choose_destination

        state = create_world("crisis-intent-save")
        self.assertTrue(choose_destination(state, "reed-anchor", forced_voyage="raiders")[0])
        self.assertTrue(begin_deck(state)[0])
        raw = state.to_dict()
        raw["vessel_threats"][0].pop("intent_id")
        loaded = game_state_from_dict(raw)
        self.assertEqual(loaded.vessel_threats[0].intent_id, "intent.crisis.observe")
        self.assertEqual(loaded.vessel_threats[0].intent, "observes the deck before committing")

    def test_alternate_crisis_text_changes_words_not_deck_mechanics(self):
        with tempfile.TemporaryDirectory() as directory:
            alternate = alternate_pack(Path(directory) / "fixture")
            script = '''
import json
from jomon.state import create_world
from jomon.travel import choose_destination
from jomon.ship_crises import begin_deck, choices, abandon_deck
state = create_world("crisis-pack-proof")
choose_destination(state, "reed-anchor", forced_voyage="raiders")
pre = choices(state)
opened, alarm = begin_deck(state)
actors = [(actor.id, actor.profile, actor.role, actor.position.x, actor.position.y, actor.position.z, actor.intent_id, actor.status, actor.health) for actor in state.vessel_threats]
finished = abandon_deck(state)
print(json.dumps({"opened": opened, "choices": pre, "actors": actors, "integrity": state.vessel_integrity, "voyage": state.voyage_status, "route": state.route_current_node, "alarm": alarm, "finished": finished}))
'''
            def snapshot(path: str | None):
                environment = dict(os.environ)
                if path:
                    environment["JOMON_CONTENT_PACK"] = path
                result = subprocess.run([sys.executable, "-c", script], cwd=ROOT, env=environment, text=True, capture_output=True, check=False)
                self.assertEqual(result.returncode, 0, result.stderr)
                return json.loads(result.stdout)
            default, fixture = snapshot(None), snapshot(str(alternate))
            self.assertEqual(default["opened"], fixture["opened"])
            self.assertEqual([(key, semantic) for key, _label, semantic in default["choices"]], [(key, semantic) for key, _label, semantic in fixture["choices"]])
            self.assertEqual(default["actors"], fixture["actors"])
            self.assertEqual((default["integrity"], default["voyage"], default["route"]), (fixture["integrity"], fixture["voyage"], fixture["route"]))
            self.assertNotEqual(default["choices"], fixture["choices"])
            self.assertNotEqual(default["alarm"], fixture["alarm"])
            self.assertNotEqual(default["finished"], fixture["finished"])


if __name__ == "__main__":
    unittest.main()
