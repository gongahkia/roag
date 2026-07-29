from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory
from typing import Any, cast

from kenjaku.models.multi_action_policy import MaskedMultiActionPolicyHead, MultiActionPolicyConfig
from kenjaku.schema import ActionV1, CheckpointManifestV1, action_v1_mask_index
from kenjaku.training.behavior_distillation import (
    BehaviorDistillationTrainingResult,
    save_behavior_distillation_checkpoint,
)
from kenjaku.training.onnx_export import (
    MULTI_ACTION_ONNX_LEGAL_MASK_INPUT,
    MULTI_ACTION_ONNX_LOGITS_OUTPUT,
    MULTI_ACTION_ONNX_OBSERVATION_INPUT,
    export_multi_action_checkpoint_to_onnx,
)

TORCH_ONNX_RUNTIME_AVAILABLE = all(
    importlib.util.find_spec(name) is not None for name in ("torch", "onnx", "onnxruntime")
)


@unittest.skipUnless(
    TORCH_ONNX_RUNTIME_AVAILABLE,
    "PyTorch, ONNX, and ONNX Runtime are unavailable",
)
class MultiActionOnnxParityTests(unittest.TestCase):
    def test_matches_pytorch_logits_and_preserves_four_player_and_sanma_masks(self) -> None:
        import numpy
        import onnxruntime
        import torch

        manifest = CheckpointManifestV1.for_current_schemas(
            checkpoint_id="onnx-parity",
            model_kind="masked-multi-action-policy-head-v0",
            model_version="0.2.0",
            rulesets=("tenhou-4p", "tenhou-3p"),
        )
        config = MultiActionPolicyConfig(hidden_dim=8)
        model = MaskedMultiActionPolicyHead(config, seed=13).eval()
        four_player_mask = _legal_mask(
            "tenhou-4p",
            (
                ("discard", "1m"),
                ("chi", "3m"),
                ("pass", None),
            ),
            torch=torch,
        )
        sanma_mask = _legal_mask(
            "tenhou-3p",
            (
                ("discard", "1p"),
                ("kita", "N"),
                ("pass", None),
            ),
            torch=torch,
        )
        observations = torch.arange(2 * config.input_dim, dtype=torch.float32).reshape(
            2, config.input_dim
        ) / 1000
        legal_action_mask = torch.stack((four_player_mask, sanma_mask))
        result = BehaviorDistillationTrainingResult(
            model=model,
            device="cpu",
            train_metrics={},
            eval_metrics={},
            history=[],
            optimizer_state={},
            completed_epochs=0,
            config=config,
            seed=13,
            checkpoint_manifest=manifest,
        )
        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "policy.pt"
            onnx_path = Path(directory) / "policy.onnx"
            save_behavior_distillation_checkpoint(result, checkpoint)
            export_multi_action_checkpoint_to_onnx(checkpoint, onnx_path)
            session = onnxruntime.InferenceSession(
                str(onnx_path), providers=["CPUExecutionProvider"]
            )
            actual = cast(
                Any,
                session.run(
                    [MULTI_ACTION_ONNX_LOGITS_OUTPUT],
                    {
                        MULTI_ACTION_ONNX_OBSERVATION_INPUT: observations.numpy(),
                        MULTI_ACTION_ONNX_LEGAL_MASK_INPUT: legal_action_mask.numpy(),
                    },
                )[0],
            )
        with torch.no_grad():
            expected = model(observations, legal_action_mask).numpy()

        self.assertEqual(actual.shape, (2, config.action_dim))
        numpy.testing.assert_array_equal(numpy.isneginf(actual), ~legal_action_mask.numpy())
        numpy.testing.assert_allclose(
            actual[legal_action_mask.numpy()],
            expected[legal_action_mask.numpy()],
            rtol=1e-5,
            atol=1e-6,
        )


def _legal_mask(
    ruleset: str,
    actions: tuple[tuple[str, str | None], ...],
    *,
    torch: Any,
) -> Any:
    mask = torch.zeros(276, dtype=torch.bool)
    for action, tile in actions:
        mask[action_v1_mask_index(ActionV1(ruleset=ruleset, action=action, tile=tile))] = True
    return mask


if __name__ == "__main__":
    unittest.main()
