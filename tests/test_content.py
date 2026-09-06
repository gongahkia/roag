from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.content import ContentError, load_catalog


class ContentTests(unittest.TestCase):
    def test_bundled_catalog_is_complete(self) -> None:
        catalog = load_catalog()
        self.assertEqual(4, len(catalog.heroes))
        self.assertEqual(32, len(catalog.cards))
        self.assertGreaterEqual(len(catalog.enemies), 8)
        self.assertEqual(10, len(catalog.events))
        self.assertEqual(6, len(catalog.afflictions))

    def test_unknown_card_reference_is_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        raw["heroes"][0]["starter_deck"][0] = "missing-card"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "unknown card"):
                load_catalog(path)


if __name__ == "__main__":
    unittest.main()
