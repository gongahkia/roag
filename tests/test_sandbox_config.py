from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.simulation.config import (
    SANDBOX_RULE_CONFIG_V1_KIND,
    SandboxRuleConfig,
    load_sandbox_rule_config,
    tenhou_3p_default,
    tenhou_4p_default,
)
from kenjaku.simulation.environment import initial_sandbox_environment


class SandboxRuleConfigTests(unittest.TestCase):
    def test_versioned_payload_round_trips_both_tenhou_defaults(self) -> None:
        for config in (tenhou_4p_default(), tenhou_3p_default()):
            with self.subTest(ruleset=config.ruleset):
                payload = config.to_versioned_payload()

                self.assertEqual(payload["kind"], SANDBOX_RULE_CONFIG_V1_KIND)
                self.assertEqual(payload["ruleset"], config.ruleset)
                self.assertEqual(SandboxRuleConfig.from_versioned_payload(payload), config)
                self.assertEqual(json.loads(config.to_versioned_json()), payload)

    def test_versioned_payload_rejects_wrong_kind_missing_fields_and_rule_mismatch(self) -> None:
        payload = tenhou_4p_default().to_versioned_payload()
        payload["kind"] = "other"
        with self.assertRaisesRegex(ValueError, "kind must be"):
            SandboxRuleConfig.from_versioned_payload(payload)
        payload = tenhou_4p_default().to_versioned_payload()
        payload["config"].pop("initial_points")
        with self.assertRaisesRegex(ValueError, "missing=initial_points"):
            SandboxRuleConfig.from_versioned_payload(payload)
        payload = tenhou_4p_default().to_versioned_payload()
        payload["ruleset"] = "tenhou-3p"
        with self.assertRaisesRegex(ValueError, "must match config ruleset"):
            SandboxRuleConfig.from_versioned_payload(payload)

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
