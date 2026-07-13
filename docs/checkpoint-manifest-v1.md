# CheckpointManifestV1

`kenjaku-checkpoint-manifest-v1` identifies a checkpoint by `checkpoint_id`, `model_kind`, semantic `model_version`, supported rulesets, and an exact schema compatibility contract.

The contract records the observation/action/result kinds and legal-mask kind/dimension. A consumer supplies a `CheckpointRequirementV1`; compatibility requires its ruleset, optional model kind, every contract field, and a model version with the same major version that meets the minimum precedence.

`SemanticVersion` implements strict [Semantic Versioning 2.0.0](https://semver.org/), including prerelease precedence and build-metadata exclusion from precedence. The manifest intentionally does not record artifact paths or digests; local artifact registration is a separate concern.
