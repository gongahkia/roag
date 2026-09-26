from __future__ import annotations

import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

from jomon.mechanical_compatibility import (
    MECHANICAL_COMPATIBILITY_VERSION,
    MECHANICAL_PROJECTION_FORMAT,
    MechanicalProjectionError,
    canonical_mechanical_projection_bytes,
    main_world_mechanical_fingerprint,
    main_world_mechanical_projection,
)

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_PACK = ROOT / "jomon" / "content_packs" / "default"
DATA = ROOT / "jomon" / "data"


class MechanicalCompatibilityTests(unittest.TestCase):
    def _pack(self, directory: Path, label: str) -> Path:
        root = directory / label
        pack = root / "content_packs" / "default"
        shutil.copytree(DEFAULT_PACK, pack)
        shutil.copytree(DATA, root / "data")
        return pack

    def _fingerprint(self, pack: Path) -> str:
        environment = dict(os.environ, JOMON_CONTENT_PACK=str(pack))
        result = subprocess.run(
            [sys.executable, "-c", "from jomon.mechanical_compatibility import main_world_mechanical_fingerprint; print(main_world_mechanical_fingerprint())"],
            cwd=ROOT, env=environment, text=True, capture_output=True, check=True,
        )
        return result.stdout.strip()

    def _json(self, path: Path) -> dict:
        return json.loads(path.read_text(encoding="utf-8"))

    def _write(self, path: Path, document: dict, *, sort_keys: bool = False) -> None:
        path.write_text(json.dumps(document, ensure_ascii=True, indent=4, sort_keys=sort_keys) + "\n", encoding="utf-8")

    def test_projection_and_fingerprint_are_repeatable(self):
        first = main_world_mechanical_projection()
        second = main_world_mechanical_projection()
        self.assertEqual(first, second)
        self.assertEqual(canonical_mechanical_projection_bytes(), canonical_mechanical_projection_bytes())
        self.assertEqual(main_world_mechanical_fingerprint(), main_world_mechanical_fingerprint())
        self.assertEqual(first["projection_format"], MECHANICAL_PROJECTION_FORMAT)
        self.assertEqual(first["mechanical_compatibility_version"], MECHANICAL_COMPATIBILITY_VERSION)

    def test_presentation_pack_identity_and_legacy_catalog_copy_do_not_change_fingerprint(self):
        with tempfile.TemporaryDirectory() as directory:
            pack = self._pack(Path(directory), "fixture")
            baseline = self._fingerprint(pack)
            manifest = self._json(pack / "manifest.json")
            manifest["id"] = "same-mechanics-different-fiction"
            manifest["display_name"] = "A Different Fictional World"
            self._write(pack / "manifest.json", manifest)
            items = self._json(pack / "items.json")
            first = next(iter(items["items"]))
            items["items"][first]["display_name"] = "Fixture Display Name"
            self._write(pack / "items.json", items)
            equipment = self._json(pack.parent.parent / "data" / "equipment.json")
            equipment["item_specs"]["billhook"]["name"] = "Fixture Hook"
            equipment["item_specs"]["billhook"]["description"] = "Fixture description with identical mechanics."
            self._write(pack.parent.parent / "data" / "equipment.json", equipment)
            sanctums = self._json(pack.parent.parent / "data" / "sanctums.json")
            sanctums["sanctums"]["hearthford"]["boss"]["capability"] = "Fixture boss wording."
            self._write(pack.parent.parent / "data" / "sanctums.json", sanctums)
            self.assertEqual(baseline, self._fingerprint(pack))

    def test_numeric_relationship_and_ordered_mechanical_changes_change_fingerprint(self):
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            numeric = self._pack(base, "numeric")
            relationship = self._pack(base, "relationship")
            ordered = self._pack(base, "ordered")
            baseline = self._fingerprint(numeric)
            equipment = self._json(numeric.parent.parent / "data" / "equipment.json")
            equipment["item_specs"]["billhook"]["cut"] += 1
            self._write(numeric.parent.parent / "data" / "equipment.json", equipment)
            self.assertNotEqual(baseline, self._fingerprint(numeric))
            equipment = self._json(relationship.parent.parent / "data" / "equipment.json")
            equipment["weapon_ammunition"]["crossbow"] = "heavy bolts"
            self._write(relationship.parent.parent / "data" / "equipment.json", equipment)
            self.assertNotEqual(baseline, self._fingerprint(relationship))
            vessel = self._json(ordered.parent.parent / "data" / "vessel.json")
            vessel["route_edges"][:2] = reversed(vessel["route_edges"][:2])
            self._write(ordered.parent.parent / "data" / "vessel.json", vessel)
            self.assertNotEqual(baseline, self._fingerprint(ordered))

    def test_path_and_json_object_order_do_not_change_fingerprint(self):
        with tempfile.TemporaryDirectory() as directory:
            first = self._pack(Path(directory), "first")
            second = self._pack(Path(directory), "second")
            equipment = self._json(second.parent.parent / "data" / "equipment.json")
            self._write(second.parent.parent / "data" / "equipment.json", equipment, sort_keys=True)
            self.assertEqual(self._fingerprint(first), self._fingerprint(second))

    def test_unclassified_catalog_field_fails_loudly(self):
        with tempfile.TemporaryDirectory() as directory:
            pack = self._pack(Path(directory), "fixture")
            equipment = self._json(pack.parent.parent / "data" / "equipment.json")
            equipment["item_specs"]["billhook"]["future_mechanic"] = 1
            self._write(pack.parent.parent / "data" / "equipment.json", equipment)
            environment = dict(os.environ, JOMON_CONTENT_PACK=str(pack))
            result = subprocess.run(
                [sys.executable, "-c", "from jomon.mechanical_compatibility import main_world_mechanical_fingerprint; main_world_mechanical_fingerprint()"],
                cwd=ROOT, env=environment, text=True, capture_output=True,
            )
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("unclassified fields future_mechanic", result.stderr)


if __name__ == "__main__":
    unittest.main()
