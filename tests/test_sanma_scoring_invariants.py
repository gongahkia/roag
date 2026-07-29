from __future__ import annotations

import random
import unittest

from kenjaku.core import score_riichi_hand


class SanmaScoringInvariantTests(unittest.TestCase):
    def test_generated_ron_and_chankan_ledgers_are_conservative(self) -> None:
        for fixture in _score_fixtures():
            with self.subTest(fixture=fixture):
                ron = score_riichi_hand(**fixture, win_kind="ron", players=3)
                chankan = score_riichi_hand(**fixture, win_kind="chankan", players=3)

                self.assertEqual(ron.ron_payment, chankan.ron_payment)
                self.assertEqual(ron.total_ron_payment, chankan.total_ron_payment)
                self.assertEqual(ron.honba_payment, fixture["honba"] * 300)
                self.assertEqual(ron.riichi_stick_points, fixture["riichi_sticks"] * 1000)
                self.assertEqual(ron.total_ron_payment, ron.ron_payment + ron.honba_payment)
                self.assertEqual(
                    ron.winner_total_points,
                    ron.total_ron_payment + ron.riichi_stick_points,
                )

    def test_generated_tsumo_ledgers_match_two_sanma_losers(self) -> None:
        for fixture in _score_fixtures():
            with self.subTest(fixture=fixture):
                result = score_riichi_hand(**fixture, win_kind="tsumo", players=3)
                child_payment = result.total_tsumo_child_payment

                self.assertIsNotNone(child_payment)
                if fixture["is_dealer"]:
                    self.assertIsNone(result.total_tsumo_dealer_payment)
                    paid_by_losers = 2 * child_payment
                else:
                    dealer_payment = result.total_tsumo_dealer_payment
                    self.assertIsNotNone(dealer_payment)
                    paid_by_losers = child_payment + dealer_payment
                self.assertEqual(
                    result.winner_total_points,
                    paid_by_losers + result.riichi_stick_points,
                )
                self.assertEqual(result.honba_payment, fixture["honba"] * 100)

    def test_payments_are_monotonic_for_generated_han_and_fu_ranges(self) -> None:
        fu_values = (20, 25, 30, 40, 50, 60, 70, 80, 90, 100, 110)
        for is_dealer in (False, True):
            for fu in fu_values:
                payments = [
                    score_riichi_hand(
                        yaku_han=han,
                        fu=fu,
                        is_dealer=is_dealer,
                        win_kind="ron",
                        players=3,
                    ).ron_payment
                    for han in range(1, 14)
                ]
                self.assertEqual(payments, sorted(payments))
            for han in range(1, 5):
                payments = [
                    score_riichi_hand(
                        yaku_han=han,
                        fu=fu,
                        is_dealer=is_dealer,
                        win_kind="ron",
                        players=3,
                    ).ron_payment
                    for fu in fu_values
                ]
                self.assertEqual(payments, sorted(payments))


def _score_fixtures() -> tuple[dict[str, int | bool], ...]:
    rng = random.Random(0x53414E4D41)
    fu_values = (20, 25, 30, 40, 50, 60, 70, 80, 90, 100, 110)
    return tuple(
        {
            "yaku_han": rng.randint(1, 13),
            "bonus_han": rng.randint(0, 4),
            "fu": rng.choice(fu_values),
            "is_dealer": bool(rng.getrandbits(1)),
            "honba": rng.randint(0, 8),
            "riichi_sticks": rng.randint(0, 5),
        }
        for _case in range(256)
    )
