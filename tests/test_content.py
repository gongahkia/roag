from __future__ import annotations

import json
import tempfile
import unittest
from collections import Counter
from pathlib import Path

from dumbest_dungeon.content import ContentError, load_catalog


class ContentTests(unittest.TestCase):
    def test_bundled_catalog_is_semantically_complete(self) -> None:
        catalog = load_catalog()
        self.assertTrue(all((catalog.heroes, catalog.cards, catalog.enemies, catalog.encounters)))
        self.assertTrue(all((catalog.biomes, catalog.worlds, catalog.events)))
        self.assertTrue(all((catalog.boons, catalog.curses, catalog.items, catalog.afflictions)))
        self.assertEqual(set(catalog.heroes), set(catalog.art["heroes"]))
        self.assertEqual(set(catalog.heroes), set(catalog.art["card_marks"]))
        self.assertEqual(len(catalog.heroes), len(set(catalog.art["card_marks"].values())))
        self.assertEqual(set(catalog.enemies), set(catalog.art["enemies"]))
        self.assertEqual(len(catalog.heroes), len({hero["role"] for hero in catalog.heroes.values()}))
        self.assertEqual(len(catalog.cards), len({card["name"] for card in catalog.cards.values()}))
        self.assertEqual(len(catalog.enemies), len({enemy["name"] for enemy in catalog.enemies.values()}))
        for biome_id in catalog.biomes:
            if biome_id == "derelict":
                continue
            biome_enemies = [
                enemy for enemy in catalog.enemies.values() if biome_id in enemy.get("biomes", [])
            ]
            self.assertGreaterEqual(len(biome_enemies), 3, biome_id)
        affinity_cards = [card for card in catalog.cards.values() if "biome" in card]
        affinity_heroes = [hero for hero in catalog.heroes.values() if "biome" in hero]
        self.assertTrue(affinity_cards)
        self.assertTrue(affinity_heroes)
        self.assertEqual(
            {hero["id"] for hero in affinity_heroes},
            {card["hero"] for card in affinity_cards},
        )
        def signature(card: dict) -> tuple:
            return (
                card["cost"],
                tuple(card["from_ranks"]),
                card["target"],
                tuple(card.get("target_ranks", [])),
                json.dumps(card["effects"], sort_keys=True),
            )

        signature_counts = Counter(signature(card) for card in catalog.cards.values())
        self.assertTrue(all(signature_counts[signature(card)] == 1 for card in affinity_cards))
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
            self.assertTrue(
                any(
                    [effect["op"] for effect in card["effects"]]
                    != [effect["op"] for effect in card["upgrade_effects"]]
                    for card in catalog.cards.values()
                    if card["hero"] == hero["id"]
                ),
                f"{hero['id']} has no structural card upgrade",
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

    def test_invalid_card_build_tag_is_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        raw["cards"][0]["tags"] = ["certainly-not-a-build-tag"]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-tag.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "invalid build tags"):
                load_catalog(path)

    def test_incomplete_biome_mechanics_are_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        del raw["biomes"][0]["mechanics"]["patrol"]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-biome.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "all six mechanic sections"):
                load_catalog(path)

    def test_content_balance_guardrails(self) -> None:
        catalog = load_catalog()
        cards_per_hero = Counter(card["hero"] for card in catalog.cards.values())
        self.assertTrue(all(count >= 5 for count in cards_per_hero.values()))
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
        for biome_id in catalog.biomes:
            normal = [
                encounter
                for encounter in catalog.encounters.values()
                if encounter["kind"] == "normal"
                and biome_id in encounter.get("biomes", ["derelict"])
            ]
            elite = [
                encounter
                for encounter in catalog.encounters.values()
                if encounter["kind"] == "elite"
                and biome_id in encounter.get("biomes", ["derelict"])
            ]
            self.assertGreaterEqual(len(normal), 4, biome_id)
            self.assertGreaterEqual(len(elite), 1, biome_id)
        for encounter in catalog.encounters.values():
            if encounter["kind"] != "normal":
                continue
            total_hp = sum(catalog.enemies[enemy_id]["max_hp"] for enemy_id in encounter["enemies"])
            self.assertGreaterEqual(total_hp, 40, encounter["id"])
            self.assertLessEqual(total_hp, 60, encounter["id"])

    def test_catalog_size_is_diagnostic_not_a_validity_rule(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        raw["items"].pop()
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "fewer-items.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            fewer = load_catalog(path)
        self.assertEqual(len(catalog.items) - 1, len(fewer.items))

        raw = json.loads(json.dumps(catalog.raw))
        extra = dict(raw["items"][-1])
        extra["id"] = "semantic_spare"
        extra["name"] = "Semantic Spare"
        raw["items"].append(extra)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "more-items.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            more = load_catalog(path)
        self.assertEqual(len(catalog.items) + 1, len(more.items))

    def test_unknown_status_and_cross_owner_starter_are_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        raw["cards"][0]["effects"][0] = {
            "op": "status",
            "status": "made_up",
            "amount": 1,
        }
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-status.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "invalid status"):
                load_catalog(path)

        raw = json.loads(json.dumps(catalog.raw))
        raw["heroes"][0]["starter_deck"][0] = raw["heroes"][1]["starter_deck"][0]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "wrong-owner.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "another owner's card"):
                load_catalog(path)

    def test_rank_lists_must_be_canonical(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        nonstarter = next(card for card in raw["cards"] if card["id"] == "shield_rush")
        nonstarter["from_ranks"] = [2, 1, 2]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-ranks.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "unique ranks in ascending order"):
                load_catalog(path)


if __name__ == "__main__":
    unittest.main()
