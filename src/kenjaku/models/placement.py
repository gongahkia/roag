from __future__ import annotations

import json
from collections.abc import Sequence
from dataclasses import dataclass
from math import exp, log
from pathlib import Path
from typing import Any

from kenjaku.io import TenhouGame, TenhouRound, iter_tenhou_xml_dataset_files

PLACEMENT_MODEL_KIND = "kenjaku-placement-linear-v0"
PLACEMENT_CHECKPOINT_KIND = "kenjaku-placement-checkpoint-v0"
PLACEMENT_DISCLAIMER = "sandbox estimate, not published placement stats"
ROUND_WIND_LABELS = ("E", "S", "W", "N")
PLACEMENT_FEATURE_NAMES = (
    "bias",
    "seat_0",
    "seat_1",
    "seat_2",
    "seat_3",
    "seat_is_dealer",
    "dealer_left",
    "dealer_across",
    "dealer_right",
    "round_wind_east",
    "round_wind_south",
    "round_wind_west",
    "round_wind_north",
    "kyoku_1",
    "kyoku_2",
    "kyoku_3",
    "kyoku_4",
    "honba",
    "kyotaku",
    "seat_score",
    "seat_score_rank",
    "seat_lead",
    "seat_trail",
    "score_delta_left",
    "score_delta_across",
    "score_delta_right",
    "score_sum",
)
PLACEMENT_FEATURE_DIM = len(PLACEMENT_FEATURE_NAMES)
PLACEMENT_OUTPUTS = 4


@dataclass(frozen=True, slots=True)
class PlacementExample:
    scores: tuple[int, int, int, int]
    round_wind: int
    kyoku: int
    honba: int
    kyotaku: int
    dealer: int
    seat: int
    target_rank: int

    def __post_init__(self) -> None:
        _validate_scores(self.scores)
        _validate_game_state(
            round_wind=self.round_wind,
            kyoku=self.kyoku,
            honba=self.honba,
            kyotaku=self.kyotaku,
            dealer=self.dealer,
            seat=self.seat,
        )
        if not 0 <= self.target_rank < PLACEMENT_OUTPUTS:
            raise ValueError("target rank must be 0..3")


@dataclass(frozen=True, slots=True)
class PlacementModel:
    weights: tuple[tuple[float, ...], ...]
    epochs: int
    learning_rate: float
    l2: float = 0.0

    def __post_init__(self) -> None:
        if len(self.weights) != PLACEMENT_OUTPUTS:
            raise ValueError("placement model must have four output rows")
        if any(len(row) != PLACEMENT_FEATURE_DIM for row in self.weights):
            raise ValueError(f"placement model rows must have {PLACEMENT_FEATURE_DIM} features")
        if self.epochs < 0:
            raise ValueError("epochs must be non-negative")
        if self.learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if self.l2 < 0:
            raise ValueError("l2 must be non-negative")

    @classmethod
    def fit(
        cls,
        examples: Sequence[PlacementExample],
        *,
        epochs: int = 50,
        learning_rate: float = 0.05,
        l2: float = 0.0,
    ) -> PlacementModel:
        if not examples:
            raise ValueError("cannot train on zero placement examples")
        if epochs < 0:
            raise ValueError("epochs must be non-negative")
        if learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if l2 < 0:
            raise ValueError("l2 must be non-negative")

        weights = [[0.0] * PLACEMENT_FEATURE_DIM for _ in range(PLACEMENT_OUTPUTS)]
        prepared = tuple(_PreparedPlacementExample.from_example(example) for example in examples)
        for _ in range(epochs):
            for example in prepared:
                _apply_update(weights, example, learning_rate=learning_rate, l2=l2)
        return cls(
            weights=tuple(tuple(row) for row in weights),
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
        )

    @classmethod
    def default(cls) -> PlacementModel:
        return cls(
            weights=tuple(tuple(0.0 for _ in range(PLACEMENT_FEATURE_DIM)) for _ in range(4)),
            epochs=0,
            learning_rate=0.05,
        )

    @property
    def kind(self) -> str:
        return PLACEMENT_MODEL_KIND

    @property
    def feature_dim(self) -> int:
        return PLACEMENT_FEATURE_DIM

    @property
    def feature_names(self) -> tuple[str, ...]:
        return PLACEMENT_FEATURE_NAMES

    def predict_probabilities(
        self,
        *,
        scores: tuple[int, int, int, int],
        round_wind: int,
        kyoku: int,
        honba: int = 0,
        kyotaku: int = 0,
        dealer: int = 0,
        seat: int = 0,
    ) -> tuple[float, float, float, float]:
        features = placement_features(
            scores=scores,
            round_wind=round_wind,
            kyoku=kyoku,
            honba=honba,
            kyotaku=kyotaku,
            dealer=dealer,
            seat=seat,
        )
        return _softmax(tuple(_dot(row, features) for row in self.weights))

    def evaluate(self, examples: Sequence[PlacementExample]) -> dict[str, Any]:
        if not examples:
            raise ValueError("cannot evaluate zero placement examples")
        correct = 0
        brier_sum = 0.0
        log_loss_sum = 0.0
        for example in examples:
            probabilities = self.predict_probabilities(
                scores=example.scores,
                round_wind=example.round_wind,
                kyoku=example.kyoku,
                honba=example.honba,
                kyotaku=example.kyotaku,
                dealer=example.dealer,
                seat=example.seat,
            )
            predicted = max(range(PLACEMENT_OUTPUTS), key=lambda index: probabilities[index])
            correct += predicted == example.target_rank
            for rank, probability in enumerate(probabilities):
                target = 1.0 if rank == example.target_rank else 0.0
                brier_sum += (probability - target) ** 2
            p = _clamped_probability(probabilities[example.target_rank])
            log_loss_sum += -log(p)
        return {
            "examples": len(examples),
            "accuracy": correct / len(examples),
            "brier_score": brier_sum / len(examples),
            "log_loss": log_loss_sum / len(examples),
        }

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": PLACEMENT_MODEL_KIND,
            "feature_dim": PLACEMENT_FEATURE_DIM,
            "feature_names": list(PLACEMENT_FEATURE_NAMES),
            "outputs": ["first", "second", "third", "fourth"],
            "epochs": self.epochs,
            "learning_rate": self.learning_rate,
            "l2": self.l2,
            "weights": [list(row) for row in self.weights],
            "disclaimer": PLACEMENT_DISCLAIMER,
        }

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> PlacementModel:
        if payload.get("kind") not in {PLACEMENT_MODEL_KIND, PLACEMENT_CHECKPOINT_KIND}:
            raise ValueError("unsupported placement model kind")
        model_payload = (
            payload.get("model")
            if payload.get("kind") == PLACEMENT_CHECKPOINT_KIND
            else payload
        )
        if not isinstance(model_payload, dict):
            raise ValueError("placement checkpoint missing model object")
        if model_payload.get("kind") != PLACEMENT_MODEL_KIND:
            raise ValueError("unsupported placement model kind")
        if model_payload.get("feature_dim") != PLACEMENT_FEATURE_DIM:
            raise ValueError("unsupported placement feature dimension")
        weights_payload = model_payload.get("weights")
        if not isinstance(weights_payload, list):
            raise ValueError("placement model missing weights")
        weights = tuple(
            tuple(float(value) for value in row)
            for row in weights_payload
            if isinstance(row, list)
        )
        return cls(
            weights=weights,
            epochs=int(model_payload["epochs"]),
            learning_rate=float(model_payload["learning_rate"]),
            l2=float(model_payload.get("l2", 0.0)),
        )

    def save(self, path: str | Path, *, metadata: dict[str, Any] | None = None) -> None:
        checkpoint = {
            "kind": PLACEMENT_CHECKPOINT_KIND,
            "model": self.to_dict(),
            "metadata": metadata or {},
            "disclaimer": PLACEMENT_DISCLAIMER,
        }
        Path(path).parent.mkdir(parents=True, exist_ok=True)
        Path(path).write_text(
            json.dumps(checkpoint, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )

    @classmethod
    def load(cls, path: str | Path) -> PlacementModel:
        payload = json.loads(Path(path).read_text(encoding="utf-8"))
        if not isinstance(payload, dict):
            raise ValueError("placement artifact must contain a JSON object")
        return cls.from_dict(payload)


@dataclass(frozen=True, slots=True)
class _PreparedPlacementExample:
    features: tuple[float, ...]
    target_rank: int

    @classmethod
    def from_example(cls, example: PlacementExample) -> _PreparedPlacementExample:
        return cls(
            features=placement_features(
                scores=example.scores,
                round_wind=example.round_wind,
                kyoku=example.kyoku,
                honba=example.honba,
                kyotaku=example.kyotaku,
                dealer=example.dealer,
                seat=example.seat,
            ),
            target_rank=example.target_rank,
        )


def placement_examples_from_paths(paths: Sequence[str | Path]) -> tuple[PlacementExample, ...]:
    examples: list[PlacementExample] = []
    for parsed in iter_tenhou_xml_dataset_files(paths):
        examples.extend(placement_examples_from_game(parsed.game))
    return tuple(examples)


def placement_examples_from_game(game: TenhouGame) -> tuple[PlacementExample, ...]:
    if not game.rounds:
        return ()
    final_scores = _final_scores(game)
    ranks = _ranks_for_scores(final_scores)
    examples: list[PlacementExample] = []
    for round_ in game.rounds:
        if len(round_.scores) != 4:
            continue
        scores = _scores4(round_.scores)
        for seat in range(4):
            examples.append(
                PlacementExample(
                    scores=scores,
                    round_wind=round_.round_wind,
                    kyoku=round_.kyoku,
                    honba=round_.honba,
                    kyotaku=round_.kyotaku,
                    dealer=round_.dealer,
                    seat=seat,
                    target_rank=ranks[seat] - 1,
                )
            )
    return tuple(examples)


def placement_features(
    *,
    scores: tuple[int, int, int, int],
    round_wind: int,
    kyoku: int,
    honba: int = 0,
    kyotaku: int = 0,
    dealer: int = 0,
    seat: int = 0,
) -> tuple[float, ...]:
    _validate_scores(scores)
    _validate_game_state(
        round_wind=round_wind,
        kyoku=kyoku,
        honba=honba,
        kyotaku=kyotaku,
        dealer=dealer,
        seat=seat,
    )
    seat_score = scores[seat]
    ordered = _placement_order(scores)
    seat_rank = ordered.index(seat)
    other_scores = [scores[(seat + offset) % 4] for offset in (1, 2, 3)]
    return (
        1.0,
        *(1.0 if seat == index else 0.0 for index in range(4)),
        1.0 if seat == dealer else 0.0,
        *(1.0 if (dealer - seat) % 4 == offset else 0.0 for offset in (1, 2, 3)),
        *(1.0 if round_wind == index else 0.0 for index in range(4)),
        *(1.0 if kyoku == index else 0.0 for index in range(1, 5)),
        honba / 8.0,
        kyotaku / 4.0,
        seat_score / 100000.0,
        seat_rank / 3.0,
        (seat_score - max(score for index, score in enumerate(scores) if index != seat)) / 100000.0,
        (seat_score - min(score for index, score in enumerate(scores) if index != seat)) / 100000.0,
        *((seat_score - score) / 100000.0 for score in other_scores),
        sum(scores) / 100000.0,
    )


def parse_scores(text: str) -> tuple[int, int, int, int]:
    values = tuple(int(part.strip()) for part in text.split(",") if part.strip())
    if len(values) != 4:
        raise ValueError("--scores must contain four comma-separated integer scores")
    return _scores4(values)


def parse_kyoku(text: str) -> tuple[int, int]:
    normalized = text.strip().upper().replace(" ", "").replace("-", "").replace("_", "")
    for long_name, short_name in (
        ("EAST", "E"),
        ("SOUTH", "S"),
        ("WEST", "W"),
        ("NORTH", "N"),
    ):
        normalized = normalized.replace(long_name, short_name)
    if len(normalized) < 2:
        raise ValueError("kyoku must look like E1, S4, East1, or South-2")
    wind = normalized[0]
    if wind not in ROUND_WIND_LABELS:
        raise ValueError("kyoku wind must be E, S, W, or N")
    try:
        kyoku = int(normalized[1:])
    except ValueError as error:
        raise ValueError("kyoku number must be 1..4") from error
    if not 1 <= kyoku <= 4:
        raise ValueError("kyoku number must be 1..4")
    return (ROUND_WIND_LABELS.index(wind), kyoku)


def build_placement_checkpoint_metadata(
    *,
    examples: Sequence[PlacementExample],
    model: PlacementModel,
    input_paths: Sequence[str | Path],
) -> dict[str, Any]:
    metrics = model.evaluate(examples)
    return {
        "examples": len(examples),
        "input_paths": [str(path) for path in input_paths],
        "metrics": metrics,
        "disclaimer": PLACEMENT_DISCLAIMER,
    }


def format_placement_training_report(
    *,
    checkpoint_path: Path,
    metadata: dict[str, Any],
) -> str:
    metrics = metadata["metrics"]
    return "\n".join(
        [
            f"kind: {PLACEMENT_CHECKPOINT_KIND}",
            f"examples: {metadata['examples']}",
            f"train_accuracy: {_format_float(metrics['accuracy'])}",
            f"train_brier_score: {_format_float(metrics['brier_score'])}",
            f"checkpoint_path: {checkpoint_path}",
            f"disclaimer: {PLACEMENT_DISCLAIMER}",
        ]
    )


def format_placement_probability_text(payload: dict[str, Any]) -> str:
    probabilities = payload["probabilities"]
    labels = ("first", "second", "third", "fourth")
    lines = [
        f"seat: {payload['seat']}",
        f"kyoku: {payload['kyoku']}",
        "probabilities:",
    ]
    lines.extend(
        f"  {label}: {_format_float(probability)}"
        for label, probability in zip(labels, probabilities, strict=True)
    )
    lines.append(f"sum: {_format_float(sum(probabilities))}")
    lines.append(f"disclaimer: {PLACEMENT_DISCLAIMER}")
    return "\n".join(lines)


def placement_probability_payload(
    model: PlacementModel,
    *,
    scores: tuple[int, int, int, int],
    round_wind: int,
    kyoku: int,
    honba: int = 0,
    kyotaku: int = 0,
    dealer: int = 0,
    seat: int = 0,
    model_path: Path | None = None,
) -> dict[str, Any]:
    probabilities = model.predict_probabilities(
        scores=scores,
        round_wind=round_wind,
        kyoku=kyoku,
        honba=honba,
        kyotaku=kyotaku,
        dealer=dealer,
        seat=seat,
    )
    return {
        "kind": "kenjaku-placement-probability-v0",
        "model_kind": model.kind,
        "model_path": None if model_path is None else str(model_path),
        "seat": seat,
        "scores": list(scores),
        "kyoku": f"{ROUND_WIND_LABELS[round_wind]}{kyoku}",
        "round_wind": ROUND_WIND_LABELS[round_wind],
        "honba": honba,
        "kyotaku": kyotaku,
        "dealer": dealer,
        "probabilities": list(probabilities),
        "probability_sum": sum(probabilities),
        "disclaimer": PLACEMENT_DISCLAIMER,
    }


def _final_scores(game: TenhouGame) -> tuple[int, int, int, int]:
    scores = _scores4(game.rounds[0].scores)
    for round_ in game.rounds:
        scores = _round_end_scores(round_, current_scores=_scores4(round_.scores))
    return scores


def _round_end_scores(
    round_: TenhouRound,
    *,
    current_scores: tuple[int, int, int, int],
) -> tuple[int, int, int, int]:
    scores = current_scores
    for agari in round_.agari:
        if agari.score_deltas is not None and len(agari.score_deltas) == 4:
            scores = tuple(scores[index] + agari.score_deltas[index] for index in range(4))
    if round_.ryuukyoku is not None:
        if round_.ryuukyoku.scores is not None and len(round_.ryuukyoku.scores) == 4:
            return _scores4(round_.ryuukyoku.scores)
        if round_.ryuukyoku.score_deltas is not None and len(round_.ryuukyoku.score_deltas) == 4:
            return tuple(
                scores[index] + round_.ryuukyoku.score_deltas[index] for index in range(4)
            )
    return scores


def _ranks_for_scores(scores: tuple[int, int, int, int]) -> tuple[int, int, int, int]:
    ranks = [0, 0, 0, 0]
    for rank, seat in enumerate(_placement_order(scores), start=1):
        ranks[seat] = rank
    return (ranks[0], ranks[1], ranks[2], ranks[3])


def _placement_order(scores: tuple[int, int, int, int]) -> tuple[int, int, int, int]:
    return tuple(sorted(range(4), key=lambda seat: (-scores[seat], seat)))


def _validate_scores(scores: tuple[int, int, int, int]) -> None:
    if len(scores) != 4:
        raise ValueError("placement model only supports four-player scores")
    if any(not isinstance(score, int) for score in scores):
        raise ValueError("scores must be integers")


def _validate_game_state(
    *,
    round_wind: int,
    kyoku: int,
    honba: int,
    kyotaku: int,
    dealer: int,
    seat: int,
) -> None:
    if not 0 <= round_wind < 4:
        raise ValueError("round_wind must be 0..3")
    if not 1 <= kyoku <= 4:
        raise ValueError("kyoku must be 1..4")
    if honba < 0:
        raise ValueError("honba must be non-negative")
    if kyotaku < 0:
        raise ValueError("kyotaku must be non-negative")
    if not 0 <= dealer < 4:
        raise ValueError("dealer must be 0..3")
    if not 0 <= seat < 4:
        raise ValueError("seat must be 0..3")


def _scores4(scores: Sequence[int]) -> tuple[int, int, int, int]:
    if len(scores) != 4:
        raise ValueError("placement model only supports four-player scores")
    return (int(scores[0]), int(scores[1]), int(scores[2]), int(scores[3]))


def _apply_update(
    weights: list[list[float]],
    example: _PreparedPlacementExample,
    *,
    learning_rate: float,
    l2: float,
) -> None:
    logits = tuple(_dot(row, example.features) for row in weights)
    probabilities = _softmax(logits)
    for output in range(PLACEMENT_OUTPUTS):
        target = 1.0 if output == example.target_rank else 0.0
        error = probabilities[output] - target
        for index, value in enumerate(example.features):
            gradient = error * value + l2 * weights[output][index]
            weights[output][index] -= learning_rate * gradient


def _softmax(logits: Sequence[float]) -> tuple[float, float, float, float]:
    offset = max(logits)
    exps = [exp(value - offset) for value in logits]
    total = sum(exps)
    probabilities = tuple(value / total for value in exps)
    return (probabilities[0], probabilities[1], probabilities[2], probabilities[3])


def _dot(weights: Sequence[float], features: Sequence[float]) -> float:
    return sum(weight * feature for weight, feature in zip(weights, features, strict=True))


def _clamped_probability(value: float) -> float:
    return min(max(value, 1e-12), 1.0 - 1e-12)


def _format_float(value: float) -> str:
    return f"{value:.4f}"
