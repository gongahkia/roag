from __future__ import annotations

import io
import json
import unittest
from contextlib import redirect_stdout
from tempfile import TemporaryDirectory

from kenjaku.commands._legacy import build_parser
from kenjaku.model_selection import select_model_on_frozen_split


class FrozenModelSelectionTests(unittest.TestCase):
    def test_selects_validation_winner_with_stable_tie_break(self) -> None:
        report = select_model_on_frozen_split(
            _split(),
            (
                {"model_id": "zeta", "validation": {"loss": 0.2, "accuracy": 0.8}},
                {"model_id": "alpha", "validation": {"loss": 0.2, "accuracy": 0.8}},
            ),
        )

        self.assertEqual(report["selected"], {"model_id": "alpha", "metric": 0.2})
        self.assertEqual(report["selection_split"], "validation")
        self.assertEqual(len(report["split_manifest_sha256"]), 64)

    def test_rejects_non_frozen_or_invalid_candidates(self) -> None:
        with self.assertRaisesRegex(ValueError, "partition"):
            select_model_on_frozen_split(
                {
                    **_split(),
                    "assignments": {"train": [0], "validation": [0], "test": [2]},
                },
                (),
            )
        with self.assertRaisesRegex(ValueError, "metric"):
            select_model_on_frozen_split(_split(), ({"model_id": "a", "validation": {}},))
        with self.assertRaisesRegex(ValueError, "object"):
            select_model_on_frozen_split(_split(), ("not-a-candidate",))

    def test_retains_sanma_ruleset(self) -> None:
        split = _split()
        split["source"] = {"ruleset": "tenhou-3p", "match_count": 3}

        report = select_model_on_frozen_split(
            split,
            ({"model_id": "sanma", "validation": {"accuracy": 0.75}},),
            metric="accuracy",
        )

        self.assertEqual(report["ruleset"], "tenhou-3p")
        self.assertEqual(report["selected"], {"model_id": "sanma", "metric": 0.75})

    def test_parser_registers_frozen_split_selection(self) -> None:
        args = build_parser().parse_args(
            ["select-model-frozen-split", "split.json", "candidates.json", "--metric", "accuracy"]
        )

        self.assertEqual(args.split_manifest.name, "split.json")
        self.assertEqual(args.candidates.name, "candidates.json")
        self.assertEqual(args.metric, "accuracy")

    def test_command_writes_deterministic_json(self) -> None:
        with TemporaryDirectory() as directory:
            split_path = f"{directory}/split.json"
            candidates_path = f"{directory}/candidates.json"
            with open(split_path, "w", encoding="utf-8") as output:
                json.dump(_split(), output)
            with open(candidates_path, "w", encoding="utf-8") as output:
                json.dump([{"model_id": "a", "validation": {"loss": 0.1}}], output)
            args = build_parser().parse_args(
                ["select-model-frozen-split", split_path, candidates_path, "--json"]
            )
            stdout = io.StringIO()
            with redirect_stdout(stdout):
                exit_code = args.func(args)

        self.assertEqual(exit_code, 0)
        self.assertEqual(json.loads(stdout.getvalue())["selected"]["model_id"], "a")


def _split() -> dict[str, object]:
    return {
        "kind": "kenjaku-synthetic-match-split-manifest-v1",
        "source": {"ruleset": "tenhou-4p", "match_count": 3},
        "split_seed": "fixed",
        "fractions": [0.8, 0.1, 0.1],
        "assignments": {"train": [0], "validation": [1], "test": [2]},
    }


if __name__ == "__main__":
    unittest.main()
