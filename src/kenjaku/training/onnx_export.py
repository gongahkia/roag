"""ONNX export for versioned multi-action behavior-distillation checkpoints."""

from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from kenjaku.models.multi_action_policy import (
    MULTI_ACTION_POLICY_HEAD_KIND,
    MaskedMultiActionPolicyHead,
)
from kenjaku.models.torch_discard import require_torch_modules
from kenjaku.schema import CheckpointManifestV1
from kenjaku.training.behavior_distillation import (
    _config_from_payload,
    load_behavior_distillation_checkpoint,
)

MULTI_ACTION_ONNX_EXPORT_KIND = "kenjaku-multi-action-onnx-export-v1"
MULTI_ACTION_ONNX_OPSET_VERSION = 18
MULTI_ACTION_ONNX_OBSERVATION_INPUT = "observations"
MULTI_ACTION_ONNX_LEGAL_MASK_INPUT = "legal_action_mask"
MULTI_ACTION_ONNX_LOGITS_OUTPUT = "action_logits"


@dataclass(frozen=True, slots=True)
class MultiActionOnnxExport:
    """Verified local ONNX export metadata for one multi-action checkpoint."""

    path: Path
    checkpoint_manifest: CheckpointManifestV1
    input_dim: int
    action_dim: int
    opset_version: int


def export_multi_action_checkpoint_to_onnx(
    checkpoint_path: str | Path,
    output_path: str | Path,
    *,
    opset_version: int = MULTI_ACTION_ONNX_OPSET_VERSION,
) -> MultiActionOnnxExport:
    """Export one validated multi-action checkpoint with boolean legal-mask input."""
    if not isinstance(opset_version, int) or isinstance(opset_version, bool) or opset_version <= 0:
        raise ValueError("opset_version must be a positive integer")
    target = Path(output_path)
    if target.suffix.lower() != ".onnx":
        raise ValueError("output_path must use a .onnx suffix")
    onnx = _require_onnx()
    torch, nn, _functional, _data_loader, _dataset_base = require_torch_modules()
    checkpoint = load_behavior_distillation_checkpoint(checkpoint_path, device="cpu")
    model_payload = checkpoint["model"]
    if model_payload.get("kind") != MULTI_ACTION_POLICY_HEAD_KIND:
        raise ValueError("checkpoint model kind is unsupported")
    config = _config_from_payload(model_payload.get("config"))
    model = MaskedMultiActionPolicyHead(config).to("cpu")
    model.load_state_dict(checkpoint["model_state_dict"])
    model.eval()

    class ActionLogitsExport(nn.Module):
        def __init__(self, policy: Any) -> None:
            super().__init__()
            self.policy = policy

        def forward(self, observations: Any, legal_action_mask: Any) -> Any:
            logits = self.policy.net(observations)
            return logits.masked_fill(~legal_action_mask, float("-inf"))

    wrapper = ActionLogitsExport(model).eval()
    observations = torch.zeros((1, config.input_dim), dtype=torch.float32)
    legal_action_mask = torch.ones((1, config.action_dim), dtype=torch.bool)
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_name(f".{target.stem}.tmp.onnx")
    try:
        batch_dimension = torch.export.Dim("batch", min=1)
        torch.onnx.export(
            wrapper,
            (observations, legal_action_mask),
            temporary,
            export_params=True,
            input_names=(
                MULTI_ACTION_ONNX_OBSERVATION_INPUT,
                MULTI_ACTION_ONNX_LEGAL_MASK_INPUT,
            ),
            output_names=(MULTI_ACTION_ONNX_LOGITS_OUTPUT,),
            dynamic_shapes={
                MULTI_ACTION_ONNX_OBSERVATION_INPUT: {0: batch_dimension},
                MULTI_ACTION_ONNX_LEGAL_MASK_INPUT: {0: batch_dimension},
            },
            opset_version=opset_version,
            dynamo=True,
        )
        onnx_model = onnx.load_model(temporary)
        onnx.checker.check_model(onnx_model)
        _validate_graph_io(
            onnx,
            onnx_model,
            input_dim=config.input_dim,
            action_dim=config.action_dim,
        )
        onnx.helper.set_model_props(
            onnx_model,
            {
                "kenjaku.export_kind": MULTI_ACTION_ONNX_EXPORT_KIND,
                "kenjaku.checkpoint_manifest": checkpoint["checkpoint_manifest"].to_json(),
                "kenjaku.model_kind": MULTI_ACTION_POLICY_HEAD_KIND,
            },
        )
        onnx.checker.check_model(onnx_model)
        onnx.save_model(onnx_model, temporary)
        os.replace(temporary, target)
    except Exception:
        temporary.unlink(missing_ok=True)
        raise
    return MultiActionOnnxExport(
        path=target,
        checkpoint_manifest=checkpoint["checkpoint_manifest"],
        input_dim=config.input_dim,
        action_dim=config.action_dim,
        opset_version=opset_version,
    )


def _require_onnx() -> Any:
    try:
        import onnx
    except ImportError as error:
        raise RuntimeError("multi-action ONNX export requires the ml extra with onnx") from error
    return onnx


def _validate_graph_io(onnx: Any, model: Any, *, input_dim: int, action_dim: int) -> None:
    inputs = {value.name: value for value in model.graph.input}
    outputs = {value.name: value for value in model.graph.output}
    if set(inputs) != {MULTI_ACTION_ONNX_OBSERVATION_INPUT, MULTI_ACTION_ONNX_LEGAL_MASK_INPUT}:
        raise ValueError("exported ONNX inputs differ from the multi-action contract")
    if set(outputs) != {MULTI_ACTION_ONNX_LOGITS_OUTPUT}:
        raise ValueError("exported ONNX outputs differ from the multi-action contract")
    _validate_tensor_value(
        onnx,
        inputs[MULTI_ACTION_ONNX_OBSERVATION_INPUT],
        element_type=onnx.TensorProto.FLOAT,
        width=input_dim,
    )
    _validate_tensor_value(
        onnx,
        inputs[MULTI_ACTION_ONNX_LEGAL_MASK_INPUT],
        element_type=onnx.TensorProto.BOOL,
        width=action_dim,
    )
    _validate_tensor_value(
        onnx,
        outputs[MULTI_ACTION_ONNX_LOGITS_OUTPUT],
        element_type=onnx.TensorProto.FLOAT,
        width=action_dim,
    )


def _validate_tensor_value(onnx: Any, value: Any, *, element_type: int, width: int) -> None:
    tensor = value.type.tensor_type
    if tensor.elem_type != element_type or len(tensor.shape.dim) != 2:
        raise ValueError("exported ONNX tensor type differs from the multi-action contract")
    if not tensor.shape.dim[0].dim_param or tensor.shape.dim[1].dim_value != width:
        raise ValueError("exported ONNX tensor width differs from the multi-action contract")
