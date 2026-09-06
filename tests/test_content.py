from __future__ import annotations

import json
import tempfile
import unittest
from collections import Counter
from pathlib import Path

from dumbest_dungeon.content import ContentError, load_catalog


class ContentTests(unittest.TestCase):
    def test_bundled_catalog_is_complete(self) -> None:
        catalog = load_catalog()
        self.assertEqual(15, len(catalog.heroes))
        self.assertEqual(105, len(catalog.cards))
        self.assertEqual(35, len(catalog.enemies))
        self.assertGreaterEqual(len(catalog.encounters), 35)
        self.assertEqual(10, len(catalog.events))
        self.assertEqual(18, len(catalog.boons))
        self.assertEqual(18, len(catalog.curses))
        self.assertEqual(18, len(catalog.items))
        self.assertEqual(6, sum(curse["kind"] == "card" for curse in catalog.curses.values()))
        self.assertEqual(6, len(catalog.afflictions))
        self.assertEqual(set(catalog.heroes), set(catalog.art["heroes"]))
        self.assertEqual(set(catalog.heroes), set(catalog.art["card_marks"]))
        self.assertEqual(len(catalog.heroes), len(set(catalog.art["card_marks"].values())))
        self.assertEqual(set(catalog.enemies), set(catalog.art["enemies"]))
        self.assertEqual(len(catalog.heroes), len({hero["role"] for hero in catalog.heroes.values()}))
        self.assertEqual(len(catalog.cards), len({card["name"] for card in catalog.cards.values()}))
        self.assertEqual(len(catalog.enemies), len({enemy["name"] for enemy in catalog.enemies.values()}))
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

    def test_content_balance_guardrails(self) -> None:
        catalog = load_catalog()
        cards_per_hero = Counter(card["hero"] for card in catalog.cards.values())
        self.assertTrue(all(6 <= count <= 8 for count in cards_per_hero.values()))
        self.assertTrue(all(0 <= card["cost"] <= 2 for card in catalog.cards.values()))
        for card in catalog.cards.values():
            if card["cost"] == 0:
                self.assertFalse(any(effect["op"] == "damage" for effect in card["effects"]))

        referenced_enemies = {
            enemy_id
            for encounter in catalog.encounters.values()
            for enemy_id in encounter["enemies"]
        }
        self.assertEqual(set(catalog.enemies), referenced_enemies)
        formations = [tuple(encounter["enemies"]) for encounter in catalog.encounters.values()]
        self.assertEqual(len(formations), len(set(formations)))
        for encounter in catalog.encounters.values():
            if encounter["kind"] != "normal":
                continue
            total_hp = sum(catalog.enemies[enemy_id]["max_hp"] for enemy_id in encounter["enemies"])
            self.assertGreaterEqual(total_hp, 40, encounter["id"])
            self.assertLessEqual(total_hp, 60, encounter["id"])


if __name__ == "__main__":
    unittest.main()
