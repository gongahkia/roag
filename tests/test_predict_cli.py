from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.cli import main
from kenjaku.models import (
    CALL_DECISION_KINDS,
    CALL_LINEAR_FEATURE_DIM,
    DEAL_IN_LINEAR_FEATURE_DIM,
    RIICHI_DECISION_KINDS,
    RIICHI_LINEAR_FEATURE_DIM,
    CallLinearModel,
    DealInLinearModel,
    DiscardLinearModel,
    RiichiLinearModel,
)
from kenjaku.models.linear_discard import RAW_COUNT_FEATURE_DIM, RAW_COUNT_FEATURE_PROFILE


class PredictCliTests(unittest.TestCase):
    def test_predict_frequency_round_trips_through_snapshot_compare(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = self._export_snapshots(root, decision_types=None, limit=5)
            predictions = root / "predictions.jsonl"

            stdout = self._predict(
                snapshots=snapshots,
                predictions=predictions,
                model="frequency",
            )
            compare = self._compare(snapshots, predictions)

        self.assertIn("model: frequency", stdout)
        self.assertEqual(compare["predictions"], 5)
        self.assertEqual(compare["missing_predictions"], 0)

    def test_predict_linear_discard_checkpoint(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = self._export_snapshots(root, decision_types="discard", limit=2)
            checkpoint = root / "discard-linear.json"
            predictions = root / "predictions.jsonl"
            self._write_discard_linear_checkpoint(checkpoint)

            stdout = self._predict(
                snapshots=snapshots,
                predictions=predictions,
                model="linear-discard",
                checkpoint=checkpoint,
            )
            rows = self._prediction_rows(predictions)

        self.assertIn("model: linear-discard", stdout)
        self.assertEqual(len(rows), 2)
        self.assertTrue(all(row["predicted_action"]["kind"] == "discard" for row in rows))

    def test_predict_linear_call_checkpoint(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = self._export_snapshots(root, decision_types="call", limit=1)
            checkpoint = root / "call-linear.json"
            predictions = root / "predictions.jsonl"
            self._write_call_linear_checkpoint(checkpoint)

            stdout = self._predict(
                snapshots=snapshots,
                predictions=predictions,
                model="linear-call",
                checkpoint=checkpoint,
            )
            rows = self._prediction_rows(predictions)

        self.assertIn("model: linear-call", stdout)
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["predicted_action"]["kind"], "pass")

    def test_predict_linear_riichi_checkpoint(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = self._export_snapshots(root, decision_types="riichi", limit=2)
            checkpoint = root / "riichi-linear.json"
            predictions = root / "predictions.jsonl"
            self._write_riichi_linear_checkpoint(checkpoint)

            stdout = self._predict(
                snapshots=snapshots,
                predictions=predictions,
                model="linear-riichi",
                checkpoint=checkpoint,
            )
            rows = self._prediction_rows(predictions)

        self.assertIn("model: linear-riichi", stdout)
        self.assertEqual(len(rows), 2)
        self.assertTrue(all(row["predicted_action"]["kind"] == "pass" for row in rows))

    def test_predict_linear_deal_in_checkpoint(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = self._export_snapshots(root, decision_types="discard", limit=2)
            checkpoint = root / "deal-in-linear.json"
            predictions = root / "predictions.jsonl"
            self._write_deal_in_checkpoint(checkpoint)

            stdout = self._predict(
                snapshots=snapshots,
                predictions=predictions,
                model="linear-deal-in",
                checkpoint=checkpoint,
            )
            rows = self._prediction_rows(predictions)

        self.assertIn("model: linear-deal-in", stdout)
        self.assertEqual(len(rows), 2)
        self.assertTrue(all(row["predicted_action"]["kind"] == "discard" for row in rows))

    def test_predict_mlp_discard_checkpoint(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        torch = __import__("torch")
        from kenjaku.models.torch_discard import DISCARD_MLP_CHECKPOINT_KIND, DiscardMlp

        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = self._export_snapshots(root, decision_types="discard", limit=1)
            checkpoint = root / "mlp.pt"
            predictions = root / "predictions.jsonl"
            model = DiscardMlp(hidden_dim=8)
            torch.save(
                {
                    "kind": DISCARD_MLP_CHECKPOINT_KIND,
                    "model": {
                        "kind": model.kind,
                        "input_dim": model.input_dim,
                        "hidden_dim": model.hidden_dim,
                        "output_dim": model.output_dim,
                    },
                    "model_state_dict": model.state_dict(),
                },
                checkpoint,
            )

            stdout = self._predict(
                snapshots=snapshots,
                predictions=predictions,
                model="mlp-discard",
                checkpoint=checkpoint,
                extra=("--device", "cpu", "--batch-size", "1"),
            )
            rows = self._prediction_rows(predictions)

        self.assertIn("model: mlp-discard", stdout)
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["predicted_action"]["kind"], "discard")

    def test_predict_transformer_discard_checkpoint(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        torch = __import__("torch")
        from kenjaku.models.torch_transformer import (
            DISCARD_TRANSFORMER_CHECKPOINT_KIND,
            DiscardTransformerPolicy,
            MahjongTransformerConfig,
            transformer_config_payload,
        )

        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = self._export_snapshots(root, decision_types="discard", limit=1)
            checkpoint = root / "transformer.pt"
            predictions = root / "predictions.jsonl"
            config = MahjongTransformerConfig(
                model_dim=16,
                num_heads=4,
                num_layers=1,
                feedforward_dim=32,
                dropout=0.0,
            )
            model = DiscardTransformerPolicy(config)
            torch.save(
                {
                    "kind": DISCARD_TRANSFORMER_CHECKPOINT_KIND,
                    "model": {
                        "kind": model.kind,
                        "encoder_kind": model.encoder.kind,
                        "input_tokens": model.input_tokens,
                        "output_dim": model.output_dim,
                        "value_head": model.has_value_head,
                        "config": transformer_config_payload(config),
                    },
                    "model_state_dict": model.state_dict(),
                },
                checkpoint,
            )

            stdout = self._predict(
                snapshots=snapshots,
                predictions=predictions,
                model="transformer-discard",
                checkpoint=checkpoint,
                extra=("--device", "cpu", "--batch-size", "1"),
            )
            rows = self._prediction_rows(predictions)

        self.assertIn("model: transformer-discard", stdout)
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["predicted_action"]["kind"], "discard")

    def _export_snapshots(
        self,
        root: Path,
        *,
        decision_types: str | None,
        limit: int,
    ) -> Path:
        snapshots = root / f"{decision_types or 'mixed'}-snapshots.jsonl"
        argv = [
            "export-decision-snapshots",
            "data/fixtures/tenhou",
            "--limit",
            str(limit),
            "--output",
            str(snapshots),
        ]
        if decision_types is not None:
            argv.extend(["--decision-types", decision_types])
        with contextlib.redirect_stdout(io.StringIO()):
            exit_code = main(argv)
        self.assertEqual(exit_code, 0)
        return snapshots

    def _predict(
        self,
        *,
        snapshots: Path,
        predictions: Path,
        model: str,
        checkpoint: Path | None = None,
        extra: tuple[str, ...] = (),
    ) -> str:
        argv = [
            "predict",
            "--snapshots",
            str(snapshots),
            "--model",
            model,
            "--output",
            str(predictions),
            *extra,
        ]
        if checkpoint is not None:
            argv.extend(["--checkpoint", str(checkpoint)])
        stdout = io.StringIO()
        with contextlib.redirect_stdout(stdout):
            exit_code = main(argv)
        self.assertEqual(exit_code, 0)
        return stdout.getvalue()

    def _compare(self, snapshots: Path, predictions: Path) -> dict[str, object]:
        stdout = io.StringIO()
        with contextlib.redirect_stdout(stdout):
            exit_code = main(
                [
                    "decision-snapshot-compare",
                    str(snapshots),
                    str(predictions),
                    "--json",
                ]
            )
        self.assertEqual(exit_code, 0)
        return json.loads(stdout.getvalue())

    def _prediction_rows(self, path: Path) -> list[dict[str, object]]:
        return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()]

    def _write_discard_linear_checkpoint(self, path: Path) -> None:
        model = DiscardLinearModel(
            weights=tuple(tuple([0.0] * RAW_COUNT_FEATURE_DIM) for _ in range(34)),
            epochs=0,
            learning_rate=0.1,
            feature_profile=RAW_COUNT_FEATURE_PROFILE,
        )
        model.save(path)

    def _write_call_linear_checkpoint(self, path: Path) -> None:
        model = CallLinearModel(
            weights=tuple(tuple([0.0] * CALL_LINEAR_FEATURE_DIM) for _ in CALL_DECISION_KINDS),
            epochs=0,
            learning_rate=0.1,
        )
        model.save(path)

    def _write_riichi_linear_checkpoint(self, path: Path) -> None:
        model = RiichiLinearModel(
            weights=tuple(tuple([0.0] * RIICHI_LINEAR_FEATURE_DIM) for _ in RIICHI_DECISION_KINDS),
            epochs=1,
            learning_rate=0.1,
        )
        model.save(path)

    def _write_deal_in_checkpoint(self, path: Path) -> None:
        model = DealInLinearModel(
            weights=tuple([0.0] * DEAL_IN_LINEAR_FEATURE_DIM),
            epochs=0,
            learning_rate=0.1,
        )
        model.save(path)


if __name__ == "__main__":
    unittest.main()
