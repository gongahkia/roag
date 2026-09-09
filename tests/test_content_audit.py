from __future__ import annotations

import unittest
from dataclasses import replace

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.content_audit import audit_content, effect_shape


class ContentAuditTests(unittest.TestCase):
    def test_shape_preserves_conditions_targets_sign_and_order(self) -> None:
        self.assertEqual(effect_shape({"op": "damage", "amount": 7}),
                         effect_shape({"amount": 19, "op": "damage"}))
        for change in ({"op": "heal", "amount": 7},
                       {"op": "damage", "amount": -7},
                       {"op": "damage", "amount": 7, "if_status": "marked"}):
            self.assertNotEqual(effect_shape({"op": "damage", "amount": 7}), effect_shape(change))

    def test_census_is_independent_of_catalog_enumeration(self) -> None:
        catalog = load_catalog()
        reversed_catalog = replace(catalog, **{
            key: dict(reversed(list(getattr(catalog, key).items())))
            for key in ("cards", "heroes", "enemies", "encounters", "biomes")
        })
        self.assertEqual(audit_content(catalog), audit_content(reversed_catalog))


if __name__ == "__main__":
    unittest.main()
