# Multi-action ONNX export

`export_multi_action_checkpoint_to_onnx` loads a validated behavior-distillation checkpoint and writes a checked ONNX model. Its public graph contract is a float32 `observations` tensor, a boolean `legal_action_mask` tensor, and float32 `action_logits`; all use a dynamic batch axis and the schema-defined feature widths.

The export retains the checkpoint manifest as canonical JSON in `kenjaku.checkpoint_manifest` ONNX metadata. The exporter rejects unsupported checkpoint kinds and non-`.onnx` output paths, writes atomically, and checks the resulting ONNX graph before returning.
