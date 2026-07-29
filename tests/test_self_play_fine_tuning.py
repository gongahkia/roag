from __future__ import annotations

import importlib.util
import unittest
from dataclasses import replace
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.core import TileType
from kenjaku.models.multi_action_policy import (
    MULTI_ACTION_POLICY_HEAD_KIND,
    MaskedMultiActionPolicyHead,
    MultiActionPolicyConfig,
)
from kenjaku.schema import CheckpointManifestV1
from kenjaku.simulation import run_self_play_match_sandbox
from kenjaku.simulation.config import tenhou_4p_default
from kenjaku.training.behavior_distillation import (
    BehaviorDistillationTrainingResult,
    load_behavior_distillation_checkpoint,
    save_behavior_distillation_checkpoint,
)
from kenjaku.training.onnx_export import export_multi_action_checkpoint_to_onnx
from kenjaku.training.self_play_fine_tuning import (
    SELF_PLAY_PPO_FINE_TUNING_KIND,
    fine_tune_behavior_checkpoint_with_self_play_ppo,
    save_self_play_fine_tuned_checkpoint,
)

TORCH_AVAILABLE = importlib.util.find_spec("torch") is not None


class MatchActionSelectorTests(unittest.TestCase):
    def test_selector_receives_only_legal_actions_deterministically_for_both_rulesets(self) -> None:
        for ruleset in ("tenhou-4p", "tenhou-3p"):
            selected: list[tuple[int, str]] = []

            def first_action(state, seat, actions, recorded=selected):
                recorded.append((seat, state.ruleset))
                return actions[0]

            first = run_self_play_match_sandbox(
                games=1,
                max_rounds=1,
                max_turns_per_round=6,
                seed="selector-test",
                ruleset=ruleset,
                action_selector=first_action,
                include_trajectories=True,
            )
            second = run_self_play_match_sandbox(
                games=1,
                max_rounds=1,
                max_turns_per_round=6,
                seed="selector-test",
                ruleset=ruleset,
                action_selector=lambda _state, _seat, actions: actions[0],
                include_trajectories=True,
            )

            self.assertTrue(selected)
            self.assertTrue(
                all(observed_ruleset == ruleset for _seat, observed_ruleset in selected)
            )
            self.assertEqual(first, second)
            for decision in first["game_summaries"][0]["trajectory"]:
                self.assertIn(decision["chosen_action"], decision["legal_actions"])


@unittest.skipUnless(TORCH_AVAILABLE, "PyTorch is not available")
class SelfPlayFineTuningTests(unittest.TestCase):
    def test_fine_tunes_and_saves_a_validated_behavior_checkpoint(self) -> None:
        with TemporaryDirectory() as directory:
            source = Path(directory) / "distilled.pt"
            destination = Path(directory) / "fine-tuned.pt"
            onnx_destination = Path(directory) / "fine-tuned.onnx"
            manifest = CheckpointManifestV1.for_current_schemas(
                checkpoint_id="self-play-test",
                model_kind=MULTI_ACTION_POLICY_HEAD_KIND,
                model_version="0.2.0",
                rulesets=("tenhou-4p", "tenhou-3p"),
            )
            config = MultiActionPolicyConfig(hidden_dim=8)
            model = MaskedMultiActionPolicyHead(config, seed=7)
            save_behavior_distillation_checkpoint(
                BehaviorDistillationTrainingResult(
                    model=model,
                    device="cpu",
                    train_metrics={},
                    eval_metrics={},
                    history=[],
                    optimizer_state={},
                    completed_epochs=0,
                    config=config,
                    seed=7,
                    checkpoint_manifest=manifest,
                ),
                source,
            )
            with self.assertRaisesRegex(ValueError, "did not complete"):
                fine_tune_behavior_checkpoint_with_self_play_ppo(
                    source,
                    updates=1,
                    rollout_games=1,
                    max_rounds=1,
                    max_turns_per_round=6,
                    seed=11,
                    device="cpu",
                )
            with self.assertRaisesRegex(RuntimeError, "produced no batches"):
                fine_tune_behavior_checkpoint_with_self_play_ppo(
                    source,
                    updates=1,
                    rollout_games=1,
                    max_rounds=4,
                    max_turns_per_round=400,
                    seed=11,
                    learning_rate=1.0,
                    max_kl=1.0e-10,
                    device="cpu",
                    rule_config=_one_wind_complete_match_config(),
                )

            result = fine_tune_behavior_checkpoint_with_self_play_ppo(
                source,
                updates=1,
                rollout_games=1,
                max_rounds=4,
                max_turns_per_round=400,
                seed=11,
                ppo_epochs=1,
                batch_size=8,
                learning_rate=0.0001,
                device="cpu",
                rule_config=_one_wind_complete_match_config(),
            )
            save_self_play_fine_tuned_checkpoint(result, destination)
            loaded = load_behavior_distillation_checkpoint(destination)
            exported = export_multi_action_checkpoint_to_onnx(destination, onnx_destination)

            self.assertEqual(result.report["kind"], SELF_PLAY_PPO_FINE_TUNING_KIND)
            self.assertEqual(result.report["training"]["updates"], 1)
            self.assertGreater(result.report["training"]["history"][0]["rollout"]["transitions"], 0)
            self.assertEqual(
                result.report["training"]["history"][0]["rollout"]["completed_games"], 1
            )
            self.assertEqual(loaded["checkpoint_manifest"], manifest)
            self.assertEqual(
                loaded["model"]["config"],
                {"input_dim": 546, "hidden_dim": 8, "action_dim": 276},
            )
            self.assertEqual(exported.path, onnx_destination)

    def test_rejects_non_conservative_clip_or_kl_limits_before_loading(self) -> None:
        with self.assertRaisesRegex(ValueError, "clip_epsilon"):
            fine_tune_behavior_checkpoint_with_self_play_ppo(
                "missing.pt",
                updates=1,
                rollout_games=1,
                max_rounds=1,
                max_turns_per_round=1,
                seed=1,
                clip_epsilon=0.21,
            )
        with self.assertRaisesRegex(ValueError, "max_kl"):
            fine_tune_behavior_checkpoint_with_self_play_ppo(
                "missing.pt",
                updates=1,
                rollout_games=1,
                max_rounds=1,
                max_turns_per_round=1,
                seed=1,
                max_kl=0.11,
            )


if __name__ == "__main__":
    unittest.main()


def _one_wind_complete_match_config():
    return replace(
        tenhou_4p_default(),
        return_points=0,
        all_last_round_wind=TileType.parse("E"),
        max_sudden_death_round_wind=TileType.parse("E"),
        round_winds=(TileType.parse("E"),),
    )
