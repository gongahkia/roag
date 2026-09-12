from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.content import ContentError, load_catalog
from dumbest_dungeon.json_data import JsonDataError, loads


class StrictJsonTests(unittest.TestCase):
    def test_duplicate_keys_are_rejected_at_every_depth(self) -> None:
        for document in ('{"seed": 1, "seed": 2}', '{"effects": [{"amount": 3, "amount": 9}]}'):
            with self.subTest(document=document), self.assertRaisesRegex(JsonDataError, "duplicate JSON key"):
                loads(document)

    def test_nonfinite_constants_and_exponents_are_rejected(self) -> None:
        for number in ("NaN", "Infinity", "-Infinity", "1e9999", "-1e9999"):
            with self.subTest(number=number), self.assertRaisesRegex(JsonDataError, "non-finite"):
                loads('{"number": ' + number + '}')
        self.assertEqual({"number": 0.35}, loads('{"number": 0.35}'))

    def test_catalog_load_rejects_duplicate_keys_before_validation(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "content.json"
            path.write_text('{"schema_version": 20, "schema_version": 20}')
            with self.assertRaisesRegex(ContentError, "duplicate JSON key"):
                load_catalog(path)

    def test_catalog_rejects_unknown_fields_and_noninteger_card_amounts(self) -> None:
        raw = load_catalog().raw
        mutations = (
            (lambda r: r.update(typo=[]), "unknown fields"),
            (lambda r: r["cards"][0].update(owner="warden"), "unknown fields"),
            (lambda r: r["cards"][0]["effects"][0].update(script="print(1)"), "unknown fields"),
            (lambda r: r["cards"][0]["effects"][0].update(amount=True), "integer"),
            (lambda r: r["cards"][0]["effects"][0].update(amount=2.5), "integer"),
            (lambda r: r["cards"][0]["effects"][0].update(amount=-2), "non-negative"),
            (lambda r: r["cards"][0]["effects"][0].update(amount=1_000_001), "integer"),
            (lambda r: r["heroes"][0].update(preferred_ranks=[True, 2]), "outside"),
            (lambda r: r["cards"][0].update(id="not an id"), "stable id"),
            (lambda r: r["cards"][0].update(cost=True), "invalid cost"),
            (lambda r: r["enemies"][0].update(max_hp=True), "invalid max_hp"),
            (lambda r: r["enemies"][0]["actions"][0].update(weight=0), "action weight"),
            (lambda r: r["enemies"][0]["actions"][0].update(callback="anything"), "unknown fields"),
        )
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "content.json"
            for change, error in mutations:
                candidate = json.loads(json.dumps(raw))
                change(candidate)
                path.write_text(json.dumps(candidate))
                with self.subTest(error=error), self.assertRaisesRegex(ContentError, error):
                    load_catalog(path)

    def test_catalog_root_must_be_an_object(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "content.json"
            path.write_text("[]")
            with self.assertRaisesRegex(ContentError, "must be an object"):
                load_catalog(path)


if __name__ == "__main__":
    unittest.main()
