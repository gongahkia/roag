from __future__ import annotations

import importlib.util
import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.models.multi_action_policy import (
    MaskedMultiActionPolicyHead,
    MultiActionPolicyConfig,
    meld_selection_index,
)
from kenjaku.schema import LEGAL_ACTION_MASK_V1_DIM, action_v1_mask_index
from kenjaku.simulation import (
    UnifiedPolicySandboxAdapter,
    draw_for_current_seat,
    initial_sandbox_environment,
    legal_sandbox_actions,
    sandbox_policy_inputs,
    select_sandbox_action_from_logits,
)

TORCH_AVAILABLE = importlib.util.find_spec("torch") is not None


class UnifiedPolicySandboxInputTests(unittest.TestCase):
    def test_encodes_4p_and_sanma_legal_actions_with_actor_observations(self) -> None:
        for ruleset, players in (("tenhou-4p", 4), ("tenhou-3p", 3)):
            with self.subTest(ruleset=ruleset):
                state = draw_for_current_seat(initial_sandbox_environment(ruleset=ruleset, seed=7))
                actions = legal_sandbox_actions(state, seat=0)
                inputs = sandbox_policy_inputs(state, seat=0, legal_actions=actions)

                self.assertEqual(inputs.observation.ruleset, ruleset)
                self.assertEqual(inputs.observation.players, players)
                self.assertEqual(len(inputs.observation_tensor), 546)
                self.assertEqual(len(inputs.action_mask.mask), LEGAL_ACTION_MASK_V1_DIM)
                self.assertTrue(
                    all(inputs.action_mask.mask[index] for index, _ in inputs.action_candidates)
                )

    def test_resolves_collapsed_chi_actions_with_meld_scores(self) -> None:
        state = draw_for_current_seat(initial_sandbox_environment(ruleset="tenhou-4p", seed=9))
        first = Action(
            ActionKind.CHI,
            TileType.parse("3m"),
            consumed=(Tile.parse("1m"), Tile.parse("2m")),
        )
        second = Action(
            ActionKind.CHI,
            TileType.parse("3m"),
            consumed=(Tile.parse("4m"), Tile.parse("5m")),
        )
        inputs = sandbox_policy_inputs(state, seat=0, legal_actions=(first, second))
        chi_index = action_v1_mask_index_from_action(first)
        logits = [-10.0] * LEGAL_ACTION_MASK_V1_DIM
        logits[chi_index] = 1.0

        selected = select_sandbox_action_from_logits(
            inputs,
            logits,
            meld_scores={
                meld_selection_index(first): 0.0,
                meld_selection_index(second): 1.0,
            },
        )

        self.assertEqual(selected, second)

    def test_rejects_invalid_logit_shape(self) -> None:
        state = draw_for_current_seat(initial_sandbox_environment(ruleset="tenhou-3p", seed=4))
        inputs = sandbox_policy_inputs(state, seat=0)

        with self.assertRaisesRegex(ValueError, "276"):
            select_sandbox_action_from_logits(inputs, (0.0,))


@unittest.skipUnless(TORCH_AVAILABLE, "PyTorch is not available")
class UnifiedPolicySandboxAdapterTests(unittest.TestCase):
    def test_selects_one_legal_sanma_action_deterministically(self) -> None:
        state = draw_for_current_seat(initial_sandbox_environment(ruleset="tenhou-3p", seed=5))
        legal_actions = legal_sandbox_actions(state, seat=0)
        adapter = UnifiedPolicySandboxAdapter(
            MaskedMultiActionPolicyHead(MultiActionPolicyConfig(hidden_dim=8), seed=11)
        )

        first = adapter.select_action(state, seat=0, legal_actions=legal_actions)
        second = adapter.select_action(state, seat=0, legal_actions=legal_actions)

        self.assertIn(first, legal_actions)
        self.assertEqual(first, second)


def action_v1_mask_index_from_action(action: Action) -> int:
    from kenjaku.schema import ActionV1

    return action_v1_mask_index(ActionV1.from_core(action, ruleset="tenhou-4p"))


if __name__ == "__main__":
    unittest.main()
