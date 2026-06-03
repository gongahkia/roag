from __future__ import annotations

import unittest

from kenjaku.training import deterministic_split


class SplitTests(unittest.TestCase):
    def test_deterministic_split_is_stable_and_preserves_order_within_subsets(self) -> None:
        items = list(range(10))

        first_train, first_eval = deterministic_split(items, eval_fraction=0.3, seed="fixed")
        second_train, second_eval = deterministic_split(items, eval_fraction=0.3, seed="fixed")

        self.assertEqual((first_train, first_eval), (second_train, second_eval))
        self.assertEqual(len(first_train), 7)
        self.assertEqual(len(first_eval), 3)
        self.assertEqual(first_train, sorted(first_train))
        self.assertEqual(first_eval, sorted(first_eval))
        self.assertEqual(sorted(first_train + first_eval), items)

    def test_nonzero_fraction_keeps_tiny_eval_set_when_possible(self) -> None:
        train, evaluation = deterministic_split(["a", "b"], eval_fraction=0.2)

        self.assertEqual(len(train), 1)
        self.assertEqual(len(evaluation), 1)

    def test_rejects_invalid_fraction(self) -> None:
        with self.assertRaises(ValueError):
            deterministic_split([1], eval_fraction=-0.1)
        with self.assertRaises(ValueError):
            deterministic_split([1], eval_fraction=1.0)


if __name__ == "__main__":
    unittest.main()
