from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.io import parse_tenhou_xml_file
from kenjaku.training import iter_discard_examples

try:
    import torch

    from kenjaku.models.torch_transformer import (
        DISCARD_TRANSFORMER_POLICY_KIND,
        MAHJONG_TRANSFORMER_ENCODER_KIND,
        TRANSFORMER_TILE_TYPES,
        TRANSFORMER_TOKEN_COUNT,
        DiscardTransformerPolicy,
        DiscardTransformerTensorDataset,
        MahjongStateTransformerEncoder,
        MahjongTransformerConfig,
        evaluate_discard_transformer,
        predict_discard_tiles,
        train_discard_transformer,
        transformer_state_payload,
        transformer_state_tensor,
    )

    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False


FIXTURE = Path("data/fixtures/tenhou/events_4p.xml")


@unittest.skipUnless(TORCH_AVAILABLE, "PyTorch is not available")
class TorchTransformerModelTests(unittest.TestCase):
    def test_state_tensor_has_fixed_token_contract(self) -> None:
        example = _discard_examples()[0]

        state = transformer_state_tensor(example)
        payload = transformer_state_payload(example)

        self.assertEqual(tuple(state.shape), (TRANSFORMER_TOKEN_COUNT,))
        self.assertEqual(payload["kind"], "kenjaku-transformer-state-v0")
        self.assertEqual(payload["token_count"], TRANSFORMER_TOKEN_COUNT)
        self.assertEqual(len(payload["tokens"]), TRANSFORMER_TOKEN_COUNT)
        self.assertEqual(payload["tokens"][0]["token_type"], "hand_count")
        self.assertEqual(payload["tokens"][34]["token_type"], "visible_count")
        self.assertEqual(payload["tokens"][68]["token_type"], "unseen_count")
        self.assertEqual(payload["tokens"][102]["token_type"], "dora_indicator_count")
        self.assertGreater(float(state.sum()), 0.0)

    def test_dataset_returns_transformer_state_mask_and_target(self) -> None:
        examples = _discard_examples()
        dataset = DiscardTransformerTensorDataset(examples)

        state, legal_mask, target = dataset[0]

        self.assertEqual(tuple(state.shape), (TRANSFORMER_TOKEN_COUNT,))
        self.assertEqual(tuple(legal_mask.shape), (TRANSFORMER_TILE_TYPES,))
        self.assertEqual(target.dtype, torch.long)
        self.assertTrue(legal_mask[int(target)])

    def test_encoder_forward_and_pool_shapes(self) -> None:
        config = MahjongTransformerConfig(
            model_dim=16,
            num_heads=4,
            num_layers=1,
            feedforward_dim=32,
            dropout=0.0,
        )
        encoder = MahjongStateTransformerEncoder(config)
        state = torch.stack([transformer_state_tensor(example) for example in _discard_examples()])

        encoded = encoder(state)
        pooled = encoder.pooled(state)

        self.assertEqual(encoder.kind, MAHJONG_TRANSFORMER_ENCODER_KIND)
        self.assertEqual(
            tuple(encoded.shape),
            (len(_discard_examples()), TRANSFORMER_TOKEN_COUNT, 16),
        )
        self.assertEqual(tuple(pooled.shape), (len(_discard_examples()), 16))

    def test_discard_policy_masks_illegal_tiles(self) -> None:
        config = MahjongTransformerConfig(
            model_dim=16,
            num_heads=4,
            num_layers=1,
            feedforward_dim=32,
            dropout=0.0,
        )
        policy = DiscardTransformerPolicy(config)
        state = transformer_state_tensor(_discard_examples()[0])
        legal_mask = torch.zeros(TRANSFORMER_TILE_TYPES, dtype=torch.bool)
        legal_mask[5] = True

        logits = policy(state, legal_mask)

        self.assertEqual(policy.kind, DISCARD_TRANSFORMER_POLICY_KIND)
        self.assertEqual(tuple(logits.shape), (1, TRANSFORMER_TILE_TYPES))
        self.assertLess(float(logits[0, 0].detach()), -1.0e8)
        self.assertGreater(float(logits[0, 5].detach()), -1.0e8)

    def test_train_evaluate_and_predict_transformer(self) -> None:
        config = MahjongTransformerConfig(
            model_dim=16,
            num_heads=4,
            num_layers=1,
            feedforward_dim=32,
            dropout=0.0,
        )
        examples = _discard_examples()

        result = train_discard_transformer(
            examples[:3],
            examples[3:],
            config=config,
            epochs=1,
            batch_size=2,
            learning_rate=0.001,
            device="cpu",
            seed=123,
        )
        metrics = evaluate_discard_transformer(
            result.model,
            examples[3:],
            batch_size=2,
            device="cpu",
        )
        predictions = predict_discard_tiles(
            result.model,
            examples[3:],
            batch_size=2,
            device="cpu",
        )

        self.assertEqual(result.model.kind, DISCARD_TRANSFORMER_POLICY_KIND)
        self.assertEqual(result.best_epoch, 1)
        self.assertEqual(result.selection_split, "eval")
        self.assertEqual(result.train_metrics["examples"], 3)
        self.assertEqual(metrics["examples"], 1)
        self.assertEqual(len(predictions), 1)


def _discard_examples():
    return list(iter_discard_examples(parse_tenhou_xml_file(FIXTURE)))


if __name__ == "__main__":
    unittest.main()
