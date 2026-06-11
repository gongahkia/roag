from __future__ import annotations

import unittest

from kenjaku.status import (
    MAX_PYTHON_EXCLUSIVE,
    MIN_PYTHON,
    STATUS_KIND,
    SUPPORTED_PYTHON,
    build_status_payload,
    format_status_text,
)


class StatusTests(unittest.TestCase):
    def test_status_payload_marks_current_stage_and_missing_product_pieces(self) -> None:
        payload = build_status_payload()
        python_parts = tuple(int(part) for part in payload["environment"]["python"].split(".")[:2])

        self.assertEqual(payload["kind"], STATUS_KIND)
        self.assertEqual(payload["stage"], "offline research toolkit")
        self.assertFalse(payload["trained_model"]["bundled"])
        self.assertEqual(payload["environment"]["supported_python"], SUPPORTED_PYTHON)
        self.assertEqual(
            payload["environment"]["current_python_supported"],
            MIN_PYTHON <= python_parts < MAX_PYTHON_EXCLUSIVE,
        )
        self.assertTrue(payload["capabilities"]["implemented"]["tenhou_xml_parsing"])
        self.assertTrue(payload["capabilities"]["implemented"]["decision_snapshot_protocol"])
        self.assertTrue(payload["capabilities"]["implemented"]["permission_aware_replay_intake"])
        self.assertTrue(payload["capabilities"]["implemented"]["permitted_replay_share_planning"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_sandbox"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_legal_discard_environment"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_tsumo_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_pending_discard_reactions"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_individual_reaction_passes"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ron_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ron_priority_reactions"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_multi_ron_resolution"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_discard_furiten_ron_filter"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_temporary_furiten_ron_filter"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_riichi_furiten_ron_filter"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_call_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_call_application"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_terminal_reward_payloads"])
        self.assertTrue(payload["capabilities"]["implemented"]["basic_winning_hand_detection"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["self_play_sandbox_tsumo_termination"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sanma_static_ruleset"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_sandbox_sanma_tile_set"])
        self.assertTrue(payload["capabilities"]["implemented"]["heuristic_defense_risk_scoring"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["deal_in_estimator_training_command"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["transformer_state_encoder_module"])
        self.assertTrue(
            payload["capabilities"]["implemented"][
                "transformer_behavior_cloning_training_command"
            ]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["transformer_anchor_benchmark_command"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["bundled_trained_model"])
        self.assertFalse(
            payload["capabilities"]["not_implemented"]["trained_deal_in_probability_estimator"]
        )
        self.assertFalse(payload["capabilities"]["not_implemented"]["transformer_policy"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["automatic_replay_posting"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["full_rules_self_play_harness"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["sanma_ruleset"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["rl_self_play"])

    def test_status_text_is_human_readable(self) -> None:
        text = format_status_text(build_status_payload())

        self.assertIn("stage: offline research toolkit", text)
        self.assertIn("trained_model: not bundled", text)
        self.assertIn("current_python_supported:", text)
        self.assertIn("tenhou_xml_parsing: yes", text)
        self.assertIn("permission_aware_replay_intake: yes", text)
        self.assertIn("permitted_replay_share_planning: yes", text)
        self.assertIn("self_play_sandbox: yes", text)
        self.assertIn("sandbox_legal_discard_environment: yes", text)
        self.assertIn("sandbox_tsumo_action_generation: yes", text)
        self.assertIn("sandbox_pending_discard_reactions: yes", text)
        self.assertIn("sandbox_individual_reaction_passes: yes", text)
        self.assertIn("sandbox_ron_action_generation: yes", text)
        self.assertIn("sandbox_ron_priority_reactions: yes", text)
        self.assertIn("sandbox_multi_ron_resolution: yes", text)
        self.assertIn("sandbox_discard_furiten_ron_filter: yes", text)
        self.assertIn("sandbox_temporary_furiten_ron_filter: yes", text)
        self.assertIn("sandbox_riichi_furiten_ron_filter: yes", text)
        self.assertIn("sandbox_call_action_generation: yes", text)
        self.assertIn("sandbox_call_application: yes", text)
        self.assertIn("sandbox_terminal_reward_payloads: yes", text)
        self.assertIn("basic_winning_hand_detection: yes", text)
        self.assertIn("self_play_sandbox_tsumo_termination: yes", text)
        self.assertIn("sanma_static_ruleset: yes", text)
        self.assertIn("self_play_sandbox_sanma_tile_set: yes", text)
        self.assertIn("heuristic_defense_risk_scoring: yes", text)
        self.assertIn("deal_in_estimator_training_command: yes", text)
        self.assertIn("transformer_state_encoder_module: yes", text)
        self.assertIn("transformer_behavior_cloning_training_command: yes", text)
        self.assertIn("transformer_anchor_benchmark_command: yes", text)
        self.assertIn("bundled_trained_model: no", text)
        self.assertIn("trained_deal_in_probability_estimator: no", text)
        self.assertIn("transformer_policy: no", text)
        self.assertIn("automatic_replay_posting: no", text)
        self.assertIn("full_rules_self_play_harness: no", text)
        self.assertIn("sanma_ruleset: no", text)


if __name__ == "__main__":
    unittest.main()
