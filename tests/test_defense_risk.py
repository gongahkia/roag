from __future__ import annotations

import unittest

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.training import (
    DiscardExample,
    RoundOutcome,
    candidate_defense_risk,
    legal_candidate_defense_risks,
    summarize_defense_risk_outcomes,
    summarize_defense_risks,
)


class DefenseRiskTests(unittest.TestCase):
    def test_no_active_riichi_has_zero_candidate_risk(self) -> None:
        example = _example(active_riichi=False)

        score = candidate_defense_risk(example, TileType.parse("5m"))

        self.assertEqual(score.risk, 0.0)
        self.assertFalse(score.calibrated_probability)
        self.assertEqual(score.active_riichi_opponents, 0)
        self.assertEqual(score.danger_reasons, ("no_active_riichi_opponent",))

    def test_genbutsu_scores_safer_than_live_middle_tile(self) -> None:
        example = _example(opponent_river=["4m"], hand=["4m", "5m"])

        genbutsu = candidate_defense_risk(example, TileType.parse("4m"))
        live_middle = candidate_defense_risk(example, TileType.parse("5m"))

        self.assertLess(genbutsu.risk, live_middle.risk)
        self.assertIn("genbutsu", genbutsu.safety_reasons)
        self.assertIn("mostly_live", live_middle.danger_reasons)
        self.assertFalse(live_middle.calibrated_probability)

    def test_suji_kabe_and_one_chance_reduce_risk(self) -> None:
        suji = _example(opponent_river=["4m"], hand=["7m", "8m"])
        kabe = _example(visible_tiles=["4p", "4p", "4p", "4p"], hand=["5p", "6p"])
        one_chance = _example(visible_tiles=["4s", "4s", "4s"], hand=["5s", "6s"])

        suji_score = candidate_defense_risk(suji, TileType.parse("7m"))
        kabe_score = candidate_defense_risk(kabe, TileType.parse("5p"))
        one_chance_score = candidate_defense_risk(one_chance, TileType.parse("5s"))

        self.assertIn("suji", suji_score.safety_reasons)
        self.assertIn("kabe", kabe_score.safety_reasons)
        self.assertIn("one_chance", one_chance_score.safety_reasons)
        self.assertLess(kabe_score.risk, one_chance_score.risk)

    def test_ippatsu_and_multiple_riichi_raise_risk(self) -> None:
        normal = _example(opponent_river=["1m"], hand=["5m", "6m"], riichi_turn=1)
        urgent = _example(
            opponent_river=["1m"],
            second_opponent_river=["9p"],
            hand=["5m", "6m"],
            riichi_turn=1,
            second_riichi_turn=0,
            ippatsu_active=(False, True, True, False),
        )

        normal_score = candidate_defense_risk(normal, TileType.parse("5m"))
        urgent_score = candidate_defense_risk(urgent, TileType.parse("5m"))

        self.assertGreater(urgent_score.risk, normal_score.risk)
        self.assertEqual(urgent_score.active_riichi_opponents, 2)
        self.assertIn("multiple_active_riichi", urgent_score.danger_reasons)
        self.assertIn("ippatsu", urgent_score.danger_reasons)

    def test_legal_candidate_scores_are_sorted_by_risk_descending(self) -> None:
        example = _example(opponent_river=["4m"], hand=["4m", "5m", "7m"])

        scores = legal_candidate_defense_risks(example)

        self.assertEqual([score.tile.notation for score in scores], ["5m", "7m", "4m"])
        self.assertGreaterEqual(scores[0].risk, scores[1].risk)
        self.assertGreaterEqual(scores[1].risk, scores[2].risk)

    def test_summarizes_actual_and_candidate_risk(self) -> None:
        no_riichi = _example(active_riichi=False, hand=["5m", "6m"])
        active_riichi = _example(opponent_river=["4m"], hand=["4m", "5m"])

        summary = summarize_defense_risks([no_riichi, active_riichi])

        self.assertEqual(summary["kind"], "kenjaku-defense-risk-summary-v0")
        self.assertEqual(summary["examples"], 2)
        self.assertEqual(summary["active_riichi_examples"], 1)
        self.assertFalse(summary["calibrated_probability"])
        self.assertEqual(summary["actual_discard_risk"]["examples"], 2)
        self.assertEqual(summary["highest_candidate_risk"]["examples"], 2)
        self.assertEqual(
            sum(bucket["examples"] for bucket in summary["actual_discard_risk_bands"].values()),
            2,
        )
        self.assertIn(
            {"reason": "no_active_riichi_opponent", "count": 1},
            summary["actual_danger_reasons"],
        )

    def test_summarizes_risk_against_terminal_outcomes(self) -> None:
        no_riichi = _example(active_riichi=False, hand=["5m", "6m"])
        active_riichi = _example(opponent_river=["4m"], hand=["5m", "4m"], round_index=1)
        outcomes = (
            _outcome(deal_in_flags=(False, False, False, False)),
            _outcome(deal_in_flags=(True, False, False, False)),
        )

        summary = summarize_defense_risk_outcomes([no_riichi, active_riichi], outcomes)

        self.assertEqual(summary["kind"], "kenjaku-defense-risk-outcome-analysis-v0")
        self.assertEqual(summary["examples"], 2)
        self.assertEqual(summary["labeled_examples"], 2)
        self.assertEqual(summary["missing_outcomes"], 0)
        self.assertFalse(summary["calibrated_probability"])
        self.assertEqual(summary["buckets"]["eventual_deal_in"]["examples"], 1)
        self.assertEqual(summary["buckets"]["no_eventual_deal_in"]["examples"], 1)
        self.assertEqual(summary["buckets"]["active_riichi_eventual_deal_in"]["examples"], 1)
        self.assertGreater(
            summary["buckets"]["eventual_deal_in"]["mean"],
            summary["buckets"]["no_eventual_deal_in"]["mean"],
        )


def _example(
    *,
    round_index: int = 0,
    active_riichi: bool = True,
    opponent_river: list[str] | None = None,
    second_opponent_river: list[str] | None = None,
    visible_tiles: list[str] | None = None,
    hand: list[str] | None = None,
    riichi_turn: int | None = 0,
    second_riichi_turn: int | None = None,
    ippatsu_active: tuple[bool, ...] = (False, False, False, False),
) -> DiscardExample:
    hand_tiles = tuple(Tile.parse(tile) for tile in hand or ["5m", "6m"])
    opponent_river_tiles = tuple(Tile.parse(tile) for tile in opponent_river or [])
    second_opponent_river_tiles = tuple(Tile.parse(tile) for tile in second_opponent_river or [])
    visible = tuple(Tile.parse(tile) for tile in visible_tiles or [])
    rivers_by_seat = (
        (),
        opponent_river_tiles,
        second_opponent_river_tiles,
        (),
    )
    river_counts_by_seat = tuple(tile_counts(river) for river in rivers_by_seat)
    active_riichi_seats = (
        False,
        active_riichi,
        second_riichi_turn is not None,
        False,
    )
    return DiscardExample(
        round_index=round_index,
        event_index=0,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=tile_counts(hand_tiles),
        visible_counts=tile_counts(
            (*hand_tiles, *opponent_river_tiles, *second_opponent_river_tiles, *visible)
        ),
        action=Action.discard(hand_tiles[0].type),
        active_riichi_seats=active_riichi_seats,
        river_counts_by_seat=river_counts_by_seat,
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=(None, riichi_turn, second_riichi_turn, None),
        riichi_declared_event_indices=(None, 1, 2, None),
        ippatsu_active_seats=ippatsu_active,
    )


def _outcome(*, deal_in_flags: tuple[bool, bool, bool, bool]) -> RoundOutcome:
    return RoundOutcome(
        kind="agari",
        event_index=99,
        winner_seats=(1,),
        from_seats=(0,) if deal_in_flags[0] else (1,),
        score_deltas=None,
        win_flags=(False, True, False, False),
        deal_in_flags=deal_in_flags,
        draw_flags=(False, False, False, False),
    )


if __name__ == "__main__":
    unittest.main()
