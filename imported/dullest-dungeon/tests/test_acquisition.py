from __future__ import annotations

import unittest
from dataclasses import replace

from dumbest_dungeon.acquisition import ContentPack, ContentRef, Lane, eligible_techniques, validate_packs
from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.threat import Threat, ThreatBudget, enemy_threat, formation_threat, threat_budget


class AcquisitionContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_owner_lane_and_pack_filters_are_intersected(self) -> None:
        catalog = load_catalog()
        card = {**catalog.cards["brace"], "lanes": ["elite"]}
        catalog = replace(catalog, cards={**catalog.cards, "brace": card})
        members = (ContentRef("cards", "brace"),)
        self.assertEqual((), eligible_techniques(catalog, {"warden"}, Lane.NORMAL, members=members))
        self.assertEqual(("brace",), eligible_techniques(catalog, {"warden"}, Lane.ELITE, members=members))
        self.assertEqual((), eligible_techniques(catalog, {"medic"}, Lane.ELITE, members=members))

    def test_live_reward_generator_cannot_leak_across_lanes(self) -> None:
        catalog = load_catalog()
        cards = {
            identity: {**card, "lanes": ["normal"] if identity == "brace" else ["elite"]}
            for identity, card in catalog.cards.items()
        }
        engine = GameEngine.new(replace(catalog, cards=cards), 42)
        self.assertEqual(["brace"], engine._generate_card_rewards(3, Lane.NORMAL))
        offer = engine.state.ledger.records[-1]
        self.assertEqual("normal", offer.data["lane"])
        self.assertEqual(1, offer.data["eligible_count"])

    def test_pack_references_requirements_and_exclusions_are_strict(self) -> None:
        catalog = load_catalog()
        base = ContentPack("base:core", (ContentRef("cards", "brace"),))
        extra = ContentPack("expansion:engine", (ContentRef("cards", "breach"),), requires=("base:core",))
        self.assertEqual(tuple(sorted(base.members + extra.members)), validate_packs(catalog, (extra, base), (base.id, extra.id)))
        with self.assertRaisesRegex(ValueError, "disabled requirements"):
            validate_packs(catalog, (base, extra), (extra.id,))
        conflict = ContentPack("challenge:bare", (), excludes=(extra.id,))
        with self.assertRaisesRegex(ValueError, "conflicts"):
            validate_packs(catalog, (base, extra, conflict), (base.id, extra.id, conflict.id))
        with self.assertRaisesRegex(ValueError, "unknown content"):
            validate_packs(catalog, (ContentPack("bad:pack", (ContentRef("cards", "missing"),)),), ())

    def test_vector_ceiling_cannot_be_bypassed_by_low_total(self) -> None:
        budget = ThreatBudget(100, Threat(60, 15, 10, 4, 8, 8, 8))
        self.assertTrue(budget.permits(Threat(durability=50, burst=8)))
        self.assertFalse(budget.permits(Threat(durability=1, burst=11)))
        self.assertEqual(Threat(burst=3, control=4), Threat(burst=3) + Threat(control=4))
        with self.assertRaises(ValueError):
            Threat(control=True)

    def test_enemy_and_composition_threat_price_three_phase_coordination(self) -> None:
        plain = enemy_threat(self.catalog.enemies["hollow_crew"])
        self.assertEqual(self.catalog.enemies["hollow_crew"]["max_hp"], plain.durability)
        self.assertGreaterEqual(plain.sustained, plain.burst)
        combo = formation_threat(self.catalog, ["rad_acolyte", "control_rod"])
        parts = enemy_threat(self.catalog.enemies["rad_acolyte"]) + enemy_threat(
            self.catalog.enemies["control_rod"]
        )
        self.assertGreater(combo.total, parts.total)
        self.assertTrue(threat_budget("normal").permits(
            formation_threat(self.catalog, self.catalog.encounters["lost_shift"]["enemies"])
        ))
        with self.assertRaises(ValueError):
            threat_budget("boss")

    def test_all_authored_normal_and_elite_templates_obey_dimensional_ceilings(self) -> None:
        for encounter in self.catalog.encounters.values():
            if encounter["kind"] not in {"normal", "elite"}:
                continue
            with self.subTest(encounter=encounter["id"]):
                vector = formation_threat(self.catalog, encounter["enemies"])
                self.assertTrue(threat_budget(encounter["kind"]).permits(vector), vector)


if __name__ == "__main__":
    unittest.main()
