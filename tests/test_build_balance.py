from __future__ import annotations

import unittest

from jomon.build_balance import (
    CHALLENGES,
    build_balance_audit,
    build_capabilities,
    validate_build_balance,
    weapon_tactical_roles,
)
from jomon.build_scenarios import BUILD_SCENARIOS
from jomon.content import WEAPONS


class BuildBalanceTests(unittest.TestCase):
    def test_every_build_has_a_real_answer_to_each_pressure_family(self):
        result = build_balance_audit()
        self.assertEqual(result["failures"], [])
        self.assertEqual(result["builds"], 24)
        self.assertEqual(result["challenge_families"], 6)
        self.assertTrue(all(count >= 6 for count in result["challenge_coverage"].values()))
        for build in BUILD_SCENARIOS:
            row = result["coverage"][build.id]
            self.assertEqual(set(row["answers"]), {challenge.id for challenge in CHALLENGES})
            self.assertTrue(all(row["answers"].values()), build.id)
            self.assertEqual(set(row["capabilities"]), set(build_capabilities(build)))

    def test_every_weapon_has_a_non_damage_tactical_identity(self):
        validate_build_balance()
        self.assertEqual(len(WEAPONS), 72)
        for weapon in WEAPONS:
            self.assertTrue(set(weapon_tactical_roles(weapon)) - {"damage"}, weapon)


if __name__ == "__main__":
    unittest.main()
