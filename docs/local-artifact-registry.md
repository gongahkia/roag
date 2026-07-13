# Local artifact registry

`LocalArtifactRegistry` writes `kenjaku-local-artifact-registry-v1` under a chosen local root. Records store root-relative paths, SHA-256, byte size, and artifact type (`checkpoint`, `report`, or `onnx`).

Checkpoint and ONNX entries must carry a `CheckpointManifestV1`; reports must be `.json` and do not carry a checkpoint manifest. ONNX entries must be `.onnx`. The registry rejects traversal and files resolving outside its root, writes atomically, and verifies sizes and digests without network access.
