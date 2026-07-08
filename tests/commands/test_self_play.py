from __future__ import annotations

from tests.commands.conftest import (
    CliCommandTests,
    Path,
    TemporaryDirectory,
    contextlib,
    io,
    json,
    main,
)


class SelfPlayCommandTests(CliCommandTests):
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
        self.assertIn("expanded_yaku_legality: yes", text_stdout.getvalue())
        self.assertIn("unsupported_yaku_list: yes", text_stdout.getvalue())
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
        self.assertIn("exact_fu_han_scoring: yes", text_stdout.getvalue())
        self.assertIn("full_scoring_engine: yes", text_stdout.getvalue())
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
        self.assertTrue(report_payload["capabilities"]["expanded_yaku_legality"])
        self.assertTrue(report_payload["capabilities"]["unsupported_yaku_list"])
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
        self.assertTrue(report_payload["capabilities"]["nagashi_mangan_next_round_progression"])
        self.assertTrue(report_payload["capabilities"]["abortive_draws"])
        self.assertTrue(report_payload["capabilities"]["kyuushu_kyuuhai_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["four_winds_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["four_riichi_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["four_kans_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["triple_ron_abortive_draw"])
        self.assertTrue(report_payload["capabilities"]["dealer_aware_win_payments"])
        self.assertTrue(report_payload["capabilities"]["exact_fu_han_scoring"])
        self.assertTrue(report_payload["capabilities"]["full_scoring_engine"])
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
            trajectory = Path(directory) / "self-play-match-trajectory.jsonl"
            viewer = Path(directory) / "viewer.html"

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
                        "--trajectory-jsonl",
                        str(trajectory),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))
            trajectory_rows = [
                json.loads(line) for line in trajectory.read_text(encoding="utf-8").splitlines()
            ]

            viewer_stdout = io.StringIO()
            with contextlib.redirect_stdout(viewer_stdout):
                viewer_exit_code = main(
                    [
                        "replay-viewer",
                        str(trajectory),
                        "--output",
                        str(viewer),
                    ]
                )
            viewer_html = viewer.read_text(encoding="utf-8")

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
        self.assertTrue(report_payload["capabilities"]["trajectory_states"])
        self.assertTrue(report_payload["capabilities"]["final_placement"])
        self.assertIsNotNone(report_payload["game_summaries"][0]["final_result"])
        self.assertGreater(len(trajectory_rows), 0)
        self.assertEqual(
            trajectory_rows[0]["kind"],
            "kenjaku-self-play-match-trajectory-row-v0",
        )
        self.assertIn("hands", trajectory_rows[0]["state"])
        self.assertIn("trajectory_path:", text_stdout.getvalue())
        self.assertIn("trajectory_rows:", text_stdout.getvalue())
        self.assertEqual(viewer_exit_code, 0)
        self.assertIn("trajectory_rows:", viewer_stdout.getvalue())
        self.assertIn("Kenjaku Self-Play Replay Viewer", viewer_html)
        self.assertIn('id="timeline"', viewer_html)
        self.assertIn('id="hand-toggles"', viewer_html)
        self.assertIn("window.KenjakuReplayViewer", viewer_html)
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
