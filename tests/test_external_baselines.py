from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.core import ActionKind
from kenjaku.training.decision_snapshots import DECISION_SNAPSHOT_KIND
from kenjaku.training.external_baselines import (
    EXTERNAL_BASELINE_REPORT_KIND,
    build_external_baseline_report,
    format_external_baseline_report,
    parse_external_baseline_spec,
)


class ExternalBaselineTests(unittest.TestCase):
    def test_parse_external_baseline_spec_accepts_family_and_default_family(self) -> None:
        spec = parse_external_baseline_spec("mortal:v1=predictions.jsonl")
        fallback = parse_external_baseline_spec("baseline=predictions.jsonl")

        self.assertEqual(
            (spec.family, spec.name, spec.predictions_path),
            ("mortal", "v1", Path("predictions.jsonl")),
        )
        self.assertEqual((fallback.family, fallback.name), ("unspecified", "baseline"))

    def test_parse_external_baseline_spec_rejects_malformed_values(self) -> None:
        for value in ("missing-equals", "=path", "family:=path", "name="):
            with self.subTest(value=value), self.assertRaises(ValueError):
                parse_external_baseline_spec(value)

    def test_build_external_baseline_report_scores_predictions(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = _write_jsonl(root / "snapshots.jsonl", [_snapshot("r1")])
            predictions = _write_jsonl(root / "predictions.jsonl", [_prediction("r1")])
            spec = parse_external_baseline_spec(f"unit:model={predictions}")
            report = build_external_baseline_report(
                snapshots, [spec], minimum_comparable_decisions=1
            )

        baseline = report["baselines"][0]
        self.assertEqual(report["kind"], EXTERNAL_BASELINE_REPORT_KIND)
        self.assertEqual(baseline["overall"]["accuracy"], 1.0)
        self.assertEqual(baseline["illegal_predictions"], 0)

    def test_build_external_baseline_report_tracks_missing_illegal_and_malformed(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = _write_jsonl(root / "snapshots.jsonl", [_snapshot("r1"), _snapshot("r2")])
            predictions = _write_jsonl(
                root / "predictions.jsonl",
                [_prediction("r1", tile="9m"), {"bad": True}, _prediction("r1")],
            )
            spec = parse_external_baseline_spec(f"unit:model={predictions}")
            report = build_external_baseline_report(
                snapshots, [spec], minimum_comparable_decisions=1
            )

        baseline = report["baselines"][0]
        self.assertEqual(baseline["missing_predictions"], 1)
        self.assertEqual(baseline["illegal_predictions"], 1)
        self.assertEqual(baseline["malformed_prediction_rows"], 1)
        self.assertEqual(baseline["duplicate_prediction_rows"], 1)

    def test_build_external_baseline_report_enforces_minimum_and_specs(self) -> None:
        with TemporaryDirectory() as directory:
            snapshots = _write_jsonl(Path(directory) / "snapshots.jsonl", [_snapshot("r1")])
            predictions = _write_jsonl(Path(directory) / "predictions.jsonl", [_prediction("r1")])
            spec = parse_external_baseline_spec(f"unit:model={predictions}")
            with self.assertRaisesRegex(ValueError, "at least one"):
                build_external_baseline_report(snapshots, [], minimum_comparable_decisions=1)
            with self.assertRaisesRegex(ValueError, "below minimum"):
                build_external_baseline_report(snapshots, [spec], minimum_comparable_decisions=2)

    def test_format_external_baseline_report_includes_binary_metrics(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = _write_jsonl(
                root / "snapshots.jsonl",
                [
                    _snapshot("r1", decision_type="call", kind="pass"),
                    _snapshot("r2", decision_type="call", kind="chi"),
                ],
            )
            predictions = _write_jsonl(
                root / "predictions.jsonl",
                [_prediction("r1", kind="pass"), _prediction("r2", kind="chi")],
            )
            spec = parse_external_baseline_spec(f"unit:model={predictions}")
            report = build_external_baseline_report(
                snapshots, [spec], minimum_comparable_decisions=1
            )
            text = format_external_baseline_report(report)
            call_binary = report["baselines"][0]["binary"]["call"]

        self.assertIn("minimum_satisfied: yes", text)
        self.assertIn("binary:", text)
        self.assertEqual(call_binary["balanced_accuracy"], 1.0)
        self.assertIn("balanced=1.0000", text)
        self.assertIn("pass_recall=1.0000", text)


def _snapshot(
    row_id: str,
    *,
    decision_type: str = "discard",
    kind: str = "discard",
    tile: str = "1m",
) -> dict[str, object]:
    action = {"kind": kind}
    if tile:
        action["tile"] = tile
    if kind == ActionKind.DISCARD.value:
        action["tsumogiri"] = False
    return {
        "kind": DECISION_SNAPSHOT_KIND,
        "row_id": row_id,
        "decision_type": decision_type,
        "source": {"label": "unit"},
        "actual_action": action,
        "legal_actions": [action],
        "mjai_events": [{"type": "start_kyoku"}],
    }


def _prediction(row_id: str, *, kind: str = "discard", tile: str = "1m") -> dict[str, object]:
    action = {"kind": kind}
    if tile:
        action["tile"] = tile
    if kind == ActionKind.DISCARD.value:
        action["tsumogiri"] = False
    return {"row_id": row_id, "predicted_action": action}


def _write_jsonl(path: Path, rows: list[dict[str, object]]) -> Path:
    path.write_text("\n".join(json.dumps(row) for row in rows) + "\n", encoding="utf-8")
    return path


if __name__ == "__main__":
    unittest.main()
