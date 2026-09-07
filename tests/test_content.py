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
        self.assertTrue(
            all(
                (
                    catalog.biomes,
                    catalog.worlds,
                    catalog.events,
                    catalog.terrains,
                    catalog.landmarks,
                    catalog.missions,
                    catalog.facilities,
                    catalog.terrain_patterns,
                )
            )
        )
        self.assertTrue(all((catalog.boons, catalog.curses, catalog.items, catalog.afflictions)))
        self.assertGreaterEqual(len(catalog.squads), 4)
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
        self.assertTrue(all(card["tags"] for card in catalog.cards.values()))
        self.assertTrue(all(card["upgrade_description"] for card in catalog.cards.values()))
        self.assertEqual(
            set(".,=~_\";:`'%-o"),
            {terrain["glyph"] for terrain in catalog.terrains.values()},
        )
        self.assertEqual(
            set(catalog.biomes),
            {landmark["biome"] for landmark in catalog.landmarks.values()},
        )
        self.assertEqual(
            set(catalog.biomes),
            {mission["biome"] for mission in catalog.missions.values()},
        )
        self.assertEqual(
            set(catalog.biomes),
            {facility["biome"] for facility in catalog.facilities.values()},
        )
        self.assertEqual(
            {"circuit", "erratic", "hunt", "migrate", "roam", "sentry", "stalk", "sweep"},
            {biome["mechanics"]["patrol"]["behavior"] for biome in catalog.biomes.values()},
        )
        self.assertEqual(
            set(catalog.biomes),
            {pattern["biome"] for pattern in catalog.terrain_patterns.values()},
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
            self.assertIn(hero["combat_role"], {"controller", "defender", "striker", "support"})
            self.assertIn(hero["complexity"], {1, 2, 3})
            self.assertIn(hero["rank"], hero["preferred_ranks"])
            self.assertEqual(2, len(set(hero["builds"])))
            self.assertTrue(all(hero[field] for field in ("signature", "strength", "weakness")))
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
            starter_ids = set(hero["starter_deck"])
            nonstarters = [
                card for card in catalog.cards.values()
                if card["hero"] == hero["id"] and card["id"] not in starter_ids
            ]
            self.assertGreaterEqual(len(nonstarters), 3, hero["id"])
        for squad in catalog.squads.values():
            self.assertEqual(4, len(set(squad["formation"])))
            for rank, hero_id in enumerate(squad["formation"], 1):
                self.assertIn(rank, catalog.heroes[hero_id]["preferred_ranks"])
        self.assertTrue(
            all(card["effects"] != card["upgrade_effects"] for card in catalog.cards.values())
        )

    def test_curated_squad_with_invalid_formation_is_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        raw["squads"][0]["formation"] = ["scout", "engineer", "medic", "warden"]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-squad.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "outside a preferred rank"):
                load_catalog(path)

    def test_archetype_without_draft_support_is_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        hero = next(hero for hero in raw["heroes"] if hero["id"] == "breacher")
        hero["starter_deck"][1] = "demolition"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "thin-pool.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "three non-starter"):
                load_catalog(path)

    def test_every_archetype_has_a_positional_card_pool(self) -> None:
        catalog = load_catalog()
        for hero_id in catalog.heroes:
            cards = [
                card for card in catalog.cards.values() if card["hero"] == hero_id
            ]
            positional = [card for card in cards if len(card["from_ranks"]) <= 2]
            self.assertGreaterEqual(
                len(positional),
                len(cards) // 2,
                f"{hero_id} has lost its positional identity",
            )
            self.assertLessEqual(
                sum(len(card["from_ranks"]) == 4 for card in cards),
                1,
                f"{hero_id} has too many positionless cards",
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

    def test_required_authored_card_tag_is_rejected_when_missing(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        card = next(card for card in raw["cards"] if card["id"] == "arc_welder")
        card["tags"].remove("payoff:marked")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "missing-tag.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "missing authored build tags"):
                load_catalog(path)

    def test_self_debuffs_are_not_misrepresented_as_enemy_setup(self) -> None:
        catalog = load_catalog()
        self.assertNotIn("control", catalog.cards["quench"]["tags"])
        self.assertNotIn("setup:wound", catalog.cards["spore_exchange"]["tags"])

    def test_incomplete_biome_mechanics_are_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        del raw["biomes"][0]["mechanics"]["patrol"]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-biome.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "all six mechanic sections"):
                load_catalog(path)

    def test_biomes_have_distinct_systemic_contracts(self) -> None:
        catalog = load_catalog()
        hazard_signatures = set()
        combat_signatures = set()
        patrol_signatures = set()
        spatial_ops = {
            "agitate_patrols",
            "calm_patrols",
            "objective_combat",
            "reveal_biome",
            "stabilize_terrain",
            "suppress_hazard",
        }
        for biome in catalog.biomes.values():
            mechanics = biome["mechanics"]
            hazard = mechanics["hazard"]
            hazard_signatures.add(
                (hazard["effect"], hazard.get("status"), hazard["amount"])
            )
            patrol = mechanics["patrol"]
            patrol_signatures.add(
                (
                    patrol["behavior"],
                    patrol["aggression"],
                    patrol["cadence"],
                    patrol["leash"],
                )
            )
            combat_signatures.add(
                tuple(
                    (
                        effect["target"],
                        effect["op"],
                        effect.get("status"),
                        effect["amount"],
                    )
                    for effect in mechanics["combat"]["effects"]
                )
            )
            mission = next(
                item for item in catalog.missions.values()
                if item["biome"] == biome["id"]
            )
            route_signatures = []
            mission_ops = set()
            for approach in mission["approaches"]:
                stage_ops = tuple(
                    stage.get("effect", {}).get("op", "none")
                    for stage in approach["stages"]
                )
                mission_ops.update(stage_ops)
                mission_ops.add(approach["completion"]["op"])
                route_signatures.append(
                    (
                        approach["telegraph"]["travel"],
                        approach["telegraph"]["risk"],
                        approach["cost"]["resource"],
                        stage_ops,
                        approach["completion"]["op"],
                    )
                )
            self.assertNotEqual(route_signatures[0], route_signatures[1])
            self.assertTrue(mission_ops & spatial_ops)
        count = len(catalog.biomes)
        self.assertEqual(count, len(hazard_signatures))
        self.assertEqual(count, len(patrol_signatures))
        self.assertEqual(count, len(combat_signatures))

    def test_facility_with_unsupported_effect_is_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        raw["facilities"][0]["options"][0]["effects"][0]["op"] = "invent_ammunition"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-facility.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "invalid option"):
                load_catalog(path)

    def test_event_with_unknown_biome_is_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        raw["events"][0]["biomes"] = ["gift_shop"]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-event-biome.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "needs at least two choices"):
                load_catalog(path)

    def test_objective_combat_cannot_be_a_terminal_stage(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        raw["missions"][0]["approaches"][0]["stages"][-1]["effect"] = {
            "op": "objective_combat",
            "amount": 1,
        }
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-objective-combat.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "invalid approach"):
                load_catalog(path)

    def test_misleading_secondary_terrain_pattern_is_rejected(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        raw["terrain_patterns"][0]["glyph"] = "~"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad-terrain-pattern.json"
            path.write_text(json.dumps(raw), encoding="utf-8")
            with self.assertRaisesRegex(ContentError, "terrain pattern"):
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

    def test_established_specialists_have_three_nonstarter_drafts(self) -> None:
        catalog = load_catalog()
        for hero_id in ("duelist", "artillerist", "chaplain", "hacker", "pilot"):
            hero = catalog.heroes[hero_id]
            starters = set(hero["starter_deck"])
            nonstarters = [
                card for card in catalog.cards.values()
                if card["hero"] == hero_id and card["id"] not in starters
            ]
            self.assertGreaterEqual(len(nonstarters), 3, hero_id)
            self.assertGreaterEqual(len({tuple(card["tags"]) for card in nonstarters}), 2, hero_id)

    def test_cryogenic_and_hydroponic_specialists_have_draft_branches(self) -> None:
        catalog = load_catalog()
        for hero_id in ("cryonaut", "horticulturist"):
            hero = catalog.heroes[hero_id]
            starters = set(hero["starter_deck"])
            nonstarters = [
                card for card in catalog.cards.values()
                if card["hero"] == hero_id and card["id"] not in starters
            ]
            self.assertGreaterEqual(len(nonstarters), 3, hero_id)
            self.assertTrue(all(f"affinity:{hero['biome']}" in card["tags"] for card in nonstarters))

    def test_foundry_and_reactor_specialists_have_draft_branches(self) -> None:
        catalog = load_catalog()
        for hero_id in ("foundryman", "reactor_saint"):
            hero = catalog.heroes[hero_id]
            starters = set(hero["starter_deck"])
            nonstarters = [
                card for card in catalog.cards.values()
                if card["hero"] == hero_id and card["id"] not in starters
            ]
            self.assertGreaterEqual(len(nonstarters), 3, hero_id)
            self.assertTrue(all(f"affinity:{hero['biome']}" in card["tags"] for card in nonstarters))

    def test_fungal_and_flooded_specialists_have_draft_branches(self) -> None:
        catalog = load_catalog()
        for hero_id in ("mycologist", "diver"):
            hero = catalog.heroes[hero_id]
            starters = set(hero["starter_deck"])
            nonstarters = [
                card for card in catalog.cards.values()
                if card["hero"] == hero_id and card["id"] not in starters
            ]
            self.assertGreaterEqual(len(nonstarters), 3, hero_id)
            self.assertTrue(all(f"affinity:{hero['biome']}" in card["tags"] for card in nonstarters))

    def test_storm_and_archive_specialists_have_draft_branches(self) -> None:
        catalog = load_catalog()
        for hero_id in ("stormcaller", "archivist"):
            hero = catalog.heroes[hero_id]
            starters = set(hero["starter_deck"])
            nonstarters = [
                card for card in catalog.cards.values()
                if card["hero"] == hero_id and card["id"] not in starters
            ]
            self.assertGreaterEqual(len(nonstarters), 3, hero_id)
            self.assertTrue(all(f"affinity:{hero['biome']}" in card["tags"] for card in nonstarters))

    def test_void_and_ossuary_specialists_have_draft_branches(self) -> None:
        catalog = load_catalog()
        for hero_id in ("voidwalker", "bonewright"):
            hero = catalog.heroes[hero_id]
            starters = set(hero["starter_deck"])
            nonstarters = [
                card for card in catalog.cards.values()
                if card["hero"] == hero_id and card["id"] not in starters
            ]
            self.assertGreaterEqual(len(nonstarters), 3, hero_id)
            self.assertTrue(all(f"affinity:{hero['biome']}" in card["tags"] for card in nonstarters))

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
