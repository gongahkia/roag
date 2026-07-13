from __future__ import annotations

import contextlib
import io
import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest.mock import patch

from kenjaku.bot import bot_main
from kenjaku.bot.mjai import load_mjai_policy
from kenjaku.cli import main
from kenjaku.core import TileType
from kenjaku.models import DiscardLinearModel
from kenjaku.models.linear_discard import RAW_COUNT_FEATURE_DIM, RAW_COUNT_FEATURE_PROFILE
from kenjaku.schema import CheckpointManifestV1


class MjaiBotTests(unittest.TestCase):
    def test_bot_main_replies_to_request_action_and_echoes_request_id(self) -> None:
        stdout = io.StringIO()
        bot_main(
            ["--policy", "frequency", "--player-id", "0"],
            stdin=io.StringIO(
                "\n".join(
                    [
                        json.dumps({"type": "start_game", "id": 0}),
                        json.dumps(_start_kyoku()),
                        json.dumps(
                            {
                                "type": "request_action",
                                "request_id": 7,
                                "possible_actions": [
                                    {"type": "dahai", "actor": 0, "pai": "9m", "tsumogiri": False},
                                    {"type": "dahai", "actor": 0, "pai": "1m", "tsumogiri": False},
                                    {"type": "none"},
                                ],
                            }
                        ),
                    ]
                )
            ),
            stdout=stdout,
        )

        rows = _jsonl(stdout)
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["type"], "dahai")
        self.assertEqual(rows[0]["pai"], "1m")
        self.assertEqual(rows[0]["request_id"], 7)

    def test_legacy_event_batch_replies_with_discard(self) -> None:
        stdout = io.StringIO()
        events = [_start_kyoku(), {"type": "tsumo", "actor": 0, "pai": "5m"}]

        bot_main(
            ["--policy", "frequency", "--player-id", "0"],
            stdin=io.StringIO(json.dumps(events) + "\n"),
            stdout=stdout,
        )

        rows = _jsonl(stdout)
        self.assertEqual(rows[0]["type"], "dahai")
        self.assertEqual(rows[0]["actor"], 0)
        self.assertIn(rows[0]["pai"], {"1m", "5m", "9m"})

    def test_linear_policy_checkpoint_ranks_legal_discards(self) -> None:
        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "linear.json"
            weights = [[0.0] * RAW_COUNT_FEATURE_DIM for _ in range(34)]
            weights[TileType.parse("9m").index][0] = 5.0
            DiscardLinearModel(
                weights=tuple(tuple(row) for row in weights),
                epochs=0,
                learning_rate=0.1,
                feature_profile=RAW_COUNT_FEATURE_PROFILE,
            ).save(checkpoint)
            stdout = io.StringIO()

            bot_main(
                ["--policy", str(checkpoint), "--player-id", "0"],
                stdin=io.StringIO(
                    "\n".join(
                        [
                            json.dumps(_start_kyoku()),
                            json.dumps(
                                {
                                    "type": "request_action",
                                    "request_id": 8,
                                    "possible_actions": [
                                        {
                                            "type": "dahai",
                                            "actor": 0,
                                            "pai": "1m",
                                            "tsumogiri": False,
                                        },
                                        {
                                            "type": "dahai",
                                            "actor": 0,
                                            "pai": "9m",
                                            "tsumogiri": False,
                                        },
                                    ],
                                }
                            ),
                        ]
                    )
                ),
                stdout=stdout,
            )

        rows = _jsonl(stdout)
        self.assertEqual(rows[0]["pai"], "9m")
        self.assertEqual(rows[0]["request_id"], 8)

    def test_cli_bot_runs_stdin_stdout_adapter(self) -> None:
        stdout = io.StringIO()
        stdin = io.StringIO(
            json.dumps(
                {
                    "type": "request_action",
                    "request_id": 9,
                    "possible_actions": [{"type": "none"}],
                }
            )
            + "\n"
        )
        with contextlib.redirect_stdout(stdout), patch("sys.stdin", stdin):
            exit_code = main(["bot", "--policy", "frequency", "--player-id", "0"])

        rows = _jsonl(stdout)
        self.assertEqual(exit_code, 0)
        self.assertEqual(rows, [{"type": "none", "request_id": 9}])

    def test_loads_linear_checkpoint_only_with_compatible_manifest(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            checkpoint = root / "linear.json"
            DiscardLinearModel(
                weights=tuple(tuple(0.0 for _ in range(RAW_COUNT_FEATURE_DIM)) for _ in range(34)),
                epochs=0,
                learning_rate=0.1,
                feature_profile=RAW_COUNT_FEATURE_PROFILE,
            ).save(checkpoint)
            manifest = CheckpointManifestV1.for_current_schemas(
                checkpoint_id="linear-v1",
                model_kind="discard-linear-v1",
                model_version="1.0.0",
                rulesets=("tenhou-4p",),
            )
            manifest_path = root / "manifest.json"
            manifest_path.write_text(manifest.to_json(), encoding="utf-8")

            policy = load_mjai_policy(checkpoint, checkpoint_manifest=manifest_path)

            self.assertEqual(type(policy).__name__, "LinearDiscardBotPolicy")
            incompatible = CheckpointManifestV1.for_current_schemas(
                checkpoint_id="linear-3p",
                model_kind="discard-linear-v1",
                model_version="1.0.0",
                rulesets=("tenhou-3p",),
            )
            manifest_path.write_text(incompatible.to_json(), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "ruleset is unsupported"):
                load_mjai_policy(checkpoint, checkpoint_manifest=manifest_path)

    def test_submission_artifacts_exist(self) -> None:
        self.assertTrue(Path("dockerfiles/mjai-bot.Dockerfile").exists())
        self.assertTrue(Path("scripts/build_mjai_submission.sh").exists())


def _start_kyoku() -> dict:
    return {
        "type": "start_kyoku",
        "bakaze": "E",
        "kyoku": 1,
        "honba": 0,
        "kyotaku": 0,
        "oya": 0,
        "scores": [25000, 25000, 25000, 25000],
        "tehais": [["1m", "5m", "9m"], ["?"] * 13, ["?"] * 13, ["?"] * 13],
        "dora_marker": "2p",
    }


def _jsonl(output: io.StringIO) -> list[dict]:
    return [json.loads(line) for line in output.getvalue().splitlines()]


if __name__ == "__main__":
    unittest.main()
