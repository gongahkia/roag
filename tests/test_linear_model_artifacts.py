from __future__ import annotations

import hashlib
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.models.linear_call import (
    CALL_DECISION_KINDS,
    CALL_LINEAR_V1_FEATURE_DIM,
    CALL_LINEAR_V1_FEATURE_PROFILE,
    CallLinearModel,
)
from kenjaku.models.linear_deal_in import DEAL_IN_LINEAR_FEATURE_DIM, DealInLinearModel
from kenjaku.models.linear_discard import (
    RAW_COUNT_FEATURE_DIM,
    RAW_COUNT_FEATURE_PROFILE,
    DiscardLinearModel,
)
from kenjaku.models.linear_riichi import (
    RIICHI_DECISION_KINDS,
    RIICHI_LINEAR_FEATURE_DIM,
    RiichiLinearModel,
)


class LinearModelArtifactSnapshotTests(unittest.TestCase):
    def test_static_artifact_snapshots_are_stable(self) -> None:
        snapshots = {
            "discard": (
                DiscardLinearModel(
                    weights=tuple(
                        tuple(0.01 * (row + col) for col in range(RAW_COUNT_FEATURE_DIM))
                        for row in range(34)
                    ),
                    epochs=2,
                    learning_rate=0.3,
                    feature_profile=RAW_COUNT_FEATURE_PROFILE,
                    l2=0.01,
                ),
                32201,
                "53e23970914d82dc6938e56b34899d5962b76e3520bbc31f9ea392094989b19a",
            ),
            "call": (
                CallLinearModel(
                    weights=tuple(
                        tuple(0.01 * (row + col) for col in range(CALL_LINEAR_V1_FEATURE_DIM))
                        for row, _ in enumerate(CALL_DECISION_KINDS)
                    ),
                    epochs=3,
                    learning_rate=0.2,
                    l2=0.02,
                    feature_profile=CALL_LINEAR_V1_FEATURE_PROFILE,
                    positive_class_weight=1.5,
                ),
                7541,
                "9a6bff677e85e3d64252945820f1015809f24aa981ce5da4237cf8863c3f31d1",
            ),
            "riichi": (
                RiichiLinearModel(
                    weights=tuple(
                        tuple(0.01 * (row + col) for col in range(RIICHI_LINEAR_FEATURE_DIM))
                        for row, _ in enumerate(RIICHI_DECISION_KINDS)
                    ),
                    epochs=4,
                    learning_rate=0.4,
                    l2=0.03,
                    positive_class_weight=2.0,
                ),
                2533,
                "c50ec2b15f94ff145f223974c7c5446239cfe700ad4b724b0c1fb7ac4750f811",
            ),
            "deal_in": (
                DealInLinearModel(
                    weights=tuple(0.01 * col for col in range(DEAL_IN_LINEAR_FEATURE_DIM)),
                    epochs=5,
                    learning_rate=0.5,
                    l2=0.04,
                    positive_class_weight=2.5,
                ),
                416,
                "6ca15b92aa45f9a0acba98ed6b2381725f0aad9b9205988cf7163476c0ecc0e2",
            ),
        }

        with TemporaryDirectory() as directory:
            for name, (model, size, digest) in snapshots.items():
                with self.subTest(model=name):
                    path = Path(directory) / f"{name}.json"
                    model.save(path)
                    artifact = path.read_bytes()

                    self.assertEqual(len(artifact), size)
                    self.assertEqual(hashlib.sha256(artifact).hexdigest(), digest)


if __name__ == "__main__":
    unittest.main()
