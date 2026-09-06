from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.content import ContentError, load_catalog


class ContentTests(unittest.TestCase):
    def test_bundled_catalog_is_complete(self) -> None:
        catalog = load_catalog()
        self.assertEqual(10, len(catalog.heroes))
        self.assertEqual(75, len(catalog.cards))
        self.assertEqual(25, len(catalog.enemies))
        self.assertGreaterEqual(len(catalog.encounters), 19)
        self.assertEqual(10, len(catalog.events))
        self.assertEqual(6, len(catalog.afflictions))
        self.assertEqual(set(catalog.heroes), set(catalog.art["heroes"]))
        self.assertEqual(set(catalog.enemies), set(catalog.art["enemies"]))
        sprites = list(catalog.art["heroes"].values()) + list(catalog.art["enemies"].values())
        for sprite in sprites:
            self.assertEqual(5, len(sprite))
            self.assertTrue(all(len(line) <= 7 and line.isascii() for line in sprite))
        for hero in catalog.heroes.values():
            starters = [catalog.cards[card_id] for card_id in hero["starter_deck"]]
            for rank in range(1, 5):
                self.assertTrue(
                    any(rank in card["from_ranks"] for card in starters),
                    f"{hero['id']} has no starter card usable from rank {rank}",
                )

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
