from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.models import (
    PLACEMENT_CHECKPOINT_KIND,
    PLACEMENT_DISCLAIMER,
    PLACEMENT_MODEL_KIND,
    PlacementModel,
    parse_kyoku,
    parse_scores,
    placement_examples_from_paths,
)


class PlacementModelTests(unittest.TestCase):
    def test_fixture_training_examples_fit_save_and_load(self) -> None:
        examples = placement_examples_from_paths([Path("data/fixtures/tenhou")])
        model = PlacementModel.fit(examples, epochs=3, learning_rate=0.05)
        probabilities = model.predict_probabilities(
            scores=(25000, 25000, 25000, 25000),
            round_wind=0,
            kyoku=1,
            seat=0,
        )

        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "placement.json"
            model.save(checkpoint, metadata={"examples": len(examples)})
            payload = json.loads(checkpoint.read_text(encoding="utf-8"))
            loaded = PlacementModel.load(checkpoint)
            loaded_probabilities = loaded.predict_probabilities(
                scores=(25000, 25000, 25000, 25000),
                round_wind=0,
                kyoku=1,
                seat=0,
            )

        self.assertEqual(len(examples), 12)
        self.assertEqual(model.kind, PLACEMENT_MODEL_KIND)
        self.assertEqual(payload["kind"], PLACEMENT_CHECKPOINT_KIND)
        self.assertEqual(payload["model"]["kind"], PLACEMENT_MODEL_KIND)
        self.assertEqual(payload["disclaimer"], PLACEMENT_DISCLAIMER)
        self.assertEqual(len(probabilities), 4)
        self.assertAlmostEqual(sum(probabilities), 1.0)
        self.assertEqual(loaded_probabilities, probabilities)

    def test_parse_scores_and_kyoku(self) -> None:
        self.assertEqual(parse_scores("25000,24000,26000,25000"), (25000, 24000, 26000, 25000))
        self.assertEqual(parse_kyoku("E1"), (0, 1))
        self.assertEqual(parse_kyoku("South-4"), (1, 4))


if __name__ == "__main__":
    unittest.main()
