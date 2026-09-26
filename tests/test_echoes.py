import unittest

from jomon.echoes import ECHOES, apply_later_echoes, lines, validate_echoes
from jomon.state import create_world
from jomon.voyage_variants import VARIANTS


class VoyageEchoTests(unittest.TestCase):
    def test_every_variant_has_one_varied_later_echo(self):
        validate_echoes()
        self.assertEqual({row.id for row in VARIANTS.values()}, {row.variant_id for row in ECHOES})
        self.assertGreaterEqual(len({row.kind for row in ECHOES}), 8)

    def test_echo_waits_for_later_voyage_and_resolves_once(self):
        state = create_world("later-voyage-echo")
        echo = ECHOES[0]
        state.travel_count = 1
        state.vessel_changes["voyage_variant:1"] = echo.variant_id
        self.assertEqual(apply_later_echoes(state), [])
        state.travel_count = 2
        self.assertEqual(apply_later_echoes(state), [echo])
        self.assertEqual(apply_later_echoes(state), [])
        self.assertIn(echo.title, " ".join(lines(state)))

    def test_all_echoes_change_region_and_named_memory(self):
        state = create_world("all-voyage-echoes")
        state.travel_count = 20
        for index, echo in enumerate(ECHOES, 1):
            state.vessel_changes[f"voyage_variant:{index}"] = echo.variant_id
        applied = []
        for _ in ECHOES:
            resolved = apply_later_echoes(state)
            self.assertLessEqual(len(resolved), 1)
            applied.extend(resolved)
        self.assertEqual(len(applied), 12)
        self.assertEqual(sum(key.startswith("voyage-echo:") for key in state.region.changes), 12)
        self.assertTrue(any(person.memories for person in state.household))
        self.assertTrue(any(key.startswith("deck_scar:") for key in state.vessel_changes))

    def test_repeated_variant_creates_only_one_bounded_echo_key(self):
        state = create_world("bounded-voyage-echoes")
        echo = ECHOES[0]
        state.travel_count = 30
        for index in range(1, 25):
            state.vessel_changes[f"voyage_variant:{index}"] = echo.variant_id
        self.assertEqual(apply_later_echoes(state), [echo])
        self.assertEqual(sum(key.startswith("voyage_echo:") for key in state.vessel_changes), 1)


if __name__ == "__main__":
    unittest.main()
