from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest import mock

from kenjaku.core import Action, ActionKind, Tile
from kenjaku.simulation import (
    HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_FIELDS,
    HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND,
    build_heuristic_distillation_trajectory_manifest,
    default_sandbox_rule_config,
    generate_heuristic_distillation_trajectory_manifest,
    write_heuristic_distillation_trajectory_manifest,
)
from kenjaku.simulation.synthetic_matches import SYNTHETIC_MATCH_MANIFEST_V1_KIND


class HeuristicDistillationTrajectoryTests(unittest.TestCase):
    def test_generates_deterministic_rule_specific_ranked_trajectories(self) -> None:
        for ruleset in ("tenhou-4p", "tenhou-3p"):
            with self.subTest(ruleset=ruleset):
                source = _source_manifest(ruleset)
                manifest = build_heuristic_distillation_trajectory_manifest(source)
                same_manifest = build_heuristic_distillation_trajectory_manifest(source)

                self.assertEqual(manifest, same_manifest)
                self.assertEqual(
                    tuple(manifest),
                    HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_FIELDS,
                )
                self.assertEqual(
                    manifest["kind"],
                    HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND,
                )
                self.assertEqual(manifest["ruleset"], ruleset)
                self.assertGreater(manifest["trajectory_count"], 0)
                self.assertEqual(manifest["trajectory_count"], len(manifest["trajectories"]))
                self.assertGreater(manifest["summary"]["family_candidate_counts"]["discard"], 0)
                for trajectory in manifest["trajectories"]:
                    legal_actions = trajectory["legal_actions"]
                    labelled_actions = [
                        candidate["action"]
                        for ranking in trajectory["heuristic_rankings"].values()
                        for candidate in ranking
                    ]
                    self.assertTrue(all(action in legal_actions for action in labelled_actions))
                    self.assertTrue(
                        all(
                            set(candidate) == {"action", "rank", "score", "factors"}
                            for ranking in trajectory["heuristic_rankings"].values()
                            for candidate in ranking
                        )
                    )

    def test_generator_uses_synthetic_match_source(self) -> None:
        source = _source_manifest("tenhou-4p")
        with mock.patch(
            "kenjaku.simulation.heuristic_trajectories.generate_synthetic_match_manifest",
            return_value=source,
        ) as generate_source:
            manifest = generate_heuristic_distillation_trajectory_manifest(
                match_count=1,
                seed="heuristic-trajectories",
                ruleset="tenhou-4p",
            )

        generate_source.assert_called_once_with(
            match_count=1,
            seed="heuristic-trajectories",
            ruleset="tenhou-4p",
            max_rounds=None,
            max_turns_per_round=512,
        )
        self.assertEqual(manifest, build_heuristic_distillation_trajectory_manifest(source))

    def test_build_write_and_invalid_source_handling(self) -> None:
        source = _source_manifest("tenhou-3p")
        manifest = build_heuristic_distillation_trajectory_manifest(source)
        with TemporaryDirectory() as directory:
            path = Path(directory) / "nested" / "heuristic-trajectories.json"
            write_heuristic_distillation_trajectory_manifest(path, manifest)
            self.assertEqual(json.loads(path.read_text(encoding="utf-8")), manifest)

        with self.assertRaisesRegex(ValueError, "not a synthetic"):
            build_heuristic_distillation_trajectory_manifest({})
        with self.assertRaisesRegex(ValueError, "not a heuristic"):
            write_heuristic_distillation_trajectory_manifest(Path("ignored.json"), {})


def _source_manifest(ruleset: str) -> dict[str, object]:
    config = default_sandbox_rule_config(ruleset)
    players = 3 if ruleset == "tenhou-3p" else 4
    base_state = {
        "turn": 0,
        "current_seat": 0,
        "round_wind": "E",
        "dealer_seat": 0,
        "honba": 0,
        "points": [config.initial_points] * players,
        "discards": [[] for _ in range(players)],
        "melds": [[] for _ in range(players)],
        "kita_tiles": [[] for _ in range(players)],
        "dora_indicators": [],
        "wall_remaining": 1,
        "drawn_tile": None,
        "needs_discard": False,
        "pending_discard": None,
        "pending_discard_seat": None,
        "pending_chankan_tile": None,
        "pending_chankan_seat": None,
        "pending_kita_tile": None,
        "pending_kita_seat": None,
        "pending_reaction_seats": [],
        "terminal_reason": None,
    }
    if ruleset == "tenhou-3p":
        hands = [
            ["1m", "9m", "1p", "1p", "2p", "2p", "3p", "3p", "4p", "4p", "5p", "5p", "N", "N"],
            [],
            [],
        ]
        decisions = [
            _decision(
                state={**base_state, "hands": hands, "hand_sizes": [14, 0, 0]},
                legal_actions=(
                    _action(ActionKind.KITA, "N", consumed=("N",)),
                    _action(ActionKind.DISCARD, "1m"),
                ),
                chosen_action=_action(ActionKind.KITA, "N", consumed=("N",)),
            )
        ]
    else:
        discard_hand = [
            "1m", "2m", "3m", "1p", "2p", "3p", "1s", "2s", "3s", "E", "E", "E", "5m", "9p"
        ]
        call_hand = ["5m", "5m", "1p", "2p", "3p", "4p", "5p", "6p", "7p", "1s", "2s", "3s", "E"]
        decisions = [
            _decision(
                state={
                    **base_state,
                    "hands": [discard_hand, [], [], []],
                    "hand_sizes": [14, 0, 0, 0],
                },
                legal_actions=(
                    _action(ActionKind.DISCARD, "5m"),
                    _action(ActionKind.RIICHI),
                ),
                chosen_action=_action(ActionKind.DISCARD, "5m"),
            ),
            _decision(
                state={
                    **base_state,
                    "hands": [[], call_hand, [], []],
                    "hand_sizes": [0, 13, 0, 0],
                    "pending_discard": "5m",
                    "pending_discard_seat": 0,
                    "pending_reaction_seats": [1],
                },
                seat=1,
                step=1,
                legal_actions=(
                    _action(ActionKind.PON, "5m", consumed=("5m", "5m")),
                    _action(ActionKind.PASS),
                ),
                chosen_action=_action(ActionKind.PASS),
            ),
        ]
    return {
        "kind": SYNTHETIC_MATCH_MANIFEST_V1_KIND,
        "ruleset": ruleset,
        "players": players,
        "match_count": 1,
        "provenance": {"rule_config": config.to_versioned_payload()},
        "matches": [{"game": 0, "completed": True, "trajectory": decisions}],
    }


def _decision(
    *,
    state: dict[str, object],
    legal_actions: tuple[dict[str, object], ...],
    chosen_action: dict[str, object],
    seat: int = 0,
    step: int = 0,
) -> dict[str, object]:
    return {
        "round": 0,
        "step": step,
        "seat": seat,
        "state": state,
        "legal_actions": list(legal_actions),
        "chosen_action": chosen_action,
        "rewards": [0.0] * len(state["hands"]),
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
        consumed=tuple(Tile.parse(item) for item in consumed),
    )
    return {
        "kind": action.kind.value,
        "tile": None if action.tile is None else action.tile.notation,
        "tsumogiri": action.tsumogiri,
        "consumed": [item.notation for item in action.consumed],
    }


if __name__ == "__main__":
    unittest.main()
