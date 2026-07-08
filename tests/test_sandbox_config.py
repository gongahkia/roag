from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.simulation.config import load_sandbox_rule_config, tenhou_4p_default
from kenjaku.simulation.environment import initial_sandbox_environment


class SandboxRuleConfigTests(unittest.TestCase):
    def test_loads_custom_json_config(self) -> None:
        base = tenhou_4p_default().to_payload()
        base.update(
            {
                "initial_points": 27000,
                "return_points": 31000,
                "uma_by_rank": [30.0, 10.0, -10.0, -30.0],
                "riichi_deposit_points": 1500,
                "dead_wall_tiles": 14,
                "replacement_tiles": 0,
            }
        )
        base.pop("non_replacement_dead_wall_tiles")

        with TemporaryDirectory() as directory:
            path = Path(directory) / "sandbox-rules.json"
            path.write_text(json.dumps(base), encoding="utf-8")
            config = load_sandbox_rule_config(path)

        self.assertEqual(config.initial_points, 27000)
        self.assertEqual(config.return_points, 31000)
        self.assertEqual(config.uma_by_rank, (30.0, 10.0, -10.0, -30.0))
        self.assertEqual(config.riichi_deposit_points, 1500)
        self.assertEqual(config.non_replacement_dead_wall_tiles, 14)

        state = initial_sandbox_environment(seed="custom", rule_config=config)

        self.assertEqual(state.points, (27000, 27000, 27000, 27000))
        self.assertEqual(state.rule_config, config)


if __name__ == "__main__":
    unittest.main()
