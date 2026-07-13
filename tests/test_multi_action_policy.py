from __future__ import annotations

import importlib.util
import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.models.multi_action_policy import (
    MELD_SELECTION_DIM,
    MULTI_ACTION_POLICY_ACTION_DIM,
    MULTI_ACTION_POLICY_INPUT_DIM,
    ActionSpecificArgumentHeads,
    MaskedMultiActionPolicyHead,
    MultiActionPolicyConfig,
    meld_selection_index,
    meld_selection_mask,
)
from kenjaku.schema import LEGAL_ACTION_MASK_V1_DIM, OBSERVATION_V1_TENSOR_DIM

TORCH_AVAILABLE = importlib.util.find_spec("torch") is not None

if TORCH_AVAILABLE:
    import torch


class MultiActionPolicyConfigTests(unittest.TestCase):
    def test_uses_versioned_observation_and_action_dimensions(self) -> None:
        config = MultiActionPolicyConfig()

        self.assertEqual(config.input_dim, OBSERVATION_V1_TENSOR_DIM)
        self.assertEqual(config.action_dim, LEGAL_ACTION_MASK_V1_DIM)
        self.assertEqual(MULTI_ACTION_POLICY_INPUT_DIM, OBSERVATION_V1_TENSOR_DIM)
        self.assertEqual(MULTI_ACTION_POLICY_ACTION_DIM, LEGAL_ACTION_MASK_V1_DIM)

    def test_rejects_schema_dimension_changes_and_empty_hidden_layer(self) -> None:
        with self.assertRaisesRegex(ValueError, "input_dim"):
            MultiActionPolicyConfig(input_dim=1)
        with self.assertRaisesRegex(ValueError, "hidden_dim"):
            MultiActionPolicyConfig(hidden_dim=0)
        with self.assertRaisesRegex(ValueError, "action_dim"):
            MultiActionPolicyConfig(action_dim=1)


class MeldSelectionTests(unittest.TestCase):
    def test_encodes_chi_compositions_separately_from_same_type_melds(self) -> None:
        chi_123 = Action(
            ActionKind.CHI,
            TileType.parse("3m"),
            consumed=(Tile.parse("1m"), Tile.parse("2m")),
        )
        chi_234 = Action(
            ActionKind.CHI,
            TileType.parse("3m"),
            consumed=(Tile.parse("2m"), Tile.parse("4m")),
        )
        pon = Action(ActionKind.PON, TileType.parse("3m"))

        chi_123_index = meld_selection_index(chi_123)
        chi_234_index = meld_selection_index(chi_234)
        mask = meld_selection_mask((chi_234, pon))

        self.assertEqual(MELD_SELECTION_DIM, 157)
        self.assertNotEqual(chi_123_index, chi_234_index)
        self.assertFalse(mask[chi_123_index])
        self.assertTrue(mask[chi_234_index])
        self.assertTrue(mask[meld_selection_index(pon)])

    def test_rejects_non_meld_actions_and_underspecified_chi(self) -> None:
        with self.assertRaisesRegex(ValueError, "no meld argument"):
            meld_selection_index(Action.discard("1m"))
        with self.assertRaisesRegex(ValueError, "two consumed"):
            meld_selection_index(Action(ActionKind.CHI, TileType.parse("3m")))
        with self.assertRaisesRegex(ValueError, "cannot be empty"):
            meld_selection_mask(())


@unittest.skipUnless(TORCH_AVAILABLE, "PyTorch is not available")
class MaskedMultiActionPolicyHeadTests(unittest.TestCase):
    def test_masks_illegal_coordinates_and_normalizes_legal_distribution(self) -> None:
        model = MaskedMultiActionPolicyHead(MultiActionPolicyConfig(hidden_dim=8), seed=7)
        observations = torch.zeros((2, MULTI_ACTION_POLICY_INPUT_DIM))
        legal_masks = torch.zeros((2, MULTI_ACTION_POLICY_ACTION_DIM), dtype=torch.bool)
        legal_masks[0, [0, 272]] = True
        legal_masks[1, [1, 273]] = True

        logits = model(observations, legal_masks)
        probabilities = model.probabilities(observations, legal_masks)

        self.assertEqual(tuple(logits.shape), (2, MULTI_ACTION_POLICY_ACTION_DIM))
        self.assertTrue(torch.isneginf(logits[0, 1]))
        self.assertTrue(torch.isneginf(logits[1, 0]))
        self.assertTrue(torch.isfinite(logits[0, 0]))
        self.assertEqual(float(probabilities[0, 1]), 0.0)
        self.assertEqual(float(probabilities[1, 0]), 0.0)
        self.assertTrue(torch.allclose(probabilities.sum(dim=1), torch.ones(2)))

    def test_accepts_single_observation_and_seeded_initialization_is_stable(self) -> None:
        config = MultiActionPolicyConfig(hidden_dim=8)
        first = MaskedMultiActionPolicyHead(config, seed=19)
        second = MaskedMultiActionPolicyHead(config, seed=19)
        observation = torch.zeros(MULTI_ACTION_POLICY_INPUT_DIM)
        legal_mask = torch.zeros(MULTI_ACTION_POLICY_ACTION_DIM, dtype=torch.bool)
        legal_mask[[0, 272]] = True

        self.assertEqual(
            tuple(first(observation, legal_mask).shape),
            (MULTI_ACTION_POLICY_ACTION_DIM,),
        )
        self.assertTrue(
            torch.equal(first(observation, legal_mask), second(observation, legal_mask))
        )

    def test_rejects_bad_or_empty_masks(self) -> None:
        model = MaskedMultiActionPolicyHead(MultiActionPolicyConfig(hidden_dim=8), seed=0)
        observations = torch.zeros((2, MULTI_ACTION_POLICY_INPUT_DIM))
        legal_masks = torch.zeros((2, MULTI_ACTION_POLICY_ACTION_DIM), dtype=torch.bool)
        legal_masks[0, 0] = True

        with self.assertRaisesRegex(ValueError, "at least one action"):
            model(observations, legal_masks)
        with self.assertRaisesRegex(ValueError, "boolean"):
            model(observations[:1], legal_masks[:1].to(torch.float32))
        with self.assertRaisesRegex(ValueError, "shape"):
            model(observations[:1], legal_masks[:1, :-1])

    def test_exposes_action_specific_tile_and_meld_heads(self) -> None:
        model = MaskedMultiActionPolicyHead(MultiActionPolicyConfig(hidden_dim=8), seed=3)
        observations = torch.zeros((2, MULTI_ACTION_POLICY_INPUT_DIM))
        tile_masks = torch.zeros((2, 34), dtype=torch.bool)
        tile_masks[:, [4, 13]] = True
        pon = Action(ActionKind.PON, TileType.parse("5p"))
        meld_mask = torch.tensor(meld_selection_mask((pon,)), dtype=torch.bool).repeat(2, 1)

        tile_logits = model.tile_logits(observations, ActionKind.DISCARD, tile_masks)
        meld_logits = model.meld_logits(observations, meld_mask)

        self.assertIsInstance(model.argument_heads, ActionSpecificArgumentHeads)
        self.assertEqual(tuple(tile_logits.shape), (2, 34))
        self.assertEqual(tuple(meld_logits.shape), (2, MELD_SELECTION_DIM))
        self.assertTrue(torch.isneginf(tile_logits[0, 0]))
        self.assertTrue(torch.isneginf(meld_logits[0, 0]))
        self.assertTrue(torch.isfinite(meld_logits[0, meld_selection_index(pon)]))
        with self.assertRaisesRegex(ValueError, "no tile argument"):
            model.tile_logits(observations[:1], ActionKind.PASS, tile_masks[:1])


if __name__ == "__main__":
    unittest.main()
