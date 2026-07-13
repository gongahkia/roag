from __future__ import annotations

import json
import unittest
from dataclasses import replace

from kenjaku.schema import OBSERVATION_V1_KIND, MeldV1, ObservationV1
from kenjaku.simulation.environment import draw_for_current_seat, initial_sandbox_environment


class ObservationV1Tests(unittest.TestCase):
    def test_round_trips_a_deterministic_four_player_actor_view(self) -> None:
        state = draw_for_current_seat(initial_sandbox_environment(seed="observation-v1"))
        same_state = draw_for_current_seat(initial_sandbox_environment(seed="observation-v1"))

        observation = ObservationV1.from_sandbox_state(state, seat=0)
        same_observation = ObservationV1.from_sandbox_state(same_state, seat=0)
        payload = observation.to_dict()

        self.assertEqual(observation, same_observation)
        self.assertEqual(payload["kind"], OBSERVATION_V1_KIND)
        self.assertEqual(payload["hand"], [tile.notation for tile in state.hands[0]])
        self.assertEqual(payload["drawn_tile"], state.drawn_tile.notation)
        self.assertEqual(payload["hand_sizes"], state.hand_sizes())
        self.assertNotIn("hands", payload)
        self.assertNotIn("wall", payload)
        self.assertNotIn("ura_dora_indicators", payload)
        self.assertEqual(ObservationV1.from_dict(payload), observation)
        self.assertEqual(json.loads(observation.to_json()), payload)

    def test_hides_another_players_drawn_tile(self) -> None:
        state = draw_for_current_seat(initial_sandbox_environment(seed="observation-v1-private"))

        observation = ObservationV1.from_sandbox_state(state, seat=1)

        self.assertIsNone(observation.drawn_tile)
        with self.assertRaisesRegex(ValueError, "another seat"):
            replace(observation, drawn_tile=state.drawn_tile.notation)

    def test_sanma_accepts_kita_and_rejects_unavailable_man_tiles(self) -> None:
        state = initial_sandbox_environment(ruleset="tenhou-3p", seed="observation-v1-sanma")
        observation = ObservationV1.from_sandbox_state(state, seat=0)
        payload = observation.to_dict()
        payload["kita_tiles"][0] = ["N"]

        self.assertEqual(ObservationV1.from_dict(payload).kita_tiles[0], ("N",))
        payload["hand"][0] = "5m"
        with self.assertRaisesRegex(ValueError, "unavailable"):
            ObservationV1.from_dict(payload)

    def test_rejects_unknown_fields_and_invalid_melds(self) -> None:
        state = initial_sandbox_environment(seed="observation-v1-validation")
        payload = ObservationV1.from_sandbox_state(state, seat=0).to_dict()
        payload["future_field"] = True

        with self.assertRaisesRegex(ValueError, "unexpected=future_field"):
            ObservationV1.from_dict(payload)
        with self.assertRaisesRegex(ValueError, "must contain 3 tiles"):
            MeldV1(kind="chi", tiles=("1m", "2m"), called_tile="3m", from_seat=1)
        with self.assertRaisesRegex(ValueError, "must be an integer"):
            MeldV1(kind="pon", tiles=("1m", "1m", "1m"), called_tile="1m", from_seat="1")


if __name__ == "__main__":
    unittest.main()
