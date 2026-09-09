from __future__ import annotations

import unittest
from dataclasses import replace

from dumbest_dungeon.acquisition import ContentPack, ContentRef, Lane, eligible_techniques, validate_packs
from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.threat import Threat, ThreatBudget


class AcquisitionContractTests(unittest.TestCase):
    def test_owner_lane_and_pack_filters_are_intersected(self) -> None:
        catalog = load_catalog()
        card = {**catalog.cards["brace"], "lanes": ["elite"]}
        catalog = replace(catalog, cards={**catalog.cards, "brace": card})
        members = (ContentRef("cards", "brace"),)
        self.assertEqual((), eligible_techniques(catalog, {"warden"}, Lane.NORMAL, members=members))
        self.assertEqual(("brace",), eligible_techniques(catalog, {"warden"}, Lane.ELITE, members=members))
        self.assertEqual((), eligible_techniques(catalog, {"medic"}, Lane.ELITE, members=members))

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


if __name__ == "__main__":
    unittest.main()
