from __future__ import annotations

from tests.commands.conftest import (
    CliCommandTests,
    Path,
    TemporaryDirectory,
    contextlib,
    io,
    json,
    main,
)


class ReplayCommandTests(CliCommandTests):
    def test_replay_intake_review_outputs_text_json_report_and_accepted_queue(self) -> None:
        manifest_payload = {
            "kind": "kenjaku-replay-manifest-v0",
            "items": [
                {
                    "id": "accepted-synthetic",
                    "platform": "synthetic",
                    "uri": "data/fixtures/replay/synthetic.json",
                    "intended_uses": ["analysis", "training"],
                    "permission": {"status": "local_synthetic"},
                },
                {
                    "id": "blocked-training",
                    "platform": "mahjong_soul",
                    "uri": "https://mahjongsoul.game.yo-star.com/?paipu=blocked",
                    "intended_uses": ["training"],
                    "permission": {"status": "user_provided"},
                },
            ],
        }
        with TemporaryDirectory() as directory:
            manifest = Path(directory) / "manifest.json"
            report = Path(directory) / "review.json"
            accepted = Path(directory) / "accepted.jsonl"
            manifest.write_text(json.dumps(manifest_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "replay-intake-review",
                        str(manifest),
                        "--report",
                        str(report),
                        "--accepted-output",
                        str(accepted),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))
            accepted_rows = [
                json.loads(line) for line in accepted.read_text(encoding="utf-8").splitlines()
            ]

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["replay-intake-review", str(manifest), "--json"])
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("items: 2", text_stdout.getvalue())
        self.assertIn("accepted: 1", text_stdout.getvalue())
        self.assertIn("rejected: 1", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertIn("accepted_output_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-replay-intake-review-v0")
        self.assertEqual(report_payload["accepted"], 1)
        self.assertEqual(report_payload["rejected"], 1)
        self.assertEqual(len(accepted_rows), 1)
        self.assertEqual(accepted_rows[0]["kind"], "kenjaku-replay-intake-item-v0")
        self.assertEqual(accepted_rows[0]["id"], "accepted-synthetic")
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["accepted"], 1)

    def test_replay_share_plan_outputs_text_json_and_report(self) -> None:
        accepted_rows = [
            {
                "kind": "kenjaku-replay-intake-item-v0",
                "id": "demo-ready",
                "platform": "other",
                "uri": "https://example.test/replay",
                "intended_uses": ["analysis", "demo"],
                "permission": {
                    "status": "explicit_permission",
                    "scope": ["analysis", "demo"],
                    "granted_by": "unit-test",
                    "granted_at": "2026-06-11",
                    "notes": None,
                },
            },
            {
                "kind": "kenjaku-replay-intake-item-v0",
                "id": "analysis-only",
                "platform": "mahjong_soul",
                "uri": "https://mahjongsoul.game.yo-star.com/?paipu=analysis",
                "intended_uses": ["analysis"],
                "permission": {
                    "status": "user_provided",
                    "scope": ["analysis", "evaluation"],
                    "granted_by": None,
                    "granted_at": None,
                    "notes": None,
                },
            },
        ]
        with TemporaryDirectory() as directory:
            accepted = Path(directory) / "accepted.jsonl"
            report = Path(directory) / "share-plan.json"
            accepted.write_text(
                "".join(json.dumps(row, sort_keys=True) + "\n" for row in accepted_rows),
                encoding="utf-8",
            )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "replay-share-plan",
                        str(accepted),
                        "--intent",
                        "demo",
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    ["replay-share-plan", str(accepted), "--intent", "demo", "--json"]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("intent: demo", text_stdout.getvalue())
        self.assertIn("shareable: 1", text_stdout.getvalue())
        self.assertIn("blocked: 1", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-replay-share-plan-v0")
        self.assertEqual(report_payload["shareable"], 1)
        self.assertEqual(report_payload["blocked"], 1)
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["intent"], "demo")

    def test_replay_public_summary_outputs_text_json_and_report(self) -> None:
        accepted_rows = [
            {
                "kind": "kenjaku-replay-intake-item-v0",
                "id": "demo-ready",
                "platform": "local_file",
                "uri": "/private/replays/demo-ready.xml",
                "intended_uses": ["analysis", "demo"],
                "permission": {
                    "status": "explicit_permission",
                    "scope": ["analysis", "demo"],
                    "granted_by": "unit-test",
                    "granted_at": "2026-06-12",
                    "notes": None,
                },
            },
            {
                "kind": "kenjaku-replay-intake-item-v0",
                "id": "analysis-only",
                "platform": "local_file",
                "uri": "/private/replays/analysis-only.xml",
                "intended_uses": ["analysis"],
                "permission": {
                    "status": "user_provided",
                    "scope": ["analysis", "evaluation"],
                    "granted_by": None,
                    "granted_at": None,
                    "notes": None,
                },
            },
        ]
        with TemporaryDirectory() as directory:
            accepted = Path(directory) / "accepted.jsonl"
            report = Path(directory) / "public-summary.json"
            accepted.write_text(
                "".join(json.dumps(row, sort_keys=True) + "\n" for row in accepted_rows),
                encoding="utf-8",
            )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "replay-public-summary",
                        str(accepted),
                        "--intent",
                        "demo",
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    ["replay-public-summary", str(accepted), "--intent", "demo", "--json"]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("raw_replay_uris_included: no", text_stdout.getvalue())
        self.assertIn("public_summaries:", text_stdout.getvalue())
        self.assertIn("blocked_items:", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-replay-public-summary-v0")
        self.assertEqual(report_payload["shareable"], 1)
        self.assertEqual(report_payload["blocked"], 1)
        self.assertFalse(report_payload["raw_replay_uris_included"])
        self.assertNotIn("/private/replays/demo-ready.xml", json.dumps(report_payload))
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["intent"], "demo")
        self.assertFalse(json_payload["raw_replay_data_included"])
