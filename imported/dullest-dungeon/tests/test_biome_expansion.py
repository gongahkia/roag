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

    def test_flooded_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("flooded", "base:pressure_sovereign")
        native_ids = {enemy_id for enemy_id, enemy in self.catalog.enemies.items()
                      if "flooded" in enemy.get("biomes", [])}
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        new_ids = {"base:ballast_tick", "base:drowned_diver", "base:tide_scribe",
                   "base:undertow_judge", "base:sump_matron", "base:pressure_sovereign"}
        effects = [effect for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]
                   for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "marked" for effect in effects))
        self.assertTrue(any(effect["op"] == "heal" for effect in effects))

    def test_storm_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("storm", "base:tempest_engine")
        native_ids = {enemy_id for enemy_id, enemy in self.catalog.enemies.items()
                      if "storm" in enemy.get("biomes", [])}
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        new_ids = {"base:arc_mite", "base:thunder_kite", "base:storm_notary",
                   "base:faraday_beast", "base:tempest_engine"}
        effects = [effect for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]
                   for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "marked" for effect in effects))
        self.assertTrue(any(effect["op"] == "move" for effect in effects))

    def test_archive_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("archive", "base:final_librarian")
        native_ids = {enemy_id for enemy_id, enemy in self.catalog.enemies.items()
                      if "archive" in enemy.get("biomes", [])}
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        new_ids = {"base:footnote_wasp", "base:redaction_hound", "base:errata_clerk",
                   "base:index_bailiff", "base:revision_specter", "base:final_librarian"}
        effects = [effect for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]
                   for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "marked" for effect in effects))
        self.assertTrue(any(effect.get("status") == "riposte" for effect in effects))

    def test_void_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("void", "base:absent_monarch")
        native_ids = {enemy_id for enemy_id, enemy in self.catalog.enemies.items()
                      if "void" in enemy.get("biomes", [])}
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        new_ids = {"base:echo_moth", "base:event_leech", "base:parallax_waif",
                   "base:null_bailiff", "base:horizon_judge", "base:absent_monarch"}
        effects = [effect for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]
                   for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "marked" for effect in effects))
        self.assertTrue(any(effect["op"] == "move" for effect in effects))

    def test_ossuary_floor_and_coordination_questions(self) -> None:
        self.assert_biome_floor("ossuary", "base:ossuary_heart")
        native_ids = {enemy_id for enemy_id, enemy in self.catalog.enemies.items()
                      if "ossuary" in enemy.get("biomes", [])}
        self.assertTrue(all(len(self.catalog.enemies[enemy_id]["actions"]) >= 3
                            for enemy_id in native_ids))
        new_ids = {"base:splint_rat", "base:marrow_cantor", "base:reliquary_guard",
                   "base:bone_auditor", "base:ossuary_heart"}
        effects = [effect for enemy_id in new_ids
                   for action in self.catalog.enemies[enemy_id]["actions"]
                   for effect in action["effects"]]
        self.assertTrue(any(effect["op"] == "guard" for effect in effects))
        self.assertTrue(any(effect.get("bonus_status") == "wound" for effect in effects))
        self.assertTrue(any(effect.get("status") == "riposte" for effect in effects))

    def test_derelict_has_native_guardian_and_existing_density(self) -> None:
        generic_normal = {enemy_id for encounter in self.catalog.encounters.values()
                          if encounter["kind"] == "normal" and not encounter.get("biomes")
                          for enemy_id in encounter["enemies"]}
        generic_elite = {enemy_id for encounter in self.catalog.encounters.values()
                         if encounter["kind"] == "elite" and not encounter.get("biomes")
                         for enemy_id in encounter["enemies"]}
        self.assertGreaterEqual(len(generic_normal), 6)
        self.assertGreaterEqual(len(generic_elite - generic_normal), 2)
        guardian = self.catalog.encounters["base:guardian_derelict"]
        self.assertEqual(["base:rusted_admiral"], guardian["enemies"])
        self.assertEqual(3, len(self.catalog.enemies["base:rusted_admiral"]["actions"]))
        art = self.catalog.art["enemies"]["base:rusted_admiral"]
        self.assertGreaterEqual(len(art), 5)
        self.assertTrue(all(line.isascii() and len(line) <= 7 for line in art))

    def test_first_completed_objective_culminates_in_one_guardian(self) -> None:
        engine = GameEngine.new(self.catalog, 4317)
        objective = engine.state.objectives[0]
        mission = engine.mission_definition(objective.biome_id)
        approach = mission["approaches"][0]
        objective.approach = approach["id"]
        objective.stage = len(approach["stages"]) - 1
        objective.facts = {"approach": approach["id"]}
        engine.state.phase = "objective"
        engine.state.current_objective_id = objective.id
        engine.state.required_objectives = 1

        message = engine.advance_objective()

        self.assertIn("bars the exit", message)
        self.assertEqual("guardian", engine.state.combat_kind)
        self.assertEqual("pending", objective.facts["guardian"])
        self.assertFalse(engine.boss_unlocked())
        self.assertEqual(
            self.catalog.encounters[f"base:guardian_{objective.biome_id}"]["enemies"],
            [enemy.definition_id for enemy in engine.state.enemies],
        )
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        loaded_objective = next(item for item in loaded.state.objectives if item.id == objective.id)
        self.assertEqual("pending", loaded_objective.facts["guardian"])
        self.assertEqual(engine.snapshot(), loaded.snapshot())

        for enemy in engine.state.enemies:
            enemy.hp = 0
        engine._combat_victory()
        self.assertEqual("defeated", objective.facts["guardian"])
        self.assertTrue(engine.boss_unlocked())
        self.assertEqual("reward", engine.state.phase)
        self.assertTrue(engine.core_patrol().active)

    def test_later_objective_does_not_add_a_second_guardian(self) -> None:
        engine = GameEngine.new(self.catalog, 4318)
        engine.state.objectives[0].facts["guardian"] = "defeated"
        objective = engine.state.objectives[1]
        mission = engine.mission_definition(objective.biome_id)
        approach = mission["approaches"][0]
        objective.approach = approach["id"]
        objective.stage = len(approach["stages"]) - 1
        objective.facts = {"approach": approach["id"]}
        engine.state.phase = "objective"
        engine.state.current_objective_id = objective.id

        engine.advance_objective()

        self.assertEqual("exploration", engine.state.phase)
        self.assertNotIn("guardian", objective.facts)


if __name__ == "__main__":
    unittest.main()
