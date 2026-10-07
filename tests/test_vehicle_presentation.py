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


class VehiclePresentationTests(unittest.TestCase):
    def test_default_pack_preserves_vehicle_words(self):
        pack = bundled_default_pack()
        self.assertEqual(pack.vehicle_presentation("vehicle.tug.name").text, "Jomon's steam tug")
        self.assertEqual(pack.vehicle_presentation("vehicle.board.tug.success").text, "You board the steam tug; Jomon lies at J and the regional shore at L.")
        self.assertEqual(pack.vehicle_presentation("vehicle.navigate.result").text, "{vehicle} travels {travelled} tile{suffix}; {fuel}/{capacity} {resource} remains.")

    def test_invalid_vehicle_text_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "vehicle_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing": lambda rows: rows.pop("vehicle.tug.name"),
                "unknown": lambda rows: rows.update({"vehicle.extra": "unexpected"}),
                "malformed": lambda rows: rows.update({"vehicle.navigate.result": "{"}),
                "unknown placeholder": lambda rows: rows.update({"vehicle.navigate.result": "{other}"}),
                "missing placeholder": lambda rows: rows.update({"vehicle.navigate.result": "vehicle result"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name=name):
                    document = json.loads(original)
                    mutate(document["text"])
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*vehicle_text\.json"):
                        load_content_pack(root)

    def test_alternate_pack_changes_vehicle_words_not_mechanics(self):
        script = '''
import json
from jomon.state import create_world
from jomon.vehicles import SPECS, board_tug, navigate
state = create_world("vehicle-pack-proof")
board = board_tug(state)
tug = state.vehicles["tug"]
move = navigate(state, 1, 0)
print(json.dumps({"mechanics": [tug.id, tug.region_id, tug.position.x, tug.position.y, tug.fuel, SPECS[tug.id]["capacity"], tug.condition, tug.travelled, state.position.x, state.position.y, move.time_advanced], "presentation": [board.message, move.message]}))
'''
        def snapshot(path: str | None):
            environment = dict(os.environ)
            if path:
                environment["JOMON_CONTENT_PACK"] = path
            result = subprocess.run([sys.executable, "-c", script], cwd=ROOT, env=environment, text=True, capture_output=True, check=False)
            self.assertEqual(result.returncode, 0, result.stderr)
            return json.loads(result.stdout)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            default, alternate = snapshot(None), snapshot(str(root))
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertNotEqual(default["presentation"], alternate["presentation"])
        self.assertIn("Fixture", alternate["presentation"][0])
        self.assertIn("Fixture", alternate["presentation"][1])


if __name__ == "__main__":
    unittest.main()
