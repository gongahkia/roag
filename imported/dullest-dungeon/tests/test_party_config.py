from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError


class PartyConfigurationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def configured_hub(self) -> GameEngine:
        engine = GameEngine.new(self.catalog, 2601, start_in_hub=True)
        engine.select_curated_squad("bulkhead_basics")
        return engine

    def test_advanced_loadout_replaces_only_its_owners_starting_cards(self) -> None:
        engine = self.configured_hub()
        engine.select_hub_loadout("warden", "base:warden_field")
        expected = list(self.catalog.loadouts["base:warden_field"]["cards"])
        engine.begin_expedition()
        actual = [card.card_id for card in engine.state.deck
                  if self.catalog.cards[card.card_id]["hero"] == "warden"]
        self.assertEqual(expected, actual)
        self.assertTrue(all(not card.upgraded and card.infusion_id is None
                            for card in engine.state.deck))

    def test_loadout_and_doctrine_round_trip_and_departure_history(self) -> None:
        engine = self.configured_hub()
        engine.select_hub_loadout("warden", "base:warden_field")
        engine.select_doctrine("base:guard_rotation")
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual(engine.snapshot(), loaded.snapshot())
        loaded.begin_expedition()
        departure = next(record for record in loaded.state.ledger.records
                         if record.kind == "departure")
        self.assertEqual("base:guard_rotation", departure.data["doctrine"])
        self.assertEqual({"warden": "base:warden_field"}, departure.data["loadouts"])

    def test_configuration_rejects_wrong_owner_and_incompatible_doctrine(self) -> None:
        engine = self.configured_hub()
        with self.assertRaisesRegex(RuleError, "unavailable"):
            engine.select_hub_loadout("warden", "base:scout_field")
        with self.assertRaisesRegex(RuleError, "does not satisfy"):
            engine.select_doctrine("base:wound_culture")
        engine.select_doctrine("base:guard_rotation")
        engine.select_curated_squad("breach_protocol")
        self.assertEqual({}, engine.state.hub_loadouts)
        self.assertIsNone(engine.state.doctrine_id)

    def test_loadout_change_drops_a_now_incompatible_doctrine(self) -> None:
        engine = self.configured_hub()
        engine.select_doctrine("base:mark_window")
        engine.select_hub_loadout("scout", "base:scout_field")
        self.assertFalse(engine.doctrine_compatible("base:mark_window"))
        self.assertIsNone(engine.state.doctrine_id)


if __name__ == "__main__":
    unittest.main()
