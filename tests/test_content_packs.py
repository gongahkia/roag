from __future__ import annotations

import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

from jomon.catalog import (
    ContentPackError,
    WORLD_TEXT_SECTIONS,
    bundled_default_pack,
    load_catalog,
    load_content_pack,
    region_contract,
)


ROOT = Path(__file__).parents[1]
DATA_ROOT = ROOT / "jomon" / "data"
DEFAULT_PACK_ROOT = ROOT / "jomon" / "content_packs" / "default"


def write_manifest(root: Path, document: str) -> None:
    root.mkdir(parents=True, exist_ok=True)
    (root / "manifest.json").write_text(document, encoding="utf-8")


def alternate_pack(root: Path) -> Path:
    """Build a complete external fixture without committing copied game data."""
    shutil.copytree(DATA_ROOT, root / "data")
    shutil.copy(DEFAULT_PACK_ROOT / "regions.json", root / "regions.json")
    write_manifest(
        root,
        '{"id": "fixture-alternate", "display_name": "Fixture Alternate", "format_version": 1}',
    )
    source = root / "data" / "world_text.json"
    world_text = json.loads(source.read_text(encoding="utf-8"))
    world_text["seed_words"][0] = "fixture-reed"
    source.write_text(json.dumps(world_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "regions.json"
    regions = json.loads(source.read_text(encoding="utf-8"))
    regions["regions"]["region.family_1"] = {
        "display_name": "Fixture Hearth",
        "route_label": "Fixture Ford",
        "short_description": "fixture river presentation",
    }
    source.write_text(json.dumps(regions, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    return root


def world_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.state import create_world; "
            "from jomon.terminal import RouteChartView, route_detail_lines, _status_lines; "
            "state = create_world('regional-pack-proof'); node = state.route_nodes['hearthford']; "
            "state.location = 'region'; state.position = state.region.landmarks['landing']; "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'region': [state.region.id, state.region.name], "
            "'route': [node.id, node.name, node.description], "
            "'route_detail': route_detail_lines(state, RouteChartView('hearthford'), 80), "
            "'status': _status_lines(state), 'signature': state.region.geography_signature, "
            "'levels': state.region.levels, "
            "'landmarks': sorted((key, point.x, point.y, point.z) for key, point in state.region.landmarks.items()), "
            "'edges': [(edge.id, edge.first, edge.second, edge.travel_time, edge.supply_cost, edge.cargo_risk, edge.weather_exposure) for edge in state.route_edges]}))",
        ],
        cwd=ROOT,
        env=environment,
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class ContentPackTests(unittest.TestCase):
    def test_default_pack_keeps_existing_catalog_root_and_data(self):
        pack = bundled_default_pack()
        self.assertEqual(pack.id, "default")
        self.assertEqual(pack.catalog_root, DATA_ROOT)
        expected = json.loads((DATA_ROOT / "world_text.json").read_text(encoding="utf-8"))
        self.assertEqual(load_catalog("world_text.json", WORLD_TEXT_SECTIONS), expected)
        self.assertEqual(
            [(slot.id, slot.engine_id) for slot in region_contract()],
            [
                ("region.family_1", "hearthford"),
                ("region.family_2", "greywash"),
                ("region.family_3", "greenwold"),
                ("region.family_4", "whitecairn"),
                ("region.family_5", "dunmire"),
                ("region.family_6", "rillscar"),
                ("region.family_7", "marlbank"),
                ("region.family_8", "frostmere"),
            ],
        )

    def test_invalid_pack_manifests_and_missing_catalogs_name_the_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with self.subTest("missing manifest"):
                with self.assertRaisesRegex(ContentPackError, r"manifest.*" + str(root)):
                    load_content_pack(root)

            with self.subTest("invalid JSON"):
                write_manifest(root, "{")
                with self.assertRaisesRegex(ContentPackError, r"invalid content-pack manifest"):
                    load_content_pack(root)

            with self.subTest("missing ID"):
                write_manifest(root, '{"display_name":"Broken", "format_version":1}')
                with self.assertRaisesRegex(ContentPackError, r"missing id"):
                    load_content_pack(root)

            with self.subTest("duplicate manifest key"):
                write_manifest(root, '{"id":"one", "id":"two", "display_name":"Broken", "format_version":1}')
                with self.assertRaisesRegex(ContentPackError, r"duplicate catalog key"):
                    load_content_pack(root)

            with self.subTest("missing catalog"):
                write_manifest(root, '{"id":"broken", "display_name":"Broken", "format_version":1}')
                (root / "data").mkdir()
                with self.assertRaisesRegex(ContentPackError, r"broken.*missing required catalog.*world_text\.json"):
                    load_content_pack(root)

    def test_regional_presentation_contract_rejects_invalid_pack_data(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "regions.json"

            cases = {
                "missing required slot": lambda value: value["regions"].pop("region.family_8"),
                "unknown slot": lambda value: value["regions"].update({"region.family_9": value["regions"].pop("region.family_8")}),
                "missing display field": lambda value: value["regions"]["region.family_1"].pop("display_name"),
                "wrong display field type": lambda value: value["regions"]["region.family_1"].update({"display_name": 1}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    content = json.loads((DEFAULT_PACK_ROOT / "regions.json").read_text(encoding="utf-8"))
                    mutate(content)
                    source.write_text(json.dumps(content), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*regions\.json"):
                        load_content_pack(root)

            with self.subTest("duplicate semantic slot"):
                content = (DEFAULT_PACK_ROOT / "regions.json").read_text(encoding="utf-8")
                source.write_text(
                    content.replace('"region.family_2"', '"region.family_1"', 1), encoding="utf-8"
                )
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*region\.family_1"):
                    load_content_pack(root)

            with self.subTest("malformed JSON"):
                source.write_text("{", encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*regions\.json"):
                    load_content_pack(root)

            with self.subTest("missing presentation file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*regions\.json"):
                    load_content_pack(root)

    def test_environment_selects_complete_alternate_pack_before_main_import(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            self.assertEqual(load_content_pack(root).catalog_root, root / "data")

            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            result = subprocess.run(
                [
                    sys.executable,
                    "-c",
                    "import jomon.main; from jomon.catalog import selected_content_pack; "
                    "print(selected_content_pack().id); print(jomon.main.SEED_WORDS[0])",
                ],
                cwd=ROOT,
                env=environment,
                text=True,
                capture_output=True,
                check=False,
            )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.splitlines(), ["fixture-alternate", "fixture-reed"])

        # The subprocess owns selection state, so the fixture cannot leak into
        # the test process or another test module.
        self.assertNotEqual(os.environ.get("JOMON_CONTENT_PACK"), str(root))

    def test_alternate_pack_changes_regional_presentation_not_generation_or_ids(self):
        default = world_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = world_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["region"][0], alternate["region"][0])
        self.assertEqual(alternate["region"], ["hearthford", "Fixture Hearth"])
        self.assertEqual(alternate["route"][:2], ["hearthford", "Fixture Ford"])
        self.assertIn("fixture river presentation", alternate["route"][2])
        self.assertIn("FIXTURE FORD", " ".join(alternate["route_detail"]))
        self.assertIn("Fixture Hearth", " ".join(alternate["status"]))
        for field in ("signature", "levels", "landmarks", "edges"):
            self.assertEqual(default[field], alternate[field], field)
        self.assertEqual(default["region"][1], "Hearthford")
        self.assertEqual(default["route"][:2], ["hearthford", "Hearthford"])

    def test_alternate_pack_loads_legacy_saved_region_names_without_rewriting_them(self):
        default_environment = dict(os.environ)
        default_environment.pop("JOMON_CONTENT_PACK", None)
        generated = subprocess.run(
            [sys.executable, "-c", "import json; from jomon.state import create_world; print(json.dumps(create_world('legacy region save').to_dict()))"],
            cwd=ROOT,
            env=default_environment,
            text=True,
            capture_output=True,
            check=False,
        )
        self.assertEqual(generated.returncode, 0, generated.stderr)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            save = Path(directory) / "legacy.json"
            save.write_text(generated.stdout, encoding="utf-8")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            loaded = subprocess.run(
                [
                    sys.executable,
                    "-c",
                    "import json, sys; from jomon.state import game_state_from_dict; "
                    "state = game_state_from_dict(json.load(open(sys.argv[1], encoding='utf-8'))); "
                    "print(json.dumps([state.region.name, state.route_nodes['hearthford'].name]))",
                    str(save),
                ],
                cwd=ROOT,
                env=environment,
                text=True,
                capture_output=True,
                check=False,
            )
        self.assertEqual(loaded.returncode, 0, loaded.stderr)
        self.assertEqual(json.loads(loaded.stdout), ["Hearthford", "Hearthford"])

    def test_selected_pack_reports_its_catalog_schema_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            (root / "data" / "world_text.json").write_text("{}\n", encoding="utf-8")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            result = subprocess.run(
                [sys.executable, "-c", "import jomon.main"],
                cwd=ROOT,
                env=environment,
                text=True,
                capture_output=True,
                check=False,
            )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("invalid world_text.json in content pack 'fixture-alternate'", result.stderr)
        self.assertIn("must contain exactly", result.stderr)


if __name__ == "__main__":
    unittest.main()
