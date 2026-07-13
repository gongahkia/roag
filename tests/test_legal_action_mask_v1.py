from __future__ import annotations

import json
import unittest

from kenjaku.schema import (
    LEGAL_ACTION_MASK_V1_DIM,
    LEGAL_ACTION_MASK_V1_KIND,
    LEGAL_ACTION_MASK_V1_KYUSHU_INDEX,
    LEGAL_ACTION_MASK_V1_PASS_INDEX,
    LEGAL_ACTION_MASK_V1_RIICHI_INDEX,
    LEGAL_ACTION_MASK_V1_TSUMO_INDEX,
    ActionV1,
    LegalActionMaskV1,
    action_v1_for_mask_index,
    action_v1_mask_index,
)
from kenjaku.training.ppo_schema import ppo_action_index, ppo_legal_action_mask


class LegalActionMaskV1Tests(unittest.TestCase):
    def test_serializes_deterministic_sanma_action_coordinates(self) -> None:
        actions = (
            ActionV1(ruleset="tenhou-3p", action="discard", tile="1m"),
            ActionV1(ruleset="tenhou-3p", action="kita", tile="N", consumed=("N",)),
            ActionV1(ruleset="tenhou-3p", action="pass"),
        )

        encoded = LegalActionMaskV1.from_actions(actions, ruleset="tenhou-3p")
        payload = encoded.to_dict()

        self.assertEqual(payload["kind"], LEGAL_ACTION_MASK_V1_KIND)
        self.assertEqual(len(payload["mask"]), LEGAL_ACTION_MASK_V1_DIM)
        self.assertEqual(
            encoded.actions(),
            (
                ActionV1(ruleset="tenhou-3p", action="discard", tile="1m"),
                ActionV1(ruleset="tenhou-3p", action="kita", tile="N"),
                ActionV1(ruleset="tenhou-3p", action="pass"),
            ),
        )
        self.assertEqual(LegalActionMaskV1.from_dict(payload), encoded)
        self.assertEqual(json.loads(encoded.to_json()), payload)

    def test_preserves_existing_ppo_coordinates(self) -> None:
        cases = (
            ({"kind": "discard", "tile": "5m"}, 4),
            ({"kind": "ron", "tile": "1m"}, 34),
            ({"kind": "pass"}, LEGAL_ACTION_MASK_V1_PASS_INDEX),
            ({"kind": "tsumo"}, LEGAL_ACTION_MASK_V1_TSUMO_INDEX),
            ({"kind": "riichi"}, LEGAL_ACTION_MASK_V1_RIICHI_INDEX),
            ({"kind": "kyushu"}, LEGAL_ACTION_MASK_V1_KYUSHU_INDEX),
        )

        for payload, expected in cases:
            with self.subTest(payload=payload):
                self.assertEqual(ppo_action_index(payload), expected)
        mask = ppo_legal_action_mask([{"kind": "pass"}, {"kind": "discard", "tile": "1m"}])
        self.assertTrue(mask[LEGAL_ACTION_MASK_V1_PASS_INDEX])
        self.assertTrue(mask[0])

    def test_rejects_ruleset_invalid_coordinates_and_empty_masks(self) -> None:
        invalid_sanma_mask = [False] * LEGAL_ACTION_MASK_V1_DIM
        invalid_sanma_mask[68] = True

        with self.assertRaisesRegex(ValueError, "unavailable tenhou-3p action"):
            LegalActionMaskV1(ruleset="tenhou-3p", mask=tuple(invalid_sanma_mask))
        with self.assertRaisesRegex(ValueError, "cannot be empty"):
            LegalActionMaskV1(ruleset="tenhou-4p", mask=(False,) * LEGAL_ACTION_MASK_V1_DIM)
        self.assertIsNone(action_v1_for_mask_index(238, ruleset="tenhou-4p"))
        self.assertEqual(
            action_v1_mask_index(ActionV1(ruleset="tenhou-4p", action="pass")),
            LEGAL_ACTION_MASK_V1_PASS_INDEX,
        )


if __name__ == "__main__":
    unittest.main()
