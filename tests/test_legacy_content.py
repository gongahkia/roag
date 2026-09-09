import unittest
import tempfile
from dataclasses import replace
from pathlib import Path
from unittest.mock import patch

from dumbest_dungeon.content import load_catalog, load_legacy_catalog
from dumbest_dungeon.manifest import content_manifest
from dumbest_dungeon.migrations import LEGACY_20_FINGERPRINT
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.save import write_save
from dumbest_dungeon.ui import TerminalUI


class LegacyContentTests(unittest.TestCase):
    def test_snapshot_selects_exact_archived_rules_instead_of_changed_installed_balance(self) -> None:
        original = GameEngine.new(load_legacy_catalog(), 42)
        installed = replace(original.catalog, balance={**original.catalog.balance, "hand_size": 6})
        loaded = GameEngine.from_snapshot(installed, original.snapshot())
        self.assertEqual(original.snapshot(), loaded.snapshot())
        self.assertEqual(5, loaded.catalog.balance["hand_size"])
        broken = original.snapshot()
        broken["content_manifest"]["fingerprint"] = "0" * 64
        with self.assertRaisesRegex(RuleError, "manifest"):
            GameEngine.from_snapshot(installed, broken)

    def test_terminal_load_uses_the_restored_catalog_for_labels_and_rules(self) -> None:
        original = GameEngine.new(load_legacy_catalog(), 42)
        ui = TerminalUI.__new__(TerminalUI)
        ui.catalog = replace(original.catalog, balance={**original.catalog.balance, "hand_size": 6})
        with tempfile.TemporaryDirectory() as directory:
            ui.save_path = Path(directory) / "run.json"
            write_save(ui.save_path, original.snapshot())
            self.assertTrue(ui._load())
            self.assertIs(ui.catalog, ui.engine.catalog)
            self.assertIn("recorded content 20", ui.message)

    def test_archived_catalog_matches_the_recorded_calibrated_rules(self) -> None:
        catalog = load_legacy_catalog()
        self.assertEqual(LEGACY_20_FINGERPRINT, catalog.manifest.fingerprint)
        self.assertIs(catalog, load_legacy_catalog())
        self.assertNotEqual(load_catalog().manifest, catalog.manifest)
        self.assertEqual(190, len(catalog.cards))
        self.assertEqual(70, len(catalog.enemies))

    def test_content_schema_comes_from_the_bundle_not_the_current_engine_constant(self) -> None:
        catalog = load_legacy_catalog()
        with patch("dumbest_dungeon.manifest.CONTENT_SCHEMA", 999):
            self.assertEqual(LEGACY_20_FINGERPRINT, content_manifest(catalog).fingerprint)
            self.assertEqual(20, content_manifest(catalog).content_schema)
