from __future__ import annotations

import json
import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.schema import ACTION_V1_ACTIONS_BY_RULESET, ACTION_V1_KIND, ActionV1


class ActionV1Tests(unittest.TestCase):
    def test_round_trips_a_core_action_with_physical_consumed_tiles(self) -> None:
        action = Action(
            ActionKind.PON,
            TileType.parse("5p"),
            consumed=(Tile.parse("0p"), Tile.parse("5p")),
        )

        encoded = ActionV1.from_core(action, ruleset="tenhou-4p")
        payload = encoded.to_dict()

        self.assertEqual(payload["kind"], ACTION_V1_KIND)
        self.assertEqual(payload["consumed"], ["0p", "5p"])
        self.assertEqual(encoded.to_core(), action)
        self.assertEqual(ActionV1.from_dict(payload), encoded)
        self.assertEqual(json.loads(encoded.to_json()), payload)

    def test_ruleset_vocabularies_are_distinct_and_complete(self) -> None:
        self.assertNotIn("kita", ACTION_V1_ACTIONS_BY_RULESET["tenhou-4p"])
        self.assertNotIn("chi", ACTION_V1_ACTIONS_BY_RULESET["tenhou-3p"])
        self.assertIn("kyushu", ACTION_V1_ACTIONS_BY_RULESET["tenhou-4p"])
        self.assertIn("kyushu", ACTION_V1_ACTIONS_BY_RULESET["tenhou-3p"])

        with self.assertRaisesRegex(ValueError, "unavailable in tenhou-4p"):
            ActionV1(ruleset="tenhou-4p", action="kita", tile="N")
        with self.assertRaisesRegex(ValueError, "unavailable in tenhou-3p"):
            ActionV1(ruleset="tenhou-3p", action="chi", tile="1m")
        with self.assertRaisesRegex(ValueError, "unavailable in tenhou-3p"):
            ActionV1(ruleset="tenhou-3p", action="discard", tile="5m")

    def test_rejects_noncanonical_and_invalid_action_arguments(self) -> None:
        with self.assertRaisesRegex(ValueError, "canonical tile-type"):
            ActionV1(ruleset="tenhou-4p", action="discard", tile="0m")
        with self.assertRaisesRegex(ValueError, "only valid for discard"):
            ActionV1(ruleset="tenhou-4p", action="pass", tsumogiri=True)
        with self.assertRaisesRegex(ValueError, "immutable tuple"):
            ActionV1(ruleset="tenhou-4p", action="discard", tile="1m", consumed=[])
        with self.assertRaisesRegex(ValueError, "empty or contain 2"):
            ActionV1(
                ruleset="tenhou-4p",
                action="pon",
                tile="1m",
                consumed=("1m",),
            )

    def test_rejects_unknown_fields(self) -> None:
        payload = ActionV1(ruleset="tenhou-3p", action="kita", tile="N").to_dict()
        payload["extra"] = True

        with self.assertRaisesRegex(ValueError, "unexpected=extra"):
            ActionV1.from_dict(payload)


if __name__ == "__main__":
    unittest.main()
