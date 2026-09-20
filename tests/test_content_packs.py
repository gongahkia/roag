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
)


ROOT = Path(__file__).parents[1]
DATA_ROOT = ROOT / "jomon" / "data"


def write_manifest(root: Path, document: str) -> None:
    root.mkdir(parents=True, exist_ok=True)
    (root / "manifest.json").write_text(document, encoding="utf-8")


def alternate_pack(root: Path) -> Path:
    """Build a complete external fixture without committing copied game data."""
    shutil.copytree(DATA_ROOT, root / "data")
    write_manifest(
        root,
        '{"id": "fixture-alternate", "display_name": "Fixture Alternate", "format_version": 1}',
    )
    source = root / "data" / "world_text.json"
    world_text = json.loads(source.read_text(encoding="utf-8"))
    world_text["seed_words"][0] = "fixture-reed"
    source.write_text(json.dumps(world_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    return root


class ContentPackTests(unittest.TestCase):
    def test_default_pack_keeps_existing_catalog_root_and_data(self):
        pack = bundled_default_pack()
        self.assertEqual(pack.id, "default")
        self.assertEqual(pack.catalog_root, DATA_ROOT)
        expected = json.loads((DATA_ROOT / "world_text.json").read_text(encoding="utf-8"))
        self.assertEqual(load_catalog("world_text.json", WORLD_TEXT_SECTIONS), expected)

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


if __name__ == "__main__":
    unittest.main()
