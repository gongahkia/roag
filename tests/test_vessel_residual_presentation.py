from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from jomon.catalog import ContentPackError, load_content_pack
from jomon.vessel_presentation import household_story_display_name, refit_display_name
from tests.test_content_packs import ROOT, alternate_pack


class VesselResidualPresentationTests(unittest.TestCase):
    def test_default_refit_and_story_names_remain_exact(self):
        self.assertEqual(refit_display_name("cargo-rail-netting"), "Cargo rail netting")
        self.assertEqual(household_story_display_name("empty-watch"), "The watch after an empty place")

    def test_invalid_refit_and_story_entries_are_rejected(self):
        cases = (
            (lambda text: text.pop("vessel.refit.cargo-rail-netting.name"), "missing required vessel keys"),
            (lambda text: text.pop("vessel.story.choice.empty-watch.w"), "missing required vessel keys"),
            (lambda text: text.__setitem__("vessel.refit.unknown.name", "Unknown"), "unknown vessel keys"),
            (lambda text: text.__setitem__("vessel.story.empty-watch.name", ""), "must be a non-empty string"),
            (lambda text: text.__setitem__("vessel.refit.cargo-rail-netting.name", 3), "must be a non-empty string"),
            (lambda text: text.__setitem__("vessel.refit.install.result", "{refit} {unknown}"), "must contain exactly placeholders"),
            (lambda text: text.__setitem__("vessel.story.record", "{story} {branch}"), "must contain exactly placeholders"),
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

    def test_duplicate_vessel_key_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "vessel_text.json"
            raw = source.read_text(encoding="utf-8")
            needle = '"vessel.refit.cargo-rail-netting.name": "Cargo rail netting"'
            replacement = needle + ',\n    "vessel.refit.cargo-rail-netting.name": "Duplicate refit"'
            self.assertIn(needle, raw)
            source.write_text(raw.replace(needle, replacement, 1), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, "duplicate catalog key"):
                load_content_pack(root)

    def test_alternate_pack_changes_refit_and_story_presentation_not_mechanics(self):
        script = (
            "import json; from jomon.household_stories import resolve, story_choices, story_lines; "
            "from jomon.state import CommodityStack, Position, create_world; "
            "from jomon.vessel_refits import REFITS, install_refit, installation_status; "
            "refit=create_world('vessel-residual-refit'); refit.position=Position(15,10,1); refit.vessel_cargo['wool']=CommodityStack(1,'dry'); refit.trade_credit=20; "
            "before=(refit.trade_credit,refit.world_time); available=installation_status(refit,'cargo-rail-netting'); installed=install_refit(refit,'cargo-rail-netting'); "
            "story=create_world('vessel-residual-story'); story.returned_expeditions=4; opened=resolve(story,'empty-watch','o'); choices=story_choices(story,'empty-watch'); finished=resolve(story,'empty-watch','w'); "
            "people=sorted((p.id,sorted(p.relationships.items())) for p in story.household); "
            "print(json.dumps({'refit_text':[available[1],installed[1]],'story_text':[opened[1],finished[1],choices,story_lines(story,'empty-watch')],"
            "'mechanics':{'refit':['cargo-rail-netting',REFITS['cargo-rail-netting'].station,before,refit.trade_credit,refit.world_time,refit.vessel_changes.get('refit:cargo-rail-netting')],"
            "'story':['empty-watch','w',story.vessel_changes.get('household-story:empty-watch:status'),story.vessel_changes.get('watch_order'),people]}}))"
        )
        default = subprocess.run([sys.executable, "-c", script], cwd=ROOT, text=True, capture_output=True, check=True)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "vessel_text.json"
            document = json.loads(source.read_text(encoding="utf-8"))
            text = document["text"]
            text.update({
                "vessel.refit.cargo-rail-netting.name": "Fixture cargo lattice",
                "vessel.refit.cargo-rail-netting.effect": "Fixture lattice keeps the same first loss mechanics.",
                "vessel.refit.cargo-rail-netting.drawback": "Fixture lattice preserves the same trade-off.",
                "vessel.refit.install.result": "FIXTURE REFIT {refit} at {station}: {effect} / {cargo} / {credit} / {actions} / {drawback}",
                "vessel.story.empty-watch.name": "Fixture shared watch",
                "vessel.story.empty-watch.premise": "Fixture premise preserves the same household conditions.",
                "vessel.story.choice.empty-watch.w": "Fixture witnessed watch choice",
                "vessel.story.outcome.empty_watch.living": "Fixture outcome keeps the same shared-watch effect.",
                "vessel.story.record": "FIXTURE RECORD {story}: {branch}. {outcome}",
            })
            source.write_text(json.dumps(document), encoding="utf-8")
            alternate = subprocess.run(
                [sys.executable, "-c", script], cwd=ROOT,
                env=dict(os.environ, JOMON_CONTENT_PACK=str(root)), text=True, capture_output=True, check=True,
            )
        default_data, alternate_data = json.loads(default.stdout), json.loads(alternate.stdout)
        self.assertEqual(default_data["mechanics"], alternate_data["mechanics"])
        self.assertNotEqual(default_data["refit_text"], alternate_data["refit_text"])
        self.assertNotEqual(default_data["story_text"], alternate_data["story_text"])
        self.assertIn("Fixture cargo lattice", " ".join(alternate_data["refit_text"]))
        self.assertIn("Fixture witnessed watch choice", " ".join(str(row) for row in alternate_data["story_text"]))


if __name__ == "__main__":
    unittest.main()
