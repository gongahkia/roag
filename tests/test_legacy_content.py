import unittest
from unittest.mock import patch

from dumbest_dungeon.content import load_catalog, load_legacy_catalog
from dumbest_dungeon.manifest import content_manifest
from dumbest_dungeon.migrations import LEGACY_20_FINGERPRINT


class LegacyContentTests(unittest.TestCase):
    def test_archived_catalog_matches_the_recorded_calibrated_rules(self) -> None:
        catalog = load_legacy_catalog()
        self.assertEqual(LEGACY_20_FINGERPRINT, catalog.manifest.fingerprint)
        self.assertIs(catalog, load_legacy_catalog())
        self.assertEqual(load_catalog().manifest, catalog.manifest)
        self.assertEqual(190, len(catalog.cards))
        self.assertEqual(70, len(catalog.enemies))

    def test_content_schema_comes_from_the_bundle_not_the_current_engine_constant(self) -> None:
        catalog = load_legacy_catalog()
        with patch("dumbest_dungeon.manifest.CONTENT_SCHEMA", 999):
            self.assertEqual(LEGACY_20_FINGERPRINT, content_manifest(catalog).fingerprint)
            self.assertEqual(20, content_manifest(catalog).content_schema)
