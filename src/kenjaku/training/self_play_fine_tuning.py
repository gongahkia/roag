"""Conservative PPO fine-tuning for validated multi-action distillation checkpoints."""

from __future__ import annotations

import copy
import math
import random
from collections import defaultdict
from collections.abc import Sequence
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from kenjaku.models.multi_action_policy import (
    MaskedMultiActionPolicyHead,
    MultiActionPolicyConfig,
)
from kenjaku.models.torch_discard import require_torch_modules, resolve_torch_device
from kenjaku.reproducibility import derive_seed_int
from kenjaku.schema import LEGAL_ACTION_MASK_V1_DIM, CheckpointManifestV1
from kenjaku.simulation import (
    run_self_play_match_sandbox,
    sandbox_policy_inputs,
    select_sandbox_action_from_logits,
)
from kenjaku.simulation.config import SandboxRuleConfig
from kenjaku.training.behavior_distillation import (
    BehaviorDistillationTrainingResult,
    load_behavior_distillation_checkpoint,
    save_behavior_distillation_checkpoint,
)
from kenjaku.training.guardrails import DEFAULT_TRAINING_TIMEOUT_SECONDS, OptimizerResourceLimits

SELF_PLAY_PPO_FINE_TUNING_KIND = "kenjaku-self-play-ppo-fine-tuning-v0"


@dataclass(slots=True)
class SelfPlayPpoTransition:
    observation: tuple[float, ...]
    legal_mask: tuple[bool, ...]
    action_index: int
    reward: float
    done: bool
    game: int
    seat: int


@dataclass(slots=True)
class SelfPlayPpoFineTuningResult:
    model: MaskedMultiActionPolicyHead
    checkpoint_manifest: CheckpointManifestV1
    config: MultiActionPolicyConfig
    optimizer_state: dict[str, Any]
    report: dict[str, Any]
    seed: int


def fine_tune_behavior_checkpoint_with_self_play_ppo(
    checkpoint_path: str | Path,
    *,
    updates: int,
    rollout_games: int,
    max_rounds: int,
    max_turns_per_round: int,
    seed: int,
    ruleset: str = "tenhou-4p",
    rule_config: SandboxRuleConfig | None = None,
    ppo_epochs: int = 1,
    batch_size: int = 64,
    learning_rate: float = 0.0001,
    gamma: float = 0.99,
    gae_lambda: float = 0.95,
    clip_epsilon: float = 0.05,
    max_kl: float = 0.01,
    entropy_coef: float = 0.001,
    value_coef: float = 0.5,
    max_grad_norm: float = 0.5,
    reward_scale: float = 100.0,
    device: str = "auto",
    timeout_seconds: float | None = DEFAULT_TRAINING_TIMEOUT_SECONDS,
) -> SelfPlayPpoFineTuningResult:
    """Fine-tune one validated behavior checkpoint without replacing its source file."""
    _validate_hyperparameters(
        updates=updates,
        rollout_games=rollout_games,
        max_rounds=max_rounds,
        max_turns_per_round=max_turns_per_round,
        ppo_epochs=ppo_epochs,
        batch_size=batch_size,
        learning_rate=learning_rate,
        gamma=gamma,
        gae_lambda=gae_lambda,
        clip_epsilon=clip_epsilon,
        max_kl=max_kl,
        entropy_coef=entropy_coef,
        value_coef=value_coef,
        max_grad_norm=max_grad_norm,
        reward_scale=reward_scale,
    )
    if rule_config is not None:
        ruleset = rule_config.ruleset
    torch, _nn, functional, _data_loader, _dataset_base = require_torch_modules()
    resolved_device = resolve_torch_device(device)
    payload = load_behavior_distillation_checkpoint(checkpoint_path, device=resolved_device)
    config = MultiActionPolicyConfig(**payload["model"]["config"])
    model = MaskedMultiActionPolicyHead(config, seed=seed)
    model.load_state_dict(payload["model_state_dict"])
    model.to(resolved_device)
    optimizer = torch.optim.AdamW(model.parameters(), lr=learning_rate)
    deadline = OptimizerResourceLimits(timeout_seconds=timeout_seconds).deadline()
    history: list[dict[str, Any]] = []

    for update in range(1, updates + 1):
        deadline.check("self-play rollout")
        transitions, rollout = _collect_rollout(
            model,
            games=rollout_games,
            max_rounds=max_rounds,
            max_turns_per_round=max_turns_per_round,
            seed=derive_seed_int(seed, "self-play", update),
            ruleset=ruleset,
            rule_config=rule_config,
            reward_scale=reward_scale,
            torch=torch,
            device=resolved_device,
        )
        deadline.check("self-play rollout")
        old_log_probs, values = _policy_predictions(
            model,
            transitions,
            torch=torch,
            functional=functional,
            device=resolved_device,
        )
        advantages, returns = _compute_gae(transitions, values, gamma=gamma, gae_lambda=gae_lambda)
        metrics = _run_ppo_update(
            model,
            optimizer=optimizer,
            transitions=transitions,
            old_log_probs=old_log_probs,
            advantages=_normalize(advantages),
            returns=returns,
            ppo_epochs=ppo_epochs,
            batch_size=batch_size,
            clip_epsilon=clip_epsilon,
            max_kl=max_kl,
            entropy_coef=entropy_coef,
            value_coef=value_coef,
            max_grad_norm=max_grad_norm,
            seed=derive_seed_int(seed, "optimizer", update),
            torch=torch,
            functional=functional,
            device=resolved_device,
            deadline=deadline,
        )
        history.append(
            {
                "kind": SELF_PLAY_PPO_FINE_TUNING_KIND,
                "update": update,
                "rollout": rollout,
                "metrics": metrics,
            }
        )

    report = {
        "kind": SELF_PLAY_PPO_FINE_TUNING_KIND,
        "source_checkpoint": str(checkpoint_path),
        "ruleset": ruleset,
        "device": resolved_device.type,
        "training": {
            "updates": updates,
            "rollout_games": rollout_games,
            "max_rounds": max_rounds,
            "max_turns_per_round": max_turns_per_round,
            "ppo_epochs": ppo_epochs,
            "batch_size": batch_size,
            "learning_rate": learning_rate,
            "gamma": gamma,
            "gae_lambda": gae_lambda,
            "clip_epsilon": clip_epsilon,
            "max_kl": max_kl,
            "entropy_coef": entropy_coef,
            "value_coef": value_coef,
            "max_grad_norm": max_grad_norm,
            "reward_scale": reward_scale,
            "history": history,
        },
        "checkpoint_manifest": payload["checkpoint_manifest"].to_dict(),
    }
    return SelfPlayPpoFineTuningResult(
        model=model,
        checkpoint_manifest=payload["checkpoint_manifest"],
        config=config,
        optimizer_state=optimizer.state_dict(),
        report=report,
        seed=seed,
    )


def save_self_play_fine_tuned_checkpoint(
    result: SelfPlayPpoFineTuningResult,
    path: str | Path,
) -> None:
    """Save a fine-tuned model as a validated behavior checkpoint for ONNX export."""
    history = list(result.report["training"]["history"])
    final_metrics: dict[str, int | float | None] = {"examples": 0}
    if history:
        for key, value in history[-1]["metrics"].items():
            if isinstance(value, bool):
                final_metrics[str(key)] = int(value)
            elif isinstance(value, (int, float)):
                final_metrics[str(key)] = value
            else:
                raise ValueError("self-play PPO metrics must be numeric")
    save_behavior_distillation_checkpoint(
        BehaviorDistillationTrainingResult(
            model=result.model,
            device=str(result.report["device"]),
            train_metrics=final_metrics,
            eval_metrics={},
            history=history,
            optimizer_state=result.optimizer_state,
            completed_epochs=int(result.report["training"]["updates"]),
            config=result.config,
            seed=result.seed,
            checkpoint_manifest=result.checkpoint_manifest,
        ),
        path,
    )


def _collect_rollout(
    model: MaskedMultiActionPolicyHead,
    *,
    games: int,
    max_rounds: int,
    max_turns_per_round: int,
    seed: int,
    ruleset: str,
    rule_config: SandboxRuleConfig | None,
    reward_scale: float,
    torch: Any,
    device: Any,
) -> tuple[list[SelfPlayPpoTransition], dict[str, Any]]:
    transitions: list[SelfPlayPpoTransition] = []
    game_reports: list[dict[str, Any]] = []
    for game in range(games):
        game_transitions: list[SelfPlayPpoTransition] = []
        rng = random.Random(derive_seed_int(seed, "game", game, "actions"))

        def select_action(
            state: Any,
            seat: int,
            actions: tuple[Any, ...],
            *,
            game_index: int = game,
            recorded_transitions: list[SelfPlayPpoTransition] = game_transitions,
            selection_rng: random.Random = rng,
        ) -> Any:
            inputs = sandbox_policy_inputs(state, seat=seat, legal_actions=actions)
            observations = torch.tensor(
                [inputs.observation_tensor], dtype=torch.float32, device=device
            )
            masks = torch.tensor([inputs.action_mask.mask], dtype=torch.bool, device=device)
            model.eval()
            with torch.no_grad():
                probabilities = (
                    torch.softmax(model(observations, masks), dim=1)[0].detach().cpu().tolist()
                )
            action_index = _sample_legal_action(
                probabilities, inputs.action_mask.mask, selection_rng
            )
            recorded_transitions.append(
                SelfPlayPpoTransition(
                    observation=inputs.observation_tensor,
                    legal_mask=inputs.action_mask.mask,
                    action_index=action_index,
                    reward=0.0,
                    done=False,
                    game=game_index,
                    seat=seat,
                )
            )
            scores = [-1.0e9] * LEGAL_ACTION_MASK_V1_DIM
            scores[action_index] = 0.0
            return select_sandbox_action_from_logits(inputs, scores)

        report = run_self_play_match_sandbox(
            games=1,
            max_rounds=max_rounds,
            max_turns_per_round=max_turns_per_round,
            seed=str(derive_seed_int(seed, "game", game, "match")),
            ruleset=ruleset,
            rule_config=rule_config,
            action_selector=select_action,
        )
        game_report = report["game_summaries"][0]
        final_scores = game_report["final_scores"]
        if not final_scores:
            raise ValueError("self-play game did not complete; increase round or turn limits")
        last_by_seat: dict[int, SelfPlayPpoTransition] = {}
        for transition in game_transitions:
            last_by_seat[transition.seat] = transition
        for seat, transition in last_by_seat.items():
            transition.reward = float(final_scores[seat]) / reward_scale
            transition.done = True
        transitions.extend(game_transitions)
        game_reports.append(game_report)
    if not transitions:
        raise ValueError("self-play rollout collected no transitions")
    return transitions, {
        "games": games,
        "completed_games": sum(1 for report in game_reports if report["completed"]),
        "transitions": len(transitions),
        "final_reasons": _count_final_reasons(game_reports),
    }


def _policy_predictions(
    model: MaskedMultiActionPolicyHead,
    transitions: Sequence[SelfPlayPpoTransition],
    *,
    torch: Any,
    functional: Any,
    device: Any,
) -> tuple[list[float], list[float]]:
    observations, masks, actions = _transition_tensors(transitions, torch=torch, device=device)
    model.eval()
    with torch.no_grad():
        logits = model(observations, masks)
        log_probs = functional.log_softmax(logits, dim=1)
        action_log_probs = log_probs.gather(1, actions.unsqueeze(1)).squeeze(1)
        values = model.value(observations)
    return (
        [float(value) for value in action_log_probs.detach().cpu().tolist()],
        [float(value) for value in values.detach().cpu().tolist()],
    )


def _run_ppo_update(
    model: MaskedMultiActionPolicyHead,
    *,
    optimizer: Any,
    transitions: Sequence[SelfPlayPpoTransition],
    old_log_probs: Sequence[float],
    advantages: Sequence[float],
    returns: Sequence[float],
    ppo_epochs: int,
    batch_size: int,
    clip_epsilon: float,
    max_kl: float,
    entropy_coef: float,
    value_coef: float,
    max_grad_norm: float,
    seed: int,
    torch: Any,
    functional: Any,
    device: Any,
    deadline: Any,
) -> dict[str, float | int | bool]:
    observations, masks, actions = _transition_tensors(transitions, torch=torch, device=device)
    old_log_prob_tensor = torch.tensor(old_log_probs, dtype=torch.float32, device=device)
    advantage_tensor = torch.tensor(advantages, dtype=torch.float32, device=device)
    return_tensor = torch.tensor(returns, dtype=torch.float32, device=device)
    rows: list[dict[str, float]] = []
    stopped_for_kl = False
    model.train()
    for epoch in range(ppo_epochs):
        indices = list(range(len(transitions)))
        random.Random(seed + epoch).shuffle(indices)
        for start in range(0, len(indices), batch_size):
            deadline.check("self-play PPO optimizer step")
            batch = torch.tensor(
                indices[start : start + batch_size], dtype=torch.long, device=device
            )
            batch_observations = observations.index_select(0, batch)
            batch_masks = masks.index_select(0, batch)
            batch_actions = actions.index_select(0, batch)
            batch_old_log_probs = old_log_prob_tensor.index_select(0, batch)
            batch_advantages = advantage_tensor.index_select(0, batch)
            batch_returns = return_tensor.index_select(0, batch)
            model_state = copy.deepcopy(model.state_dict())
            optimizer_state = copy.deepcopy(optimizer.state_dict())
            optimizer.zero_grad(set_to_none=True)
            logits = model(batch_observations, batch_masks)
            log_probs = functional.log_softmax(logits, dim=1)
            probabilities = log_probs.exp()
            log_prob = log_probs.gather(1, batch_actions.unsqueeze(1)).squeeze(1)
            ratio = (log_prob - batch_old_log_probs).exp()
            unclipped = ratio * batch_advantages
            clipped = ratio.clamp(1.0 - clip_epsilon, 1.0 + clip_epsilon) * batch_advantages
            policy_loss = -torch.minimum(unclipped, clipped)
            value_loss = (model.value(batch_observations) - batch_returns).square()
            legal_log_probs = torch.where(batch_masks, log_probs, torch.zeros_like(log_probs))
            entropy = -(probabilities * legal_log_probs).sum(dim=1)
            loss = (policy_loss + value_coef * value_loss - entropy_coef * entropy).mean()
            loss.backward()
            torch.nn.utils.clip_grad_norm_(model.parameters(), max_grad_norm)
            optimizer.step()
            model.eval()
            with torch.no_grad():
                updated_logits = model(batch_observations, batch_masks)
                updated_log_probs = functional.log_softmax(updated_logits, dim=1)
                legal_log_probs = torch.where(batch_masks, log_probs, torch.zeros_like(log_probs))
                updated_legal_log_probs = torch.where(
                    batch_masks, updated_log_probs, torch.zeros_like(updated_log_probs)
                )
                approx_kl = float(
                    (
                        probabilities * (legal_log_probs - updated_legal_log_probs)
                    ).sum(dim=1).mean().detach().cpu()
                )
            if approx_kl > max_kl:
                model.load_state_dict(model_state)
                optimizer.load_state_dict(optimizer_state)
                stopped_for_kl = True
                break
            model.train()
            rows.append(
                {
                    "loss": float(loss.detach().cpu()),
                    "policy_loss": float(policy_loss.mean().detach().cpu()),
                    "value_loss": float(value_loss.mean().detach().cpu()),
                    "entropy": float(entropy.mean().detach().cpu()),
                    "approx_kl": approx_kl,
                    "clip_fraction": float(
                        ((ratio - 1.0).abs() > clip_epsilon).float().mean().detach().cpu()
                    ),
                }
            )
        if stopped_for_kl:
            break
    if not rows:
        raise RuntimeError("self-play PPO update produced no batches")
    return {
        **{key: sum(row[key] for row in rows) / len(rows) for key in rows[0]},
        "batches": len(rows),
        "stopped_for_kl": stopped_for_kl,
    }


def _transition_tensors(
    transitions: Sequence[SelfPlayPpoTransition],
    *,
    torch: Any,
    device: Any,
) -> tuple[Any, Any, Any]:
    return (
        torch.tensor(
            [transition.observation for transition in transitions],
            dtype=torch.float32,
            device=device,
        ),
        torch.tensor(
            [transition.legal_mask for transition in transitions],
            dtype=torch.bool,
            device=device,
        ),
        torch.tensor(
            [transition.action_index for transition in transitions],
            dtype=torch.long,
            device=device,
        ),
    )


def _compute_gae(
    transitions: Sequence[SelfPlayPpoTransition],
    values: Sequence[float],
    *,
    gamma: float,
    gae_lambda: float,
) -> tuple[list[float], list[float]]:
    advantages = [0.0] * len(transitions)
    returns = [0.0] * len(transitions)
    by_sequence: dict[tuple[int, int], list[int]] = defaultdict(list)
    for index, transition in enumerate(transitions):
        by_sequence[(transition.game, transition.seat)].append(index)
    for indices in by_sequence.values():
        next_value = 0.0
        next_advantage = 0.0
        for index in reversed(indices):
            transition = transitions[index]
            non_terminal = 0.0 if transition.done else 1.0
            delta = transition.reward + gamma * next_value * non_terminal - values[index]
            advantage = delta + gamma * gae_lambda * non_terminal * next_advantage
            advantages[index] = advantage
            returns[index] = advantage + values[index]
            next_value = values[index]
            next_advantage = advantage
    return advantages, returns


def _sample_legal_action(
    probabilities: Sequence[float], mask: Sequence[bool], rng: random.Random
) -> int:
    threshold = rng.random()
    cumulative = 0.0
    last_legal = -1
    for index, (probability, legal) in enumerate(zip(probabilities, mask, strict=True)):
        if not legal:
            continue
        if not math.isfinite(probability) or probability < 0.0:
            raise ValueError("policy produced invalid legal-action probabilities")
        cumulative += probability
        last_legal = index
        if threshold < cumulative:
            return index
    if last_legal < 0 or not math.isclose(cumulative, 1.0, rel_tol=1.0e-5, abs_tol=1.0e-5):
        raise ValueError("policy probabilities do not normalize over legal actions")
    return last_legal


def _normalize(values: Sequence[float]) -> list[float]:
    if len(values) < 2:
        return list(values)
    mean = sum(values) / len(values)
    variance = sum((value - mean) ** 2 for value in values) / len(values)
    std = math.sqrt(variance)
    if std <= 1.0e-8:
        return [value - mean for value in values]
    return [(value - mean) / (std + 1.0e-8) for value in values]


def _count_final_reasons(reports: Sequence[dict[str, Any]]) -> dict[str, int]:
    counts: dict[str, int] = {}
    for report in reports:
        reason = str(report["terminal_reason"])
        counts[reason] = counts.get(reason, 0) + 1
    return dict(sorted(counts.items()))


def _validate_hyperparameters(**values: Any) -> None:
    positive_integers = (
        "updates",
        "rollout_games",
        "max_rounds",
        "max_turns_per_round",
        "ppo_epochs",
        "batch_size",
    )
    if any(type(values[name]) is not int or values[name] <= 0 for name in positive_integers):
        raise ValueError("self-play PPO counts must be positive integers")
    if values["learning_rate"] <= 0.0:
        raise ValueError("learning_rate must be positive")
    if not 0.0 <= values["gamma"] <= 1.0 or not 0.0 <= values["gae_lambda"] <= 1.0:
        raise ValueError("gamma and gae_lambda must be in [0.0, 1.0]")
    if not 0.0 < values["clip_epsilon"] <= 0.2:
        raise ValueError("clip_epsilon must be in (0.0, 0.2]")
    if not 0.0 < values["max_kl"] <= 0.1:
        raise ValueError("max_kl must be in (0.0, 0.1]")
    if values["entropy_coef"] < 0.0 or values["value_coef"] < 0.0:
        raise ValueError("entropy_coef and value_coef must be non-negative")
    if values["max_grad_norm"] <= 0.0 or values["reward_scale"] <= 0.0:
        raise ValueError("max_grad_norm and reward_scale must be positive")
