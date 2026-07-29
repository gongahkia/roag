from __future__ import annotations

import unittest

from kenjaku.schema import (
    OBSERVATION_V1_TENSOR_DIM,
    OBSERVATION_V1_TENSOR_LAYOUT,
    ObservationV1,
    observation_v1_tensor,
)
from kenjaku.simulation import draw_for_current_seat, initial_sandbox_environment


class ObservationV1TensorTests(unittest.TestCase):
    def test_encodes_deterministic_fixed_width_four_player_tensor(self) -> None:
        state = draw_for_current_seat(initial_sandbox_environment(seed="observation-tensor"))
        observation = ObservationV1.from_sandbox_state(state, seat=0)

        tensor = observation_v1_tensor(observation)

        self.assertEqual(tensor, observation_v1_tensor(observation))
        self.assertEqual(len(tensor), OBSERVATION_V1_TENSOR_DIM)
        self.assertEqual(
            OBSERVATION_V1_TENSOR_DIM,
            sum(width for _name, width in OBSERVATION_V1_TENSOR_LAYOUT),
        )
        self.assertTrue(all(isinstance(value, float) for value in tensor))
        self.assertEqual(sum(tensor[:34]), len(observation.hand) / 4.0)

    def test_actor_relative_sanma_slots_are_zero_padded(self) -> None:
        state = initial_sandbox_environment(ruleset="tenhou-3p", seed="observation-tensor-sanma")
        observation = ObservationV1.from_sandbox_state(state, seat=1)

        tensor = observation_v1_tensor(observation)

        discard_start = 68
        discard_rows = tensor[discard_start : discard_start + 136]
        self.assertEqual(discard_rows[102:136], (0.0,) * 34)
        kita_start = 340
        self.assertEqual(tensor[kita_start + 3], 0.0)


if __name__ == "__main__":
    unittest.main()
