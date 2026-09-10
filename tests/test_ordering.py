from __future__ import annotations

import json
import gzip
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.content import load_catalog, load_legacy_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.policies import Command, Policy, canonical_hash, execute_command


class OrderingTests(unittest.TestCase):
    def test_recorded_calibration_commands_preserve_original_gameplay_and_rng(self) -> None:
        path = Path(__file__).resolve().parents[1] / "docs/evidence/pass3/0-bulkhead_basics-explorer.json.gz"
        with gzip.open(path, "rt") as stream:
            baseline = json.load(stream)
        engine = GameEngine.new(load_legacy_catalog(), baseline["seed"], start_in_hub=True)
        engine.select_curated_squad(baseline["squad"])
        engine.begin_expedition()
        for row in baseline["commands"]:
            command = row["command"]
            execute_command(engine, Command(command["method"], tuple(command["args"])))
        current = json.loads(json.dumps(engine.snapshot()))
        # Schema additions are checked by migration tests. This recorded command
        # transcript proves that adding durable observations does not alter the
        # pre-existing simulation or consume RNG.
        for field in (
            "ledger", "pressure", "pressure_recent", "pressure_incomplete_before_tick",
            "encounter_pressure", "encounter_modules", "reinforcement_tickets", "reinforcement_reserve_id",
            "next_card_copy_id", "hub_loadouts", "doctrine_id",
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
