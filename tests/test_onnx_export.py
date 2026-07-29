from __future__ import annotations

import importlib.util
import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.models.multi_action_policy import MaskedMultiActionPolicyHead, MultiActionPolicyConfig
from kenjaku.schema import CheckpointManifestV1
from kenjaku.training.behavior_distillation import (
    BehaviorDistillationTrainingResult,
    save_behavior_distillation_checkpoint,
)
from kenjaku.training.onnx_export import (
    MULTI_ACTION_ONNX_EXPORT_KIND,
    MULTI_ACTION_ONNX_LEGAL_MASK_INPUT,
    MULTI_ACTION_ONNX_LOGITS_OUTPUT,
    MULTI_ACTION_ONNX_OBSERVATION_INPUT,
    export_multi_action_checkpoint_to_onnx,
)

TORCH_AND_ONNX_AVAILABLE = all(
    importlib.util.find_spec(name) is not None for name in ("torch", "onnx")
)


@unittest.skipUnless(TORCH_AND_ONNX_AVAILABLE, "PyTorch and ONNX are not available")
class MultiActionOnnxExportTests(unittest.TestCase):
    def test_exports_validated_checkpoint_with_public_io_and_manifest_metadata(self) -> None:
        import onnx

        manifest = CheckpointManifestV1.for_current_schemas(
            checkpoint_id="onnx-fixture",
            model_kind="masked-multi-action-policy-head-v0",
            model_version="0.2.0",
            rulesets=("tenhou-4p", "tenhou-3p"),
        )
        config = MultiActionPolicyConfig(hidden_dim=8)
        result = BehaviorDistillationTrainingResult(
            model=MaskedMultiActionPolicyHead(config, seed=7),
            device="cpu",
            train_metrics={},
            eval_metrics={},
            history=[],
            optimizer_state={},
            completed_epochs=0,
            config=config,
            seed=7,
            checkpoint_manifest=manifest,
        )
        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "policy.pt"
            output = Path(directory) / "policy.onnx"
            save_behavior_distillation_checkpoint(result, checkpoint)

            exported = export_multi_action_checkpoint_to_onnx(checkpoint, output)

            self.assertEqual(exported.path, output)
            self.assertEqual(exported.checkpoint_manifest, manifest)
            self.assertEqual(exported.opset_version, 18)
            model = onnx.load_model(output)
            onnx.checker.check_model(model)
            self.assertEqual(
                {item.name for item in model.graph.input},
                {MULTI_ACTION_ONNX_OBSERVATION_INPUT, MULTI_ACTION_ONNX_LEGAL_MASK_INPUT},
            )
            self.assertEqual(
                {item.name for item in model.graph.output},
                {MULTI_ACTION_ONNX_LOGITS_OUTPUT},
            )
            self.assertTrue(
                all(
                    item.type.tensor_type.shape.dim[0].dim_param
                    for item in (*model.graph.input, *model.graph.output)
                )
            )
            metadata = {item.key: item.value for item in model.metadata_props}
            self.assertEqual(metadata["kenjaku.export_kind"], MULTI_ACTION_ONNX_EXPORT_KIND)
            self.assertEqual(
                json.loads(metadata["kenjaku.checkpoint_manifest"]), manifest.to_dict()
            )

    def test_rejects_non_onnx_output_path(self) -> None:
        with self.assertRaisesRegex(ValueError, "output_path"):
            export_multi_action_checkpoint_to_onnx("missing.pt", "policy.bin")


if __name__ == "__main__":
    unittest.main()
