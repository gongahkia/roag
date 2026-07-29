from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory
from typing import Any

from kenjaku.experiments import (
    BENCHMARK_SUMMARY_KIND,
    CALL_BENCHMARK_REPORT_KIND,
    DISCARD_BENCHMARK_REPORT_KIND,
    DISCARD_BENCHMARK_SUMMARY_KIND,
    DISCARD_DISAGREEMENT_SUMMARY_KIND,
    DISCARD_LINEAR_REPORT_KIND,
    DISCARD_MLP_BENCHMARK_REPORT_KIND,
    DISCARD_MLP_REPORT_KIND,
    DISCARD_TRANSFORMER_BENCHMARK_REPORT_KIND,
    DISCARD_TRANSFORMER_REPORT_KIND,
    PUBLIC_BENCHMARK_DASHBOARD_KIND,
    RIICHI_BENCHMARK_REPORT_KIND,
    TENHOU_INSPECT_REPORT_KIND,
    build_call_benchmark_report,
    build_discard_benchmark_report,
    build_discard_benchmark_report_from_models,
    build_discard_benchmark_summary,
    build_discard_disagreement_summary,
    build_discard_linear_report,
    build_discard_mlp_benchmark_report,
    build_discard_mlp_report,
    build_discard_transformer_benchmark_report,
    build_discard_transformer_report,
    build_public_benchmark_dashboard,
    build_riichi_benchmark_report,
    build_tenhou_inspect_report,
    format_discard_benchmark_summary,
    format_discard_disagreement_summary,
    format_public_benchmark_dashboard_html,
    write_json_report,
)
from kenjaku.io import TenhouGame, TenhouParseFailure, parse_tenhou_xml_file

SOURCE = {"label": "unit", "date": "2026-07-08"}
EMPTY_GAME = TenhouGame(rounds=())
SHANTEN = {"examples": 0, "average_delta": None}


class ExperimentReportTests(unittest.TestCase):
    def test_tenhou_inspect_report_counts_fixture_rounds(self) -> None:
        game = parse_tenhou_xml_file("data/fixtures/tenhou/minimal_4p.xml")

        report = build_tenhou_inspect_report(
            **_base_kwargs(game=game),
            discard_shanten=SHANTEN,
        )

        self.assertEqual(report["kind"], TENHOU_INSPECT_REPORT_KIND)
        self.assertEqual(report["rounds"], 1)
        self.assertEqual(report["discards"], 2)

    def test_tenhou_inspect_report_keeps_parse_failures(self) -> None:
        report = build_tenhou_inspect_report(
            **_base_kwargs(parse_failures=[_failure()]),
            discard_shanten=SHANTEN,
        )

        self.assertEqual(report["parse_failures"]["count"], 1)
        self.assertEqual(report["parse_failures"]["items"][0]["error_type"], "ValueError")

    def test_discard_linear_report_has_training_and_artifact(self) -> None:
        report = build_discard_linear_report(
            **_base_kwargs(),
            **_split_kwargs(),
            model_kind="discard-linear-v1",
            feature_dim=76,
            epochs=3,
            learning_rate=0.1,
            l2=0.01,
            train_accuracy=0.5,
            eval_accuracy=0.25,
            discard_shanten=SHANTEN,
            model_path=Path("model.json"),
        )

        self.assertEqual(report["kind"], DISCARD_LINEAR_REPORT_KIND)
        self.assertEqual(report["artifacts"]["model_path"], "model.json")
        self.assertEqual(report["training"]["l2"], 0.01)

    def test_discard_linear_report_allows_missing_eval_accuracy(self) -> None:
        report = build_discard_linear_report(
            **_base_kwargs(),
            **_split_kwargs(),
            model_kind="discard-linear-v1",
            feature_dim=76,
            epochs=1,
            learning_rate=0.1,
            l2=0.0,
            train_accuracy=1.0,
            eval_accuracy=None,
            discard_shanten=SHANTEN,
            model_path=None,
        )

        self.assertIsNone(report["metrics"]["eval_accuracy"])
        self.assertIsNone(report["artifacts"]["model_path"])

    def test_discard_mlp_report_normalizes_training_history(self) -> None:
        report = build_discard_mlp_report(
            **_base_kwargs(),
            **_split_kwargs(),
            **_mlp_kwargs(),
            checkpoint_path=Path("mlp.pt"),
        )

        self.assertEqual(report["kind"], DISCARD_MLP_REPORT_KIND)
        self.assertEqual(report["training_history"]["records"][0]["step"], 1)
        self.assertEqual(report["training_history"]["curves"]["loss"][0]["value"], 1.0)
        self.assertEqual(report["artifacts"]["checkpoint_path"], "mlp.pt")

    def test_discard_mlp_report_allows_missing_checkpoint(self) -> None:
        report = build_discard_mlp_report(
            **_base_kwargs(),
            **_split_kwargs(),
            **_mlp_kwargs(),
            checkpoint_path=None,
        )

        self.assertIsNone(report["artifacts"]["checkpoint_path"])
        self.assertEqual(report["metrics"]["eval"]["accuracy"], 0.4)

    def test_discard_mlp_benchmark_report_computes_deltas(self) -> None:
        report = build_discard_mlp_benchmark_report(
            **_base_kwargs(),
            **_split_kwargs(),
            models={**_baseline_models(), "discard_mlp": _mlp_model()},
            discard_shanten=SHANTEN,
            checkpoint_path=Path("mlp.pt"),
        )

        self.assertEqual(report["kind"], DISCARD_MLP_BENCHMARK_REPORT_KIND)
        self.assertAlmostEqual(report["deltas"]["mlp_eval_accuracy_lift_over_frequency"], 0.2)

    def test_discard_mlp_benchmark_report_handles_missing_metrics(self) -> None:
        report = build_discard_mlp_benchmark_report(
            **_base_kwargs(),
            **_split_kwargs(),
            models={"discard_mlp": {"metrics": {}}},
            discard_shanten=SHANTEN,
            checkpoint_path=None,
        )

        self.assertIsNone(report["deltas"]["mlp_eval_accuracy_lift_over_frequency"])
        self.assertIsNone(report["artifacts"]["checkpoint_path"])

    def test_discard_benchmark_report_merges_model_diagnostics(self) -> None:
        report = build_discard_benchmark_report(
            **_discard_benchmark_kwargs(),
            model_diagnostics={"linear": {"diagnostic": {"ok": True}}},
        )

        self.assertEqual(report["kind"], DISCARD_BENCHMARK_REPORT_KIND)
        self.assertEqual(report["models"]["linear"]["diagnostic"], {"ok": True})

    def test_discard_benchmark_report_from_models_uses_cached_counts(self) -> None:
        report = build_discard_benchmark_report_from_models(
            **_base_kwargs(game=None),
            **_split_kwargs(),
            models=_discard_models(),
            discard_shanten=SHANTEN,
            game_counts=_game_counts(),
        )

        self.assertEqual(report["rounds"], 0)
        self.assertAlmostEqual(report["ablation"]["eval_accuracy_lift_over_raw_count"], 0.1)

    def test_discard_benchmark_report_from_models_requires_counts_without_game(self) -> None:
        with self.assertRaisesRegex(ValueError, "game_counts"):
            build_discard_benchmark_report_from_models(
                **_base_kwargs(game=None),
                **_split_kwargs(),
                models=_discard_models(),
                discard_shanten=SHANTEN,
                game_counts=None,
            )

    def test_discard_transformer_report_records_value_head(self) -> None:
        report = build_discard_transformer_report(
            **_base_kwargs(),
            **_split_kwargs(),
            **_transformer_kwargs(),
            checkpoint_path=Path("transformer.pt"),
            value_head=True,
        )

        self.assertEqual(report["kind"], DISCARD_TRANSFORMER_REPORT_KIND)
        self.assertTrue(report["model"]["value_head"])
        self.assertEqual(report["artifacts"]["checkpoint_path"], "transformer.pt")

    def test_discard_transformer_benchmark_report_computes_deltas(self) -> None:
        report = build_discard_transformer_benchmark_report(
            **_base_kwargs(),
            **_split_kwargs(),
            models={**_baseline_models(), "discard_transformer": _transformer_model()},
            discard_shanten=SHANTEN,
            checkpoint_path=None,
        )

        self.assertEqual(report["kind"], DISCARD_TRANSFORMER_BENCHMARK_REPORT_KIND)
        self.assertAlmostEqual(
            report["deltas"]["transformer_eval_accuracy_lift_over_frequency"],
            0.25,
        )

    def test_call_benchmark_report_keeps_optional_caches(self) -> None:
        report = build_call_benchmark_report(
            **_base_kwargs(game=None),
            **_split_kwargs(),
            models={"linear": _binary_model("call")},
            call_examples_total=4,
            example_limit=2,
            example_limit_strategy="first",
            timing={"fit": 0.1},
            feature_cache={"hits": 1},
            example_cache={"path": "cache.json"},
            game_counts=_game_counts(),
        )

        self.assertEqual(report["kind"], CALL_BENCHMARK_REPORT_KIND)
        self.assertEqual(report["call_examples_total"], 4)
        self.assertEqual(report["feature_cache"]["hits"], 1)

    def test_call_benchmark_report_requires_counts_without_game(self) -> None:
        with self.assertRaisesRegex(ValueError, "game_counts"):
            build_call_benchmark_report(
                **_base_kwargs(game=None),
                **_split_kwargs(),
                models={"linear": _binary_model("call")},
            )

    def test_riichi_benchmark_report_records_riichi_examples(self) -> None:
        report = build_riichi_benchmark_report(
            **_base_kwargs(game=None),
            **_split_kwargs(),
            riichi_examples=3,
            models={"linear": _binary_model("riichi")},
            game_counts=_game_counts(),
        )

        self.assertEqual(report["kind"], RIICHI_BENCHMARK_REPORT_KIND)
        self.assertEqual(report["riichi_examples"], 3)

    def test_write_json_report_creates_parent_dirs(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "nested" / "report.json"
            write_json_report(path, {"kind": "unit", "value": 1})
            payload = json.loads(path.read_text(encoding="utf-8"))

        self.assertEqual(payload["kind"], "unit")
        self.assertEqual(payload["value"], 1)
        self.assertEqual(
            set(payload["provenance"]),
            {
                "argv",
                "duration_seconds",
                "git_commit",
                "git_dirty",
                "kenjaku_version",
                "python_version",
                "started_at",
            },
        )
        self.assertIsInstance(payload["provenance"]["git_dirty"], bool)
        self.assertGreaterEqual(payload["provenance"]["duration_seconds"], 0.0)

    def test_discard_benchmark_summary_handles_empty_input(self) -> None:
        summary = build_discard_benchmark_summary([])

        self.assertEqual(summary["kind"], DISCARD_BENCHMARK_SUMMARY_KIND)
        self.assertEqual(summary["reports"], [])

    def test_discard_benchmark_summary_reads_discard_report(self) -> None:
        with TemporaryDirectory() as directory:
            path = _write_json(Path(directory), _discard_report())
            summary = build_discard_benchmark_summary([path])

        self.assertEqual(summary["kind"], DISCARD_BENCHMARK_SUMMARY_KIND)
        self.assertEqual(summary["reports"][0]["target"], "discard")

    def test_discard_benchmark_summary_uses_mixed_kind_for_call_report(self) -> None:
        with TemporaryDirectory() as directory:
            path = _write_json(Path(directory), _call_report())
            summary = build_discard_benchmark_summary([path])

        self.assertEqual(summary["kind"], BENCHMARK_SUMMARY_KIND)
        self.assertEqual(summary["reports"][0]["target"], "call")

    def test_discard_benchmark_summary_rejects_non_object_json(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "bad.json"
            path.write_text("[]", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "JSON object"):
                build_discard_benchmark_summary([path])

    def test_format_discard_benchmark_summary_outputs_model_lines(self) -> None:
        with TemporaryDirectory() as directory:
            summary = build_discard_benchmark_summary(
                [_write_json(Path(directory), _discard_report())],
            )
            text = format_discard_benchmark_summary(summary)

        self.assertIn("report:", text)
        self.assertIn("linear: train=0.6000 eval=0.5000", text)

    def test_format_discard_benchmark_summary_rejects_bad_kind(self) -> None:
        with self.assertRaisesRegex(ValueError, "benchmark summary"):
            format_discard_benchmark_summary({"kind": "bad", "reports": []})

    def test_public_benchmark_dashboard_handles_empty_summary(self) -> None:
        dashboard = build_public_benchmark_dashboard(
            [],
            version="0.1.0",
            generated_at="2026-07-08T00:00:00Z",
        )

        self.assertEqual(dashboard["kind"], PUBLIC_BENCHMARK_DASHBOARD_KIND)
        self.assertTrue(dashboard["scope"]["offline_benchmarks_only"])

    def test_format_public_benchmark_dashboard_html_escapes_title(self) -> None:
        dashboard = build_public_benchmark_dashboard(
            [],
            version="0.1.0",
            generated_at="2026-07-08T00:00:00Z",
            title="<Unit>",
        )

        html = format_public_benchmark_dashboard_html(dashboard)

        self.assertIn("&lt;Unit&gt;", html)
        self.assertIn("Live rank tracking: not included", html)

    def test_format_public_benchmark_dashboard_html_rejects_bad_kind(self) -> None:
        with self.assertRaisesRegex(ValueError, "public benchmark dashboard"):
            format_public_benchmark_dashboard_html({"kind": "bad"})

    def test_discard_disagreement_summary_handles_empty_input(self) -> None:
        summary = build_discard_disagreement_summary([])

        self.assertEqual(summary["kind"], DISCARD_DISAGREEMENT_SUMMARY_KIND)
        self.assertEqual(summary["reports"], [])

    def test_discard_disagreement_summary_summarizes_category(self) -> None:
        with TemporaryDirectory() as directory:
            path = _write_json(Path(directory), _disagreement_report())
            summary = build_discard_disagreement_summary([path])

        category = summary["reports"][0]["categories"]["risk_correct_defense_wrong"]
        self.assertEqual(category["defense_buckets"]["genbutsu"]["true"], 1)
        self.assertEqual(category["actual_prediction_pairs"][0]["wrong_prediction"], "8m")

    def test_discard_disagreement_summary_rejects_malformed_items(self) -> None:
        payload = _disagreement_report(items="bad")
        with TemporaryDirectory() as directory:
            path = _write_json(Path(directory), payload)
            with self.assertRaisesRegex(ValueError, "items must be a list"):
                build_discard_disagreement_summary([path])

    def test_format_discard_disagreement_summary_outputs_margins(self) -> None:
        with TemporaryDirectory() as directory:
            summary = build_discard_disagreement_summary(
                [_write_json(Path(directory), _disagreement_report())],
            )
            text = format_discard_disagreement_summary(summary)

        self.assertIn("risk_correct_defense_wrong: count=1 stored=1", text)
        self.assertIn("margins: correct_model_actual=2.0000", text)

    def test_format_discard_disagreement_summary_rejects_bad_kind(self) -> None:
        with self.assertRaisesRegex(ValueError, "discard disagreement summary"):
            format_discard_disagreement_summary({"kind": "bad", "reports": []})


def _base_kwargs(**overrides: Any) -> dict[str, Any]:
    values: dict[str, Any] = {
        "input_paths": [Path("input")],
        "xml_files": [Path("input/game.xml")],
        "game": EMPTY_GAME,
        "discard_examples": 1,
        "call_examples": 1,
        "parse_failures": [],
        "source": SOURCE,
    }
    values.update(overrides)
    return values


def _split_kwargs(**overrides: Any) -> dict[str, Any]:
    values: dict[str, Any] = {
        "split_seed": "fixed",
        "eval_fraction": 0.5,
        "train_examples": 1,
        "eval_examples": 1,
    }
    values.update(overrides)
    return values


def _mlp_kwargs() -> dict[str, Any]:
    return {
        "model_kind": "discard-mlp-v0",
        "input_dim": 4,
        "hidden_dim": 8,
        "output_dim": 34,
        "epochs": 2,
        "batch_size": 1,
        "learning_rate": 0.01,
        "device": "cpu",
        "seed": 7,
        "train_metrics": {"accuracy": 0.5, "loss": 1.0},
        "eval_metrics": {"accuracy": 0.4, "loss": 1.2},
        "history": [{"epoch": 1, "metrics": {"loss": 1.0}}],
        "best_epoch": 1,
        "selection_split": "eval",
        "best_metrics": {"train": {"accuracy": 0.5}, "eval": {"accuracy": 0.4}},
        "discard_shanten": SHANTEN,
    }


def _transformer_kwargs() -> dict[str, Any]:
    return {
        "model_kind": "discard-transformer-v0",
        "encoder_kind": "events",
        "input_tokens": 16,
        "output_dim": 34,
        "model_config": {
            "model_dim": 8,
            "num_heads": 1,
            "num_layers": 1,
            "feedforward_dim": 16,
            "dropout": 0.0,
        },
        "epochs": 2,
        "batch_size": 1,
        "learning_rate": 0.01,
        "device": "cpu",
        "seed": 7,
        "train_metrics": {"accuracy": 0.5, "loss": 1.0},
        "eval_metrics": {"accuracy": 0.45, "loss": 1.2},
        "history": [{"epoch": 1, "metrics": {"loss": 1.0}}],
        "best_epoch": 1,
        "selection_split": "eval",
        "best_metrics": {"train": {"accuracy": 0.5}, "eval": {"accuracy": 0.45}},
        "discard_shanten": SHANTEN,
    }


def _discard_benchmark_kwargs() -> dict[str, Any]:
    return {
        **_base_kwargs(),
        **_split_kwargs(),
        "frequency_train_accuracy": 0.4,
        "frequency_eval_accuracy": 0.3,
        "frequency_eval_analysis": {},
        "raw_count_linear_epochs": 1,
        "raw_count_linear_learning_rate": 0.1,
        "raw_count_linear_model_kind": "raw",
        "raw_count_linear_feature_dim": 69,
        "raw_count_linear_train_accuracy": 0.5,
        "raw_count_linear_eval_accuracy": 0.4,
        "raw_count_linear_eval_analysis": {},
        "linear_epochs": 1,
        "linear_learning_rate": 0.1,
        "linear_model_kind": "linear",
        "linear_feature_dim": 76,
        "linear_train_accuracy": 0.6,
        "linear_eval_accuracy": 0.5,
        "linear_eval_analysis": {},
        "risk_context_linear_epochs": 1,
        "risk_context_linear_learning_rate": 0.1,
        "risk_context_linear_model_kind": "risk",
        "risk_context_linear_feature_dim": 86,
        "risk_context_linear_train_accuracy": 0.7,
        "risk_context_linear_eval_accuracy": 0.6,
        "risk_context_linear_eval_analysis": {},
        "defense_context_linear_epochs": 1,
        "defense_context_linear_learning_rate": 0.1,
        "defense_context_linear_model_kind": "defense",
        "defense_context_linear_feature_dim": 98,
        "defense_context_linear_train_accuracy": 0.8,
        "defense_context_linear_eval_accuracy": 0.7,
        "defense_context_linear_eval_analysis": {},
        "defense_context_v1_linear_epochs": 1,
        "defense_context_v1_linear_learning_rate": 0.1,
        "defense_context_v1_linear_model_kind": "defense-v1",
        "defense_context_v1_linear_feature_dim": 112,
        "defense_context_v1_linear_train_accuracy": 0.9,
        "defense_context_v1_linear_eval_accuracy": 0.8,
        "defense_context_v1_linear_eval_analysis": {},
        "linear_l2": 0.0,
        "discard_shanten": SHANTEN,
    }


def _baseline_models() -> dict[str, dict[str, Any]]:
    return {
        "frequency": _linear_model(eval_accuracy=0.3),
        "risk_context_linear": _linear_model(eval_accuracy=0.4),
        "defense_context_linear": _linear_model(eval_accuracy=0.45),
    }


def _discard_models() -> dict[str, dict[str, Any]]:
    return {
        "frequency": _linear_model(train_accuracy=0.4, eval_accuracy=0.3),
        "raw_count_linear": _linear_model(train_accuracy=0.5, eval_accuracy=0.4),
        "linear": _linear_model(train_accuracy=0.6, eval_accuracy=0.5),
        "risk_context_linear": _linear_model(train_accuracy=0.7, eval_accuracy=0.6),
        "defense_context_linear": _linear_model(train_accuracy=0.8, eval_accuracy=0.7),
        "defense_context_v1_linear": _linear_model(train_accuracy=0.9, eval_accuracy=0.8),
    }


def _linear_model(
    *,
    train_accuracy: float = 0.5,
    eval_accuracy: float | None = 0.5,
) -> dict[str, Any]:
    return {
        "kind": "linear",
        "feature_dim": 2,
        "metrics": {
            "train_accuracy": train_accuracy,
            "eval_accuracy": eval_accuracy,
        },
        "eval_analysis": _analysis(),
    }


def _mlp_model() -> dict[str, Any]:
    return {
        "kind": "discard-mlp-v0",
        "input_dim": 4,
        "hidden_dim": 8,
        "output_dim": 34,
        "training": _neural_training(),
        "metrics": _neural_metrics(eval_accuracy=0.5),
    }


def _transformer_model() -> dict[str, Any]:
    return {
        "kind": "discard-transformer-v0",
        "encoder_kind": "events",
        "input_tokens": 16,
        "output_dim": 34,
        "value_head": True,
        "config": {"model_dim": 8, "num_heads": 1, "num_layers": 1},
        "training": _neural_training(),
        "metrics": _neural_metrics(eval_accuracy=0.55),
    }


def _neural_training() -> dict[str, Any]:
    return {
        "epochs": 2,
        "batch_size": 1,
        "learning_rate": 0.01,
        "device": "cpu",
        "seed": 7,
        "best_epoch": 1,
        "selection_split": "eval",
    }


def _neural_metrics(*, eval_accuracy: float) -> dict[str, Any]:
    return {
        "train": {"accuracy": 0.5, "loss": 1.0},
        "eval": {"accuracy": eval_accuracy, "loss": 1.2},
        "best": {"train": {"accuracy": 0.5}, "eval": {"accuracy": eval_accuracy}},
    }


def _binary_model(target: str) -> dict[str, Any]:
    return {
        "kind": f"{target}-linear",
        "feature_dim": 2,
        "metrics": {
            "train_accuracy": 0.7,
            "eval_accuracy": 0.6,
            "eval_balanced_accuracy": 0.55,
            "eval_pass_recall": 0.5,
            f"eval_{target}_recall": 0.6,
        },
        "policy": {"threshold": 0.4, "threshold_source": "eval"},
        "training": {"positive_class_weight": 2.0},
    }


def _discard_report() -> dict[str, Any]:
    return build_discard_benchmark_report_from_models(
        **_base_kwargs(),
        **_split_kwargs(),
        models=_discard_models(),
        discard_shanten=SHANTEN,
    )


def _call_report() -> dict[str, Any]:
    return build_call_benchmark_report(
        **_base_kwargs(),
        **_split_kwargs(),
        models={"linear": _binary_model("call")},
    )


def _disagreement_report(*, items: Any | None = None) -> dict[str, Any]:
    if items is None:
        items = [_disagreement_item()]
    return {
        "kind": "kenjaku-discard-disagreements-v0",
        "examples": 1,
        "max_per_category": 5,
        "categories": {
            "risk_correct_defense_wrong": {
                "count": 1,
                "items": items,
            },
        },
    }


def _disagreement_item() -> dict[str, Any]:
    return {
        "actual_discard": "5m",
        "predictions": {"risk_context_linear": "5m", "defense_context_linear": "8m"},
        "defense_buckets": {"genbutsu": True, "suji": False},
        "candidate_logits": {
            "risk_context_linear": [
                {"tile": "5m", "logit": 3.0},
                {"tile": "8m", "logit": 1.0},
            ],
            "defense_context_linear": [
                {"tile": "5m", "logit": 0.5},
                {"tile": "8m", "logit": 2.0},
            ],
        },
    }


def _analysis() -> dict[str, Any]:
    return {
        "by_active_opponent_riichi": {
            "yes": {"accuracy": 1.0, "correct": 1, "examples": 1},
        },
    }


def _game_counts() -> dict[str, int]:
    return {
        "rounds": 0,
        "draws": 0,
        "discards": 0,
        "reaches": 0,
        "calls": 0,
        "wins": 0,
        "exhaustive_draws": 0,
    }


def _failure() -> TenhouParseFailure:
    return TenhouParseFailure(Path("bad.xml"), "ValueError", "bad xml")


def _write_json(directory: Path, payload: dict[str, Any]) -> Path:
    path = directory / "report.json"
    path.write_text(json.dumps(payload), encoding="utf-8")
    return path


if __name__ == "__main__":
    unittest.main()
