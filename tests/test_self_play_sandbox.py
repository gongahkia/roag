from __future__ import annotations

import unittest

from kenjaku.simulation import (
    SELF_PLAY_MATCH_REPORT_KIND,
    SELF_PLAY_SANDBOX_REPORT_KIND,
    format_self_play_match_report,
    format_self_play_sandbox_report,
    run_self_play_match_sandbox,
    run_self_play_sandbox,
)


class SelfPlaySandboxTests(unittest.TestCase):
    def test_run_self_play_match_sandbox_completes_four_player_match(self) -> None:
        report = run_self_play_match_sandbox(
            games=1,
            max_rounds=12,
            max_turns_per_round=512,
            seed="smoke",
            ron_policy="pass",
            include_trajectories=True,
        )
        game = report["game_summaries"][0]
        final_result = game["final_result"]
        trajectory = game["trajectory"]

        self.assertEqual(report["kind"], SELF_PLAY_MATCH_REPORT_KIND)
        self.assertEqual(report["games"], 1)
        self.assertEqual(report["completed_games"], 1)
        self.assertEqual(report["ruleset"], "tenhou-4p")
        self.assertEqual(report["players"], 4)
        self.assertEqual(report["policies"]["discard"], "drawn")
        self.assertEqual(report["policies"]["ron"], "pass")
        self.assertEqual(report["final_summary"]["completed_games"], 1)
        self.assertTrue(report["capabilities"]["multi_round_matches"])
        self.assertTrue(report["capabilities"]["discard_policy"])
        self.assertTrue(report["capabilities"]["call_policy"])
        self.assertTrue(report["capabilities"]["riichi_policy"])
        self.assertTrue(report["capabilities"]["kan_policy"])
        self.assertTrue(report["capabilities"]["kita_policy"])
        self.assertTrue(report["capabilities"]["ron_policy"])
        self.assertTrue(report["capabilities"]["pass_policy"])
        self.assertTrue(report["capabilities"]["trajectory_states"])
        self.assertTrue(report["capabilities"]["trajectory_legal_actions"])
        self.assertTrue(report["capabilities"]["trajectory_rewards"])
        self.assertTrue(report["capabilities"]["final_placement"])
        self.assertTrue(game["completed"])
        self.assertEqual(game["rounds"], 12)
        self.assertGreater(game["decisions"], 0)
        self.assertIsNotNone(final_result)
        self.assertEqual(len(final_result["points"]), 4)
        self.assertEqual(len(final_result["placement"]), 4)
        self.assertEqual(len(final_result["ranks"]), 4)
        self.assertEqual(len(final_result["scores"]), 4)
        self.assertEqual(game["final_scores"], final_result["scores"])
        self.assertEqual(game["final_placement"], final_result["placement"])
        self.assertEqual(trajectory[-1]["rewards"], final_result["scores"])
        self.assertIn("state", trajectory[0])
        self.assertIn("legal_actions", trajectory[0])
        self.assertIn("chosen_action", trajectory[0])
        self.assertIn("rewards", trajectory[0])
        self.assertEqual(
            {entry["decision_type"] for entry in trajectory},
            {"discard", "pass"},
        )

    def test_run_self_play_match_sandbox_supports_sanma_final_results(self) -> None:
        report = run_self_play_match_sandbox(
            games=1,
            max_rounds=9,
            max_turns_per_round=512,
            seed="sanma-smoke",
            ruleset="tenhou-3p",
            ron_policy="pass",
        )
        final_result = report["game_summaries"][0]["final_result"]

        self.assertEqual(report["ruleset"], "tenhou-3p")
        self.assertEqual(report["players"], 3)
        self.assertEqual(report["completed_games"], 1)
        self.assertTrue(report["capabilities"]["kita_policy"])
        self.assertIsNotNone(final_result)
        self.assertEqual(final_result["return_points"], 40000)
        self.assertEqual(final_result["oka_points"], 15000)
        self.assertEqual(final_result["uma_by_rank"], [20.0, 0.0, -20.0])
        self.assertEqual(len(final_result["placement"]), 3)
        self.assertEqual(len(final_result["scores"]), 3)

    def test_self_play_match_text_summary_marks_policies_and_final_scores(self) -> None:
        report = run_self_play_match_sandbox(
            games=1,
            max_rounds=12,
            max_turns_per_round=512,
            seed="summary-match",
            ron_policy="pass",
        )

        text = format_self_play_match_report(report)

        self.assertIn("games: 1", text)
        self.assertIn("completed_games: 1", text)
        self.assertIn("ruleset: tenhou-4p", text)
        self.assertIn("discard=drawn", text)
        self.assertIn("ron=pass", text)
        self.assertIn("final_reasons:", text)
        self.assertIn("average_final_scores:", text)
        self.assertIn("multi_round_matches: yes", text)
        self.assertIn("final_placement: yes", text)

    def test_run_self_play_sandbox_is_deterministic(self) -> None:
        first = run_self_play_sandbox(
            episodes=2,
            max_turns=8,
            seed="fixed",
            policy="random",
            include_trajectories=True,
        )
        second = run_self_play_sandbox(
            episodes=2,
            max_turns=8,
            seed="fixed",
            policy="random",
            include_trajectories=True,
        )

        self.assertEqual(first, second)
        self.assertEqual(first["kind"], SELF_PLAY_SANDBOX_REPORT_KIND)
        self.assertEqual(first["episodes"], 2)
        self.assertEqual(first["ruleset"], "tenhou-4p")
        self.assertEqual(first["players"], 4)
        self.assertEqual(first["reward_mode"], "terminal")
        self.assertEqual(
            first["reward_modes"],
            ["terminal", "point-delta", "normalized-point-delta", "placement-delta"],
        )
        self.assertEqual(first["reward_summary"]["mode"], "terminal")
        self.assertEqual(
            set(first["reward_summaries"]),
            {"terminal", "point-delta", "normalized-point-delta", "placement-delta"},
        )
        self.assertEqual(first["outcome_summary"]["draw_outcomes"], {"max_turns": 2})
        self.assertEqual(first["decisions"], 16)
        self.assertEqual(first["terminal_reasons"], {"max_turns": 2})
        self.assertEqual(first["seat_decisions"], [4, 4, 4, 4])
        self.assertTrue(first["capabilities"]["draw_discard_loop"])
        self.assertTrue(first["capabilities"]["multi_agent_turn_rotation"])
        self.assertFalse(first["capabilities"]["basic_closed_hand_win_detection"])
        self.assertFalse(first["capabilities"]["full_riichi_rules"])
        self.assertTrue(first["capabilities"]["riichi_declaration_action"])
        self.assertTrue(first["capabilities"]["post_riichi_action_restrictions"])
        self.assertTrue(first["capabilities"]["post_riichi_closed_kan_exceptions"])
        self.assertTrue(first["capabilities"]["riichi_deposit_accounting"])
        self.assertTrue(first["capabilities"]["honba_bonus_accounting"])
        self.assertTrue(first["capabilities"]["next_round_transition"])
        self.assertTrue(first["capabilities"]["round_wind_progression"])
        self.assertTrue(first["capabilities"]["ippatsu_window_tracking"])
        self.assertTrue(first["capabilities"]["closed_kan_actions"])
        self.assertTrue(first["capabilities"]["added_kan_actions"])
        self.assertTrue(first["capabilities"]["chankan_reaction_windows"])
        self.assertTrue(first["capabilities"]["chankan_ron_resolution"])
        self.assertTrue(first["capabilities"]["ankan_kokushi_chankan"])
        self.assertTrue(first["capabilities"]["dead_wall_replacement_draws"])
        self.assertTrue(first["capabilities"]["kan_dora_indicator_metadata"])
        self.assertTrue(first["capabilities"]["rinshan_draw_metadata"])
        self.assertTrue(first["capabilities"]["endgame_yaku_timing_fixtures"])
        self.assertFalse(first["capabilities"]["sanma_initial_points"])
        self.assertFalse(first["capabilities"]["sanma_no_chi"])
        self.assertFalse(first["capabilities"]["sanma_north_guest_wind"])
        self.assertFalse(first["capabilities"]["sanma_kita_action"])
        self.assertFalse(first["capabilities"]["kita_policy"])
        self.assertFalse(first["capabilities"]["kita_ron_reaction_windows"])
        self.assertFalse(first["capabilities"]["kita_ron_resolution"])
        self.assertFalse(first["capabilities"]["kan_policy"])
        self.assertFalse(first["capabilities"]["chankan_policy"])
        self.assertFalse(first["capabilities"]["call_policy"])
        self.assertFalse(first["capabilities"]["ron_policy"])
        self.assertTrue(first["capabilities"]["open_hand_win_detection"])
        self.assertTrue(first["capabilities"]["reaction_windows_auto_passed"])
        self.assertTrue(first["capabilities"]["discard_furiten_ron_filter"])
        self.assertTrue(first["capabilities"]["temporary_furiten_ron_filter"])
        self.assertTrue(first["capabilities"]["riichi_furiten_ron_filter"])
        self.assertTrue(first["capabilities"]["basic_yaku_win_filter"])
        self.assertTrue(first["capabilities"]["basic_yaku_metadata"])
        self.assertTrue(first["capabilities"]["expanded_yaku_legality"])
        self.assertTrue(first["capabilities"]["unsupported_yaku_list"])
        self.assertTrue(first["capabilities"]["yakuhai_seat_round_dragon_filter"])
        self.assertTrue(first["capabilities"]["toitoi_yaku_metadata"])
        self.assertTrue(first["capabilities"]["honroutou_yaku_metadata"])
        self.assertTrue(first["capabilities"]["terminal_rewards"])
        self.assertTrue(first["capabilities"]["selectable_reward_modes"])
        self.assertTrue(first["capabilities"]["reward_mode_comparison"])
        self.assertTrue(first["capabilities"]["point_delta_reward_mode"])
        self.assertTrue(first["capabilities"]["normalized_point_delta_reward_mode"])
        self.assertTrue(first["capabilities"]["placement_delta_reward_mode"])
        self.assertTrue(first["capabilities"]["terminal_point_delta_metadata"])
        self.assertTrue(first["capabilities"]["exhaustive_draw_tenpai_noten_payments"])
        self.assertTrue(first["capabilities"]["abortive_draws"])
        self.assertTrue(first["capabilities"]["kyuushu_kyuuhai_abortive_draw"])
        self.assertTrue(first["capabilities"]["four_winds_abortive_draw"])
        self.assertTrue(first["capabilities"]["four_riichi_abortive_draw"])
        self.assertTrue(first["capabilities"]["four_kans_abortive_draw"])
        self.assertTrue(first["capabilities"]["triple_ron_abortive_draw"])
        self.assertTrue(first["capabilities"]["dealer_aware_win_payments"])
        self.assertTrue(first["capabilities"]["exact_fu_han_scoring"])
        self.assertTrue(first["capabilities"]["full_scoring_engine"])
        self.assertTrue(first["capabilities"]["visible_dora_score_estimates"])
        self.assertTrue(first["capabilities"]["ura_dora_score_estimates"])
        self.assertTrue(first["capabilities"]["red_dora_score_estimates"])
        self.assertEqual(first["episode_summaries"][0]["terminal_rewards"], [0.0, 0.0, 0.0, 0.0])
        self.assertEqual(
            first["episode_summaries"][0]["terminal_point_deltas"],
            [0, 0, 0, 0],
        )
        self.assertEqual(first["episode_summaries"][0]["raw_point_delta"], [0, 0, 0, 0])
        self.assertEqual(
            first["episode_summaries"][0]["normalized_point_delta"],
            [0.0, 0.0, 0.0, 0.0],
        )
        self.assertEqual(
            first["episode_summaries"][0]["placement_delta"],
            [0.0, 0.0, 0.0, 0.0],
        )
        self.assertEqual(first["episode_summaries"][0]["win_events"], [0, 0, 0, 0])
        self.assertEqual(first["episode_summaries"][0]["deal_in_events"], [0, 0, 0, 0])
        self.assertEqual(first["episode_summaries"][0]["draw_outcome"], "max_turns")
        self.assertEqual(
            set(first["episode_summaries"][0]["reward_vectors"]),
            {"terminal", "point-delta", "normalized-point-delta", "placement-delta"},
        )
        self.assertEqual(
            first["episode_summaries"][0]["selected_rewards"],
            first["episode_summaries"][0]["reward_vectors"]["terminal"],
        )
        self.assertEqual(
            first["episode_summaries"][0]["final_points"],
            [25000, 25000, 25000, 25000],
        )
        self.assertEqual(first["episode_summaries"][0]["riichi_sticks"], 0)
        self.assertEqual(first["episode_summaries"][0]["honba"], 0)
        self.assertEqual(first["episode_summaries"][0]["dealer_seat"], 0)
        self.assertEqual(first["episode_summaries"][0]["round_wind"], "E")
        self.assertEqual(first["episode_summaries"][0]["ippatsu_seats"], [])
        self.assertEqual(first["episode_summaries"][0]["winning_ippatsu_seats"], [])
        self.assertFalse(first["episode_summaries"][0]["rinshan_draw"])
        self.assertEqual(first["episode_summaries"][0]["winning_rinshan_seats"], [])
        self.assertEqual(first["episode_summaries"][0]["kita_tiles"], [[], [], [], []])
        self.assertEqual(first["episode_summaries"][0]["kita_counts"], [0, 0, 0, 0])
        self.assertEqual(first["episode_summaries"][0]["dead_wall_remaining"], 14)
        self.assertEqual(len(first["episode_summaries"][0]["dora_indicators"]), 1)
        self.assertIn("trajectory", first["episode_summaries"][0])

    def test_sanma_ruleset_uses_three_seats_and_excluded_tiles(self) -> None:
        report = run_self_play_sandbox(
            episodes=1,
            max_turns=9,
            seed="sanma",
            policy="drawn",
            ruleset="tenhou-3p",
            include_trajectories=True,
        )

        discarded_tiles = set(report["discard_counts"])

        self.assertEqual(report["ruleset"], "tenhou-3p")
        self.assertEqual(report["players"], 3)
        self.assertEqual(report["decisions"], 9)
        self.assertEqual(report["seat_decisions"], [3, 3, 3])
        self.assertTrue(report["capabilities"]["static_sanma_tile_set"])
        self.assertTrue(report["capabilities"]["sanma_initial_points"])
        self.assertTrue(report["capabilities"]["sanma_no_chi"])
        self.assertTrue(report["capabilities"]["sanma_north_guest_wind"])
        self.assertTrue(report["capabilities"]["sanma_kita_action"])
        self.assertTrue(report["capabilities"]["kita_ron_reaction_windows"])
        self.assertTrue(report["capabilities"]["kita_ron_resolution"])
        self.assertFalse(any(tile in discarded_tiles for tile in _excluded_sanma_manzu()))
        self.assertEqual(report["episode_summaries"][0]["final_points"], [35000, 35000, 35000])
        self.assertEqual(len(report["episode_summaries"][0]["kita_tiles"]), 3)
        self.assertEqual(len(report["episode_summaries"][0]["kita_counts"]), 3)

    def test_frequency_policy_updates_counts(self) -> None:
        report = run_self_play_sandbox(
            episodes=3,
            max_turns=4,
            seed="frequency",
            policy="frequency",
        )

        learned_counts = report["policy"]["learned_discard_counts"]
        self.assertEqual(sum(learned_counts.values()), report["decisions"])
        self.assertEqual(report["policy"]["updates"], report["decisions"])

    def test_reward_mode_selects_projection_and_reports_comparable_summaries(self) -> None:
        report = run_self_play_sandbox(
            episodes=2,
            max_turns=4,
            seed="reward-mode",
            policy="drawn",
            reward_mode="placement-delta",
        )

        episode = report["episode_summaries"][0]

        self.assertEqual(report["reward_mode"], "placement-delta")
        self.assertEqual(report["reward_summary"]["mode"], "placement-delta")
        self.assertEqual(
            set(report["reward_summaries"]),
            {"terminal", "point-delta", "normalized-point-delta", "placement-delta"},
        )
        self.assertEqual(
            episode["selected_rewards"],
            episode["reward_vectors"]["placement-delta"],
        )
        self.assertEqual(len(report["reward_summaries"]), 4)
        self.assertEqual(report["outcome_summary"]["draw_outcomes"], {"max_turns": 2})
        self.assertEqual(report["outcome_summary"]["win_events_by_seat"], [0, 0, 0, 0])
        self.assertEqual(report["outcome_summary"]["deal_in_events_by_seat"], [0, 0, 0, 0])
        self.assertEqual(
            report["outcome_summary"]["raw_point_delta_sum_by_seat"],
            [0, 0, 0, 0],
        )
        self.assertEqual(
            report["outcome_summary"]["normalized_point_delta_sum_by_seat"],
            [0.0, 0.0, 0.0, 0.0],
        )
        self.assertEqual(
            report["outcome_summary"]["placement_delta_sum_by_seat"],
            [0.0, 0.0, 0.0, 0.0],
        )

    def test_text_summary_marks_missing_full_rules(self) -> None:
        report = run_self_play_sandbox(
            episodes=1,
            max_turns=4,
            seed="summary",
            policy="drawn",
            stop_on_tsumo=True,
        )

        text = format_self_play_sandbox_report(report)

        self.assertIn("episodes: 1", text)
        self.assertIn("ruleset: tenhou-4p", text)
        self.assertIn("policy: drawn-discard-sandbox-v0", text)
        self.assertIn("reward_mode: terminal", text)
        self.assertIn(
            "reward_modes: terminal, point-delta, normalized-point-delta, placement-delta",
            text,
        )
        self.assertIn("reward_summary: 0=0.000 1=0.000 2=0.000 3=0.000", text)
        self.assertIn("draw_outcomes: max_turns=1", text)
        self.assertIn("stop_on_tsumo: yes", text)
        self.assertIn("basic_closed_hand_win_detection: yes", text)
        self.assertIn("draw_discard_loop: yes", text)
        self.assertIn("riichi_declaration_action: yes", text)
        self.assertIn("post_riichi_action_restrictions: yes", text)
        self.assertIn("post_riichi_closed_kan_exceptions: yes", text)
        self.assertIn("riichi_deposit_accounting: yes", text)
        self.assertIn("honba_bonus_accounting: yes", text)
        self.assertIn("next_round_transition: yes", text)
        self.assertIn("round_wind_progression: yes", text)
        self.assertIn("ippatsu_window_tracking: yes", text)
        self.assertIn("closed_kan_actions: yes", text)
        self.assertIn("added_kan_actions: yes", text)
        self.assertIn("chankan_reaction_windows: yes", text)
        self.assertIn("chankan_ron_resolution: yes", text)
        self.assertIn("ankan_kokushi_chankan: yes", text)
        self.assertIn("dead_wall_replacement_draws: yes", text)
        self.assertIn("kan_dora_indicator_metadata: yes", text)
        self.assertIn("rinshan_draw_metadata: yes", text)
        self.assertIn("sanma_initial_points: no", text)
        self.assertIn("sanma_no_chi: no", text)
        self.assertIn("sanma_north_guest_wind: no", text)
        self.assertIn("sanma_kita_action: no", text)
        self.assertIn("kita_policy: no", text)
        self.assertIn("kita_ron_reaction_windows: no", text)
        self.assertIn("kita_ron_resolution: no", text)
        self.assertIn("kan_policy: no", text)
        self.assertIn("chankan_policy: no", text)
        self.assertIn("call_policy: no", text)
        self.assertIn("ron_policy: no", text)
        self.assertIn("open_hand_win_detection: yes", text)
        self.assertIn("reaction_windows_auto_passed: yes", text)
        self.assertIn("discard_furiten_ron_filter: yes", text)
        self.assertIn("temporary_furiten_ron_filter: yes", text)
        self.assertIn("riichi_furiten_ron_filter: yes", text)
        self.assertIn("basic_yaku_win_filter: yes", text)
        self.assertIn("basic_yaku_metadata: yes", text)
        self.assertIn("expanded_yaku_legality: yes", text)
        self.assertIn("unsupported_yaku_list: yes", text)
        self.assertIn("yakuhai_seat_round_dragon_filter: yes", text)
        self.assertIn("toitoi_yaku_metadata: yes", text)
        self.assertIn("honroutou_yaku_metadata: yes", text)
        self.assertIn("terminal_rewards: yes", text)
        self.assertIn("selectable_reward_modes: yes", text)
        self.assertIn("reward_mode_comparison: yes", text)
        self.assertIn("point_delta_reward_mode: yes", text)
        self.assertIn("normalized_point_delta_reward_mode: yes", text)
        self.assertIn("placement_delta_reward_mode: yes", text)
        self.assertIn("terminal_point_delta_metadata: yes", text)
        self.assertIn("exhaustive_draw_tenpai_noten_payments: yes", text)
        self.assertIn("dealer_aware_win_payments: yes", text)
        self.assertIn("visible_dora_score_estimates: yes", text)
        self.assertIn("ura_dora_score_estimates: yes", text)
        self.assertIn("red_dora_score_estimates: yes", text)
        self.assertIn("exact_fu_han_scoring: yes", text)
        self.assertIn("full_scoring_engine: yes", text)
        self.assertIn("scoring: yes", text)
        self.assertIn("ppo: no", text)

    def test_stop_on_tsumo_adds_terminal_metadata(self) -> None:
        report = run_self_play_sandbox(
            episodes=1,
            max_turns=8,
            seed="tsumo-metadata",
            policy="drawn",
            stop_on_tsumo=True,
        )
        episode = report["episode_summaries"][0]

        self.assertTrue(report["stop_on_tsumo"])
        self.assertIn(episode["terminal_reason"], {"max_turns", "wall_exhausted", "tsumo"})
        self.assertIn("winner_seat", episode)
        self.assertIn("winner_seats", episode)
        self.assertIn("winning_tile", episode)
        self.assertIn("winning_shapes", episode)
        self.assertIn("winning_shapes_by_seat", episode)
        self.assertIn("winning_yaku", episode)
        self.assertIn("winning_yaku_by_seat", episode)
        self.assertIn("terminal_rewards", episode)
        self.assertIn("terminal_point_deltas", episode)
        self.assertIn("raw_point_delta", episode)
        self.assertIn("normalized_point_delta", episode)
        self.assertIn("placement_delta", episode)
        self.assertIn("win_events", episode)
        self.assertIn("deal_in_events", episode)
        self.assertIn("draw_outcome", episode)
        self.assertIn("reward_vectors", episode)
        self.assertIn("selected_rewards", episode)
        self.assertIn("exhaustive_draw_tenpai_seats", episode)
        self.assertIn("exhaustive_draw_noten_seats", episode)
        self.assertIn("final_points", episode)
        self.assertIn("riichi_sticks", episode)
        self.assertIn("honba", episode)
        self.assertIn("dealer_seat", episode)
        self.assertIn("round_wind", episode)
        self.assertIn("ippatsu_seats", episode)
        self.assertIn("winning_ippatsu_seats", episode)
        self.assertIn("rinshan_draw", episode)
        self.assertIn("winning_rinshan_seats", episode)
        self.assertIn("kita_tiles", episode)
        self.assertIn("kita_counts", episode)
        self.assertIn("dead_wall_remaining", episode)
        self.assertIn("dora_indicators", episode)
        self.assertEqual(len(episode["terminal_rewards"]), report["players"])
        self.assertEqual(len(episode["terminal_point_deltas"]), report["players"])
        self.assertEqual(len(episode["raw_point_delta"]), report["players"])
        self.assertEqual(len(episode["normalized_point_delta"]), report["players"])
        self.assertEqual(len(episode["placement_delta"]), report["players"])
        self.assertEqual(len(episode["win_events"]), report["players"])
        self.assertEqual(len(episode["deal_in_events"]), report["players"])
        self.assertEqual(len(episode["selected_rewards"]), report["players"])
        self.assertIsInstance(episode["exhaustive_draw_tenpai_seats"], list)
        self.assertIsInstance(episode["exhaustive_draw_noten_seats"], list)
        self.assertEqual(len(episode["final_points"]), report["players"])
        self.assertEqual(len(episode["kita_tiles"]), report["players"])
        self.assertEqual(len(episode["kita_counts"]), report["players"])
        if episode["terminal_reason"] == "tsumo":
            self.assertIsInstance(episode["winner_seat"], int)
            self.assertEqual(episode["winner_seats"], [episode["winner_seat"]])
            self.assertIsInstance(episode["winning_tile"], str)
            self.assertTrue(episode["winning_shapes"])
            self.assertTrue(episode["winning_shapes_by_seat"])
            self.assertTrue(episode["winning_yaku"])
            self.assertTrue(episode["winning_yaku_by_seat"])
            self.assertAlmostEqual(sum(episode["terminal_rewards"]), 0.0)

    def test_invalid_arguments_raise_value_error(self) -> None:
        with self.assertRaisesRegex(ValueError, "episodes must be positive"):
            run_self_play_sandbox(episodes=0, max_turns=4, seed="bad")
        with self.assertRaisesRegex(ValueError, "max_turns must be positive"):
            run_self_play_sandbox(episodes=1, max_turns=0, seed="bad")
        with self.assertRaisesRegex(ValueError, "unsupported self-play sandbox policy"):
            run_self_play_sandbox(episodes=1, max_turns=4, seed="bad", policy="bad")
        with self.assertRaisesRegex(ValueError, "unsupported .*ruleset"):
            run_self_play_sandbox(episodes=1, max_turns=4, seed="bad", ruleset="bad")
        with self.assertRaisesRegex(ValueError, "games must be positive"):
            run_self_play_match_sandbox(
                games=0,
                max_rounds=1,
                max_turns_per_round=1,
                seed="bad",
            )
        with self.assertRaisesRegex(ValueError, "max rounds must be positive"):
            run_self_play_match_sandbox(
                games=1,
                max_rounds=0,
                max_turns_per_round=1,
                seed="bad",
            )
        with self.assertRaisesRegex(ValueError, "max turns per round must be positive"):
            run_self_play_match_sandbox(
                games=1,
                max_rounds=1,
                max_turns_per_round=0,
                seed="bad",
            )
        with self.assertRaisesRegex(ValueError, "unsupported match discard policy"):
            run_self_play_match_sandbox(
                games=1,
                max_rounds=1,
                max_turns_per_round=1,
                seed="bad",
                discard_policy="bad",
            )
        with self.assertRaisesRegex(ValueError, "unsupported match ron policy"):
            run_self_play_match_sandbox(
                games=1,
                max_rounds=1,
                max_turns_per_round=1,
                seed="bad",
                ron_policy="bad",
            )


def _excluded_sanma_manzu() -> tuple[str, ...]:
    return tuple(f"{rank}m" for rank in range(2, 9))


if __name__ == "__main__":
    unittest.main()
