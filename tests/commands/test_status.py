from __future__ import annotations

from tests.commands.conftest import (
    CliCommandTests,
    Path,
    contextlib,
    importlib,
    io,
    json,
    main,
    mock,
    subprocess,
    sys,
)


class StatusCommandTests(CliCommandTests):
    def test_status_reports_current_project_stage(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["status"])

        output = stdout.getvalue()
        self.assertEqual(exit_code, 0)
        self.assertIn("stage: offline research toolkit", output)
        self.assertIn("trained_model: not bundled", output)
        self.assertIn("product_status: research toolkit, not a trained production agent", output)
        self.assertIn("tenhou_xml_parsing: yes", output)
        self.assertIn("permission_aware_replay_intake: yes", output)
        self.assertIn("permitted_replay_share_planning: yes", output)
        self.assertIn("public_safe_replay_summary: yes", output)
        self.assertIn("public_benchmark_dashboard: yes", output)
        self.assertIn("browser_playable_demo: yes", output)
        self.assertIn("self_play_sandbox: yes", output)
        self.assertIn("self_play_match_sandbox: yes", output)
        self.assertIn("self_play_match_trajectory_artifacts: yes", output)
        self.assertIn("self_play_match_final_placement: yes", output)
        self.assertIn("ppo_sandbox_training_command: yes", output)
        self.assertIn("ppo_sandbox_policy_value_losses: yes", output)
        self.assertIn("ppo_sandbox_gae_clipping_entropy: yes", output)
        self.assertIn("ppo_sandbox_checkpoint_resume: yes", output)
        self.assertIn("ppo_sandbox_training_curves: yes", output)
        self.assertIn("ppo_sandbox_evaluation_summaries: yes", output)
        self.assertIn("population_sandbox_training_command: yes", output)
        self.assertIn("population_sandbox_policy_pool: yes", output)
        self.assertIn("population_sandbox_opponent_sampling: yes", output)
        self.assertIn("population_sandbox_promotion_criteria: yes", output)
        self.assertIn("population_sandbox_matchup_metrics: yes", output)
        self.assertIn("pytorch_extra: kenjaku[ml]", output)
        self.assertIn("pytorch_commands: train-discard-mlp", output)
        self.assertIn("sandbox_legal_discard_environment: yes", output)
        self.assertIn("sandbox_tsumo_action_generation: yes", output)
        self.assertIn("sandbox_pending_discard_reactions: yes", output)
        self.assertIn("sandbox_individual_reaction_passes: yes", output)
        self.assertIn("sandbox_ron_action_generation: yes", output)
        self.assertIn("sandbox_ron_priority_reactions: yes", output)
        self.assertIn("sandbox_multi_ron_resolution: yes", output)
        self.assertIn("sandbox_discard_furiten_ron_filter: yes", output)
        self.assertIn("sandbox_temporary_furiten_ron_filter: yes", output)
        self.assertIn("sandbox_riichi_furiten_ron_filter: yes", output)
        self.assertIn("sandbox_riichi_declaration_action: yes", output)
        self.assertIn("sandbox_post_riichi_action_restrictions: yes", output)
        self.assertIn("sandbox_post_riichi_closed_kan_exceptions: yes", output)
        self.assertIn("sandbox_riichi_deposit_accounting: yes", output)
        self.assertIn("sandbox_honba_bonus_accounting: yes", output)
        self.assertIn("sandbox_next_round_transition: yes", output)
        self.assertIn("sandbox_round_wind_progression: yes", output)
        self.assertIn("sandbox_game_end_final_results: yes", output)
        self.assertIn("sandbox_all_last_sudden_death_progression: yes", output)
        self.assertIn("sandbox_bankruptcy_game_end: yes", output)
        self.assertIn("sandbox_oka_uma_final_scores: yes", output)
        self.assertIn("sandbox_ippatsu_window_tracking: yes", output)
        self.assertIn("sandbox_ankan_action_generation: yes", output)
        self.assertIn("sandbox_ankan_application: yes", output)
        self.assertIn("sandbox_kakan_action_generation: yes", output)
        self.assertIn("sandbox_kakan_application: yes", output)
        self.assertIn("sandbox_chankan_reaction_window: yes", output)
        self.assertIn("sandbox_chankan_ron_resolution: yes", output)
        self.assertIn("sandbox_ankan_kokushi_chankan: yes", output)
        self.assertIn("sandbox_dead_wall_replacement_draws: yes", output)
        self.assertIn("sandbox_kan_dora_indicator_metadata: yes", output)
        self.assertIn("sandbox_rinshan_draw_metadata: yes", output)
        self.assertIn("sandbox_endgame_yaku_timing_fixtures: yes", output)
        self.assertIn("sandbox_double_riichi_yaku_metadata: yes", output)
        self.assertIn("sandbox_call_action_generation: yes", output)
        self.assertIn("sandbox_call_application: yes", output)
        self.assertIn("sandbox_basic_yaku_win_filter: yes", output)
        self.assertIn("sandbox_basic_yaku_metadata: yes", output)
        self.assertIn("sandbox_expanded_yaku_legality: yes", output)
        self.assertIn("sandbox_unsupported_yaku_list: yes", output)
        self.assertIn("sandbox_yakuhai_seat_round_dragon_filter: yes", output)
        self.assertIn("sandbox_toitoi_yaku_metadata: yes", output)
        self.assertIn("sandbox_honroutou_yaku_metadata: yes", output)
        self.assertIn("sandbox_terminal_reward_payloads: yes", output)
        self.assertIn("self_play_reward_modes: yes", output)
        self.assertIn("self_play_reward_mode_comparison: yes", output)
        self.assertIn("sandbox_terminal_point_delta_metadata: yes", output)
        self.assertIn("sandbox_exhaustive_draw_tenpai_noten_payments: yes", output)
        self.assertIn("sandbox_nagashi_mangan_wall_exhaustion: yes", output)
        self.assertIn("sandbox_nagashi_mangan_next_round_progression: yes", output)
        self.assertIn("sandbox_abortive_draws: yes", output)
        self.assertIn("sandbox_kyuushu_kyuuhai_abortive_draw: yes", output)
        self.assertIn("sandbox_four_winds_abortive_draw: yes", output)
        self.assertIn("sandbox_four_riichi_abortive_draw: yes", output)
        self.assertIn("sandbox_four_kans_abortive_draw: yes", output)
        self.assertIn("sandbox_triple_ron_abortive_draw: yes", output)
        self.assertIn("sandbox_dealer_aware_win_payments: yes", output)
        self.assertIn("exact_fu_han_scoring: yes", output)
        self.assertIn("full_scoring_engine: yes", output)
        self.assertIn("sandbox_visible_dora_score_estimates: yes", output)
        self.assertIn("sandbox_ura_dora_score_estimates: yes", output)
        self.assertIn("sandbox_red_dora_score_estimates: yes", output)
        self.assertIn("sandbox_kazoe_yakuman_score_estimates: yes", output)
        self.assertIn("sandbox_yakuman_bonus_han_suppression: yes", output)
        self.assertIn("basic_winning_hand_detection: yes", output)
        self.assertIn("sandbox_open_meld_win_detection: yes", output)
        self.assertIn("self_play_sandbox_tsumo_termination: yes", output)
        self.assertIn("sanma_static_ruleset: yes", output)
        self.assertIn("self_play_sandbox_sanma_tile_set: yes", output)
        self.assertIn("sandbox_sanma_initial_points: yes", output)
        self.assertIn("sandbox_sanma_no_chi: yes", output)
        self.assertIn("sandbox_sanma_north_guest_wind_yaku_filter: yes", output)
        self.assertIn("sandbox_sanma_kita_action: yes", output)
        self.assertIn("sandbox_sanma_kita_ron_reaction_window: yes", output)
        self.assertIn("sandbox_sanma_kita_ron_resolution: yes", output)
        self.assertIn("opponent_shape_baseline: yes", output)
        self.assertIn("deal_in_estimator_training_command: yes", output)
        self.assertIn("deal_in_estimator_threshold_calibration: yes", output)
        self.assertIn("transformer_state_encoder_module: yes", output)
        self.assertIn("transformer_behavior_cloning_training_command: yes", output)
        self.assertIn("transformer_anchor_benchmark_command: yes", output)
        self.assertIn("bundled_trained_deal_in_probability_estimator: no", output)
        self.assertIn("transformer_policy: no", output)
        self.assertIn("automatic_replay_posting: no", output)
        self.assertIn("full_rules_self_play_harness: no", output)
        self.assertIn("sanma_ruleset: no", output)
        self.assertIn("rl_self_play: no", output)
        self.assertIn("live_ladder_automation: no", output)

    def test_torch_modules_import_without_torch_installed(self) -> None:
        code = """
import importlib.abc
import sys
sys.path.insert(0, "src")

class BlockTorch(importlib.abc.MetaPathFinder):
    def find_spec(self, fullname, path=None, target=None):
        if fullname == "torch" or fullname.startswith("torch."):
            raise ImportError("blocked torch")
        return None

sys.meta_path.insert(0, BlockTorch())
import kenjaku.models.torch_discard
import kenjaku.models.torch_transformer
print("ok")
"""
        result = subprocess.run(
            [sys.executable, "-c", code],
            check=False,
            cwd=Path.cwd(),
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "ok")

    def test_train_discard_mlp_without_torch_reports_ml_extra(self) -> None:
        original_import_module = importlib.import_module

        def fake_import_module(name: str, package: str | None = None):
            if name == "torch" or name.startswith("torch."):
                raise ImportError("blocked torch")
            return original_import_module(name, package)

        with (
            mock.patch("importlib.import_module", side_effect=fake_import_module),
            self.assertRaises(SystemExit) as context,
        ):
            main(["train-discard-mlp", "data/fixtures/tenhou", "--epochs", "0"])

        self.assertEqual(
            str(context.exception),
            "PyTorch is required for train-discard-mlp; install with `pip install kenjaku[ml]`",
        )

    def test_status_json_reports_current_project_stage(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["status", "--json"])
        payload = json.loads(stdout.getvalue())

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-status-v0")
        self.assertEqual(payload["stage"], "offline research toolkit")
        self.assertFalse(payload["trained_model"]["bundled"])
        self.assertEqual(payload["agent_status"]["rulesets"][0]["name"], "tenhou-4p")
        self.assertTrue(payload["capabilities"]["implemented"]["tenhou_xml_parsing"])
        self.assertTrue(payload["capabilities"]["implemented"]["permission_aware_replay_intake"])
        self.assertTrue(payload["capabilities"]["implemented"]["permitted_replay_share_planning"])
        self.assertTrue(payload["capabilities"]["implemented"]["public_safe_replay_summary"])
        self.assertTrue(payload["capabilities"]["implemented"]["public_benchmark_dashboard"])
        self.assertTrue(payload["capabilities"]["implemented"]["browser_playable_demo"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_sandbox"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_match_sandbox"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["self_play_match_trajectory_artifacts"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_match_final_placement"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_training_command"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_policy_value_losses"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_gae_clipping_entropy"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_checkpoint_resume"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_training_curves"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_evaluation_summaries"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["population_sandbox_training_command"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["population_sandbox_policy_pool"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["population_sandbox_opponent_sampling"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["population_sandbox_promotion_criteria"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["population_sandbox_matchup_metrics"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_legal_discard_environment"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_tsumo_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_pending_discard_reactions"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_individual_reaction_passes"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ron_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ron_priority_reactions"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_multi_ron_resolution"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_discard_furiten_ron_filter"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_temporary_furiten_ron_filter"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_riichi_furiten_ron_filter"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_riichi_declaration_action"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_post_riichi_action_restrictions"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_post_riichi_closed_kan_exceptions"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_riichi_deposit_accounting"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_honba_bonus_accounting"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_next_round_transition"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_round_wind_progression"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_game_end_final_results"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_all_last_sudden_death_progression"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_bankruptcy_game_end"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_oka_uma_final_scores"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ippatsu_window_tracking"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ankan_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ankan_application"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_kakan_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_kakan_application"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_chankan_reaction_window"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_chankan_ron_resolution"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ankan_kokushi_chankan"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_dead_wall_replacement_draws"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_kan_dora_indicator_metadata"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_rinshan_draw_metadata"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_endgame_yaku_timing_fixtures"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_double_riichi_yaku_metadata"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_call_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_call_application"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_basic_yaku_win_filter"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_basic_yaku_metadata"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_expanded_yaku_legality"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_unsupported_yaku_list"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_yakuhai_seat_round_dragon_filter"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_toitoi_yaku_metadata"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_honroutou_yaku_metadata"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_terminal_reward_payloads"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_reward_modes"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_reward_mode_comparison"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_terminal_point_delta_metadata"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_exhaustive_draw_tenpai_noten_payments"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_nagashi_mangan_wall_exhaustion"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_nagashi_mangan_next_round_progression"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_abortive_draws"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_kyuushu_kyuuhai_abortive_draw"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_four_winds_abortive_draw"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_four_riichi_abortive_draw"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_four_kans_abortive_draw"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_triple_ron_abortive_draw"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_dealer_aware_win_payments"])
        self.assertTrue(payload["capabilities"]["implemented"]["exact_fu_han_scoring"])
        self.assertTrue(payload["capabilities"]["implemented"]["full_scoring_engine"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_visible_dora_score_estimates"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ura_dora_score_estimates"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_red_dora_score_estimates"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_kazoe_yakuman_score_estimates"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_yakuman_bonus_han_suppression"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["basic_winning_hand_detection"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_open_meld_win_detection"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["self_play_sandbox_tsumo_termination"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sanma_static_ruleset"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_sandbox_sanma_tile_set"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_sanma_initial_points"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_sanma_no_chi"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_sanma_north_guest_wind_yaku_filter"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_sanma_kita_action"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_sanma_kita_ron_reaction_window"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_sanma_kita_ron_resolution"])
        self.assertTrue(payload["capabilities"]["implemented"]["opponent_shape_baseline"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["deal_in_estimator_training_command"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["deal_in_estimator_threshold_calibration"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["transformer_state_encoder_module"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["transformer_behavior_cloning_training_command"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["transformer_anchor_benchmark_command"]
        )
        self.assertFalse(payload["capabilities"]["not_implemented"]["transformer_policy"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["automatic_replay_posting"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["full_rules_self_play_harness"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["sanma_ruleset"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["rl_self_play"])
        self.assertEqual(payload["environment"]["supported_python"], ">=3.11,<3.14")
