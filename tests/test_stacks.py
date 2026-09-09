from __future__ import annotations

import unittest
from fractions import Fraction

from dumbest_dungeon.stacks import StackMode as M, StackRule


class StackContractTests(unittest.TestCase):
    def test_all_modes_have_exact_current_and_next_results(self) -> None:
        cases = (
            (StackRule(M.LINEAR, 3), 2, 6, 9),
            (StackRule(M.MULTIPLICATIVE, 5000, max_effective_stacks=100), 2, 22500, 33750),
            (StackRule(M.INDEPENDENT_CHANCE, 5000, max_effective_stacks=100), 2, 7500, 8750),
            (StackRule(M.HYPERBOLIC, 2000, cap=8000), 2, 3200, 4000),
            (StackRule(M.THRESHOLD, 2, every=3), 2, 0, 2),
            (StackRule(M.REFRESH, 3), 2, 3, 3),
            (StackRule(M.UNIQUE, 1), 2, 1, 1),
            (StackRule(M.CONVERSION, 2, converted_from="base:old"), 2, 4, 6),
            (StackRule(M.TABLE, 0, table=(0, 1, 1, 3, 5)), 2, 1, 3),
        )
        for rule, count, current, following in cases:
            with self.subTest(mode=rule.mode):
                preview = rule.preview(count)
                self.assertEqual((current, following), (preview["current"], preview["next"]))
                self.assertEqual(0, rule.value(0))
                self.assertTrue(preview["formula"])

    def test_chance_matches_exact_rational_formula(self) -> None:
        for amount in (0, 1, 3333, 9999, 10000):
            rule = StackRule(M.INDEPENDENT_CHANCE, amount, max_effective_stacks=100)
            for count in range(1, 30):
                exact = 10000 * (1 - Fraction(10000 - amount, 10000) ** count)
                self.assertEqual(exact.numerator // exact.denominator, rule.value(count))

    def test_caps_tables_and_effective_stack_bounds_are_disclosed(self) -> None:
        rule = StackRule(M.LINEAR, 3, cap=8)
        self.assertEqual((8, 8), (rule.preview(3)["current"], rule.preview(3)["next"]))
        rule = StackRule(M.MULTIPLICATIVE, 10000, max_effective_stacks=100)
        self.assertEqual(rule.value(100), rule.value(10**100))
        self.assertEqual(100, rule.preview(100)["effective_stack_bound"])
        self.assertEqual(3 * 10**100, StackRule(M.LINEAR, 3).value(10**100))

    def test_invalid_or_dishonest_policies_fail(self) -> None:
        for factory in (
            lambda: StackRule(M.LINEAR, True),
            lambda: StackRule(M.TABLE, 0, table=(0, 3, 1)),
            lambda: StackRule(M.MULTIPLICATIVE, 1000),
            lambda: StackRule(M.CONVERSION, 1),
            lambda: StackRule(M.HYPERBOLIC, 5, cap=2),
        ):
            with self.assertRaises(ValueError):
                factory()
        self.assertEqual(1, StackRule(M.TABLE, 0, table=(0, 3, 1), monotonic=False).value(2))


if __name__ == "__main__":
    unittest.main()
