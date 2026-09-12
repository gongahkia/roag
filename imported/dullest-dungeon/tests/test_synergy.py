from __future__ import annotations

import json
import tempfile
import unittest
from dataclasses import replace
from pathlib import Path

from dumbest_dungeon.content import ContentError, load_catalog
from dumbest_dungeon.synergy import Relation, analyze_catalog, analyze_pool, derive_edges


class SynergyTests(unittest.TestCase):
    def test_self_reward_condition_reads_primary_enemy_target(self) -> None:
        edges = derive_edges(load_catalog(), {"cross_reference", "backdoor"})
        conditions = {(edge.source_id, edge.mechanic) for edge in edges if edge.relation == Relation.REQUIRES}
        self.assertIn(("cross_reference", "enemy:marked"), conditions)
        self.assertIn(("backdoor", "enemy:vulnerable"), conditions)
        self.assertNotIn(("cross_reference", "crew:marked"), conditions)

    def test_eligible_pool_distinguishes_orphan_thin_and_external_setup(self) -> None:
        catalog = load_catalog()
        report = analyze_pool(catalog, {"cross_reference"})
        self.assertIn("unsupported_payoff", {d["code"] for d in report["diagnostics"]})
        report = analyze_pool(catalog, {"challenge"})
        self.assertIn("orphan_producer", {d["code"] for d in report["diagnostics"]})
        report = analyze_pool(catalog, {"field_dressing"})
        self.assertIn("encounter_dependent_setup", {d["code"] for d in report["diagnostics"]})
        self.assertTrue(report["densities"]["crew:wound"]["external_enemies"])

    def test_dependency_reports_owner_death_and_rank_risk_without_claiming_inert_card(self) -> None:
        catalog = load_catalog()
        squad = tuple(catalog.squads["wound_ward"]["formation"])
        # Keep this diagnostic fixture intentionally thin. The expanded live
        # squad now has multiple mark producers, which is the desired repair.
        cards = {"crushing_depth", "specimen_scan"}
        report = analyze_pool(catalog, cards, squad)
        risk = next(d for d in report["diagnostics"] if d["code"] == "single_owner_dependency")
        self.assertEqual("biologist", risk["provider"])
        self.assertEqual(["crushing_depth"], risk["cards"])
        self.assertIn("fallback", risk["meaning"])
        self.assertEqual(0, report["rank_access"][0]["rank_invalid"])

    def test_analysis_is_independent_of_definition_enumeration(self) -> None:
        catalog = load_catalog()
        reordered = replace(catalog, cards=dict(reversed(list(catalog.cards.items()))))
        self.assertEqual(analyze_catalog(catalog), analyze_catalog(reordered))

    def test_tags_cannot_advertise_an_unimplemented_payoff(self) -> None:
        raw = json.loads(json.dumps(load_catalog().raw))
        raw["cards"][0]["tags"].append("payoff:wound")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "content.json"
            path.write_text(json.dumps(raw))
            with self.assertRaisesRegex(ContentError, "unsupported by effects"):
                load_catalog(path)


if __name__ == "__main__":
    unittest.main()
