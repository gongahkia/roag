from __future__ import annotations
import importlib.util
import os
from pathlib import Path
import tempfile
import unittest

HAS_PYGAME = importlib.util.find_spec("pygame") is not None

@unittest.skipUnless(HAS_PYGAME, "optional pygame-ce dependency is not installed")
class PygameFrontendTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
        os.environ.setdefault("SDL_AUDIODRIVER", "dummy")
        import pygame
        pygame.init()
        cls.pygame = pygame
        from jomon.pygame_frontend import PygameFrontend
        cls.Frontend = PygameFrontend

    @classmethod
    def tearDownClass(cls): cls.pygame.quit()

    def frontend(self):
        from jomon.session import GameSession
        return self.Frontend(GameSession.create("pygame-slice"), pygame=self.pygame, size=(640,480))

    def test_draws_semantic_view_and_loads_real_image_resource(self):
        frontend=self.frontend(); frontend.draw()
        self.assertIsNotNone(frontend.resources.image("image.terrain.floor", frontend.tile_size))
        cell=frontend.session.world_view().cells[0]
        self.assertTrue(cell.terrain_id.startswith("terrain."))
        self.assertFalse(hasattr(cell,"topology_token"))

    def test_input_commands_and_events_stay_outside_engine(self):
        from jomon.commands import MoveCommand
        frontend=self.frontend(); before=frontend.session.world_view().courier_position
        event=self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_RIGHT)
        frontend.handle_event(event)
        self.assertIn(frontend.last_result,{"move.ok","move.rejected"})
        # Direct semantic submission also queues presentation-only movement feedback.
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            outcome=frontend.submit(MoveCommand(dx,dy))
            if outcome.changed:
                self.assertTrue(frontend.motions); break
        self.assertIsNotNone(frontend.session.world_view().courier_position)

    def test_event_audio_and_save_load_are_frontend_local(self):
        from jomon.runtime_events import DamageApplied
        from jomon.session import GameSession
        frontend=self.frontend(); frontend.consume_events((DamageApplied("a","missing",3,"damage.normal","body"),))
        self.assertTrue(frontend.feedback)
        # No device/path failure can mutate the session.
        before=frontend.session.world_view().courier_position
        frontend.resources.play("audio.event.damage")
        self.assertIn("audio.event.damage", frontend.resources.sounds)
        self.assertEqual(frontend.session.world_view().courier_position,before)
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/"game.json"; frontend.session.save(path); loaded=GameSession.load(path)
        self.assertEqual(loaded.world_view().courier_position,before)

    def test_selection_projects_visible_semantic_inspection_without_mutation(self):
        frontend=self.frontend(); view=frontend.session.world_view()
        cell=next(cell for cell in view.cells if cell.visible)
        before=(frontend.session.revision, frontend.session.world_view())
        frontend._select_at(frontend._rect(cell.position, frontend._camera(view)).center)
        data=frontend.inspection_data()
        self.assertIsNotNone(data)
        assert data is not None
        self.assertEqual(data.position, cell.position)
        self.assertEqual(data.terrain_id, cell.terrain_id)
        self.assertIn("Terrain:", "\n".join(frontend._inspection_lines()))
        self.assertEqual((frontend.session.revision, frontend.session.world_view()), before)

    def test_actor_inspection_uses_view_presentation_and_hidden_cells_reveal_nothing(self):
        from jomon.state import Position, Threat
        frontend=self.frontend(); state=frontend.session._state
        state.location, state.position = "region", Position(40,25)
        for y in range(20,31):
            for x in range(30,55):
                state.region.tile_changes[f"{x},{y},0"]="."
        visible=next(cell for cell in frontend.session.world_view().cells if cell.visible and cell.position != state.position)
        target=Threat("inspect-target","raw catalog name must not render","pursuer",visible.position,7,9,status="watching",archetype_id="fen-pail")
        state.threats=[target]
        frontend.selected=visible.position; frontend.selected_actor_id=target.id
        data=frontend.inspection_data()
        self.assertIsNotNone(data); assert data is not None and data.actor is not None
        self.assertNotEqual(data.actor.display_name, target.name)
        self.assertIn(data.actor.display_name, "\n".join(frontend._inspection_lines()))
        hidden=next(cell for cell in frontend.session.world_view().cells if not cell.visible and not cell.remembered)
        frontend.selected=hidden.position; frontend.selected_actor_id=target.id
        self.assertIsNone(frontend.inspection_data())

    def test_ctrl_s_saves_with_frontend_notification_and_f5_does_not(self):
        from jomon.commands import MoveCommand
        from jomon.session import GameSession
        frontend=self.frontend()
        for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
            if frontend.submit(MoveCommand(dx,dy)).changed:
                break
        else:
            self.fail("the graphical save/load fixture needs one legal move")
        before_view = frontend.session.world_view()
        before_messages = tuple(frontend.session._state.messages)
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/"game.json"
            frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_F5,mod=0), path)
            self.assertFalse(path.exists())
            frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_s,mod=self.pygame.KMOD_CTRL), path)
            self.assertTrue(path.exists())
            self.assertIsNotNone(frontend.notification)
            self.assertTrue(frontend.notification.text.startswith("Saved"))
            self.assertEqual(tuple(frontend.session._state.messages), before_messages)
            loaded=GameSession.load(path)
            self.assertEqual(loaded.world_view(), before_view)
            frontend.update(2.1)
            self.assertIsNone(frontend.notification)

    def test_real_attack_events_drive_feedback_from_stable_target_id(self):
        from jomon.commands import AttackCommand
        from jomon.state import Position, Threat
        frontend=self.frontend(); state=frontend.session._state
        state.location, state.position, state.world_time = "region", Position(40,25), 8
        state.weather, state.weapon = "clear", "billhook"
        for z in (-1,0,1):
            for y in range(20,31):
                for x in range(30,55): state.region.tile_changes[f"{x},{y},{z}"]="."
        target=Threat("pygame-target","ignored presentation","pursuer",Position(41,25),20,20,status="engaged",morale=8)
        state.threats=[target]
        outcome=frontend.submit(AttackCommand(target.id))
        self.assertTrue(any(event.event_id=="combat.attack.resolved" for event in outcome.events))
        self.assertTrue(any(event.event_id=="combat.damage.applied" for event in outcome.events))
        self.assertTrue(frontend.feedback)

    def test_inventory_quest_and_travel_panels_use_session_views(self):
        frontend=self.frontend()
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_i,mod=0))
        self.assertEqual(frontend.panel,"inventory")
        frontend.draw()
        equipped=frontend.session.equipment_view().slots[0]
        frontend.panel_cursor=next(index for index,row in enumerate(frontend._panel_rows()) if row.id==equipped.id)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_r,mod=0))
        self.assertEqual(frontend.last_result,"item.unequipped")
        frontend.panel_cursor=next(index for index,row in enumerate(frontend._panel_rows()) if "equip" in row.legal_operations)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_e,mod=0))
        self.assertEqual(frontend.last_result,"item.equipped")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_ESCAPE,mod=0))
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_q,mod=0))
        self.assertEqual(frontend.panel,"quests"); frontend.draw()
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_ESCAPE,mod=0))
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_t,mod=0))
        self.assertEqual(frontend.panel,"travel"); frontend.draw()
        destination=next(index for index,row in enumerate(frontend._panel_rows()) if row.available)
        frontend.panel_cursor=destination
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_RETURN,mod=0))
        self.assertEqual(frontend.last_result,"travel.resolved")

    def test_inventory_use_and_multiple_interactions_use_stable_view_records(self):
        from jomon.inventory import auto_place, create_item
        from jomon.state import Position
        from jomon.travel import choose_destination
        frontend=self.frontend(); state=frontend.session._state
        state.location, state.position = "region", Position(40,24); state.water["40,24,0"] = 1
        item=create_item(state,"consumable:preparation.waterline","fixture",owner_id=state.active_courier_id)
        self.assertTrue(auto_place(state,item.id,"pack",owner_id=state.active_courier_id))
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_i,mod=0))
        frontend.panel_cursor=next(index for index,row in enumerate(frontend._panel_rows()) if row.id==item.id)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_u,mod=0))
        self.assertEqual(frontend.last_result,"gear.resolved")
        state.location="jomon"
        destination=next(row.destination_id for row in frontend.session.travel_view().destinations if row.available)
        self.assertTrue(choose_destination(state,destination,forced_voyage="raiders")[0])
        frontend.panel=None
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_e,mod=0))
        self.assertEqual(frontend.panel,"interaction")
        frontend.panel_cursor=next(index for index,row in enumerate(frontend._panel_rows()) if row.interaction_id=="voyage.response.y")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN,key=self.pygame.K_RETURN,mod=0))
        self.assertEqual(frontend.last_result,"voyage.resolved")

    def test_tavern_draw_and_dice_panels_use_only_session_views_and_commands(self):
        from jomon.vessel import DICE_PLAYER_SEAT, DRAW_PLAYER_SEAT

        frontend = self.frontend(); state = frontend.session._state
        state.jomon_space, state.position = "tavern", DRAW_PLAYER_SEAT
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_e, mod=0))
        self.assertEqual(frontend.panel, "tavern-draw")
        frontend.draw()
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "tavern.game.started")
        view = frontend.session.tavern_draw_view()
        self.assertTrue(view.active)
        self.assertEqual(len(view.hand), 5)
        self.assertFalse(hasattr(view, "deck"))
        if "draw.exchange" in view.legal_actions:
            frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_1, mod=0))
            frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
            self.assertEqual(frontend.last_result, "tavern.draw.exchanged")
        self.assertTrue(frontend.feedback)

        frontend = self.frontend(); state = frontend.session._state
        state.jomon_space, state.position = "tavern", DICE_PLAYER_SEAT
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_e, mod=0))
        self.assertEqual(frontend.panel, "tavern-dice")
        frontend.draw()
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "tavern.game.started")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_r, mod=0))
        self.assertEqual(frontend.last_result, "tavern.dice.resolved")
        self.assertTrue(any(note.text.startswith("Rolled") for note in frontend.feedback))
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_ESCAPE, mod=0))
        self.assertIsNone(frontend.panel)


class PygameDependencyBoundaryTests(unittest.TestCase):
    def test_core_modules_do_not_import_pygame(self):
        import subprocess, sys
        result=subprocess.run([sys.executable,"-c","import sys; import jomon.session,jomon.commands,jomon.views,jomon.runtime_events; print('pygame' in sys.modules)"],text=True,capture_output=True,check=True)
        self.assertEqual(result.stdout.strip(),"False")

    def test_graphical_core_surface_does_not_reach_session_private_state(self):
        source=Path("jomon/pygame_frontend.py").read_text(encoding="utf-8")
        self.assertNotIn("session._state",source)

if __name__ == "__main__": unittest.main()
