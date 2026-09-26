from __future__ import annotations

import copy
import json
from pathlib import Path
import tempfile
import unittest

from jomon.mechanical_compatibility import main_world_mechanical_fingerprint
from jomon.save import save_game
from jomon.state import (
    NARRATIVE_RECORD_LIMIT, SAVE_FORMAT, StateError, append_narrative_record, create_world,
    game_state_from_dict,
)


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


if __name__ == "__main__":
    unittest.main()
