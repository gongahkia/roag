from __future__ import annotations

import json
import gzip
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.policies import Policy, canonical_hash, execute_command


class OrderingTests(unittest.TestCase):
    def test_recorded_calibration_snapshot_migrates_without_rewriting_legacy_state(self) -> None:
        path = Path(__file__).resolve().parents[1] / "docs/evidence/pass3/0-bulkhead_basics-explorer.json.gz"
        with gzip.open(path, "rt") as stream:
            baseline = json.load(stream)
        original = json.loads(json.dumps(baseline["final"]))
        engine = GameEngine.from_snapshot(load_catalog(), baseline["final"])
        self.assertEqual(original, baseline["final"])
        current = json.loads(json.dumps(engine.snapshot()))
        # Pure migrations add durable fields, but must preserve all recorded
        # version-26 state and the exact continuation stream.
        for field in (
            "ledger", "pressure", "pressure_recent", "pressure_incomplete_before_tick",
            "encounter_pressure", "encounter_budget_version", "encounter_modules",
            "reinforcement_tickets", "reinforcement_reserve_id",
            "next_card_copy_id", "hub_loadouts", "doctrine_id", "recycler_credits",
            "ladder_rank", "expedition_mode", "active_modifiers", "enabled_packs",
            "base_victory", "base_victory_archived", "loop_depth",
            "archived_loop_depth", "score", "boss_sequence",
        ):
            del current["state"][field]
        for zone in ("deck", "hand", "draw_pile", "discard_pile"):
            for card in current["state"][zone]:
                for field in ("copy_id", "mastery", "infusion_id"):
                    del card[field]
        self.assertEqual(baseline["final"]["state"], current["state"])
        self.assertEqual(baseline["final"]["rng_state"], current["rng_state"])

    def test_reordered_json_definitions_keep_world_and_continuation_identical(self) -> None:
        catalog = load_catalog()
        raw = json.loads(json.dumps(catalog.raw))
        for key, value in raw.items():
            if isinstance(value, list):
                value.reverse()
        raw = dict(reversed(list(raw.items())))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "reordered.json"
            path.write_text(json.dumps(raw))
            reordered = load_catalog(path)
        self.assertEqual(catalog.manifest, reordered.manifest)
        engine = GameEngine.new(catalog, 42)
        other = GameEngine.new(reordered, 42)
        self.assertEqual(canonical_hash(engine), canonical_hash(other))
        policy = Policy()
        for _ in range(50):
            command = policy.next_command(engine)
            execute_command(engine, command)
            execute_command(other, command)
            self.assertEqual(canonical_hash(engine), canonical_hash(other))

    def test_python_hash_seed_does_not_change_world_commands_or_records(self) -> None:
        program = """
from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.policies import Policy, canonical_hash, execute_command
engine = GameEngine.new(load_catalog(), 42)
policy = Policy()
for _ in range(50):
    execute_command(engine, policy.next_command(engine))
print(canonical_hash(engine))
"""
        snapshots = []
        for seed in ("0", "1", "987654"):
            snapshots.append(subprocess.check_output([sys.executable, "-c", program],
                             env={**os.environ, "PYTHONHASHSEED": seed}, text=True, timeout=30).strip())
        self.assertEqual(1, len(set(snapshots)))


if __name__ == "__main__":
    unittest.main()
