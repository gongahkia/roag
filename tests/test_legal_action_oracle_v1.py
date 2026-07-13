from __future__ import annotations

import json
import unittest

from kenjaku.core import Tile
from kenjaku.schema import LEGAL_ACTION_MASK_V1_PASS_INDEX, action_v1_mask_index
from kenjaku.simulation import (
    LEGAL_ACTION_ORACLE_V1_KIND,
    SandboxEnvironmentState,
    draw_for_current_seat,
    initial_sandbox_environment,
    legal_action_oracle_v1,
    legal_sandbox_actions,
)
from kenjaku.simulation.legal_action_oracle import LegalActionOracleV1


class LegalActionOracleV1Tests(unittest.TestCase):
    def test_four_player_oracle_matches_sandbox_actions_and_round_trips(self) -> None:
        state = draw_for_current_seat(
            initial_sandbox_environment(ruleset="tenhou-4p", seed="oracle")
        )
        oracle = legal_action_oracle_v1(state)
        expected = tuple(
            action.kind.value for action in legal_sandbox_actions(state, seat=state.current_seat)
        )
        payload = oracle.to_dict()

        self.assertEqual(oracle.ruleset, "tenhou-4p")
        self.assertEqual(oracle.seat, state.current_seat)
        self.assertEqual(tuple(action.action for action in oracle.actions), expected)
        self.assertEqual(payload["kind"], LEGAL_ACTION_ORACLE_V1_KIND)
        self.assertEqual(LegalActionOracleV1.from_dict(payload), oracle)
        self.assertEqual(json.loads(oracle.to_json()), payload)
        self.assertEqual(
            {action_v1_mask_index(action) for action in oracle.mask.actions()},
            {action_v1_mask_index(action) for action in oracle.actions},
        )

    def test_sanma_oracle_includes_kita_and_excludes_chi(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("8s"),),
            hands=(
                _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s N"),
                (),
                (),
            ),
        )
        oracle = legal_action_oracle_v1(draw_for_current_seat(state))

        self.assertIn("kita", {action.action for action in oracle.actions})
        self.assertNotIn("chi", {action.action for action in oracle.actions})
        self.assertNotIn("chi", {action.action for action in oracle.mask.actions()})

    def test_rejects_empty_and_mismatched_masks(self) -> None:
        oracle = legal_action_oracle_v1(
            draw_for_current_seat(initial_sandbox_environment(ruleset="tenhou-4p", seed="oracle"))
        )
        payload = oracle.to_dict()
        payload["mask"]["mask"] = [False] * len(payload["mask"]["mask"])

        with self.assertRaisesRegex(ValueError, "cannot be empty"):
            LegalActionOracleV1.from_dict(payload)
        payload = oracle.to_dict()
        payload["mask"]["mask"] = [False] * len(payload["mask"]["mask"])
        payload["mask"]["mask"][LEGAL_ACTION_MASK_V1_PASS_INDEX] = True
        with self.assertRaisesRegex(ValueError, "must encode the oracle actions"):
            LegalActionOracleV1.from_dict(payload)
def _tiles(value: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in value.split())
