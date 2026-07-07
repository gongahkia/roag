from __future__ import annotations

import importlib.util
import sys
from pathlib import Path
from typing import Any

from kenjaku import __version__
from kenjaku.core import SUPPORTED_YAKU_NAMES, UNSUPPORTED_YAKU_NAMES

STATUS_KIND = "kenjaku-status-v0"
SUPPORTED_PYTHON = ">=3.11,<3.14"
MIN_PYTHON = (3, 11)
MAX_PYTHON_EXCLUSIVE = (3, 14)


def build_status_payload() -> dict[str, Any]:
    return {
        "kind": STATUS_KIND,
        "version": __version__,
        "stage": "offline research toolkit",
        "trained_model": {
            "bundled": False,
            "note": "No trained strong policy or release checkpoint is bundled with this repo.",
        },
        "product_status": "not a trained production mahjong agent",
        "environment": {
            "python": ".".join(str(part) for part in sys.version_info[:3]),
            "supported_python": SUPPORTED_PYTHON,
            "current_python_supported": _current_python_supported(),
            "pytorch_available": importlib.util.find_spec("torch") is not None,
        },
        "local_artifacts": {
            "data_raw": Path("data/raw").exists(),
            "runs": Path("runs").exists(),
            "models": Path("models").exists(),
        },
        "capabilities": {
            "implemented": {
                "tenhou_xml_parsing": True,
                "tenhou_meld_decoding": True,
                "decision_reconstruction": True,
                "discard_call_riichi_supervised_examples": True,
                "small_baseline_benchmarks": True,
                "decision_snapshot_protocol": True,
                "external_prediction_subprocess_boundary": True,
                "external_baseline_report_protocol": True,
                "external_baseline_confidence_intervals": True,
                "external_baseline_min_decision_gate": True,
                "mortal_akochan_compatible_baseline_boundary": True,
                "permission_aware_replay_intake": True,
                "permitted_replay_share_planning": True,
                "public_safe_replay_summary": True,
                "public_benchmark_dashboard": True,
                "browser_playable_demo": True,
                "self_play_sandbox": True,
                "self_play_match_sandbox": True,
                "self_play_match_trajectory_artifacts": True,
                "self_play_match_final_placement": True,
                "sandbox_legal_discard_environment": True,
                "sandbox_tsumo_action_generation": True,
                "sandbox_pending_discard_reactions": True,
                "sandbox_individual_reaction_passes": True,
                "sandbox_ron_action_generation": True,
                "sandbox_ron_priority_reactions": True,
                "sandbox_multi_ron_resolution": True,
                "sandbox_discard_furiten_ron_filter": True,
                "sandbox_temporary_furiten_ron_filter": True,
                "sandbox_riichi_furiten_ron_filter": True,
                "sandbox_riichi_declaration_action": True,
                "sandbox_post_riichi_action_restrictions": True,
                "sandbox_post_riichi_closed_kan_exceptions": True,
                "sandbox_riichi_deposit_accounting": True,
                "sandbox_honba_bonus_accounting": True,
                "sandbox_next_round_transition": True,
                "sandbox_round_wind_progression": True,
                "sandbox_game_end_final_results": True,
                "sandbox_all_last_sudden_death_progression": True,
                "sandbox_bankruptcy_game_end": True,
                "sandbox_oka_uma_final_scores": True,
                "sandbox_ippatsu_window_tracking": True,
                "sandbox_ankan_action_generation": True,
                "sandbox_ankan_application": True,
                "sandbox_kakan_action_generation": True,
                "sandbox_kakan_application": True,
                "sandbox_chankan_reaction_window": True,
                "sandbox_chankan_ron_resolution": True,
                "sandbox_ankan_kokushi_chankan": True,
                "sandbox_dead_wall_replacement_draws": True,
                "sandbox_kan_dora_indicator_metadata": True,
                "sandbox_rinshan_draw_metadata": True,
                "sandbox_endgame_yaku_timing_fixtures": True,
                "sandbox_double_riichi_yaku_metadata": True,
                "sandbox_call_action_generation": True,
                "sandbox_call_application": True,
                "sandbox_basic_yaku_win_filter": True,
                "sandbox_basic_yaku_metadata": True,
                "sandbox_expanded_yaku_legality": True,
                "sandbox_unsupported_yaku_list": True,
                "sandbox_yakuhai_seat_round_dragon_filter": True,
                "sandbox_toitoi_yaku_metadata": True,
                "sandbox_honroutou_yaku_metadata": True,
                "sandbox_terminal_reward_payloads": True,
                "self_play_reward_modes": True,
                "self_play_reward_mode_comparison": True,
                "sandbox_terminal_point_delta_metadata": True,
                "sandbox_exhaustive_draw_tenpai_noten_payments": True,
                "sandbox_nagashi_mangan_wall_exhaustion": True,
                "sandbox_nagashi_mangan_next_round_progression": True,
                "sandbox_abortive_draws": True,
                "sandbox_kyuushu_kyuuhai_abortive_draw": True,
                "sandbox_four_winds_abortive_draw": True,
                "sandbox_four_riichi_abortive_draw": True,
                "sandbox_four_kans_abortive_draw": True,
                "sandbox_triple_ron_abortive_draw": True,
                "sandbox_terminal_score_estimate_metadata": True,
                "sandbox_score_estimate_point_accounting": True,
                "sandbox_dealer_aware_win_payments": True,
                "exact_fu_han_scoring": True,
                "full_scoring_engine": True,
                "sandbox_visible_dora_score_estimates": True,
                "sandbox_ura_dora_score_estimates": True,
                "sandbox_red_dora_score_estimates": True,
                "sandbox_kazoe_yakuman_score_estimates": True,
                "sandbox_yakuman_bonus_han_suppression": True,
                "basic_winning_hand_detection": True,
                "sandbox_open_meld_win_detection": True,
                "self_play_sandbox_tsumo_termination": True,
                "sanma_static_ruleset": True,
                "self_play_sandbox_sanma_tile_set": True,
                "sandbox_sanma_initial_points": True,
                "sandbox_sanma_no_chi": True,
                "sandbox_sanma_north_guest_wind_yaku_filter": True,
                "sandbox_sanma_kita_action": True,
                "sandbox_sanma_kita_ron_reaction_window": True,
                "sandbox_sanma_kita_ron_resolution": True,
                "discard_mlp_training_command": True,
                "heuristic_defense_risk_scoring": True,
                "deal_in_estimator_training_command": True,
                "deal_in_estimator_threshold_calibration": True,
                "transformer_state_encoder_module": True,
                "transformer_behavior_cloning_training_command": True,
                "transformer_anchor_benchmark_command": True,
                "ppo_sandbox_training_command": True,
                "ppo_sandbox_policy_value_losses": True,
                "ppo_sandbox_gae_clipping_entropy": True,
                "ppo_sandbox_checkpoint_resume": True,
                "ppo_sandbox_training_curves": True,
                "ppo_sandbox_evaluation_summaries": True,
                "population_sandbox_training_command": True,
                "population_sandbox_policy_pool": True,
                "population_sandbox_opponent_sampling": True,
                "population_sandbox_promotion_criteria": True,
                "population_sandbox_matchup_metrics": True,
            },
            "not_implemented": {
                "bundled_trained_model": False,
                "bundled_trained_deal_in_probability_estimator": False,
                "transformer_policy": False,
                "rl_self_play": False,
                "sanma_ruleset": False,
                "browser_demo": False,
                "automatic_replay_posting": False,
                "full_rules_self_play_harness": False,
                "live_ladder_automation": False,
            },
            "supported_yaku": SUPPORTED_YAKU_NAMES,
            "unsupported_yaku": UNSUPPORTED_YAKU_NAMES,
        },
    }


def format_status_text(payload: dict[str, Any]) -> str:
    environment = payload["environment"]
    local_artifacts = payload["local_artifacts"]
    capabilities = payload["capabilities"]
    lines = [
        f"kenjaku: {payload['version']}",
        f"stage: {payload['stage']}",
        "trained_model: not bundled",
        "product_status: research toolkit, not a trained production agent",
        f"python: {environment['python']}",
        f"supported_python: {environment['supported_python']}",
        f"current_python_supported: {_format_bool(environment['current_python_supported'])}",
        f"pytorch: {_format_bool(environment['pytorch_available'])}",
        f"local_raw_data: {_format_bool(local_artifacts['data_raw'])}",
        f"local_reports: {_format_bool(local_artifacts['runs'])}",
        f"local_models: {_format_bool(local_artifacts['models'])}",
        "implemented:",
    ]
    lines.extend(
        f"  {name}: {_format_bool(enabled)}"
        for name, enabled in capabilities["implemented"].items()
    )
    lines.append("not_implemented:")
    lines.extend(
        f"  {name}: {_format_bool(enabled)}"
        for name, enabled in capabilities["not_implemented"].items()
    )
    lines.append("unsupported_yaku:")
    lines.extend(f"  {name}" for name in capabilities["unsupported_yaku"])
    return "\n".join(lines)


def _format_bool(value: bool) -> str:
    return "yes" if value else "no"


def _current_python_supported() -> bool:
    current = sys.version_info[:2]
    return MIN_PYTHON <= current < MAX_PYTHON_EXCLUSIVE
