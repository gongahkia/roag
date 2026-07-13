from __future__ import annotations

import copy
import importlib.util
import unittest
from dataclasses import replace

from kenjaku.core import Action, ActionKind, Tile
from kenjaku.models.multi_action_policy import MELD_SELECTION_DIM
from kenjaku.schema import LEGAL_ACTION_MASK_V1_RIICHI_INDEX, OBSERVATION_V1_TENSOR_DIM
from kenjaku.simulation import HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND
from kenjaku.training.behavior_distillation import (
    BehaviorDistillationExample,
    distillation_examples_from_manifest,
    resolve_action_kind_loss_weights,
    train_multi_task_behavior_distillation,
)

TORCH_AVAILABLE = importlib.util.find_spec("torch") is not None


class BehaviorDistillationExampleTests(unittest.TestCase):
    def test_builds_ranked_action_tile_and_meld_tasks_without_value_leakage(self) -> None:
        examples = distillation_examples_from_manifest(_manifest())

        self.assertEqual(len(examples), 3)
        self.assertEqual(
            tuple(example.family for example in examples),
            ("discard", "special_action", "call_pass"),
        )
        discard, special, call = examples
        self.assertEqual(discard.action_target, 0)
        self.assertEqual(discard.tile_action, "discard")
        self.assertEqual(discard.tile_target, 0)
        self.assertEqual(special.action_target, LEGAL_ACTION_MASK_V1_RIICHI_INDEX)
        self.assertIsNone(special.tile_target)
        self.assertEqual(call.tile_action, "chi")
        self.assertIsNotNone(call.meld_target)
        self.assertEqual(len(call.meld_mask or ()), MELD_SELECTION_DIM)
        self.assertTrue(all(example.value_target is None for example in examples))
        self.assertTrue(
            all(len(example.observation) == OBSERVATION_V1_TENSOR_DIM for example in examples)
        )

    def test_rejects_outcome_leakage_and_invalid_example_masks(self) -> None:
        leaked = copy.deepcopy(_manifest())
        leaked["trajectories"][0]["rewards"] = [1.0, -1.0, 0.0, 0.0]

        with self.assertRaisesRegex(ValueError, "future outcome leakage"):
            distillation_examples_from_manifest(leaked)
        with self.assertRaisesRegex(ValueError, "action_mask cannot be empty"):
            BehaviorDistillationExample(
                family="discard",
                observation=(0.0,) * OBSERVATION_V1_TENSOR_DIM,
                action_mask=(False,) * 276,
                action_target=0,
            )

    def test_resolves_explicit_and_inverse_frequency_action_weights(self) -> None:
        discard, special, _call = distillation_examples_from_manifest(_manifest())

        weights = resolve_action_kind_loss_weights(
            (discard, discard, special),
            action_kind_weights={"discard": 2.0},
            balance_action_kinds=True,
        )

        self.assertEqual(weights, {"discard": 1.5, "riichi": 1.5})
        with self.assertRaisesRegex(ValueError, "positive"):
            resolve_action_kind_loss_weights((discard,), action_kind_weights={"discard": 0.0})


@unittest.skipUnless(TORCH_AVAILABLE, "PyTorch is not available")
class BehaviorDistillationTrainerTests(unittest.TestCase):
    def test_trains_all_available_tasks_deterministically(self) -> None:
        examples = distillation_examples_from_manifest(_manifest())
        examples = (replace(examples[0], value_target=1.0), *examples[1:])

        first = train_multi_task_behavior_distillation(
            examples,
            epochs=1,
            batch_size=2,
            learning_rate=0.01,
            device="cpu",
            seed=5,
        )
        second = train_multi_task_behavior_distillation(
            examples,
            epochs=1,
            batch_size=2,
            learning_rate=0.01,
            device="cpu",
            seed=5,
        )

        self.assertEqual(first.device, "cpu")
        self.assertEqual(first.train_metrics["action_examples"], 3)
        self.assertEqual(first.train_metrics["tile_examples"], 2)
        self.assertEqual(first.train_metrics["meld_examples"], 1)
        self.assertEqual(first.train_metrics["value_examples"], 1)
        self.assertEqual(first.history, second.history)


def _manifest() -> dict[str, object]:
    discard = _action(ActionKind.DISCARD, "1m")
    discard_alternative = _action(ActionKind.DISCARD, "2m")
    riichi = _action(ActionKind.RIICHI)
    chi = _action(ActionKind.CHI, "3m", consumed=("1m", "2m"))
    pass_action = _action(ActionKind.PASS)
    return {
        "kind": HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND,
        "ruleset": "tenhou-4p",
        "players": 4,
        "source": {},
        "trajectory_count": 2,
        "trajectories": [
            {
                "game": 0,
                "round": 0,
                "step": 0,
                "seat": 0,
                "state": _state(players=4, seat=0, hand_size=14),
                "legal_actions": [discard, discard_alternative, riichi],
                "heuristic_rankings": {
                    "discard": [
                        _candidate(discard, rank=1),
                        _candidate(discard_alternative, rank=2),
                    ],
                    "call_pass": [],
                    "special_action": [_candidate(riichi, rank=1)],
                },
                "unranked_actions": [],
                "chosen_action": discard,
            },
            {
                "game": 0,
                "round": 0,
                "step": 1,
                "seat": 1,
                "state": _state(
                    players=4,
                    seat=1,
                    hand_size=13,
                    current_seat=0,
                    pending_discard="3m",
                    pending_discard_seat=0,
                    pending_reaction_seats=[1],
                ),
                "legal_actions": [chi, pass_action],
                "heuristic_rankings": {
                    "discard": [],
                    "call_pass": [_candidate(chi, rank=1)],
                    "special_action": [],
                },
                "unranked_actions": [pass_action],
                "chosen_action": pass_action,
            },
        ],
        "summary": {
            "family_candidate_counts": {"discard": 2, "call_pass": 1, "special_action": 1},
            "unranked_action_count": 1,
        },
    }


def _state(
    *,
    players: int,
    seat: int,
    hand_size: int,
    current_seat: int = 0,
    pending_discard: str | None = None,
    pending_discard_seat: int | None = None,
    pending_reaction_seats: list[int] | None = None,
) -> dict[str, object]:
    hands = [[] for _seat in range(players)]
    hands[seat] = (["1m", "2m", "3m", "4m", "5m", "6m", "7m"] * 2)[:hand_size]
    return {
        "turn": 0,
        "current_seat": current_seat,
        "round_wind": "E",
        "dealer_seat": 0,
        "honba": 0,
        "points": [25000] * players,
        "riichi_sticks": 0,
        "hands": hands,
        "hand_sizes": [len(hand) for hand in hands],
        "discards": [[] for _seat in range(players)],
        "melds": [[] for _seat in range(players)],
        "kita_tiles": [[] for _seat in range(players)],
        "dora_indicators": [],
        "wall_remaining": 1,
        "dead_wall_remaining": 0,
        "drawn_tile": None,
        "needs_discard": False,
        "rinshan_draw": False,
        "last_draw_was_final_live_wall": False,
        "pending_discard": pending_discard,
        "pending_discard_seat": pending_discard_seat,
        "pending_chankan_tile": None,
        "pending_chankan_seat": None,
        "pending_chankan_kind": None,
        "pending_kita_tile": None,
        "pending_kita_seat": None,
        "pending_abortive_draw_reason": None,
        "abortive_draw_after_discard_reason": None,
        "pending_reaction_seats": pending_reaction_seats or [],
        "riichi_seats": [],
        "double_riichi_seats": [],
        "riichi_pending_discard_seats": [],
        "ippatsu_seats": [],
        "terminal_reason": None,
        "game_finished": False,
    }


def _action(
    kind: ActionKind,
    tile: str | None = None,
    *,
    consumed: tuple[str, ...] = (),
) -> dict[str, object]:
    action = Action(
        kind,
        None if tile is None else Tile.parse(tile).type,
        consumed=tuple(Tile.parse(value) for value in consumed),
    )
    return {
        "kind": action.kind.value,
        "tile": None if action.tile is None else action.tile.notation,
        "tsumogiri": action.tsumogiri,
        "consumed": [value.notation for value in action.consumed],
    }


def _candidate(action: dict[str, object], *, rank: int) -> dict[str, object]:
    return {"action": copy.deepcopy(action), "rank": rank, "score": 1.0, "factors": []}


if __name__ == "__main__":
    unittest.main()
