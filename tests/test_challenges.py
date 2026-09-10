from __future__ import annotations

from datetime import date
import unittest

from dumbest_dungeon.challenges import (
    CONTRACTS, ChallengeError, ExpeditionConfig, daily_config, daily_seed,
    decode_code, encode_code, validate_config,
)
from dumbest_dungeon.content import load_catalog


class ChallengeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_twenty_contracts_teach_distinct_runtime_modifiers(self) -> None:
        self.assertEqual(20, len(CONTRACTS))
        self.assertEqual(len(CONTRACTS), len({item.id for item in CONTRACTS}))
        self.assertEqual(len(CONTRACTS), len({item.modifier for item in CONTRACTS}))
        self.assertTrue(all(item.id.startswith("base:") for item in CONTRACTS))
        self.assertTrue(all(len(item.rule) <= 80 and item.lesson for item in CONTRACTS))

    def test_daily_seed_is_stable_and_date_sensitive(self) -> None:
        self.assertEqual(8404185418283187962, daily_seed(date(2026, 9, 10)))
        self.assertNotEqual(daily_seed(date(2026, 9, 10)), daily_seed(date(2026, 9, 11)))
        self.assertEqual(daily_config(date(2026, 9, 10)), daily_config(date(2026, 9, 10)))

    def test_code_round_trips_full_custom_configuration(self) -> None:
        party = tuple(list(self.catalog.heroes)[:4])
        loadout = next(item for item in self.catalog.loadouts.values() if item["hero"] == party[0])
        config = ExpeditionConfig(
            seed=2**63 + 91, party=party, loadouts=((party[0], loadout["id"]),),
            biomes=tuple(list(self.catalog.biomes)[:4]), layout="ring",
            starting_pressure=480, ladder_rank=7,
            modifiers=("accelerated_pressure", "elite_weather"),
        )
        code = encode_code(config, self.catalog)
        self.assertLess(len(code), 400)
        self.assertEqual(config, decode_code(code, self.catalog))

    def test_strict_code_rejects_tampering_and_incompatible_fields(self) -> None:
        code = encode_code(ExpeditionConfig(seed=9), self.catalog)
        with self.assertRaises(ChallengeError):
            decode_code(code[:-1] + ("0" if code[-1] != "0" else "1"), self.catalog)
        with self.assertRaises(ChallengeError):
            validate_config(ExpeditionConfig(seed=True), self.catalog)
        with self.assertRaises(ChallengeError):
            validate_config(ExpeditionConfig(seed=1, modifiers=("unknown",)), self.catalog)
        with self.assertRaises(ChallengeError):
            validate_config(ExpeditionConfig(seed=1, content_packs=("base:future",)), self.catalog)


if __name__ == "__main__":
    unittest.main()
