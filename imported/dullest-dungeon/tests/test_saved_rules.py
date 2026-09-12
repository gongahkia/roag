import hashlib
import json
import unittest
from dataclasses import replace

from dumbest_dungeon.content import load_catalog, load_rules
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.manifest import canonical_bytes


class SavedRulesTests(unittest.TestCase):
    def variant(self):
        catalog = load_catalog()
        cards = json.loads(json.dumps(catalog.cards))
        cards["brace"]["name"] = "Recorded Brace Variant"
        return replace(catalog, cards=cards)

    def test_valid_saved_revision_survives_a_different_installed_catalog(self) -> None:
        original = GameEngine.new(self.variant(), 42)
        original.start_combat("lost_shift")
        restored = GameEngine.from_snapshot(load_catalog(), original.snapshot())
        self.assertEqual(original.snapshot(), restored.snapshot())
        self.assertEqual("Recorded Brace Variant", restored.catalog.cards["brace"]["name"])
        self.assertIs(load_rules(original.catalog.rules), load_rules(original.catalog.rules))

    def test_unknown_opcode_is_rejected_even_with_a_recomputed_fingerprint(self) -> None:
        original = GameEngine.new(load_catalog(), 42)
        broken = json.loads(json.dumps(original.snapshot()))
        broken["content_rules"]["cards"]["brace"]["effects"][0]["op"] = "evaluate_python"
        broken["content_manifest"]["fingerprint"] = hashlib.sha256(canonical_bytes(broken["content_rules"])).hexdigest()
        with self.assertRaisesRegex(RuleError, "unknown.*effect|unknown.*op"):
            GameEngine.from_snapshot(original.catalog, broken)

    def test_missing_tampered_and_unavailable_rules_do_not_regenerate(self) -> None:
        original = GameEngine.new(self.variant(), 42)
        for action in ("missing", "tampered", "unavailable", "nonfinite"):
            broken = json.loads(json.dumps(original.snapshot()))
            if action == "missing":
                del broken["content_rules"]
            elif action == "tampered":
                broken["content_rules"]["balance"]["energy"] = 4
            elif action == "nonfinite":
                broken["content_rules"]["balance"]["energy"] = float("inf")
            else:
                broken["content_rules"] = None
            with self.assertRaises(RuleError):
                GameEngine.from_snapshot(load_catalog(), broken)

    def test_saved_definition_enumeration_is_not_a_rule(self) -> None:
        original = GameEngine.new(self.variant(), 42)
        raw = original.snapshot()
        raw["content_rules"] = {key: dict(reversed(list(value.items()))) if isinstance(value, dict) else value
                                for key, value in reversed(list(raw["content_rules"].items()))}
        restored = GameEngine.from_snapshot(load_catalog(), raw)
        self.assertEqual(original.snapshot(), restored.snapshot())
