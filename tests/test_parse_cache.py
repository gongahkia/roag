from __future__ import annotations

import io
import json
import time
import unittest
from contextlib import redirect_stdout
from hashlib import blake2b
from pathlib import Path
from tempfile import TemporaryDirectory
from typing import Any, cast
from unittest import mock

from kenjaku.cli import build_parser, main
from kenjaku.io import (
    parse_tenhou_xml_dataset,
    parse_tenhou_xml_file,
    parse_tenhou_xml_file_cached,
)
from kenjaku.io.parse_cache import (
    CACHE_KIND,
    tenhou_game_from_payload,
    tenhou_game_payload_from_xml_file,
)

FIXTURE_DIR = Path("data/fixtures/tenhou")
MINIMAL_FIXTURE = FIXTURE_DIR / "minimal_4p.xml"
EVENTS_FIXTURE = FIXTURE_DIR / "events_4p.xml"


class TenhouParseCacheTests(unittest.TestCase):
    def test_cached_parse_round_trips_and_writes_digest_file(self) -> None:
        with TemporaryDirectory() as directory:
            cache_dir = Path(directory)
            cached = parse_tenhou_xml_file_cached(MINIMAL_FIXTURE, cache_dir)
            uncached = parse_tenhou_xml_file(MINIMAL_FIXTURE)
            cache_files = tuple(cache_dir.glob("*.json"))

            self.assertEqual(cached, uncached)
            self.assertEqual(len(cache_files), 1)
            payload = json.loads(cache_files[0].read_text(encoding="utf-8"))
            self.assertEqual(payload["kind"], CACHE_KIND)
            self.assertEqual(
                payload["source_digest"],
                blake2b(MINIMAL_FIXTURE.read_bytes()).hexdigest(),
            )

    def test_direct_xml_payload_matches_normal_parser(self) -> None:
        for fixture in (
            MINIMAL_FIXTURE,
            EVENTS_FIXTURE,
            FIXTURE_DIR / "ryuukyoku_4p.xml",
            Path("data/fixtures/sanma/sanma_kita_3p.xml"),
        ):
            with self.subTest(fixture=fixture):
                payload = tenhou_game_payload_from_xml_file(fixture)
                self.assertEqual(tenhou_game_from_payload(payload), parse_tenhou_xml_file(fixture))

    def test_warm_cache_does_not_reparse_xml(self) -> None:
        with TemporaryDirectory() as directory:
            cache_dir = Path(directory)
            expected = parse_tenhou_xml_file_cached(MINIMAL_FIXTURE, cache_dir)

            with mock.patch("kenjaku.io.parse_cache.parse_tenhou_xml") as parse_mock:
                parse_mock.side_effect = AssertionError("warm cache missed")
                cached = parse_tenhou_xml_file_cached(MINIMAL_FIXTURE, cache_dir)

            self.assertEqual(cached, expected)
            parse_mock.assert_not_called()

    def test_cache_invalidates_by_content(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "game.xml"
            cache_dir = root / "cache"
            path.write_bytes(MINIMAL_FIXTURE.read_bytes())
            first = parse_tenhou_xml_file_cached(path, cache_dir)

            path.write_bytes(EVENTS_FIXTURE.read_bytes())
            second = parse_tenhou_xml_file_cached(path, cache_dir)

            self.assertEqual(first, parse_tenhou_xml_file(MINIMAL_FIXTURE))
            self.assertEqual(second, parse_tenhou_xml_file(EVENTS_FIXTURE))
            self.assertEqual(len(tuple(cache_dir.glob("*.json"))), 2)

    def test_corrupt_cache_file_is_rebuilt(self) -> None:
        with TemporaryDirectory() as directory:
            cache_dir = Path(directory) / "cache"
            digest = blake2b(MINIMAL_FIXTURE.read_bytes()).hexdigest()
            cache_path = cache_dir / f"{digest}.json"
            cache_dir.mkdir()
            cache_path.write_text("{not-json", encoding="utf-8")

            cached = parse_tenhou_xml_file_cached(MINIMAL_FIXTURE, cache_dir)

            self.assertEqual(cached, parse_tenhou_xml_file(MINIMAL_FIXTURE))
            self.assertEqual(json.loads(cache_path.read_text(encoding="utf-8"))["kind"], CACHE_KIND)

    def test_dataset_parse_cache_writes_one_entry_per_fixture_file(self) -> None:
        with TemporaryDirectory() as directory:
            cache_dir = Path(directory)
            dataset = parse_tenhou_xml_dataset([FIXTURE_DIR], parse_cache_dir=cache_dir)

            self.assertEqual(len(dataset.files), 3)
            self.assertEqual(len(tuple(cache_dir.glob("*.json"))), len(dataset.files))

    def test_warm_cache_reparse_is_under_five_ms_per_file(self) -> None:
        with TemporaryDirectory() as directory:
            cache_dir = Path(directory)
            parse_tenhou_xml_file_cached(MINIMAL_FIXTURE, cache_dir)

            runs = 25
            start = time.perf_counter()
            for _ in range(runs):
                parse_tenhou_xml_file_cached(MINIMAL_FIXTURE, cache_dir)
            per_file = (time.perf_counter() - start) / runs

            self.assertLess(per_file, 0.005)

    def test_fixture_files_warm_cache_is_five_times_faster_than_cold(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            files = _write_cache_timing_fixtures(root / "fixtures")
            runs = 20

            cold_start = time.perf_counter()
            for index in range(runs):
                cold_cache = root / f"cold-{index}"
                for file in files:
                    parse_tenhou_xml_file_cached(file, cold_cache)
            cold = time.perf_counter() - cold_start

            warm_cache = root / "warm"
            for file in files:
                parse_tenhou_xml_file_cached(file, warm_cache)
            warm_start = time.perf_counter()
            for _ in range(runs):
                for file in files:
                    parse_tenhou_xml_file_cached(file, warm_cache)
            warm = time.perf_counter() - warm_start

            self.assertLess(warm * 5, cold, f"cold={cold:.6f}s warm={warm:.6f}s")

    def test_parse_cache_flag_is_available_on_xml_parsing_commands(self) -> None:
        parser = build_parser()
        subparsers = cast(
            Any, next(action for action in parser._actions if action.dest == "command")
        )
        commands = (
            "inspect-tenhou",
            "defense-risk-summary",
            "benchmark-deal-in",
            "train-placement",
            "export-decision-snapshots",
            "train-discard-baseline",
            "train-discard-linear",
            "train-discard-mlp",
            "train-discard-transformer",
            "export-bc-examples",
            "benchmark-discard",
            "benchmark-discard-mlp",
            "benchmark-discard-transformer",
            "benchmark-call",
            "benchmark-riichi",
        )

        for command in commands:
            with self.subTest(command=command):
                command_parser = subparsers.choices[command]
                self.assertTrue(
                    any(
                        "--parse-cache" in action.option_strings
                        for action in command_parser._actions
                    )
                )

    def test_jobs_flag_is_available_on_xml_parsing_commands(self) -> None:
        parser = build_parser()
        subparsers = cast(
            Any, next(action for action in parser._actions if action.dest == "command")
        )
        commands = (
            "inspect-tenhou",
            "defense-risk-summary",
            "benchmark-deal-in",
            "train-placement",
            "export-decision-snapshots",
            "train-discard-baseline",
            "train-discard-linear",
            "train-discard-mlp",
            "train-discard-transformer",
            "export-bc-examples",
            "benchmark-discard",
            "benchmark-discard-mlp",
            "benchmark-discard-transformer",
            "benchmark-call",
            "benchmark-riichi",
        )

        for command in commands:
            with self.subTest(command=command):
                command_parser = subparsers.choices[command]
                self.assertTrue(
                    any("--jobs" in action.option_strings for action in command_parser._actions)
                )

    def test_inspect_tenhou_uses_parse_cache_flag(self) -> None:
        with TemporaryDirectory() as directory:
            cache_dir = Path(directory) / "cache"
            with redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "inspect-tenhou",
                        str(FIXTURE_DIR),
                        "--parse-cache",
                        str(cache_dir),
                    ]
                )

            self.assertEqual(exit_code, 0)
            self.assertEqual(len(tuple(cache_dir.glob("*.json"))), 3)


def _write_cache_timing_fixtures(root: Path) -> tuple[Path, ...]:
    root.mkdir()
    ignored_tags = "".join(f'  <GO type="{index}" />\n' for index in range(1000))
    files: list[Path] = []
    for source in sorted(FIXTURE_DIR.glob("*.xml")):
        target = root / source.name
        target.write_text(
            source.read_text(encoding="utf-8").replace("</mjloggm>", f"{ignored_tags}</mjloggm>"),
            encoding="utf-8",
        )
        files.append(target)
    return tuple(files)


if __name__ == "__main__":
    unittest.main()
