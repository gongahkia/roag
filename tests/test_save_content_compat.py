from __future__ import annotations

import copy
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from jomon.mechanical_compatibility import main_world_mechanical_fingerprint
from jomon.save import save_game
from jomon.state import (
    NARRATIVE_RECORD_LIMIT, SAVE_FORMAT, StateError, append_narrative_record, create_world,
    game_state_from_dict,
)
from tests.test_content_packs import ROOT, alternate_pack


class SaveContentCompatibilityTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("format fifteen compatibility")

    def _legacy_v14(self):
        data = copy.deepcopy(self.state.to_dict())
        data["save_format"] = 14
        data.pop("content_compat")
        data.pop("narrative_records")
        for item in data["items"]:
            item.pop("archived_hostile_issue", None)
        return data

    def test_v14_migrates_without_rewriting_history_and_adopts_on_write(self):
        self.state.history.append("Frozen historical line.")
        legacy = self._legacy_v14()
        loaded = game_state_from_dict(legacy)
        self.assertEqual(loaded.save_format, SAVE_FORMAT)
        self.assertEqual(loaded.content_compat, {
            "source": "legacy-unverified", "last_active_pack": None, "mechanical": None,
        })
        self.assertEqual(loaded.history[-1], "Frozen historical line.")
        self.assertEqual(loaded.narrative_records, [])
        with tempfile.TemporaryDirectory() as directory:
            path = save_game(loaded, Path(directory) / "save.json")
            written = json.loads(path.read_text(encoding="utf-8"))
        self.assertEqual(written["content_compat"]["source"], "legacy-unverified")
        self.assertIsNotNone(written["content_compat"]["last_active_pack"])
        self.assertEqual(written["content_compat"]["mechanical"]["catalog_fingerprint"], main_world_mechanical_fingerprint())
        self.assertEqual(game_state_from_dict(written).history[-1], "Frozen historical line.")

    def test_native_round_trip_and_compatibility_rejection(self):
        data = self.state.to_dict()
        self.assertEqual(data["content_compat"]["source"], "native")
        self.assertEqual(game_state_from_dict(data).to_dict(), data)
        incompatible = copy.deepcopy(data)
        incompatible["content_compat"]["mechanical"]["catalog_fingerprint"] = "0" * 64
        with self.assertRaisesRegex(StateError, "different main-world mechanical catalogs"):
            game_state_from_dict(incompatible)
        incompatible = copy.deepcopy(data)
        incompatible["content_compat"]["mechanical"]["compatibility_version"] += 1
        with self.assertRaisesRegex(StateError, "mechanical compatibility version"):
            game_state_from_dict(incompatible)

    def test_pack_provenance_and_presentation_fingerprint_do_not_enforce_loading(self):
        data = self.state.to_dict()
        data["content_compat"]["last_active_pack"]["id"] = "pack-b"
        data["content_compat"]["last_active_pack"]["presentation_fingerprint"] = "f" * 64
        self.assertEqual(game_state_from_dict(data).seed, self.state.seed)

    def test_known_hostile_provenance_migrates_but_similar_text_does_not(self):
        legacy = self._legacy_v14()
        household_ids = {person["id"] for person in legacy["household"]}
        hostile = next(item for item in legacy["items"] if item["owner_id"] not in household_ids and item["location"] == "readied")
        loaded = game_state_from_dict(legacy)
        migrated = next(item for item in loaded.items if item.id == hostile["id"])
        self.assertTrue(migrated.archived_hostile_issue)
        arbitrary = self._legacy_v14()
        raw = next(item for item in arbitrary["items"] if item["id"] == hostile["id"])
        raw["provenance"] = "working issue carried by an unrelated rewrite"
        loaded = game_state_from_dict(arbitrary)
        self.assertFalse(next(item for item in loaded.items if item.id == hostile["id"]).archived_hostile_issue)

    def test_new_hostile_flag_is_independent_of_rendered_provenance(self):
        item = next(item for item in self.state.items if item.archived_hostile_issue)
        item.provenance = "Different selected-pack enemy wording."
        self.assertEqual(game_state_from_dict(self.state.to_dict()).seed, self.state.seed)

    def test_narrative_ledger_round_trips_unknown_event_and_rejects_invalid_records(self):
        data = self.state.to_dict()
        record = {
            "event_id": "future.unknown.event",
            "refs": {"region_id": "hearthford"},
            "params": {"world_time": 0, "visible": True},
            "rendered": "Frozen unknown event.",
            "origin": {"pack_id": "default", "presentation_fingerprint": "a" * 64},
        }
        data["narrative_records"] = [record]
        loaded = game_state_from_dict(data)
        self.assertEqual(loaded.narrative_records, [record])
        bad = copy.deepcopy(data)
        bad["narrative_records"][0]["params"] = {"nested": []}
        with self.assertRaisesRegex(StateError, "narrative record parameters"):
            game_state_from_dict(bad)
        bad = copy.deepcopy(data)
        bad["narrative_records"] = [record] * (NARRATIVE_RECORD_LIMIT + 1)
        with self.assertRaisesRegex(StateError, "narrative record ledger"):
            game_state_from_dict(bad)

    def test_narrative_append_uses_active_pack_origin_and_discards_oldest_at_bound(self):
        self.state.narrative_records.clear()
        for index in range(NARRATIVE_RECORD_LIMIT + 1):
            append_narrative_record(
                self.state,
                event_id="test.provenance",
                refs={"region_id": "hearthford"},
                params={"index": index},
                rendered=f"Frozen record {index}.",
            )
        self.assertEqual(len(self.state.narrative_records), NARRATIVE_RECORD_LIMIT)
        self.assertEqual(self.state.narrative_records[0]["params"]["index"], 1)
        self.assertEqual(self.state.narrative_records[-1]["params"]["index"], NARRATIVE_RECORD_LIMIT)
        origin = self.state.narrative_records[-1]["origin"]
        self.assertEqual(origin["pack_id"], "default")
        self.assertEqual(len(origin["presentation_fingerprint"]), 64)

    def test_pack_switch_preserves_frozen_records_and_uses_new_pack_for_new_records(self):
        create_under_default = (
            "import json,sys; "
            "from jomon.actions import depart; "
            "from jomon.situations import BY_REGION_BAND,resolve; "
            "from jomon.state import create_world; "
            "state=create_world('narrative-pack-switch'); state.weapon='billhook'; state.gear='repair tools'; "
            "depart(state); row=BY_REGION_BAND['hearthford','steady']; resolve(state,row.id,'t'); "
            "state.travel_count=2; state.vessel_changes['voyage_variant:1']='shortage-skiffs'; "
            "open(sys.argv[1],'w',encoding='utf-8').write(json.dumps(state.to_dict(),sort_keys=True))"
        )
        continue_under_alternate = (
            "import json,sys; "
            "from jomon.echoes import apply_later_echoes; "
            "from jomon.state import game_state_from_dict; "
            "state=game_state_from_dict(json.load(open(sys.argv[1],encoding='utf-8'))); "
            "echo=apply_later_echoes(state)[0]; "
            "mechanics={'variant':echo.variant_id,'kind':echo.kind,'travel_count':state.travel_count,'world_time':state.world_time,'integrity':state.vessel_integrity,'route_risk':state.route_nodes[state.route_current_node].risk,'accounts':sorted((key,value.confidence,value.obligation) for key,value in state.institutions.items())}; "
            "print(json.dumps({'records':state.narrative_records,'mechanics':mechanics}))"
        )
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "switch.json"
            created = subprocess.run(
                [sys.executable, "-c", create_under_default, str(path)], cwd=ROOT,
                text=True, capture_output=True,
            )
            self.assertEqual(created.returncode, 0, created.stderr)
            alternate = alternate_pack(Path(directory) / "fixture")
            # The generic fixture deliberately changes a catalog seed word;
            # restore it so this is a mechanically compatible Pack B.
            (alternate / "data" / "world_text.json").write_bytes(
                (ROOT / "jomon" / "data" / "world_text.json").read_bytes()
            )
            source = alternate / "travel_text.json"
            document = json.loads(source.read_text(encoding="utf-8"))
            document["text"].update({
                "travel.echo.shortage-skiffs.title": "Fixture echo title",
                "travel.echo.shortage-skiffs.consequence": "fixture echo consequence",
                "travel.echo.activation": "FIXTURE ECHO {title}: {consequence}.",
            })
            source.write_text(json.dumps(document), encoding="utf-8")
            default_continued = subprocess.run(
                [sys.executable, "-c", continue_under_alternate, str(path)], cwd=ROOT,
                text=True, capture_output=True,
            )
            self.assertEqual(default_continued.returncode, 0, default_continued.stderr)
            continued = subprocess.run(
                [sys.executable, "-c", continue_under_alternate, str(path)], cwd=ROOT,
                env=dict(os.environ, JOMON_CONTENT_PACK=str(alternate)),
                text=True, capture_output=True,
            )
            self.assertEqual(continued.returncode, 0, continued.stderr)
        default_data, alternate_data = json.loads(default_continued.stdout), json.loads(continued.stdout)
        self.assertEqual(default_data["mechanics"], alternate_data["mechanics"])
        default_echo = next(record for record in default_data["records"] if record["event_id"] == "travel.echo.applied")
        situation = next(record for record in alternate_data["records"] if record["event_id"] == "situation.resolved")
        echo = next(record for record in alternate_data["records"] if record["event_id"] == "travel.echo.applied")
        self.assertEqual(
            {key: default_echo[key] for key in ("event_id", "refs", "params")},
            {key: echo[key] for key in ("event_id", "refs", "params")},
        )
        self.assertEqual(situation["origin"]["pack_id"], "default")
        self.assertNotIn("FIXTURE", situation["rendered"])
        self.assertEqual(echo["origin"]["pack_id"], "fixture-alternate")
        self.assertIn("FIXTURE ECHO", echo["rendered"])


if __name__ == "__main__":
    unittest.main()
