import unittest

from jomon.verification import (
    content_audit, encounter_verification, memory_soak, persistence_audit,
    quest_verification, replay_audit,
)


class VerificationCommandTests(unittest.TestCase):
    def test_content_audit_reports_exact_integrated_targets(self):
        report = content_audit("focused content audit")
        self.assertEqual(report["failures"], [])
        self.assertEqual(report["counts"]["standard_enemies"], 72)
        self.assertEqual(report["counts"]["elite_situations"], 24)
        self.assertEqual(report["counts"]["named_rivals"], 8)
        self.assertEqual(report["counts"]["weapons"], 36)
        self.assertEqual(report["counts"]["armour"], 36)
        self.assertEqual(report["counts"]["techniques"], 32)
        self.assertEqual(report["counts"]["active_passives_and_techniques"], 80)
        self.assertEqual(report["counts"]["secondary_tools_supplies_drinks"], 51)
        self.assertEqual(report["counts"]["relics"], 16)
        self.assertEqual(report["counts"]["regional_questlines"], 20)
        self.assertEqual(report["counts"]["cross_region_arcs"], 5)
        self.assertEqual(report["counts"]["institutions"], 12)
        self.assertEqual(report["counts"]["persistent_nonhostile_characters"], 38)
        self.assertEqual(report["counts"]["vessel_refits"], 8)

    def test_persistence_and_replay_audits_are_exact(self):
        persistence = persistence_audit("focused persistence audit")
        self.assertEqual(persistence["failures"], [])
        self.assertTrue(persistence["format_7_round_trip"])
        self.assertTrue(persistence["format_6_item_identities_preserved"])
        self.assertEqual(replay_audit(2)["failures"], [])

    def test_encounter_and_all_region_quest_audits_expose_failures(self):
        self.assertEqual(encounter_verification(2)["failures"], [])
        quest = quest_verification(1)
        self.assertEqual(quest["failures"], [])
        self.assertEqual(quest["regions_checked"], 8)
        self.assertEqual(quest["cross_region_arcs"], 5)

    def test_short_actual_route_soak_stays_bounded(self):
        report = memory_soak(6)
        self.assertEqual(report["failures"], [])
        self.assertLessEqual(report["bounds"]["history"], 40)
        self.assertLessEqual(report["bounds"]["messages"], 8)


if __name__ == "__main__":
    unittest.main()
