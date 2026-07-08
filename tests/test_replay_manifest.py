from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.io import (
    REPLAY_INTAKE_ACCEPTED_ITEM_KIND,
    REPLAY_INTAKE_REVIEW_KIND,
    REPLAY_MANIFEST_KIND,
    REPLAY_PUBLIC_SUMMARY_KIND,
    REPLAY_SHARE_PLAN_KIND,
    accepted_replay_intake_items,
    build_replay_public_summary,
    build_replay_public_summary_file,
    build_replay_share_plan,
    build_replay_share_plan_file,
    format_replay_intake_review,
    format_replay_public_summary,
    format_replay_share_plan,
    review_replay_manifest,
    review_replay_manifest_file,
    write_accepted_replay_intake_jsonl,
)


class ReplayManifestTests(unittest.TestCase):
    def test_review_manifest_accepts_only_permission_covered_items(self) -> None:
        review = review_replay_manifest(_manifest_payload())

        self.assertEqual(review["kind"], REPLAY_INTAKE_REVIEW_KIND)
        self.assertEqual(review["items"], 6)
        self.assertEqual(review["accepted"], 2)
        self.assertEqual(review["rejected"], 4)
        self.assertEqual(review["platforms"]["mahjong_soul"], 3)
        self.assertEqual(review["permission_statuses"]["user_provided"], 2)
        accepted_ids = [decision["id"] for decision in review["decisions"] if decision["accepted"]]
        self.assertEqual(accepted_ids, ["synthetic-training", "mjs-explicit-analysis"])
        rejected = {
            decision["id"]: decision["reasons"]
            for decision in review["decisions"]
            if not decision["accepted"]
        }
        self.assertTrue(
            any(
                "mahjong_soul replay intake requires explicit permission" in reason
                for reason in rejected["mjs-review"]
            )
        )
        self.assertTrue(
            any(
                "mahjong_soul replay intake requires explicit permission" in reason
                for reason in rejected["mjs-training"]
            )
        )
        self.assertIn("tenhou replay redistribution", rejected["tenhou-redistribution"][0])
        self.assertIn("permission status is unknown", rejected["unknown-permission"][0])

    def test_replay_manifest_file_and_accepted_jsonl(self) -> None:
        with TemporaryDirectory() as directory:
            manifest = Path(directory) / "manifest.json"
            accepted = Path(directory) / "accepted.jsonl"
            manifest.write_text(json.dumps(_manifest_payload()), encoding="utf-8")

            review = review_replay_manifest_file(manifest)
            write_accepted_replay_intake_jsonl(accepted, review)
            rows = [json.loads(line) for line in accepted.read_text(encoding="utf-8").splitlines()]

        self.assertEqual(review["manifest_path"], str(manifest))
        self.assertEqual(len(rows), 2)
        self.assertTrue(all(row["kind"] == REPLAY_INTAKE_ACCEPTED_ITEM_KIND for row in rows))
        self.assertEqual(rows[0]["id"], "synthetic-training")

    def test_accepted_items_and_text_summary(self) -> None:
        review = review_replay_manifest(_manifest_payload())
        accepted = accepted_replay_intake_items(review)
        text = format_replay_intake_review(review)

        self.assertEqual(
            [row["id"] for row in accepted],
            ["synthetic-training", "mjs-explicit-analysis"],
        )
        self.assertIn("items: 6", text)
        self.assertIn("accepted: 2", text)
        self.assertIn("rejected: 4", text)
        self.assertIn("permission_statuses:", text)
        self.assertIn("rejections:", text)

    def test_explicit_permission_requires_scope(self) -> None:
        payload = {
            "kind": REPLAY_MANIFEST_KIND,
            "items": [
                {
                    "id": "missing-scope",
                    "platform": "other",
                    "uri": "https://example.test/replay",
                    "intended_uses": ["training"],
                    "permission": {"status": "explicit_permission"},
                }
            ],
        }

        review = review_replay_manifest(payload)

        self.assertEqual(review["accepted"], 0)
        self.assertEqual(review["rejected"], 1)
        self.assertIn("requires a permission scope", review["decisions"][0]["reasons"][0])

    def test_share_plan_requires_intended_use_and_permission_scope(self) -> None:
        rows = [
            {
                "kind": REPLAY_INTAKE_ACCEPTED_ITEM_KIND,
                "id": "demo-ready",
                "platform": "other",
                "uri": "https://example.test/replay",
                "intended_uses": ["analysis", "demo"],
                "permission": {
                    "status": "explicit_permission",
                    "scope": ["analysis", "demo"],
                },
            },
            {
                "kind": REPLAY_INTAKE_ACCEPTED_ITEM_KIND,
                "id": "analysis-only",
                "platform": "mahjong_soul",
                "uri": "https://mahjongsoul.game.yo-star.com/?paipu=abc",
                "intended_uses": ["analysis"],
                "permission": {
                    "status": "user_provided",
                    "scope": ["analysis", "evaluation"],
                },
            },
        ]

        plan = build_replay_share_plan(rows, intent="demo")
        text = format_replay_share_plan(plan)

        self.assertEqual(plan["kind"], REPLAY_SHARE_PLAN_KIND)
        self.assertEqual(plan["items"], 2)
        self.assertEqual(plan["shareable"], 1)
        self.assertEqual(plan["blocked"], 1)
        self.assertTrue(plan["decisions"][0]["shareable"])
        self.assertFalse(plan["decisions"][1]["shareable"])
        self.assertIn("accepted item intended_uses does not include demo", text)
        self.assertIn("permission scope does not include demo", text)

    def test_share_plan_file_reads_accepted_queue(self) -> None:
        review = review_replay_manifest(
            {
                "kind": REPLAY_MANIFEST_KIND,
                "items": [
                    {
                        "id": "demo-ready",
                        "platform": "other",
                        "uri": "https://example.test/replay",
                        "intended_uses": ["analysis", "demo"],
                        "permission": {
                            "status": "explicit_permission",
                            "scope": ["analysis", "demo"],
                        },
                    }
                ],
            }
        )
        with TemporaryDirectory() as directory:
            accepted = Path(directory) / "accepted.jsonl"
            write_accepted_replay_intake_jsonl(accepted, review)

            plan = build_replay_share_plan_file(accepted, intent="demo")

        self.assertEqual(plan["accepted_input_path"], str(accepted))
        self.assertEqual(plan["shareable"], 1)

    def test_public_summary_sanitizes_shareable_rows_and_keeps_block_reasons(self) -> None:
        rows = [
            {
                "kind": REPLAY_INTAKE_ACCEPTED_ITEM_KIND,
                "id": "demo-ready",
                "platform": "local_file",
                "uri": "/private/replays/demo-ready.xml",
                "intended_uses": ["analysis", "demo"],
                "permission": {
                    "status": "explicit_permission",
                    "scope": ["analysis", "demo"],
                    "granted_by": "unit-test",
                    "granted_at": "2026-06-12",
                    "notes": "private room export",
                },
            },
            {
                "kind": REPLAY_INTAKE_ACCEPTED_ITEM_KIND,
                "id": "analysis-only",
                "platform": "local_file",
                "uri": "/private/replays/analysis-only.xml",
                "intended_uses": ["analysis"],
                "permission": {
                    "status": "user_provided",
                    "scope": ["analysis", "evaluation"],
                },
            },
        ]

        summary = build_replay_public_summary(rows, intent="demo")
        text = format_replay_public_summary(summary)
        encoded = json.dumps(summary, sort_keys=True)

        self.assertEqual(summary["kind"], REPLAY_PUBLIC_SUMMARY_KIND)
        self.assertEqual(summary["shareable"], 1)
        self.assertEqual(summary["blocked"], 1)
        self.assertFalse(summary["raw_replay_data_included"])
        self.assertFalse(summary["raw_replay_uris_included"])
        self.assertFalse(summary["accepted_input_path_included"])
        self.assertIsNone(summary["accepted_input_path"])
        self.assertEqual(summary["public_summaries"][0]["id"], "demo-ready")
        self.assertEqual(summary["public_summaries"][0]["intent"], "demo")
        self.assertEqual(summary["public_summaries"][0]["platform"], "local_file")
        self.assertFalse(summary["public_summaries"][0]["raw_uri_included"])
        self.assertIsNotNone(summary["public_summaries"][0]["uri_fingerprint"])
        self.assertNotIn("/private/replays/demo-ready.xml", encoded)
        self.assertNotIn("/private/replays/analysis-only.xml", encoded)
        self.assertIn("accepted item intended_uses does not include demo", encoded)
        self.assertIn("raw_replay_uris_included: no", text)
        self.assertIn("public_summaries:", text)
        self.assertIn("blocked_items:", text)

    def test_public_summary_file_reads_accepted_queue(self) -> None:
        review = review_replay_manifest(
            {
                "kind": REPLAY_MANIFEST_KIND,
                "items": [
                    {
                        "id": "demo-ready",
                        "platform": "synthetic",
                        "uri": "data/fixtures/replay/synthetic.json",
                        "intended_uses": ["analysis", "demo"],
                        "permission": {"status": "local_synthetic"},
                    }
                ],
            }
        )
        with TemporaryDirectory() as directory:
            accepted = Path(directory) / "accepted.jsonl"
            write_accepted_replay_intake_jsonl(accepted, review)

            summary = build_replay_public_summary_file(accepted, intent="demo")

        self.assertIsNone(summary["accepted_input_path"])
        self.assertFalse(summary["accepted_input_path_included"])
        self.assertEqual(summary["shareable"], 1)
        self.assertEqual(summary["public_summaries"][0]["id"], "demo-ready")


def _manifest_payload() -> dict:
    return {
        "kind": REPLAY_MANIFEST_KIND,
        "source": {"label": "unit-test"},
        "items": [
            {
                "id": "synthetic-training",
                "platform": "synthetic",
                "uri": "data/fixtures/replay/synthetic.json",
                "intended_uses": ["analysis", "training"],
                "permission": {"status": "local_synthetic"},
            },
            {
                "id": "mjs-review",
                "platform": "mahjong_soul",
                "uri": "https://mahjongsoul.game.yo-star.com/?paipu=abc",
                "intended_uses": ["analysis"],
                "permission": {"status": "user_provided"},
            },
            {
                "id": "mjs-explicit-analysis",
                "platform": "mahjong_soul",
                "uri": "https://mahjongsoul.game.yo-star.com/?paipu=explicit",
                "intended_uses": ["analysis"],
                "permission": {
                    "status": "explicit_permission",
                    "scope": ["analysis"],
                    "granted_by": "unit-test",
                    "granted_at": "2026-06-12",
                },
            },
            {
                "id": "mjs-training",
                "platform": "mahjong_soul",
                "uri": "https://mahjongsoul.game.yo-star.com/?paipu=def",
                "intended_uses": ["training"],
                "permission": {"status": "user_provided"},
            },
            {
                "id": "tenhou-redistribution",
                "platform": "tenhou",
                "uri": "https://tenhou.net/0/?log=2026010100gm-0000-0000",
                "intended_uses": ["redistribution"],
                "permission": {
                    "status": "explicit_permission",
                    "scope": ["redistribution"],
                },
            },
            {
                "id": "unknown-permission",
                "platform": "other",
                "uri": "https://example.test/replay",
                "intended_uses": ["analysis"],
                "permission": {"status": "unknown"},
            },
        ],
    }


if __name__ == "__main__":
    unittest.main()
