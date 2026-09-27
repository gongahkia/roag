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

    def test_new_game_setup_uses_immutable_view_and_semantic_command(self):
        frontend = self.Frontend(
            __import__("jomon.session", fromlist=["GameSession"]).GameSession.create("pygame-setup"),
            pygame=self.pygame, size=(640, 480), require_character_setup=True,
        )
        self.assertEqual(frontend.panel, "setup")
        self.assertIsNotNone(frontend.setup_draft)
        assert frontend.setup_draft is not None
        frontend.setup_draft.cursor = frontend._setup_fields().index("begin")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "character.setup.completed")
        self.assertIsNone(frontend.panel)
        self.assertIsNone(frontend.setup_draft)

    def test_title_pause_save_settings_and_renderer_switch_are_frontend_local(self):
        from jomon.app_settings import AppSettings, load_app_settings
        from jomon.pygame_frontend import create_frontend
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            settings_file, save_root = root / "settings.json", root / "saves"
            frontend = create_frontend(
                __import__("jomon.session", fromlist=["GameSession"]).GameSession.create("shell"),
                renderer="debug", pygame=self.pygame, shell_mode="title", settings=AppSettings(),
                settings_file=settings_file, save_root=save_root, save_path=save_root / "continue.json",
            )
            self.assertEqual(frontend.panel, "title")
            frontend._activate_shell("join")
            self.assertEqual(frontend.panel, "setup")
            frontend.setup_draft.cursor = frontend._setup_fields().index("begin")
            frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
            before = (frontend.session.revision, frontend.session.world_view())
            frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_ESCAPE, mod=0))
            self.assertEqual(frontend.panel, "pause")
            self.assertEqual((frontend.session.revision, frontend.session.world_view()), before)
            frontend._activate_shell("save")
            frontend._activate_shell("save.new")
            self.assertTrue(tuple(save_root.glob("*.json")))
            frontend._open_panel("settings")
            frontend._activate_shell("renderer.ascii")
            self.assertEqual(load_app_settings(settings_file), AppSettings("ascii"))
            replacement = frontend._replacement_renderer("ascii")
            self.assertEqual(replacement.renderer_id, "ascii")
            self.assertIs(replacement.session, frontend.session)
            self.assertEqual(replacement.session.world_view(), before[1])

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

    def test_guard_and_retreat_are_submitted_through_session_commands(self):
        from jomon.state import Position, Threat

        frontend = self.frontend(); state = frontend.session._state
        state.location, state.position, state.world_time = "region", Position(40, 25), 8
        state.weather, state.weapon = "clear", "billhook"
        for z in (-1, 0, 1):
            for y in range(20, 31):
                for x in range(30, 55): state.region.tile_changes[f"{x},{y},{z}"] = "."
        state.threats = [Threat("pygame-guard", "ignored", "pursuer", Position(41, 25), 20, 20,
                               status="engaged", morale=8)]
        frontend.selected_actor_id = "pygame-guard"
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_g, mod=0))
        self.assertIn(frontend.last_result, {"guard.resolved", "guard.rejected"})
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_r, mod=0))
        self.assertIn(frontend.last_result, {"retreat.resolved", "retreat.rejected"})

    def test_active_game_hotkeys_use_session_commands_or_activity_views(self):
        frontend = self.frontend()
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_l, mod=0))
        self.assertEqual(frontend.panel, "activity")
        self.assertEqual(frontend.activity_context, "loadout")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_ESCAPE, mod=0))
        # Away from a vessel station the semantic view truthfully has no
        # operation rather than manufacturing a frontend-only action.
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_v, mod=0))
        self.assertIsNotNone(frontend.notification)

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

    def test_activity_panel_uses_session_view_and_semantic_command(self):
        from jomon.production import site_position

        frontend = self.frontend()
        state = frontend.session._state
        state.location, state.position = "region", site_position(state)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_c, mod=0))
        self.assertEqual(frontend.panel, "activity")
        self.assertEqual(frontend.activity_context, "production")
        self.assertTrue(frontend._panel_rows())
        frontend.draw()
        frontend.panel_cursor = next(index for index, row in enumerate(frontend._panel_rows())
                                     if row.action_id == "production.gather:0")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "activity.resolved")

    def test_inventory_transfer_and_drop_use_stable_item_ids(self):
        frontend = self.frontend()
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_i, mod=0))
        equipped = frontend.session.equipment_view().slots[0]
        frontend.panel_cursor = next(index for index, row in enumerate(frontend._panel_rows()) if row.id == equipped.id)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_r, mod=0))
        frontend.panel_cursor = next(index for index, row in enumerate(frontend._panel_rows()) if row.id == equipped.id)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_t, mod=0))
        self.assertEqual(frontend.last_result, "item.moved")
        frontend.panel_cursor = next(index for index, row in enumerate(frontend._panel_rows()) if row.id == equipped.id)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_t, mod=0))
        frontend.panel_cursor = next(index for index, row in enumerate(frontend._panel_rows()) if row.id == equipped.id)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_d, mod=0))
        self.assertEqual(frontend.last_result, "item.dropped")

    def test_container_interaction_opens_a_stable_source_inventory_and_transfers(self):
        from jomon.inventory import create_item

        frontend = self.frontend(); state = frontend.session._state
        state.location = "region"
        container = state.region.containers[0]
        state.position, container.opened = container.position, True
        item = create_item(state, "passive:rain cape", "pygame fixture", location="container")
        item.container_id = container.id
        container.item_ids.append(item.id)

        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_e, mod=0))
        self.assertEqual(frontend.panel, "inventory")
        self.assertEqual(frontend.inventory_source, f"container:{container.id}")
        self.assertEqual(frontend._panel_rows()[0].id, item.id)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_t, mod=0))
        self.assertEqual(frontend.last_result, "item.moved")
        self.assertNotIn(item.id, container.item_ids)
        self.assertEqual(next(row for row in frontend.session.inventory_view().items if row.id == item.id).location_id, "pack")

    def test_resolved_single_interaction_without_overlay_does_not_crash(self):
        """A successful interaction may resolve in-world instead of opening UI."""
        from jomon.session import CommandOutcome
        from jomon.state import Position
        from jomon.views import InteractionOptionView, InteractionView

        frontend = self.frontend()
        frontend.session.interaction_view = lambda: InteractionView(
            Position(0, 0), (InteractionOptionView("interact.resolved", "fixture", True),),
        )
        frontend.submit = lambda command: CommandOutcome(
            True, True, False, "interaction.resolved", 1,
        )

        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_e, mod=0))
        self.assertIsNone(frontend.panel)

    def test_inventory_auto_place_preference_uses_the_session_command(self):
        frontend = self.frontend()
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_i, mod=0))
        before = frontend.session.auto_place_enabled
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_o, mod=0))
        self.assertNotEqual(frontend.session.auto_place_enabled, before)
        self.assertIn("Auto-place", frontend.notification.text)

    def test_open_inventory_prefers_recoverable_ground_items(self):
        from jomon.commands import DropItemCommand, UnequipItemCommand

        frontend = self.frontend()
        equipped = frontend.session.equipment_view().slots[0]
        self.assertTrue(frontend.submit(UnequipItemCommand(equipped.location_id)).changed)
        self.assertTrue(frontend.submit(DropItemCommand(equipped.id)).changed)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_i, mod=0))
        self.assertEqual(frontend.panel, "inventory")
        self.assertEqual(frontend.inventory_source, "ground")
        self.assertEqual(frontend._panel_rows()[0].id, equipped.id)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_t, mod=0))
        self.assertEqual(frontend.last_result, "item.moved")

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

    def test_dummy_active_game_flow_crosses_the_public_graphical_boundary(self):
        """Exercise the ordinary frontend flow without a curses bootstrap.

        This is intentionally a compact SDL-dummy integration scenario.  The
        specialised tests above cover the detailed reducer results; here the
        value is proving that a newly configured graphical session can move,
        inspect, use an activity, travel, enter Draw, and round-trip a save by
        following the same public frontend/session boundary as a player.
        """
        from jomon.production import site_position
        from jomon.session import GameSession
        from jomon.vessel import DRAW_PLAYER_SEAT

        frontend = self.Frontend(
            GameSession.create("pygame-active-flow"), pygame=self.pygame,
            size=(640, 480), require_character_setup=True,
        )
        assert frontend.setup_draft is not None
        frontend.setup_draft.cursor = frontend._setup_fields().index("begin")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "character.setup.completed")

        view = frontend.session.world_view()
        selected = next(cell for cell in view.cells if cell.visible)
        frontend._select_at(frontend._rect(selected.position, frontend._camera(view)).center)
        self.assertIsNotNone(frontend.inspection_data())

        # Movement comes through a semantic command generated by graphical
        # input; a blocked direction is harmless, so select any legal one.
        for key in (self.pygame.K_UP, self.pygame.K_DOWN, self.pygame.K_LEFT, self.pygame.K_RIGHT):
            frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=key, mod=0))
            if frontend.last_result == "move.ok":
                break
        self.assertEqual(frontend.last_result, "move.ok")

        # A production fixture supplies a reachable active system, while the
        # panel itself discovers and resolves only its immutable activity view.
        state = frontend.session._state
        state.location, state.position = "region", site_position(state)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_c, mod=0))
        frontend.panel_cursor = next(index for index, row in enumerate(frontend._panel_rows())
                                     if row.action_id == "production.gather:0")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "activity.resolved")

        state.location = "jomon"
        frontend.panel = None
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_t, mod=0))
        frontend.panel_cursor = next(index for index, row in enumerate(frontend._panel_rows()) if row.available)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "travel.resolved")

        # Draw is reached through the ordinary world interaction result and
        # its public tavern view, never a terminal loop.
        state.jomon_space, state.position = "tavern", DRAW_PLAYER_SEAT
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_e, mod=0))
        self.assertEqual(frontend.panel, "tavern-draw")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "tavern.game.started")

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "active-flow.json"
            frontend.session.save(path)
            restored = GameSession.load(path)
        self.assertEqual(restored.tavern_draw_view(), frontend.session.tavern_draw_view())


class PygameDependencyBoundaryTests(unittest.TestCase):
    def test_core_modules_do_not_import_pygame(self):
        import subprocess, sys
        result=subprocess.run([sys.executable,"-c","import sys; import jomon.session,jomon.commands,jomon.views,jomon.runtime_events; print('pygame' in sys.modules)"],text=True,capture_output=True,check=True)
        self.assertEqual(result.stdout.strip(),"False")

    def test_graphical_core_surface_does_not_reach_session_private_state(self):
        source=Path("jomon/pygame_frontend.py").read_text(encoding="utf-8")
        self.assertNotIn("session._state",source)

if __name__ == "__main__": unittest.main()
