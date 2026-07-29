from __future__ import annotations

import io
import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.io import TenhouParseFailure, parse_tenhou_xml_file
from kenjaku.training.bc_examples import (
    BC_EXAMPLE_MANIFEST_KIND,
    BC_EXAMPLE_ROW_KIND,
    BcExampleShard,
    bc_example_files,
    bc_example_from_payload,
    bc_example_to_payload,
    build_bc_manifest,
    parse_bc_decision_types,
    read_bc_examples,
    write_bc_example_row,
    write_bc_manifest,
)
from kenjaku.training.call_examples import iter_call_examples
from kenjaku.training.discard_examples import iter_discard_examples
from kenjaku.training.riichi_examples import iter_riichi_examples

MINIMAL_FIXTURE = Path("data/fixtures/tenhou/minimal_4p.xml")
EVENTS_FIXTURE = Path("data/fixtures/tenhou/events_4p.xml")


class BcExampleTests(unittest.TestCase):
    def test_parse_bc_decision_types_strips_and_deduplicates(self) -> None:
        self.assertEqual(parse_bc_decision_types(" discard,call,discard "), ("discard", "call"))

    def test_parse_bc_decision_types_rejects_empty_or_unknown(self) -> None:
        with self.assertRaisesRegex(ValueError, "at least one"):
            parse_bc_decision_types(" , ")
        with self.assertRaisesRegex(ValueError, "unsupported"):
            parse_bc_decision_types("discard,kan")

    def test_discard_example_payload_round_trips(self) -> None:
        example = list(iter_discard_examples(parse_tenhou_xml_file(MINIMAL_FIXTURE)))[0]
        payload = bc_example_to_payload(example)
        restored = bc_example_from_payload("discard", payload)

        self.assertEqual(restored.action, example.action)
        self.assertEqual(restored.hand_counts, example.hand_counts)

    def test_call_and_riichi_payloads_round_trip(self) -> None:
        game = parse_tenhou_xml_file(EVENTS_FIXTURE)
        call = list(iter_call_examples(game))[0]
        riichi = list(iter_riichi_examples(game))[0]

        self.assertEqual(bc_example_from_payload("call", bc_example_to_payload(call)), call)
        self.assertEqual(bc_example_from_payload("riichi", bc_example_to_payload(riichi)), riichi)

    def test_write_bc_example_row_emits_compact_jsonl(self) -> None:
        example = list(iter_discard_examples(parse_tenhou_xml_file(MINIMAL_FIXTURE)))[0]
        handle = io.StringIO()

        write_bc_example_row(
            handle,
            decision_type="discard",
            source_file=MINIMAL_FIXTURE,
            source_file_index=2,
            sequence_index=3,
            example=example,
        )
        row = json.loads(handle.getvalue())

        self.assertEqual(row["kind"], BC_EXAMPLE_ROW_KIND)
        self.assertEqual(row["source_file_index"], 2)
        self.assertEqual(row["sequence_index"], 3)

    def test_manifest_read_loads_metadata_and_limit(self) -> None:
        example = list(iter_discard_examples(parse_tenhou_xml_file(MINIMAL_FIXTURE)))[0]
        with TemporaryDirectory() as directory:
            root = Path(directory)
            shard = root / "discard-000.jsonl"
            with shard.open("w", encoding="utf-8") as handle:
                write_bc_example_row(
                    handle,
                    decision_type="discard",
                    source_file=MINIMAL_FIXTURE,
                    source_file_index=0,
                    sequence_index=0,
                    example=example,
                )
            manifest = _manifest(root, shard, failures=[_failure()])
            self.assertEqual(manifest["kind"], BC_EXAMPLE_MANIFEST_KIND)
            write_bc_manifest(root / "manifest.json", manifest)
            load = read_bc_examples([root], decision_type="discard", limit=1)

        self.assertEqual(load.total_examples, 1)
        self.assertEqual(load.decision_counts["discard"], 1)
        self.assertEqual(load.parse_failures[0].error_type, "ValueError")

    def test_bc_example_files_falls_back_to_directory_glob(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-000.jsonl"
            path.write_text("", encoding="utf-8")
            files, manifests = bc_example_files([directory], decision_type="discard")

        self.assertEqual(len(files), 1)
        self.assertEqual(manifests, ())

    def test_read_bc_examples_rejects_negative_limit_and_bad_rows(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-000.jsonl"
            path.write_text('{"kind":"wrong"}\n', encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "limit"):
                read_bc_examples([path], decision_type="discard", limit=-1)
            with self.assertRaisesRegex(ValueError, "not a BC example row"):
                read_bc_examples([path], decision_type="discard")

    def test_bc_example_from_payload_rejects_bad_inputs(self) -> None:
        with self.assertRaisesRegex(ValueError, "payload must be an object"):
            bc_example_from_payload("discard", [])
        with self.assertRaisesRegex(ValueError, "unsupported"):
            bc_example_from_payload("kan", {})


def _manifest(
    root: Path,
    shard: Path,
    *,
    failures: list[TenhouParseFailure] | None = None,
) -> dict[str, object]:
    return build_bc_manifest(
        input_paths=[MINIMAL_FIXTURE],
        xml_files=[MINIMAL_FIXTURE],
        parsed_files=[MINIMAL_FIXTURE],
        parse_failures=failures or [],
        game_counts={"rounds": 1},
        decision_counts={"discard": 1},
        shards=[BcExampleShard(path=shard, decision_type="discard", examples=1)],
        source={"label": "unit", "command": None, "date": "2026-07-08"},
        output_dir=root,
        shard_size=1,
        source_complete=True,
    )


def _failure() -> TenhouParseFailure:
    return TenhouParseFailure(Path("bad.xml"), "ValueError", "bad")


if __name__ == "__main__":
    unittest.main()
