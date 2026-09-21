from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from jomon.catalog import ContentPackError, load_content_pack
from jomon.travel_presentation import travel_format
from tests.test_content_packs import ROOT, alternate_pack


class TravelPresentationTests(unittest.TestCase):
    def test_default_text_remains_exact(self):
        self.assertEqual(travel_format("travel.destination.arrival", destination="Reed Anchor", duration=4), "Jomon reaches Reed Anchor after 4 measures of travel.")
        self.assertEqual(travel_format("travel.result.raiders.repel"), "readied reach drives the cargo thieves back before they can disengage")

    def test_invalid_travel_entries_are_rejected(self):
        cases = (
            (lambda text: text.pop("travel.frame.depart"), "missing required travel keys"),
            (lambda text: text.__setitem__("travel.unknown", "Unknown"), "unknown travel keys"),
            (lambda text: text.__setitem__("travel.destination.arrival", ""), "must be a non-empty string"),
        )
        for change, reason in cases:
            with self.subTest(reason=reason), tempfile.TemporaryDirectory() as directory:
                root = alternate_pack(Path(directory) / "fixture")
                source = root / "travel_text.json"
                document = json.loads(source.read_text(encoding="utf-8"))
                change(document["text"])
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, reason):
                    load_content_pack(root)

    def test_alternate_pack_changes_text_not_raider_mechanics(self):
        script = (
            "import json; from jomon.actions import choose_weapon; from jomon.state import create_world; "
            "from jomon.travel import choose_destination, resolve_voyage, travel_animation_frames; "
            "state=create_world('travel-presentation-proof'); state.weapon='pike'; "
            "frames=travel_animation_frames(state,'hearthford','reed-anchor'); before=(state.travel_count,state.world_time,state.route_current_node); "
            "opened=choose_destination(state,'reed-anchor',forced_voyage='raiders'); result=resolve_voyage(state,'repel'); "
            "print(json.dumps({'frames':[f.text for f in frames], 'opened':opened, 'result':result, 'mechanics':[before,state.travel_count,state.world_time,state.route_current_node,state.voyage_status,state.vessel_integrity,sorted(state.vessel_cargo)]}))"
        )
        default = subprocess.run([sys.executable, "-c", script], cwd=ROOT, text=True, capture_output=True, check=True)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            alternate = subprocess.run([sys.executable, "-c", script], cwd=ROOT, env=dict(os.environ, JOMON_CONTENT_PACK=str(root)), text=True, capture_output=True, check=True)
        first, second = json.loads(default.stdout), json.loads(alternate.stdout)
        self.assertEqual(first["mechanics"], second["mechanics"])
        self.assertNotEqual(first["frames"], second["frames"])
        self.assertNotEqual(first["result"][1], second["result"][1])
        self.assertIn("FIXTURE VESSEL", second["frames"][0])
        self.assertIn("FIXTURE REACH", second["result"][1])


if __name__ == "__main__":
    unittest.main()
