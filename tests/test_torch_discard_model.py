from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.io import parse_tenhou_xml_file
from kenjaku.training import iter_discard_examples

try:
    import torch

    from kenjaku.models.torch_discard import (
        DISCARD_MLP_CHECKPOINT_KIND,
        DISCARD_MLP_INPUT_DIM,
        DISCARD_MLP_OUTPUT_DIM,
        DiscardMlp,
        DiscardTensorDataset,
        predict_discard_tiles,
        save_discard_mlp_checkpoint,
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
        self.assertEqual([row["epoch"] for row in result.history], [1])
        self.assertEqual(result.selection_split, "eval")
        self.assertEqual(result.best_epoch, 1)
        self.assertEqual(result.best_metrics["eval"]["examples"], 1)
        self.assertEqual(len(predictions), len(examples))

    def test_zero_epoch_training_records_initial_metrics(self) -> None:
        examples = list(iter_discard_examples(parse_tenhou_xml_file(FIXTURE)))

        result = train_discard_mlp(
            examples[:2],
            [],
            epochs=0,
            batch_size=1,
            learning_rate=0.001,
            hidden_dim=8,
            device="cpu",
            seed=123,
        )

        self.assertEqual(result.device, "cpu")
        self.assertEqual([row["epoch"] for row in result.history], [0])
        self.assertEqual(result.selection_split, "train")
        self.assertEqual(result.best_epoch, 0)
        self.assertEqual(result.train_metrics["examples"], 2)
        self.assertEqual(result.eval_metrics["examples"], 0)
        self.assertEqual(result.best_metrics["train"]["examples"], 2)
        self.assertEqual(set(result.best_model_state), set(result.model.state_dict()))

    def test_training_rejects_oversized_adamw_state_and_expired_timeout(self) -> None:
        examples = list(iter_discard_examples(parse_tenhou_xml_file(FIXTURE)))

        with self.assertRaisesRegex(ValueError, "optimizer state exceeds"):
            train_discard_mlp(
                examples[:1],
                (),
                epochs=1,
                batch_size=1,
                learning_rate=0.001,
                hidden_dim=8,
                device="cpu",
                max_optimizer_state_bytes=1,
            )
        with self.assertRaisesRegex(TimeoutError, "timeout"):
            train_discard_mlp(
                examples[:1],
                (),
                epochs=0,
                batch_size=1,
                learning_rate=0.001,
                hidden_dim=8,
                device="cpu",
                timeout_seconds=1.0e-12,
            )

    def test_saves_best_checkpoint_payload(self) -> None:
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

        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "discard-mlp.pt"
            save_discard_mlp_checkpoint(
                result,
                checkpoint,
                epochs=1,
                batch_size=1,
                learning_rate=0.001,
                eval_fraction=0.5,
                split_seed="fixed",
                seed=123,
            )
            payload = torch.load(checkpoint, map_location="cpu")

        self.assertEqual(payload["kind"], DISCARD_MLP_CHECKPOINT_KIND)
        self.assertEqual(payload["model"]["kind"], "discard-mlp-v0")
        self.assertEqual(payload["model"]["hidden_dim"], 8)
        self.assertEqual(payload["training"]["best_epoch"], result.best_epoch)
        self.assertEqual(payload["training"]["selection_split"], result.selection_split)
        self.assertEqual(payload["metrics"]["best"], result.best_metrics)
        self.assertEqual(set(payload["model_state_dict"]), set(result.best_model_state))


if __name__ == "__main__":
    unittest.main()
