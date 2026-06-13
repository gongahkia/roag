from __future__ import annotations

from collections import Counter
from dataclasses import dataclass
from hashlib import blake2b
from typing import Any

from kenjaku.core import Action, RuleSet, Tile, TileType
from kenjaku.simulation.environment import (
    SANDBOX_RULESETS,
    SandboxEnvironmentState,
    apply_discard_action,
    apply_reaction_pass_action,
    draw_for_current_seat,
    initial_sandbox_environment,
    legal_discard_actions,
    resolve_sandbox_ruleset,
)

SELF_PLAY_SANDBOX_REPORT_KIND = "kenjaku-self-play-sandbox-report-v0"
SELF_PLAY_SANDBOX_POLICIES = ("random", "drawn", "frequency")
SELF_PLAY_SANDBOX_RULESETS = SANDBOX_RULESETS
SELF_PLAY_SANDBOX_REWARD_MODES = (
    "terminal",
    "point-delta",
    "normalized-point-delta",
    "placement-delta",
)


@dataclass(frozen=True, slots=True)
class SelfPlaySandboxDecision:
    turn: int
    seat: int
    draw: Tile
    discard: Tile
    hand_size_after_discard: int

    def to_payload(self) -> dict[str, Any]:
        return {
            "turn": self.turn,
            "seat": self.seat,
            "draw": self.draw.notation,
            "discard": self.discard.notation,
            "hand_size_after_discard": self.hand_size_after_discard,
        }


def run_self_play_sandbox(
    *,
    episodes: int,
    max_turns: int,
    seed: str,
    policy: str = "random",
    ruleset: str = "tenhou-4p",
    reward_mode: str = "terminal",
    include_trajectories: bool = False,
    stop_on_tsumo: bool = False,
) -> dict[str, Any]:
    if episodes <= 0:
        raise ValueError("episodes must be positive")
    if max_turns <= 0:
        raise ValueError("max_turns must be positive")
    if policy not in SELF_PLAY_SANDBOX_POLICIES:
        raise ValueError("unsupported self-play sandbox policy: " + policy)
    if reward_mode not in SELF_PLAY_SANDBOX_REWARD_MODES:
        raise ValueError("unsupported self-play sandbox reward mode: " + reward_mode)
    rules = resolve_sandbox_ruleset(ruleset)

    policy_counts = [0] * 34
    episode_payloads: list[dict[str, Any]] = []
    terminal_reasons: Counter[str] = Counter()
    discard_counts: Counter[str] = Counter()
    seat_decisions = [0] * rules.players
    total_decisions = 0

    for episode_index in range(episodes):
        episode_seed = _episode_seed(seed, episode_index)
        episode = _simulate_episode(
            episode_index=episode_index,
            seed=episode_seed,
            max_turns=max_turns,
            policy=policy,
            reward_mode=reward_mode,
            rules=rules,
            policy_counts=policy_counts,
            include_trajectory=include_trajectories,
            stop_on_tsumo=stop_on_tsumo,
        )
        terminal_reasons[episode["terminal_reason"]] += 1
        total_decisions += int(episode["decisions"])
        for seat, count in enumerate(episode["seat_decisions"]):
            seat_decisions[seat] += int(count)
        for tile, count in episode["discard_counts"].items():
            discard_counts[str(tile)] += int(count)
        episode_payloads.append(episode)
    reward_summaries = _reward_summaries(episode_payloads, players=rules.players)

    return {
        "kind": SELF_PLAY_SANDBOX_REPORT_KIND,
        "seed": seed,
        "policy": {
            "kind": f"{policy}-discard-sandbox-v0",
            "name": policy,
            "updates": total_decisions,
            "learned_discard_counts": _tile_count_payload(policy_counts),
        },
        "episodes": episodes,
        "max_turns": max_turns,
        "stop_on_tsumo": stop_on_tsumo,
        "ruleset": rules.name,
        "players": rules.players,
        "reward_mode": reward_mode,
        "reward_modes": list(SELF_PLAY_SANDBOX_REWARD_MODES),
        "reward_summary": reward_summaries[reward_mode],
        "reward_summaries": reward_summaries,
        "outcome_summary": _outcome_summary(episode_payloads, players=rules.players),
        "decisions": total_decisions,
        "average_decisions": total_decisions / episodes,
        "terminal_reasons": dict(sorted(terminal_reasons.items())),
        "seat_decisions": seat_decisions,
        "discard_counts": dict(sorted(discard_counts.items())),
        "episode_summaries": episode_payloads,
        "capabilities": {
            "draw_discard_loop": True,
            "multi_agent_turn_rotation": True,
            "policy_updates": True,
            "basic_closed_hand_win_detection": stop_on_tsumo,
            "static_sanma_tile_set": rules.players == 3,
            "sanma_initial_points": rules.players == 3,
            "sanma_no_chi": rules.players == 3,
            "sanma_north_guest_wind": rules.players == 3,
            "sanma_kita_action": rules.players == 3,
            "full_riichi_rules": False,
            "riichi_declaration_action": True,
            "post_riichi_action_restrictions": True,
            "post_riichi_closed_kan_exceptions": True,
            "riichi_deposit_accounting": True,
            "honba_bonus_accounting": True,
            "next_round_transition": True,
            "round_wind_progression": True,
            "ippatsu_window_tracking": True,
            "closed_kan_actions": True,
            "added_kan_actions": True,
            "chankan_reaction_windows": True,
            "chankan_ron_resolution": True,
            "ankan_kokushi_chankan": True,
            "dead_wall_replacement_draws": True,
            "kan_dora_indicator_metadata": True,
            "rinshan_draw_metadata": True,
            "haitei_houtei_yaku_metadata": True,
            "endgame_yaku_timing_fixtures": True,
            "double_riichi_yaku_metadata": True,
            "kita_policy": False,
            "kita_ron_reaction_windows": rules.players == 3,
            "kita_ron_resolution": rules.players == 3,
            "calls": False,
            "kan_policy": False,
            "chankan_policy": False,
            "call_policy": False,
            "ron_policy": False,
            "open_hand_win_detection": True,
            "reaction_windows_auto_passed": True,
            "discard_furiten_ron_filter": True,
            "temporary_furiten_ron_filter": True,
            "riichi_furiten_ron_filter": True,
            "basic_yaku_win_filter": True,
            "basic_yaku_metadata": True,
            "yakuhai_seat_round_dragon_filter": True,
            "toitoi_yaku_metadata": True,
            "honroutou_yaku_metadata": True,
            "terminal_rewards": True,
            "selectable_reward_modes": True,
            "reward_mode_comparison": True,
            "point_delta_reward_mode": True,
            "normalized_point_delta_reward_mode": True,
            "placement_delta_reward_mode": True,
            "terminal_point_delta_metadata": True,
            "exhaustive_draw_tenpai_noten_payments": True,
            "nagashi_mangan_wall_exhaustion": True,
            "nagashi_mangan_next_round_progression": True,
            "terminal_score_estimate_metadata": True,
            "score_estimate_point_accounting": True,
            "dealer_aware_win_payments": True,
            "visible_dora_score_estimates": True,
            "ura_dora_score_estimates": True,
            "red_dora_score_estimates": True,
            "kazoe_yakuman_score_estimates": True,
            "yakuman_bonus_han_suppression": True,
            "scoring": False,
            "ppo": False,
        },
    }


def format_self_play_sandbox_report(report: dict[str, Any]) -> str:
    if report.get("kind") != SELF_PLAY_SANDBOX_REPORT_KIND:
        raise ValueError(f"report kind must be {SELF_PLAY_SANDBOX_REPORT_KIND}")
    lines = [
        f"episodes: {report['episodes']}",
        f"ruleset: {report['ruleset']}",
        f"policy: {report['policy']['kind']}",
        f"reward_mode: {report['reward_mode']}",
        f"decisions: {report['decisions']}",
        f"average_decisions: {float(report['average_decisions']):.2f}",
        f"stop_on_tsumo: {'yes' if report.get('stop_on_tsumo') else 'no'}",
        "terminal_reasons: " + _format_counts(report["terminal_reasons"]),
        "draw_outcomes: " + _format_counts(report["outcome_summary"]["draw_outcomes"]),
        "reward_modes: " + ", ".join(report["reward_modes"]),
        "reward_summary: "
        + " ".join(
            f"{seat}={reward:.3f}"
            for seat, reward in enumerate(report["reward_summary"]["average_reward_by_seat"])
        ),
        "seat_decisions: " + " ".join(
            f"{seat}={count}" for seat, count in enumerate(report["seat_decisions"])
        ),
        "capabilities:",
    ]
    for name, enabled in report["capabilities"].items():
        lines.append(f"  {name}: {'yes' if enabled else 'no'}")
    return "\n".join(lines)


def _simulate_episode(
    *,
    episode_index: int,
    seed: int,
    max_turns: int,
    policy: str,
    reward_mode: str,
    rules: RuleSet,
    policy_counts: list[int],
    include_trajectory: bool,
    stop_on_tsumo: bool,
) -> dict[str, Any]:
    state = initial_sandbox_environment(ruleset=rules.name, seed=seed)
    decisions: list[SelfPlaySandboxDecision] = []
    seat_decisions = [0] * rules.players
    discard_counts: Counter[str] = Counter()

    for turn in range(max_turns):
        seat = state.current_seat
        state = draw_for_current_seat(state, stop_on_tsumo=stop_on_tsumo)
        if state.terminal_reason is not None:
            break
        draw = state.drawn_tile
        if draw is None:
            raise RuntimeError("sandbox draw did not record a drawn tile")
        action = _choose_discard_action(
            state=state,
            policy=policy,
            policy_counts=policy_counts,
            seed=seed,
            turn=turn,
        )
        next_state, discard = apply_discard_action(state, action)
        next_state = _auto_pass_reactions(next_state)
        policy_counts[discard.type.index] += 1
        discard_counts[discard.notation] += 1
        seat_decisions[seat] += 1
        decisions.append(
            SelfPlaySandboxDecision(
                turn=turn,
                seat=seat,
                draw=draw,
                discard=discard,
                hand_size_after_discard=len(next_state.hands[seat]),
            )
        )
        state = next_state
    else:
        state = _terminal_max_turns(state)

    state_payload = state.to_payload()
    reward_payload = _episode_reward_payload(state)
    payload: dict[str, Any] = {
        "episode": episode_index,
        "seed": seed,
        "decisions": len(decisions),
        "terminal_reason": state.terminal_reason,
        "winner_seat": state.winner_seat,
        "winner_seats": list(state.winner_seats),
        "winning_tile": None if state.winning_tile is None else state.winning_tile.notation,
        "winning_shapes": list(state.winning_shapes),
        "winning_shapes_by_seat": [
            {"seat": seat, "shapes": list(shapes)}
            for seat, shapes in state.winning_shapes_by_seat
        ],
        "winning_yaku": list(state.winning_yaku),
        "winning_yaku_by_seat": [
            {"seat": seat, "yaku": list(yaku)}
            for seat, yaku in state.winning_yaku_by_seat
        ],
        "terminal_rewards": list(state.terminal_rewards),
        "terminal_point_deltas": list(state.terminal_point_deltas),
        "raw_point_delta": reward_payload["raw_point_delta"],
        "normalized_point_delta": reward_payload["normalized_point_delta"],
        "placement_delta": reward_payload["placement_delta"],
        "win_events": reward_payload["win_events"],
        "deal_in_events": reward_payload["deal_in_events"],
        "draw_outcome": reward_payload["draw_outcome"],
        "reward_vectors": reward_payload["reward_vectors"],
        "selected_rewards": reward_payload["reward_vectors"][reward_mode],
        "exhaustive_draw_tenpai_seats": state_payload["exhaustive_draw_tenpai_seats"],
        "exhaustive_draw_noten_seats": state_payload["exhaustive_draw_noten_seats"],
        "terminal_score_estimates": state_payload["terminal_score_estimates"],
        "final_points": state_payload["points"],
        "riichi_sticks": state.riichi_sticks,
        "honba": state.honba,
        "dealer_seat": state.dealer_seat,
        "round_wind": state_payload["round_wind"],
        "double_riichi_seats": state_payload["double_riichi_seats"],
        "ippatsu_seats": state_payload["ippatsu_seats"],
        "winning_ippatsu_seats": state_payload["winning_ippatsu_seats"],
        "rinshan_draw": state_payload["rinshan_draw"],
        "last_draw_was_final_live_wall": state_payload["last_draw_was_final_live_wall"],
        "winning_rinshan_seats": state_payload["winning_rinshan_seats"],
        "kita_tiles": state_payload["kita_tiles"],
        "kita_counts": state_payload["kita_counts"],
        "wall_remaining": len(state.wall),
        "dead_wall_remaining": state_payload["dead_wall_remaining"],
        "dora_indicators": state_payload["dora_indicators"],
        "seat_decisions": seat_decisions,
        "discard_counts": dict(sorted(discard_counts.items())),
        "final_hand_sizes": state.hand_sizes(),
    }
    if include_trajectory:
        payload["trajectory"] = [decision.to_payload() for decision in decisions]
    return payload


def _episode_reward_payload(state: SandboxEnvironmentState) -> dict[str, Any]:
    raw_point_delta = (
        tuple(state.terminal_point_deltas)
        if state.terminal_point_deltas
        else tuple(0 for _seat in range(state.players))
    )
    terminal_rewards = (
        tuple(state.terminal_rewards)
        if state.terminal_rewards
        else tuple(0.0 for _seat in range(state.players))
    )
    normalized_point_delta = _normalized_point_delta_rewards(raw_point_delta)
    placement_delta = _placement_delta_rewards(state.points, players=state.players)
    reward_vectors = {
        "terminal": list(terminal_rewards),
        "point-delta": list(raw_point_delta),
        "normalized-point-delta": list(normalized_point_delta),
        "placement-delta": list(placement_delta),
    }
    return {
        "raw_point_delta": list(raw_point_delta),
        "normalized_point_delta": list(normalized_point_delta),
        "placement_delta": list(placement_delta),
        "win_events": [
            1 if seat in state.winner_seats else 0
            for seat in range(state.players)
        ],
        "deal_in_events": list(_deal_in_events(state, raw_point_delta)),
        "draw_outcome": _draw_outcome(state),
        "reward_vectors": reward_vectors,
    }


def _reward_summaries(
    episodes: list[dict[str, Any]],
    *,
    players: int,
) -> dict[str, dict[str, Any]]:
    summaries: dict[str, dict[str, Any]] = {}
    episode_count = len(episodes)
    for mode in SELF_PLAY_SANDBOX_REWARD_MODES:
        reward_sum = [0.0] * players
        nonzero_episodes = 0
        total_abs_reward = 0.0
        for episode in episodes:
            rewards = [float(value) for value in episode["reward_vectors"][mode]]
            if any(abs(value) > 1e-12 for value in rewards):
                nonzero_episodes += 1
            for seat, reward in enumerate(rewards):
                reward_sum[seat] += reward
                total_abs_reward += abs(reward)
        summaries[mode] = {
            "mode": mode,
            "episodes": episode_count,
            "reward_sum_by_seat": reward_sum,
            "average_reward_by_seat": [
                reward / episode_count for reward in reward_sum
            ],
            "mean_abs_reward": total_abs_reward / (episode_count * players),
            "nonzero_episodes": nonzero_episodes,
        }
    return summaries


def _outcome_summary(
    episodes: list[dict[str, Any]],
    *,
    players: int,
) -> dict[str, Any]:
    win_events = [0] * players
    deal_in_events = [0] * players
    raw_point_delta = [0] * players
    normalized_point_delta = [0.0] * players
    placement_delta = [0.0] * players
    draw_outcomes: Counter[str] = Counter()
    for episode in episodes:
        draw_outcomes[str(episode["draw_outcome"])] += 1
        for seat in range(players):
            win_events[seat] += int(episode["win_events"][seat])
            deal_in_events[seat] += int(episode["deal_in_events"][seat])
            raw_point_delta[seat] += int(episode["raw_point_delta"][seat])
            normalized_point_delta[seat] += float(episode["normalized_point_delta"][seat])
            placement_delta[seat] += float(episode["placement_delta"][seat])
    return {
        "win_events_by_seat": win_events,
        "deal_in_events_by_seat": deal_in_events,
        "raw_point_delta_sum_by_seat": raw_point_delta,
        "normalized_point_delta_sum_by_seat": normalized_point_delta,
        "placement_delta_sum_by_seat": placement_delta,
        "draw_outcomes": dict(sorted(draw_outcomes.items())),
    }


def _normalized_point_delta_rewards(point_deltas: tuple[int, ...]) -> tuple[float, ...]:
    max_delta = max((abs(delta) for delta in point_deltas), default=0)
    if max_delta == 0:
        return tuple(0.0 for _delta in point_deltas)
    return tuple(delta / max_delta for delta in point_deltas)


def _placement_delta_rewards(points: tuple[int, ...], *, players: int) -> tuple[float, ...]:
    if not points:
        return tuple(0.0 for _seat in range(players))

    rewards = [0.0] * players
    ranked = sorted((points[seat], seat) for seat in range(players))
    ranked.reverse()
    center_rank = (players - 1) / 2
    rank = 0
    while rank < players:
        end = rank + 1
        point_value = ranked[rank][0]
        while end < players and ranked[end][0] == point_value:
            end += 1
        average_rank = (rank + end - 1) / 2
        reward = center_rank - average_rank
        for _points, seat in ranked[rank:end]:
            rewards[seat] = reward
        rank = end
    return tuple(rewards)


def _deal_in_events(
    state: SandboxEnvironmentState,
    raw_point_delta: tuple[int, ...],
) -> tuple[int, ...]:
    if state.terminal_reason not in {"ron", "chankan"}:
        return tuple(0 for _seat in range(state.players))
    winner_seats = set(state.winner_seats)
    return tuple(
        1 if seat not in winner_seats and raw_point_delta[seat] < 0 else 0
        for seat in range(state.players)
    )


def _draw_outcome(state: SandboxEnvironmentState) -> str:
    if state.terminal_reason == "max_turns":
        return "max_turns"
    if state.terminal_reason == "wall_exhausted":
        if state.exhaustive_draw_tenpai_seats or state.exhaustive_draw_noten_seats:
            return "wall_exhausted_tenpai_noten"
        return "wall_exhausted_neutral"
    if state.terminal_reason == "nagashi_mangan":
        return "nagashi_mangan"
    return "none"


def _choose_discard_action(
    *,
    state: SandboxEnvironmentState,
    policy: str,
    policy_counts: list[int],
    seed: int,
    turn: int,
) -> Action:
    actions = legal_discard_actions(state)
    if policy == "drawn":
        draw = state.drawn_tile
        if draw is None:
            raise RuntimeError("drawn policy requires a drawn tile")
        return Action.discard(draw.type)
    if policy == "frequency":
        best_score = max(policy_counts[action.tile.index] for action in actions if action.tile)
        candidates = [
            action
            for action in actions
            if action.tile is not None and policy_counts[action.tile.index] == best_score
        ]
        return _deterministic_choice(candidates, seed=seed, turn=turn)
    return _deterministic_choice(actions, seed=seed, turn=turn)


def _deterministic_choice(
    actions: tuple[Action, ...] | list[Action],
    *,
    seed: int,
    turn: int,
) -> Action:
    if not actions:
        raise ValueError("no legal discard actions")
    choice_seed = _episode_seed(str(seed), turn)
    return actions[choice_seed % len(actions)]


def _terminal_max_turns(state: SandboxEnvironmentState) -> SandboxEnvironmentState:
    return SandboxEnvironmentState(
        ruleset=state.ruleset,
        players=state.players,
        wall=state.wall,
        hands=state.hands,
        dead_wall=state.dead_wall,
        dora_indicators=state.dora_indicators,
        discards=state.discards,
        melds=state.melds,
        points=state.points,
        riichi_sticks=state.riichi_sticks,
        honba=state.honba,
        dealer_seat=state.dealer_seat,
        round_wind=state.round_wind,
        current_seat=state.current_seat,
        turn=state.turn,
        drawn_tile=state.drawn_tile,
        rinshan_draw=state.rinshan_draw,
        last_draw_was_final_live_wall=state.last_draw_was_final_live_wall,
        needs_discard=state.needs_discard,
        pending_discard=state.pending_discard,
        pending_discard_seat=state.pending_discard_seat,
        pending_chankan_tile=state.pending_chankan_tile,
        pending_chankan_seat=state.pending_chankan_seat,
        pending_chankan_kind=state.pending_chankan_kind,
        pending_kita_tile=state.pending_kita_tile,
        pending_kita_seat=state.pending_kita_seat,
        pending_reaction_seats=state.pending_reaction_seats,
        temporary_furiten_seats=state.temporary_furiten_seats,
        riichi_seats=state.riichi_seats,
        double_riichi_seats=state.double_riichi_seats,
        riichi_pending_discard_seats=state.riichi_pending_discard_seats,
        ippatsu_seats=state.ippatsu_seats,
        riichi_furiten_seats=state.riichi_furiten_seats,
        terminal_reason="max_turns",
        winner_seat=state.winner_seat,
        winner_seats=state.winner_seats,
        winning_tile=state.winning_tile,
        winning_shapes=state.winning_shapes,
        winning_shapes_by_seat=state.winning_shapes_by_seat,
        winning_yaku=state.winning_yaku,
        winning_yaku_by_seat=state.winning_yaku_by_seat,
        winning_ippatsu_seats=state.winning_ippatsu_seats,
        winning_rinshan_seats=state.winning_rinshan_seats,
        terminal_rewards=tuple(0.0 for _seat in range(state.players)),
        terminal_point_deltas=tuple(0 for _seat in range(state.players)),
        exhaustive_draw_tenpai_seats=state.exhaustive_draw_tenpai_seats,
        exhaustive_draw_noten_seats=state.exhaustive_draw_noten_seats,
    )


def _auto_pass_reactions(state: SandboxEnvironmentState) -> SandboxEnvironmentState:
    while (
        state.pending_discard is not None
        or state.pending_chankan_tile is not None
        or state.pending_kita_tile is not None
    ):
        if not state.pending_reaction_seats:
            raise RuntimeError("pending reaction has no reaction seats")
        state = apply_reaction_pass_action(state, seat=state.pending_reaction_seats[0])
    return state


def _episode_seed(seed: str, episode_index: int) -> int:
    digest = blake2b(f"{seed}:{episode_index}".encode(), digest_size=8).digest()
    return int.from_bytes(digest, "big")


def _tile_count_payload(counts: list[int]) -> dict[str, int]:
    return {
        TileType(tile_type).notation: count
        for tile_type, count in enumerate(counts)
        if count
    }


def _format_counts(counts: dict[str, int]) -> str:
    if not counts:
        return "none"
    return " ".join(f"{key}={counts[key]}" for key in sorted(counts))
