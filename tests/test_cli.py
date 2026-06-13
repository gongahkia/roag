from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import shutil
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.cli import (
    _call_example_from_payload,
    _call_example_to_payload,
    _call_examples_signature,
    _disagreement_record,
    _limit_call_examples,
    main,
)
from kenjaku.core import Action, ActionKind, Tile, TileType, tile_counts
from kenjaku.experiments import build_discard_mlp_benchmark_report
from kenjaku.io import parse_tenhou_xml_file
from kenjaku.training import CallExample, DiscardExample


class CliTests(unittest.TestCase):
    def test_version(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["--version"])

        self.assertEqual(exit_code, 0)
        self.assertIn("kenjaku", stdout.getvalue())

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

    def test_status_json_reports_current_project_stage(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["status", "--json"])
        payload = json.loads(stdout.getvalue())

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-status-v0")
        self.assertEqual(payload["stage"], "offline research toolkit")
        self.assertFalse(payload["trained_model"]["bundled"])
        self.assertTrue(payload["capabilities"]["implemented"]["tenhou_xml_parsing"])
        self.assertTrue(payload["capabilities"]["implemented"]["permission_aware_replay_intake"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["permitted_replay_share_planning"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["public_safe_replay_summary"])
        self.assertTrue(payload["capabilities"]["implemented"]["public_benchmark_dashboard"])
        self.assertTrue(payload["capabilities"]["implemented"]["browser_playable_demo"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_sandbox"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_match_sandbox"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["self_play_match_trajectory_artifacts"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["self_play_match_final_placement"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_training_command"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_policy_value_losses"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_gae_clipping_entropy"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_checkpoint_resume"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_training_curves"])
        self.assertTrue(payload["capabilities"]["implemented"]["ppo_sandbox_evaluation_summaries"])
        self.assertTrue(payload["capabilities"]["implemented"]["population_sandbox_training_command"])
        self.assertTrue(payload["capabilities"]["implemented"]["population_sandbox_policy_pool"])
        self.assertTrue(payload["capabilities"]["implemented"]["population_sandbox_opponent_sampling"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["population_sandbox_promotion_criteria"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["population_sandbox_matchup_metrics"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_legal_discard_environment"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_tsumo_action_generation"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_pending_discard_reactions"]
        )
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
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_riichi_furiten_ron_filter"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_riichi_declaration_action"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_post_riichi_action_restrictions"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"][
                "sandbox_post_riichi_closed_kan_exceptions"
            ]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_riichi_deposit_accounting"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_honba_bonus_accounting"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_next_round_transition"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_round_wind_progression"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_game_end_final_results"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"][
                "sandbox_all_last_sudden_death_progression"
            ]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_bankruptcy_game_end"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_oka_uma_final_scores"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_ippatsu_window_tracking"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ankan_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_ankan_application"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_kakan_action_generation"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_kakan_application"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_chankan_reaction_window"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_chankan_ron_resolution"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_ankan_kokushi_chankan"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_dead_wall_replacement_draws"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_kan_dora_indicator_metadata"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_rinshan_draw_metadata"]
        )
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
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_yakuhai_seat_round_dragon_filter"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_toitoi_yaku_metadata"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_honroutou_yaku_metadata"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_terminal_reward_payloads"])
        self.assertTrue(payload["capabilities"]["implemented"]["self_play_reward_modes"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["self_play_reward_mode_comparison"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_terminal_point_delta_metadata"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"][
                "sandbox_exhaustive_draw_tenpai_noten_payments"
            ]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"][
                "sandbox_nagashi_mangan_wall_exhaustion"
            ]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"][
                "sandbox_nagashi_mangan_next_round_progression"
            ]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_abortive_draws"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_kyuushu_kyuuhai_abortive_draw"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_four_winds_abortive_draw"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_four_riichi_abortive_draw"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_four_kans_abortive_draw"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_triple_ron_abortive_draw"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_dealer_aware_win_payments"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_visible_dora_score_estimates"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_ura_dora_score_estimates"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_red_dora_score_estimates"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_kazoe_yakuman_score_estimates"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_yakuman_bonus_han_suppression"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["basic_winning_hand_detection"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_open_meld_win_detection"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["self_play_sandbox_tsumo_termination"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sanma_static_ruleset"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["self_play_sandbox_sanma_tile_set"]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_sanma_initial_points"])
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_sanma_no_chi"])
        self.assertTrue(
            payload["capabilities"]["implemented"][
                "sandbox_sanma_north_guest_wind_yaku_filter"
            ]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["sandbox_sanma_kita_action"])
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_sanma_kita_ron_reaction_window"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["sandbox_sanma_kita_ron_resolution"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["deal_in_estimator_training_command"]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"][
                "deal_in_estimator_threshold_calibration"
            ]
        )
        self.assertTrue(payload["capabilities"]["implemented"]["transformer_state_encoder_module"])
        self.assertTrue(
            payload["capabilities"]["implemented"][
                "transformer_behavior_cloning_training_command"
            ]
        )
        self.assertTrue(
            payload["capabilities"]["implemented"]["transformer_anchor_benchmark_command"]
        )
        self.assertFalse(payload["capabilities"]["not_implemented"]["transformer_policy"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["automatic_replay_posting"])
        self.assertFalse(
            payload["capabilities"]["not_implemented"]["full_rules_self_play_harness"]
        )
        self.assertFalse(payload["capabilities"]["not_implemented"]["sanma_ruleset"])
        self.assertFalse(payload["capabilities"]["not_implemented"]["rl_self_play"])
        self.assertEqual(payload["environment"]["supported_python"], ">=3.11,<3.14")

    def test_browser_demo_writes_static_assets_without_serving(self) -> None:
        with TemporaryDirectory() as directory:
            output_dir = Path(directory) / "demo"
            stdout = io.StringIO()

            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "browser-demo",
                        "--output-dir",
                        str(output_dir),
                        "--no-serve",
                    ]
                )

            index_html = (output_dir / "index.html").read_text(encoding="utf-8")
            styles_css = (output_dir / "styles.css").read_text(encoding="utf-8")
            demo_js = (output_dir / "demo.js").read_text(encoding="utf-8")

        self.assertEqual(exit_code, 0)
        self.assertIn("wrote browser demo:", stdout.getvalue())
        self.assertIn("open:", stdout.getvalue())
        self.assertIn("Kenjaku Browser Demo", index_html)
        self.assertIn("Legal Actions", index_html)
        self.assertIn("Dora", index_html)
        self.assertIn("Calls", index_html)
        self.assertIn("finishExhaustiveDraw", demo_js)
        self.assertIn("botDiscard", demo_js)
        self.assertIn("window.KenjakuDemo", demo_js)
        self.assertIn(".tile", styles_css)
        self.assertNotIn("private", index_html + styles_css + demo_js)
        self.assertNotIn("replay", index_html.lower() + styles_css.lower() + demo_js.lower())

    def test_replay_intake_review_outputs_text_json_report_and_accepted_queue(self) -> None:
        manifest_payload = {
            "kind": "kenjaku-replay-manifest-v0",
            "items": [
                {
                    "id": "accepted-synthetic",
                    "platform": "synthetic",
                    "uri": "data/fixtures/replay/synthetic.json",
                    "intended_uses": ["analysis", "training"],
                    "permission": {"status": "local_synthetic"},
                },
                {
                    "id": "blocked-training",
                    "platform": "mahjong_soul",
                    "uri": "https://mahjongsoul.game.yo-star.com/?paipu=blocked",
                    "intended_uses": ["training"],
                    "permission": {"status": "user_provided"},
                },
            ],
        }
        with TemporaryDirectory() as directory:
            manifest = Path(directory) / "manifest.json"
            report = Path(directory) / "review.json"
            accepted = Path(directory) / "accepted.jsonl"
            manifest.write_text(json.dumps(manifest_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "replay-intake-review",
                        str(manifest),
                        "--report",
                        str(report),
                        "--accepted-output",
                        str(accepted),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))
            accepted_rows = [
                json.loads(line)
                for line in accepted.read_text(encoding="utf-8").splitlines()
            ]

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["replay-intake-review", str(manifest), "--json"])
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("items: 2", text_stdout.getvalue())
        self.assertIn("accepted: 1", text_stdout.getvalue())
        self.assertIn("rejected: 1", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertIn("accepted_output_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-replay-intake-review-v0")
        self.assertEqual(report_payload["accepted"], 1)
        self.assertEqual(report_payload["rejected"], 1)
        self.assertEqual(len(accepted_rows), 1)
        self.assertEqual(accepted_rows[0]["kind"], "kenjaku-replay-intake-item-v0")
        self.assertEqual(accepted_rows[0]["id"], "accepted-synthetic")
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["accepted"], 1)

    def test_replay_share_plan_outputs_text_json_and_report(self) -> None:
        accepted_rows = [
            {
                "kind": "kenjaku-replay-intake-item-v0",
                "id": "demo-ready",
                "platform": "other",
                "uri": "https://example.test/replay",
                "intended_uses": ["analysis", "demo"],
                "permission": {
                    "status": "explicit_permission",
                    "scope": ["analysis", "demo"],
                    "granted_by": "unit-test",
                    "granted_at": "2026-06-11",
                    "notes": None,
                },
            },
            {
                "kind": "kenjaku-replay-intake-item-v0",
                "id": "analysis-only",
                "platform": "mahjong_soul",
                "uri": "https://mahjongsoul.game.yo-star.com/?paipu=analysis",
                "intended_uses": ["analysis"],
                "permission": {
                    "status": "user_provided",
                    "scope": ["analysis", "evaluation"],
                    "granted_by": None,
                    "granted_at": None,
                    "notes": None,
                },
            },
        ]
        with TemporaryDirectory() as directory:
            accepted = Path(directory) / "accepted.jsonl"
            report = Path(directory) / "share-plan.json"
            accepted.write_text(
                "".join(json.dumps(row, sort_keys=True) + "\n" for row in accepted_rows),
                encoding="utf-8",
            )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "replay-share-plan",
                        str(accepted),
                        "--intent",
                        "demo",
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    ["replay-share-plan", str(accepted), "--intent", "demo", "--json"]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("intent: demo", text_stdout.getvalue())
        self.assertIn("shareable: 1", text_stdout.getvalue())
        self.assertIn("blocked: 1", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-replay-share-plan-v0")
        self.assertEqual(report_payload["shareable"], 1)
        self.assertEqual(report_payload["blocked"], 1)
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["intent"], "demo")

    def test_replay_public_summary_outputs_text_json_and_report(self) -> None:
        accepted_rows = [
            {
                "kind": "kenjaku-replay-intake-item-v0",
                "id": "demo-ready",
                "platform": "local_file",
                "uri": "/private/replays/demo-ready.xml",
                "intended_uses": ["analysis", "demo"],
                "permission": {
                    "status": "explicit_permission",
                    "scope": ["analysis", "demo"],
                    "granted_by": "unit-test",
                    "granted_at": "2026-06-12",
                    "notes": None,
                },
            },
            {
                "kind": "kenjaku-replay-intake-item-v0",
                "id": "analysis-only",
                "platform": "local_file",
                "uri": "/private/replays/analysis-only.xml",
                "intended_uses": ["analysis"],
                "permission": {
                    "status": "user_provided",
                    "scope": ["analysis", "evaluation"],
                    "granted_by": None,
                    "granted_at": None,
                    "notes": None,
                },
            },
        ]
        with TemporaryDirectory() as directory:
            accepted = Path(directory) / "accepted.jsonl"
            report = Path(directory) / "public-summary.json"
            accepted.write_text(
                "".join(json.dumps(row, sort_keys=True) + "\n" for row in accepted_rows),
                encoding="utf-8",
            )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "replay-public-summary",
                        str(accepted),
                        "--intent",
                        "demo",
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    ["replay-public-summary", str(accepted), "--intent", "demo", "--json"]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("raw_replay_uris_included: no", text_stdout.getvalue())
        self.assertIn("public_summaries:", text_stdout.getvalue())
        self.assertIn("blocked_items:", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-replay-public-summary-v0")
        self.assertEqual(report_payload["shareable"], 1)
        self.assertEqual(report_payload["blocked"], 1)
        self.assertFalse(report_payload["raw_replay_uris_included"])
        self.assertNotIn("/private/replays/demo-ready.xml", json.dumps(report_payload))
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["intent"], "demo")
        self.assertFalse(json_payload["raw_replay_data_included"])

    def test_self_play_sandbox_outputs_text_json_and_report(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "self-play.json"

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "self-play-sandbox",
                        "--episodes",
                        "2",
                        "--max-turns",
                        "8",
                        "--seed",
                        "fixed",
                        "--policy",
                        "frequency",
                        "--ruleset",
                        "tenhou-3p",
                        "--stop-on-tsumo",
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "self-play-sandbox",
                        "--episodes",
                        "1",
                        "--max-turns",
                        "4",
                        "--seed",
                        "fixed",
                        "--policy",
                        "drawn",
                        "--reward-mode",
                        "placement-delta",
                        "--stop-on-tsumo",
                        "--json",
                        "--include-trajectories",
                    ]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("episodes: 2", text_stdout.getvalue())
        self.assertIn("ruleset: tenhou-3p", text_stdout.getvalue())
        self.assertIn("policy: frequency-discard-sandbox-v0", text_stdout.getvalue())
        self.assertIn("reward_mode: terminal", text_stdout.getvalue())
        self.assertIn(
            "reward_modes: terminal, point-delta, normalized-point-delta, placement-delta",
            text_stdout.getvalue(),
        )
        self.assertIn("draw_outcomes:", text_stdout.getvalue())
        self.assertIn("reward_summary:", text_stdout.getvalue())
        self.assertIn("stop_on_tsumo: yes", text_stdout.getvalue())
        self.assertIn("full_riichi_rules: no", text_stdout.getvalue())
        self.assertIn("open_hand_win_detection: yes", text_stdout.getvalue())
        self.assertIn("discard_furiten_ron_filter: yes", text_stdout.getvalue())
        self.assertIn("temporary_furiten_ron_filter: yes", text_stdout.getvalue())
        self.assertIn("riichi_furiten_ron_filter: yes", text_stdout.getvalue())
        self.assertIn("riichi_declaration_action: yes", text_stdout.getvalue())
        self.assertIn("post_riichi_action_restrictions: yes", text_stdout.getvalue())
        self.assertIn("post_riichi_closed_kan_exceptions: yes", text_stdout.getvalue())
        self.assertIn("riichi_deposit_accounting: yes", text_stdout.getvalue())
        self.assertIn("honba_bonus_accounting: yes", text_stdout.getvalue())
        self.assertIn("next_round_transition: yes", text_stdout.getvalue())
        self.assertIn("round_wind_progression: yes", text_stdout.getvalue())
        self.assertIn("ippatsu_window_tracking: yes", text_stdout.getvalue())
        self.assertIn("closed_kan_actions: yes", text_stdout.getvalue())
        self.assertIn("added_kan_actions: yes", text_stdout.getvalue())
        self.assertIn("chankan_reaction_windows: yes", text_stdout.getvalue())
        self.assertIn("chankan_ron_resolution: yes", text_stdout.getvalue())
        self.assertIn("ankan_kokushi_chankan: yes", text_stdout.getvalue())
        self.assertIn("dead_wall_replacement_draws: yes", text_stdout.getvalue())
        self.assertIn("kan_dora_indicator_metadata: yes", text_stdout.getvalue())
        self.assertIn("rinshan_draw_metadata: yes", text_stdout.getvalue())
        self.assertIn("endgame_yaku_timing_fixtures: yes", text_stdout.getvalue())
        self.assertIn("double_riichi_yaku_metadata: yes", text_stdout.getvalue())
        self.assertIn("sanma_initial_points: yes", text_stdout.getvalue())
        self.assertIn("sanma_no_chi: yes", text_stdout.getvalue())
        self.assertIn("sanma_north_guest_wind: yes", text_stdout.getvalue())
        self.assertIn("sanma_kita_action: yes", text_stdout.getvalue())
        self.assertIn("kita_policy: no", text_stdout.getvalue())
        self.assertIn("kita_ron_reaction_windows: yes", text_stdout.getvalue())
        self.assertIn("kita_ron_resolution: yes", text_stdout.getvalue())
        self.assertIn("basic_yaku_win_filter: yes", text_stdout.getvalue())
        self.assertIn("basic_yaku_metadata: yes", text_stdout.getvalue())
        self.assertIn("yakuhai_seat_round_dragon_filter: yes", text_stdout.getvalue())
        self.assertIn("toitoi_yaku_metadata: yes", text_stdout.getvalue())
        self.assertIn("honroutou_yaku_metadata: yes", text_stdout.getvalue())
        self.assertIn("selectable_reward_modes: yes", text_stdout.getvalue())
        self.assertIn("reward_mode_comparison: yes", text_stdout.getvalue())
        self.assertIn("point_delta_reward_mode: yes", text_stdout.getvalue())
        self.assertIn("normalized_point_delta_reward_mode: yes", text_stdout.getvalue())
        self.assertIn("placement_delta_reward_mode: yes", text_stdout.getvalue())
        self.assertIn("terminal_point_delta_metadata: yes", text_stdout.getvalue())
        self.assertIn("nagashi_mangan_wall_exhaustion: yes", text_stdout.getvalue())
        self.assertIn("nagashi_mangan_next_round_progression: yes", text_stdout.getvalue())
        self.assertIn("abortive_draws: yes", text_stdout.getvalue())
        self.assertIn("kyuushu_kyuuhai_abortive_draw: yes", text_stdout.getvalue())
        self.assertIn("four_winds_abortive_draw: yes", text_stdout.getvalue())
        self.assertIn("four_riichi_abortive_draw: yes", text_stdout.getvalue())
        self.assertIn("four_kans_abortive_draw: yes", text_stdout.getvalue())
        self.assertIn("triple_ron_abortive_draw: yes", text_stdout.getvalue())
        self.assertIn("dealer_aware_win_payments: yes", text_stdout.getvalue())
        self.assertIn("visible_dora_score_estimates: yes", text_stdout.getvalue())
        self.assertIn("ura_dora_score_estimates: yes", text_stdout.getvalue())
        self.assertIn("red_dora_score_estimates: yes", text_stdout.getvalue())
        self.assertIn("kazoe_yakuman_score_estimates: yes", text_stdout.getvalue())
        self.assertIn("yakuman_bonus_han_suppression: yes", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-self-play-sandbox-report-v0")
        self.assertTrue(report_payload["stop_on_tsumo"])
        self.assertEqual(report_payload["ruleset"], "tenhou-3p")
        self.assertEqual(report_payload["players"], 3)
        self.assertEqual(report_payload["reward_mode"], "terminal")
        self.assertEqual(len(report_payload["reward_summaries"]), 4)
        self.assertIn("outcome_summary", report_payload)
        self.assertLessEqual(report_payload["decisions"], 16)
        self.assertTrue(report_payload["capabilities"]["discard_furiten_ron_filter"])
        self.assertTrue(report_payload["capabilities"]["temporary_furiten_ron_filter"])
        self.assertTrue(report_payload["capabilities"]["riichi_furiten_ron_filter"])
        self.assertTrue(report_payload["capabilities"]["riichi_declaration_action"])
        self.assertTrue(report_payload["capabilities"]["post_riichi_action_restrictions"])
        self.assertTrue(report_payload["capabilities"]["post_riichi_closed_kan_exceptions"])
        self.assertTrue(report_payload["capabilities"]["riichi_deposit_accounting"])
        self.assertTrue(report_payload["capabilities"]["honba_bonus_accounting"])
        self.assertTrue(report_payload["capabilities"]["next_round_transition"])
        self.assertTrue(report_payload["capabilities"]["round_wind_progression"])
        self.assertTrue(report_payload["capabilities"]["ippatsu_window_tracking"])
        self.assertTrue(report_payload["capabilities"]["closed_kan_actions"])
        self.assertTrue(report_payload["capabilities"]["added_kan_actions"])
        self.assertTrue(report_payload["capabilities"]["chankan_reaction_windows"])
        self.assertTrue(report_payload["capabilities"]["chankan_ron_resolution"])
        self.assertTrue(report_payload["capabilities"]["ankan_kokushi_chankan"])
        self.assertTrue(report_payload["capabilities"]["dead_wall_replacement_draws"])
        self.assertTrue(report_payload["capabilities"]["kan_dora_indicator_metadata"])
        self.assertTrue(report_payload["capabilities"]["rinshan_draw_metadata"])
        self.assertTrue(report_payload["capabilities"]["endgame_yaku_timing_fixtures"])
        self.assertTrue(report_payload["capabilities"]["double_riichi_yaku_metadata"])
        self.assertTrue(report_payload["capabilities"]["sanma_initial_points"])
        self.assertTrue(report_payload["capabilities"]["sanma_no_chi"])
        self.assertTrue(report_payload["capabilities"]["sanma_north_guest_wind"])
        self.assertTrue(report_payload["capabilities"]["sanma_kita_action"])
        self.assertFalse(report_payload["capabilities"]["kita_policy"])
        self.assertTrue(report_payload["capabilities"]["kita_ron_reaction_windows"])
        self.assertTrue(report_payload["capabilities"]["kita_ron_resolution"])
        self.assertTrue(report_payload["capabilities"]["basic_yaku_win_filter"])
        self.assertTrue(report_payload["capabilities"]["basic_yaku_metadata"])
        self.assertTrue(report_payload["capabilities"]["yakuhai_seat_round_dragon_filter"])
        self.assertTrue(report_payload["capabilities"]["toitoi_yaku_metadata"])
        self.assertTrue(report_payload["capabilities"]["honroutou_yaku_metadata"])
        self.assertTrue(report_payload["capabilities"]["selectable_reward_modes"])
        self.assertTrue(report_payload["capabilities"]["reward_mode_comparison"])
        self.assertTrue(report_payload["capabilities"]["point_delta_reward_mode"])
        self.assertTrue(report_payload["capabilities"]["normalized_point_delta_reward_mode"])
        self.assertTrue(report_payload["capabilities"]["placement_delta_reward_mode"])
        self.assertTrue(report_payload["capabilities"]["terminal_point_delta_metadata"])
        self.assertTrue(report_payload["capabilities"]["nagashi_mangan_wall_exhaustion"])
        self.assertTrue(
            report_payload["capabilities"]["nagashi_mangan_next_round_progression"]
        )
        self.assertTrue(report_payload["capabilities"]["abortive_draws"])
        self.assertTrue(report_payload["capabilities"]["kyuushu_kyuuhai_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["four_winds_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["four_riichi_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["four_kans_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["triple_ron_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["dealer_aware_win_payments"])
        self.assertTrue(report_payload["capabilities"]["visible_dora_score_estimates"])
        self.assertTrue(report_payload["capabilities"]["ura_dora_score_estimates"])
        self.assertTrue(report_payload["capabilities"]["red_dora_score_estimates"])
        self.assertTrue(report_payload["capabilities"]["kazoe_yakuman_score_estimates"])
        self.assertTrue(report_payload["capabilities"]["yakuman_bonus_han_suppression"])
        self.assertTrue(report_payload["capabilities"]["open_hand_win_detection"])
        self.assertFalse(report_payload["capabilities"]["kan_policy"])
        self.assertFalse(report_payload["capabilities"]["chankan_policy"])
        self.assertFalse(report_payload["capabilities"]["ppo"])
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["episodes"], 1)
        self.assertEqual(json_payload["reward_mode"], "placement-delta")
        self.assertTrue(json_payload["stop_on_tsumo"])
        self.assertIn("trajectory", json_payload["episode_summaries"][0])
        self.assertIn("kita_tiles", json_payload["episode_summaries"][0])
        self.assertIn("kita_counts", json_payload["episode_summaries"][0])
        self.assertIn("reward_vectors", json_payload["episode_summaries"][0])
        self.assertEqual(
            json_payload["episode_summaries"][0]["selected_rewards"],
            json_payload["episode_summaries"][0]["reward_vectors"]["placement-delta"],
        )

    def test_self_play_match_sandbox_command_writes_report_and_json(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "self-play-match.json"

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "self-play-match-sandbox",
                        "--games",
                        "1",
                        "--max-rounds",
                        "12",
                        "--max-turns-per-round",
                        "512",
                        "--seed",
                        "smoke",
                        "--ron-policy",
                        "pass",
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "self-play-match-sandbox",
                        "--games",
                        "1",
                        "--max-rounds",
                        "9",
                        "--max-turns-per-round",
                        "512",
                        "--seed",
                        "sanma-smoke",
                        "--ruleset",
                        "tenhou-3p",
                        "--ron-policy",
                        "pass",
                        "--json",
                    ]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("games: 1", text_stdout.getvalue())
        self.assertIn("completed_games: 1", text_stdout.getvalue())
        self.assertIn("ruleset: tenhou-4p", text_stdout.getvalue())
        self.assertIn("discard=drawn", text_stdout.getvalue())
        self.assertIn("ron=pass", text_stdout.getvalue())
        self.assertIn("final_reasons:", text_stdout.getvalue())
        self.assertIn("average_final_scores:", text_stdout.getvalue())
        self.assertIn("multi_round_matches: yes", text_stdout.getvalue())
        self.assertIn("final_placement: yes", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-self-play-match-report-v0")
        self.assertEqual(report_payload["completed_games"], 1)
        self.assertEqual(report_payload["ruleset"], "tenhou-4p")
        self.assertTrue(report_payload["capabilities"]["multi_round_matches"])
        self.assertTrue(report_payload["capabilities"]["game_end_final_results"])
        self.assertTrue(report_payload["capabilities"]["discard_policy"])
        self.assertTrue(report_payload["capabilities"]["call_policy"])
        self.assertTrue(report_payload["capabilities"]["riichi_policy"])
        self.assertTrue(report_payload["capabilities"]["kan_policy"])
        self.assertTrue(report_payload["capabilities"]["kita_policy"])
        self.assertTrue(report_payload["capabilities"]["ron_policy"])
        self.assertTrue(report_payload["capabilities"]["pass_policy"])
        self.assertTrue(report_payload["capabilities"]["final_placement"])
        self.assertIsNotNone(report_payload["game_summaries"][0]["final_result"])
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["kind"], "kenjaku-self-play-match-report-v0")
        self.assertEqual(json_payload["ruleset"], "tenhou-3p")
        self.assertEqual(json_payload["players"], 3)
        self.assertEqual(json_payload["completed_games"], 1)
        self.assertTrue(json_payload["capabilities"]["kita_policy"])
        self.assertEqual(
            json_payload["game_summaries"][0]["final_result"]["return_points"],
            40000,
        )

    def test_train_ppo_sandbox_command_writes_report_checkpoint_and_json(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "ppo.json"
            checkpoint = Path(directory) / "ppo-checkpoint.json"

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "train-ppo-sandbox",
                        "--total-steps",
                        "16",
                        "--rollout-games",
                        "1",
                        "--max-rounds",
                        "1",
                        "--max-turns-per-round",
                        "16",
                        "--ppo-epochs",
                        "1",
                        "--batch-size",
                        "8",
                        "--hidden-dim",
                        "16",
                        "--device",
                        "cpu",
                        "--model-seed",
                        "123",
                        "--report",
                        str(report),
                        "--checkpoint",
                        str(checkpoint),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "train-ppo-sandbox",
                        "--total-steps",
                        "8",
                        "--rollout-games",
                        "1",
                        "--max-rounds",
                        "1",
                        "--max-turns-per-round",
                        "8",
                        "--ppo-epochs",
                        "1",
                        "--batch-size",
                        "4",
                        "--hidden-dim",
                        "16",
                        "--device",
                        "cpu",
                        "--json",
                    ]
                )
            json_payload = json.loads(json_stdout.getvalue())
            checkpoint_exists = checkpoint.exists()

        self.assertEqual(text_exit_code, 0)
        self.assertTrue(checkpoint_exists)
        self.assertIn("environment_steps:", text_stdout.getvalue())
        self.assertIn("ppo_policy_loss: yes", text_stdout.getvalue())
        self.assertIn("checkpoint_path:", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-ppo-sandbox-report-v0")
        self.assertGreaterEqual(report_payload["training"]["environment_steps"], 16)
        self.assertTrue(report_payload["capabilities"]["ppo_policy_loss"])
        self.assertTrue(report_payload["capabilities"]["ppo_value_loss"])
        self.assertTrue(report_payload["capabilities"]["gae_advantages"])
        self.assertTrue(report_payload["capabilities"]["clipped_objective"])
        self.assertTrue(report_payload["capabilities"]["entropy_regularization"])
        self.assertTrue(report_payload["capabilities"]["checkpointing"])
        self.assertTrue(report_payload["capabilities"]["resume_support"])
        self.assertFalse(report_payload["capabilities"]["learned_policy_environment_integration"])
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["kind"], "kenjaku-ppo-sandbox-report-v0")
        self.assertGreaterEqual(json_payload["training"]["environment_steps"], 8)

    def test_train_population_sandbox_command_writes_report_and_artifacts(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "population.json"
            output_dir = Path(directory) / "population"

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "train-population-sandbox",
                        "--pool-size",
                        "4",
                        "--generations",
                        "1",
                        "--candidates-per-generation",
                        "1",
                        "--matchups-per-candidate",
                        "1",
                        "--total-steps",
                        "8",
                        "--max-rounds",
                        "1",
                        "--max-turns-per-round",
                        "8",
                        "--evaluation-max-rounds",
                        "1",
                        "--evaluation-max-turns-per-round",
                        "8",
                        "--output-dir",
                        str(output_dir),
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))
            artifact_count = len(list(output_dir.glob("*.json")))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "train-population-sandbox",
                        "--pool-size",
                        "4",
                        "--generations",
                        "1",
                        "--candidates-per-generation",
                        "1",
                        "--matchups-per-candidate",
                        "1",
                        "--total-steps",
                        "4",
                        "--max-rounds",
                        "1",
                        "--max-turns-per-round",
                        "4",
                        "--evaluation-max-rounds",
                        "1",
                        "--evaluation-max-turns-per-round",
                        "4",
                        "--json",
                    ]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("pool_size: 4", text_stdout.getvalue())
        self.assertIn("matchups: 5", text_stdout.getvalue())
        self.assertIn("promotion_decisions:", text_stdout.getvalue())
        self.assertIn("output_dir:", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(artifact_count, 5)
        self.assertEqual(report_payload["kind"], "kenjaku-population-sandbox-report-v0")
        self.assertEqual(len(report_payload["pool"]), 4)
        self.assertEqual(len(report_payload["snapshots"]), 5)
        self.assertEqual(report_payload["matchup_counts"]["total"], 5)
        self.assertTrue(report_payload["capabilities"]["policy_snapshot_pool"])
        self.assertTrue(report_payload["capabilities"]["opponent_sampling"])
        self.assertTrue(report_payload["capabilities"]["promotion_criteria"])
        self.assertFalse(
            report_payload["capabilities"]["learned_policy_environment_integration"]
        )
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["kind"], "kenjaku-population-sandbox-report-v0")
        self.assertEqual(json_payload["pool_size"], 4)

    def test_inspect_tenhou_fixture(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["inspect-tenhou", "data/fixtures/tenhou/minimal_4p.xml"])

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines(),
            ["rounds: 1", "discards: 2", "discard_examples: 2", "call_examples: 0"],
        )

    def test_inspect_tenhou_fixture_directory(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["inspect-tenhou", "data/fixtures/tenhou"])

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines(),
            ["rounds: 3", "discards: 4", "discard_examples: 4", "call_examples: 1"],
        )

    def test_inspect_tenhou_smoke_parses_exported_xml_directory_outside_git(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            export_dir = Path(directory) / "houou-export"
            export_dir.mkdir()
            shutil.copyfile(
                Path("data/fixtures/tenhou/events_4p.xml"),
                export_dir / "2026040200gm-00a9-0000-smoke.xml",
            )
            report = Path(directory) / "inspect-export.json"

            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "inspect-tenhou",
                        str(export_dir),
                        "--report",
                        str(report),
                        "--source-label",
                        "houou-export-smoke",
                        "--source-date",
                        "synthetic-runtime-copy",
                        "--source-command",
                        "houou-logs export DB OUT --players 4 --length h --limit 1",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines()[:4],
            ["rounds: 1", "discards: 2", "discard_examples: 2", "call_examples: 1"],
        )
        self.assertEqual(payload["kind"], "kenjaku-tenhou-inspect-report-v0")
        self.assertEqual(payload["source"]["label"], "houou-export-smoke")
        self.assertEqual(payload["xml_file_count"], 1)
        self.assertEqual(payload["parse_failures"]["count"], 0)
        self.assertIn("report_path:", stdout.getvalue())

    def test_inspect_tenhou_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "inspect.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "inspect-tenhou",
                        "data/fixtures/tenhou",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-dir",
                        "--source-date",
                        "synthetic",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-tenhou-inspect-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-dir")
        self.assertEqual(payload["source"]["date"], "synthetic")
        self.assertEqual(payload["xml_file_count"], 3)
        self.assertEqual(payload["rounds"], 3)
        self.assertEqual(payload["discards"], 4)
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["call_examples"], 1)
        self.assertEqual(payload["discard_shanten"]["examples"], 4)
        self.assertIn("average_delta", payload["discard_shanten"])
        self.assertEqual(payload["parse_failures"]["count"], 0)
        self.assertIn("report_path:", stdout.getvalue())

    def test_inspect_tenhou_can_report_parse_failures(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            broken = Path(directory) / "broken.xml"
            broken.write_text("<mjloggm><INIT /></mjloggm>", encoding="utf-8")
            report = Path(directory) / "inspect.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "inspect-tenhou",
                        "data/fixtures/tenhou/minimal_4p.xml",
                        str(broken),
                        "--skip-errors",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("parse_failures: 1", stdout.getvalue())
        self.assertEqual(payload["rounds"], 1)
        self.assertEqual(payload["parse_failures"]["count"], 1)
        self.assertEqual(payload["parse_failures"]["items"][0]["error_type"], "ValueError")

    def test_defense_risk_summary_outputs_text_json_and_report(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "defense-risk.json"

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "defense-risk-summary",
                        "data/fixtures/tenhou",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-defense-risk",
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    ["defense-risk-summary", "data/fixtures/tenhou", "--json"]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("examples: 4", text_stdout.getvalue())
        self.assertIn("active_riichi_examples: 1", text_stdout.getvalue())
        self.assertIn("actual_mean_risk: 0.1775", text_stdout.getvalue())
        self.assertIn("outcome_labeled_examples: 4", text_stdout.getvalue())
        self.assertIn("eventual_deal_in_examples: 1", text_stdout.getvalue())
        self.assertIn("eventual_deal_in_mean_risk: 0.7100", text_stdout.getvalue())
        self.assertIn("active_riichi_deal_in_examples: 1", text_stdout.getvalue())
        self.assertIn("actual_risk_bands: low=3 medium=0 high=1", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-defense-risk-summary-v0")
        self.assertEqual(report_payload["source"]["label"], "fixture-defense-risk")
        self.assertEqual(report_payload["xml_file_count"], 3)
        self.assertEqual(report_payload["examples"], 4)
        self.assertEqual(report_payload["active_riichi_examples"], 1)
        self.assertFalse(report_payload["calibrated_probability"])
        self.assertEqual(
            report_payload["outcome_analysis"]["kind"],
            "kenjaku-defense-risk-outcome-analysis-v0",
        )
        self.assertEqual(report_payload["outcome_analysis"]["labeled_examples"], 4)
        self.assertEqual(
            report_payload["outcome_analysis"]["buckets"]["eventual_deal_in"]["examples"],
            1,
        )
        self.assertEqual(report_payload["parse_failures"]["count"], 0)
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["kind"], "kenjaku-defense-risk-summary-v0")
        self.assertEqual(json_payload["actual_discard_risk"]["mean"], 0.1775)
        self.assertEqual(json_payload["outcome_analysis"]["missing_outcomes"], 0)

    def test_benchmark_deal_in_outputs_text_json_and_report(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "deal-in.json"

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "benchmark-deal-in",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "2",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-deal-in",
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "benchmark-deal-in",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "2",
                        "--json",
                    ]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("examples: 4", text_stdout.getvalue())
        self.assertIn("direct_deal_in_examples: 1", text_stdout.getvalue())
        self.assertIn("active_riichi_examples: 1", text_stdout.getvalue())
        self.assertIn("model: deal-in-linear-v0", text_stdout.getvalue())
        self.assertIn("eval_brier_score:", text_stdout.getvalue())
        self.assertIn("heuristic_eval_brier_score:", text_stdout.getvalue())
        self.assertIn("eval_best_threshold:", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-deal-in-benchmark-report-v0")
        self.assertEqual(report_payload["source"]["label"], "fixture-deal-in")
        self.assertEqual(report_payload["deal_in_examples"], 4)
        self.assertEqual(report_payload["label_summary"]["direct_deal_in_examples"], 1)
        self.assertEqual(report_payload["model"]["kind"], "deal-in-linear-v0")
        self.assertEqual(report_payload["calibration"]["target"], "deal_in")
        self.assertEqual(len(report_payload["calibration"]["thresholds"]), 21)
        self.assertIsNotNone(report_payload["calibration"]["train"]["best"])
        self.assertIsNotNone(report_payload["calibration"]["eval"]["best"])
        self.assertFalse(report_payload["heuristic_risk_baseline"]["calibrated_probability"])
        self.assertEqual(report_payload["parse_failures"]["count"], 0)
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["kind"], "kenjaku-deal-in-benchmark-report-v0")
        self.assertEqual(json_payload["label_summary"]["examples"], 4)
        self.assertEqual(json_payload["calibration"]["target"], "deal_in")

    def test_benchmark_report_summary_supports_deal_in_reports(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "deal-in.json"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "benchmark-deal-in",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "2",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-deal-in",
                    ]
                )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("model: deal-in-linear-v0 feature_dim=25", text_stdout.getvalue())
        self.assertIn("calibration: target=deal_in", text_stdout.getvalue())
        self.assertIn("heuristic_eval:", text_stdout.getvalue())
        self.assertIn("eval_brier_score_vs_heuristic:", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["target"], "deal_in")
        self.assertEqual(payload["reports"][0]["label_summary"]["direct_deal_in_examples"], 1)
        self.assertEqual(payload["reports"][0]["calibration"]["target"], "deal_in")
        self.assertIsNotNone(payload["reports"][0]["calibration"]["train_best_threshold"])
        self.assertIsNotNone(payload["reports"][0]["calibration"]["eval_best_threshold"])
        self.assertIn("eval_brier_score_vs_heuristic", payload["reports"][0]["deltas"])

    def test_export_decision_snapshots_writes_jsonl(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "snapshots.jsonl"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(output),
                        "--limit",
                        "5",
                        "--source-label",
                        "fixture-snapshots",
                    ]
                )
            rows = [
                json.loads(line)
                for line in output.read_text(encoding="utf-8").splitlines()
            ]

        self.assertEqual(exit_code, 0)
        self.assertEqual(len(rows), 5)
        self.assertIn("snapshots: 5", stdout.getvalue())
        self.assertEqual(rows[0]["kind"], "kenjaku-decision-snapshot-v0")
        self.assertEqual(rows[0]["source"]["label"], "fixture-snapshots")
        self.assertEqual(rows[0]["xml_file_count"], 3)
        self.assertEqual(rows[0]["mjai_events"][0]["type"], "start_kyoku")
        self.assertIn(rows[0]["decision_type"], {"discard", "call", "riichi"})
        self.assertIn("actual_action", rows[0])
        self.assertIn("legal_actions", rows[0])
        self.assertNotIn("terminal_outcome", rows[0])

    def test_export_decision_snapshots_can_include_outcome_labels(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "call-snapshots.jsonl"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--decision-types",
                        "call",
                        "--include-outcome",
                        "--output",
                        str(output),
                    ]
                )
            rows = [
                json.loads(line)
                for line in output.read_text(encoding="utf-8").splitlines()
            ]

        self.assertEqual(exit_code, 0)
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["terminal_outcome"]["kind"], "agari")
        self.assertEqual(rows[0]["terminal_outcome"]["winner_seats"], [2])
        self.assertEqual(rows[0]["terminal_outcome"]["from_seats"], [1])
        self.assertIsNone(rows[0]["terminal_outcome"]["score_deltas"])
        self.assertIn("snapshots: 1", stdout.getvalue())

    def test_export_decision_snapshots_filters_types(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "call-snapshots.jsonl"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--decision-types",
                        "call",
                        "--output",
                        str(output),
                    ]
                )
            rows = [
                json.loads(line)
                for line in output.read_text(encoding="utf-8").splitlines()
            ]

        self.assertEqual(exit_code, 0)
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["decision_type"], "call")
        self.assertEqual(rows[0]["actual_action"]["kind"], "pon")
        self.assertEqual(rows[0]["discarded_tile"], "1p")
        self.assertIn({"kind": "pass"}, rows[0]["legal_actions"])
        self.assertIn("decision_types: call", stdout.getvalue())

    def test_export_decision_snapshots_exports_riichi_type(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "riichi-snapshots.jsonl"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--decision-types",
                        "riichi",
                        "--output",
                        str(output),
                    ]
                )
            rows = [
                json.loads(line)
                for line in output.read_text(encoding="utf-8").splitlines()
            ]

        self.assertEqual(exit_code, 0)
        self.assertGreaterEqual(len(rows), 1)
        self.assertTrue(all(row["decision_type"] == "riichi" for row in rows))
        self.assertTrue(
            all(
                {"kind": "riichi"} in row["legal_actions"]
                for row in rows
            )
        )

    def test_decision_snapshot_summary_reports_counts_and_malformed_rows(self) -> None:
        with TemporaryDirectory() as directory:
            output = Path(directory) / "snapshots.jsonl"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(output),
                        "--limit",
                        "5",
                        "--source-label",
                        "fixture-snapshots",
                    ]
                )
            with output.open("a", encoding="utf-8") as handle:
                handle.write("not-json\n")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["decision-snapshot-summary", str(output)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["decision-snapshot-summary", str(output), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("snapshots: 5", text_stdout.getvalue())
        self.assertIn("malformed_rows: 1", text_stdout.getvalue())
        self.assertIn("fixture-snapshots", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-decision-snapshot-summary-v0")
        self.assertEqual(payload["snapshots"], 5)
        self.assertEqual(payload["malformed_rows"], 1)
        self.assertEqual(payload["sources"]["fixture-snapshots"], 5)
        self.assertGreaterEqual(payload["mjai_events"]["present"], 1)

    def test_decision_snapshots_include_row_id_and_compare_predictions(self) -> None:
        with TemporaryDirectory() as directory:
            snapshots = Path(directory) / "snapshots.jsonl"
            predictions = Path(directory) / "predictions.jsonl"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(snapshots),
                        "--limit",
                        "5",
                    ]
                )
            rows = [
                json.loads(line)
                for line in snapshots.read_text(encoding="utf-8").splitlines()
            ]
            prediction_rows = [
                {
                    "row_id": row["row_id"],
                    "predicted_action": row["actual_action"],
                }
                for row in rows[:-1]
            ]
            predictions.write_text(
                "\n".join(json.dumps(row) for row in prediction_rows) + "\nnot-json\n",
                encoding="utf-8",
            )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    ["decision-snapshot-compare", str(snapshots), str(predictions)]
                )

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "decision-snapshot-compare",
                        str(snapshots),
                        str(predictions),
                        "--json",
                    ]
                )
            payload = json.loads(json_stdout.getvalue())

        self.assertTrue(all(isinstance(row["row_id"], str) for row in rows))
        self.assertEqual(text_exit_code, 0)
        self.assertIn("missing_predictions: 1", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-decision-snapshot-comparison-v0")
        self.assertEqual(payload["snapshots"], 5)
        self.assertEqual(payload["predictions"], 4)
        self.assertEqual(payload["missing_predictions"], 1)
        self.assertEqual(payload["malformed_prediction_rows"], 1)
        self.assertEqual(payload["overall"]["correct"], 4)

    def test_produce_decision_predictions_echo_actual_round_trips(self) -> None:
        with TemporaryDirectory() as directory:
            snapshots = Path(directory) / "snapshots.jsonl"
            predictions = Path(directory) / "predictions.jsonl"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(snapshots),
                        "--limit",
                        "5",
                    ]
                )

            producer_stdout = io.StringIO()
            with contextlib.redirect_stdout(producer_stdout):
                producer_exit_code = main(
                    [
                        "produce-decision-predictions",
                        str(snapshots),
                        "--strategy",
                        "echo-actual",
                        "--output",
                        str(predictions),
                    ]
                )

            compare_stdout = io.StringIO()
            with contextlib.redirect_stdout(compare_stdout):
                compare_exit_code = main(
                    ["decision-snapshot-compare", str(snapshots), str(predictions)]
                )
            prediction_rows = [
                json.loads(line)
                for line in predictions.read_text(encoding="utf-8").splitlines()
            ]

        self.assertEqual(producer_exit_code, 0)
        self.assertIn("strategy: echo-actual", producer_stdout.getvalue())
        self.assertIn("predictions: 5", producer_stdout.getvalue())
        self.assertEqual(compare_exit_code, 0)
        self.assertIn("overall_accuracy: 1.0000", compare_stdout.getvalue())
        self.assertTrue(all("row_id" in row for row in prediction_rows))
        self.assertTrue(all("predicted_action" in row for row in prediction_rows))

    def test_external_prediction_producer_round_trips_through_subprocess_boundary(self) -> None:
        with TemporaryDirectory() as directory:
            snapshots = Path(directory) / "snapshots.jsonl"
            predictions = Path(directory) / "predictions.jsonl"
            compare_report = Path(directory) / "compare.json"
            producer = Path(directory) / "producer.py"
            producer.write_text(
                "\n".join(
                    [
                        "import json",
                        "import os",
                        "snapshots = os.environ['KENJAKU_SNAPSHOTS']",
                        "predictions = os.environ['KENJAKU_PREDICTIONS']",
                        "with open(snapshots, encoding='utf-8') as source, "
                        "open(predictions, 'w', encoding='utf-8') as target:",
                        "    for line in source:",
                        "        row = json.loads(line)",
                        "        target.write(json.dumps({"
                        "'row_id': row['row_id'], "
                        "'predicted_action': row['actual_action']"
                        "}) + '\\n')",
                    ]
                ),
                encoding="utf-8",
            )
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(snapshots),
                        "--limit",
                        "5",
                    ]
                )

            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "run-external-prediction-producer",
                        str(snapshots),
                        "--output",
                        str(predictions),
                        "--compare-report",
                        str(compare_report),
                        "--command",
                        sys.executable,
                        str(producer),
                    ]
                )
            comparison = json.loads(compare_report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("command_returncode: 0", stdout.getvalue())
        self.assertIn("predictions: 5", stdout.getvalue())
        self.assertIn("compare_report_path:", stdout.getvalue())
        self.assertEqual(comparison["kind"], "kenjaku-decision-snapshot-comparison-v0")
        self.assertEqual(comparison["overall"]["accuracy"], 1.0)

    def test_external_baseline_report_compares_named_prediction_files(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = root / "snapshots.jsonl"
            echo_predictions = root / "echo.jsonl"
            pass_predictions = root / "pass.jsonl"
            first_predictions = root / "first.jsonl"
            report_path = root / "external-baselines.json"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(snapshots),
                        "--limit",
                        "5",
                    ]
                )
                for strategy, output in (
                    ("echo-actual", echo_predictions),
                    ("pass", pass_predictions),
                    ("first-legal", first_predictions),
                ):
                    main(
                        [
                            "produce-decision-predictions",
                            str(snapshots),
                            "--strategy",
                            strategy,
                            "--output",
                            str(output),
                        ]
                    )

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "external-baseline-report",
                        str(snapshots),
                        "--baseline",
                        f"kenjaku:echo-actual={echo_predictions}",
                        "--baseline",
                        f"mortal-compatible:pass-smoke={pass_predictions}",
                        "--baseline",
                        f"akochan-compatible:first-legal-smoke={first_predictions}",
                        "--min-decisions",
                        "1",
                        "--json",
                    ]
                )
            payload = json.loads(json_stdout.getvalue())

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "external-baseline-report",
                        str(snapshots),
                        "--baseline",
                        f"kenjaku:echo-actual={echo_predictions}",
                        "--baseline",
                        f"mortal-compatible:pass-smoke={pass_predictions}",
                        "--baseline",
                        f"akochan-compatible:first-legal-smoke={first_predictions}",
                        "--min-decisions",
                        "1",
                        "--report",
                        str(report_path),
                    ]
                )
            saved_report = json.loads(report_path.read_text(encoding="utf-8"))

        self.assertEqual(json_exit_code, 0)
        self.assertEqual(text_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-external-baseline-report-v0")
        self.assertEqual(payload["protocol"]["comparison_unit"], "decision")
        self.assertTrue(payload["minimum_satisfied"])
        self.assertEqual(payload["snapshots"]["snapshots"], 5)
        self.assertEqual(len(payload["baselines"]), 3)
        self.assertEqual(payload["baselines"][0]["family"], "kenjaku")
        self.assertEqual(payload["baselines"][0]["comparable_decisions"], 5)
        self.assertEqual(payload["baselines"][0]["overall"]["accuracy"], 1.0)
        self.assertEqual(payload["baselines"][0]["overall"]["accuracy_ci"]["method"], "wilson")
        self.assertIn("mortal-compatible:pass-smoke", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(saved_report["kind"], "kenjaku-external-baseline-report-v0")

    def test_external_baseline_report_enforces_minimum_decisions(self) -> None:
        with TemporaryDirectory() as directory:
            snapshots = Path(directory) / "snapshots.jsonl"
            predictions = Path(directory) / "predictions.jsonl"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(snapshots),
                        "--limit",
                        "2",
                    ]
                )
                main(
                    [
                        "produce-decision-predictions",
                        str(snapshots),
                        "--strategy",
                        "echo-actual",
                        "--output",
                        str(predictions),
                    ]
                )

            with self.assertRaises(SystemExit) as raised:
                main(
                    [
                        "external-baseline-report",
                        str(snapshots),
                        "--baseline",
                        f"kenjaku:echo-actual={predictions}",
                        "--min-decisions",
                        "3",
                    ]
                )

        self.assertIn("below minimum comparable decisions", str(raised.exception))

    def test_train_discard_baseline_fixture(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["train-discard-baseline", "data/fixtures/tenhou/minimal_4p.xml"])

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines(),
            ["examples: 2", "top_discard: 4p", "training_accuracy: 0.5000"],
        )

    def test_train_discard_linear_fixture(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(
                [
                    "train-discard-linear",
                    "data/fixtures/tenhou/minimal_4p.xml",
                    "--epochs",
                    "5",
                    "--eval-fraction",
                    "0.5",
                ]
            )

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines(),
            [
                "examples: 2",
                "train_examples: 1",
                "eval_examples: 1",
                "train_accuracy: 1.0000",
                "eval_accuracy: 0.0000",
            ],
        )

    def test_train_discard_linear_writes_model_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "model.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "train-discard-linear",
                        "data/fixtures/tenhou/minimal_4p.xml",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0",
                        "--output",
                        str(output),
                    ]
                )
            artifact_exists = output.exists()

        self.assertEqual(exit_code, 0)
        self.assertTrue(artifact_exists)
        self.assertIn("model_path:", stdout.getvalue())

    def test_train_discard_linear_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "model.json"
            report = Path(directory) / "reports" / "linear.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "train-discard-linear",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--l2",
                        "0.001",
                        "--output",
                        str(output),
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-train",
                        "--source-command",
                        "unit-test",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-discard-linear-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-train")
        self.assertEqual(payload["source"]["command"], "unit-test")
        self.assertEqual(payload["xml_file_count"], 3)
        self.assertEqual(payload["rounds"], 3)
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["call_examples"], 1)
        self.assertEqual(payload["split"]["seed"], "fixed")
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(payload["model"]["kind"], "discard-linear-v1")
        self.assertEqual(payload["model"]["feature_dim"], 76)
        self.assertEqual(payload["training"]["l2"], 0.001)
        self.assertEqual(payload["discard_shanten"]["examples"], 4)
        self.assertEqual(payload["parse_failures"]["count"], 0)
        self.assertEqual(payload["artifacts"]["model_path"], str(output))
        self.assertIn("report_path:", stdout.getvalue())

    def test_train_discard_mlp_fixture_smoke_writes_report_artifact(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "mlp.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "train-discard-mlp",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--hidden-dim",
                        "8",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--seed",
                        "123",
                        "--device",
                        "cpu",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-mlp",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("device: cpu", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-mlp-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-mlp")
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(payload["model"]["kind"], "discard-mlp-v0")
        self.assertEqual(payload["model"]["input_dim"], 68)
        self.assertEqual(payload["model"]["hidden_dim"], 8)
        self.assertEqual(payload["training"]["device"], "cpu")
        self.assertEqual(payload["training"]["seed"], 123)
        self.assertEqual(payload["training"]["best_epoch"], 1)
        self.assertEqual(payload["training"]["selection_split"], "eval")
        self.assertEqual([row["epoch"] for row in payload["training"]["history"]], [1])
        self.assertEqual(payload["metrics"]["train"]["examples"], 3)
        self.assertEqual(payload["metrics"]["eval"]["examples"], 1)
        self.assertEqual(payload["metrics"]["best"]["eval"]["examples"], 1)
        self.assertIsNone(payload["artifacts"]["checkpoint_path"])

    def test_benchmark_report_summary_supports_standalone_mlp_report(self) -> None:
        report_payload = {
            "kind": "kenjaku-discard-mlp-report-v0",
            "source": {"label": "synthetic-mlp", "command": None, "date": None},
            "input_paths": ["synthetic"],
            "xml_file_count": 1,
            "rounds": 1,
            "draws": 0,
            "discards": 0,
            "reaches": 0,
            "calls": 0,
            "wins": 0,
            "exhaustive_draws": 0,
            "discard_examples": 4,
            "call_examples": 1,
            "split": {
                "seed": "fixed",
                "eval_fraction": 0.25,
                "train_examples": 3,
                "eval_examples": 1,
            },
            "model": {
                "kind": "discard-mlp-v0",
                "input_dim": 68,
                "hidden_dim": 8,
                "output_dim": 34,
            },
            "training": {
                "epochs": 1,
                "batch_size": 2,
                "learning_rate": 0.001,
                "device": "cpu",
                "seed": 123,
                "history": [],
                "best_epoch": 1,
                "selection_split": "eval",
            },
            "metrics": {
                "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.0},
                "best": {
                    "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                    "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.0},
                },
            },
            "discard_shanten": {"examples": 4},
            "parse_failures": {"count": 0, "items": []},
            "artifacts": {"checkpoint_path": None},
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "mlp.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("model: discard-mlp-v0 hidden_dim=8", text_stdout.getvalue())
        self.assertIn("best: epoch=1 split=eval", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-discard-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["target"], "discard_mlp")
        self.assertEqual(payload["reports"][0]["model"]["hidden_dim"], 8)

    def test_train_discard_transformer_fixture_smoke_writes_report_artifact(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "transformer.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "train-discard-transformer",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--model-dim",
                        "16",
                        "--num-heads",
                        "4",
                        "--num-layers",
                        "1",
                        "--feedforward-dim",
                        "32",
                        "--dropout",
                        "0.0",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--seed",
                        "123",
                        "--device",
                        "cpu",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-transformer",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("device: cpu", stdout.getvalue())
        self.assertIn("model: discard-transformer-policy-v0", stdout.getvalue())
        self.assertIn("encoder: mahjong-transformer-encoder-v0", stdout.getvalue())
        self.assertIn("input_tokens: 152", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-transformer-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-transformer")
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(payload["model"]["kind"], "discard-transformer-policy-v0")
        self.assertEqual(payload["model"]["encoder_kind"], "mahjong-transformer-encoder-v0")
        self.assertEqual(payload["model"]["input_tokens"], 152)
        self.assertEqual(payload["model"]["config"]["model_dim"], 16)
        self.assertEqual(payload["model"]["config"]["num_layers"], 1)
        self.assertEqual(payload["training"]["device"], "cpu")
        self.assertEqual(payload["training"]["seed"], 123)
        self.assertEqual(payload["training"]["best_epoch"], 1)
        self.assertEqual(payload["metrics"]["train"]["examples"], 3)
        self.assertEqual(payload["metrics"]["eval"]["examples"], 1)
        self.assertIsNone(payload["artifacts"]["checkpoint_path"])

    def test_benchmark_report_summary_supports_synthetic_transformer_report(self) -> None:
        report_payload = {
            "kind": "kenjaku-discard-transformer-report-v0",
            "source": {"label": "synthetic-transformer", "command": None, "date": None},
            "input_paths": ["synthetic"],
            "xml_file_count": 1,
            "rounds": 1,
            "draws": 0,
            "discards": 0,
            "reaches": 0,
            "calls": 0,
            "wins": 0,
            "exhaustive_draws": 0,
            "discard_examples": 4,
            "call_examples": 1,
            "split": {
                "seed": "fixed",
                "eval_fraction": 0.25,
                "train_examples": 3,
                "eval_examples": 1,
            },
            "model": {
                "kind": "discard-transformer-policy-v0",
                "encoder_kind": "mahjong-transformer-encoder-v0",
                "input_tokens": 152,
                "output_dim": 34,
                "config": {
                    "model_dim": 16,
                    "num_heads": 4,
                    "num_layers": 1,
                    "feedforward_dim": 32,
                    "dropout": 0.0,
                },
            },
            "training": {
                "epochs": 1,
                "batch_size": 2,
                "learning_rate": 0.001,
                "device": "cpu",
                "seed": 123,
                "history": [],
                "best_epoch": 1,
                "selection_split": "eval",
            },
            "metrics": {
                "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.0},
                "best": {
                    "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                    "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.0},
                },
            },
            "discard_shanten": {"examples": 4},
            "parse_failures": {"count": 0, "items": []},
            "artifacts": {"checkpoint_path": None},
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "transformer.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn(
            "model: discard-transformer-policy-v0 encoder=mahjong-transformer-encoder-v0",
            text_stdout.getvalue(),
        )
        self.assertIn("tokens=152 dim=16 heads=4 layers=1", text_stdout.getvalue())
        self.assertIn("best: epoch=1 split=eval", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-discard-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["target"], "discard_transformer")
        self.assertEqual(payload["reports"][0]["model"]["config"]["model_dim"], 16)

    def test_benchmark_discard_transformer_fixture_smoke_writes_report_artifact(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "transformer-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-discard-transformer",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--model-dim",
                        "16",
                        "--num-heads",
                        "4",
                        "--num-layers",
                        "1",
                        "--feedforward-dim",
                        "32",
                        "--dropout",
                        "0.0",
                        "--linear-epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--seed",
                        "123",
                        "--device",
                        "cpu",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-transformer-benchmark",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("device: cpu", stdout.getvalue())
        self.assertIn("discard_transformer_eval_accuracy:", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-transformer-benchmark-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-transformer-benchmark")
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(
            payload["models"]["discard_transformer"]["kind"],
            "discard-transformer-policy-v0",
        )
        self.assertEqual(payload["models"]["discard_transformer"]["config"]["model_dim"], 16)
        self.assertIn("transformer_eval_accuracy_lift_over_defense_context", payload["deltas"])

    def test_benchmark_report_summary_supports_synthetic_transformer_benchmark(self) -> None:
        report_payload = {
            "kind": "kenjaku-discard-transformer-benchmark-report-v0",
            "source": {
                "label": "synthetic-transformer-benchmark",
                "command": None,
                "date": None,
            },
            "input_paths": ["synthetic"],
            "xml_file_count": 1,
            "rounds": 1,
            "draws": 0,
            "discards": 0,
            "reaches": 0,
            "calls": 0,
            "wins": 0,
            "exhaustive_draws": 0,
            "discard_examples": 4,
            "call_examples": 1,
            "split": {
                "seed": "fixed",
                "eval_fraction": 0.25,
                "train_examples": 3,
                "eval_examples": 1,
            },
            "models": {
                "frequency": {
                    "metrics": {"train_accuracy": 2 / 3, "eval_accuracy": 0.0},
                },
                "risk_context_linear": {
                    "kind": "discard-linear-risk-context-v0",
                    "feature_dim": 86,
                    "metrics": {"train_accuracy": 2 / 3, "eval_accuracy": 0.25},
                },
                "defense_context_linear": {
                    "kind": "discard-linear-defense-context-v0",
                    "feature_dim": 98,
                    "metrics": {"train_accuracy": 2 / 3, "eval_accuracy": 0.5},
                },
                "discard_transformer": {
                    "kind": "discard-transformer-policy-v0",
                    "encoder_kind": "mahjong-transformer-encoder-v0",
                    "input_tokens": 152,
                    "output_dim": 34,
                    "config": {
                        "model_dim": 16,
                        "num_heads": 4,
                        "num_layers": 1,
                        "feedforward_dim": 32,
                        "dropout": 0.0,
                    },
                    "training": {
                        "epochs": 1,
                        "batch_size": 2,
                        "learning_rate": 0.001,
                        "device": "cpu",
                        "seed": 123,
                        "history": [],
                        "best_epoch": 1,
                        "selection_split": "eval",
                    },
                    "metrics": {
                        "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                        "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.75},
                        "best": {
                            "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                            "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.75},
                        },
                    },
                },
            },
            "deltas": {
                "transformer_eval_accuracy_lift_over_frequency": 0.75,
                "transformer_eval_accuracy_lift_over_risk_context": 0.5,
                "transformer_eval_accuracy_lift_over_defense_context": 0.25,
            },
            "discard_shanten": {"examples": 4},
            "parse_failures": {"count": 0, "items": []},
            "artifacts": {"checkpoint_path": None},
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "transformer-benchmark.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("discard_transformer: eval=0.7500", text_stdout.getvalue())
        self.assertIn(
            "transformer_eval_accuracy_lift_over_defense_context: +0.2500",
            text_stdout.getvalue(),
        )
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["reports"][0]["target"], "discard_transformer_benchmark")
        self.assertEqual(
            payload["reports"][0]["deltas"][
                "transformer_eval_accuracy_lift_over_defense_context"
            ],
            0.25,
        )

    def test_benchmark_report_summary_supports_synthetic_mlp_benchmark_report(self) -> None:
        report_payload = build_discard_mlp_benchmark_report(
            input_paths=[Path("synthetic")],
            xml_files=[Path("synthetic.xml")],
            game=parse_tenhou_xml_file(Path("data/fixtures/tenhou/minimal_4p.xml")),
            discard_examples=4,
            call_examples=1,
            split_seed="fixed",
            eval_fraction=0.25,
            train_examples=3,
            eval_examples=1,
            models={
                "frequency": {
                    "metrics": {"train_accuracy": 2 / 3, "eval_accuracy": 0.0},
                },
                "risk_context_linear": {
                    "kind": "discard-linear-risk-context-v0",
                    "feature_dim": 86,
                    "metrics": {"train_accuracy": 2 / 3, "eval_accuracy": 0.25},
                },
                "defense_context_linear": {
                    "kind": "discard-linear-defense-context-v0",
                    "feature_dim": 98,
                    "metrics": {"train_accuracy": 2 / 3, "eval_accuracy": 0.5},
                },
                "discard_mlp": {
                    "kind": "discard-mlp-v0",
                    "input_dim": 68,
                    "hidden_dim": 8,
                    "output_dim": 34,
                    "training": {
                        "epochs": 1,
                        "batch_size": 2,
                        "learning_rate": 0.001,
                        "device": "cpu",
                        "seed": 123,
                        "history": [],
                        "best_epoch": 1,
                        "selection_split": "eval",
                    },
                    "metrics": {
                        "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                        "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.75},
                        "best": {
                            "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                            "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.75},
                        },
                    },
                },
            },
            discard_shanten={"examples": 4},
            parse_failures=(),
            checkpoint_path=None,
            source={"label": "synthetic-mlp-benchmark", "command": None, "date": None},
        )

        with TemporaryDirectory() as directory:
            report = Path(directory) / "mlp-benchmark.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("discard_mlp: eval=0.7500", text_stdout.getvalue())
        self.assertIn(
            "mlp_eval_accuracy_lift_over_defense_context: +0.2500",
            text_stdout.getvalue(),
        )
        self.assertEqual(
            report_payload["deltas"]["mlp_eval_accuracy_lift_over_defense_context"],
            0.25,
        )
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["reports"][0]["target"], "discard_mlp_benchmark")
        self.assertEqual(payload["reports"][0]["models"]["discard_mlp"]["hidden_dim"], 8)

    def test_train_discard_mlp_checkpoint_writes_best_state(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        torch = __import__("torch")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "mlp.pt"
            report = Path(directory) / "mlp.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "train-discard-mlp",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--hidden-dim",
                        "8",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--seed",
                        "123",
                        "--device",
                        "cpu",
                        "--checkpoint",
                        str(checkpoint),
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))
            checkpoint_payload = torch.load(checkpoint, map_location="cpu")

        self.assertEqual(exit_code, 0)
        self.assertIn("checkpoint_path:", stdout.getvalue())
        self.assertEqual(report_payload["artifacts"]["checkpoint_path"], str(checkpoint))
        self.assertEqual(checkpoint_payload["kind"], "kenjaku-discard-mlp-checkpoint-v0")
        self.assertEqual(checkpoint_payload["model"]["kind"], "discard-mlp-v0")
        self.assertEqual(checkpoint_payload["model"]["hidden_dim"], 8)
        self.assertEqual(
            checkpoint_payload["training"]["best_epoch"],
            report_payload["training"]["best_epoch"],
        )
        self.assertEqual(
            checkpoint_payload["training"]["selection_split"],
            report_payload["training"]["selection_split"],
        )
        self.assertEqual(checkpoint_payload["metrics"]["best"], report_payload["metrics"]["best"])
        self.assertIn("net.0.weight", checkpoint_payload["model_state_dict"])

    def test_benchmark_discard_mlp_fixture_smoke_writes_report_artifact(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "mlp-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-discard-mlp",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--hidden-dim",
                        "8",
                        "--linear-epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--seed",
                        "123",
                        "--device",
                        "cpu",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-mlp-benchmark",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("discard_mlp_best_eval_accuracy:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-mlp-benchmark-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-mlp-benchmark")
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(
            set(payload["models"]),
            {"frequency", "risk_context_linear", "defense_context_linear", "discard_mlp"},
        )
        self.assertEqual(payload["models"]["discard_mlp"]["hidden_dim"], 8)
        self.assertEqual(payload["models"]["discard_mlp"]["training"]["device"], "cpu")
        self.assertIn("mlp_eval_accuracy_lift_over_defense_context", payload["deltas"])

        summary_stdout = io.StringIO()
        with contextlib.redirect_stdout(summary_stdout):
            summary_exit_code = main(["benchmark-report-summary", str(report)])
        self.assertEqual(summary_exit_code, 0)
        self.assertIn("discard_mlp: eval=", summary_stdout.getvalue())
        self.assertIn("deltas:", summary_stdout.getvalue())

    def test_benchmark_discard_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-benchmark",
                        "--source-command",
                        "unit-test",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines()[:19],
            [
                "examples: 4",
                "train_examples: 3",
                "eval_examples: 1",
                "frequency_train_accuracy: 0.6667",
                "frequency_eval_accuracy: 0.0000",
                "raw_count_linear_train_accuracy: 0.6667",
                "raw_count_linear_eval_accuracy: 0.0000",
                "linear_train_accuracy: 0.6667",
                "linear_eval_accuracy: 0.0000",
                "linear_eval_lift_over_raw_count: +0.0000",
                "risk_context_linear_train_accuracy: 0.6667",
                "risk_context_linear_eval_accuracy: 0.0000",
                "risk_context_linear_eval_lift_over_linear: +0.0000",
                "defense_context_linear_train_accuracy: 0.6667",
                "defense_context_linear_eval_accuracy: 0.0000",
                "defense_context_linear_eval_lift_over_risk_context: +0.0000",
                "defense_context_v1_linear_train_accuracy: 0.6667",
                "defense_context_v1_linear_eval_accuracy: 0.0000",
                "defense_context_v1_linear_eval_lift_over_defense_context: +0.0000",
            ],
        )
        self.assertIn("report_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-benchmark-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-benchmark")
        self.assertEqual(payload["source"]["command"], "unit-test")
        self.assertEqual(payload["xml_file_count"], 3)
        self.assertEqual(payload["rounds"], 3)
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["call_examples"], 1)
        self.assertEqual(payload["split"]["seed"], "fixed")
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(payload["models"]["frequency"]["metrics"]["train_accuracy"], 2 / 3)
        self.assertEqual(payload["models"]["frequency"]["metrics"]["eval_accuracy"], 0.0)
        self.assertEqual(payload["models"]["frequency"]["metrics"]["loss_kind"], "zero_one")
        self.assertAlmostEqual(payload["models"]["frequency"]["metrics"]["train_loss"], 1 / 3)
        self.assertEqual(payload["models"]["frequency"]["metrics"]["eval_loss"], 1.0)
        self.assertIn("4p", payload["models"]["frequency"]["metrics"]["train_action_recall"])
        self.assertIn("4p", payload["models"]["frequency"]["metrics"]["eval_action_recall"])
        self.assertIn("train_balanced_accuracy", payload["models"]["frequency"]["metrics"])
        self.assertIn("eval_balanced_accuracy", payload["models"]["frequency"]["metrics"])
        self.assertEqual(payload["models"]["frequency"]["eval_analysis"]["overall"]["examples"], 1)
        self.assertEqual(payload["models"]["frequency"]["eval_analysis"]["overall"]["correct"], 0)
        self.assertEqual(
            payload["models"]["raw_count_linear"]["kind"],
            "discard-linear-raw-count-v0",
        )
        self.assertEqual(payload["models"]["raw_count_linear"]["feature_dim"], 69)
        self.assertEqual(payload["models"]["raw_count_linear"]["training"]["l2"], 0.0)
        self.assertEqual(
            payload["models"]["raw_count_linear"]["metrics"]["train_accuracy"],
            2 / 3,
        )
        self.assertIn("weight_summary", payload["models"]["raw_count_linear"])
        self.assertIn("feature_summary", payload["models"]["raw_count_linear"])
        self.assertEqual(
            payload["models"]["raw_count_linear"]["feature_summary"]["feature_count"],
            69,
        )
        self.assertEqual(payload["models"]["raw_count_linear"]["metrics"]["eval_accuracy"], 0.0)
        self.assertEqual(payload["models"]["raw_count_linear"]["metrics"]["loss_kind"], "zero_one")
        self.assertIn("train_action_recall", payload["models"]["raw_count_linear"]["metrics"])
        self.assertIn("eval_action_recall", payload["models"]["raw_count_linear"]["metrics"])
        self.assertEqual(
            payload["models"]["raw_count_linear"]["eval_analysis"]["overall"]["examples"],
            1,
        )
        self.assertEqual(payload["models"]["linear"]["kind"], "discard-linear-v1")
        self.assertEqual(payload["models"]["linear"]["feature_dim"], 76)
        self.assertEqual(payload["models"]["linear"]["training"]["epochs"], 1)
        self.assertEqual(payload["models"]["linear"]["training"]["l2"], 0.0)
        self.assertEqual(payload["models"]["linear"]["metrics"]["train_accuracy"], 2 / 3)
        self.assertEqual(payload["models"]["linear"]["metrics"]["eval_accuracy"], 0.0)
        self.assertEqual(payload["models"]["linear"]["eval_analysis"]["overall"]["examples"], 1)
        self.assertIn("by_shanten_delta", payload["models"]["linear"]["eval_analysis"])
        self.assertIn("by_tile_family", payload["models"]["linear"]["eval_analysis"])
        self.assertIn("by_round_event_phase", payload["models"]["linear"]["eval_analysis"])
        self.assertIn("by_seat_turn_phase", payload["models"]["linear"]["eval_analysis"])
        self.assertEqual(
            payload["models"]["risk_context_linear"]["kind"],
            "discard-linear-risk-context-v0",
        )
        self.assertEqual(payload["models"]["risk_context_linear"]["feature_dim"], 86)
        self.assertEqual(payload["models"]["risk_context_linear"]["training"]["epochs"], 1)
        self.assertEqual(payload["models"]["risk_context_linear"]["training"]["l2"], 0.0)
        self.assertEqual(
            payload["models"]["risk_context_linear"]["metrics"]["train_accuracy"],
            2 / 3,
        )
        self.assertEqual(
            payload["models"]["risk_context_linear"]["metrics"]["eval_accuracy"],
            0.0,
        )
        self.assertEqual(
            payload["models"]["risk_context_linear"]["eval_analysis"]["overall"]["examples"],
            1,
        )
        self.assertIn(
            "by_seat_turn_phase",
            payload["models"]["risk_context_linear"]["eval_analysis"],
        )
        self.assertEqual(
            payload["models"]["defense_context_linear"]["kind"],
            "discard-linear-defense-context-v0",
        )
        self.assertEqual(payload["models"]["defense_context_linear"]["feature_dim"], 98)
        self.assertEqual(payload["models"]["defense_context_linear"]["training"]["epochs"], 1)
        self.assertEqual(payload["models"]["defense_context_linear"]["training"]["l2"], 0.0)
        self.assertEqual(
            payload["models"]["defense_context_linear"]["metrics"]["train_accuracy"],
            2 / 3,
        )
        self.assertEqual(
            payload["models"]["defense_context_linear"]["metrics"]["eval_accuracy"],
            0.0,
        )
        self.assertEqual(
            payload["models"]["defense_context_linear"]["eval_analysis"]["overall"]["examples"],
            1,
        )
        self.assertIn(
            "by_active_opponent_riichi",
            payload["models"]["defense_context_linear"]["eval_analysis"],
        )
        self.assertIn(
            "by_actual_discard_genbutsu",
            payload["models"]["defense_context_linear"]["eval_analysis"],
        )
        self.assertIn(
            "by_actual_discard_suji",
            payload["models"]["defense_context_linear"]["eval_analysis"],
        )
        self.assertEqual(
            payload["models"]["defense_context_v1_linear"]["kind"],
            "discard-linear-defense-context-v1",
        )
        self.assertEqual(payload["models"]["defense_context_v1_linear"]["feature_dim"], 112)
        self.assertEqual(payload["models"]["defense_context_v1_linear"]["training"]["epochs"], 1)
        self.assertEqual(payload["models"]["defense_context_v1_linear"]["training"]["l2"], 0.0)
        self.assertEqual(
            payload["models"]["defense_context_v1_linear"]["feature_summary"]["feature_count"],
            112,
        )
        self.assertEqual(
            payload["models"]["defense_context_v1_linear"]["metrics"]["train_accuracy"],
            2 / 3,
        )
        self.assertEqual(
            payload["models"]["defense_context_v1_linear"]["metrics"]["eval_accuracy"],
            0.0,
        )
        self.assertIn(
            "by_actual_discard_seen_after_riichi",
            payload["models"]["defense_context_v1_linear"]["eval_analysis"],
        )
        self.assertEqual(payload["ablation"]["train_accuracy_lift_over_raw_count"], 0.0)
        self.assertEqual(payload["ablation"]["eval_accuracy_lift_over_raw_count"], 0.0)
        self.assertEqual(payload["ablation"]["risk_context_train_accuracy_lift_over_linear"], 0.0)
        self.assertEqual(payload["ablation"]["risk_context_eval_accuracy_lift_over_linear"], 0.0)
        self.assertEqual(
            payload["ablation"]["defense_context_train_accuracy_lift_over_risk_context"],
            0.0,
        )
        self.assertEqual(
            payload["ablation"]["defense_context_eval_accuracy_lift_over_risk_context"],
            0.0,
        )
        self.assertEqual(
            payload["ablation"]["defense_context_v1_train_accuracy_lift_over_defense_context"],
            0.0,
        )
        self.assertEqual(
            payload["ablation"]["defense_context_v1_eval_accuracy_lift_over_defense_context"],
            0.0,
        )
        self.assertEqual(payload["discard_shanten"]["examples"], 4)
        self.assertEqual(payload["parse_failures"]["count"], 0)

    def test_benchmark_report_summary_outputs_text_and_json(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark.json"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(report),
                    ]
                )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("selected_buckets:", text_stdout.getvalue())
        self.assertIn("risk_context_linear", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-discard-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["models"]["linear"]["feature_dim"], 76)

    def test_benchmark_report_summary_supports_call_and_riichi_reports(self) -> None:
        with TemporaryDirectory() as directory:
            call_report = Path(directory) / "call.json"
            riichi_report = Path(directory) / "riichi.json"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--include-weighted",
                        "--report",
                        str(call_report),
                    ]
                )
                main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--include-weighted",
                        "--report",
                        str(riichi_report),
                    ]
                )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    ["benchmark-report-summary", str(call_report), str(riichi_report)]
                )

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "benchmark-report-summary",
                        str(call_report),
                        str(riichi_report),
                        "--json",
                    ]
                )
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("call_linear_v1_calibrated", text_stdout.getvalue())
        self.assertIn("threshold=0.40", text_stdout.getvalue())
        self.assertIn("source=tenhou-100-v0-eval-sweep", text_stdout.getvalue())
        self.assertIn("train_best=", text_stdout.getvalue())
        self.assertIn("riichi_linear_calibrated", text_stdout.getvalue())
        self.assertIn("threshold=0.95", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["target"], "call")
        self.assertEqual(payload["reports"][1]["target"], "riichi")
        self.assertEqual(
            payload["reports"][0]["models"]["call_linear_v1_calibrated"]["policy_threshold"],
            0.4,
        )
        self.assertEqual(
            payload["reports"][0]["models"]["call_linear_v1_calibrated"][
                "policy_threshold_source"
            ],
            "tenhou-100-v0-eval-sweep",
        )
        self.assertIsNotNone(
            payload["reports"][0]["models"]["call_linear_v1"]["train_best_threshold"]
        )
        self.assertEqual(
            payload["reports"][1]["models"]["riichi_linear_weighted"]["positive_class_weight"],
            2.0,
        )

    def test_benchmark_report_summary_selects_call_policy_by_balanced_accuracy(self) -> None:
        report_payload = {
            "kind": "kenjaku-call-benchmark-report-v0",
            "source": {"label": "synthetic-call", "command": None, "date": None},
            "input_paths": ["synthetic"],
            "xml_file_count": 1,
            "rounds": 1,
            "draws": 0,
            "discards": 0,
            "reaches": 0,
            "calls": 0,
            "wins": 0,
            "exhaustive_draws": 0,
            "discard_examples": 0,
            "call_examples": 100,
            "call_examples_total": 200,
            "example_limit": 100,
            "example_limit_strategy": "balanced",
            "split": {
                "seed": "fixed",
                "eval_fraction": 0.2,
                "train_examples": 80,
                "eval_examples": 20,
            },
            "models": {
                "call_linear_v1_calibrated": {
                    "kind": "call-linear-v1",
                    "feature_dim": 137,
                    "training": {"positive_class_weight": 1.0},
                    "policy": {
                        "threshold": 0.4,
                        "threshold_source": "train-best",
                    },
                    "calibration": {
                        "train": {"best": {"threshold": 0.4}},
                        "eval": {"best": {"threshold": 0.45}},
                    },
                    "metrics": {
                        "train_accuracy": 0.8,
                        "eval_accuracy": 0.78,
                        "eval_balanced_accuracy": 0.74,
                        "eval_pass_recall": 0.76,
                        "eval_call_recall": 0.72,
                    },
                },
                "call_linear_v1_weighted": {
                    "kind": "call-linear-v1",
                    "feature_dim": 137,
                    "training": {"positive_class_weight": 2.0},
                    "calibration": {
                        "train": {"best": {"threshold": 0.8}},
                        "eval": {"best": {"threshold": 0.85}},
                    },
                    "metrics": {
                        "train_accuracy": 0.7,
                        "eval_accuracy": 0.7,
                        "eval_balanced_accuracy": 0.73,
                        "eval_pass_recall": 0.60,
                        "eval_call_recall": 0.86,
                    },
                },
            },
            "timing": None,
            "feature_cache": None,
            "example_cache": None,
            "parse_failures": {"count": 0, "items": []},
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            summary = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("selected_policy: model=call_linear_v1_calibrated", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(
            summary["reports"][0]["selected_policy"]["model_name"],
            "call_linear_v1_calibrated",
        )
        self.assertEqual(
            summary["reports"][0]["selected_policy"]["eval_balanced_accuracy"],
            0.74,
        )

    def test_benchmark_dashboard_writes_public_static_html(self) -> None:
        report_payload = {
            "kind": "kenjaku-call-benchmark-report-v0",
            "source": {
                "label": "fixture-call",
                "command": "kenjaku benchmark-call local/private/raw.xml",
                "date": "2026-06-12",
            },
            "xml_file_count": 1,
            "rounds": 2,
            "call_examples": 100,
            "call_examples_total": 200,
            "example_limit": 100,
            "example_limit_strategy": "balanced",
            "split": {
                "seed": "fixed",
                "eval_fraction": 0.2,
                "train_examples": 80,
                "eval_examples": 20,
            },
            "models": {
                "call_linear_v1_calibrated": {
                    "kind": "call-linear-v1",
                    "feature_dim": 137,
                    "training": {"positive_class_weight": 1.0},
                    "policy": {
                        "threshold": 0.4,
                        "threshold_source": "train-best",
                    },
                    "calibration": {
                        "train": {"best": {"threshold": 0.4}},
                        "eval": {"best": {"threshold": 0.45}},
                    },
                    "metrics": {
                        "train_accuracy": 0.8,
                        "eval_accuracy": 0.78,
                        "eval_balanced_accuracy": 0.74,
                        "eval_pass_recall": 0.76,
                        "eval_call_recall": 0.72,
                    },
                },
            },
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call.json"
            output = Path(directory) / "public" / "index.html"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-dashboard",
                        str(report),
                        "--output",
                        str(output),
                        "--title",
                        "Fixture Public Benchmarks",
                    ]
                )
            html = output.read_text(encoding="utf-8")

        self.assertEqual(exit_code, 0)
        self.assertIn("wrote public benchmark dashboard:", stdout.getvalue())
        self.assertIn("Fixture Public Benchmarks", html)
        self.assertIn("fixture-call (2026-06-12)", html)
        self.assertIn("call_linear_v1_calibrated", html)
        self.assertIn("0.780", html)
        self.assertIn("balanced_accuracy", html)
        self.assertIn("Report JSON", html)
        self.assertIn("call.json", html)
        self.assertIn("Live rank tracking: not included", html)
        self.assertIn("requires explicit platform permission", html)
        self.assertNotIn("local/private/raw.xml", html)
        self.assertNotIn("current live rank", html.lower())

    def test_benchmark_discard_models_fast_writes_sparse_report(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark-fast.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertNotIn("raw_count_linear_train_accuracy", stdout.getvalue())
        self.assertNotIn("defense_context_v1_linear_train_accuracy", stdout.getvalue())
        self.assertEqual(
            set(payload["models"]),
            {"frequency", "linear", "risk_context_linear", "defense_context_linear"},
        )
        self.assertIsNone(payload["ablation"]["eval_accuracy_lift_over_raw_count"])
        self.assertEqual(
            payload["ablation"]["defense_context_eval_accuracy_lift_over_risk_context"],
            0.0,
        )

    def test_benchmark_discard_example_limit_records_total_examples(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark-limit.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.5",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "2",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["discard_examples"], 2)
        self.assertEqual(payload["discard_examples_total"], 4)
        self.assertEqual(payload["example_limit"], 2)
        self.assertEqual(payload["split"]["train_examples"], 1)
        self.assertEqual(payload["split"]["eval_examples"], 1)

    def test_streamed_benchmarks_stop_at_example_limit_and_record_prefix(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            discard_report = root / "discard.json"
            call_report = root / "call.json"
            riichi_report = root / "riichi.json"

            with contextlib.redirect_stdout(io.StringIO()):
                discard_exit_code = main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--models",
                        "frequency",
                        "--stream-examples",
                        "--example-limit",
                        "2",
                        "--report",
                        str(discard_report),
                    ]
                )
                call_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--models",
                        "call_frequency",
                        "--stream-examples",
                        "--example-limit",
                        "1",
                        "--report",
                        str(call_report),
                    ]
                )
                riichi_exit_code = main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou",
                        "--stream-examples",
                        "--example-limit",
                        "1",
                        "--report",
                        str(riichi_report),
                    ]
                )
            discard_payload = json.loads(discard_report.read_text(encoding="utf-8"))
            call_payload = json.loads(call_report.read_text(encoding="utf-8"))
            riichi_payload = json.loads(riichi_report.read_text(encoding="utf-8"))

        self.assertEqual(discard_exit_code, 0)
        self.assertEqual(call_exit_code, 0)
        self.assertEqual(riichi_exit_code, 0)
        for payload in (discard_payload, call_payload, riichi_payload):
            self.assertTrue(payload["streaming_example_limit"])
            self.assertEqual(payload["source_xml_file_count"], 3)
            self.assertGreaterEqual(payload["parsed_xml_file_count"], 1)
            self.assertLessEqual(
                payload["parsed_xml_file_count"],
                payload["source_xml_file_count"],
            )
        self.assertEqual(discard_payload["discard_examples"], 2)
        self.assertEqual(call_payload["call_examples"], 1)
        self.assertEqual(riichi_payload["riichi_examples"], 1)

    def test_benchmark_discard_explicit_models(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark-explicit.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "risk_context_linear,defense_context_linear",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertNotIn("frequency_train_accuracy", stdout.getvalue())
        self.assertEqual(set(payload["models"]), {"risk_context_linear", "defense_context_linear"})
        self.assertIsNone(payload["ablation"]["risk_context_eval_accuracy_lift_over_linear"])
        self.assertEqual(
            payload["ablation"]["defense_context_eval_accuracy_lift_over_risk_context"],
            0.0,
        )

    def test_benchmark_discard_writes_disagreement_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            disagreements = Path(directory) / "disagreements.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--disagreements",
                        str(disagreements),
                        "--max-disagreements",
                        "2",
                    ]
                )
            payload = json.loads(disagreements.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("disagreements_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-disagreements-v0")
        self.assertEqual(payload["max_per_category"], 2)
        self.assertIn("risk_correct_defense_wrong", payload["categories"])
        for category in payload["categories"].values():
            self.assertLessEqual(len(category["items"]), 2)

    def test_disagreement_record_includes_defense_risk_payload(self) -> None:
        hand = tuple(Tile.parse(tile) for tile in ["4m", "5m"])
        opponent_river = (Tile.parse("4m"),)
        rivers_by_seat = ((), opponent_river, (), ())
        example = DiscardExample(
            round_index=0,
            event_index=0,
            seat=0,
            dealer=0,
            scores=(25000, 25000, 25000, 25000),
            hand_counts=tile_counts(hand),
            visible_counts=tile_counts((*hand, *opponent_river)),
            action=Action.discard("5m"),
            active_riichi_seats=(False, True, False, False),
            river_counts_by_seat=tuple(tile_counts(river) for river in rivers_by_seat),
            rivers_by_seat=rivers_by_seat,
            riichi_declared_turns=(None, 0, None, None),
            riichi_declared_event_indices=(None, 1, None, None),
        )
        tile_4m = TileType.parse("4m")
        tile_5m = TileType.parse("5m")

        record = _disagreement_record(
            example,
            predictions={
                "risk_context_linear": tile_5m,
                "defense_context_linear": tile_4m,
            },
            correct={
                "risk_context_linear": True,
                "defense_context_linear": False,
            },
            logits_by_model={
                "risk_context_linear": {tile_4m: 0.5, tile_5m: 2.0},
                "defense_context_linear": {tile_4m: 2.0, tile_5m: 0.5},
            },
        )

        defense_risk = record["defense_risk"]
        self.assertEqual(defense_risk["actual_discard"]["tile"], "5m")
        self.assertFalse(defense_risk["actual_discard"]["calibrated_probability"])
        self.assertIn("mostly_live", defense_risk["actual_discard"]["danger_reasons"])
        self.assertEqual(defense_risk["predictions"]["defense_context_linear"]["tile"], "4m")
        self.assertIn(
            "genbutsu",
            defense_risk["predictions"]["defense_context_linear"]["safety_reasons"],
        )

    def test_disagreement_report_summary_outputs_text_and_json(self) -> None:
        payload = {
            "kind": "kenjaku-discard-disagreements-v0",
            "examples": 5,
            "max_per_category": 10,
            "categories": {
                "risk_correct_defense_wrong": {
                    "count": 1,
                    "items": [
                        {
                            "actual_discard": "5m",
                            "predictions": {
                                "risk_context_linear": "5m",
                                "defense_context_linear": "8m",
                            },
                            "defense_buckets": {
                                "active_riichi_opponent": True,
                                "genbutsu": True,
                                "suji": False,
                            },
                            "defense_risk": {
                                "actual_discard": {
                                    "tile": "5m",
                                    "risk": 0.2,
                                    "calibrated_probability": False,
                                    "active_riichi_opponents": 1,
                                    "safety_reasons": ["genbutsu"],
                                    "danger_reasons": [],
                                },
                                "predictions": {
                                    "risk_context_linear": {
                                        "tile": "5m",
                                        "risk": 0.2,
                                        "calibrated_probability": False,
                                        "active_riichi_opponents": 1,
                                        "safety_reasons": ["genbutsu"],
                                        "danger_reasons": [],
                                    },
                                    "defense_context_linear": {
                                        "tile": "8m",
                                        "risk": 0.8,
                                        "calibrated_probability": False,
                                        "active_riichi_opponents": 1,
                                        "safety_reasons": [],
                                        "danger_reasons": ["mostly_live"],
                                    },
                                },
                            },
                            "shanten_delta": {
                                "before": 2,
                                "after": 2,
                                "delta": 0,
                            },
                            "candidate_logits": {
                                "risk_context_linear": [
                                    {"tile": "5m", "logit": 3.0},
                                    {"tile": "8m", "logit": 1.0},
                                ],
                                "defense_context_linear": [
                                    {"tile": "5m", "logit": 0.5},
                                    {"tile": "8m", "logit": 2.0},
                                ],
                            },
                        }
                    ],
                }
            },
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "disagreements.json"
            report.write_text(json.dumps(payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["disagreement-report-summary", str(report)])

            examples_stdout = io.StringIO()
            with contextlib.redirect_stdout(examples_stdout):
                examples_exit_code = main(
                    ["disagreement-report-summary", str(report), "--examples", "1"]
                )

            tags_stdout = io.StringIO()
            with contextlib.redirect_stdout(tags_stdout):
                tags_exit_code = main(
                    ["disagreement-report-summary", str(report), "--examples", "1", "--tags"]
                )

            filtered_stdout = io.StringIO()
            with contextlib.redirect_stdout(filtered_stdout):
                filtered_exit_code = main(
                    [
                        "disagreement-report-summary",
                        str(report),
                        "--examples",
                        "1",
                        "--tag",
                        "defense_signal",
                    ]
                )

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    ["disagreement-report-summary", str(report), "--json", "--tags"]
                )
            summary = json.loads(json_stdout.getvalue())

        category = summary["reports"][0]["categories"]["risk_correct_defense_wrong"]
        tags = summary["tags"]["reports"][0]["categories"]["risk_correct_defense_wrong"]
        self.assertEqual(text_exit_code, 0)
        self.assertIn("risk_correct_defense_wrong: count=1 stored=1", text_stdout.getvalue())
        self.assertEqual(examples_exit_code, 0)
        self.assertIn("examples:", examples_stdout.getvalue())
        self.assertIn("actual=5m", examples_stdout.getvalue())
        self.assertIn(
            "defense_risk: actual=5m:0.200 risk_context_linear=5m:0.200 "
            "defense_context_linear=8m:0.800",
            examples_stdout.getvalue(),
        )
        self.assertIn("logits:", examples_stdout.getvalue())
        self.assertEqual(tags_exit_code, 0)
        self.assertIn("tags:", tags_stdout.getvalue())
        self.assertIn("defense_signal=1", tags_stdout.getvalue())
        self.assertIn(
            "tags: defense_signal, efficiency_like, active_riichi, safe_tile_candidate",
            tags_stdout.getvalue(),
        )
        self.assertEqual(filtered_exit_code, 0)
        self.assertIn("actual=5m", filtered_stdout.getvalue())
        self.assertIn("tags: defense_signal", filtered_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(summary["kind"], "kenjaku-discard-disagreement-summary-v0")
        self.assertEqual(category["defense_buckets"]["genbutsu"]["true"], 1)
        self.assertEqual(tags["stored"], 1)
        self.assertEqual(tags["defense_signal"], 1)
        self.assertEqual(tags["efficiency_like"], 1)
        self.assertEqual(tags["close_logit"], 0)
        self.assertEqual(category["actual_prediction_pairs"][0]["wrong_prediction"], "8m")
        self.assertEqual(
            category["logit_margins"]["correct_model_actual_margin"]["mean"],
            2.0,
        )
        self.assertEqual(
            category["logit_margins"]["wrong_model_error_margin"]["mean"],
            1.5,
        )

    def test_benchmark_call_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-call-benchmark-report-v0")
        self.assertEqual(payload["call_examples"], 1)
        self.assertEqual(
            set(payload["models"]),
            {
                "call_frequency",
                "call_legal_frequency",
                "call_linear",
                "call_linear_v1",
                "call_linear_v1_calibrated",
            },
        )
        frequency = payload["models"]["call_frequency"]
        legal_frequency = payload["models"]["call_legal_frequency"]
        call_linear = payload["models"]["call_linear"]
        call_linear_v1 = payload["models"]["call_linear_v1"]
        call_linear_v1_calibrated = payload["models"]["call_linear_v1_calibrated"]
        self.assertEqual(frequency["kind"], "call-frequency-v0")
        self.assertEqual(frequency["counts"]["pon"], 1)
        self.assertEqual(frequency["metrics"]["train_accuracy"], 1.0)
        self.assertEqual(frequency["metrics"]["loss_kind"], "zero_one")
        self.assertEqual(frequency["metrics"]["train_loss"], 0.0)
        self.assertIsNone(frequency["metrics"]["eval_loss"])
        self.assertEqual(frequency["metrics"]["train_balanced_accuracy"], 1.0)
        self.assertEqual(frequency["metrics"]["train_call_recall"], 1.0)
        self.assertIsNone(frequency["metrics"]["train_pass_recall"])
        self.assertIsNone(frequency["metrics"]["eval_accuracy"])
        self.assertIsNone(frequency["metrics"]["eval_balanced_accuracy"])
        self.assertEqual(frequency["train_analysis"]["by_call_or_pass"]["call"]["examples"], 1)
        self.assertEqual(frequency["train_analysis"]["by_legal_call_kinds"]["pon"]["correct"], 1)
        self.assertEqual(legal_frequency["kind"], "call-legal-frequency-v0")
        self.assertEqual(legal_frequency["counts"]["pon"], 1)
        self.assertEqual(legal_frequency["metrics"]["train_action_recall"]["pon"], 1.0)
        self.assertEqual(call_linear["kind"], "call-linear-v0")
        self.assertEqual(call_linear["feature_dim"], 120)
        self.assertEqual(call_linear["feature_profile"], "v0")
        self.assertEqual(call_linear["training"]["epochs"], 25)
        self.assertEqual(call_linear["training"]["positive_class_weight"], 1.0)
        self.assertEqual(call_linear["metrics"]["train_action_recall"]["pon"], 1.0)
        self.assertEqual(call_linear["calibration"]["target"], "call")
        self.assertEqual(len(call_linear["calibration"]["thresholds"]), 21)
        self.assertIsNotNone(call_linear["calibration"]["train"]["best"])
        self.assertIsNone(call_linear["calibration"]["eval"]["best"])
        self.assertEqual(call_linear_v1["kind"], "call-linear-v1")
        self.assertGreater(call_linear_v1["feature_dim"], call_linear["feature_dim"])
        self.assertEqual(call_linear_v1["feature_profile"], "v1")
        self.assertEqual(call_linear_v1["training"]["epochs"], 25)
        self.assertEqual(call_linear_v1["training"]["positive_class_weight"], 1.0)
        self.assertEqual(call_linear_v1["calibration"]["target"], "call")
        self.assertEqual(
            call_linear_v1["calibration"]["train"]["best"]["call_recall"],
            1.0,
        )
        self.assertEqual(call_linear_v1_calibrated["kind"], "call-linear-v1")
        self.assertEqual(call_linear_v1_calibrated["feature_profile"], "v1")
        self.assertEqual(
            call_linear_v1_calibrated["policy"],
            {
                "kind": "threshold-calibrated-v0",
                "target": "call",
                "base_model": "call_linear_v1",
                "threshold": 0.4,
                "threshold_source": "tenhou-100-v0-eval-sweep",
            },
        )
        self.assertEqual(call_linear_v1_calibrated["training"]["positive_class_weight"], 1.0)
        self.assertIn("call_frequency_eval_balanced_accuracy:", stdout.getvalue())
        self.assertIn("call_legal_frequency_eval_call_recall:", stdout.getvalue())
        self.assertIn("call_linear_eval_call_recall:", stdout.getvalue())
        self.assertIn("call_linear_eval_best_threshold:", stdout.getvalue())
        self.assertIn("call_linear_v1_eval_call_recall:", stdout.getvalue())
        self.assertIn("call_linear_v1_eval_best_threshold:", stdout.getvalue())
        self.assertIn("call_linear_v1_calibrated_policy_threshold: 0.40", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())

    def test_benchmark_call_models_fast_writes_sparse_report(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark-fast.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            set(payload["models"]),
            {
                "call_frequency",
                "call_legal_frequency",
                "call_linear_v1",
                "call_linear_v1_calibrated",
            },
        )
        self.assertNotIn("call_linear", payload["models"])
        self.assertEqual(payload["example_limit"], 1)
        self.assertEqual(payload["call_examples_total"], 1)
        self.assertIn("call_linear_v1_eval_best_threshold:", stdout.getvalue())

    def test_benchmark_call_supports_zero_epochs(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark-zero-epochs.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--epochs",
                        "0",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["models"]["call_linear_v1"]["training"]["epochs"], 0)
        self.assertEqual(
            payload["models"]["call_linear_v1_calibrated"]["training"]["epochs"],
            0,
        )

    def test_benchmark_call_profiles_and_reuses_feature_cache(self) -> None:
        with TemporaryDirectory() as directory:
            cache = Path(directory) / "feature-cache.json"
            first_report = Path(directory) / "call-first.json"
            second_report = Path(directory) / "call-second.json"

            first_stdout = io.StringIO()
            with contextlib.redirect_stdout(first_stdout):
                first_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--profile-stages",
                        "--feature-cache",
                        str(cache),
                        "--report",
                        str(first_report),
                    ]
                )
            first_payload = json.loads(first_report.read_text(encoding="utf-8"))

            second_stdout = io.StringIO()
            with contextlib.redirect_stdout(second_stdout):
                second_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--profile-stages",
                        "--feature-cache",
                        str(cache),
                        "--report",
                        str(second_report),
                    ]
                )
            second_payload = json.loads(second_report.read_text(encoding="utf-8"))
            cache_payload = json.loads(cache.read_text(encoding="utf-8"))
            cache_exists = cache.exists()

        self.assertEqual(first_exit_code, 0)
        self.assertTrue(cache_exists)
        self.assertIn("stage_parse_seconds:", first_stdout.getvalue())
        self.assertIn("feature_cache_misses: v1", first_stdout.getvalue())
        self.assertEqual(first_payload["feature_cache"]["misses"], ["v1"])
        self.assertEqual(first_payload["feature_cache"]["writes"], ["v1"])
        self.assertIn("parse", first_payload["timing"])
        self.assertEqual(second_exit_code, 0)
        self.assertIn("feature_cache_hits: v1", second_stdout.getvalue())
        self.assertEqual(second_payload["feature_cache"]["hits"], ["v1"])
        self.assertEqual(second_payload["feature_cache"]["misses"], [])
        self.assertIn("example_signature", cache_payload["cache_key"])
        self.assertIn("train_signature", cache_payload["cache_key"])
        self.assertIn("eval_signature", cache_payload["cache_key"])

    def test_balanced_call_example_limit_interleaves_non_pass_and_pass_examples(self) -> None:
        tile = Tile.parse("1p")

        def example(index: int, action: Action) -> CallExample:
            return CallExample(
                round_index=0,
                event_index=index,
                call_event_index=index if action.kind != ActionKind.PASS else None,
                seat=1,
                from_seat=0,
                dealer=0,
                scores=(25000, 25000, 25000, 25000),
                discarded_tile=tile,
                legal_call_kinds=(ActionKind.PON,),
                hand_counts=(0,) * 34,
                visible_counts=(0,) * 34,
                action=action,
            )

        examples = [
            example(0, Action.pass_()),
            example(1, Action(ActionKind.PON, tile.type)),
            example(2, Action.pass_()),
            example(3, Action(ActionKind.PON, tile.type)),
            example(4, Action(ActionKind.PON, tile.type)),
            example(5, Action.pass_()),
            example(6, Action(ActionKind.PON, tile.type)),
            example(7, Action.pass_()),
        ]

        selected = _limit_call_examples(examples, 4, strategy="balanced")

        self.assertEqual(
            [example.action.kind for example in selected],
            [ActionKind.PON, ActionKind.PASS, ActionKind.PON, ActionKind.PASS],
        )

    def test_call_examples_signature_changes_when_order_changes(self) -> None:
        tile = Tile.parse("1p")

        def example(index: int, action: Action) -> CallExample:
            return CallExample(
                round_index=0,
                event_index=index,
                call_event_index=index if action.kind != ActionKind.PASS else None,
                seat=1,
                from_seat=0,
                dealer=0,
                scores=(25000, 25000, 25000, 25000),
                discarded_tile=tile,
                legal_call_kinds=(ActionKind.PON,),
                hand_counts=(0,) * 34,
                visible_counts=(0,) * 34,
                action=action,
            )

        examples = [
            example(0, Action(ActionKind.PON, tile.type)),
            example(1, Action.pass_()),
            example(2, Action(ActionKind.PON, tile.type)),
        ]

        self.assertNotEqual(
            _call_examples_signature(examples),
            _call_examples_signature(tuple(reversed(examples))),
        )

    def test_call_example_cache_serialization_round_trips(self) -> None:
        discarded_tile = Tile.parse("3p")
        consumed = (Tile.parse("1p"), Tile.parse("2p"))
        example = CallExample(
            round_index=2,
            event_index=10,
            call_event_index=11,
            seat=1,
            from_seat=0,
            dealer=3,
            scores=(27000, 24000, 26000, 23000),
            discarded_tile=discarded_tile,
            legal_call_kinds=(ActionKind.CHI, ActionKind.PON),
            hand_counts=(1, 1, 0, *([0] * 31)),
            visible_counts=(0, 1, 1, *([0] * 31)),
            action=Action(ActionKind.CHI, discarded_tile.type, consumed=consumed),
        )

        restored = _call_example_from_payload(_call_example_to_payload(example))

        self.assertEqual(restored.round_index, example.round_index)
        self.assertEqual(restored.event_index, example.event_index)
        self.assertEqual(restored.call_event_index, example.call_event_index)
        self.assertEqual(restored.seat, example.seat)
        self.assertEqual(restored.from_seat, example.from_seat)
        self.assertEqual(restored.dealer, example.dealer)
        self.assertEqual(restored.scores, example.scores)
        self.assertEqual(restored.discarded_tile, example.discarded_tile)
        self.assertEqual(restored.legal_call_kinds, example.legal_call_kinds)
        self.assertEqual(restored.hand_counts, example.hand_counts)
        self.assertEqual(restored.visible_counts, example.visible_counts)
        self.assertEqual(restored.action.kind, ActionKind.CHI)
        self.assertEqual(restored.action.tile, discarded_tile.type)
        self.assertEqual(restored.action.consumed, consumed)

    def test_benchmark_call_reuses_example_cache(self) -> None:
        with TemporaryDirectory() as directory:
            cache = Path(directory) / "call-examples-cache.json"
            first_report = Path(directory) / "call-first.json"
            second_report = Path(directory) / "call-second.json"

            first_stdout = io.StringIO()
            with contextlib.redirect_stdout(first_stdout):
                first_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--profile-stages",
                        "--report",
                        str(first_report),
                    ]
                )
            first_payload = json.loads(first_report.read_text(encoding="utf-8"))

            second_stdout = io.StringIO()
            with contextlib.redirect_stdout(second_stdout):
                second_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--profile-stages",
                        "--report",
                        str(second_report),
                    ]
                )
            second_payload = json.loads(second_report.read_text(encoding="utf-8"))
            cache_payload = json.loads(cache.read_text(encoding="utf-8"))

        self.assertEqual(first_exit_code, 0)
        self.assertEqual(second_exit_code, 0)
        self.assertIn("example_cache_hit: no", first_stdout.getvalue())
        self.assertIn("stage_parse_seconds:", first_stdout.getvalue())
        self.assertIn("example_cache_hit: yes", second_stdout.getvalue())
        self.assertNotIn("stage_parse_seconds:", second_stdout.getvalue())
        self.assertEqual(first_payload["example_cache"]["hit"], False)
        self.assertEqual(first_payload["example_cache"]["writes"], True)
        self.assertEqual(first_payload["example_cache"]["examples"], 1)
        self.assertEqual(second_payload["example_cache"]["hit"], True)
        self.assertEqual(second_payload["example_cache"]["writes"], False)
        self.assertEqual(second_payload["example_cache"]["examples"], 1)
        self.assertEqual(second_payload["discard_examples"], 4)
        self.assertEqual(second_payload["rounds"], 3)
        self.assertEqual(cache_payload["kind"], "kenjaku-call-example-cache-v0")
        self.assertEqual(len(cache_payload["examples"]), 1)
        self.assertEqual(cache_payload["game_counts"]["rounds"], 3)
        self.assertEqual(cache_payload["discard_examples"], 4)
        self.assertEqual(
            cache_payload["cache_key"]["xml_files"][0]["path"],
            str(Path("data/fixtures/tenhou/events_4p.xml").resolve()),
        )

    def test_benchmark_call_example_limit_is_applied_after_cache_load(self) -> None:
        with TemporaryDirectory() as directory:
            fixture_dir = Path(directory) / "fixtures"
            fixture_dir.mkdir()
            fixture_text = Path("data/fixtures/tenhou/events_4p.xml").read_text(
                encoding="utf-8",
            )
            (fixture_dir / "a.xml").write_text(fixture_text, encoding="utf-8")
            (fixture_dir / "b.xml").write_text(fixture_text, encoding="utf-8")
            cache = Path(directory) / "call-examples-cache.json"
            first_report = Path(directory) / "call-first.json"
            second_report = Path(directory) / "call-second.json"

            with contextlib.redirect_stdout(io.StringIO()):
                first_exit_code = main(
                    [
                        "benchmark-call",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--report",
                        str(first_report),
                    ]
                )

            second_stdout = io.StringIO()
            with contextlib.redirect_stdout(second_stdout):
                second_exit_code = main(
                    [
                        "benchmark-call",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--example-cache",
                        str(cache),
                        "--report",
                        str(second_report),
                    ]
                )
            first_payload = json.loads(first_report.read_text(encoding="utf-8"))
            second_payload = json.loads(second_report.read_text(encoding="utf-8"))

        self.assertEqual(first_exit_code, 0)
        self.assertEqual(second_exit_code, 0)
        self.assertEqual(first_payload["call_examples"], 2)
        self.assertEqual(first_payload["call_examples_total"], 2)
        self.assertIn("example_cache_hit: yes", second_stdout.getvalue())
        self.assertEqual(second_payload["call_examples"], 1)
        self.assertEqual(second_payload["call_examples_total"], 2)
        self.assertEqual(second_payload["example_cache"]["examples"], 2)

    def test_benchmark_call_example_cache_invalidates_when_file_changes(self) -> None:
        with TemporaryDirectory() as directory:
            fixture_dir = Path(directory) / "fixtures"
            fixture_dir.mkdir()
            fixture = fixture_dir / "events.xml"
            fixture.write_text(
                Path("data/fixtures/tenhou/events_4p.xml").read_text(encoding="utf-8"),
                encoding="utf-8",
            )
            cache = Path(directory) / "call-examples-cache.json"
            first_report = Path(directory) / "call-first.json"
            second_report = Path(directory) / "call-second.json"

            with contextlib.redirect_stdout(io.StringIO()):
                first_exit_code = main(
                    [
                        "benchmark-call",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--report",
                        str(first_report),
                    ]
                )

            fixture.write_text(fixture.read_text(encoding="utf-8") + "\n", encoding="utf-8")

            second_stdout = io.StringIO()
            with contextlib.redirect_stdout(second_stdout):
                second_exit_code = main(
                    [
                        "benchmark-call",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--profile-stages",
                        "--report",
                        str(second_report),
                    ]
                )
            second_payload = json.loads(second_report.read_text(encoding="utf-8"))

        self.assertEqual(first_exit_code, 0)
        self.assertEqual(second_exit_code, 0)
        self.assertIn("example_cache_hit: no", second_stdout.getvalue())
        self.assertIn("stage_parse_seconds:", second_stdout.getvalue())
        self.assertEqual(second_payload["example_cache"]["hit"], False)
        self.assertEqual(second_payload["example_cache"]["writes"], True)

    def test_benchmark_call_models_rejects_unknown_name(self) -> None:
        with self.assertRaises(SystemExit) as context:
            main(
                [
                    "benchmark-call",
                    "data/fixtures/tenhou",
                    "--models",
                    "call_linear_v9",
                ]
            )

        self.assertIn("unsupported call benchmark model", str(context.exception))

    def test_benchmark_call_can_include_weighted_variant(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--include-weighted",
                        "--call-positive-weight",
                        "3.0",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("call_linear_v1_weighted", payload["models"])
        weighted = payload["models"]["call_linear_v1_weighted"]
        self.assertEqual(weighted["kind"], "call-linear-v1")
        self.assertEqual(weighted["feature_profile"], "v1")
        self.assertEqual(weighted["training"]["positive_class_weight"], 3.0)
        self.assertNotIn("policy", weighted)
        self.assertIn("call_linear_v1_weighted_eval_call_recall:", stdout.getvalue())

    def test_benchmark_call_can_use_train_best_threshold_source(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--call-threshold-source",
                        "train-best",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        base = payload["models"]["call_linear_v1"]
        calibrated = payload["models"]["call_linear_v1_calibrated"]
        self.assertEqual(exit_code, 0)
        self.assertEqual(calibrated["policy"]["threshold_source"], "train-best")
        self.assertEqual(
            calibrated["policy"]["threshold"],
            base["calibration"]["train"]["best"]["threshold"],
        )

    def test_benchmark_riichi_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "riichi-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-riichi-benchmark-report-v0")
        self.assertEqual(payload["riichi_examples"], 1)
        self.assertEqual(
            set(payload["models"]),
            {"riichi_frequency", "riichi_linear", "riichi_linear_calibrated"},
        )
        model = payload["models"]["riichi_frequency"]
        linear = payload["models"]["riichi_linear"]
        calibrated = payload["models"]["riichi_linear_calibrated"]
        self.assertEqual(model["kind"], "riichi-frequency-v0")
        self.assertEqual(model["counts"]["riichi"], 1)
        self.assertEqual(model["metrics"]["loss_kind"], "zero_one")
        self.assertEqual(model["metrics"]["train_loss"], 0.0)
        self.assertIsNone(model["metrics"]["eval_loss"])
        self.assertEqual(model["metrics"]["train_riichi_recall"], 1.0)
        self.assertEqual(linear["kind"], "riichi-linear-v0")
        self.assertGreater(linear["feature_dim"], 0)
        self.assertEqual(linear["training"]["epochs"], 25)
        self.assertEqual(linear["training"]["positive_class_weight"], 1.0)
        self.assertEqual(linear["metrics"]["train_riichi_recall"], 1.0)
        self.assertEqual(linear["calibration"]["target"], "riichi")
        self.assertEqual(len(linear["calibration"]["thresholds"]), 21)
        self.assertIsNotNone(linear["calibration"]["train"]["best"])
        self.assertIsNone(linear["calibration"]["eval"]["best"])
        self.assertEqual(linear["calibration"]["train"]["best"]["riichi_recall"], 1.0)
        self.assertEqual(calibrated["kind"], "riichi-linear-v0")
        self.assertEqual(
            calibrated["policy"],
            {
                "kind": "threshold-calibrated-v0",
                "target": "riichi",
                "base_model": "riichi_linear",
                "threshold": 0.95,
                "threshold_source": "tenhou-100-v0-eval-sweep",
            },
        )
        self.assertEqual(calibrated["training"]["positive_class_weight"], 1.0)
        self.assertIn("riichi_frequency_eval_balanced_accuracy:", stdout.getvalue())
        self.assertIn("riichi_linear_eval_balanced_accuracy:", stdout.getvalue())
        self.assertIn("riichi_linear_eval_best_threshold:", stdout.getvalue())
        self.assertIn("riichi_linear_calibrated_policy_threshold: 0.95", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())

    def test_benchmark_riichi_can_use_train_best_threshold_source(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "riichi-benchmark.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--riichi-threshold-source",
                        "train-best",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        base = payload["models"]["riichi_linear"]
        calibrated = payload["models"]["riichi_linear_calibrated"]
        self.assertEqual(exit_code, 0)
        self.assertEqual(calibrated["policy"]["threshold_source"], "train-best")
        self.assertEqual(
            calibrated["policy"]["threshold"],
            base["calibration"]["train"]["best"]["threshold"],
        )

    def test_benchmark_riichi_example_limit_records_total_examples(self) -> None:
        with TemporaryDirectory() as directory:
            fixture_dir = Path(directory) / "fixtures"
            fixture_dir.mkdir()
            fixture_text = Path("data/fixtures/tenhou/events_4p.xml").read_text(
                encoding="utf-8",
            )
            (fixture_dir / "a.xml").write_text(fixture_text, encoding="utf-8")
            (fixture_dir / "b.xml").write_text(fixture_text, encoding="utf-8")
            report = Path(directory) / "riichi-benchmark.json"

            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-riichi",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.5",
                        "--split-seed",
                        "fixed",
                        "--example-limit",
                        "1",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["riichi_examples"], 1)
        self.assertEqual(payload["riichi_examples_total"], 2)
        self.assertEqual(payload["example_limit"], 1)

    def test_benchmark_riichi_can_include_weighted_variant(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "riichi-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--include-weighted",
                        "--riichi-positive-weight",
                        "3.0",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("riichi_linear_weighted", payload["models"])
        weighted = payload["models"]["riichi_linear_weighted"]
        self.assertEqual(weighted["kind"], "riichi-linear-v0")
        self.assertEqual(weighted["training"]["positive_class_weight"], 3.0)
        self.assertNotIn("policy", weighted)
        self.assertIn("riichi_linear_weighted_eval_riichi_recall:", stdout.getvalue())


if __name__ == "__main__":
    unittest.main()
