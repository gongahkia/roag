from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from torch import nn

from kenjaku.training.ppo import (
    PPO_ACTION_DIM,
    PPO_SANDBOX_CHECKPOINT_KIND,
    PPO_SANDBOX_REPORT_KIND,
    PPO_STATE_DIM,
    SandboxLinearPpoActorCritic,
    _compute_gae,
    _legacy_old_policy_predictions,
    _LegacySandboxLinearPpoActorCritic,
    _normalize,
    _old_policy_predictions,
    _run_ppo_update,
    _run_ppo_update_legacy,
    collect_ppo_sandbox_rollout,
    evaluate_ppo_sandbox_policy,
    format_ppo_sandbox_report,
    load_ppo_sandbox_checkpoint,
    ppo_action_index,
    save_ppo_sandbox_checkpoint,
    train_ppo_sandbox,
)


class PpoSandboxTests(unittest.TestCase):
    def test_rollout_tensor_contract_encodes_state_masks_and_actions(self) -> None:
        rollout = collect_ppo_sandbox_rollout(
            games=1,
            max_rounds=1,
            max_turns_per_round=8,
            seed="ppo-rollout",
            ron_policy="pass",
        )
        transition = rollout.transitions[0]

        self.assertEqual(len(transition.state), PPO_STATE_DIM)
        self.assertEqual(len(transition.legal_mask), PPO_ACTION_DIM)
        self.assertTrue(transition.legal_mask[transition.action_index])
        self.assertEqual(
            transition.action_index,
            ppo_action_index(rollout.report["game_summaries"][0]["trajectory"][0]["chosen_action"]),
        )
        self.assertGreater(len(rollout.transitions), 0)

    def test_train_evaluate_checkpoint_and_resume(self) -> None:
        result = train_ppo_sandbox(
            total_steps=16,
            rollout_games=1,
            max_rounds=1,
            max_turns_per_round=16,
            seed="ppo-train",
            ppo_epochs=1,
            batch_size=8,
            learning_rate=0.001,
            hidden_dim=16,
            supervised_warmup_epochs=1,
            device="cpu",
            torch_seed=123,
        )
        report = result.report
        text = format_ppo_sandbox_report(report)

        self.assertEqual(report["kind"], PPO_SANDBOX_REPORT_KIND)
        self.assertEqual(report["model"]["kind"], "sandbox-linear-ppo-actor-critic-v0")
        self.assertEqual(report["model"]["hidden_dim"], 16)
        self.assertGreaterEqual(report["training"]["environment_steps"], 16)
        self.assertEqual(report["training"]["updates"], 1)
        self.assertEqual(len(report["training"]["history"]), 1)
        self.assertEqual(report["training_history"]["step_unit"], "update")
        self.assertEqual(report["training_history"]["records"][0]["step"], 1)
        self.assertEqual(len(report["training_history"]["curves"]["policy_loss"]), 1)
        self.assertEqual(len(report["training_curves"]["policy_loss"]), 1)
        self.assertTrue(report["capabilities"]["ppo_policy_loss"])
        self.assertTrue(report["capabilities"]["ppo_value_loss"])
        self.assertTrue(report["capabilities"]["gae_advantages"])
        self.assertTrue(report["capabilities"]["clipped_objective"])
        self.assertTrue(report["capabilities"]["entropy_regularization"])
        self.assertTrue(report["capabilities"]["checkpointing"])
        self.assertTrue(report["capabilities"]["resume_support"])
        self.assertTrue(report["capabilities"]["supervised_warmup_initialization"])
        self.assertFalse(report["capabilities"]["learned_policy_environment_integration"])
        self.assertIn("environment_steps:", text)
        self.assertIn("ppo_policy_loss: yes", text)

        rollout = collect_ppo_sandbox_rollout(
            games=1,
            max_rounds=1,
            max_turns_per_round=4,
            seed="ppo-eval",
            ron_policy="pass",
        )
        metrics = evaluate_ppo_sandbox_policy(
            result.model,
            rollout,
            gamma=0.99,
            gae_lambda=0.95,
        )
        self.assertEqual(metrics["examples"], 4)

        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "ppo.json"
            save_ppo_sandbox_checkpoint(result, checkpoint)
            payload = load_ppo_sandbox_checkpoint(checkpoint)
            resumed = train_ppo_sandbox(
                total_steps=8,
                rollout_games=1,
                max_rounds=1,
                max_turns_per_round=8,
                seed="ppo-train",
                ppo_epochs=1,
                batch_size=4,
                learning_rate=0.001,
                hidden_dim=999,
                device="cpu",
                torch_seed=123,
                resume_checkpoint=checkpoint,
            )

        self.assertEqual(payload["kind"], PPO_SANDBOX_CHECKPOINT_KIND)
        self.assertEqual(set(payload["model_state_dict"]), set(result.model_state))
        self.assertGreater(
            resumed.report["training"]["environment_steps"],
            result.report["training"]["environment_steps"],
        )
        self.assertEqual(resumed.report["model"]["hidden_dim"], 16)
        self.assertIsNotNone(resumed.report["training"]["resume_checkpoint"])

    def test_actor_critic_masks_illegal_actions(self) -> None:
        model = SandboxLinearPpoActorCritic(hidden_dim=8, seed=123)
        state = [0.0 for _feature in range(PPO_STATE_DIM)]
        legal_mask = [False for _action in range(PPO_ACTION_DIM)]
        legal_mask[7] = True

        logits = model.logits(state, legal_mask)
        value = model.value(state)

        self.assertEqual(len(logits), PPO_ACTION_DIM)
        self.assertIsInstance(value, float)
        self.assertLess(logits[0], -1.0e8)
        self.assertGreater(logits[7], -1.0e8)

    def test_actor_critic_is_torch_module_with_sequential_heads(self) -> None:
        model = SandboxLinearPpoActorCritic(hidden_dim=8, seed=123, device="cpu")

        self.assertIsInstance(model, nn.Module)
        self.assertIsInstance(model.policy, nn.Sequential)
        self.assertIsInstance(model.value_net, nn.Sequential)
        self.assertIsInstance(model.policy[0], nn.Linear)
        self.assertIsInstance(model.policy[1], nn.Tanh)
        self.assertIsInstance(model.policy[2], nn.Linear)
        self.assertEqual(str(next(model.parameters()).device), "cpu")

    def test_torch_update_matches_legacy_one_step_golden_numbers(self) -> None:
        arrays = _golden_update_arrays()
        legacy = _LegacySandboxLinearPpoActorCritic(seed=19)
        model = SandboxLinearPpoActorCritic(seed=19, device="cpu")
        old_log_probs, old_values = _legacy_old_policy_predictions(legacy, arrays)
        torch_log_probs, torch_values = _old_policy_predictions(model, arrays)
        advantages, returns = _compute_gae(
            rewards=arrays["rewards"],
            dones=arrays["dones"],
            values=old_values,
            sequence_keys=arrays["sequence_keys"],
            gamma=0.93,
            gae_lambda=0.87,
        )
        advantages = _normalize(advantages)
        kwargs = {
            "arrays": arrays,
            "old_log_probs": old_log_probs,
            "advantages": advantages,
            "returns": returns,
            "ppo_epochs": 1,
            "batch_size": 2,
            "learning_rate": 0.003,
            "clip_epsilon": 0.2,
            "entropy_coef": 0.01,
            "value_coef": 0.5,
            "max_grad_norm": 0.5,
            "seed": 23,
        }

        legacy_metrics = _run_ppo_update_legacy(legacy, **kwargs)
        torch_metrics = _run_ppo_update(model, **kwargs)

        self.assertLess(
            max(
                abs(left - right)
                for left, right in zip(old_log_probs, torch_log_probs, strict=True)
            ),
            1.0e-12,
        )
        self.assertLess(
            max(abs(left - right) for left, right in zip(old_values, torch_values, strict=True)),
            1.0e-12,
        )
        self.assertAlmostEqual(torch_metrics["loss"], 0.025170880058630773)
        self.assertAlmostEqual(torch_metrics["value_loss"], 0.07806715777361181)
        self.assertAlmostEqual(torch_metrics["entropy"], 1.386269882817479)
        for key in ("loss", "policy_loss", "value_loss", "entropy", "approx_kl"):
            self.assertAlmostEqual(torch_metrics[key], legacy_metrics[key], places=8)
        self.assertAlmostEqual(
            model.logits(arrays["states"][0], arrays["legal_masks"][0])[3],
            legacy.logits(arrays["states"][0], arrays["legal_masks"][0])[3],
            places=7,
        )
        self.assertAlmostEqual(model.value(arrays["states"][0]), legacy.value(arrays["states"][0]))


def _golden_update_arrays() -> dict[str, object]:
    states = [
        [((index % 7) - 3) / 10.0 for index in range(PPO_STATE_DIM)],
        [((index % 5) - 2) / 8.0 for index in range(PPO_STATE_DIM)],
        [((index % 3) - 1) / 6.0 for index in range(PPO_STATE_DIM)],
        [((index % 11) - 5) / 12.0 for index in range(PPO_STATE_DIM)],
    ]
    actions = [3, 17, 41, 85]
    legal_masks = []
    for action in actions:
        mask = [False] * PPO_ACTION_DIM
        for offset in (0, 2, 5, 13):
            mask[(action + offset) % PPO_ACTION_DIM] = True
        mask[action] = True
        legal_masks.append(mask)
    return {
        "states": states,
        "legal_masks": legal_masks,
        "actions": actions,
        "rewards": [0.0, 0.25, -0.1, 0.4],
        "dones": [False, True, False, True],
        "sequence_keys": [(0, 0), (0, 0), (0, 1), (0, 1)],
    }


if __name__ == "__main__":
    unittest.main()
