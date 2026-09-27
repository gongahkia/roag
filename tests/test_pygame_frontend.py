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


class PygameDependencyBoundaryTests(unittest.TestCase):
    def test_core_modules_do_not_import_pygame(self):
        import subprocess, sys
        result=subprocess.run([sys.executable,"-c","import sys; import jomon.session,jomon.commands,jomon.views,jomon.runtime_events; print('pygame' in sys.modules)"],text=True,capture_output=True,check=True)
        self.assertEqual(result.stdout.strip(),"False")

if __name__ == "__main__": unittest.main()
