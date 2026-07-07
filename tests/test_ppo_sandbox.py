from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.training.ppo import (
    PPO_ACTION_DIM,
    PPO_SANDBOX_CHECKPOINT_KIND,
    PPO_SANDBOX_REPORT_KIND,
    PPO_STATE_DIM,
    SandboxLinearPpoActorCritic,
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
            ppo_action_index(
                rollout.report["game_summaries"][0]["trajectory"][0]["chosen_action"]
            ),
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


if __name__ == "__main__":
    unittest.main()
