from __future__ import annotations

import json
import unittest
from dataclasses import replace

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.manifest import canonical_bytes


class ManifestTests(unittest.TestCase):
    def test_key_and_catalog_enumeration_do_not_change_identity(self) -> None:
        catalog = load_catalog()
        reordered = replace(catalog, **{
            name: dict(reversed(list(getattr(catalog, name).items())))
            for name in catalog.__dataclass_fields__
        })
        self.assertEqual(catalog.manifest, reordered.manifest)
        self.assertEqual(canonical_bytes({"b": 2, "a": 1}), canonical_bytes({"a": 1, "b": 2}))

    def test_effect_order_and_balance_are_part_of_identity(self) -> None:
        catalog = load_catalog()
        cards = json.loads(json.dumps(catalog.cards))
        card = next(card for card in cards.values() if len(card["effects"]) > 1)
        card["effects"].reverse()
        self.assertNotEqual(catalog.manifest.fingerprint, replace(catalog, cards=cards).manifest.fingerprint)
        changed = replace(catalog, balance={**catalog.balance, "death_chance": 1})
        self.assertNotEqual(catalog.manifest.fingerprint, changed.manifest.fingerprint)

    def test_manifest_has_separate_versioned_contracts_and_stable_pack_ids(self) -> None:
        manifest = load_catalog().manifest.snapshot()
        self.assertEqual({"schema", "engine", "content_schema", "rng_architecture", "fingerprint", "enabled_packs"}, set(manifest))
        self.assertEqual(["base:core"], manifest["enabled_packs"])
        self.assertEqual(64, len(manifest["fingerprint"]))
        self.assertEqual(manifest, json.loads(canonical_bytes(manifest)))


if __name__ == "__main__":
    unittest.main()
