from __future__ import annotations

import unittest
from pathlib import Path

from jomon.catalog import select_content_pack, template_root
from jomon.commands import EquipItemCommand, InteractCommand, MoveCommand
from jomon.session import GameSession
from jomon.state import Position
from jomon.views import readable_result_text


PACK = Path(__file__).parents[1] / "jomon" / "content_packs" / "first-playable"


class ContextAndFogTests(unittest.TestCase):
    def setUp(self) -> None:
        select_content_pack(PACK)
        self.session = GameSession.create("ux-context")

    def tearDown(self) -> None:
        select_content_pack(template_root())

    def move(self, dx: int, dy: int, count: int = 1) -> None:
        for _ in range(count):
            self.assertTrue(self.session.submit(MoveCommand(dx, dy)).accepted)

    def test_current_visibility_is_remembered_but_actors_never_are(self) -> None:
        state = self.session._state
        initial_visible = {row.position for row in self.session.world_view().cells if row.visible}
        self.assertTrue(initial_visible.issubset(state.remembered))
        defender = self.session.actor_view("actor.service-defender")
        self.assertFalse(defender.visible)
        hidden_context = self.session.context_view(defender.position)
        self.assertEqual(hidden_context.entity_type_id, "none")

        self.move(1, 0, 3)
        defender = self.session.actor_view("actor.service-defender")
        self.assertTrue(defender.visible)
        visible_context = self.session.context_view(defender.position)
        self.assertEqual(visible_context.relation_id, "hostile")
        self.assertEqual((visible_context.health, visible_context.maximum_health), (4, 4))
        self.assertEqual(visible_context.actions[0].binding, "F")

        self.move(-1, 0, 3)
        defender = self.session.actor_view("actor.service-defender")
        self.assertFalse(defender.visible)
        # The terrain square is remembered, while the defender itself is not.
        self.assertIn(defender.position, state.remembered)
        hidden_again = self.session.context_view(defender.position)
        self.assertNotEqual(hidden_again.relation_id, "hostile")
        self.assertNotIn("defender", hidden_again.display_name.lower())

        state.actors[0].alive = False  # labelled adversarial fixture: corpse visibility is presentation-only.
        self.move(1, 0, 3)
        corpse = self.session.context_view(defender.position)
        self.assertEqual(corpse.entity_type_id, "remains")
        self.move(-1, 0, 3)
        self.assertNotEqual(self.session.context_view(defender.position).entity_type_id, "remains")

    def test_context_projects_generic_requirements_and_plain_feedback(self) -> None:
        friendly = self.session.context_view(Position(1, 2))
        self.assertEqual(friendly.relation_id, "ally")
        self.assertEqual(friendly.actions, ())
        # The latch becomes visible and adjacent without changing its prerequisites.
        self.move(0, -1, 2)
        self.move(1, 0, 6)
        latch = self.session.context_view(Position(9, 1))
        self.assertEqual(latch.entity_type_id, "maintenance_latch")
        self.assertEqual(latch.actions[0].binding, "E")
        self.assertFalse(latch.actions[0].enabled)
        requirements = {row.label: row.met for row in latch.requirements}
        self.assertFalse(requirements["Maintenance tool equipped"])
        self.assertTrue(requirements["Maintenance Service learned"])
        self.assertEqual(latch.actions[0].reason_text, "Equip the required tool first.")

        self.assertTrue(self.session.submit(EquipItemCommand("item.maintenance-tool")).accepted)
        ready = self.session.context_view(Position(9, 1))
        self.assertTrue(ready.actions[0].enabled)
        self.assertEqual(ready.actions[0].reason_text, "Available.")

        self.assertTrue(self.session.submit(InteractCommand("feature.maintenance-latch")).accepted)
        self.move(1, 0, 8)
        self.move(0, 1, 2)
        cache = self.session.context_view(Position(16, 3))
        self.assertEqual(cache.entity_type_id, "objective_cache")
        self.assertIn("assigned objective", cache.operation_note)
        self.assertTrue(cache.actions[0].enabled)

        base = self.session.context_view(Position(2, 3))
        self.assertEqual(base.entity_type_id, "base")
        self.assertIn("Deliver", base.operation_note)
        self.assertEqual(readable_result_text("interaction.requires-capability"), "You lack the required learned capability.")
        self.assertEqual(readable_result_text("attack.rejected"), "Target is outside melee range or cannot be attacked.")
        self.assertEqual(readable_result_text("interaction.access-opened"), "The maintenance route opens.")

    def test_visible_terrain_updates_on_each_move(self) -> None:
        self.move(1, 0)
        visible = {row.position for row in self.session.world_view().cells if row.visible}
        self.assertTrue(visible.issubset(self.session._state.remembered))
        self.assertIn(Position(2, 3), self.session._state.remembered)

    def test_visible_remains_context_only_enables_local_recovery(self) -> None:
        survivor = next(row for row in self.session._state.crew if row.id == "crew.survivor-one")
        survivor.alive = False  # focused UI fixture; actual death/recovery is covered by continuity tests.
        far = self.session.context_view(survivor.position)
        self.assertEqual(far.entity_type_id, "remains")
        self.assertFalse(far.actions[0].enabled)
        self.assertEqual(far.actions[0].reason_text, "Move next to the body to recover items.")
        self.move(0, -1)
        near = self.session.context_view(survivor.position)
        self.assertTrue(near.actions[0].enabled)
        self.assertEqual(near.actions[0].binding, "R")


if __name__ == "__main__":
    unittest.main()
