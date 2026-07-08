from __future__ import annotations

import json
import math
import random
from collections import defaultdict
from collections.abc import Sequence
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from kenjaku.core import TileType
from kenjaku.models.torch_discard import require_torch_modules, resolve_torch_device
from kenjaku.simulation import run_self_play_match_sandbox
from kenjaku.training.history import normalize_training_history

try:
    from torch import nn as _nn
except ImportError:  # pragma: no cover - exercised only without the ml extra
    _nn = None

_TorchModuleBase = _nn.Module if _nn is not None else object

PPO_SANDBOX_POLICY_KIND = "sandbox-linear-ppo-actor-critic-v0"
PPO_SANDBOX_REPORT_KIND = "kenjaku-ppo-sandbox-report-v0"
PPO_SANDBOX_CHECKPOINT_KIND = "kenjaku-ppo-sandbox-checkpoint-v0"

PPO_DECISION_TYPES = ("discard", "pass", "call", "kan", "kita", "riichi", "ron")
PPO_STATE_DIM = 58
PPO_ACTION_KIND_OFFSETS = {
    "discard": 0,
    "ron": 34,
    "chi": 68,
    "pon": 102,
    "minkan": 136,
    "ankan": 170,
    "kakan": 204,
    "kita": 238,
}
PPO_PASS_ACTION_INDEX = 272
PPO_TSUMO_ACTION_INDEX = 273
PPO_RIICHI_ACTION_INDEX = 274
PPO_KYUSHU_ACTION_INDEX = 275
PPO_ACTION_DIM = 276


@dataclass(slots=True)
class SandboxPpoTransition:
    state: list[float]
    legal_mask: list[bool]
    action_index: int
    reward: float
    done: bool
    game: int
    seat: int
    step: int
    decision_type: str


@dataclass(slots=True)
class SandboxPpoRollout:
    transitions: list[SandboxPpoTransition]
    report: dict[str, Any]
    reward_scale: float


@dataclass(slots=True)
class SandboxPpoTrainingResult:
    model: SandboxLinearPpoActorCritic
    optimizer_state: dict[str, Any]
    device: str
    report: dict[str, Any]
    model_state: dict[str, Any]


class SandboxLinearPpoActorCritic(_TorchModuleBase):
    """Torch actor-critic for deterministic PPO sandbox smoke tests."""

    def __init__(
        self,
        *,
        input_dim: int = PPO_STATE_DIM,
        hidden_dim: int = 0,
        action_dim: int = PPO_ACTION_DIM,
        seed: int = 0,
        state: dict[str, Any] | None = None,
        device: Any | None = None,
    ) -> None:
        torch, nn, functional, _data_loader, _dataset_base = require_torch_modules()
        super().__init__()
        if input_dim <= 0:
            raise ValueError("input_dim must be positive")
        if hidden_dim < 0:
            raise ValueError("hidden_dim must be non-negative")
        if action_dim <= 0:
            raise ValueError("action_dim must be positive")
        self.kind = PPO_SANDBOX_POLICY_KIND
        self.input_dim = input_dim
        self.hidden_dim = hidden_dim
        self.action_dim = action_dim
        self._torch = torch
        self._functional = functional
        self.device = torch.device("cpu") if device is None else torch.device(device)
        self.dtype = _ppo_dtype(torch, self.device)
        self.policy = _mlp(nn, input_dim, hidden_dim, action_dim, dtype=self.dtype)
        self.value_net = _mlp(nn, input_dim, hidden_dim, 1, dtype=self.dtype)
        if state is None:
            self._reset_parameters(seed)
        else:
            self.load_serialized_state_dict(state)
        self.to(self.device)

    def forward(self, states: Any, legal_masks: Any) -> tuple[Any, Any]:
        return self.logits_tensor(states, legal_masks), self.value_tensor(states)

    def logits_tensor(self, states: Any, legal_masks: Any) -> Any:
        logits = self.policy(states)
        return logits.masked_fill(~legal_masks, -1.0e9)

    def value_tensor(self, states: Any) -> Any:
        return self.value_net(states).squeeze(-1)

    def logits(self, state: Sequence[float], legal_mask: Sequence[bool]) -> list[float]:
        with self._torch.no_grad():
            states = _state_tensor(self, [state])
            masks = _mask_tensor(self, [legal_mask])
            logits = self.logits_tensor(states, masks)[0].detach().cpu().tolist()
        return [float(value) for value in logits]

    def value(self, state: Sequence[float]) -> float:
        with self._torch.no_grad():
            values = self.value_tensor(_state_tensor(self, [state])).detach().cpu().tolist()
        return float(values[0])

    def serializable_state_dict(self) -> dict[str, Any]:
        return {
            "format": "torch-module-v1",
            "policy": _serializable_module_state(self.policy),
            "value_net": _serializable_module_state(self.value_net),
        }

    def load_serialized_state_dict(self, state: dict[str, Any]) -> None:
        if state.get("format") == "torch-module-v1":
            self.policy.load_state_dict(_module_tensor_state(self, state["policy"]))
            self.value_net.load_state_dict(_module_tensor_state(self, state["value_net"]))
            return
        if any(str(key).startswith(("policy.", "value_net.")) for key in state):
            _TorchModuleBase.load_state_dict(self, state)
            return
        if self.hidden_dim != 0:
            raise ValueError("legacy PPO checkpoint state only supports hidden_dim=0")
        self._load_legacy_linear_state(state)

    def _reset_parameters(self, seed: int) -> None:
        rng = random.Random(seed)
        with self._torch.no_grad():
            for module in _linear_layers(self.policy):
                _fill_linear(module, rng)
            for module in _linear_layers(self.value_net):
                _fill_linear(module, rng)

    def _load_legacy_linear_state(self, state: dict[str, Any]) -> None:
        policy_layers = _linear_layers(self.policy)
        value_layers = _linear_layers(self.value_net)
        if len(policy_layers) != 1 or len(value_layers) != 1:
            raise ValueError("legacy PPO checkpoint state requires linear modules")
        policy_weights = state["policy_weights"]
        policy_bias = state["policy_bias"]
        value_weights = state["value_weights"]
        value_bias = state["value_bias"]
        if len(policy_weights) != self.action_dim:
            raise ValueError("policy weight action dimension mismatch")
        if any(len(row) != self.input_dim for row in policy_weights):
            raise ValueError("policy weight input dimension mismatch")
        if len(policy_bias) != self.action_dim:
            raise ValueError("policy bias action dimension mismatch")
        if len(value_weights) != self.input_dim:
            raise ValueError("value weight input dimension mismatch")
        with self._torch.no_grad():
            policy_layers[0].weight.copy_(
                self._torch.tensor(policy_weights, dtype=self.dtype, device=self.device)
            )
            policy_layers[0].bias.copy_(
                self._torch.tensor(policy_bias, dtype=self.dtype, device=self.device)
            )
            value_layers[0].weight.copy_(
                self._torch.tensor([value_weights], dtype=self.dtype, device=self.device)
            )
            value_layers[0].bias.copy_(
                self._torch.tensor([value_bias], dtype=self.dtype, device=self.device)
            )


class _LegacySandboxLinearPpoActorCritic:
    def __init__(
        self,
        *,
        input_dim: int = PPO_STATE_DIM,
        action_dim: int = PPO_ACTION_DIM,
        seed: int = 0,
    ) -> None:
        self.kind = PPO_SANDBOX_POLICY_KIND
        self.input_dim = input_dim
        self.hidden_dim = 0
        self.action_dim = action_dim
        rng = random.Random(seed)
        self.policy_weights = [
            [rng.uniform(-0.01, 0.01) for _feature in range(input_dim)]
            for _action in range(action_dim)
        ]
        self.policy_bias = [0.0 for _action in range(action_dim)]
        self.value_weights = [rng.uniform(-0.01, 0.01) for _feature in range(input_dim)]
        self.value_bias = 0.0

    def logits(self, state: Sequence[float], legal_mask: Sequence[bool]) -> list[float]:
        logits: list[float] = []
        for action, legal in enumerate(legal_mask):
            if not legal:
                logits.append(-1.0e9)
                continue
            logits.append(
                self.policy_bias[action]
                + sum(
                    weight * float(feature)
                    for weight, feature in zip(self.policy_weights[action], state, strict=True)
                )
            )
        return logits

    def value(self, state: Sequence[float]) -> float:
        return self.value_bias + sum(
            weight * float(feature)
            for weight, feature in zip(self.value_weights, state, strict=True)
        )


def _ppo_dtype(torch: Any, device: Any) -> Any:
    return torch.float32 if str(device).startswith(("cuda", "mps")) else torch.float64


def _mlp(nn: Any, input_dim: int, hidden_dim: int, output_dim: int, *, dtype: Any) -> Any:
    if hidden_dim:
        return nn.Sequential(
            nn.Linear(input_dim, hidden_dim, dtype=dtype),
            nn.Tanh(),
            nn.Linear(hidden_dim, output_dim, dtype=dtype),
        )
    return nn.Sequential(nn.Linear(input_dim, output_dim, dtype=dtype))


def _linear_layers(module: Any) -> list[Any]:
    return [child for child in module.modules() if child is not module and hasattr(child, "weight")]


def _fill_linear(module: Any, rng: random.Random) -> None:
    rows, columns = module.weight.shape
    values = [[rng.uniform(-0.01, 0.01) for _column in range(columns)] for _row in range(rows)]
    module.weight.copy_(module.weight.new_tensor(values))
    module.bias.zero_()


def _serializable_module_state(module: Any) -> dict[str, Any]:
    return {name: tensor.detach().cpu().tolist() for name, tensor in module.state_dict().items()}


def _module_tensor_state(
    model: SandboxLinearPpoActorCritic,
    state: dict[str, Any],
) -> dict[str, Any]:
    return {
        name: model._torch.tensor(value, dtype=model.dtype, device=model.device)
        for name, value in state.items()
    }


def _state_tensor(model: SandboxLinearPpoActorCritic, states: Sequence[Sequence[float]]) -> Any:
    return model._torch.tensor(states, dtype=model.dtype, device=model.device)


def _mask_tensor(model: SandboxLinearPpoActorCritic, masks: Sequence[Sequence[bool]]) -> Any:
    return model._torch.tensor(masks, dtype=model._torch.bool, device=model.device)


def _action_tensor(model: SandboxLinearPpoActorCritic, actions: Sequence[int]) -> Any:
    return model._torch.tensor(actions, dtype=model._torch.long, device=model.device)


def _float_tensor(model: SandboxLinearPpoActorCritic, values: Sequence[float]) -> Any:
    return model._torch.tensor(values, dtype=model.dtype, device=model.device)


def _tensor_indices(model: SandboxLinearPpoActorCritic, indices: Sequence[int]) -> Any:
    return model._torch.tensor(indices, dtype=model._torch.long, device=model.device)


def _ppo_tensor_arrays(
    model: SandboxLinearPpoActorCritic,
    arrays: dict[str, Any],
) -> dict[str, Any]:
    return {
        "states": _state_tensor(model, arrays["states"]),
        "legal_masks": _mask_tensor(model, arrays["legal_masks"]),
        "actions": _action_tensor(model, arrays["actions"]),
    }


def train_ppo_sandbox(
    *,
    total_steps: int,
    rollout_games: int,
    max_rounds: int,
    max_turns_per_round: int,
    seed: str,
    ruleset: str = "tenhou-4p",
    rollout_discard_policy: str = "drawn",
    rollout_call_policy: str = "pass",
    rollout_riichi_policy: str = "pass",
    rollout_kan_policy: str = "pass",
    rollout_kita_policy: str = "pass",
    rollout_ron_policy: str = "pass",
    ppo_epochs: int = 2,
    batch_size: int = 64,
    learning_rate: float = 0.001,
    hidden_dim: int = 0,
    gamma: float = 0.99,
    gae_lambda: float = 0.95,
    clip_epsilon: float = 0.2,
    entropy_coef: float = 0.01,
    value_coef: float = 0.5,
    max_grad_norm: float = 0.5,
    reward_scale: float = 100.0,
    supervised_warmup_epochs: int = 0,
    device: str = "auto",
    torch_seed: int = 0,
    resume_checkpoint: str | Path | None = None,
) -> SandboxPpoTrainingResult:
    _validate_ppo_hyperparameters(
        total_steps=total_steps,
        rollout_games=rollout_games,
        max_rounds=max_rounds,
        max_turns_per_round=max_turns_per_round,
        ppo_epochs=ppo_epochs,
        batch_size=batch_size,
        learning_rate=learning_rate,
        hidden_dim=hidden_dim,
        gamma=gamma,
        gae_lambda=gae_lambda,
        clip_epsilon=clip_epsilon,
        entropy_coef=entropy_coef,
        value_coef=value_coef,
        max_grad_norm=max_grad_norm,
        reward_scale=reward_scale,
        supervised_warmup_epochs=supervised_warmup_epochs,
        device=device,
    )
    resolved_device = resolve_torch_device(device)

    history: list[dict[str, Any]] = []
    start_update = 0
    starting_steps = 0
    resume_path = None if resume_checkpoint is None else Path(resume_checkpoint)
    optimizer_state: dict[str, Any] = {"kind": "torch-sgd-v0", "steps": 0}
    if resume_path is None:
        model = SandboxLinearPpoActorCritic(
            hidden_dim=hidden_dim,
            seed=torch_seed,
            device=resolved_device,
        )
    else:
        payload = load_ppo_sandbox_checkpoint(resume_path)
        model_config = payload["model"]
        model = SandboxLinearPpoActorCritic(
            input_dim=int(model_config["input_dim"]),
            hidden_dim=int(model_config["hidden_dim"]),
            action_dim=int(model_config["action_dim"]),
            state=payload["model_state_dict"],
            device=resolved_device,
        )
        training_payload = payload["training"]
        history = list(training_payload.get("history", []))
        start_update = int(training_payload.get("updates", len(history)))
        starting_steps = int(training_payload.get("environment_steps", 0))
        optimizer_state = dict(payload.get("optimizer_state_dict", optimizer_state))

    target_steps = starting_steps + total_steps
    environment_steps = starting_steps
    update = start_update
    final_metrics: dict[str, Any] | None = None
    final_eval: dict[str, Any] | None = None
    final_rollout_summary: dict[str, Any] | None = None
    warmup_rows: list[dict[str, Any]] = []

    while environment_steps < target_steps:
        update += 1
        rollout = collect_ppo_sandbox_rollout(
            games=rollout_games,
            max_rounds=max_rounds,
            max_turns_per_round=max_turns_per_round,
            seed=f"{seed}:update:{update}",
            ruleset=ruleset,
            discard_policy=rollout_discard_policy,
            call_policy=rollout_call_policy,
            riichi_policy=rollout_riichi_policy,
            kan_policy=rollout_kan_policy,
            kita_policy=rollout_kita_policy,
            ron_policy=rollout_ron_policy,
            reward_scale=reward_scale,
        )
        arrays = _rollout_arrays(rollout)
        if update == start_update + 1 and supervised_warmup_epochs:
            warmup_rows.extend(
                _run_supervised_warmup(
                    model,
                    arrays=arrays,
                    epochs=supervised_warmup_epochs,
                    batch_size=batch_size,
                    learning_rate=learning_rate,
                    max_grad_norm=max_grad_norm,
                    seed=torch_seed + update,
                )
            )

        old_log_probs, old_values = _old_policy_predictions(model, arrays)
        advantages, returns = _compute_gae(
            rewards=arrays["rewards"],
            dones=arrays["dones"],
            values=old_values,
            sequence_keys=arrays["sequence_keys"],
            gamma=gamma,
            gae_lambda=gae_lambda,
        )
        normalized_advantages = _normalize(advantages)
        train_metrics = _run_ppo_update(
            model,
            arrays=arrays,
            old_log_probs=old_log_probs,
            advantages=normalized_advantages,
            returns=returns,
            ppo_epochs=ppo_epochs,
            batch_size=batch_size,
            learning_rate=learning_rate,
            clip_epsilon=clip_epsilon,
            entropy_coef=entropy_coef,
            value_coef=value_coef,
            max_grad_norm=max_grad_norm,
            seed=torch_seed + update,
        )
        optimizer_state["steps"] = int(optimizer_state.get("steps", 0)) + int(
            train_metrics["batches"]
        )
        eval_metrics = evaluate_ppo_sandbox_policy(
            model,
            rollout,
            gamma=gamma,
            gae_lambda=gae_lambda,
        )
        environment_steps += len(rollout.transitions)
        rollout_summary = _rollout_summary(rollout.report)
        row = {
            "update": update,
            "environment_steps": len(rollout.transitions),
            "cumulative_environment_steps": environment_steps,
            "losses": train_metrics,
            "evaluation": eval_metrics,
            "rollout": rollout_summary,
        }
        history.append(row)
        final_metrics = train_metrics
        final_eval = eval_metrics
        final_rollout_summary = rollout_summary

    if final_metrics is None or final_eval is None or final_rollout_summary is None:
        raise RuntimeError("PPO training did not collect any rollout updates")

    report = _ppo_report(
        seed=seed,
        ruleset=ruleset,
        device=str(resolved_device),
        model=model,
        requested_total_steps=total_steps,
        starting_environment_steps=starting_steps,
        environment_steps=environment_steps,
        updates=update,
        rollout_games=rollout_games,
        max_rounds=max_rounds,
        max_turns_per_round=max_turns_per_round,
        rollout_policies={
            "discard": rollout_discard_policy,
            "call": rollout_call_policy,
            "riichi": rollout_riichi_policy,
            "kan": rollout_kan_policy,
            "kita": rollout_kita_policy,
            "ron": rollout_ron_policy,
        },
        ppo_epochs=ppo_epochs,
        batch_size=batch_size,
        learning_rate=learning_rate,
        gamma=gamma,
        gae_lambda=gae_lambda,
        clip_epsilon=clip_epsilon,
        entropy_coef=entropy_coef,
        value_coef=value_coef,
        max_grad_norm=max_grad_norm,
        reward_scale=reward_scale,
        supervised_warmup_epochs=supervised_warmup_epochs,
        supervised_warmup_history=warmup_rows,
        history=history,
        final_metrics=final_metrics,
        final_eval=final_eval,
        final_rollout_summary=final_rollout_summary,
        resume_checkpoint=resume_path,
    )
    return SandboxPpoTrainingResult(
        model=model,
        optimizer_state=optimizer_state,
        device=str(resolved_device),
        report=report,
        model_state=model.serializable_state_dict(),
    )


def collect_ppo_sandbox_rollout(
    *,
    games: int,
    max_rounds: int,
    max_turns_per_round: int,
    seed: str,
    ruleset: str = "tenhou-4p",
    discard_policy: str = "drawn",
    call_policy: str = "pass",
    riichi_policy: str = "pass",
    kan_policy: str = "pass",
    kita_policy: str = "pass",
    ron_policy: str = "pass",
    reward_scale: float = 100.0,
) -> SandboxPpoRollout:
    if reward_scale <= 0:
        raise ValueError("reward_scale must be positive")
    report = run_self_play_match_sandbox(
        games=games,
        max_rounds=max_rounds,
        max_turns_per_round=max_turns_per_round,
        seed=seed,
        ruleset=ruleset,
        discard_policy=discard_policy,
        call_policy=call_policy,
        riichi_policy=riichi_policy,
        kan_policy=kan_policy,
        kita_policy=kita_policy,
        ron_policy=ron_policy,
        include_trajectories=True,
    )
    transitions: list[SandboxPpoTransition] = []
    last_transition_by_seat: dict[tuple[int, int], int] = {}

    for game in report["game_summaries"]:
        game_index = int(game["game"])
        for step, entry in enumerate(game.get("trajectory", [])):
            legal_mask = ppo_legal_action_mask(entry["legal_actions"])
            action_index = ppo_action_index(entry["chosen_action"])
            legal_mask[action_index] = True
            transition = SandboxPpoTransition(
                state=ppo_state_features(entry),
                legal_mask=legal_mask,
                action_index=action_index,
                reward=0.0,
                done=False,
                game=game_index,
                seat=int(entry["seat"]),
                step=step,
                decision_type=str(entry["decision_type"]),
            )
            last_transition_by_seat[(game_index, transition.seat)] = len(transitions)
            transitions.append(transition)

        final_scores = game.get("final_scores") or []
        for seat, score in enumerate(final_scores):
            transition_index = last_transition_by_seat.get((game_index, seat))
            if transition_index is None:
                continue
            transitions[transition_index].reward = float(score) / reward_scale
            transitions[transition_index].done = True

    if not transitions:
        raise ValueError("PPO rollout collected no transitions")
    return SandboxPpoRollout(
        transitions=transitions,
        report=report,
        reward_scale=reward_scale,
    )


def ppo_state_features(entry: dict[str, Any]) -> list[float]:
    state = entry["state"]
    points = list(state.get("points", []))[:4]
    points.extend(0 for _seat in range(4 - len(points)))
    drawn_tile = state.get("drawn_tile")
    drawn_one_hot = [0.0] * 34
    if isinstance(drawn_tile, str):
        drawn_one_hot[TileType.parse(drawn_tile).index] = 1.0
    round_wind = str(state.get("round_wind", "E"))
    round_wind_one_hot = [1.0 if round_wind == wind else 0.0 for wind in ("E", "S", "W", "N")]
    decision_type = str(entry.get("decision_type", "pass"))
    decision_one_hot = [
        1.0 if decision_type == candidate else 0.0 for candidate in PPO_DECISION_TYPES
    ]
    pending_reactions = state.get("pending_reaction_seats", [])
    features = [
        float(state.get("turn", 0)) / 256.0,
        float(state.get("current_seat", 0)) / 3.0,
        float(entry.get("seat", 0)) / 3.0,
        float(state.get("dealer_seat", 0)) / 3.0,
        float(state.get("honba", 0)) / 8.0,
        float(state.get("wall_remaining", 0)) / 80.0,
        1.0 if state.get("needs_discard") else 0.0,
        float(len(pending_reactions)) / 4.0,
        float(len(state.get("points", []))) / 4.0,
        *round_wind_one_hot,
        *(float(point) / 100000.0 for point in points),
        *drawn_one_hot,
        *decision_one_hot,
    ]
    if len(features) != PPO_STATE_DIM:
        raise ValueError(f"PPO state must have {PPO_STATE_DIM} features")
    return features


def ppo_legal_action_mask(actions: Sequence[dict[str, Any]]) -> list[bool]:
    mask = [False] * PPO_ACTION_DIM
    for action in actions:
        mask[ppo_action_index(action)] = True
    if not any(mask):
        raise ValueError("PPO legal action mask cannot be empty")
    return mask


def ppo_action_index(action: dict[str, Any]) -> int:
    kind = str(action["kind"])
    if kind in PPO_ACTION_KIND_OFFSETS:
        tile = action.get("tile")
        if not isinstance(tile, str):
            raise ValueError(f"{kind} action requires tile payload")
        return PPO_ACTION_KIND_OFFSETS[kind] + TileType.parse(tile).index
    if kind == "pass":
        return PPO_PASS_ACTION_INDEX
    if kind == "tsumo":
        return PPO_TSUMO_ACTION_INDEX
    if kind == "riichi":
        return PPO_RIICHI_ACTION_INDEX
    if kind == "kyushu":
        return PPO_KYUSHU_ACTION_INDEX
    raise ValueError("unsupported PPO action kind: " + kind)


def evaluate_ppo_sandbox_policy(
    model: SandboxLinearPpoActorCritic,
    rollout: SandboxPpoRollout,
    *,
    gamma: float,
    gae_lambda: float,
) -> dict[str, int | float | None]:
    arrays = _rollout_arrays(rollout)
    _old_log_probs, values = _old_policy_predictions(model, arrays)
    _advantages, returns = _compute_gae(
        rewards=arrays["rewards"],
        dones=arrays["dones"],
        values=values,
        sequence_keys=arrays["sequence_keys"],
        gamma=gamma,
        gae_lambda=gae_lambda,
    )
    examples = len(arrays["actions"])
    tensors = _ppo_tensor_arrays(model, arrays)
    with model._torch.no_grad():
        logits = model.logits_tensor(tensors["states"], tensors["legal_masks"])
        log_probs = model._functional.log_softmax(logits, dim=1)
        probabilities = log_probs.exp()
        entropy = -(probabilities * log_probs).sum(dim=1)
        action_log_probs = log_probs.gather(1, tensors["actions"].unsqueeze(1)).squeeze(1)
        predictions = logits.argmax(dim=1)
        value_tensor = model.value_tensor(tensors["states"])
        returns_tensor = _float_tensor(model, returns)
    return {
        "examples": examples,
        "action_nll": float((-action_log_probs).mean().cpu()),
        "action_accuracy": float((predictions == tensors["actions"]).to(model.dtype).mean().cpu()),
        "entropy": float(entropy.mean().cpu()),
        "value_mse": float((value_tensor - returns_tensor).square().mean().cpu()),
        "explained_variance": _explained_variance(values, returns),
    }


def save_ppo_sandbox_checkpoint(
    result: SandboxPpoTrainingResult,
    path: str | Path,
) -> None:
    checkpoint_path = Path(path)
    checkpoint_path.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "kind": PPO_SANDBOX_CHECKPOINT_KIND,
        "model": result.report["model"],
        "training": result.report["training"],
        "metrics": result.report["metrics"],
        "model_state_dict": result.model_state,
        "optimizer_state_dict": result.optimizer_state,
    }
    checkpoint_path.write_text(json.dumps(payload, indent=2, sort_keys=True), encoding="utf-8")


def load_ppo_sandbox_checkpoint(path: str | Path) -> dict[str, Any]:
    payload = json.loads(Path(path).read_text(encoding="utf-8"))
    if payload.get("kind") != PPO_SANDBOX_CHECKPOINT_KIND:
        raise ValueError(f"checkpoint kind must be {PPO_SANDBOX_CHECKPOINT_KIND}")
    return payload


def format_ppo_sandbox_report(report: dict[str, Any]) -> str:
    if report.get("kind") != PPO_SANDBOX_REPORT_KIND:
        raise ValueError(f"report kind must be {PPO_SANDBOX_REPORT_KIND}")
    metrics = report["metrics"]["final"]
    evaluation = report["metrics"]["evaluation"]
    lines = [
        f"kind: {report['kind']}",
        f"ruleset: {report['ruleset']}",
        f"device: {report['device']}",
        f"updates: {report['training']['updates']}",
        f"environment_steps: {report['training']['environment_steps']}",
        f"policy_loss: {float(metrics['policy_loss']):.6f}",
        f"value_loss: {float(metrics['value_loss']):.6f}",
        f"entropy: {float(metrics['entropy']):.6f}",
        f"action_accuracy: {float(evaluation['action_accuracy']):.6f}",
        "rollout_final_reasons: " + _format_counts(report["rollout"]["final_reasons"]),
        "capabilities:",
    ]
    for name, enabled in report["capabilities"].items():
        lines.append(f"  {name}: {'yes' if enabled else 'no'}")
    return "\n".join(lines)


def _validate_ppo_hyperparameters(
    *,
    total_steps: int,
    rollout_games: int,
    max_rounds: int,
    max_turns_per_round: int,
    ppo_epochs: int,
    batch_size: int,
    learning_rate: float,
    hidden_dim: int,
    gamma: float,
    gae_lambda: float,
    clip_epsilon: float,
    entropy_coef: float,
    value_coef: float,
    max_grad_norm: float,
    reward_scale: float,
    supervised_warmup_epochs: int,
    device: str,
) -> None:
    if total_steps <= 0:
        raise ValueError("total_steps must be positive")
    if rollout_games <= 0:
        raise ValueError("rollout_games must be positive")
    if max_rounds <= 0:
        raise ValueError("max_rounds must be positive")
    if max_turns_per_round <= 0:
        raise ValueError("max_turns_per_round must be positive")
    if ppo_epochs <= 0:
        raise ValueError("ppo_epochs must be positive")
    if batch_size <= 0:
        raise ValueError("batch_size must be positive")
    if learning_rate <= 0:
        raise ValueError("learning_rate must be positive")
    if hidden_dim < 0:
        raise ValueError("hidden_dim must be non-negative")
    if not 0.0 <= gamma <= 1.0:
        raise ValueError("gamma must be in [0.0, 1.0]")
    if not 0.0 <= gae_lambda <= 1.0:
        raise ValueError("gae_lambda must be in [0.0, 1.0]")
    if clip_epsilon <= 0:
        raise ValueError("clip_epsilon must be positive")
    if entropy_coef < 0:
        raise ValueError("entropy_coef must be non-negative")
    if value_coef < 0:
        raise ValueError("value_coef must be non-negative")
    if max_grad_norm <= 0:
        raise ValueError("max_grad_norm must be positive")
    if reward_scale <= 0:
        raise ValueError("reward_scale must be positive")
    if supervised_warmup_epochs < 0:
        raise ValueError("supervised_warmup_epochs must be non-negative")
    if device not in {"auto", "cpu", "cuda", "mps"}:
        raise ValueError("PPO sandbox device must be auto, cpu, cuda, or mps")


def _rollout_arrays(rollout: SandboxPpoRollout) -> dict[str, Any]:
    return {
        "states": [transition.state for transition in rollout.transitions],
        "legal_masks": [transition.legal_mask for transition in rollout.transitions],
        "actions": [transition.action_index for transition in rollout.transitions],
        "rewards": [transition.reward for transition in rollout.transitions],
        "dones": [transition.done for transition in rollout.transitions],
        "sequence_keys": [(transition.game, transition.seat) for transition in rollout.transitions],
    }


def _old_policy_predictions(
    model: SandboxLinearPpoActorCritic,
    arrays: dict[str, Any],
) -> tuple[list[float], list[float]]:
    if isinstance(model, SandboxLinearPpoActorCritic):
        tensors = _ppo_tensor_arrays(model, arrays)
        with model._torch.no_grad():
            logits = model.logits_tensor(tensors["states"], tensors["legal_masks"])
            log_probs = model._functional.log_softmax(logits, dim=1)
            action_log_probs = log_probs.gather(1, tensors["actions"].unsqueeze(1)).squeeze(1)
            values = model.value_tensor(tensors["states"])
        return (
            [float(value) for value in action_log_probs.detach().cpu().tolist()],
            [float(value) for value in values.detach().cpu().tolist()],
        )
    return _legacy_old_policy_predictions(model, arrays)


def _legacy_old_policy_predictions(
    model: Any,
    arrays: dict[str, Any],
) -> tuple[list[float], list[float]]:
    log_probs: list[float] = []
    values: list[float] = []
    for index, state in enumerate(arrays["states"]):
        logits = model.logits(state, arrays["legal_masks"][index])
        action_log_probs, _probabilities, _entropy = _masked_log_probs(logits)
        log_probs.append(action_log_probs[arrays["actions"][index]])
        values.append(model.value(state))
    return log_probs, values


def _compute_gae(
    *,
    rewards: Sequence[float],
    dones: Sequence[bool],
    values: Sequence[float],
    sequence_keys: Sequence[tuple[int, int]],
    gamma: float,
    gae_lambda: float,
) -> tuple[list[float], list[float]]:
    advantages = [0.0 for _reward in rewards]
    returns = [0.0 for _reward in rewards]
    grouped: dict[tuple[int, int], list[int]] = defaultdict(list)
    for index, key in enumerate(sequence_keys):
        grouped[key].append(index)

    for indices in grouped.values():
        next_value = 0.0
        next_advantage = 0.0
        for index in reversed(indices):
            non_terminal = 0.0 if dones[index] else 1.0
            delta = rewards[index] + gamma * next_value * non_terminal - values[index]
            advantage = delta + gamma * gae_lambda * non_terminal * next_advantage
            advantages[index] = advantage
            returns[index] = advantage + values[index]
            next_value = values[index]
            next_advantage = advantage
    return advantages, returns


def _normalize(values: Sequence[float]) -> list[float]:
    if len(values) < 2:
        return list(values)
    mean = sum(values) / len(values)
    variance = sum((value - mean) ** 2 for value in values) / len(values)
    std = math.sqrt(variance)
    if std <= 1.0e-8:
        return [value - mean for value in values]
    return [(value - mean) / (std + 1.0e-8) for value in values]


def _run_supervised_warmup(
    model: SandboxLinearPpoActorCritic,
    *,
    arrays: dict[str, Any],
    epochs: int,
    batch_size: int,
    learning_rate: float,
    max_grad_norm: float,
    seed: int,
) -> list[dict[str, Any]]:
    tensors = _ppo_tensor_arrays(model, arrays)
    optimizer = model._torch.optim.SGD(model.parameters(), lr=learning_rate)
    rows: list[dict[str, Any]] = []
    for epoch in range(1, epochs + 1):
        losses: list[float] = []
        for indices in _minibatch_indices(len(arrays["actions"]), batch_size, seed + epoch):
            batch = _tensor_indices(model, indices)
            optimizer.zero_grad()
            logits = model.logits_tensor(
                tensors["states"].index_select(0, batch),
                tensors["legal_masks"].index_select(0, batch),
            )
            log_probs = model._functional.log_softmax(logits, dim=1)
            actions = tensors["actions"].index_select(0, batch)
            loss = -log_probs.gather(1, actions.unsqueeze(1)).squeeze(1).mean()
            loss.backward()
            model._torch.nn.utils.clip_grad_norm_(model.parameters(), max_grad_norm)
            optimizer.step()
            losses.append(float(loss.detach().cpu()))
        rows.append({"epoch": epoch, "loss": sum(losses) / len(losses)})
    return rows


def _run_ppo_update(
    model: SandboxLinearPpoActorCritic,
    *,
    arrays: dict[str, Any],
    old_log_probs: Sequence[float],
    advantages: Sequence[float],
    returns: Sequence[float],
    ppo_epochs: int,
    batch_size: int,
    learning_rate: float,
    clip_epsilon: float,
    entropy_coef: float,
    value_coef: float,
    max_grad_norm: float,
    seed: int,
) -> dict[str, float]:
    tensors = _ppo_tensor_arrays(model, arrays)
    old_log_prob_tensor = _float_tensor(model, old_log_probs)
    advantage_tensor = _float_tensor(model, advantages)
    return_tensor = _float_tensor(model, returns)
    optimizer = model._torch.optim.SGD(model.parameters(), lr=learning_rate)
    metrics: list[dict[str, float]] = []
    for epoch in range(ppo_epochs):
        for indices in _minibatch_indices(len(arrays["actions"]), batch_size, seed + epoch):
            batch = _tensor_indices(model, indices)
            states = tensors["states"].index_select(0, batch)
            legal_masks = tensors["legal_masks"].index_select(0, batch)
            actions = tensors["actions"].index_select(0, batch)
            old_batch_log_probs = old_log_prob_tensor.index_select(0, batch)
            batch_advantages = advantage_tensor.index_select(0, batch)
            batch_returns = return_tensor.index_select(0, batch)

            optimizer.zero_grad()
            logits = model.logits_tensor(states, legal_masks)
            log_probs = model._functional.log_softmax(logits, dim=1)
            probabilities = log_probs.exp()
            entropy = -(probabilities * log_probs).sum(dim=1)
            log_prob = log_probs.gather(1, actions.unsqueeze(1)).squeeze(1)
            ratio = (log_prob - old_batch_log_probs).exp()
            unclipped = ratio * batch_advantages
            clipped_ratio = ratio.clamp(1.0 - clip_epsilon, 1.0 + clip_epsilon)
            clipped = clipped_ratio * batch_advantages
            policy_loss = -model._torch.minimum(unclipped, clipped)
            values = model.value_tensor(states)
            value_loss = (values - batch_returns).square()
            loss = (policy_loss + value_coef * value_loss - entropy_coef * entropy).mean()

            loss.backward()
            model._torch.nn.utils.clip_grad_norm_(model.parameters(), max_grad_norm)
            optimizer.step()

            batch_metrics = {
                "loss": float(loss.detach().cpu()),
                "policy_loss": float(policy_loss.mean().detach().cpu()),
                "value_loss": float(value_loss.mean().detach().cpu()),
                "entropy": float(entropy.mean().detach().cpu()),
                "approx_kl": float((old_batch_log_probs - log_prob).mean().detach().cpu()),
                "clip_fraction": float(
                    ((ratio - 1.0).abs() > clip_epsilon).to(model.dtype).mean().detach().cpu()
                ),
            }
            metrics.append(batch_metrics)
    mean = _mean_metric_rows(metrics)
    mean["batches"] = float(len(metrics))
    return mean


def _run_ppo_update_legacy(
    model: Any,
    *,
    arrays: dict[str, Any],
    old_log_probs: Sequence[float],
    advantages: Sequence[float],
    returns: Sequence[float],
    ppo_epochs: int,
    batch_size: int,
    learning_rate: float,
    clip_epsilon: float,
    entropy_coef: float,
    value_coef: float,
    max_grad_norm: float,
    seed: int,
) -> dict[str, float]:
    metrics: list[dict[str, float]] = []
    for epoch in range(ppo_epochs):
        for indices in _minibatch_indices(len(arrays["actions"]), batch_size, seed + epoch):
            gradients = _empty_gradients(model)
            batch_metrics = _empty_metric_row()
            for index in indices:
                state = arrays["states"][index]
                mask = arrays["legal_masks"][index]
                action = arrays["actions"][index]
                logits = model.logits(state, mask)
                log_probs, probabilities, entropy = _masked_log_probs(logits)
                log_prob = log_probs[action]
                ratio = math.exp(log_prob - old_log_probs[index])
                advantage = advantages[index]
                unclipped = ratio * advantage
                clipped_ratio = min(max(ratio, 1.0 - clip_epsilon), 1.0 + clip_epsilon)
                clipped = clipped_ratio * advantage
                active_policy_gradient = unclipped <= clipped
                policy_loss = -min(unclipped, clipped)
                value = model.value(state)
                value_error = value - returns[index]
                value_loss = value_error * value_error
                loss = policy_loss + value_coef * value_loss - entropy_coef * entropy

                policy_coeff = -advantage * ratio if active_policy_gradient else 0.0
                for candidate in _legal_indices(mask):
                    grad_logit = policy_coeff * (
                        float(candidate == action) - probabilities[candidate]
                    )
                    if entropy_coef:
                        grad_logit += (
                            entropy_coef
                            * probabilities[candidate]
                            * (log_probs[candidate] + entropy)
                        )
                    _accumulate_policy_gradient(gradients, state, candidate, grad_logit)
                value_coeff = 2.0 * value_coef * value_error
                _accumulate_value_gradient(gradients, state, value_coeff)

                batch_metrics["loss"] += loss
                batch_metrics["policy_loss"] += policy_loss
                batch_metrics["value_loss"] += value_loss
                batch_metrics["entropy"] += entropy
                batch_metrics["approx_kl"] += old_log_probs[index] - log_prob
                batch_metrics["clip_fraction"] += float(abs(ratio - 1.0) > clip_epsilon)
            _apply_gradients(
                model,
                gradients,
                learning_rate=learning_rate,
                batch_size=len(indices),
                max_grad_norm=max_grad_norm,
            )
            metrics.append({key: value / len(indices) for key, value in batch_metrics.items()})
    mean = _mean_metric_rows(metrics)
    mean["batches"] = float(len(metrics))
    return mean


def _empty_gradients(model: Any) -> dict[str, Any]:
    return {
        "policy_weights": [
            [0.0 for _feature in range(model.input_dim)] for _action in range(model.action_dim)
        ],
        "policy_bias": [0.0 for _action in range(model.action_dim)],
        "value_weights": [0.0 for _feature in range(model.input_dim)],
        "value_bias": 0.0,
    }


def _accumulate_policy_gradient(
    gradients: dict[str, Any],
    state: Sequence[float],
    action: int,
    grad_logit: float,
) -> None:
    gradients["policy_bias"][action] += grad_logit
    row = gradients["policy_weights"][action]
    for feature_index, feature in enumerate(state):
        row[feature_index] += grad_logit * float(feature)


def _accumulate_value_gradient(
    gradients: dict[str, Any],
    state: Sequence[float],
    value_coeff: float,
) -> None:
    gradients["value_bias"] += value_coeff
    for feature_index, feature in enumerate(state):
        gradients["value_weights"][feature_index] += value_coeff * float(feature)


def _apply_gradients(
    model: Any,
    gradients: dict[str, Any],
    *,
    learning_rate: float,
    batch_size: int,
    max_grad_norm: float,
) -> None:
    scale = 1.0 / batch_size
    norm_square = gradients["value_bias"] ** 2
    for value in gradients["value_weights"]:
        norm_square += value * value
    for value in gradients["policy_bias"]:
        norm_square += value * value
    for row in gradients["policy_weights"]:
        for value in row:
            norm_square += value * value
    norm = math.sqrt(norm_square) * scale
    if norm > max_grad_norm:
        scale *= max_grad_norm / (norm + 1.0e-12)

    model.value_bias -= learning_rate * gradients["value_bias"] * scale
    for index, value in enumerate(gradients["value_weights"]):
        model.value_weights[index] -= learning_rate * value * scale
    for action, value in enumerate(gradients["policy_bias"]):
        model.policy_bias[action] -= learning_rate * value * scale
    for action, row in enumerate(gradients["policy_weights"]):
        model_row = model.policy_weights[action]
        for feature_index, value in enumerate(row):
            model_row[feature_index] -= learning_rate * value * scale


def _masked_log_probs(logits: Sequence[float]) -> tuple[list[float], list[float], float]:
    legal_logits = [value for value in logits if value > -1.0e8]
    max_logit = max(legal_logits)
    legal_exp_sum = sum(math.exp(value - max_logit) for value in legal_logits)
    log_denominator = max_logit + math.log(legal_exp_sum)
    log_probs: list[float] = []
    probabilities: list[float] = []
    entropy = 0.0
    for value in logits:
        if value <= -1.0e8:
            log_probs.append(-1.0e9)
            probabilities.append(0.0)
            continue
        log_prob = value - log_denominator
        probability = math.exp(log_prob)
        log_probs.append(log_prob)
        probabilities.append(probability)
        entropy -= probability * log_prob
    return log_probs, probabilities, entropy


def _legal_indices(mask: Sequence[bool]) -> list[int]:
    return [index for index, legal in enumerate(mask) if legal]


def _argmax_legal(logits: Sequence[float]) -> int:
    best_index = 0
    best_value = float("-inf")
    for index, value in enumerate(logits):
        if value > best_value:
            best_index = index
            best_value = value
    return best_index


def _minibatch_indices(
    size: int,
    batch_size: int,
    seed: int,
) -> list[list[int]]:
    indices = list(range(size))
    random.Random(seed).shuffle(indices)
    return [indices[start : start + batch_size] for start in range(0, size, batch_size)]


def _empty_metric_row() -> dict[str, float]:
    return {
        "loss": 0.0,
        "policy_loss": 0.0,
        "value_loss": 0.0,
        "entropy": 0.0,
        "approx_kl": 0.0,
        "clip_fraction": 0.0,
    }


def _mean_metric_rows(rows: Sequence[dict[str, float]]) -> dict[str, float]:
    if not rows:
        return _empty_metric_row()
    keys = rows[0].keys()
    return {key: sum(row[key] for row in rows) / len(rows) for key in keys}


def _explained_variance(values: Sequence[float], returns: Sequence[float]) -> float | None:
    if not returns:
        return None
    mean_return = sum(returns) / len(returns)
    variance = sum((value - mean_return) ** 2 for value in returns) / len(returns)
    if variance <= 1.0e-12:
        return None
    residuals = [target - value for target, value in zip(returns, values, strict=True)]
    mean_residual = sum(residuals) / len(residuals)
    residual_variance = sum((value - mean_residual) ** 2 for value in residuals) / len(residuals)
    return 1.0 - residual_variance / variance


def _rollout_summary(report: dict[str, Any]) -> dict[str, Any]:
    return {
        "games": report["games"],
        "completed_games": report["completed_games"],
        "decisions": report["decisions"],
        "rounds": report["rounds"],
        "final_reasons": report["final_reasons"],
        "final_summary": report["final_summary"],
    }


def _ppo_report(
    *,
    seed: str,
    ruleset: str,
    device: str,
    model: SandboxLinearPpoActorCritic,
    requested_total_steps: int,
    starting_environment_steps: int,
    environment_steps: int,
    updates: int,
    rollout_games: int,
    max_rounds: int,
    max_turns_per_round: int,
    rollout_policies: dict[str, str],
    ppo_epochs: int,
    batch_size: int,
    learning_rate: float,
    gamma: float,
    gae_lambda: float,
    clip_epsilon: float,
    entropy_coef: float,
    value_coef: float,
    max_grad_norm: float,
    reward_scale: float,
    supervised_warmup_epochs: int,
    supervised_warmup_history: list[dict[str, Any]],
    history: list[dict[str, Any]],
    final_metrics: dict[str, Any],
    final_eval: dict[str, Any],
    final_rollout_summary: dict[str, Any],
    resume_checkpoint: Path | None,
) -> dict[str, Any]:
    return {
        "kind": PPO_SANDBOX_REPORT_KIND,
        "seed": seed,
        "ruleset": ruleset,
        "device": device,
        "model": {
            "kind": model.kind,
            "input_dim": model.input_dim,
            "hidden_dim": model.hidden_dim,
            "action_dim": model.action_dim,
        },
        "training": {
            "requested_total_steps": requested_total_steps,
            "starting_environment_steps": starting_environment_steps,
            "environment_steps": environment_steps,
            "updates": updates,
            "optimizer": "torch-sgd-v0",
            "learning_rate": learning_rate,
            "batch_size": batch_size,
            "ppo_epochs": ppo_epochs,
            "gamma": gamma,
            "gae_lambda": gae_lambda,
            "clip_epsilon": clip_epsilon,
            "entropy_coef": entropy_coef,
            "value_coef": value_coef,
            "max_grad_norm": max_grad_norm,
            "reward_scale": reward_scale,
            "supervised_warmup_epochs": supervised_warmup_epochs,
            "supervised_warmup_history": supervised_warmup_history,
            "resume_checkpoint": None if resume_checkpoint is None else str(resume_checkpoint),
            "history": history,
        },
        "rollout": {
            "games_per_update": rollout_games,
            "max_rounds": max_rounds,
            "max_turns_per_round": max_turns_per_round,
            "policies": rollout_policies,
            **final_rollout_summary,
        },
        "metrics": {
            "final": final_metrics,
            "evaluation": final_eval,
        },
        "training_history": normalize_training_history(
            history,
            step_key="update",
            step_unit="update",
            metric_roots=("losses", "evaluation"),
        ),
        "training_curves": _training_curves(history),
        "capabilities": {
            "ppo_policy_loss": True,
            "ppo_value_loss": True,
            "gae_advantages": True,
            "clipped_objective": True,
            "entropy_regularization": True,
            "minibatching": True,
            "checkpointing": True,
            "resume_support": True,
            "random_initialization": True,
            "supervised_warmup_initialization": supervised_warmup_epochs > 0,
            "evaluation_summaries": True,
            "training_curves": True,
            "learned_policy_environment_integration": False,
            "torch_nn_module": True,
            "torch_device": True,
        },
    }


def _training_curves(history: Sequence[dict[str, Any]]) -> dict[str, list[float]]:
    curves = {
        "loss": [],
        "policy_loss": [],
        "value_loss": [],
        "entropy": [],
        "approx_kl": [],
        "clip_fraction": [],
        "action_accuracy": [],
        "value_mse": [],
    }
    for row in history:
        losses = row["losses"]
        evaluation = row["evaluation"]
        for key in (
            "loss",
            "policy_loss",
            "value_loss",
            "entropy",
            "approx_kl",
            "clip_fraction",
        ):
            curves[key].append(float(losses[key]))
        curves["action_accuracy"].append(float(evaluation["action_accuracy"]))
        curves["value_mse"].append(float(evaluation["value_mse"]))
    return curves


def _format_counts(counts: dict[str, Any]) -> str:
    if not counts:
        return "none"
    return " ".join(f"{name}={count}" for name, count in sorted(counts.items()))
