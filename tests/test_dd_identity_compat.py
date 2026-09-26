"""Stable DD identity and mechanics-only compatibility contracts."""

from __future__ import annotations

import copy
import json
import tempfile
import unittest
from pathlib import Path

from jomon.dumbest_dungeon.content import ContentError, _load_catalog, load_catalog
from jomon.dumbest_dungeon.engine import Actor, GameEngine
from jomon.dumbest_dungeon.expedition import new_match, normalize_active_match
from jomon.dumbest_dungeon.manifest import mechanical_rules_projection
from jomon.dumbest_dungeon.migrations import MigrationError, migrate_run
from jomon.dumbest_dungeon.tabletop import normalize_tabletop_records


DATA = Path("jomon/dumbest_dungeon/data/game.json")


class DullestDungeonIdentityTests(unittest.TestCase):
    def _catalog_with(self, change):
        data = json.loads(DATA.read_text())
        change(data)
        temporary = tempfile.TemporaryDirectory()
        path = Path(temporary.name) / "game.json"
        path.write_text(json.dumps(data), encoding="utf-8")
        return temporary, load_catalog(path)

    def test_presentation_does_not_change_rules_fingerprint(self):
        base = load_catalog()
        temporary, changed = self._catalog_with(lambda data: data["cards"][0].update(name="A wholly different filing technique"))
        with temporary:
            self.assertEqual(base.manifest.fingerprint, changed.manifest.fingerprint)

    def test_presentation_art_does_not_change_rules_fingerprint(self):
        base = load_catalog()
        game = DATA.read_text(encoding="utf-8")
        art_path = Path("jomon/dumbest_dungeon/data/art.json")
        metadata_path = Path("jomon/dumbest_dungeon/data/card_metadata.json")
        art = json.loads(art_path.read_text(encoding="utf-8"))
        art["title"][0] = "X" + art["title"][0][1:]
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "game.json").write_text(game, encoding="utf-8")
            (root / "art.json").write_text(json.dumps(art), encoding="utf-8")
            (root / "card_metadata.json").write_text(metadata_path.read_text(encoding="utf-8"), encoding="utf-8")
            rewritten = _load_catalog(root / "game.json", assets=root)
        self.assertEqual(base.manifest.fingerprint, rewritten.manifest.fingerprint)

    def test_mechanical_cost_and_action_weight_change_rules_fingerprint(self):
        base = load_catalog()
        temporary, cost_changed = self._catalog_with(lambda data: data["cards"][0].update(cost=data["cards"][0]["cost"] + 1))
        with temporary:
            self.assertNotEqual(base.manifest.fingerprint, cost_changed.manifest.fingerprint)
        temporary, weight_changed = self._catalog_with(lambda data: data["enemies"][0]["actions"][0].update(weight=data["enemies"][0]["actions"][0]["weight"] + 1))
        with temporary:
            self.assertNotEqual(base.manifest.fingerprint, weight_changed.manifest.fingerprint)

    def test_unclassified_catalog_field_is_rejected_by_projection(self):
        data = json.loads(DATA.read_text())
        data["cards"][0]["unclassified_future_rule"] = True
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "game.json"
            path.write_text(json.dumps(data), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "unknown fields"):
                load_catalog(path)

    def test_action_ids_are_stable_when_display_names_change(self):
        base = load_catalog()
        action_id = base.enemies["scrap_drone"]["actions"][0]["id"]
        temporary, changed = self._catalog_with(lambda data: data["enemies"][0]["actions"][0].update(name="Rewritten office gesture"))
        with temporary:
            self.assertEqual(action_id, changed.enemies["scrap_drone"]["actions"][0]["id"])
            self.assertEqual(base.manifest.fingerprint, changed.manifest.fingerprint)

    def test_fiction_rewrite_preserves_seeded_mechanical_state(self):
        base = load_catalog()
        temporary, rewritten = self._catalog_with(lambda data: data["art"].update(title="Ignored here") if False else data["cards"][0].update(description="New fiction"))
        with temporary:
            first = GameEngine.new(base, 222)
            second = GameEngine.new(rewritten, 222)
            self.assertEqual(first.state, second.state)
            self.assertEqual(first.rng.getstate(), second.rng.getstate())

    def test_fiction_renamed_catalog_resumes_an_active_snapshot(self):
        base = load_catalog()
        snapshot = GameEngine.new(base, 111).snapshot()
        temporary, rewritten = self._catalog_with(lambda data: data["enemies"][0].update(name="A renamed costume"))
        with temporary:
            resumed = GameEngine.from_snapshot(rewritten, snapshot)
            self.assertEqual(resumed.catalog.manifest.fingerprint, base.manifest.fingerprint)
            self.assertEqual(resumed.catalog.enemies["scrap_drone"]["name"], "A renamed costume")

    def test_repetition_weight_uses_action_id_not_display_name(self):
        catalog = load_catalog()
        engine = GameEngine.new(catalog, 12)
        action = catalog.enemies["scrap_drone"]["actions"][0]
        enemy = Actor("enemy", "Old visible name", 18, 18, 1, "enemy", definition_id="scrap_drone",
                      last_action_id=action["id"], action_repeats=2)
        repeated = engine._enemy_action_weight(enemy, action, set(), set())
        renamed = dict(action, name="Different visible name")
        self.assertEqual(repeated, engine._enemy_action_weight(enemy, renamed, set(), set()))

    def test_legacy_action_mapping_is_exact(self):
        catalog = load_catalog()
        snapshot = json.loads(json.dumps(GameEngine.new(catalog, 13).snapshot()))
        snapshot["save_version"] = 46
        snapshot["content_schema_version"] = 46
        snapshot["content_rules"]["content_schema"] = 46
        for enemy in snapshot["content_rules"]["enemies"].values():
            for action in enemy["actions"]:
                action.pop("id")
        for event in snapshot["content_rules"]["events"].values():
            for choice in event["choices"]:
                choice.pop("id")
        snapshot["state"]["enemies"] = [{"id": "enemy-1", "definition_id": "scrap_drone", "last_action": "Saw Arm"}]
        migrated = migrate_run(snapshot)
        self.assertEqual(migrated["state"]["enemies"][0]["last_action_id"], "scrap_drone:action:1")
        snapshot["state"]["enemies"][0]["last_action"] = "Saw Arm-like"
        with self.assertRaisesRegex(MigrationError, "cannot be mapped exactly"):
            migrate_run(snapshot)

    def test_legacy_pending_labels_are_rebuilt_from_match_state(self):
        catalog = load_catalog()
        roles = list(catalog.heroes)
        match = new_match("identity-pending", "courier", "patron", roles[:4], roles[4:8])
        camp = next(station for station in match["stations"] if station["kind"] == "camp")
        match.update(version=4, pending={
            "side": 0, "kind": "camp", "station": camp["id"],
            "choices": ["Arbitrary old label", "Another old label"],
        })
        normalize_active_match(match)
        self.assertEqual(match["version"], 5)
        self.assertEqual(match["pending"]["choices"], ["recover", "treat"])

    def test_legacy_department_mapping_is_exact(self):
        ledger = {"records": [{
            "courier": "c", "patron": "p", "season": "spring", "result": "win",
            "score": [2, 1], "department": "Accounts Payable",
        }]}
        normalize_tabletop_records(ledger)
        self.assertTrue(ledger["records"][0]["department_id"].startswith("department:"))
        unknown_record = dict(ledger["records"][0])
        unknown_record.pop("department_id")
        unknown_record["department"] = "Accounts Payable-like"
        with self.assertRaisesRegex(ValueError, "cannot be mapped exactly"):
            normalize_tabletop_records({"records": [unknown_record]})

    def test_snapshot_round_trip_uses_mechanics_manifest(self):
        catalog = load_catalog()
        engine = GameEngine.new(catalog, 77)
        restored = GameEngine.from_snapshot(catalog, engine.snapshot())
        self.assertEqual(restored.catalog.manifest.fingerprint, catalog.manifest.fingerprint)

    def test_v46_snapshot_migrates_action_names_without_guessing(self):
        catalog = load_catalog()
        snapshot = json.loads(json.dumps(GameEngine.new(catalog, 91).snapshot()))
        snapshot["save_version"] = 46
        snapshot["content_schema_version"] = 46
        snapshot["content_rules"]["content_schema"] = 46
        for enemy in snapshot["content_rules"]["enemies"].values():
            for action in enemy["actions"]:
                action.pop("id")
        for event in snapshot["content_rules"]["events"].values():
            for choice in event["choices"]:
                choice.pop("id")
        migrated = migrate_run(snapshot)
        self.assertEqual(migrated["save_version"], 47)
        self.assertEqual(migrated["content_manifest"]["schema"], 2)
        self.assertTrue(all("id" in action for enemy in migrated["content_rules"]["enemies"].values()
                            for action in enemy["actions"]))


if __name__ == "__main__":
    unittest.main()
