from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from jomon.catalog import ContentPackError, load_content_pack
from jomon.vessel_presentation import drink_benefit, drink_display_name, drink_drawback, schedule_display_name, vessel_format
from tests.test_content_packs import ROOT, alternate_pack


class VesselPresentationTests(unittest.TestCase):
    def test_default_drink_and_schedule_text_remain_exact(self):
        self.assertEqual(drink_display_name("hearth-ale"), "Hearth Ale")
        self.assertEqual(drink_benefit("hearth-ale"), "guard resists fear")
        self.assertEqual(drink_drawback("hearth-ale"), "steps make one more noise")
        self.assertEqual(schedule_display_name("serving"), "serving")
        self.assertEqual(vessel_format("vessel.drink.served", drink="Hearth Ale", benefit="guard resists fear", drawback="steps make one more noise"), "You drink Hearth Ale: guard resists fear; drawback: steps make one more noise.")

    def test_invalid_vessel_template_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "vessel_text.json"
            document = json.loads(source.read_text(encoding="utf-8"))
            document["text"]["vessel.drink.served"] = "Broken {drink} {benefit} {drawback} {unknown}."
            source.write_text(json.dumps(document), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*vessel_text\.json.*must contain exactly placeholders"):
                load_content_pack(root)

    def test_missing_unknown_and_empty_vessel_entries_are_rejected(self):
        cases = (
            (lambda text: text.pop("vessel.drink.hearth-ale.name"), "missing required vessel keys"),
            (lambda text: text.__setitem__("vessel.unknown", "Unknown"), "unknown vessel keys"),
            (lambda text: text.__setitem__("vessel.drink.hearth-ale.name", ""), "must be a non-empty string"),
        )
        for change, reason in cases:
            with self.subTest(reason=reason), tempfile.TemporaryDirectory() as directory:
                root = alternate_pack(Path(directory) / "fixture")
                source = root / "vessel_text.json"
                document = json.loads(source.read_text(encoding="utf-8"))
                change(document["text"])
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, reason):
                    load_content_pack(root)

    def test_alternate_pack_rewrites_bar_presentation_not_mechanics(self):
        script = (
            "import json; from jomon.actions import purchase_bar_drink; from jomon.state import create_world; "
            "from jomon.terminal import _overlay_lines; "
            "state=create_world('vessel-presentation-proof'); state.location='jomon'; state.jomon_space='tavern'; state.trade_credit=10; "
            "state.bartender_stock['hearth-ale']=1; before=(state.trade_credit,state.bartender_stock['hearth-ale'],state.world_time); "
            "result=purchase_bar_drink(state,'hearth-ale',bottle=False); schedule=state.actor_schedules[state.bartender.id]; "
            "print(json.dumps({'id':'hearth-ale','result':result.message,'effect':list(state.drink_effects),'mechanics':[before,state.trade_credit,state.bartender_stock['hearth-ale'],state.world_time,schedule.activity], 'overlay':_overlay_lines(state,'bartender:drinks'), 'bartender_overlay':_overlay_lines(state,'bartender')}))"
        )
        default = subprocess.run([sys.executable, "-c", script], cwd=ROOT, text=True, capture_output=True, check=True)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ, JOMON_CONTENT_PACK=str(root))
            alternate = subprocess.run([sys.executable, "-c", script], cwd=ROOT, env=environment, text=True, capture_output=True, check=True)
        default_data, alternate_data = json.loads(default.stdout), json.loads(alternate.stdout)
        self.assertEqual(default_data["id"], alternate_data["id"])
        self.assertEqual(default_data["mechanics"], alternate_data["mechanics"])
        self.assertEqual(default_data["effect"], alternate_data["effect"])
        self.assertNotEqual(default_data["result"], alternate_data["result"])
        self.assertIn("Fixture drink Fixture Hearth Measure", alternate_data["result"])
        self.assertIn("Fixture Hearth Measure", " ".join(alternate_data["overlay"][1]))
        self.assertIn("fixture service watch", " ".join(alternate_data["bartender_overlay"][1]))
        self.assertNotEqual(default_data["bartender_overlay"], alternate_data["bartender_overlay"])


if __name__ == "__main__":
    unittest.main()
