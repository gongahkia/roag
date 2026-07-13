from __future__ import annotations

import inspect
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

import kenjaku
from kenjaku import api

EXPECTED_EXPORTS = [
    "parse_tenhou_xml_file",
    "parse_tenhou_xml_dataset",
    "TenhouGame",
    "TenhouParseFailure",
    "DiscardExample",
    "CallExample",
    "RiichiExample",
    "iter_discard_examples",
    "iter_call_examples",
    "iter_riichi_examples",
    "RISK_CONTEXT_FEATURE_PROFILE",
    "DEFENSE_CONTEXT_FEATURE_PROFILE",
    "SHANTEN_FEATURE_PROFILE",
    "RAW_COUNT_FEATURE_PROFILE",
    "CALL_LINEAR_V1_FEATURE_PROFILE",
    "DiscardLinearModel",
    "CallLinearModel",
    "RiichiLinearModel",
    "DealInLinearModel",
    "DiscardFrequencyBaseline",
    "RiichiFrequencyBaseline",
    "CallFrequencyBaseline",
    "ActionV1",
    "ObservationV1",
    "export_decision_snapshots",
    "load_decision_snapshots",
    "build_interpretability_overlay",
]
FIXTURE = Path("data/fixtures/tenhou/minimal_4p.xml")


class PublicAPITests(unittest.TestCase):
    def test_package_reexports_api_module(self) -> None:
        self.assertIs(kenjaku.api, api)

    def test_imports_every_public_export(self) -> None:
        from kenjaku.api import DiscardLinearModel, parse_tenhou_xml_file

        self.assertIs(parse_tenhou_xml_file, api.parse_tenhou_xml_file)
        self.assertIs(DiscardLinearModel, api.DiscardLinearModel)
        self.assertEqual(api.__all__, EXPECTED_EXPORTS)
        for name in EXPECTED_EXPORTS:
            self.assertIsNotNone(getattr(api, name))

    def test_exports_have_docs_and_stability_markers(self) -> None:
        source = Path(api.__file__).read_text(encoding="utf-8")

        self.assertEqual(set(api.API_EXPORT_DOCS), set(EXPECTED_EXPORTS))
        for name in EXPECTED_EXPORTS:
            self.assertTrue(api.API_EXPORT_DOCS[name])
            self.assertIn(f"{name}", source)
            self.assertIn("# stable since 0.2.0", _source_line_for_name(source, name))
            exported = getattr(api, name)
            if inspect.isfunction(exported) or inspect.isclass(exported):
                self.assertTrue(inspect.getdoc(exported))

    def test_snapshot_helpers_round_trip_jsonl(self) -> None:
        game = api.parse_tenhou_xml_file(FIXTURE)

        with TemporaryDirectory() as directory:
            path = Path(directory) / "snapshots.jsonl"
            snapshots = api.export_decision_snapshots(game, path, limit=1)
            loaded = api.load_decision_snapshots(path)

        self.assertEqual(len(snapshots), 1)
        self.assertEqual(loaded, snapshots)


def _source_line_for_name(source: str, name: str) -> str:
    for line in source.splitlines():
        if name in line and "# stable since 0.2.0" in line:
            return line
    return ""


if __name__ == "__main__":
    unittest.main()
