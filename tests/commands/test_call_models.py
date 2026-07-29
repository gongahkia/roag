from __future__ import annotations

from tests.commands.conftest import (
    Action,
    ActionKind,
    CallExample,
    CliCommandTests,
    Path,
    TemporaryDirectory,
    Tile,
    _call_example_from_payload,
    _call_example_to_payload,
    _call_examples_signature,
    _limit_call_examples,
    contextlib,
    io,
    json,
    main,
)


class CallModelCommandTests(CliCommandTests):
    def test_benchmark_call_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-call-benchmark-report-v0")
        self.assertEqual(payload["call_examples"], 1)
        self.assertEqual(
            set(payload["models"]),
            {
                "call_frequency",
                "call_legal_frequency",
                "call_linear",
                "call_linear_v1",
                "call_linear_v1_calibrated",
            },
        )
        frequency = payload["models"]["call_frequency"]
        legal_frequency = payload["models"]["call_legal_frequency"]
        call_linear = payload["models"]["call_linear"]
        call_linear_v1 = payload["models"]["call_linear_v1"]
        call_linear_v1_calibrated = payload["models"]["call_linear_v1_calibrated"]
        self.assertEqual(frequency["kind"], "call-frequency-v0")
        self.assertEqual(frequency["counts"]["pon"], 1)
        self.assertEqual(frequency["metrics"]["train_accuracy"], 1.0)
        self.assertEqual(frequency["metrics"]["loss_kind"], "zero_one")
        self.assertEqual(frequency["metrics"]["train_loss"], 0.0)
        self.assertIsNone(frequency["metrics"]["eval_loss"])
        self.assertEqual(frequency["metrics"]["train_balanced_accuracy"], 1.0)
        self.assertEqual(frequency["metrics"]["train_call_recall"], 1.0)
        self.assertIsNone(frequency["metrics"]["train_pass_recall"])
        self.assertIsNone(frequency["metrics"]["eval_accuracy"])
        self.assertIsNone(frequency["metrics"]["eval_balanced_accuracy"])
        self.assertEqual(frequency["train_analysis"]["by_call_or_pass"]["call"]["examples"], 1)
        self.assertEqual(frequency["train_analysis"]["by_legal_call_kinds"]["pon"]["correct"], 1)
        self.assertEqual(legal_frequency["kind"], "call-legal-frequency-v0")
        self.assertEqual(legal_frequency["counts"]["pon"], 1)
        self.assertEqual(legal_frequency["metrics"]["train_action_recall"]["pon"], 1.0)
        self.assertEqual(call_linear["kind"], "call-linear-v0")
        self.assertEqual(call_linear["feature_dim"], 120)
        self.assertEqual(call_linear["feature_profile"], "v0")
        self.assertEqual(call_linear["training"]["epochs"], 25)
        self.assertEqual(call_linear["training"]["positive_class_weight"], 1.0)
        self.assertEqual(call_linear["metrics"]["train_action_recall"]["pon"], 1.0)
        self.assertEqual(call_linear["calibration"]["target"], "call")
        self.assertEqual(len(call_linear["calibration"]["thresholds"]), 21)
        self.assertIsNotNone(call_linear["calibration"]["train"]["best"])
        self.assertIsNone(call_linear["calibration"]["eval"]["best"])
        self.assertEqual(call_linear_v1["kind"], "call-linear-v1")
        self.assertGreater(call_linear_v1["feature_dim"], call_linear["feature_dim"])
        self.assertEqual(call_linear_v1["feature_profile"], "v1")
        self.assertEqual(call_linear_v1["training"]["epochs"], 25)
        self.assertEqual(call_linear_v1["training"]["positive_class_weight"], 1.0)
        self.assertEqual(call_linear_v1["calibration"]["target"], "call")
        self.assertEqual(
            call_linear_v1["calibration"]["train"]["best"]["call_recall"],
            1.0,
        )
        self.assertEqual(call_linear_v1_calibrated["kind"], "call-linear-v1")
        self.assertEqual(call_linear_v1_calibrated["feature_profile"], "v1")
        self.assertEqual(
            call_linear_v1_calibrated["policy"],
            {
                "kind": "threshold-calibrated-v0",
                "target": "call",
                "base_model": "call_linear_v1",
                "threshold": 0.4,
                "threshold_source": "tenhou-100-v0-eval-sweep",
            },
        )
        self.assertEqual(call_linear_v1_calibrated["training"]["positive_class_weight"], 1.0)
        self.assertIn("call_frequency_eval_balanced_accuracy:", stdout.getvalue())
        self.assertIn("call_legal_frequency_eval_call_recall:", stdout.getvalue())
        self.assertIn("call_linear_eval_call_recall:", stdout.getvalue())
        self.assertIn("call_linear_eval_best_threshold:", stdout.getvalue())
        self.assertIn("call_linear_v1_eval_call_recall:", stdout.getvalue())
        self.assertIn("call_linear_v1_eval_best_threshold:", stdout.getvalue())
        self.assertIn("call_linear_v1_calibrated_policy_threshold: 0.40", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())

    def test_benchmark_call_models_fast_writes_sparse_report(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark-fast.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            set(payload["models"]),
            {
                "call_frequency",
                "call_legal_frequency",
                "call_linear_v1",
                "call_linear_v1_calibrated",
            },
        )
        self.assertNotIn("call_linear", payload["models"])
        self.assertEqual(payload["example_limit"], 1)
        self.assertEqual(payload["call_examples_total"], 1)
        self.assertIn("call_linear_v1_eval_best_threshold:", stdout.getvalue())

    def test_benchmark_call_supports_zero_epochs(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark-zero-epochs.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--epochs",
                        "0",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["models"]["call_linear_v1"]["training"]["epochs"], 0)
        self.assertEqual(
            payload["models"]["call_linear_v1_calibrated"]["training"]["epochs"],
            0,
        )

    def test_benchmark_call_profiles_and_reuses_feature_cache(self) -> None:
        with TemporaryDirectory() as directory:
            cache = Path(directory) / "feature-cache.json"
            first_report = Path(directory) / "call-first.json"
            second_report = Path(directory) / "call-second.json"

            first_stdout = io.StringIO()
            with contextlib.redirect_stdout(first_stdout):
                first_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--profile-stages",
                        "--feature-cache",
                        str(cache),
                        "--report",
                        str(first_report),
                    ]
                )
            first_payload = json.loads(first_report.read_text(encoding="utf-8"))

            second_stdout = io.StringIO()
            with contextlib.redirect_stdout(second_stdout):
                second_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--profile-stages",
                        "--feature-cache",
                        str(cache),
                        "--report",
                        str(second_report),
                    ]
                )
            second_payload = json.loads(second_report.read_text(encoding="utf-8"))
            cache_payload = json.loads(cache.read_text(encoding="utf-8"))
            cache_exists = cache.exists()

        self.assertEqual(first_exit_code, 0)
        self.assertTrue(cache_exists)
        self.assertIn("stage_parse_seconds:", first_stdout.getvalue())
        self.assertIn("feature_cache_misses: v1", first_stdout.getvalue())
        self.assertEqual(first_payload["feature_cache"]["misses"], ["v1"])
        self.assertEqual(first_payload["feature_cache"]["writes"], ["v1"])
        self.assertIn("parse", first_payload["timing"])
        self.assertEqual(second_exit_code, 0)
        self.assertIn("feature_cache_hits: v1", second_stdout.getvalue())
        self.assertEqual(second_payload["feature_cache"]["hits"], ["v1"])
        self.assertEqual(second_payload["feature_cache"]["misses"], [])
        self.assertIn("example_signature", cache_payload["cache_key"])
        self.assertIn("train_signature", cache_payload["cache_key"])
        self.assertIn("eval_signature", cache_payload["cache_key"])

    def test_balanced_call_example_limit_interleaves_non_pass_and_pass_examples(self) -> None:
        tile = Tile.parse("1p")

        def example(index: int, action: Action) -> CallExample:
            return CallExample(
                round_index=0,
                event_index=index,
                call_event_index=index if action.kind != ActionKind.PASS else None,
                seat=1,
                from_seat=0,
                dealer=0,
                scores=(25000, 25000, 25000, 25000),
                discarded_tile=tile,
                legal_call_kinds=(ActionKind.PON,),
                hand_counts=(0,) * 34,
                visible_counts=(0,) * 34,
                action=action,
            )

        examples = [
            example(0, Action.pass_()),
            example(1, Action(ActionKind.PON, tile.type)),
            example(2, Action.pass_()),
            example(3, Action(ActionKind.PON, tile.type)),
            example(4, Action(ActionKind.PON, tile.type)),
            example(5, Action.pass_()),
            example(6, Action(ActionKind.PON, tile.type)),
            example(7, Action.pass_()),
        ]

        selected = _limit_call_examples(examples, 4, strategy="balanced")

        self.assertEqual(
            [example.action.kind for example in selected],
            [ActionKind.PON, ActionKind.PASS, ActionKind.PON, ActionKind.PASS],
        )

    def test_call_examples_signature_changes_when_order_changes(self) -> None:
        tile = Tile.parse("1p")

        def example(index: int, action: Action) -> CallExample:
            return CallExample(
                round_index=0,
                event_index=index,
                call_event_index=index if action.kind != ActionKind.PASS else None,
                seat=1,
                from_seat=0,
                dealer=0,
                scores=(25000, 25000, 25000, 25000),
                discarded_tile=tile,
                legal_call_kinds=(ActionKind.PON,),
                hand_counts=(0,) * 34,
                visible_counts=(0,) * 34,
                action=action,
            )

        examples = [
            example(0, Action(ActionKind.PON, tile.type)),
            example(1, Action.pass_()),
            example(2, Action(ActionKind.PON, tile.type)),
        ]

        self.assertNotEqual(
            _call_examples_signature(examples),
            _call_examples_signature(tuple(reversed(examples))),
        )

    def test_call_example_cache_serialization_round_trips(self) -> None:
        discarded_tile = Tile.parse("3p")
        consumed = (Tile.parse("1p"), Tile.parse("2p"))
        example = CallExample(
            round_index=2,
            event_index=10,
            call_event_index=11,
            seat=1,
            from_seat=0,
            dealer=3,
            scores=(27000, 24000, 26000, 23000),
            discarded_tile=discarded_tile,
            legal_call_kinds=(ActionKind.CHI, ActionKind.PON),
            hand_counts=(1, 1, 0, *([0] * 31)),
            visible_counts=(0, 1, 1, *([0] * 31)),
            action=Action(ActionKind.CHI, discarded_tile.type, consumed=consumed),
        )

        restored = _call_example_from_payload(_call_example_to_payload(example))

        self.assertEqual(restored.round_index, example.round_index)
        self.assertEqual(restored.event_index, example.event_index)
        self.assertEqual(restored.call_event_index, example.call_event_index)
        self.assertEqual(restored.seat, example.seat)
        self.assertEqual(restored.from_seat, example.from_seat)
        self.assertEqual(restored.dealer, example.dealer)
        self.assertEqual(restored.scores, example.scores)
        self.assertEqual(restored.discarded_tile, example.discarded_tile)
        self.assertEqual(restored.legal_call_kinds, example.legal_call_kinds)
        self.assertEqual(restored.hand_counts, example.hand_counts)
        self.assertEqual(restored.visible_counts, example.visible_counts)
        self.assertEqual(restored.action.kind, ActionKind.CHI)
        self.assertEqual(restored.action.tile, discarded_tile.type)
        self.assertEqual(restored.action.consumed, consumed)

    def test_benchmark_call_reuses_example_cache(self) -> None:
        with TemporaryDirectory() as directory:
            cache = Path(directory) / "call-examples-cache.json"
            first_report = Path(directory) / "call-first.json"
            second_report = Path(directory) / "call-second.json"

            first_stdout = io.StringIO()
            with contextlib.redirect_stdout(first_stdout):
                first_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--profile-stages",
                        "--report",
                        str(first_report),
                    ]
                )
            first_payload = json.loads(first_report.read_text(encoding="utf-8"))

            second_stdout = io.StringIO()
            with contextlib.redirect_stdout(second_stdout):
                second_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--profile-stages",
                        "--report",
                        str(second_report),
                    ]
                )
            second_payload = json.loads(second_report.read_text(encoding="utf-8"))
            cache_payload = json.loads(cache.read_text(encoding="utf-8"))

        self.assertEqual(first_exit_code, 0)
        self.assertEqual(second_exit_code, 0)
        self.assertIn("example_cache_hit: no", first_stdout.getvalue())
        self.assertIn("stage_parse_seconds:", first_stdout.getvalue())
        self.assertIn("example_cache_hit: yes", second_stdout.getvalue())
        self.assertNotIn("stage_parse_seconds:", second_stdout.getvalue())
        self.assertEqual(first_payload["example_cache"]["hit"], False)
        self.assertEqual(first_payload["example_cache"]["writes"], True)
        self.assertEqual(first_payload["example_cache"]["examples"], 1)
        self.assertEqual(second_payload["example_cache"]["hit"], True)
        self.assertEqual(second_payload["example_cache"]["writes"], False)
        self.assertEqual(second_payload["example_cache"]["examples"], 1)
        self.assertEqual(second_payload["discard_examples"], 4)
        self.assertEqual(second_payload["rounds"], 3)
        self.assertEqual(cache_payload["kind"], "kenjaku-call-example-cache-v0")
        self.assertEqual(len(cache_payload["examples"]), 1)
        self.assertEqual(cache_payload["game_counts"]["rounds"], 3)
        self.assertEqual(cache_payload["discard_examples"], 4)
        self.assertEqual(
            cache_payload["cache_key"]["xml_files"][0]["path"],
            str(Path("data/fixtures/tenhou/events_4p.xml").resolve()),
        )

    def test_benchmark_call_example_limit_is_applied_after_cache_load(self) -> None:
        with TemporaryDirectory() as directory:
            fixture_dir = Path(directory) / "fixtures"
            fixture_dir.mkdir()
            fixture_text = Path("data/fixtures/tenhou/events_4p.xml").read_text(
                encoding="utf-8",
            )
            (fixture_dir / "a.xml").write_text(fixture_text, encoding="utf-8")
            (fixture_dir / "b.xml").write_text(fixture_text, encoding="utf-8")
            cache = Path(directory) / "call-examples-cache.json"
            first_report = Path(directory) / "call-first.json"
            second_report = Path(directory) / "call-second.json"

            with contextlib.redirect_stdout(io.StringIO()):
                first_exit_code = main(
                    [
                        "benchmark-call",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--report",
                        str(first_report),
                    ]
                )

            second_stdout = io.StringIO()
            with contextlib.redirect_stdout(second_stdout):
                second_exit_code = main(
                    [
                        "benchmark-call",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--example-cache",
                        str(cache),
                        "--report",
                        str(second_report),
                    ]
                )
            first_payload = json.loads(first_report.read_text(encoding="utf-8"))
            second_payload = json.loads(second_report.read_text(encoding="utf-8"))

        self.assertEqual(first_exit_code, 0)
        self.assertEqual(second_exit_code, 0)
        self.assertEqual(first_payload["call_examples"], 2)
        self.assertEqual(first_payload["call_examples_total"], 2)
        self.assertIn("example_cache_hit: yes", second_stdout.getvalue())
        self.assertEqual(second_payload["call_examples"], 1)
        self.assertEqual(second_payload["call_examples_total"], 2)
        self.assertEqual(second_payload["example_cache"]["examples"], 2)

    def test_benchmark_call_example_cache_invalidates_when_file_changes(self) -> None:
        with TemporaryDirectory() as directory:
            fixture_dir = Path(directory) / "fixtures"
            fixture_dir.mkdir()
            fixture = fixture_dir / "events.xml"
            fixture.write_text(
                Path("data/fixtures/tenhou/events_4p.xml").read_text(encoding="utf-8"),
                encoding="utf-8",
            )
            cache = Path(directory) / "call-examples-cache.json"
            first_report = Path(directory) / "call-first.json"
            second_report = Path(directory) / "call-second.json"

            with contextlib.redirect_stdout(io.StringIO()):
                first_exit_code = main(
                    [
                        "benchmark-call",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--report",
                        str(first_report),
                    ]
                )

            fixture.write_text(fixture.read_text(encoding="utf-8") + "\n", encoding="utf-8")

            second_stdout = io.StringIO()
            with contextlib.redirect_stdout(second_stdout):
                second_exit_code = main(
                    [
                        "benchmark-call",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-cache",
                        str(cache),
                        "--profile-stages",
                        "--report",
                        str(second_report),
                    ]
                )
            second_payload = json.loads(second_report.read_text(encoding="utf-8"))

        self.assertEqual(first_exit_code, 0)
        self.assertEqual(second_exit_code, 0)
        self.assertIn("example_cache_hit: no", second_stdout.getvalue())
        self.assertIn("stage_parse_seconds:", second_stdout.getvalue())
        self.assertEqual(second_payload["example_cache"]["hit"], False)
        self.assertEqual(second_payload["example_cache"]["writes"], True)

    def test_benchmark_call_models_rejects_unknown_name(self) -> None:
        with self.assertRaises(SystemExit) as context:
            main(
                [
                    "benchmark-call",
                    "data/fixtures/tenhou",
                    "--models",
                    "call_linear_v9",
                ]
            )

        self.assertIn("unsupported call benchmark model", str(context.exception))

    def test_benchmark_call_can_include_weighted_variant(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--include-weighted",
                        "--call-positive-weight",
                        "3.0",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("call_linear_v1_weighted", payload["models"])
        weighted = payload["models"]["call_linear_v1_weighted"]
        self.assertEqual(weighted["kind"], "call-linear-v1")
        self.assertEqual(weighted["feature_profile"], "v1")
        self.assertEqual(weighted["training"]["positive_class_weight"], 3.0)
        self.assertNotIn("policy", weighted)
        self.assertIn("call_linear_v1_weighted_eval_call_recall:", stdout.getvalue())

    def test_benchmark_call_can_use_train_best_threshold_source(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--call-threshold-source",
                        "train-best",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        base = payload["models"]["call_linear_v1"]
        calibrated = payload["models"]["call_linear_v1_calibrated"]
        self.assertEqual(exit_code, 0)
        self.assertEqual(calibrated["policy"]["threshold_source"], "train-best")
        self.assertEqual(
            calibrated["policy"]["threshold"],
            base["calibration"]["train"]["best"]["threshold"],
        )
