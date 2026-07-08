from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.training.ppo import (
    PPO_ACTION_DIM,
    PPO_ACTION_KIND_OFFSETS,
    PPO_PASS_ACTION_INDEX,
    PPO_RIICHI_ACTION_INDEX,
    PPO_STATE_DIM,
    SandboxLinearPpoActorCritic,
    format_ppo_sandbox_report,
    load_ppo_sandbox_checkpoint,
    ppo_action_index,
    ppo_legal_action_mask,
    ppo_state_features,
)


class PpoTests(unittest.TestCase):
    def test_ppo_state_features_encodes_drawn_tile_and_padding(self) -> None:
        features = ppo_state_features(_entry(drawn_tile="5m", points=[25000, 24000]))

        self.assertEqual(len(features), PPO_STATE_DIM)
        self.assertEqual(features[17 + 4], 1.0)
        self.assertEqual(features[13:17], [0.25, 0.24, 0.0, 0.0])

    def test_ppo_action_index_maps_tile_and_terminal_actions(self) -> None:
        self.assertEqual(ppo_action_index({"kind": "discard", "tile": "5m"}), 4)
        self.assertEqual(
            ppo_action_index({"kind": "ron", "tile": "1m"}), PPO_ACTION_KIND_OFFSETS["ron"]
        )
        self.assertEqual(ppo_action_index({"kind": "pass"}), PPO_PASS_ACTION_INDEX)
        self.assertEqual(ppo_action_index({"kind": "riichi"}), PPO_RIICHI_ACTION_INDEX)

    def test_ppo_legal_action_mask_rejects_empty_and_malformed_actions(self) -> None:
        mask = ppo_legal_action_mask([{"kind": "pass"}, {"kind": "discard", "tile": "1m"}])

        self.assertEqual(len(mask), PPO_ACTION_DIM)
        self.assertTrue(mask[PPO_PASS_ACTION_INDEX])
        with self.assertRaisesRegex(ValueError, "cannot be empty"):
            ppo_legal_action_mask([])
        with self.assertRaisesRegex(ValueError, "requires tile"):
            ppo_action_index({"kind": "discard"})

    def test_actor_critic_state_dict_round_trips(self) -> None:
        model = SandboxLinearPpoActorCritic(hidden_dim=0, seed=7)
        restored = SandboxLinearPpoActorCritic(hidden_dim=0, state=model.state_dict())
        state = [0.0] * PPO_STATE_DIM
        legal_mask = ppo_legal_action_mask([{"kind": "pass"}])

        self.assertEqual(restored.state_dict(), model.state_dict())
        self.assertEqual(restored.logits(state, legal_mask), model.logits(state, legal_mask))

    def test_actor_critic_rejects_bad_dimensions(self) -> None:
        with self.assertRaisesRegex(ValueError, "input_dim"):
            SandboxLinearPpoActorCritic(input_dim=0)
        with self.assertRaisesRegex(ValueError, "hidden_dim"):
            SandboxLinearPpoActorCritic(hidden_dim=-1)

    def test_load_checkpoint_and_format_report_reject_bad_kind(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "bad.json"
            path.write_text(json.dumps({"kind": "bad"}), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "checkpoint kind"):
                load_ppo_sandbox_checkpoint(path)
        with self.assertRaisesRegex(ValueError, "report kind"):
            format_ppo_sandbox_report({"kind": "bad"})


def _entry(*, drawn_tile: str, points: list[int]) -> dict[str, object]:
    return {
        "seat": 0,
        "decision_type": "discard",
        "state": {
            "turn": 1,
            "current_seat": 0,
            "dealer_seat": 0,
            "honba": 0,
            "wall_remaining": 60,
            "needs_discard": True,
            "pending_reaction_seats": [],
            "round_wind": "E",
            "points": points,
            "drawn_tile": drawn_tile,
        },
    }


if __name__ == "__main__":
    unittest.main()
