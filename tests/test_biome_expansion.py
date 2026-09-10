from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine


class BiomeExpansionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def assert_biome_floor(self, biome_id: str, guardian_id: str) -> None:
        native = {
            enemy_id for enemy_id, enemy in self.catalog.enemies.items()
            if biome_id in enemy.get("biomes", [])
        }
        normal = {
            enemy_id for encounter in self.catalog.encounters.values()
            if encounter["kind"] == "normal" and biome_id in encounter.get("biomes", [])
            for enemy_id in encounter["enemies"]
        } & native
        elite = {
            enemy_id for encounter in self.catalog.encounters.values()
            if encounter["kind"] == "elite" and biome_id in encounter.get("biomes", [])
            for enemy_id in encounter["enemies"]
        } & native
        guardians = [
            encounter for encounter in self.catalog.encounters.values()
            if encounter["kind"] == "boss" and encounter["id"].startswith("base:guardian_")
            and biome_id in encounter.get("biomes", [])
        ]
        self.assertGreaterEqual(len(normal), 6)
        self.assertGreaterEqual(len(elite - normal), 2)
        self.assertEqual([[guardian_id]], [encounter["enemies"] for encounter in guardians])
        art = self.catalog.art["enemies"][guardian_id]
        self.assertGreaterEqual(len(art), 5)
        self.assertTrue(all(line.isascii() and len(line) <= 7 for line in art))

    def test_cryogenic_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("cryogenic", "base:thawing_regent")
        native_ids = {
            enemy_id for enemy_id, enemy in self.catalog.enemies.items()
            if "cryogenic" in enemy.get("biomes", [])
        }
        new_ids = {
            "base:chill_mirror", "base:coolant_leecher", "base:glacier_notary",
            "base:zero_orderer", "base:thawing_regent",
        }
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        actions = [action for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]]
        effects = [effect for action in actions for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "marked" for effect in effects))
        self.assertTrue(any(effect.get("status") == "stun" for effect in effects))
        engine = GameEngine.new(self.catalog, 3301)
        engine.start_combat("base:guardian_cryogenic")
        self.assertEqual(["base:thawing_regent"], [enemy.definition_id for enemy in engine.state.enemies])

    def test_hydroponic_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("hydroponic", "base:orchard_leviathan")
        native_ids = {enemy_id for enemy_id, enemy in self.catalog.enemies.items()
                      if "hydroponic" in enemy.get("biomes", [])}
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        new_ids = {"base:seed_slinger", "base:sap_runner", "base:bloom_skirmisher",
                   "base:thorn_bailiff", "base:canopy_midwife", "base:orchard_leviathan"}
        effects = [effect for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]
                   for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "marked" for effect in effects))
        self.assertTrue(any(effect.get("status") == "riposte" for effect in effects))

    def test_foundry_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("foundry", "base:crucible_magistrate")
        native_ids = {enemy_id for enemy_id, enemy in self.catalog.enemies.items()
                      if "foundry" in enemy.get("biomes", [])}
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        new_ids = {"base:cinder_tally", "base:chain_slug", "base:anvil_prefect",
                   "base:furnace_widow", "base:crucible_magistrate"}
        effects = [effect for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]
                   for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "vulnerable" for effect in effects))
        self.assertTrue(any(effect.get("status") == "riposte" for effect in effects))

    def test_reactor_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("reactor", "base:choir_meltdown")
        native_ids = {enemy_id for enemy_id, enemy in self.catalog.enemies.items()
                      if "reactor" in enemy.get("biomes", [])}
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        new_ids = {"base:isotope_tick", "base:relay_penitent", "base:flux_scribe",
                   "base:critical_abbot", "base:containment_hulk", "base:choir_meltdown"}
        effects = [effect for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]
                   for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "marked" for effect in effects))
        self.assertTrue(any(effect.get("status") == "vulnerable" for effect in effects))

    def test_fungal_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("fungal", "base:colony_crown")
        native_ids = {enemy_id for enemy_id, enemy in self.catalog.enemies.items()
                      if "fungal" in enemy.get("biomes", [])}
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        new_ids = {"base:hypha_hound", "base:bloom_mimic", "base:spore_bailiff",
                   "base:rot_matron", "base:colony_crown"}
        effects = [effect for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]
                   for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "marked" for effect in effects))
        self.assertTrue(any(effect["op"] == "heal" for effect in effects))


if __name__ == "__main__":
    unittest.main()
