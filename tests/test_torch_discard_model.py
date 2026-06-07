from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.io import parse_tenhou_xml_file
from kenjaku.training import iter_discard_examples

try:
    import torch

    from kenjaku.models.torch_discard import (
        DISCARD_MLP_INPUT_DIM,
        DISCARD_MLP_OUTPUT_DIM,
        DiscardMlp,
        DiscardTensorDataset,
        predict_discard_tiles,
        train_discard_mlp,
    )

    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False


FIXTURE = Path("data/fixtures/tenhou/minimal_4p.xml")


@unittest.skipUnless(TORCH_AVAILABLE, "PyTorch is not available")
class TorchDiscardModelTests(unittest.TestCase):
    def test_dataset_returns_state_mask_and_target_tensors(self) -> None:
        examples = list(iter_discard_examples(parse_tenhou_xml_file(FIXTURE)))
        dataset = DiscardTensorDataset(examples)

        state, legal_mask, target = dataset[0]

        self.assertEqual(tuple(state.shape), (DISCARD_MLP_INPUT_DIM,))
        self.assertEqual(tuple(legal_mask.shape), (DISCARD_MLP_OUTPUT_DIM,))
        self.assertEqual(target.dtype, torch.long)
        self.assertTrue(legal_mask[int(target)])
        self.assertGreater(float(state.sum()), 0.0)

    def test_mlp_forward_masks_illegal_tiles(self) -> None:
        model = DiscardMlp(hidden_dim=8)
        state = torch.zeros((1, DISCARD_MLP_INPUT_DIM), dtype=torch.float32)
        legal_mask = torch.zeros((1, DISCARD_MLP_OUTPUT_DIM), dtype=torch.bool)
        legal_mask[0, 5] = True

        logits = model(state, legal_mask)

        self.assertEqual(tuple(logits.shape), (1, DISCARD_MLP_OUTPUT_DIM))
        self.assertLess(float(logits[0, 0].detach()), -1.0e8)
        self.assertGreater(float(logits[0, 5].detach()), -1.0e8)

    def test_tiny_training_run_completes_on_cpu(self) -> None:
        examples = list(iter_discard_examples(parse_tenhou_xml_file(FIXTURE)))

        result = train_discard_mlp(
            examples[:1],
            examples[1:],
            epochs=1,
            batch_size=1,
            learning_rate=0.001,
            hidden_dim=8,
            device="cpu",
            seed=123,
        )
        predictions = predict_discard_tiles(
            result.model,
            examples,
            batch_size=1,
            device=result.device,
        )

        self.assertEqual(result.device, "cpu")
        self.assertEqual(result.train_metrics["examples"], 1)
        self.assertEqual(result.eval_metrics["examples"], 1)
        self.assertEqual(len(predictions), len(examples))


if __name__ == "__main__":
    unittest.main()
